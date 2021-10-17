namespace Hkmp.Networking.Packet {
    public interface IPacketData {

        /**
         * Whether the data contained in this class is considered reliable and requires resending if lost 
         */
        bool IsReliable { get; }
        
        /*
         * Whether lost reliable data in this class should be dropped if a newer version has
         * already been received by the endpoint
         */
        bool DropReliableDataIfNewerExists { get; }
        
        /**
         * Write the data in from the class into the given Packet instance
         */
        void WriteData(IPacket packet);

        /**
         * Read the data from the given Packet into the class
         */
        void ReadData(IPacket packet);
    }
}