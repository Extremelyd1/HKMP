using System;
using System.Collections.Generic;
using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data {
    public class PlayerUpdate : GenericClientData {
        public HashSet<PlayerUpdateType> UpdateTypes { get; }

        // Position: 3x float - 3x4 = 12 bytes
        public Vector2 Position { get; set; } = Vector2.Zero;

        // Scale: bool - 1 byte
        public bool Scale { get; set; }

        // Map position: 3x float - 3x4 = 12 bytes
        public Vector2 MapPosition { get; set; } = Vector2.Zero;

        public List<AnimationInfo> AnimationInfos { get; }

        public PlayerUpdate() {
            UpdateTypes = new HashSet<PlayerUpdateType>();
            AnimationInfos = new List<AnimationInfo>();

            IsReliable = false;
            DropReliableDataIfNewerExists = false;
        }

        public override void WriteData(Packet packet) {
            // Write the player update information
            packet.Write(Id);

            // Construct the byte flag representing update types
            byte updateTypeFlag = 0;
            // Keep track of value of current bit
            byte currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(PlayerUpdateType)).Length; i++) {
                // Cast the current index of the loop to a PlayerUpdateType and check if it is
                // contained in the update type list, if so, we add the current bit to the flag
                if (UpdateTypes.Contains((PlayerUpdateType) i)) {
                    updateTypeFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            // Write the update type flag
            packet.Write(updateTypeFlag);

            // Conditionally write the position, scale, map position and animation info
            if (UpdateTypes.Contains(PlayerUpdateType.Position)) {
                packet.Write(Position);
            }

            if (UpdateTypes.Contains(PlayerUpdateType.Scale)) {
                packet.Write(Scale);
            }

            if (UpdateTypes.Contains(PlayerUpdateType.MapPosition)) {
                packet.Write(MapPosition);
            }

            if (UpdateTypes.Contains(PlayerUpdateType.Animation)) {
                // First write the number of infos we are writing
                // We also limit this to a byte, if the list is larger than 255 animations,
                // we just don't send them the rest ¯\_(ツ)_/¯
                var numAnimations = (byte) System.Math.Min(AnimationInfos.Count, 255);

                packet.Write(numAnimations);

                for (var i = 0; i < numAnimations; i++) {
                    var animationInfo = AnimationInfos[i];

                    packet.Write(animationInfo.ClipId);
                    packet.Write(animationInfo.Frame);

                    // Check whether there is effect info to write
                    if (animationInfo.EffectInfo == null) {
                        packet.Write((byte) 0);
                    } else {
                        // Again, we first write the length of the effect info array
                        var numEffects = animationInfo.EffectInfo.Length;

                        packet.Write((byte) numEffects);

                        byte currentByte = 0;
                        byte currentBitValue = 1;

                        // And then the values of the array itself
                        for (var j = 0; j < numEffects; j++) {
                            if (animationInfo.EffectInfo[j]) {
                                currentByte |= currentBitValue;
                            }

                            if (currentBitValue == 128) {
                                // We have reached the last bit in our byte, so we reset
                                packet.Write(currentByte);
                                currentByte = 0;
                                currentBitValue = 1;
                            } else {
                                // Otherwise we move on to the next bit by doubling the value
                                currentBitValue *= 2;
                            }
                        }

                        // If we haven't written this byte yet, we write it now
                        if (currentBitValue != 128) {
                            packet.Write(currentByte);
                        }
                    }
                }
            }
        }

        public override void ReadData(Packet packet) {
            Id = packet.ReadUShort();

            // Read the byte flag representing update types and reconstruct it
            var updateTypeFlag = packet.ReadByte();
            // Keep track of value of current bit
            var currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(PlayerUpdateType)).Length; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    UpdateTypes.Add((PlayerUpdateType) i);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }

            // Based on the update types, we read the corresponding values
            if (UpdateTypes.Contains(PlayerUpdateType.Position)) {
                Position = packet.ReadVector2();
            }

            if (UpdateTypes.Contains(PlayerUpdateType.Scale)) {
                Scale = packet.ReadBool();
            }

            if (UpdateTypes.Contains(PlayerUpdateType.MapPosition)) {
                MapPosition = packet.ReadVector2();
            }

            if (UpdateTypes.Contains(PlayerUpdateType.Animation)) {
                // We first read how many animations are in the packet
                var numAnimations = packet.ReadByte();

                for (var i = 0; i < numAnimations; i++) {
                    // Create a new animation info instance
                    var animationInfo = new AnimationInfo {
                        ClipId = packet.ReadUShort(),
                        Frame = packet.ReadByte()
                    };

                    // Now we read how many effect are in the packet and
                    // create an array with that length
                    var numEffects = packet.ReadByte();
                    // Check whether there is effect info to be read
                    if (numEffects != 0) {
                        var effectInfo = new bool[numEffects];

                        var currentByte = packet.ReadByte();
                        byte currentBitValue = 1;

                        for (var j = 0; j < numEffects; j++) {
                            effectInfo[j] = (currentByte & currentBitValue) != 0;

                            if (currentBitValue == 128 && j != numEffects - 1) {
                                // We have reached the last bit in our byte, so we read another
                                currentByte = packet.ReadByte();
                                currentBitValue = 1;
                            } else {
                                // Otherwise we move on to the next bit by doubling the value
                                currentBitValue *= 2;
                            }
                        }

                        // Save the effect info in the animation info instance
                        animationInfo.EffectInfo = effectInfo;
                    }

                    AnimationInfos.Add(animationInfo);
                }
            }
        }
    }

    public enum PlayerUpdateType {
        Position = 0,
        Scale,
        MapPosition,
        Animation,
    }

    public class AnimationInfo {
        public ushort ClipId { get; set; }
        public byte Frame { get; set; }
        public bool[] EffectInfo { get; set; }
    }
}