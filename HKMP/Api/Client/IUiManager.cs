using System;

namespace Hkmp.Api.Client;

/// <summary>
/// UI manager that handles all UI related interaction.
/// </summary>
public interface IUiManager {
    /// <summary>
    /// The message box that shows information related to HKMP.
    /// </summary>
    IChatBox ChatBox { get; }

    /// <summary>
    /// Disables the ability for the user to select a team.
    /// </summary>
    [Obsolete("DisableTeamSelection is deprecated. There is no UI anymore for changing team. Changing teams is handled by the IServerManager.")]
    void DisableTeamSelection();

    /// <summary>
    /// Enables the ability for the user to select a team if it was disabled.
    /// </summary>
    [Obsolete("EnableTeamSelection is deprecated. There is no UI anymore for changing team. Changing teams is handled by the IServerManager.")]
    void EnableTeamSelection();

    /// <summary>
    /// Disables the ability for the user to select a skin.
    /// </summary>
    void DisableSkinSelection();

    /// <summary>
    /// Enables the ability for the user to select a skin if it was disabled.
    /// </summary>
    void EnableSkinSelection();
}
