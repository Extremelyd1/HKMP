using System.Collections.Generic;
using Hkmp.Game;
using JetBrains.Annotations;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Client manager that handles the local client and related data.
    /// </summary>
    [PublicAPI]
    public interface IClientManager {
        /// <summary>
        /// The current team of the local player.
        /// </summary>
        Team Team { get; }
        
        /// <summary>
        /// A read-only collection of all connected players.
        /// </summary>
        IReadOnlyCollection<IClientPlayer> Players { get; }

        /// <summary>
        /// Get a specific player by their ID.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <returns>The player with the given ID, or null if no such player exists.</returns>
        IClientPlayer GetPlayer(ushort id);

        /// <summary>
        /// Try to get a specific player by their ID.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="player">The parameter that will contain the player if it exists.</param>
        /// <returns>True if the player was found, false otherwise.</returns>
        bool TryGetPlayer(ushort id, out IClientPlayer player);

        /// <summary>
        /// Changes the team of the local player.
        /// </summary>
        /// <param name="team">The team value.</param>
        void ChangeTeam(Team team);

        /// <summary>
        /// Changes the skin of the local player.
        /// </summary>
        /// <param name="skinId">The ID of the skin.</param>
        void ChangeSkin(byte skinId);
    }
}