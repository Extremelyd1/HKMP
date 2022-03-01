namespace Hkmp.Game.Settings {
    /// <summary>
    /// Settings class that stores user preferences.
    /// </summary>
    public class ModSettings {
        /// <summary>
        /// The authentication key for the user.
        /// </summary>
        public string AuthKey { get; set; } = null;

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
        public string ConnectAddress { get; set; }

        /// <summary>
        /// The last used port to join a server.
        /// </summary>
        public int ConnectPort { get; set; } = -1;

        /// <summary>
        /// The last used username to join a server.
        /// </summary>
        public string Username { get; set; }

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