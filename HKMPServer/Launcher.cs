using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

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
            var assemblyStream = currentAssembly.GetManifestResourceStream($"HkmpServer.Lib.{assemblyName}.dll");
            if (assemblyStream == null) {
                return null;
            }

            var assemblyMemoryStream = new MemoryStream();
            assemblyStream.CopyTo(assemblyMemoryStream);

            Console.WriteLine($"Found resource for resolving assembly: {assemblyName}");

            // Exception message for when assembly loading fails
            const string assemblyLoadingExceptionMsg = "Exception occurred while loading assembly: ";

            // Try to get the PDB for the assembly if it exists
            var symbolStream = currentAssembly.GetManifestResourceStream($"HkmpServer.Lib.{assemblyName}.pdb");
            if (symbolStream != null) {
                Console.WriteLine("  Found PDB for assembly");

                var symbolMemoryStream = new MemoryStream();
                symbolStream.CopyTo(symbolMemoryStream);

                // Load the assembly with the PDB
                try {
                    return Assembly.Load(assemblyMemoryStream.ToArray(), symbolMemoryStream.ToArray());
                } catch (BadImageFormatException ex) {
                    Console.WriteLine(assemblyLoadingExceptionMsg + ex.Message);
                }
            }

            // Load the assembly without the PDB
            try {
                return Assembly.Load(assemblyMemoryStream.ToArray());
            } catch (BadImageFormatException ex) {
                Console.WriteLine(assemblyLoadingExceptionMsg + ex.Message);
            }

            return null;
        }
    }
}
