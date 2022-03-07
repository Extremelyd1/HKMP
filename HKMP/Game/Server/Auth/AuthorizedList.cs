using System.IO;
using Hkmp.Util;

namespace Hkmp.Game.Server.Auth {
    public class AuthorizedList : AuthKeyList {
        private const string AuthorizedFileName = "authorized.json";

        public static AuthorizedList LoadFromFile() {
            var authListFilePath = Path.Combine(FileUtil.GetCurrentPath(), AuthorizedFileName);
            if (File.Exists(authListFilePath)) {
                return FileUtil.LoadObjectFromJsonFile<AuthorizedList>(authListFilePath);
            }

            return new AuthorizedList();
        }        
        
        public override void WriteToFile() {
            var authListFilePath = Path.Combine(FileUtil.GetCurrentPath(), AuthorizedFileName);

            FileUtil.WriteObjectToJsonFile(this, authListFilePath);
        }
    }
}