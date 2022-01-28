using System;
using System.IO;
using System.Reflection;

namespace HkmpServer {
    /// <summary>
    /// Launcher class with the entry point for the program. Primarily here to make sure embedded assemblies
    /// are resolved and loaded correctly.
    /// </summary>
    internal class Launcher {
        public static void Main(string[] args) {
            // Register event listeners for when assemblies are trying to get resolved
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveAssembly;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            new HkmpServer().Initialize(args);
        }
        
        /// <summary>
        /// Callback for assembly resolve event. Will try to find and load an embedded assembly for the assembly
        /// that is trying to get resolved.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="eventArgs">The event arguments.</param>
        /// <returns>The resolved assembly, or null if no such assembly could be found.</returns>
        private static Assembly ResolveAssembly(object sender, ResolveEventArgs eventArgs) {
            var assemblyName = eventArgs.Name.Split(',')[0];
            var currentAssembly = Assembly.GetExecutingAssembly();

            // Try to find the assembly as an embedded resource
            var stream = currentAssembly.GetManifestResourceStream($"HKMPServer.Lib.{assemblyName}.dll");
            if (stream == null) {
                return null;
            }
            
            Console.WriteLine($"Found resource for resolving assembly: {assemblyName}");
            
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            try {
                return Assembly.Load(memoryStream.ToArray());
            } catch (BadImageFormatException ex) {
                Console.WriteLine($"Exception occurred while loading assembly: {ex.Message}");
            }

            return null;
        }
    }
}