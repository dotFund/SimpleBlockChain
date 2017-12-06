using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SimpleBlockchain.Network
{
    public class LocalPeer : IDisposable
    {
        public const uint ProtocolVersion = 0;
        private const int ConnectedMax = 10;
        private const int UnconnectedMax = 1000;
        public const int MemoryPoolSize = 30000;

        private static readonly HashSet<IPEndPoint> unconnectedPeers = new HashSet<IPEndPoint>();
        private static readonly HashSet<IPEndPoint> badPeers = new HashSet<IPEndPoint>();
        internal readonly List<RemotePeer> connectedPeers = new List<RemotePeer>();

        internal static readonly HashSet<IPAddress> LocalAddresses = new HashSet<IPAddress>();
        internal ushort Port;
        internal readonly uint Nonce;
        private TcpListener listener;
        //private IWebHost ws_host;
        private Thread connectThread;
        private Thread poolThread;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public bool GlobalMissionsEnabled { get; set; } = true;
        public int RemoteNodeCount => connectedPeers.Count;
        public bool ServiceEnabled { get; set; } = true;
        public bool UpnpEnabled { get; set; } = false;
        public string UserAgent { get; set; }

        static LocalPeer()
        {
            //Insert local Ip Address into LocalAddresses
            LocalAddresses.UnionWith(NetworkInterface.GetAllNetworkInterfaces().SelectMany(p => p.GetIPProperties().UnicastAddresses).Select(p => p.Address.MapToIPv6()));
        }

        public LocalPeer()
        {
            Random rand = new Random();
            this.Nonce = (uint)rand.Next();
            this.connectThread = new Thread(ConnectToPeersLoop)
            {
                IsBackground = true,
                Name = "LocalPeer.ConnectToPeersLoop"
            };

            /*
            if (Blockchain.Default != null)
            {
                this.poolThread = new Thread(AddTransactionLoop)
                {
                    IsBackground = true,
                    Name = "LocalNode.AddTransactionLoop"
                };
            }
            */
            this.UserAgent = string.Format("dfBlockchain:{0}", GetType().GetTypeInfo().Assembly.GetName().Version.ToString(3));
            //Blockchain.PersistCompleted += Blockchain_PersistCompleted;
        }

        private void ConnectToPeersLoop()
        {
            while(!cancellationTokenSource.IsCancellationRequested)
            {
                int connectedCount = connectedPeers.Count;
                int unconnectedCount = unconnectedPeers.Count;

                if (connectedCount < ConnectedMax)
                {
                    Task[] tasks = { };
                    if (unconnectedCount > 0)
                    {
                        IPEndPoint[] endpoints;
                        lock (unconnectedPeers)
                        {
                            endpoints = unconnectedPeers.Take(ConnectedMax - connectedCount).ToArray();
                        }
                        tasks = endpoints.Select(p => ConnectToPeerAsync(p)).ToArray();
                    }
                    else if (connectedCount > 0)
                    {
                        lock (connectedPeers)
                        {
                            foreach (RemotePeer node in connectedPeers)
                                node.RequestPeers();
                        }
                    }
                    else
                    {
                        tasks = Settings.Default.SeedList.OfType<string>().Select(p => p.Split(':')).Select(p => ConnectToPeerAsync(p[0], int.Parse(p[1]))).ToArray();
                    }

                    try
                    {
                        Task.WaitAll(tasks, cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                for (int i = 0; i < 50 && !cancellationTokenSource.IsCancellationRequested; i++)
                {
                    Thread.Sleep(100);
                }
            }
        }

        public async Task ConnectToPeerAsync(string hostNameOrAddress, int port)
        {
            IPAddress ipAddress;

            if (IPAddress.TryParse(hostNameOrAddress, out ipAddress))
            {
                ipAddress = ipAddress.MapToIPv6();
            }
            else
            {
                IPHostEntry entry;
                try
                {
                    entry = await Dns.GetHostEntryAsync(hostNameOrAddress);
                }
                catch (SocketException)
                {
                    return;
                }
                ipAddress = entry.AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo)?.MapToIPv6();
                if (ipAddress == null) return;
            }

            await ConnectToPeerAsync(new IPEndPoint(ipAddress, port));
        }

        public async Task ConnectToPeerAsync(IPEndPoint remoteEndpoint)
        {
            if (remoteEndpoint.Port == Port && LocalAddresses.Contains(remoteEndpoint.Address)) return;

            lock (unconnectedPeers)
            {
                unconnectedPeers.Remove(remoteEndpoint);
            }
            lock (connectedPeers)
            {
                if (connectedPeers.Any(p => remoteEndpoint.Equals(p.ListenerEndpoint)))
                    return;
            }

            TcpRemotePeer remoteNode = new TcpRemotePeer(this, remoteEndpoint);
            if (await remoteNode.ConnectAsync())
            {
                OnConnected(remoteNode);
            }
        }

        private void OnConnected(RemotePeer remotePeer)
        {
            lock (connectedPeers)
            {
                connectedPeers.Add(remotePeer);
            }

            remotePeer.Disconnected += RemoteNode_Disconnected;
            remotePeer.InventoryReceived += RemoteNode_InventoryReceived;
            remotePeer.PeersReceived += RemoteNode_PeersReceived;
            remotePeer.StartProtocol();
        }

        private void RemoteNode_Disconnected(object sender, bool error)
        {
            RemotePeer remoteNode = (RemotePeer)sender;
            remoteNode.Disconnected -= RemoteNode_Disconnected;
            remoteNode.InventoryReceived -= RemoteNode_InventoryReceived;
            remoteNode.PeersReceived -= RemoteNode_PeersReceived;
            if (error && remoteNode.ListenerEndpoint != null)
            {
                lock (badPeers)
                {
                    badPeers.Add(remoteNode.ListenerEndpoint);
                }
            }
            lock (unconnectedPeers)
            {
                lock (connectedPeers)
                {
                    if (remoteNode.ListenerEndpoint != null)
                    {
                        unconnectedPeers.Remove(remoteNode.ListenerEndpoint);
                    }
                    connectedPeers.Remove(remoteNode);
                }
            }
        }

        private void RemoteNode_InventoryReceived(object sender, IInventory inventory)
        {
            /*
            if (inventory is Transaction tx && tx.Type != TransactionType.ClaimTransaction && tx.Type != TransactionType.IssueTransaction)
            {
                if (Blockchain.Default == null) return;
                lock (KnownHashes)
                {
                    if (!KnownHashes.Add(inventory.Hash)) return;
                }
                InventoryReceivingEventArgs args = new InventoryReceivingEventArgs(inventory);
                InventoryReceiving?.Invoke(this, args);
                if (args.Cancel) return;
                lock (temp_pool)
                {
                    temp_pool.Add(tx);
                }
                new_tx_event.Set();
            }
            else
            {
                Relay(inventory);
            }
            */
        }

        private void RemoteNode_PeersReceived(object sender, IPEndPoint[] peers)
        {
            lock (unconnectedPeers)
            {
                if (unconnectedPeers.Count < UnconnectedMax)
                {
                    lock (badPeers)
                    {
                        lock (connectedPeers)
                        {
                            unconnectedPeers.UnionWith(peers);
                            unconnectedPeers.ExceptWith(badPeers);
                            unconnectedPeers.ExceptWith(connectedPeers.Select(p => p.ListenerEndpoint));
                        }
                    }
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
