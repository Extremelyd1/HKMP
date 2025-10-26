using InControl;

namespace Hkmp.Menu;

/// <summary>
/// Class that stores keybinds for HKMP, to allow them to be (de)serialized to the settings file.
/// </summary>
internal class Keybinds : PlayerActionSet {
    /// <summary>
    /// Keybind to open the chat.
    /// </summary>
    public PlayerAction OpenChat { get; }

    public Keybinds() {
        OpenChat = CreatePlayerAction("OpenChat");
        OpenChat.AddDefaultBinding(Key.T);
    }
}
