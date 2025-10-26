using System;
using System.IO;
using System.Reflection;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Game.Server.Save;
using Hkmp.Game.Settings;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Networking.Server;
using HkmpServer.Command;
using HkmpServer.Logging;
using Newtonsoft.Json;

namespace HkmpServer {
    /// <summary>
    /// Specialization of the server manager for the console program.
    /// </summary>
    internal class ConsoleServerManager : ServerManager {
        /// <summary>
        /// Name of the file used to store save data.
        /// </summary>
        private const string SaveFileName = "save.json";

        /// <summary>
        /// The exit command for exiting the server.
        /// </summary>
        private readonly IServerCommand _exitCommand;
        /// <summary>
        /// The console settings command for changing console settings.
        /// </summary>
        private readonly IServerCommand _consoleSettingsCommand;
        /// <summary>
        /// The log command for changing log levels.
        /// </summary>
        private readonly IServerCommand _logCommand;

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
            PacketManager packetManager,
            ServerSettings serverSettings,
            ConsoleLogger consoleLogger
        ) : base(netServer, packetManager, serverSettings) {
            _exitCommand = new ExitCommand(this);
            _consoleSettingsCommand = new ConsoleSettingsCommand(this, InternalServerSettings);
            _logCommand = new LogCommand(consoleLogger);
        }

        /// <inheritdoc />
        public override void Initialize() {
            base.Initialize();
            
            // Start loading addons
            AddonManager.LoadAddons();

            // Register a callback for when the application is closed to stop the server
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => {
                if (Environment.ExitCode == 5) {
                    return;
                }

                Stop();
            };
        }

        /// <inheritdoc />
        public override void Start(int port, bool fullSynchronisation) {
            base.Start(port, fullSynchronisation);
            
            InitializeSaveFile();
        }

        /// <inheritdoc />
        protected override void RegisterCommands() {
            base.RegisterCommands();

            CommandManager.RegisterCommand(_exitCommand);
            CommandManager.RegisterCommand(_consoleSettingsCommand);
            CommandManager.RegisterCommand(_logCommand);
        }

        /// <inheritdoc />
        protected override void DeregisterCommands() {
            base.DeregisterCommands();
            
            CommandManager.DeregisterCommand(_exitCommand);
            CommandManager.DeregisterCommand(_consoleSettingsCommand);
            CommandManager.DeregisterCommand(_logCommand);
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
                // Read the JSON text from the file
                var saveFileText = File.ReadAllText(_saveFilePath);

                try {
                    var consoleSaveFile = JsonConvert.DeserializeObject<ConsoleSaveFile>(saveFileText);

                    serverSaveData = consoleSaveFile.ToServerSaveData();
                    return true;
                } catch (Exception e) {
                    Logger.Error($"Could not read the JSON from save file:\n{e}");
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
                    var consoleSaveFile = ConsoleSaveFile.FromServerSaveData(serverSaveData);
                    var saveFileText = JsonConvert.SerializeObject(consoleSaveFile, Formatting.Indented);

                    File.WriteAllText(_saveFilePath, saveFileText);
                } catch (Exception e) {
                    Logger.Error($"Exception occurred while serializing/writing to save file:\n{e}");
                }
            }
        }
    }
}
