using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Xb.Net
{
    /// <summary>
    /// Implementation of UDP asynchronous communication
    /// </summary>
    public class Udp : IDisposable
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
            var isBroadcast = (remoteEndPoint.Address == IPAddress.Broadcast);

            using (var socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(localEndPoint);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, isBroadcast);
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
            Udp.SendOnce(bytes, new IPEndPoint(remoteIpAddress, remotePort), localPort);
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
                    Udp.SendOnce(bytes, remoteEndPoint, localPort);
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
                    Udp.SendOnce(bytes, new IPEndPoint(remoteIpAddress, remotePort), localPort);
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
        /// On data recieved from remote host.
        /// </summary>
        /// <remarks>
        /// UDP is connectionless protocol, It can not obtain remote host information.
        /// If host information is required, include in the data.
        /// </remarks>
        /// 
        public EventHandler<byte[]> OnRecieved;

        /// <summary>
        /// Socket instance
        /// </summary>
        private Socket _socket;

        /// <summary>
        /// Remote IP Address
        /// </summary>
        public IPAddress RemoteIpAddress { get; private set; }

        /// <summary>
        /// Remote Port number
        /// </summary>
        public int RemotePort { get; private set; }

        /// <summary>
        /// Remote Endpoint
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

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
        /// Constructor - Random Local Port
        /// Listen ALL local ip, local unused random port.
        /// Allow from ANY remote ip/port.
        /// </summary>
        public Udp()
        {
            this.RemoteIpAddress = IPAddress.Any;
            this.RemotePort = 0;
            this.LocalIpAddress = IPAddress.Any;
            this.LocalPort = Util.GetNewUdpPort();

            this.Init();
        }

        /// <summary>
        /// Constructor - Local Port Only
        /// Listen ALL local ip.
        /// Allow from ANY remote ip/port.
        /// </summary>
        /// <param name="localPort"></param>
        public Udp(int localPort)
        {
            this.RemoteIpAddress = IPAddress.Any;
            this.RemotePort = 0;
            this.LocalIpAddress = IPAddress.Any;
            this.LocalPort = localPort;

            this.Init();
        }

        /// <summary>
        /// Constructor - Remote IP/Port and Local Port
        /// Listen ALL local ip.
        /// Allow from ORDERED ip/port.
        /// </summary>
        /// <param name="remoteIpAddress"></param>
        /// <param name="remotePort"></param>
        /// <param name="localPort"></param>
        public Udp(IPAddress remoteIpAddress, int remotePort, int localPort = 0)
        {
            this.RemoteIpAddress = remoteIpAddress;
            this.RemotePort = remotePort;
            this.LocalIpAddress = IPAddress.Any;
            this.LocalPort = localPort;

            this.Init();
        }

        /// <summary>
        /// Initialize the socket, start Server.
        /// </summary>
        private void Init()
        {
            this.LocalEndPoint = new IPEndPoint(this.LocalIpAddress, this.LocalPort);
            this.RemoteEndPoint = new IPEndPoint(this.RemoteIpAddress, this.RemotePort);

            this._socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            this._socket.Bind(this.LocalEndPoint);

            var socSet = new SocketSet(this._socket);

            this._socket.BeginReceive(
                socSet.ReceiveBuffer,
                0,
                socSet.ReceiveBuffer.Length,
                SocketFlags.None,
                this.OnRecievedPrivate,
                socSet);

            Xb.Util.Out($"Listen Started.");
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

            if (length > 0)
            {
                // Recieve completed
                Xb.Util.Out($"Recieve completed");

                var bytes = new byte[length];
                Array.Copy(sset.ReceiveBuffer, 0, bytes, 0, length);

                // * UDP is connection-less, CANNOT GET remote end-point info *
                Xb.Util.Out($"Recieved {bytes.Length.ToString()} bytes: {BitConverter.ToString(bytes)}");
                this.OnRecieved?.Invoke(this, bytes);
            }

            try
            {
                this._socket.BeginReceive(
                    sset.ReceiveBuffer,
                    0,
                    sset.ReceiveBuffer.Length,
                    SocketFlags.None,
                    this.OnRecievedPrivate,
                    sset);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Send synchronous, to specified target
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteEndPoint"></param>
        public void SendTo(byte[] bytes, IPEndPoint remoteEndPoint)
        {
            var isBroadcast = (remoteEndPoint.Address == IPAddress.Broadcast);

            this._socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, isBroadcast);
            this._socket.SendTo(bytes, remoteEndPoint);

            this._socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
            Xb.Util.Out($"Send to {remoteEndPoint.Address}, {bytes.Length.ToString()} bytes: {BitConverter.ToString(bytes)}");
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
        /// Send synchronous, to paired remote host
        /// </summary>
        /// <param name="bytes"></param>
        public void Send(byte[] bytes)
        {
            if (this.RemotePort == 0
                || this.RemoteIpAddress == IPAddress.Any
                || this.RemoteEndPoint == null)
                throw new NotSupportedException("Remote EndPoint Not Specified");

            this.SendTo(bytes, this.RemoteEndPoint);
        }

        /// <summary>
        /// Send async, to paired remote host
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
        public async Task<byte[]> SendAndRecieveAsync(byte[] bytes, IPEndPoint remoteEndPoint, int timeoutSeconds = 30)
        {
            return await Task.Run(() =>
            {
                byte[] result = null;

                var handler = new EventHandler<byte[]>((object sender, byte[] remoteBytes) =>
                {
                    result = remoteBytes;
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
                catch (Exception)
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
        public async Task<byte[]> SendAndRecieveAsync(byte[] bytes, IPAddress remoteIpAddress, int remotePort, int timeoutSeconds = 30)
        {
            var remoteEndPoint = new IPEndPoint(remoteIpAddress, remotePort);
            return await this.SendAndRecieveAsync(bytes, remoteEndPoint, timeoutSeconds);
        }

        /// <summary>
        /// Send and wait data from paired remote host.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public async Task<byte[]> SendAndRecieveAsync(byte[] bytes, int timeoutSeconds = 30)
        {
            if (this.RemotePort == 0
                || this.RemoteIpAddress == IPAddress.Any
                || this.RemoteEndPoint == null)
                throw new NotSupportedException("Remote EndPoint Not Specified");

            return await this.SendAndRecieveAsync(bytes, this.RemoteEndPoint, timeoutSeconds);
        }

        #region IDisposable Support
        private bool IsDisposed { get; set; } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        this.RemoteIpAddress = null;
                        this.RemoteEndPoint = null;
                        this.LocalEndPoint = null;

                        if (this._socket != null)
                        {
                            if (this._socket.Connected)
                                this._socket.Dispose();

                            this._socket.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception: {ex.Message}");
                    }
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
