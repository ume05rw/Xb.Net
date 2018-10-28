using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Xb.Net
{
    public class Util
    {
        /// <summary>
        /// Get the TCP port number in use.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If unused port are required, this is not necessary.
        /// Set the local endpoint port to 0.
        /// </remarks>
        public static int[] GetUsedTcpPorts()
        {
            var ipGlobalProp = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProp.GetActiveTcpListeners();
            var connections = ipGlobalProp.GetActiveTcpConnections();
            var ports = new List<int>();

            foreach (var listener in listeners)
                ports.Add(listener.Port);

            foreach (var connection in connections)
                if (!ports.Contains(connection.LocalEndPoint.Port))
                    ports.Add(connection.LocalEndPoint.Port);

            return ports.ToArray();
        }

        /// <summary>
        /// Get the UDP port number in use.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If unused port are required, this is not necessary.
        /// Set the local endpoint port to 0.
        /// </remarks>
        public static int[] GetUsedUdpPorts()
        {
            var ipGlobalProp = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProp.GetActiveUdpListeners();
            var ports = new List<int>();

            foreach (var listener in listeners)
                ports.Add(listener.Port);

            return ports.ToArray();
        }

        /// <summary>
        /// Get Unused TCP-Port Number.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If unused port are required, this is not necessary.
        /// Set the local endpoint port to 0.
        /// </remarks>
        public static int GetNewTcpPort()
        {
            Func<int> getNew = () =>
            {
                var r = new System.Random();
                return r.Next(1, 65535);
            };

            var usedPorts = Util.GetUsedTcpPorts();
            var result = -1;

            for (; ; )
            {
                result = getNew();
                if (!usedPorts.Contains(result))
                    break;
            }

            return result;
        }

        /// <summary>
        /// Get Unused UDP-Port Number.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If unused port are required, this is not necessary.
        /// Set the local endpoint port to 0.
        /// </remarks>
        public static int GetNewUdpPort()
        {
            Func<int> getNew = () =>
            {
                var r = new System.Random();
                return r.Next(1, 65535);
            };

            var usedPorts = Util.GetUsedUdpPorts();
            var result = -1;

            for (; ; )
            {
                result = getNew();
                if (!usedPorts.Contains(result))
                    break;
            }

            return result;
        }

        /// <summary>
        /// Get the local address, v4 and v6
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetLocalAddresses()
        {
            try
            {
                //get v4 and v6-address
                return NetworkInterface
                    .GetAllNetworkInterfaces()
                    .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                    .Select(ua => ua.Address)
                    .Where(addr => !IPAddress.IsLoopback(addr))
                    .ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get the local IPv4 address.
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetLocalV4Addresses()
        {
            try
            {
                //get v4-address only
                return NetworkInterface
                    .GetAllNetworkInterfaces()
                    .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                    .Select(ua => ua.Address)
                    .Where(addr => addr.AddressFamily == AddressFamily.InterNetwork
                                && !IPAddress.IsLoopback(addr))
                    .ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get the local IPv6 address.
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetLocalV6Addresses()
        {
            try
            {
                //get v6-address only
                return NetworkInterface
                    .GetAllNetworkInterfaces()
                    .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                    .Select(ua => ua.Address)
                    .Where(addr => addr.AddressFamily == AddressFamily.InterNetworkV6 
                            && !IPAddress.IsLoopback(addr))
                    .ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Acquire the IPv4 address from the host name.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IPAddress[] GetV4AddressesByName(string host)
        {
            try
            {
                //get v4-address only
                return System.Net.Dns
                    .GetHostEntryAsync(host)
                    .GetAwaiter()
                    .GetResult()
                    .AddressList
                    .Where(addr => addr.AddressFamily == AddressFamily.InterNetwork)
                    .ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Acquire the IPv6 address from the host name.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IPAddress[] GetV6AddressesByName(string host)
        {
            try
            {
                //get v6-address only
                return System.Net.Dns
                    .GetHostEntryAsync(host)
                    .GetAwaiter()
                    .GetResult()
                    .AddressList
                    .Where(addr => addr.AddressFamily == AddressFamily.InterNetworkV6)
                    .ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get the local default gateway address on IPv4.
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetDefaultGateway()
        {
            var prop = NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Select(i => i.GetIPProperties())
                    .FirstOrDefault(p => p.GatewayAddresses.Count > 0);

            return (prop != null)
                ? prop.GatewayAddresses[0].Address
                : null;
        }

        /// <summary>
        /// Get the local IPv4 address of the same segment as the default gateway.
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetLocalPrimaryAddress()
        {
            var prop = NetworkInterface
                .GetAllNetworkInterfaces()
                .Select(i => i.GetIPProperties())
                .FirstOrDefault(p => p.GatewayAddresses.Count > 0);

            if (prop == null)
                return null;

            var v4Addr = prop.UnicastAddresses
                .Select(ua => ua.Address)
                .Where(addr => addr.AddressFamily == AddressFamily.InterNetwork
                                && !IPAddress.IsLoopback(addr))
                .FirstOrDefault();

            return (v4Addr != null)
                ? v4Addr
                : null;
        }
    }
}
