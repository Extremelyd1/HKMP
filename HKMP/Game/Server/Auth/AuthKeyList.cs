using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hkmp.Game.Server.Auth {
    /// <summary>
    /// Generic authentication list containing a set of authentication keys.
    /// </summary>
    internal abstract class AuthKeyList {
        /// <summary>
        /// Set of approved authentication keys.
        /// </summary>
        [JsonProperty]
        private readonly HashSet<string> _approved;

        /// <summary>
        /// Construct the auth key list.
        /// </summary>
        protected AuthKeyList() {
            _approved = new HashSet<string>();
        }

        /// <summary>
        /// Whether a given key is contained in this list.
        /// </summary>
        /// <param name="authKey">The authentication key to check.</param>
        /// <returns>true if the key is contained in this list, false otherwise.</returns>
        public bool Contains(string authKey) {
            return _approved.Contains(authKey);
        }

        /// <summary>
        /// Add the given auth key to the list.
        /// </summary>
        /// <param name="authKey">The authentication key to add.</param>
        public void Add(string authKey) {
            _approved.Add(authKey);
            
            WriteToFile();
        }

        /// <summary>
        /// Remove the given auth key from the list.
        /// </summary>
        /// <param name="authKey">The authentication key to remove.</param>
        public void Remove(string authKey) {
            _approved.Remove(authKey);
            
            WriteToFile();
        }

        /// <summary>
        /// Remove all authentication keys from the list.
        /// </summary>
        public void Clear() {
            _approved.Clear();
            
            WriteToFile();
        }

        /// <summary>
        /// Write this authentication key list to a file.
        /// </summary>
        public abstract void WriteToFile();
    }
}