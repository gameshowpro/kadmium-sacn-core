using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace kadmium_sacn_core
{
    public class SACNSender
    {
        public Guid UUID { get; set; }
        private UdpClient Socket { get; set; }
        public IPAddress UnicastAddress { get; set; }
        public bool Multicast { get { return UnicastAddress == null; } }
        public int Port { get; set; }
        public string SourceName { get; set; }

        private readonly Dictionary<UInt16, byte> sequenceIds = new Dictionary<ushort, byte>();

        public SACNSender(Guid uuid, string sourceName, int port)
        {
            SourceName = sourceName;
            UUID = uuid;
            Socket = new UdpClient();
            Port = port;
        }

        public SACNSender(Guid uuid, string sourceName) : this(uuid, sourceName, SACNCommon.SACN_PORT) { }

        /// <summary>
        /// Multicast send
        /// </summary>
        /// <param name="universeID">The universe ID to multicast to</param>
        /// <param name="data">Up to 512 bytes of DMX data</param>
        public async Task Send(UInt16 universeID, byte[] data, byte priority = 100)
        {
            this.sequenceIds.TryGetValue(universeID, out byte sequenceID);
            var packet = new SACNPacket(universeID, SourceName, UUID, sequenceID++, data, priority);
            this.sequenceIds[universeID] = sequenceID;

            byte[] packetBytes = packet.ToArray();
            await Socket.SendAsync(packetBytes, packetBytes.Length, GetEndPoint(universeID, Port));
        }

        /// <summary>
        /// Unicast send
        /// </summary>
        /// <param name="hostname">The hostname to unicast to</param>
        /// <param name="universeID">The Universe ID</param>
        /// <param name="data">Up to 512 bytes of DMX data</param>
        public async Task Send(string hostname, UInt16 universeID, byte[] data, byte priority = 100)
        {
            this.sequenceIds.TryGetValue(universeID, out byte sequenceID);
            var packet = new SACNPacket(universeID, SourceName, UUID, sequenceID++, data, priority);
            this.sequenceIds[universeID] = sequenceID;

            byte[] packetBytes = packet.ToArray();
            await Socket.SendAsync(packetBytes, packetBytes.Length, hostname, Port);
        }

        private IPEndPoint GetEndPoint(UInt16 universeID, int port)
        {
            if (Multicast)
            {
                return new IPEndPoint(SACNCommon.GetMulticastAddress(universeID), port);
            }
            else
            {
                return new IPEndPoint(UnicastAddress, port);
            }
        }

        /// <summary>
        /// The network interface with the supplied IPAddress will be used to send multicast packets. If this is not called, the default interface will be used.
        /// </summary>
        public void SetMulticastInterface(IPAddress sourceAddress)
        {
            // Set the outgoing multicast interface
            try

            {
                SocketOptionLevel level;
                byte[] addressArray;
                switch (sourceAddress.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        level = SocketOptionLevel.IP;
                        addressArray = sourceAddress.GetAddressBytes();
                        break;
                    case AddressFamily.InterNetworkV6:
                        level = SocketOptionLevel.IPv6;
                        addressArray = BitConverter.GetBytes((int)sourceAddress.ScopeId);
                        break;
                    default:
                        throw new Exception("Unsupported address family");
                }
                Socket.Client.SetSocketOption(
                    level,
                    SocketOptionName.MulticastInterface,
                    addressArray
                );
            }
            catch (SocketException err)
            {
                Console.WriteLine("SetSendInterface: Unable to set the multicast interface: {0}", err.Message);
                throw;
            }
        }

        /// <summary>
        /// The network interface with the supplied index will be used to send multicast packets. If this is not called, the default interface will be used.
        /// </summary>
        public void SetMulticastInterface(int sourceIndex)
            => Socket.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(sourceIndex));

        public void Close()
        {
            //Socket.Close();
        }
    }
}