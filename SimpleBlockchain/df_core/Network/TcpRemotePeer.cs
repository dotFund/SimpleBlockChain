using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleBlockchain.IO;

namespace SimpleBlockchain.Network
{
    internal class TcpRemotePeer : RemotePeer
    {
        private Socket socket;
        private NetworkStream stream;
        private bool connected = false;
        private int disposed = 0;

        public TcpRemotePeer(LocalPeer localPeer, IPEndPoint remoteEndpoint)
            : base(localPeer)
        {
            this.socket = new Socket(remoteEndpoint.Address.IsIPv4MappedToIPv6 ? AddressFamily.InterNetwork : remoteEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.ListenerEndpoint = remoteEndpoint;
        }

        public TcpRemotePeer(LocalPeer localPeer, Socket socket)
            : base(localPeer)
        {
            this.socket = socket;
            OnConnected();
        }

        private void OnConnected()
        {
            IPEndPoint remoteEndpoint = (IPEndPoint)socket.RemoteEndPoint;
            RemoteEndpoint = new IPEndPoint(remoteEndpoint.Address.MapToIPv6(), remoteEndpoint.Port);
            stream = new NetworkStream(socket);
            connected = true;
        }

        public override void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (stream != null) stream.Dispose();
                socket.Dispose();
                base.Disconnect(error);
            }
        }

        public async Task<bool> ConnectAsync()
        {
            IPAddress address = ListenerEndpoint.Address;
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            try
            {
                await socket.ConnectAsync(address, ListenerEndpoint.Port);
                OnConnected();
            }
            catch (SocketException)
            {
                Disconnect(false);
                return false;
            }
            return true;
        }

        protected override async Task<bool> SendMessageAsync(Message message)
        {
            if (!connected) throw new InvalidOperationException();
            if (disposed > 0) return false;
            byte[] buffer = message.ToArray();
            CancellationTokenSource source = new CancellationTokenSource(10000);
            //Stream.WriteAsync doesn't support CancellationToken
            //see: https://stackoverflow.com/questions/20131434/cancel-networkstream-readasync-using-tcplistener
            source.Token.Register(() => Disconnect(true));
            try
            {
                await stream.WriteAsync(buffer, 0, buffer.Length, source.Token);
                return true;
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) when (ex is IOException || ex is OperationCanceledException)
            {
                Disconnect(false);
            }
            finally
            {
                source.Dispose();
            }
            return false;
        }

        protected override async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            CancellationTokenSource source = new CancellationTokenSource(timeout);
            //Stream.ReadAsync doesn't support CancellationToken
            //see: https://stackoverflow.com/questions/20131434/cancel-networkstream-readasync-using-tcplistener
            source.Token.Register(() => Disconnect(true));
            try
            {
                return await Message.DeserializeFromAsync(stream, source.Token);
            }
            catch (ArgumentException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex) when (ex is FormatException || ex is IOException || ex is OperationCanceledException)
            {
                Disconnect(true);
            }
            finally
            {
                source.Dispose();
            }
            return null;
        }
    }
}
