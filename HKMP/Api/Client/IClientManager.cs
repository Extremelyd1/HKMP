using Hkmp.Game;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Client manager that handles the local client and related data.
    /// </summary>
    public interface IClientManager {
        /// <summary>
        /// The current team of the local player.
        /// </summary>
        Team Team { get; }

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