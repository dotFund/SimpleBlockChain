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
            StartSendLoop();
            
            while (disposed == 0)
            {
                if (Blockchain.Default != null)
                {
                    if (missions.Count == 0 && Blockchain.Default.Height < Version.StartHeight)
                    {
                        EnqueueMessage("getblocks", GetBlocksPayload.Create(Blockchain.Default.CurrentBlockHash), true);
                    }
                }
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
            }*/
        }
    }
}
