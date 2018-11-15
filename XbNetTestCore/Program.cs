using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace XbNetTestCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //TcpServerAndClientTest()
            //    .GetAwaiter()
            //    .GetResult();

            //UdpServerAndClientTest()
            //    .GetAwaiter()
            //    .GetResult();

            //TcpServerTest();

            //UdpServerTest();

            //UdpClientClassTest();

            // SubnetTest();

            //GetLocalPrimaryAddressTest()
            //    .GetAwaiter()
            //    .GetResult();

            //GetV4AddressRangeOnLanTest()
            //    .GetAwaiter()
            //    .GetResult();

            GetV4ActiveAddressSetOnLanTest()
                .GetAwaiter()
                .GetResult();
        }

        static async Task<bool> GetV4ActiveAddressSetOnLanTest()
        {
            var addrs = await Xb.Net.Util.GetV4ActiveAddressSetOnLan();

            return true;
        }

        static async Task<bool> GetV4AddressRangeOnLanTest()
        {
            var addrs = Xb.Net.Util.GetV4AddressRangeOnLan();


            return true;
        }

        static async Task<bool> GetLocalPrimaryAddressTest()
        {
            var addr = Xb.Net.Util.GetLocalPrimaryAddress();


            return true;
        }

        static async Task<bool> TcpServerAndClientTest()
        {
            var tcp = new Xb.Net.Tcp(10241);
            tcp.OnRecieved += (object e, Xb.Net.RemoteData rdata) =>
            {
                System.Diagnostics.Debug.WriteLine($"Recieved: {rdata.RemoteEndPoint.Address}");
                System.Diagnostics.Debug.WriteLine(BitConverter.ToString(rdata.Bytes));

                tcp.SendTo(new byte[] { 0x31, 0x32, 0x33 }, rdata.RemoteEndPoint);
            };

            var tcp2 = new Xb.Net.Tcp(IPAddress.Parse("192.168.254.90"), 10241);
            var result = await tcp2.SendAndRecieveAsync(new byte[] { 0x31, 0x32, 0x33 });

            return true;
        }

        static async Task<bool> UdpServerAndClientTest()
        {
            var udp = new Xb.Net.Udp(10241);
            udp.OnRecieved += (object e, Xb.Net.RemoteData rdata) =>
            {
                System.Diagnostics.Debug.WriteLine("Server recieved:");
                System.Diagnostics.Debug.WriteLine(BitConverter.ToString(rdata.Bytes));

                udp.SendTo(new byte[] { 0x31, 0x32, 0x33 }, new IPAddress(rdata.Bytes.Take(4).ToArray()), rdata.Bytes[4]);
            };

            var udp2 = new Xb.Net.Udp(IPAddress.Parse("192.168.254.90"), 10241, 81);
            var result = await udp2.SendAndRecieveAsync(new byte[] { 192, 168, 254, 90, 81 });

            udp2.OnRecieved += (object e, Xb.Net.RemoteData rdata) => 
            {
                System.Diagnostics.Debug.WriteLine("Client recieved:");
                System.Diagnostics.Debug.WriteLine(BitConverter.ToString(rdata.Bytes));
            };

            udp.SendTo(new byte[] { 0x00, 0x01, 0x01 }, IPAddress.Broadcast, 81);

            return true;
        }

        static void TcpServerTest()
        {
            var tcp = new Xb.Net.Tcp(1024);

            tcp.OnAccepted += (object e, Xb.Net.RemoteData rdata) =>
            {
                Debug.WriteLine($"Accepted: {rdata.RemoteEndPoint.Address}");

                var value = new byte[] { 11, 22, 33, 44 };
                tcp.Send(value);
            };

            tcp.OnRecieved += (object e, Xb.Net.RemoteData rdata) =>
            {
                Debug.WriteLine($"Recieved: {rdata.RemoteEndPoint.Address}");
                Debug.WriteLine(rdata.Bytes);

                var value = new byte[] { 10, 20, 30, 40 };
                tcp.SendTo(value, rdata.RemoteEndPoint);

                tcp.Disconnect();
            };

            tcp.OnDisconnected += (object e, Xb.Net.RemoteData rdata) =>
            {
                Debug.WriteLine($"Disconnected: {rdata.RemoteEndPoint.Address}");
            };

            while (true)
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        static void UdpServerTest()
        {
            var udp = new Xb.Net.Udp(1026);

            udp.OnRecieved += (object e, Xb.Net.RemoteData rdata) =>
            {
                Debug.WriteLine($"Recieved.");
                Debug.WriteLine(rdata.Bytes);

                var value = new byte[] { 11, 22, 33, 44 };
                //udp.Send(value);

                value = new byte[] { 10, 20, 30, 40 };
                udp.SendTo(value, (new IPEndPoint(IPAddress.Parse("192.168.254.90"), 1027)));
            };

            while (true)
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        static void UdpClientClassTest()
        {
            var ep1 = new IPEndPoint(IPAddress.Parse("192.168.254.90"), 1024);
            var client1 = new UdpClient(ep1);
            client1.BeginReceive((IAsyncResult ar) => 
            {
            }, client1);

            // ↓fail here. same local endpoint not allow.
            var ep2 = new IPEndPoint(IPAddress.Parse("192.168.254.90"), 1024);
            var client2 = new UdpClient(ep2);
            client2.BeginReceive((IAsyncResult ar) =>
            {
            }, client2);

            while (true)
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        static void SubnetTest()
        {
            var a = Xb.Net.Util.GetDefaultGateway();
            var b = Xb.Net.Util.GetLocalPrimaryAddress();

            var props = NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Select(i => i.GetIPProperties());

            foreach (var prop in props)
            {
                foreach (var uaddr in prop.UnicastAddresses)
                {
                    Debug.WriteLine($"Address: {uaddr.Address}");
                    Debug.WriteLine($"Mask: {BitConverter.ToString(uaddr.IPv4Mask.GetAddressBytes())}");

                    var maskArray = uaddr.IPv4Mask
                        .GetAddressBytes()
                        .Select(bt => Convert.ToString((int)bt, 2).PadLeft(8, '0'));
                    var maskString = string.Join("", maskArray);
                    var idx = maskString.IndexOf('0');
                    Debug.WriteLine($"Mask Length: {idx}");

                }

                foreach (var gaddr in prop.GatewayAddresses)
                {
                    Debug.WriteLine($"Gateway: {gaddr.Address}");
                }
            }
        }
    }
}
