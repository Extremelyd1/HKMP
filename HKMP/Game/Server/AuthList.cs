using System.Collections.Generic;
using System.IO;
using Hkmp.Util;

namespace Hkmp.Game.Server {
    /// <summary>
    /// Generic authentication list containing a set of authentication keys.
    /// </summary>
    public class AuthList {
        public bool IsEnabled { get; set; }
        
        private readonly HashSet<string> _approved;

        public AuthList() {
            _approved = new HashSet<string>();
        }

        public bool Contains(string authKey) {
            return _approved.Contains(authKey);
        }

        public void Add(string authKey) {
            _approved.Add(authKey);
        }

        public void Remove(string authKey) {
            _approved.Remove(authKey);
        }

        public static AuthList LoadFromFile(string fileName, bool defaultEnabled = false) {
            var whiteListFilePath = Path.Combine(FileUtil.GetCurrentPath(), fileName);
            if (File.Exists(whiteListFilePath)) {
                return FileUtil.LoadObjectFromJsonFile<AuthList>(whiteListFilePath);
            }

            return new AuthList {
                IsEnabled = defaultEnabled
            };
        }

        public void WriteToFile(string fileName) {
            var whiteListFilePath = Path.Combine(FileUtil.GetCurrentPath(), fileName);

            FileUtil.WriteObjectToJsonFile(this, whiteListFilePath);
        }
    }
}