using System.Collections.Generic;
using Hkmp.Api.Server;

namespace Hkmp.Util {
    /// <summary>
    /// Class for utilities regarding player commands.
    /// </summary>
    internal static class CommandUtil {
        /// <summary>
        /// Try and get a player by name from the given collection.
        /// </summary>
        /// <param name="players">The collection of players.</param>
        /// <param name="username">The username to search for.</param>
        /// <param name="player">If the method returns will contain the player with the username if found;
        /// otherwise will contain null.</param>
        /// <returns>true if the player was found; otherwise false.</returns>
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