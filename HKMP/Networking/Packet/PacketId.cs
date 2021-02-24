namespace HKMP.Networking.Packet {
    public enum PacketId {
        // Server-bound
        // Initial hello
        HelloServer = 1,
        // Server-bound
        // Indicating that client is disconnecting
        Disconnect,
        // Client-bound
        // Indicating that server is shutting down
        Shutdown,
    }
}