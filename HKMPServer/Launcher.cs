namespace HkmpServer {
    /// <summary>
    /// Launcher class with the entry point for the program. Primarily here to make sure embedded assemblies
    /// are resolved and loaded correctly.
    /// </summary>
    internal static class Launcher {
        /// <summary>
        /// Main entry point for the HKMP Server program.
        /// </summary>
        /// <param name="args">Command line arguments for the server.</param>
        public static void Main(string[] args) {
            new HkmpServer().Initialize(args);
        }
    }
}
