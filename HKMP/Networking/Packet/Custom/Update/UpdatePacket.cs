using System.Collections.Generic;
using HKMP.Game.Client.Entity;
using UnityEngine;

namespace HKMP.Networking.Packet.Custom.Update {
    public abstract class UpdatePacket : Packet, IPacket {
        public ushort SequenceNumber { get; set; }

        protected UpdatePacket() {
        }

        protected UpdatePacket(Packet packet) : base(packet) {
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
            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Position)) {
                Write(playerUpdate.Position);
            }
            
            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Scale)) {
                Write(playerUpdate.Scale);
            }
            
            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.MapPosition)) {
                Write(playerUpdate.MapPosition);
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Animation)) {
                // First write the number of infos we are writing
                // We also limit this to a byte, if the list is larger than 255 animations,
                // we just don't send them the rest ¯\_(ツ)_/¯
                var numAnimations = (byte) Mathf.Min(playerUpdate.AnimationInfos.Count, 255);
                
                Write(numAnimations);

                for (var i = 0; i < numAnimations; i++) {
                    var animationInfo = playerUpdate.AnimationInfos[i];
                    
                    Write(animationInfo.ClipId);
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
            for (var i = 0; i < (int) PlayerUpdateType.Count; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    playerUpdate.UpdateTypes.Add((PlayerUpdateType) currentTypeValue);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }
            
            // Based on the update types, we read the corresponding values
            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Position)) {
                playerUpdate.Position = ReadVector3();
            }
            
            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Scale)) {
                playerUpdate.Scale = ReadVector3();
            }
            
            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.MapPosition)) {
                playerUpdate.MapPosition = ReadVector3();
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Animation)) {
                // We first read how many animations are in the packet
                var numAnimations = ReadByte();
                
                for (var i = 0; i < numAnimations; i++) {
                    // Create a new animation info instance
                    var animationInfo = new AnimationInfo {
                        ClipId = ReadUShort(),
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

        /**
         * Write the given EntityUpdate instance to the underlying packet
         */
        protected void WriteEntityUpdate(EntityUpdate entityUpdate) {
            Write((byte) entityUpdate.EntityType);
            Write(entityUpdate.Id);
            
            // Construct the byte flag representing update types
            byte updateTypeFlag = 0;
            foreach (var updateType in entityUpdate.UpdateTypes) {
                updateTypeFlag |= (byte) updateType;
            }
            
            // Write the update type flag
            Write(updateTypeFlag);

            // Conditionally write the state and data fields
            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)) {
                Write(entityUpdate.StateIndex);
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Variables)) {
                // First write the number of bytes we are writing
                Write(entityUpdate.FsmVariables.Count);

                foreach (var b in entityUpdate.FsmVariables) {
                    Write(b);
                }
            }
        }

        /**
         * Reads a single EntityUpdate of values from the packet and puts them in the
         * given EntityUpdates dictionary.
         * This assumes that the given EntityUpdate parameter is already instantiated.
         */
        protected void ReadEntityUpdate(EntityUpdate entityUpdate) {
            entityUpdate.EntityType = (EntityType) ReadByte();
            entityUpdate.Id = ReadByte();
            
            // Read the byte flag representing update types and reconstruct it
            var updateTypeFlag = ReadByte();
            // Keep track of value of current bit
            var currentTypeValue = 1;
            for (var i = 0; i < (int) EntityUpdateType.Count; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    entityUpdate.UpdateTypes.Add((EntityUpdateType) currentTypeValue);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }
            
            // Based on the update types, we read the corresponding values
            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)) {
                entityUpdate.StateIndex = ReadByte();
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Variables)) {
                // We first read how many bytes are in the array
                var numBytes = ReadByte();

                for (var i = 0; i < numBytes; i++) {
                    entityUpdate.FsmVariables.Add(ReadByte());
                }
            }
        }
    } 
    
    public class ServerUpdatePacket : UpdatePacket {
        
        public HashSet<UpdateType> UpdateTypes { get; }
        
        public PlayerUpdate PlayerUpdate { get; }
        
        public List<EntityUpdate> EntityUpdates { get; }

        public ServerUpdatePacket() {
            UpdateTypes = new HashSet<UpdateType>();
            PlayerUpdate = new PlayerUpdate();
            EntityUpdates = new List<EntityUpdate>();
        }
        
        public ServerUpdatePacket(Packet packet) : base(packet) {
            UpdateTypes = new HashSet<UpdateType>();
            PlayerUpdate = new PlayerUpdate();
            EntityUpdates = new List<EntityUpdate>();
        }
        
        public override Packet CreatePacket() {
            Reset();

            // Write packet header information
            Write(PacketId.PlayerUpdate);
            Write(SequenceNumber);
            
            // Construct the byte flag representing update types
            byte updateTypeFlag = 0;
            foreach (var updateType in UpdateTypes) {
                updateTypeFlag |= (byte) updateType;
            }
            
            // Write the update type flag
            Write(updateTypeFlag);

            if (UpdateTypes.Contains(UpdateType.PlayerUpdate)) {
                WritePlayerUpdate(PlayerUpdate);
            }
            
            if (UpdateTypes.Contains(UpdateType.EntityUpdate)) {
                // First we write the length of the entity update list
                Write((byte) EntityUpdates.Count);
                
                // Then for each EntityUpdate instance, we write their values
                foreach (var entityUpdate in EntityUpdates) {
                    WriteEntityUpdate(entityUpdate);
                }
            }

            WriteLength();

            return this;
        }

        public override void ReadPacket() {
            SequenceNumber = ReadUShort();
            
            // Read the byte flag representing update types and reconstruct it
            var updateTypeFlag = ReadByte();
            // Keep track of value of current bit
            var currentTypeValue = 1;
            for (var i = 0; i < (int) UpdateType.Count; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    UpdateTypes.Add((UpdateType) currentTypeValue);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }

            // Based on the update types, we read the corresponding values
            if (UpdateTypes.Contains(UpdateType.PlayerUpdate)) {
                ReadPlayerUpdate(PlayerUpdate);
            }
            
            if (UpdateTypes.Contains(UpdateType.EntityUpdate)) {
                // First we read the length of the entity update list
                var numEntityUpdates = ReadByte();

                // Then we read all the values into EntityUpdate instances
                for (var i = 0; i < numEntityUpdates; i++) {
                    // Create a new EntityUpdate instance
                    var entityUpdate = new EntityUpdate();
                    
                    // Read the values into the instance
                    ReadEntityUpdate(entityUpdate);
                    
                    // Add it to the list
                    EntityUpdates.Add(entityUpdate);
                }
            }
        }

        public void ResetValues() {
            UpdateTypes.Clear();
            
            PlayerUpdate.UpdateTypes.Clear();
            PlayerUpdate.AnimationInfos.Clear();
            
            EntityUpdates.Clear();
        }
    }

    public class ClientUpdatePacket : UpdatePacket {
        
        public HashSet<UpdateType> UpdateTypes { get; }
        
        public List<PlayerUpdate> PlayerUpdates { get; }
        
        public List<EntityUpdate> EntityUpdates { get; }
        
        public ClientUpdatePacket() {
            UpdateTypes = new HashSet<UpdateType>();
            PlayerUpdates = new List<PlayerUpdate>();
            EntityUpdates = new List<EntityUpdate>();
        }
        
        public ClientUpdatePacket(Packet packet) : base(packet) {
            UpdateTypes = new HashSet<UpdateType>();
            PlayerUpdates = new List<PlayerUpdate>();
            EntityUpdates = new List<EntityUpdate>();
        }
        
        public override Packet CreatePacket() {
            Reset();
            
            // Write packet header information
            Write(PacketId.PlayerUpdate);
            Write(SequenceNumber);
            
            // Construct the byte flag representing update types
            byte updateTypeFlag = 0;
            foreach (var updateType in UpdateTypes) {
                updateTypeFlag |= (byte) updateType;
            }
            
            // Write the update type flag
            Write(updateTypeFlag);

            if (UpdateTypes.Contains(UpdateType.PlayerUpdate)) {
                // First we write the length of the player update list
                Write((byte) PlayerUpdates.Count);

                // Then for each PlayerUpdate instance, we write their values
                foreach (var playerUpdate in PlayerUpdates) {
                    WritePlayerUpdate(playerUpdate);
                }
            }

            if (UpdateTypes.Contains(UpdateType.EntityUpdate)) {
                // First we write the length of the entity update list
                Write((byte) EntityUpdates.Count);
                
                // Then for each EntityUpdate instance, we write their values
                foreach (var entityUpdate in EntityUpdates) {
                    WriteEntityUpdate(entityUpdate);
                }
            }

            WriteLength();

            return this;
        }

        public override void ReadPacket() {
            SequenceNumber = ReadUShort();

            // Read the byte flag representing update types and reconstruct it
            var updateTypeFlag = ReadByte();
            // Keep track of value of current bit
            var currentTypeValue = 1;
            for (var i = 0; i < (int) UpdateType.Count; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    UpdateTypes.Add((UpdateType) currentTypeValue);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }
            
            // Based on the update types, we read the corresponding values
            if (UpdateTypes.Contains(UpdateType.PlayerUpdate)) {
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
            
            if (UpdateTypes.Contains(UpdateType.EntityUpdate)) {
                // First we read the length of the entity update list
                var numEntityUpdates = ReadByte();

                // Then we read all the values into the EntityUpdates dictionary
                for (var i = 0; i < numEntityUpdates; i++) {
                    // Create a new EntityUpdate instance
                    var entityUpdate = new EntityUpdate();
                    
                    // Read the values into the instance
                    ReadEntityUpdate(entityUpdate);
                    
                    // Add it to the list
                    EntityUpdates.Add(entityUpdate);
                }
            }
        }
    }

    public enum UpdateType {
        PlayerUpdate = 1,
        EntityUpdate = 2,
        
        // Represents the number of values in the enum
        Count = 2
    }
}