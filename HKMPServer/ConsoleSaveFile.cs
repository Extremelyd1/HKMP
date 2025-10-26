using Hkmp.Game.Server.Save;
using Hkmp.Util;
using Newtonsoft.Json;

namespace HkmpServer {
    /// <summary>
    /// Class for serialization and deserialization of save data from a standalone server to the local save file.
    /// See <see cref="ServerSaveData"/> for the representation of the same data of the running server.
    /// </summary>
    internal class ConsoleSaveFile : ModSaveFile {
        /// <summary>
        /// The global save data for the server. E.g. broken walls, open doors, etc.
        /// </summary>
        [JsonProperty("global_save_data")]
        public SaveData GlobalSaveData { get; set; }

        public ConsoleSaveFile() {
            GlobalSaveData = new SaveData();
        }

        /// <inheritdoc />
        public override ServerSaveData ToServerSaveData() {
            // Create new instance of server save data, which we return at the end
            var serverSaveData = new ServerSaveData {
                GlobalSaveData = EncodeUtil.ConvertToServerSaveData(GlobalSaveData)
            };

            foreach (var authKey in PlayerSaveData.Keys) {
                serverSaveData.PlayerSaveData[authKey] = EncodeUtil.ConvertToServerSaveData(PlayerSaveData[authKey]);
            }
        
            return serverSaveData;
        }

        /// <inheritdoc cref="ModSaveFile.FromServerSaveData"/>
        public new static ConsoleSaveFile FromServerSaveData(ServerSaveData serverSaveData) {
            // Create new instance of this class, which we return at the end
            var consoleSaveFile = new ConsoleSaveFile {
                GlobalSaveData = EncodeUtil.ConvertFromServerSaveData(serverSaveData.GlobalSaveData)
            };

            var playerSaveData = serverSaveData.PlayerSaveData;
            foreach (var authKey in playerSaveData.Keys) {
                var entries = EncodeUtil.ConvertFromServerSaveData(playerSaveData[authKey]);
                // Store the entries in the player save data dictionary of the instance
                consoleSaveFile.PlayerSaveData[authKey] = entries;
            }

            return consoleSaveFile;
        }
    }
}
