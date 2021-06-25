namespace Hkmp.Networking.Packet {
    public interface IPacketData {
        /**
         * Write the data in from the class into the given Packet instance
         */
        void WriteData(Packet packet);

        /**
         * Read the data from the given Packet into the class
         */
        void ReadData(Packet packet);
    }
}