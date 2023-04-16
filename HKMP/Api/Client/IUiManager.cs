
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
    void DisableTeamSelection();

    /// <summary>
    /// Enables the ability for the user to select a team if it was disabled.
    /// </summary>
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
