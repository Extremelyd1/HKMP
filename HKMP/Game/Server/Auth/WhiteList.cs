using System.Collections.Generic;
using System.IO;
using Hkmp.Util;
using Newtonsoft.Json;

namespace Hkmp.Game.Server.Auth {
    /// <summary>
    /// Class that managed the whitelist.
    /// </summary>
    public class WhiteList : AuthKeyList {
        private const string WhiteListFileName = "whitelist.json";
        
        public bool IsEnabled { get; set; }

        [JsonProperty]
        private readonly HashSet<string> _preListed;

        public WhiteList() {
            _preListed = new HashSet<string>();
        }

        public bool IsPreListed(string name) {
            return _preListed.Contains(name.ToLower());
        }

        public void AddPreList(string name) {
            _preListed.Add(name.ToLower());
            
            WriteToFile();
        }

        public void RemovePreList(string name) {
            _preListed.Remove(name.ToLower());
            
            WriteToFile();
        }

        public void ClearPreList() {
            _preListed.Clear();
            
            WriteToFile();
        }

        public string GetPreListed() {
            return string.Join(", ", _preListed);
        }

        public static WhiteList LoadFromFile() {
            var whiteListFilePath = Path.Combine(FileUtil.GetCurrentPath(), WhiteListFileName);
            if (File.Exists(whiteListFilePath)) {
                return FileUtil.LoadObjectFromJsonFile<WhiteList>(whiteListFilePath);
            }

            return new WhiteList();
        }
        
        public override void WriteToFile() {
            var authListFilePath = Path.Combine(FileUtil.GetCurrentPath(), WhiteListFileName);

            FileUtil.WriteObjectToJsonFile(this, authListFilePath);
        }
    }
}