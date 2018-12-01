using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XbNetTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void UdpSendRecieveLoopTest()
        {
            var ip1 = Xb.Net.Util.GetLocalPrimaryAddress();
            var ep1 = new IPEndPoint(ip1, 10241);
            var testLength = 1000;

            var udp1 = new Xb.Net.Udp(10241);
            udp1.OnRecieved += (object e, Xb.Net.RemoteData rdata) =>
            {
                Debug.WriteLine("UDP1 recieved:");
                Debug.WriteLine(BitConverter.ToString(rdata.Bytes));

                Debug.WriteLine("UDP1 Send to: " + rdata.RemoteEndPoint.Address);
                try
                {
                    udp1.SendTo(new byte[] { 0x31, 0x32, 0x33 }, rdata.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    Xb.Util.Out(ex);
                }
            };

            var suceeded = 0;
            var udp2 = new Xb.Net.Udp(10242);
            udp2.OnRecieved += (object e, Xb.Net.RemoteData rdata) =>
            {
                suceeded++;
                Debug.WriteLine("UDP2 recieved: " + suceeded);
                Debug.WriteLine(BitConverter.ToString(rdata.Bytes));
            };

            for (var i = 0; i < testLength; i++)
            {
                Task.Run(async () => 
                {
                    try
                    {
                        Debug.WriteLine("UDP2 Send");
                        var rdata = await udp2.SendAndRecieveAsync(Encoding.UTF8.GetBytes("hello"), ep1);

                        Debug.WriteLine("Recieve from: " + rdata.RemoteEndPoint.Address);
                        Debug.WriteLine(BitConverter.ToString(rdata.Bytes));
                    }
                    catch (Exception ex)
                    {
                        Xb.Util.Out(ex);
                    }
                }).ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }

            Assert.AreEqual(suceeded, testLength);
        }

        [TestMethod]
        public void UdpDisposeTest()
        {
            var ip1 = Xb.Net.Util.GetLocalPrimaryAddress();
            var ep1 = new IPEndPoint(ip1, 10241);
            var testLength = 1000;
            var suceeded = 0;

            for (var i = 0; i < testLength; i++)
            {
                Task.Run(async () => 
                {
                    var udp1 = new Xb.Net.Udp(10241);
                    EventHandler<Xb.Net.RemoteData> eh1 = (object e, Xb.Net.RemoteData rdata1) =>
                    {
                        Debug.WriteLine("UDP1 recieved:");
                        Debug.WriteLine(BitConverter.ToString(rdata1.Bytes));

                        Debug.WriteLine("UDP1 Send to: " + rdata1.RemoteEndPoint.Address);
                        try
                        {
                            udp1.SendTo(new byte[] { 0x31, 0x32, 0x33 }, rdata1.RemoteEndPoint);
                        }
                        catch (Exception ex)
                        {
                            Xb.Util.Out(ex);
                        }
                    };
                    udp1.OnRecieved += eh1;

                    var udp2 = new Xb.Net.Udp(10242);
                    EventHandler<Xb.Net.RemoteData> eh2 = (object e, Xb.Net.RemoteData rdata2) =>
                    {
                        suceeded++;
                        Debug.WriteLine("UDP2 recieved: " + suceeded);
                        Debug.WriteLine(BitConverter.ToString(rdata2.Bytes));
                    };
                    udp2.OnRecieved += eh2;

                    var rdata = await udp2.SendAndRecieveAsync(Encoding.UTF8.GetBytes("hello"), ep1);

                    try
                    {
                        udp1.OnRecieved -= eh1;
                        udp1.Dispose();
                        Debug.WriteLine("UDP1 Disposed");
                    }
                    catch (Exception ex)
                    {
                        Xb.Util.Out(ex);
                        throw ex;
                    }

                    try
                    {
                        udp2.OnRecieved -= eh2;
                        udp2.Dispose();
                        Debug.WriteLine("UDP2 Disposed");
                    }
                    catch (Exception ex)
                    {
                        Xb.Util.Out(ex);
                        throw ex;
                    }

                }).ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }

            Assert.AreEqual(suceeded, testLength);
        }


        [TestMethod]
        public void UdpSendOnceTest()
        {
            var testLength = 1000;
            var ep = new IPEndPoint(IPAddress.Broadcast, 10243);
            var count = 0;

            for (var i = 0; i < testLength; i++)
            {
                Task.Run(async () => 
                {
                    try
                    {
                        Xb.Net.Udp.SendOnce(Encoding.UTF8.GetBytes("hello"), ep);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Xb.Util.Out(ex);
                    }
                }).ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }

            Assert.AreEqual(count, testLength);
        }
    }
}
