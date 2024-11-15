using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Mirror.SimpleWeb
{
    internal sealed class Connection : IDisposable
    {
        public const int IdNotSet = -1;
        private readonly object disposedLock = new();

        public TcpClient client;

        public int connId = IdNotSet;
        private volatile bool hasDisposed;

        public Action<Connection> onDispose;
        public Thread receiveThread;

        /// <summary>
        ///     RemoteEndpoint address or address from request header
        ///     <para>Only valid on server</para>
        /// </summary>
        public string remoteAddress;

        /// <summary>
        ///     Connect request, sent from client to start handshake
        ///     <para>Only valid on server</para>
        /// </summary>
        public Request request;

        public ManualResetEventSlim sendPending = new(false);
        public ConcurrentQueue<ArrayBuffer> sendQueue = new();
        public Thread sendThread;


        public Stream stream;

        public Connection(TcpClient client, Action<Connection> onDispose)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.onDispose = onDispose;
        }

        /// <summary>
        ///     disposes client and stops threads
        /// </summary>
        public void Dispose()
        {
            Log.Verbose($"[SimpleWebTransport] Dispose {ToString()}");

            // check hasDisposed first to stop ThreadInterruptedException on lock
            if (hasDisposed) return;

            Log.Info($"[SimpleWebTransport] Connection Close: {ToString()}");

            lock (disposedLock)
            {
                // check hasDisposed again inside lock to make sure no other object has called this
                if (hasDisposed) return;

                hasDisposed = true;

                // stop threads first so they don't try to use disposed objects
                receiveThread.Interrupt();
                sendThread?.Interrupt();

                try
                {
                    // stream 
                    stream?.Dispose();
                    stream = null;
                    client.Dispose();
                    client = null;
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                sendPending.Dispose();

                // release all buffers in send queue
                while (sendQueue.TryDequeue(out var buffer))
                    buffer.Release();

                onDispose.Invoke(this);
            }
        }

        public override string ToString()
        {
            if (hasDisposed)
                return $"[Conn:{connId}, Disposed]";
            try
            {
                var endpoint = client?.Client?.RemoteEndPoint;
                return $"[Conn:{connId}, endPoint:{endpoint}]";
            }
            catch (SocketException)
            {
                return $"[Conn:{connId}, endPoint:n/a]";
            }
        }

        /// <summary>
        ///     Gets the address based on the <see cref="request" /> and RemoteEndPoint
        ///     <para>Called after ServerHandShake is accepted</para>
        /// </summary>
        internal string CalculateAddress()
        {
            if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardFor))
            {
                var actualClientIP = forwardFor.Split(',').First();
                // Remove the port number from the address
                return actualClientIP.Split(':').First();
            }

            var ipEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            var ipAddress = ipEndPoint.Address;
            if (ipAddress.IsIPv4MappedToIPv6)
                ipAddress = ipAddress.MapToIPv4();

            return ipAddress.ToString();
        }
    }
}