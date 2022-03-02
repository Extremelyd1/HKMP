using System.Collections.Generic;
using Hkmp.Api.Server;

namespace Hkmp.Util {
    public static class CommandUtil {
        public static bool TryGetPlayerByName(IReadOnlyCollection<IServerPlayer> players, string username, out IServerPlayer player) {
            foreach (var onlinePlayer in players) {
                if (onlinePlayer.Username.ToLower().Equals(username.ToLower())) {
                    player = onlinePlayer;
                    return true;
                }
            }

            player = null;
            return false;
        }
    }
}