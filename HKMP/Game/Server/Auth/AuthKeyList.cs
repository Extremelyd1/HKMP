using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hkmp.Game.Server.Auth {
    /// <summary>
    /// Generic authentication list containing a set of authentication keys.
    /// </summary>
    public abstract class AuthKeyList {
        [JsonProperty]
        private readonly HashSet<string> _approved;

        public AuthKeyList() {
            _approved = new HashSet<string>();
        }

        public bool Contains(string authKey) {
            return _approved.Contains(authKey);
        }

        public void Add(string authKey) {
            _approved.Add(authKey);
            
            WriteToFile();
        }

        public void Remove(string authKey) {
            _approved.Remove(authKey);
            
            WriteToFile();
        }

        public void Clear() {
            _approved.Clear();
            
            WriteToFile();
        }

        public abstract void WriteToFile();
    }
}