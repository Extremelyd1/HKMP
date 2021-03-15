using System.Collections.Generic;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerAnimationUpdatePacket : Packet, IPacket {
        
        public string AnimationClipName { get; set; }
        public int Frame { get; set; }
        
        // TODO: this is a bit sloppy, what if we want to send other data type
        // for animation effects?
        public bool[] EffectInfo { get; set; } = { };

        public ServerPlayerAnimationUpdatePacket() {
        }
        
        public ServerPlayerAnimationUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerAnimationUpdate);

            Write(AnimationClipName);
            Write(Frame);

            foreach (var effectBool in EffectInfo) {
                Write(effectBool);
            }

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            AnimationClipName = ReadString();
            Frame = ReadInt();

            var effectInfo = new List<bool>();
            while (UnreadLength() > 0) {
                effectInfo.Add(ReadBool());
            }
            
            EffectInfo = effectInfo.ToArray();
        }
    }
}