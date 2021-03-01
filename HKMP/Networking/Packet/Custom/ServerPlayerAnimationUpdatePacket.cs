using System.Collections.Generic;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerAnimationUpdatePacket : Packet, IPacket {
        
        public string AnimationClipName { get; set; }
        
        // TODO: this is a bit sloppy, what if we want to send other data type
        // for animation effects?
        public List<bool> EffectInfo { get; }

        public ServerPlayerAnimationUpdatePacket() {
            EffectInfo = new List<bool>();
        }
        
        public ServerPlayerAnimationUpdatePacket(Packet packet) : base(packet) {
            EffectInfo = new List<bool>();
        }
        
        public void CreatePacket() {
            Reset();

            Write(PacketId.ServerPlayerAnimationUpdate);

            Write(AnimationClipName);

            foreach (var effectBool in EffectInfo) {
                Write(effectBool);
            }

            WriteLength();
        }

        public void ReadPacket() {
            AnimationClipName = ReadString();

            // Clear the effect info first
            EffectInfo.Clear();
            while (UnreadLength() > 0) {
                EffectInfo.Add(ReadBool());
            }
        }
    }
}