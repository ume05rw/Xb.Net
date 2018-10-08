using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Xb.Net
{
    /// <summary>
    /// Implementation of TCP asynchronous communication
    /// </summary>
    public class Tcp : IDisposable
    {
        #region "Static Methods"

        /// <summary>
        /// Static send
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="localPort"></param>
        public static void SendOnce(byte[] bytes, IPEndPoint remoteEndPoint, int localPort = 0)
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, localPort);

            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(localEndPoint);
                socket.SendTo(bytes, remoteEndPoint);
                socket.Shutdown(SocketShutdown.Both);
            }
        }

        /// <summary>
        /// Static send
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteIpAddress"></param>
        /// <param name="remotePort"></param>
        /// <param name="localPort"></param>
        public static void SendOnce(byte[] bytes, IPAddress remoteIpAddress, int remotePort, int localPort = 0)
        {
            Tcp.SendOnce(bytes, new IPEndPoint(remoteIpAddress, remotePort), localPort);
        }

        /// <summary>
        /// Static async send
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public static async Task<bool> SendOnceAsync(byte[] bytes, IPEndPoint remoteEndPoint, int localPort = 0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Tcp.SendOnce(bytes, remoteEndPoint, localPort);
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Static async send
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteIpAddress"></param>
        /// <param name="remotePort"></param>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public static async Task<bool> SendOnceAsync(byte[] bytes, IPAddress remoteIpAddress, int remotePort, int localPort = 0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Tcp.SendOnce(bytes, new IPEndPoint(remoteIpAddress, remotePort), localPort);
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }).ConfigureAwait(false);
        }

        #endregion

        /// <summary>
        /// On accept remote host.
        /// </summary>
        public EventHandler<RemoteData> OnAccepted;

        /// <summary>
        /// On data recieved from remote host.
        /// </summary>
        public EventHandler<RemoteData> OnRecieved;

        /// <summary>
        /// On disconnect from remote host.
        /// </summary>
        public EventHandler<RemoteData> OnDisconnected;

        /// <summary>
        /// Socket instance
        /// </summary>
        private Socket _socket;

        /// <summary>
        /// Remote IP Address
        /// </summary>
        public IPAddress[] RemoteIpAddresses 
            => this._socketSets
                .Select(pair => pair.Key.Address)
                .ToArray();

        /// <summary>
        /// Remote Port number
        /// </summary>
        public int[] RemotePorts
            => this._socketSets
                .Select(pair => pair.Key.Port)
                .ToArray();

        /// <summary>
        /// Remote Endpoint
        /// </summary>
        public IPEndPoint[] RemoteEndPoints
            => this._socketSets
                .Select(pair => pair.Key)
                .ToArray();

        /// <summary>
        /// Local IP Address
        /// </summary>
        public IPAddress LocalIpAddress { get; private set; }

        /// <summary>
        /// Local Port number
        /// </summary>
        public int LocalPort { get; private set; }

        /// <summary>
        /// Local Endpoint
        /// </summary>
        public IPEndPoint LocalEndPoint { get; private set; }

        // NOT Implements. use SendAndRecieveAsync.
        ///// <summary>
        ///// Timeout setting
        ///// </summary>
        //public int Timeout { get; set; } = 30;

        /// <summary>
        /// Socket role (Server or Client)
        /// </summary>
        public RoleType Role { get; private set; }

        /// <summary>
        /// Connection list
        /// </summary>
        private Dictionary<IPEndPoint, SocketSet> _socketSets 
            = new Dictionary<IPEndPoint, SocketSet>();


        /// <summary>
        /// Constructor - Server mode
        /// </summary>
        /// <param name="localPort"></param>
        public Tcp(int localPort)
        {
            this.Role = RoleType.Server;
            this.LocalIpAddress = IPAddress.Any;
            this.LocalPort = localPort;

            this.Init();

            // Max connection: 1000
            this._socket.Listen(1000);

            try
            {
                this._socket.BeginAccept(this.OnAcceptPrivate, this._socket);
                Xb.Util.Out($"Start listen port: {this.LocalEndPoint.Port}.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Constructor - Client mode
        /// </summary>
        /// <param name="remoteIpAddress"></param>
        /// <param name="remotePort"></param>
        /// <param name="localPort"></param>
        public Tcp(IPAddress remoteIpAddress, int remotePort, int localPort = 0)
        {
            this.Role = RoleType.Client;
            this.LocalIpAddress = IPAddress.Any;
            this.LocalPort = localPort;

            this.Init();

            var remoteEndPoint = new IPEndPoint(remoteIpAddress, remotePort);
            this._socket.Connect(remoteEndPoint);

            var sset = this.CreateSocketSet(this._socket);

            sset.Socket.BeginReceive(
                sset.ReceiveBuffer,
                0,
                sset.ReceiveBuffer.Length,
                SocketFlags.None,
                this.OnRecievedPrivate,
                sset);

            Xb.Util.Out($"Connected to {((IPEndPoint)sset.Socket.RemoteEndPoint).Address}.");
        }

        /// <summary>
        /// Initialize the socket.
        /// </summary>
        private void Init()
        {
            this.LocalEndPoint = new IPEndPoint(this.LocalIpAddress, this.LocalPort);
            this._socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            this._socket.Bind(this.LocalEndPoint);
        }

        /// <summary>
        /// Event handling at connection acceptance.
        /// </summary>
        /// <param name="ar"></param>
        private void OnAcceptPrivate(IAsyncResult ar)
        {
            if (this.IsDisposed)
                return;

            // Get accepted socket(=this._socket)
            var localSocket = (Socket)ar.AsyncState;

            try
            {
                var connectionSocket = localSocket.EndAccept(ar);
                var sset = this.CreateSocketSet(connectionSocket);

                connectionSocket.BeginReceive(
                    sset.ReceiveBuffer,
                    0,
                    sset.ReceiveBuffer.Length,
                    SocketFlags.None,
                    this.OnRecievedPrivate,
                    sset);

                var remoteData = new RemoteData((IPEndPoint)connectionSocket.RemoteEndPoint);
                this.OnAccepted?.Invoke(this, remoteData);

                Xb.Util.Out($"Connected from {((IPEndPoint)sset.Socket.RemoteEndPoint).Address}.");
            }
            catch
            {
                Xb.Util.Out($"Accept failure.");
            }

            try
            {
                localSocket.BeginAccept(this.OnAcceptPrivate, localSocket);
                Xb.Util.Out($"Continue listen port: {this.LocalEndPoint.Port}.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Event handling at data reception.
        /// </summary>
        /// <param name="ar"></param>
        private void OnRecievedPrivate(System.IAsyncResult ar)
        {
            if (this.IsDisposed)
                return;

            // Get SocketSet-object.
            var sset = (SocketSet)ar.AsyncState;

            var length = 0;
            try
            {
                length = sset.Socket.EndReceive(ar);
            }
            catch (System.ObjectDisposedException)
            {
                // Recieve-Socket disposed
                return;
            }
            catch (System.Net.Sockets.SocketException)
            {
                // Disconnect from remote host
                Xb.Util.Out($"Disconnected from {((IPEndPoint)sset.Socket.RemoteEndPoint).Address}, type-1");
                var remoteData = new RemoteData((IPEndPoint)sset.Socket.RemoteEndPoint);
                this.OnDisconnected?.Invoke(this, remoteData);
                this.DisposeSocketSet((IPEndPoint)sset.Socket.RemoteEndPoint);

                return;
            }

            if (length > 0)
            {
                // Bytes recieved.
                Xb.Util.Out($"Recieving");

                sset.ReceivedData.Write(sset.ReceiveBuffer, (int)sset.ReceivedData.Length, length);
            }

            if (sset.Socket.Available == 0)
            {
                // Recieve completed
                Xb.Util.Out($"Recieve completed");

                var bytes = sset.ReceivedData.ToArray();
                sset.ReceivedData.Dispose();
                sset.ReceivedData = new System.IO.MemoryStream();

                if (bytes.Length > 0)
                {
                    var remoteData = new RemoteData((IPEndPoint)sset.Socket.RemoteEndPoint, bytes);
                    Xb.Util.Out($"Recieved from {remoteData.RemoteEndPoint.Address}, {bytes.Length} bytes: {BitConverter.ToString(bytes)}");
                    this.OnRecieved?.Invoke(this, remoteData);
                }
                else
                {
                    Xb.Util.Out($"Disconnected from {((IPEndPoint)sset.Socket.RemoteEndPoint).Address}, type-2");
                    var remoteData = new RemoteData((IPEndPoint)sset.Socket.RemoteEndPoint);
                    this.OnDisconnected?.Invoke(this, remoteData);
                    this.DisposeSocketSet((IPEndPoint)sset.Socket.RemoteEndPoint);

                    return;
                }
            }

            if (sset.IsDisposed)
                return;

            // Recieve again.
            try
            {
                sset.Socket.BeginReceive(
                    sset.ReceiveBuffer,
                    0,
                    sset.ReceiveBuffer.Length,
                    SocketFlags.None,
                    this.OnRecievedPrivate,
                    sset);
            }
            catch
            {
                // On disconnect by RST-Flag, BeginRecieve fails.
                Xb.Util.Out($"Disconnected from {((IPEndPoint)sset.Socket.RemoteEndPoint).Address}, type-3");
                var remoteData = new RemoteData((IPEndPoint)sset.Socket.RemoteEndPoint);
                this.OnDisconnected?.Invoke(this, remoteData);
                this.DisposeSocketSet((IPEndPoint)sset.Socket.RemoteEndPoint);
            }
        }

        /// <summary>
        /// Send synchronous, to specified target
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteEndPoint"></param>
        public void SendTo(byte[] bytes, IPEndPoint remoteEndPoint)
        {
            var sset = this.GetSocketSet(remoteEndPoint);
            var result = sset.Socket.SendTo(bytes, remoteEndPoint);
            Xb.Util.Out($"Send to {remoteEndPoint.Address}, {bytes.Length} bytes: {BitConverter.ToString(bytes)}");
        }

        /// <summary>
        /// Send synchronous, to specified target
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteIpAddress"></param>
        /// <param name="remotePort"></param>
        public void SendTo(byte[] bytes, IPAddress remoteIpAddress, int remotePort)
        {
            this.SendTo(bytes, new IPEndPoint(remoteIpAddress, remotePort));
        }

        /// <summary>
        /// Send async, to specified target
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteEndPoint"></param>
        /// <returns></returns>
        public async Task<bool> SendToAsync(byte[] bytes, IPEndPoint remoteEndPoint)
        {
            return await Task.Run(() =>
            {
                try
                {
                    this.SendTo(bytes, remoteEndPoint);
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Send async, to specified target
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteIpAddress"></param>
        /// <param name="remotePort"></param>
        /// <returns></returns>
        public async Task<bool> SendToAsync(byte[] bytes, IPAddress remoteIpAddress, int remotePort)
        {
            return await Task.Run(() =>
            {
                try
                {
                    this.SendTo(bytes, remoteIpAddress, remotePort);
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Send synchronous (to all connected remote host)
        /// </summary>
        /// <param name="bytes"></param>
        public void Send(byte[] bytes)
        {
            foreach (var pair in this._socketSets)
                this.SendTo(bytes, pair.Key);
        }

        /// <summary>
        /// Send async (to all connected remote host)
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(byte[] bytes)
        {
            return await Task.Run(() => 
            {
                try
                {
                    this.Send(bytes);
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Send and wait data from remote.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public async Task<RemoteData> SendAndRecieveAsync(byte[] bytes, IPEndPoint remoteEndPoint, int timeoutSeconds = 30)
        {
            return await Task.Run(() =>
            {
                RemoteData result = null;

                var handler = new EventHandler<RemoteData>((object sender, RemoteData rdata) =>
                {
                    result = rdata;
                });

                this.OnRecieved += handler;

                try
                {
                    this.SendTo(bytes, remoteEndPoint);
                }
                catch (Exception)
                {
                    this.OnRecieved -= handler;
                    return result;
                }

                try
                {
                    var limitTime = DateTime.Now.AddSeconds(timeoutSeconds);
                    while (true)
                    {
                        System.Threading.Thread.Sleep(100);

                        if (result != null)
                            break;

                        if (limitTime < DateTime.Now)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result = null;
                }
                finally
                {
                    this.OnRecieved -= handler;
                }

                return result;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Send and wait data from remote.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteIpAddress"></param>
        /// <param name="remotePort"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public async Task<RemoteData> SendAndRecieveAsync(byte[] bytes, IPAddress remoteIpAddress, int remotePort, int timeoutSeconds = 30)
        {
            var remoteEndPoint = new IPEndPoint(remoteIpAddress, remotePort);
            return await this.SendAndRecieveAsync(bytes, remoteEndPoint, timeoutSeconds);
        }

        /// <summary>
        /// Send and wait data from remote for Client-Mode.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public async Task<RemoteData> SendAndRecieveAsync(byte[] bytes, int timeoutSeconds = 30)
        {
            if (this.Role != RoleType.Client)
                throw new InvalidOperationException("Supported only Client mode");

            var remoteEndPoint = (IPEndPoint)this._socketSets.First().Value.Socket.RemoteEndPoint;
            return await this.SendAndRecieveAsync(bytes, remoteEndPoint, timeoutSeconds);
        }

        /// <summary>
        /// Disconnect(all connections).
        /// </summary>
        public void Disconnect()
        {
            var keys = this._socketSets.Keys.ToArray();
            foreach (var key in keys)
                this.DisconnectFrom(key);
        }

        /// <summary>
        /// Disconnect a specific connection.
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        public void DisconnectFrom(IPEndPoint remoteEndPoint)
        {
            var sset = this.GetSocketSet(remoteEndPoint);
            if (sset == null)
                return;

            try
            {
                if (sset.Socket.Connected)
                {
                    sset.Socket.Disconnect(false);
                    var remoteData = new RemoteData((IPEndPoint)sset.Socket.RemoteEndPoint);
                    this.OnDisconnected?.Invoke(this, remoteData);
                }
            }
            catch (Exception) { }

            try
            {
                this.DisposeSocketSet((IPEndPoint)sset.Socket.RemoteEndPoint);
            }
            catch (Exception) { }

            Xb.Util.Out($"Disconnected {remoteEndPoint.Address}, type-4");
        }

        /// <summary>
        /// Create a new SocketSet, and add it to the list.
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private SocketSet CreateSocketSet(Socket socket)
        {
            var sset = new SocketSet(socket);
            this._socketSets.Add((IPEndPoint)socket.RemoteEndPoint, sset);
            return sset;
        }

        /// <summary>
        /// Dispose a SocketSet of the specific connection, and delete it from the list.
        /// </summary>
        /// <param name="socketSet"></param>
        private void DisposeSocketSet(IPEndPoint remoteEndPoint)
        {
            var key = this.GetSocketSetKey(remoteEndPoint);
            var socketSet = this._socketSets[key];
            this._socketSets.Remove(key);
            socketSet.Dispose();
        }

        /// <summary>
        /// Get a SocketSet for a specific connection.
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <returns></returns>
        private SocketSet GetSocketSet(IPEndPoint remoteEndPoint)
        {
            return this._socketSets[this.GetSocketSetKey(remoteEndPoint)];
        }

        /// <summary>
        /// Get a key on the list of specific connections.
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <returns></returns>
        private IPEndPoint GetSocketSetKey(IPEndPoint remoteEndPoint)
        {
            var addrBytes = remoteEndPoint.Address.GetAddressBytes();
            var port = remoteEndPoint.Port;

            var key = this._socketSets
                .Select(s => s.Key)
                .FirstOrDefault(e => e.Address.GetAddressBytes().SequenceEqual(addrBytes)
                                        && e.Port == port);

            if (key != null)
                return key;
            else
                throw new ArgumentOutOfRangeException($"Connected address not found: {remoteEndPoint}");
        }

        #region IDisposable Support
        private bool IsDisposed { get; set; } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    this.Disconnect();
                    this._socketSets = null;

                    try
                    {
                        if (this._socket.Connected)
                            this._socket.Disconnect(false);
                    }
                    catch (Exception) { }

                    try
                    {
                        this._socket.Dispose();
                    }
                    catch (Exception) { }
                }
                this.IsDisposed = true;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
