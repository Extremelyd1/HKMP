using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hkmp.Api.Addon {
    /// <summary>
    /// Abstract base class for loading addons from file.
    /// </summary>
    public abstract class AddonLoader {
        /// <summary>
        /// The file pattern to look for when obtaining candidate files to load.
        /// </summary>
        private const string AssemblyFilePattern = "*.dll";
        
        /// <summary>
        /// The directory in which to look for assembly files. 
        /// </summary>
        private static readonly string ModsDirectoryPath = 
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Get the paths for all assembly files in the HKMP directory.
        /// </summary>
        /// <returns>A string array containing file paths.</returns>
        private static string[] GetAssemblyPaths() {
            return Directory.GetFiles(ModsDirectoryPath, AssemblyFilePattern);
        }

        /// <summary>
        /// Get all loadable types from the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly instance to get the types from.</param>
        /// <returns>An enumerator that traverses the loadable types.</returns>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
            try {
                return assembly.GetTypes();
            } catch (ReflectionTypeLoadException e) {
                return e.Types.Where(t => t != null);
            }
        }
        
        /// <summary>
        /// Load all addons given their type and given an API interface instance.
        /// </summary>
        /// <param name="apiObject">The API interface instance in object form.</param>
        /// <typeparam name="TAddon">The type of the addon.</typeparam>
        /// <typeparam name="TApiInterface">The type of the API interface.</typeparam>
        /// <returns>A list of addon instance of type TAddon.</returns>
        protected List<TAddon> LoadAddons<TAddon, TApiInterface>(object apiObject) {
            var addons = new List<TAddon>();
            
            var assemblyPaths = GetAssemblyPaths();

            foreach (var assemblyPath in assemblyPaths) {
                Logger.Get().Info(this, $"Trying to load assembly at: {assemblyPath}");

                Assembly assembly;
                try {
                    assembly = Assembly.LoadFrom(assemblyPath);
                } catch (Exception e) {
                    Logger.Get().Warn(this, 
                        $"  Could not load assembly, exception: {e.GetType()}, message: {e.Message}");
                    continue;
                }

                foreach (var type in GetLoadableTypes(assembly)) {
                    if (!type.IsClass
                        || type.IsAbstract
                        || type.IsInterface
                        || !type.IsSubclassOf(typeof(TAddon))
                    ) {
                        continue;
                    }
                    
                    Logger.Get().Info(this, "  Found ClientAddon extending class, constructing addon");

                    var constructor = type.GetConstructor(new[] {typeof(TApiInterface)});
                    if (constructor == null) {
                        Logger.Get().Warn(this, "  Could not find constructor for addon");
                        continue;
                    }

                    object addonObject;
                    try {
                        addonObject = constructor.Invoke(new[] {apiObject});
                    } catch (Exception e) {
                        Logger.Get().Warn(this, $"  Could not invoke constructor for addon, exception: {e.GetType()}, message: {e.Message}");
                        continue;
                    }

                    if (!(addonObject is TAddon addon)) {
                        Logger.Get().Warn(this, $"  Addon is not of type {typeof(TAddon).Name}");
                        continue;
                    }

                    addons.Add(addon);
                    // We only allow a single class extending the addon subclass
                    break;
                }
            }

            return addons;
        }
    }
}