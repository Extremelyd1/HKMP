using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hkmp.Logging;

namespace Hkmp.Api.Addon;

/// <summary>
/// Abstract base class for loading addons from file.
/// </summary>
internal abstract class AddonLoader {
    /// <summary>
    /// The file pattern to look for when obtaining candidate files to load.
    /// </summary>
    private const string AssemblyFilePattern = "*.dll";

    /// <summary>
    /// The directory in which to look for assembly files.
    /// </summary>
    /// <returns>A string denoting the path of the current directory.</returns>
    protected abstract string GetCurrentDirectoryPath();

    /// <summary>
    /// Get the paths for all assembly files in the HKMP directory.
    /// </summary>
    /// <returns>A string array containing file paths.</returns>
    private string[] GetAssemblyPaths() {
        return Directory.GetFiles(GetCurrentDirectoryPath(), AssemblyFilePattern);
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
    /// <typeparam name="TAddon">The type of the addon.</typeparam>
    /// <returns>A list of addon instance of type TAddon.</returns>
    protected List<TAddon> LoadAddons<TAddon>() {
        var addons = new List<TAddon>();

        var assemblyPaths = GetAssemblyPaths();

        foreach (var assemblyPath in assemblyPaths) {
            Logger.Info($"Trying to load assembly at: {assemblyPath}");

            Assembly assembly;
            try {
                assembly = Assembly.LoadFrom(assemblyPath);
            } catch (Exception e) {
                Logger.Warn($"  Could not load assembly, exception:\n{e}");
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

                Logger.Info($"  Found {typeof(TAddon)} extending class, constructing addon");

                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null) {
                    Logger.Warn("  Could not find constructor for addon");
                    continue;
                }

                object addonObject;
                try {
                    addonObject = constructor.Invoke(Array.Empty<object>());
                } catch (Exception e) {
                    Logger.Warn(
                        $"  Could not invoke constructor for addon, exception:\n{e}");
                    continue;
                }

                if (!(addonObject is TAddon addon)) {
                    Logger.Warn($"  Addon is not of type {typeof(TAddon).Name}");
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
