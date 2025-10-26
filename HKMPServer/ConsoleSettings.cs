namespace HkmpServer {
    /// <summary>
    /// Class that houses settings for the console program specifically. Settings that should be known upon starting
    /// the console program specifically.
    /// </summary>
    internal class ConsoleSettings {
        /// <summary>
        /// The port that the console program should run on.
        /// </summary>
        public int Port { get; set; } = 26950;
        
        /// <summary>
        /// Whether full synchronisation of bosses, enemies, worlds, and saves is enabled.
        /// </summary>
        public bool FullSynchronisation { get; set; }
    }
}
