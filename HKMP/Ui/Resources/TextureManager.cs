using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Hkmp.Ui.Resources {
    public class TextureManager {
        private const string ImagePathPrefix = "HKMP.Ui.Resources.Images";

        public static Texture2D ButtonBackground;
        public static Texture2D Checkmark;
        public static Texture2D InputFieldBackground;
        public static Texture2D ToggleBackground;
        public static Texture2D RadioFilled;
        public static Texture2D RadioBackground;
        public static Texture2D Divider;

        public static Texture2D NetworkIcon;

        public static void LoadTextures() {
            var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var name in resourceNames) {
                if (name.StartsWith(ImagePathPrefix)) {
                    try {
                        // Get the texture stream from assembly by name
                        var textureStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                        if (textureStream == null) {
                            Logger.Get().Error(typeof(TextureManager),
                                $"Could not load resource with name {name}, textureStream was null");
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

                        switch (textureName) {
                            case "button_background":
                                ButtonBackground = texture;
                                break;
                            case "checkmark":
                                Checkmark = texture;
                                break;
                            case "input_field_background":
                                InputFieldBackground = texture;
                                break;
                            case "toggle_background":
                                ToggleBackground = texture;
                                break;
                            case "radio_filled":
                                RadioFilled = texture;
                                break;
                            case "radio_background":
                                RadioBackground = texture;
                                break;
                            case "divider":
                                Divider = texture;
                                break;
                            case "network_icon":
                                NetworkIcon = texture;
                                break;
                        }
                    } catch (Exception e) {
                        Logger.Get().Error(typeof(TextureManager),
                            $"Could not load resource with name {name}, exception: {e.Message}");
                    }
                }
            }
        }
    }
}