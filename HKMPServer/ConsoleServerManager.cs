using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Networking.Server;
using HkmpServer.Command;
using HkmpServer.Logging;

namespace HkmpServer {
    /// <summary>
    /// Specialization of the server manager for the console program.
    /// </summary>
    internal class ConsoleServerManager : ServerManager {
        /// <summary>
        /// Name of the file used to store save data.
        /// </summary>
        private const string SaveFileName = "save.dat";

        /// <summary>
        /// The logger class for logging to console.
        /// </summary>
        private readonly ConsoleLogger _consoleLogger;

        /// <summary>
        /// Lock object for asynchronous access to the save file.
        /// </summary>
        private readonly object _saveFileLock = new object();

        /// <summary>
        /// The absolute file path of the save file.
        /// </summary>
        private string _saveFilePath;

        public ConsoleServerManager(
            NetServer netServer,
            ServerSettings serverSettings,
            PacketManager packetManager,
            ConsoleLogger consoleLogger
        ) : base(netServer, serverSettings, packetManager) {
            _consoleLogger = consoleLogger;

            // Start loading addons
            AddonManager.LoadAddons();

            // Register a callback for when the application is closed to stop the server
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => {
                if (Environment.ExitCode == 5) {
                    return;
                }

                Stop();
            };

            InitializeSaveFile();
        }

        /// <inheritdoc />
        protected override void RegisterCommands() {
            base.RegisterCommands();

            CommandManager.RegisterCommand(new ExitCommand(this));
            CommandManager.RegisterCommand(new ConsoleSettingsCommand(this, InternalServerSettings));
            CommandManager.RegisterCommand(new LogCommand(_consoleLogger));
        }

        /// <inheritdoc />
        protected override void OnSaveUpdate(ushort id, SaveUpdate packet) {
            base.OnSaveUpdate(id, packet);

            // After the server manager has processed the save update, we write the current save data to file
            WriteToSaveFile(ServerSaveData);
        }

        /// <summary>
        /// Initialize the save file by either reading it from disk or creating a new one and writing it to disk.
        /// </summary>
        /// <exception cref="Exception">Thrown when the directory of the assembly could not be found.</exception>
        private void InitializeSaveFile() {
            // We first try to get the entry assembly in case the executing assembly was
            // embedded in the standalone server
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null) {
                // If the entry assembly doesn't exist, we fall back on the executing assembly
                assembly = Assembly.GetExecutingAssembly();
            }

            var currentPath = Path.GetDirectoryName(assembly.Location);
            if (currentPath == null) {
                throw new Exception("Could not get directory of assembly for save file");
            }

            lock (_saveFileLock) {
                _saveFilePath = Path.Combine(currentPath, SaveFileName);

                // If the file exists, simply read it into the current save data for the server
                // Otherwise, create an empty dictionary for save data and save it to file
                if (File.Exists(_saveFilePath) && TryReadSaveFile(out var saveData)) {
                    ServerSaveData = saveData;
                } else {
                    ServerSaveData = new ServerSaveData();

                    WriteToSaveFile(ServerSaveData);
                }
            }
        }

        /// <summary>
        /// Try to read the save data in the save file into a server save data instance.
        /// </summary>
        /// <param name="serverSaveData">The server save data instance if it was read, otherwise null.</param>
        /// <returns>true if the save file could be read, false otherwise.</returns>
        private bool TryReadSaveFile(out ServerSaveData serverSaveData) {
            lock (_saveFileLock) {
                // Read the raw bytes from the file
                var bytes = File.ReadAllBytes(_saveFilePath);

                try {
                    // We use the Packet class to easily read the raw bytes in the data
                    var packet = new Packet(bytes);

                    // First read the global save data from the packet using the method in CurrentSave
                    var globalSaveData = CurrentSave.ReadSaveDataDict(packet);
                    
                    // Then read the number of players from the packet for player specific save data
                    var numPlayers = packet.ReadUShort();

                    var playerSaveData = new Dictionary<string, Dictionary<ushort, byte[]>>();
                    
                    // Next, for each of the players read their save data into the dictionary
                    for (var i = 0; i < numPlayers; i++) {
                        // Read the auth key of the player
                        var authKey = packet.ReadString();
                        
                        // And read the save data
                        var saveData = CurrentSave.ReadSaveDataDict(packet);
                        
                        // Store it in the player save data dictionary
                        playerSaveData[authKey] = saveData;
                    }

                    serverSaveData = new ServerSaveData {
                        GlobalSaveData = globalSaveData,
                        PlayerSaveData = playerSaveData
                    };
                    return true;
                } catch (Exception e) {
                    Logger.Error($"Could not read the save data from file:\n{e}");
                }

                serverSaveData = null;
                return false;
            }
        }

        /// <summary>
        /// Write the save data from the server to the save file.
        /// </summary>
        /// <param name="serverSaveData">The save data from the server to write to file.</param>
        private void WriteToSaveFile(ServerSaveData serverSaveData) {
            lock (_saveFileLock) {
                try {
                    // We use the Packet class to easily write the data to raw bytes
                    var packet = new Packet();

                    // First write the global save data to the packet using the method in CurrentSave
                    CurrentSave.WriteSaveDataDict(serverSaveData.GlobalSaveData, packet);

                    // Then write the number of players for which we have player specific save data
                    var numPlayers = serverSaveData.PlayerSaveData.Keys.Count;
                    if (numPlayers > ushort.MaxValue) {
                        throw new Exception(
                            $"Number of players for player specific save data is too large: {numPlayers}");
                    }

                    packet.Write((ushort) numPlayers);

                    foreach (var playerDataEntry in serverSaveData.PlayerSaveData) {
                        var authKey = playerDataEntry.Key;
                        var saveData = playerDataEntry.Value;

                        packet.Write(authKey);
                        CurrentSave.WriteSaveDataDict(saveData, packet);
                    }

                    // And finally obtain the byte array to write to file
                    var bytes = packet.ToArray();

                    File.WriteAllBytes(_saveFilePath, bytes);
                } catch (Exception e) {
                    Logger.Error($"Exception occurred while writing to save file:\n{e}");
                }
            }
        }
    }
}
