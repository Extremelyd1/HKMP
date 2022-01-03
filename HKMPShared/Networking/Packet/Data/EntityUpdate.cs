using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data {
    public class EntityUpdate : IPacketData {
        public bool IsReliable => 
            UpdateTypes.Contains(EntityUpdateType.Scale) || 
            UpdateTypes.Contains(EntityUpdateType.State);

        public bool DropReliableDataIfNewerExists => true;
        public byte EntityType { get; set; }

        public byte Id { get; set; }

        public HashSet<EntityUpdateType> UpdateTypes { get; }

        public Vector2 Position { get; set; }

        public bool Scale { get; set; }

        public List<EntityAnimationInfo> AnimationInfos { get; }

        public byte State { get; set; }

        public EntityUpdate() {
            UpdateTypes = new HashSet<EntityUpdateType>();
            AnimationInfos = new List<EntityAnimationInfo>();
        }

        public void WriteData(Packet packet) {
            packet.Write(EntityType);
            packet.Write(Id);

            // Construct the byte flag representing update types
            byte updateTypeFlag = 0;
            // Keep track of value of current bit
            byte currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(EntityUpdateType)).Length; i++) {
                // Cast the current index of the loop to a PlayerUpdateType and check if it is
                // contained in the update type list, if so, we add the current bit to the flag
                if (UpdateTypes.Contains((EntityUpdateType) i)) {
                    updateTypeFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            // Write the update type flag
            packet.Write(updateTypeFlag);

            // Conditionally write the state and data fields
            if (UpdateTypes.Contains(EntityUpdateType.Position)) {
                packet.Write(Position);
            }

            if (UpdateTypes.Contains(EntityUpdateType.Scale)) {
                packet.Write(Scale);
            }

            if (UpdateTypes.Contains(EntityUpdateType.Animation)) {
                // First write the number of animations
                // Maximum number of animations in one packet can be 255, this should be more than enough
                var numAnimations = (byte) System.Math.Min(AnimationInfos.Count, byte.MaxValue);

                packet.Write(numAnimations);

                foreach (var animation in AnimationInfos) {
                    packet.Write(animation.AnimationIndex);

                    // Check whether there is info to write
                    if (animation.AnimationInfo == null) {
                        packet.Write(0);
                    } else {
                        // Write the length of the info
                        var animationInfoLength = (byte) System.Math.Min(animation.AnimationInfo.Length, byte.MaxValue);

                        packet.Write(animationInfoLength);

                        // Write the info in the packet
                        foreach (var animationInfo in animation.AnimationInfo) {
                            packet.Write(animationInfo);
                        }
                    }
                }
            }

            if (UpdateTypes.Contains(EntityUpdateType.State)) {
                packet.Write(State);
            }
        }

        public void ReadData(Packet packet) {
            EntityType = packet.ReadByte();
            Id = packet.ReadByte();

            // Read the byte flag representing update types and reconstruct it
            var updateTypeFlag = packet.ReadByte();
            // Keep track of value of current bit
            var currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(EntityUpdateType)).Length; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    UpdateTypes.Add((EntityUpdateType) i);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }

            // Based on the update types, we read the corresponding values
            if (UpdateTypes.Contains(EntityUpdateType.Position)) {
                Position = packet.ReadVector2();
            }

            if (UpdateTypes.Contains(EntityUpdateType.Scale)) {
                Scale = packet.ReadBool();
            }
            
            if (UpdateTypes.Contains(EntityUpdateType.Animation)) {
                // First read how many animations are in the packet
                var numAnimations = packet.ReadByte();

                for (var i = 0; i < numAnimations; i++) {
                    // Create new animation info
                    var animation = new EntityAnimationInfo {
                        AnimationIndex = packet.ReadByte()
                    };

                    // Read how many info bytes there are for this animation
                    var animationInfoLength = packet.ReadByte();

                    animation.AnimationInfo = new byte[animationInfoLength];
                    for (var j = 0; j < animationInfoLength; j++) {
                        animation.AnimationInfo[j] = packet.ReadByte();
                    }

                    AnimationInfos.Add(animation);
                }
            }

            if (UpdateTypes.Contains(EntityUpdateType.State)) {
                State = packet.ReadByte();
            }
        }
    }

    public enum EntityUpdateType {
        Position = 0,
        Scale,
        Animation,
        State
    }

    public class EntityAnimationInfo {
        public byte AnimationIndex { get; set; }
        public byte[] AnimationInfo { get; set;  }
    }
}