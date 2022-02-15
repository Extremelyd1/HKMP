namespace Hkmp.Game.Settings {
    /// <summary>
    /// Settings class that stores user preferences.
    /// </summary>
    public class ModSettings {
        /// <summary>
        /// The key to hide the HKMP UI. Default: Left ALT
        /// </summary>
        public int HideUiKey { get; set; } = 307;

        /// <summary>
        /// The key to open the chat. Default: T
        /// </summary>
        public int OpenChatKey { get; set; } = 116;

        /// <summary>
        /// The last used address to join a server.
        /// </summary>
        public string JoinAddress { get; set; }

        /// <summary>
        /// The last used port to join a server.
        /// </summary>
        public int JoinPort { get; set; } = -1;

        /// <summary>
        /// The last used username to join a server.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The last used port to host a server.
        /// </summary>
        public int HostPort { get; set; } = 26950;

        /// <summary>
        /// Whether to display a UI element for the ping.
        /// </summary>
        public bool DisplayPing { get; set; }

        /// <summary>
        /// Whether to automatically connect to the server when starting hosting.
        /// </summary>
        public bool AutoConnectWhenHosting { get; set; } = true;

        /// <summary>
        /// The last used game settings in a hosted server.
        /// </summary>
        public GameSettings GameSettings { get; set; }

    }
}