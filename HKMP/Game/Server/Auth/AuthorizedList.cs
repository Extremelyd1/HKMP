using System.IO;
using Hkmp.Util;

namespace Hkmp.Game.Server.Auth {
    /// <summary>
    /// Authentication key list containing keys for authorized users.
    /// </summary>
    internal class AuthorizedList : AuthKeyList {
        /// <summary>
        /// The file name of the authorized list.
        /// </summary>
        private const string AuthorizedFileName = "authorized.json";

        /// <summary>
        /// Load the authorized lists from file.
        /// </summary>
        /// <returns>The loaded instance of the authorized list.</returns>
        public static AuthorizedList LoadFromFile() {
            var authListFilePath = Path.Combine(FileUtil.GetCurrentPath(), AuthorizedFileName);
            if (File.Exists(authListFilePath)) {
                return FileUtil.LoadObjectFromJsonFile<AuthorizedList>(authListFilePath);
            }

            return new AuthorizedList();
        }        

        /// <inheritdoc />
        public override void WriteToFile() {
            var authListFilePath = Path.Combine(FileUtil.GetCurrentPath(), AuthorizedFileName);

            FileUtil.WriteObjectToJsonFile(this, authListFilePath);
        }
    }
}