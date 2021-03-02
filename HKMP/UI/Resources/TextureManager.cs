using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HKMP.UI.Resources {
    public class TextureManager {
        private const string ImagePathPrefix = "HKMP.UI.Resources.Images";

        private static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

        public static void LoadTextures() {
            // Clear current list of textures before trying to load new ones
            _textures.Clear();

            var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var name in resourceNames) {
                if (name.StartsWith(ImagePathPrefix)) {
                    try {
                        // Get the texture stream from assembly by name
                        var textureStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                        if (textureStream == null) {
                            Logger.Error(typeof(TextureManager), $"Could not load resource with name {name}, textureStream was null");
                            continue;
                        }
                        
                        // Read texture stream to byte buffer
                        var byteBuffer = new byte[textureStream.Length];
                        textureStream.Read(byteBuffer, 0, byteBuffer.Length);

                        // Create texture object and load buffer into texture
                        var texture = new Texture2D(1, 1);
                        texture.LoadImage(byteBuffer.ToArray(), true);

                        // Get the name of the texture by splitting on the '.' character
                        // For example, for 'HKMP.Images.some_name.png' we get the 'some_name'
                        // which is the second to last in the split
                        var splitName = name.Split('.');
                        var textureName = splitName[splitName.Length - 2];

                        _textures.Add(textureName, texture);
                    } catch (Exception e) {
                        Logger.Error(typeof(TextureManager), $"Could not load resource with name {name}, exception: {e.Message}");
                    }
                }
            }
            
            Logger.Info(typeof(TextureManager), $"Successfully loaded {_textures.Count} textures");
        }

        public static Texture2D GetTexture(string textureName) {
            if (!_textures.ContainsKey(textureName)) {
                Logger.Warn(typeof(TextureManager), $"Tried getting texture by name {textureName}, which does not exist");
                return null;
            }

            return _textures[textureName];
        }
        
    }
}