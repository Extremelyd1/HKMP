using System.Collections.Generic;

namespace Hkmp.Api.Server {
    /// <summary>
    /// The server manager that handles server state.
    /// </summary>
    public interface IServerManager {
        /// <summary>
        /// A read-only collection of all connected players.
        /// </summary>
        IReadOnlyCollection<IServerPlayer> Players { get; }

        /// <summary>
        /// Get a specific player by their ID.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <returns>The player with the given ID, or null if no such player exists.</returns>
        IServerPlayer GetPlayer(ushort id);

        /// <summary>
        /// Try to get a specific player by their ID.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="player">The parameter that will contain the player if it exists.</param>
        /// <returns>True if the player was found, false otherwise.</returns>
        bool TryGetPlayer(ushort id, out IServerPlayer player);
    }
}