using System.Collections.Generic;
using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public abstract class PlayerUpdatePacket : Packet, IPacket {
        public ushort SequenceNumber { get; set; }

        protected PlayerUpdatePacket() {
        }

        protected PlayerUpdatePacket(Packet packet) : base(packet) {
        }

        public abstract Packet CreatePacket();

        public abstract void ReadPacket();

        /**
         * Write the given PlayerUpdate instance to the underlying packet
         */
        protected void WritePlayerUpdate(PlayerUpdate playerUpdate) {
            // Write the player update information
            Write(playerUpdate.Id);
            
            // Construct the byte flag representing update types
            byte updateTypeFlag = 0;
            foreach (var updateType in playerUpdate.UpdateTypes) {
                updateTypeFlag |= (byte) updateType;
            }
            
            // Write the update type flag
            Write(updateTypeFlag);
            
            // Conditionally write the position, scale, map position and animation info
            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Position)) {
                Write(playerUpdate.Position);
            }
            
            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Scale)) {
                Write(playerUpdate.Scale);
            }
            
            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.MapPosition)) {
                Write(playerUpdate.MapPosition);
            }

            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Animation)) {
                // First write the number of infos we are writing
                // We also limit this to a byte, if the list is larger than 255 animations,
                // we just don't send them the rest ¯\_(ツ)_/¯
                var numAnimations = (byte) Mathf.Min(playerUpdate.AnimationInfos.Count, 255);
                
                Write(numAnimations);

                for (var i = 0; i < numAnimations; i++) {
                    var animationInfo = playerUpdate.AnimationInfos[i];
                    
                    Write(animationInfo.ClipName);
                    Write(animationInfo.Frame);

                    // Check whether there is effect info to write
                    if (animationInfo.EffectInfo == null) {
                        Write((byte) 0);
                    } else {
                        // Again, we first write the length of the effect info array
                        var numEffects = animationInfo.EffectInfo.Length;

                        Write((byte) numEffects);

                        byte currentByte = 0;
                        byte currentBitValue = 1;

                        // And then the values of the array itself
                        for (var j = 0; j < numEffects; j++) {
                            if (animationInfo.EffectInfo[j]) {
                                currentByte |= currentBitValue;
                            }

                            if (currentBitValue == 128) {
                                // We have reached the last bit in our byte, so we reset
                                Write(currentByte);
                                currentByte = 0;
                                currentBitValue = 1;
                            } else {
                                // Otherwise we move on to the next bit by doubling the value
                                currentBitValue *= 2;
                            }
                        }

                        // If we haven't written this byte yet, we write it now
                        if (currentBitValue != 128) {
                            Write(currentByte);
                        }
                    }
                }
            }
        }

        /**
         * Reads the PlayerUpdate values from the packet and puts them in the
         * given PlayerUpdate instance.
         * This assumes that the given PlayerUpdate parameter is already instantiated.
         */
        protected void ReadPlayerUpdate(PlayerUpdate playerUpdate) {
            playerUpdate.Id = ReadUShort();
            
            // Read the byte flag representing update types and reconstruct it
            var updateTypeFlag = ReadByte();
            // Keep track of value of current bit
            var currentTypeValue = 1;
            for (var i = 0; i < (int) UpdatePacketType.Count; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    playerUpdate.UpdateTypes.Add((UpdatePacketType) currentTypeValue);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }
            
            // Based on the update types, we read the corresponding values
            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Position)) {
                playerUpdate.Position = ReadVector3();
            }
            
            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Scale)) {
                playerUpdate.Scale = ReadVector3();
            }
            
            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.MapPosition)) {
                playerUpdate.MapPosition = ReadVector3();
            }

            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Animation)) {
                // We first read how many animations are in the packet
                var numAnimations = ReadByte();
                
                for (var i = 0; i < numAnimations; i++) {
                    // Create a new animation info instance
                    var animationInfo = new AnimationInfo {
                        ClipName = ReadString(),
                        Frame = ReadByte()
                    };
                    
                    // Now we read how many effect are in the packet and
                    // create an array with that length
                    var numEffects = ReadByte();
                    // Check whether there is effect info to be read
                    if (numEffects != 0) {
                        var effectInfo = new bool[numEffects];

                        var currentByte = ReadByte();
                        byte currentBitValue = 1;

                        for (var j = 0; j < numEffects; j++) {
                            effectInfo[j] = (currentByte & currentBitValue) != 0;

                            if (currentBitValue == 128 && j != numEffects - 1) {
                                // We have reached the last bit in our byte, so we read another
                                currentByte = ReadByte();
                                currentBitValue = 1;
                            } else {
                                // Otherwise we move on to the next bit by doubling the value
                                currentBitValue *= 2;
                            }
                        }

                        // Save the effect info in the animation info instance
                        animationInfo.EffectInfo = effectInfo;
                    }

                    playerUpdate.AnimationInfos.Add(animationInfo);
                }
            }
        }
    } 
    
    public class ServerPlayerUpdatePacket : PlayerUpdatePacket {
        public PlayerUpdate PlayerUpdate { get; }

        public ServerPlayerUpdatePacket() {
            PlayerUpdate = new PlayerUpdate();
        }
        
        public ServerPlayerUpdatePacket(Packet packet) : base(packet) {
            PlayerUpdate = new PlayerUpdate();
        }
        
        public override Packet CreatePacket() {
            Reset();

            // Write packet header information
            Write(PacketId.PlayerUpdate);
            Write(SequenceNumber);

            WritePlayerUpdate(PlayerUpdate);

            WriteLength();

            return this;
        }

        public override void ReadPacket() {
            SequenceNumber = ReadUShort();

            ReadPlayerUpdate(PlayerUpdate);
        }

        public void ResetValues() {
            PlayerUpdate.UpdateTypes.Clear();
            PlayerUpdate.AnimationInfos.Clear();
        }
    }

    public class ClientPlayerUpdatePacket : PlayerUpdatePacket {
        public List<PlayerUpdate> PlayerUpdates { get; }
        
        public ClientPlayerUpdatePacket() {
            PlayerUpdates = new List<PlayerUpdate>();
        }
        
        public ClientPlayerUpdatePacket(Packet packet) : base(packet) {
            PlayerUpdates = new List<PlayerUpdate>();
        }
        
        public override Packet CreatePacket() {
            Reset();
            
            // Write packet header information
            Write(PacketId.PlayerUpdate);
            Write(SequenceNumber);
            
            // First we write the length of the player update list
            Write((byte) PlayerUpdates.Count);
            
            // Then for each PlayerUpdate instance, we write their values
            foreach (var playerUpdate in PlayerUpdates) {
                WritePlayerUpdate(playerUpdate);
            }

            WriteLength();

            return this;
        }

        public override void ReadPacket() {
            SequenceNumber = ReadUShort();

            // First we read the length of the player update list
            var numPlayerUpdates = ReadByte();
            
            // Then we read all the values into PlayerUpdate instances
            for (var i = 0; i < numPlayerUpdates; i++) {
                // We create a new instance
                var playerUpdate = new PlayerUpdate();
                
                // Read the information into the new instance
                ReadPlayerUpdate(playerUpdate);
                
                // Add the instance to the list
                PlayerUpdates.Add(playerUpdate);
            }
        }
    }

    public class PlayerUpdate {
        // ID: ushort - 2 bytes
        public ushort Id { get; set; }

        public HashSet<UpdatePacketType> UpdateTypes { get; }

        // Position: 3x float - 3x4 = 12 bytes
        public Vector3 Position { get; set; } = Vector3.zero;
        
        // Scale: 3x float - 3x4 = 12 bytes
        public Vector3 Scale { get; set; } = Vector3.zero;
        
        // Map position: 3x float - 3x4 = 12 bytes
        public Vector3 MapPosition { get; set; } = Vector3.zero;

        public List<AnimationInfo> AnimationInfos { get; }

        public PlayerUpdate() {
            UpdateTypes = new HashSet<UpdatePacketType>();
            AnimationInfos = new List<AnimationInfo>();
        }
    }

    public class AnimationInfo {
        // TODO: discretize this, so we can use less bytes
        public string ClipName { get; set; }
        public byte Frame { get; set; }
        // TODO: this can be optimized by creating bytes where the bits represent bool entries
        public bool[] EffectInfo { get; set; }
    }

    public enum UpdatePacketType {
        Position = 1,
        Scale = 2,
        MapPosition = 4,
        Animation = 8,
        
        // Represents the number of values in the enum
        Count = 4
    }
}