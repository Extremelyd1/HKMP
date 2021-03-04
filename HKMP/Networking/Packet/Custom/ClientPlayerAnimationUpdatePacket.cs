using System.Collections.Generic;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerAnimationUpdatePacket : Packet, IPacket {
        
        public int Id { get; set; }
        public string ClipName { get; set; }
        public int Frame { get; set; }
        
        // TODO: this is a bit sloppy, what if we want to send other data type
        // for animation effects?
        public List<bool> EffectInfo { get; set; }

        public ClientPlayerAnimationUpdatePacket() {
            EffectInfo = new List<bool>();
        }
        
        public ClientPlayerAnimationUpdatePacket(Packet packet) : base(packet) {
            EffectInfo = new List<bool>();
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.ClientPlayerAnimationUpdate);

            Write(Id);

            Write(ClipName);
            Write(Frame);

            foreach (var effectBool in EffectInfo) {
                Write(effectBool);
            }

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadInt();
            ClipName = ReadString();
            Frame = ReadInt();

            // Clear the existing list
            EffectInfo.Clear();
            
            while (UnreadLength() > 0) {
                EffectInfo.Add(ReadBool());
            }
        }
    }
}