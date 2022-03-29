using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Hkmp.Networking.Server {
    /// <summary>
    /// A client managed by the server. This is only used for communication from server to client.
    /// </summary>
    internal class NetServerClient {
        /// <summary>
        /// Static set of IDs that are used.
        /// </summary>
        private static readonly HashSet<ushort> UsedIds = new HashSet<ushort>();
        /// <summary>
        /// The last ID that was assigned.
        /// </summary>
        private static ushort _lastId = 0;

        /// <summary>
        /// The ID of this client.
        /// </summary>
        public ushort Id { get; }

        /// <summary>
        /// Whether the client is registered.
        /// </summary>
        public bool IsRegistered { get; set; }

        /// <summary>
        /// The update manager for the client.
        /// </summary>
        public ServerUpdateManager UpdateManager { get; }

        /// <summary>
        /// The endpoint of the client.
        /// </summary>
        public readonly IPEndPoint EndPoint;

        /// <summary>
        /// Construct the client with the given UDP client and endpoint.
        /// </summary>
        /// <param name="udpClient">The underlying UDP client.</param>
        /// <param name="endPoint">The endpoint.</param>
        public NetServerClient(UdpClient udpClient, IPEndPoint endPoint) {
            // Also store endpoint with TCP address and TCP port
            EndPoint = endPoint;

            Id = GetId();
            UpdateManager = new ServerUpdateManager(udpClient, EndPoint);
        }

        /// <summary>
        /// Whether this client resides at the given endpoint.
        /// </summary>
        /// <param name="endPoint">The endpoint to test for.</param>
        /// <returns>true if the address and port of the endpoint match the endpoint of the client; otherwise
        /// false.</returns>
        public bool HasAddress(IPEndPoint endPoint) {
            return EndPoint.Address.Equals(endPoint.Address) && EndPoint.Port == endPoint.Port;
        }

        /// <summary>
        /// Disconnect the client from the server.
        /// </summary>
        public void Disconnect() {
            UsedIds.Remove(Id);
            
            UpdateManager.StopUdpUpdates();
        }

        /// <summary>
        /// Get a new ID that is not in use by another client.
        /// </summary>
        /// <returns>An unused ID.</returns>
        private static ushort GetId() {
            ushort newId;
            do {
                newId = _lastId++;
            } while (UsedIds.Contains(newId));

            UsedIds.Add(newId);
            return newId;
        }
    }
}