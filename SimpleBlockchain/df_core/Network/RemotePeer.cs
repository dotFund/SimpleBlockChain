using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using SimpleBlockchain.Cryptography;
using SimpleBlockchain.Network.Payloads;
using SimpleBlockchain.IO;

namespace SimpleBlockchain.Network
{
    public abstract class RemotePeer : IDisposable
    {
        public event EventHandler<bool> Disconnected;
        internal event EventHandler<IInventory> InventoryReceived;
        internal event EventHandler<IPEndPoint[]> PeersReceived;

        private static readonly TimeSpan HalfMinute = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan HalfHour = TimeSpan.FromMinutes(30);

        private Queue<Message> message_queue = new Queue<Message>();
        private static HashSet<UInt256> missions_global = new HashSet<UInt256>();
        private HashSet<UInt256> missions = new HashSet<UInt256>();
        private DateTime mission_start = DateTime.Now.AddYears(100);

        private LocalPeer localNode;
        private int disposed = 0;
        private BloomFilter bloom_filter;

        public VersionPayload Version { get; private set; }
        public IPEndPoint RemoteEndpoint { get; protected set; }
        public IPEndPoint ListenerEndpoint { get; protected set; }

        protected RemotePeer(LocalPeer localNode)
        {
            this.localNode = localNode;
        }

        public virtual void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                Disconnected?.Invoke(this, error);
                bool needSync = false;
                lock (missions_global)
                    lock (missions)
                        if (missions.Count > 0)
                        {
                            missions_global.ExceptWith(missions);
                            needSync = true;
                        }
                if (needSync)
                    lock (localNode.connectedPeers)
                        foreach (RemotePeer node in localNode.connectedPeers)
                        {
                            //node.EnqueueMessage("getblocks", GetBlocksPayload.Create(Blockchain.Default.CurrentBlockHash), true);
                        }
            }
        }

        public void Dispose()
        {
            Disconnect(false);
        }

        public void EnqueueMessage(string command, ISerializable payload = null)
        {
            EnqueueMessage(command, payload, false);
        }

        private void EnqueueMessage(string command, ISerializable payload, bool is_single)
        {
            lock (message_queue)
            {
                if (!is_single || message_queue.All(p => p.Command != command))
                {
                    message_queue.Enqueue(Message.Create(command, payload));
                }
            }
        }

        internal void RequestPeers()
        {
            EnqueueMessage("getaddr", null, true);
        }

        protected abstract Task<bool> SendMessageAsync(Message message);
        protected abstract Task<Message> ReceiveMessageAsync(TimeSpan timeout);

        private async void StartSendLoop()
        {
            while (disposed == 0)
            {
                Message message = null;
                lock (message_queue)
                {
                    if (message_queue.Count > 0)
                    {
                        message = message_queue.Dequeue();
                    }
                }
                if (message == null)
                {
                    for (int i = 0; i < 10 && disposed == 0; i++)
                    {
                        Thread.Sleep(100);
                    }
                }
                else
                {
                    await SendMessageAsync(message);
                }
            }
        }

        internal async void StartProtocol()
        {
            if (!await SendMessageAsync(Message.Create("version", VersionPayload.Create(localNode.Port, localNode.Nonce, localNode.UserAgent))))
                return;
            Message message = await ReceiveMessageAsync(HalfMinute);
            if (message == null) return;
            if (message.Command != "version")
            {
                Disconnect(true);
                return;
            }
            try
            {
                Version = message.Payload.AsSerializable<VersionPayload>();
            }
            catch (EndOfStreamException)
            {
                Disconnect(false);
                return;
            }
            catch (FormatException)
            {
                Disconnect(true);
                return;
            }
            if (Version.Nonce == localNode.Nonce)
            {
                Disconnect(true);
                return;
            }
            bool isSelf;
            lock (localNode.connectedPeers)
            {
                isSelf = localNode.connectedPeers.Where(p => p != this).Any(p => p.RemoteEndpoint.Address.Equals(RemoteEndpoint.Address) && p.Version?.Nonce == Version.Nonce);
            }
            if (isSelf)
            {
                Disconnect(false);
                return;
            }
            if (ListenerEndpoint != null)
            {
                if (ListenerEndpoint.Port != Version.Port)
                {
                    Disconnect(true);
                    return;
                }
            }
            else if (Version.Port > 0)
            {
                ListenerEndpoint = new IPEndPoint(RemoteEndpoint.Address, Version.Port);
            }
            if (!await SendMessageAsync(Message.Create("verack"))) return;
            message = await ReceiveMessageAsync(HalfMinute);
            if (message == null) return;
            if (message.Command != "verack")
            {
                Disconnect(true);
                return;
            }
            /*
            if (Blockchain.Default?.HeaderHeight < Version.StartHeight)
            {
                EnqueueMessage("getheaders", GetBlocksPayload.Create(Blockchain.Default.CurrentHeaderHash), true);
            }
            */
            StartSendLoop();
            
            while (disposed == 0)
            {
                /*
                if (Blockchain.Default != null)
                {
                    if (missions.Count == 0 && Blockchain.Default.Height < Version.StartHeight)
                    {
                        EnqueueMessage("getblocks", GetBlocksPayload.Create(Blockchain.Default.CurrentBlockHash), true);
                    }
                } */
                TimeSpan timeout = missions.Count == 0 ? HalfHour : OneMinute;
                message = await ReceiveMessageAsync(timeout);
                if (message == null) break;
                if (DateTime.Now - mission_start > OneMinute
                    && message.Command != "block" && message.Command != "consensus" && message.Command != "tx")
                {
                    Disconnect(false);
                    break;
                }
                try
                {
                    OnMessageReceived(message);
                }
                catch (EndOfStreamException)
                {
                    Disconnect(false);
                    break;
                }
                catch (FormatException)
                {
                    Disconnect(true);
                    break;
                }
            }
        }

        private void OnMessageReceived(Message message)
        {
            switch (message.Command)
            {
                case "addr":
                    OnAddrMessageReceived(message.Payload.AsSerializable<AddrPayload>());
                    break;
                case "block":
                    //OnInventoryReceived(message.Payload.AsSerializable<Block>());
                    break;
                case "consensus":
                    //OnInventoryReceived(message.Payload.AsSerializable<ConsensusPayload>());
                    break;
                case "filteradd":
                    //OnFilterAddMessageReceived(message.Payload.AsSerializable<FilterAddPayload>());
                    break;
                case "filterclear":
                    //OnFilterClearMessageReceived();
                    break;
                case "filterload":
                    //OnFilterLoadMessageReceived(message.Payload.AsSerializable<FilterLoadPayload>());
                    break;
                case "getaddr":
                    OnGetAddrMessageReceived();
                    break;
                case "getblocks":
                    //OnGetBlocksMessageReceived(message.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "getdata":
                    //OnGetDataMessageReceived(message.Payload.AsSerializable<InvPayload>());
                    break;
                case "getheaders":
                    //OnGetHeadersMessageReceived(message.Payload.AsSerializable<GetBlocksPayload>());
                    break;
                case "headers":
                    //OnHeadersMessageReceived(message.Payload.AsSerializable<HeadersPayload>());
                    break;
                case "inv":
                    //OnInvMessageReceived(message.Payload.AsSerializable<InvPayload>());
                    break;
                case "mempool":
                    //OnMemPoolMessageReceived();
                    break;
                case "tx":
                    //if (message.Payload.Length <= 1024 * 1024)
                    //    OnInventoryReceived(Transaction.DeserializeFrom(message.Payload));
                    break;
                case "verack":
                case "version":
                    Disconnect(true);
                    break;
                case "alert":
                case "merkleblock":
                case "notfound":
                case "ping":
                case "pong":
                case "reject":
                default:
                    //暂时忽略
                    break;
            }
        }

        private void OnGetAddrMessageReceived()
        {
            if (!localNode.ServiceEnabled) return;
            AddrPayload payload;
            lock (localNode.connectedPeers)
            {
                const int MaxCountToSend = 200;
                IEnumerable<RemotePeer> peers = localNode.connectedPeers.Where(p => p.ListenerEndpoint != null && p.Version != null);
                if (localNode.connectedPeers.Count > MaxCountToSend)
                {
                    Random rand = new Random();
                    peers = peers.OrderBy(p => rand.Next());
                }
                peers = peers.Take(MaxCountToSend);
                payload = AddrPayload.Create(peers.Select(p => NetworkAddressWithTime.Create(p.ListenerEndpoint, p.Version.Services, p.Version.Timestamp)).ToArray());
            }
            EnqueueMessage("addr", payload, true);
        }

        private void OnAddrMessageReceived(AddrPayload payload)
        {
            IPEndPoint[] peers = payload.AddressList.Select(p => p.EndPoint).Where(p => p.Port != localNode.Port || !LocalPeer.LocalAddresses.Contains(p.Address)).ToArray();
            if (peers.Length > 0) PeersReceived?.Invoke(this, peers);
        }
    }
}
