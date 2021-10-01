using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Hkmp.Api.Addon {
    public abstract class AddonLoader {
        private const string AssemblyFilePattern = "*.dll";
        
        private static readonly string ModsFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private string[] GetAssemblyPaths() {
            return Directory.GetFiles(ModsFolderPath, AssemblyFilePattern);
        }
        
        protected static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
            try {
                return assembly.GetTypes();
            } catch (ReflectionTypeLoadException e) {
                return e.Types.Where(t => t != null);
            }
        }
        
        protected List<TAddon> LoadAddons<TAddon, TApiInterface>(object api) {
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
                        addonObject = constructor.Invoke(new[] {api});
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