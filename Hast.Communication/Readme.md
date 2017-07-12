# Hastlayer - Communication Readme



Component dealing with the communication between the host and the hardware implementation.


## Using Ethernet

When generating proxies for you hardware-accelarated objects use `"Ethernet"` as the `CommunicationChannelName` to select this communication channel.

For Hastlayer to be able to communicate with connected devices they should be on the same network as the host PC (or on one of the networks the host PC is connected to). You can achieve this by connecting the devices to a relevant router or switch, or by directly connecting them to the host (peer to peer). If doing the latter make sure to also run a DHCP server for the given network adapters otherwise the devices won't be able to obtain an IP address and won't be reachable.

You can set up a peer to peer connection as following:

1. Set up at least one network adapter (network inteface card, NIC) that has DHCP disabled, i.e. has statically configured IP and subnet mask (you can set an IP and subnet mask for the adapter manually: you can e.g. use 192.168.10.1/255.255.255.0 as the IP/subnet mask, leaving everything else empty).
2. Connect the powered up FPGA board (it doesn't need to be programmed yet at this stage but it can be) to the NIC.
3. Set up a DHCP Server.
	- You can use [Open DHCP Server](http://dhcpserver.sourceforge.net/):
		1. Install Open DHCP Server; don't install as service (you won't need that).
		2. Locate the `DHCPRange=192.168.0.1-192.168.0.254` line in the OpenDHCPServer.ini file in the installation directory. This is a default config for the range of IPs available to be handed out. Unless you set 192.168.0.0 as the NICs IP this won't work.
		3. Change the line to reflect an IP range suitable for your NIC's subnet. To follow the exampe you could use `DHCPRange=192.168.10.2-192.168.10.254`.
		4. Run RunStandAlone.bat to start the DCHP server.
	- You can also use [DHCP Server for Windows](http://www.dhcpserver.de/) (since the latest version, 5.2.3 doesn't seem to be on the website, use the one bundled with this project):
		1. Use the wizard to set up the server for the statically configured NIC(s). You can leave everything on default in the wizard but don't forget to write out the INI file. Also make sure to configure the firewall exception.
		2. Run the server app and start the server by clicking "Continue as tray app". You don't need to install it as a service.
4. Start up the software on the FPGA board (make sure to configure the software to use Ehternet communication: change the `COMMUNICATION_CHANNEL` symbol's value to `ETHERNET` under the _HastlayerOperatingSystem_ project's properties, C/C++ Build, Settings, Microblaze g++ compiler, Symbols) and wait for it to receive an IP address.

Also tried but don't recommend [Tftpd32](http://tftpd32.jounin.net/) (needs manual configuration for the given NICs), [Tiny DHCP Server](http://softcab.com/dhcp-server/index.php) (also needs manual configuration) and [haneWIN DHCP Server](http://www.hanewin.net/dhcp-e.htm) (30 days shareware).


## Using USB UART (virtual serial port)

Serial is set as the default communication channel so Hastlayer will use it if you don't change anything. But generating proxies for you hardware-accelarated objects you can also set `"Serial"` as the `CommunicationChannelName` to select this communication channel.

Connect the device(s) to the host PC with an USB cable to use USB UART as the communication channel.