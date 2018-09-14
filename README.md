Xb.Net
====

Xamarin & .NET Core Ready, Tcp and Udp socket async-implementation and Utilities.

## Description

This is an asynchronous server/client implementation of TCP/UDP socket, and TCP-IP utility methods.  
It is an implementation of a TCP server that accepts multiple connections.
Also, UDP transmission / reception that does not discard the socket by maintaining a specific port is possible.

Supports .NET Standard2.0

## Requirement
[Xb.Core](https://www.nuget.org/packages/Xb.Core/)  

## Usage
1. [Add NuGet Package](https://www.nuget.org/packages/Xb.Net/) to your project.  
2. Create Xb.Net.Tcp / Xb.Net.Udp Instance, or Call Static Methods.
  

ex) Tcp Server:  


    var tcp = new Xb.Net.Tcp(10241);
    tcp.OnRecieved += (object e, Xb.Net.RemoteData rdata) =>
    {
        System.Diagnostics.Debug.WriteLine($"Recieved: {rdata.RemoteEndPoint.Address}");
        System.Diagnostics.Debug.WriteLine(BitConverter.ToString(rdata.Bytes));
        
        tcp.SendTo(new byte[] { 0x31, 0x32, 0x33 }, rdata.RemoteEndPoint);
    };


ex) Tcp Client:  


    var tcp = new Xb.Net.Tcp(IPAddress.Parse("192.168.0.1"), 10241);
    var result = await tcp.SendAndRecieveAsync(new byte[] { 0x31, 0x32, 0x33 });


ex) Udp, Accept Any:


    var udp = new Xb.Net.Udp(10241);
    udp.OnRecieved += (object e, byte[] bytes) =>
    {
        System.Diagnostics.Debug.WriteLine(BitConverter.ToString(bytes));
        
        // When bytes [0] - bytes [3] is an IP address v4. 
        udp.SendTo(new byte[] { 0x31, 0x32, 0x33 }, new IPAddress(bytes.Take(4).ToArray()), 80);
    };
     
    // Local Broadcast
    udp.SendTo(new byte[] { 0x01, 0x02, 0x03 }, IPAddress.Broadcast, 80);


ex) Udp, Specified Remote Host:


    var udp = new Xb.Net.Udp(IPAddress.Parse("192.168.0.1"), 10241);
    var result = await udp.SendAndRecieveAsync(new byte[] { 0x31, 0x32, 0x33 });



  
Namespace and Methods are...

    ・Xb.Net
          |
          +- Tcp(static)
          |   |
          |   +- .SendOnce(byte[] bytes, 
          |   |            IPEndPoint remoteEndPoint, 
          |   |            int localPort = 0)
          |   |   Static send
          |   |
          |   +- .SendOnce(byte[] bytes, 
          |   |            IPAddress remoteIpAddress,
          |   |            int remotePort,  
          |   |            int localPort = 0)
          |   |   Static send
          |   |
          |   +- .SendOnceAsync(byte[] bytes, 
          |   |            IPEndPoint remoteEndPoint, 
          |   |            int localPort = 0)
          |   |   Static async send
          |   |
          |   +- .SendOnceAsync(byte[] bytes, 
          |                IPAddress remoteIpAddress,
          |                int remotePort,  
          |                int localPort = 0)
          |       Static async send
          |
          |
          +- Tcp(instance)
          |   |
          |   +- EventHandler<RemoteData> OnAccepted
          |   |  On accept remote host.
          |   |
          |   +- EventHandler<RemoteData> OnRecieved
          |   |  On data recieved from remote host.
          |   |
          |   +- EventHandler<RemoteData> OnDisconnected
          |   |  On disconnect from remote host.
          |   |
          |   |
          |   +- [Constructor](int localPort)
          |   |  Constructor - Server mode
          |   |
          |   |
          |   +- [Constructor](IPAddress remoteIpAddress, 
          |   |                int remotePort, 
          |   |                int localPort = 0)
          |   |  Constructor - Client mode
          |   |
          |   |
          |   +- .SendTo(byte[] bytes, 
          |   |          IPEndPoint remoteEndPoint)
          |   |   Send synchronous, to specified target
          |   |
          |   +- .SendTo(byte[] bytes, 
          |   |          IPAddress remoteIpAddress, 
          |   |          int remotePort)
          |   |   Send synchronous, to specified target
          |   |
          |   +- .SendToAsync(byte[] bytes, 
          |   |               IPEndPoint remoteEndPoint)
          |   |   Send async, to specified target
          |   |
          |   +- .SendToAsync(byte[] bytes, 
          |   |               IPAddress remoteIpAddress, 
          |   |               int remotePort)
          |   |   Send async, to specified target
          |   |
          |   +- .Send(byte[] bytes)
          |   |   Send synchronous (to all connected remote host)
          |   |
          |   +- .SendAsync(byte[] bytes)
          |   |   Send async (to all connected remote host)
          |   |
          |   +- .SendAndRecieveAsync(byte[] bytes, 
          |   |                       IPEndPoint remoteEndPoint, 
          |   |                       int timeoutSeconds = 30)
          |   |   Send and wait data from remote.
          |   |
          |   +- .SendAndRecieveAsync(byte[] bytes, 
          |   |                       IPAddress remoteIpAddress, 
          |   |                       int remotePort,
          |   |                       int timeoutSeconds = 30)
          |   |   Send and wait data from remote.
          |   |
          |   +- .SendAndRecieveAsync(byte[] bytes, 
          |   |                       int timeoutSeconds = 30)
          |   |   Send and wait data from remote for Client-Mode.
          |   |
          |   +- .Disconnect()
          |   |   Disconnect(all connections).
          |   |
          |   +- .DisconnectFrom(IPEndPoint remoteEndPoint)
          |   |   Disconnect a specific connection.
          |   |
          |   +- .Dispose()
          |       Dispose instance.
          |
          |
          +- Udp(static)
          |   |
          |   +- .SendOnce(byte[] bytes, 
          |   |            IPEndPoint remoteEndPoint, 
          |   |            int localPort = 0)
          |   |   Static send
          |   |
          |   +- .SendOnce(byte[] bytes, 
          |   |            IPAddress remoteIpAddress,
          |   |            int remotePort,  
          |   |            int localPort = 0)
          |   |   Static send
          |   |
          |   +- .SendOnceAsync(byte[] bytes, 
          |   |                 IPEndPoint remoteEndPoint, 
          |   |                 int localPort = 0)
          |   |   Static async send
          |   |
          |   +- .SendOnceAsync(byte[] bytes, 
          |                     IPAddress remoteIpAddress,
          |                     int remotePort,  
          |                     int localPort = 0)
          |       Static async send
          |
          |
          +- Udp(instance)
          |   |
          |   +- EventHandler<byte[]> OnRecieved
          |   |  On data recieved from remote host.
          |   |  * UDP is connectionless protocol, It can not obtain remote host information.
          |   |  * If host information is required, include in the data.
          |   |
          |   |
          |   +- [Constructor]()
          |   |  Constructor - Random Local Port
          |   |  Listen ALL local ip, local unused random port.
          |   |  Allow from ANY remote ip/port.
          |   |
          |   +- [Constructor](int localPort)
          |   |  Constructor - Local Port Only
          |   |  Listen ALL local ip.
          |   |  Allow from ANY remote ip/port.
          |   |
          |   +- [Constructor](IPAddress remoteIpAddress,
          |   |                int remotePort,
          |   |                int localPort = 0)
          |   |  Constructor - Remote IP/Port and Local Port
          |   |  Listen ALL local ip.
          |   |  Allow from ORDERED ip/port.
          |   |
          |   |
          |   +- .SendTo(byte[] bytes, 
          |   |          IPEndPoint remoteEndPoint)
          |   |   Send synchronous, to specified target
          |   |
          |   +- .SendTo(byte[] bytes, 
          |   |          IPAddress remoteIpAddress, 
          |   |          int remotePort)
          |   |   Send synchronous, to specified target
          |   |
          |   +- .SendToAsync(byte[] bytes, 
          |   |               IPEndPoint remoteEndPoint)
          |   |   Send async, to specified target
          |   |
          |   +- .SendToAsync(byte[] bytes, 
          |   |               IPAddress remoteIpAddress, 
          |   |               int remotePort)
          |   |   Send async, to specified target
          |   |
          |   +- .Send(byte[] bytes)
          |   |   Send synchronous, to paired remote host
          |   |
          |   +- .SendAsync(byte[] bytes)
          |   |   Send async, to paired remote host
          |   |
          |   +- .SendAndRecieveAsync(byte[] bytes, 
          |   |                       IPEndPoint remoteEndPoint, 
          |   |                       int timeoutSeconds = 30)
          |   |   Send and wait data from remote.
          |   |
          |   +- .SendAndRecieveAsync(byte[] bytes, 
          |   |                       IPAddress remoteIpAddress, 
          |   |                       int remotePort,
          |   |                       int timeoutSeconds = 30)
          |   |   Send and wait data from remote.
          |   |
          |   +- .SendAndRecieveAsync(byte[] bytes, 
          |   |                       int timeoutSeconds = 30)
          |   |   Send and wait data from paired remote host.
          |   |
          |   +- .Dispose()
          |       Dispose instance.
          |
          |
          +- Util(static)
              |
              +- .GetUsedTcpPorts()
              |   Get the TCP port number in use.
              |   * If unused port are required, this is not necessary.
              |   * Set the local endpoint port to 0.
              |
              +- .GetUsedUdpPorts()
              |   Get the UDP port number in use.
              |   * If unused port are required, this is not necessary.
              |   * Set the local endpoint port to 0.
              |
              +- .GetNewTcpPort()
              |   Get Unused TCP-Port Number.
              |   * If unused port are required, this is not necessary.
              |   * Set the local endpoint port to 0.              |
              |
              +- .GetNewUdpPort()
              |   Get Unused UDP-Port Number.
              |   * If unused port are required, this is not necessary.
              |   * Set the local endpoint port to 0.              |
              |
              +- .GetLocalV4Addresses()
              |   Get the local IPv4 address.
              |
              +- .GetLocalV6Addresses()
              |   Get the local IPv6 address.
              |
              +- .GetV4AddressesByName(string host)
              |   Acquire the IPv4 address from the host name.
              |
              +- .GetV6AddressesByName(string host)
              |   Acquire the IPv6 address from the host name.
              |
              +- .GetDefaultGateway()
              |   Get the local default gateway address on IPv4.
              |
              +- .GetLocalPrimaryAddress()
                  Get the local IPv4 address of the same segment as the default gateway.



## Licence

[MIT Licence](https://github.com/ume05rw/Xb.Net/blob/master/LICENSE)

## Author

[Do-Be's](http://dobes.jp)
