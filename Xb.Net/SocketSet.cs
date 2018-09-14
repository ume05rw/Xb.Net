using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Xb.Net
{
    /// <summary>
    /// Socket role type
    /// </summary>
    public enum RoleType
    {
        /// <summary>
        /// Server
        /// </summary>
        Server = 0,

        /// <summary>
        /// Client
        /// </summary>
        Client = 1
    }

    /// <summary>
    /// RecieveData class
    /// </summary>
    public class RemoteData
    {
        /// <summary>
        /// Remote IPAddress
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// Recieved byte array
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="remoteEndPoint"></param>
        public RemoteData(IPEndPoint remoteEndPoint, byte[] bytes = null)
        {
            this.Bytes = bytes ?? new byte[] { };
            this.RemoteEndPoint = remoteEndPoint;
        }
    }

    internal class SocketSet : IDisposable
    {
        private int BufferSize = 2048;

        public System.Net.Sockets.Socket Socket;
        public byte[] ReceiveBuffer;
        public System.IO.MemoryStream ReceivedData;

        public SocketSet(System.Net.Sockets.Socket soc)
        {
            this.Socket = soc;
            this.ReceiveBuffer = new byte[BufferSize];
            this.ReceivedData = new System.IO.MemoryStream();
        }

        #region IDisposable Support
        public bool IsDisposed { get; private set; } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    if (this.Socket != null)
                    {
                        try
                        {
                            if (this.Socket.Connected)
                                this.Socket.Disconnect(false);
                        }
                        catch (Exception) { }
                        try
                        {
                            this.Socket.Dispose();
                        }
                        catch (Exception) { }
                    }

                    try
                    {
                        if (this.ReceivedData != null)
                            this.ReceivedData.Dispose();
                    }
                    catch (Exception) { }

                    this.ReceiveBuffer = null;
                }

                this.IsDisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
