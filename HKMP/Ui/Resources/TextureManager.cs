using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Ui.Resources;

/// <summary>
/// The texture manager for storing sprites of UI elements.
/// </summary>
internal static class TextureManager {
    /// <summary>
    /// The path prefix of embedded resources in the assembly.
    /// </summary>
    private const string ImagePathPrefix = "Hkmp.Ui.Resources.Images.";

    /// <summary>
    /// The suffix of image resources.
    /// </summary>
    private const string ImageSuffix = ".png";

    /// <summary>
    /// The suffix of image data resources.
    /// </summary>
    private const string TextureDataSuffix = ".dat";

    /// <summary>
    /// The button background sprites.
    /// </summary>
    public static MultiStateSprite ButtonBg;

    /// <summary>
    /// The input field background sprites.
    /// </summary>
    public static MultiStateSprite InputFieldBg;

    /// <summary>
    /// The radio button background sprites.
    /// </summary>
    public static MultiStateSprite RadioButtonBg;

    /// <summary>
    /// The close button background sprites.
    /// </summary>
    public static MultiStateSprite CloseButtonBg;

    /// <summary>
    /// The radio button toggle sprite.
    /// </summary>
    public static Sprite RadioButtonToggle;

    /// <summary>
    /// The checkbox toggle sprite.
    /// </summary>
    public static Sprite CheckBoxToggle;

    /// <summary>
    /// The HKMP logo sprite.
    /// </summary>
    public static Sprite HkmpLogo;

    /// <summary>
    /// The network icon sprite.
    /// </summary>
    public static Sprite NetworkIcon;

    /// <summary>
    /// Load texture by searching for the embedded resources in the assembly.
    /// </summary>
    public static void LoadTextures() {
        var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        foreach (var name in resourceNames) {
            if (name.StartsWith(ImagePathPrefix) && name.EndsWith(ImageSuffix)) {
                try {
                    var texture = GetTextureFromManifestResource(name);
                    if (texture == null) {
                        Logger.Error($"Could not find texture for manifest resource: {name}");
                        continue;
                    }

                    // Get the name of the texture by splitting on the '.' character
                    // For example, for 'HKMP.Images.some_name.png' we get the 'some_name'
                    // which is the second to last in the split
                    var splitName = name.Split('.');
                    var textureName = splitName[splitName.Length - 2];

                    Stream textureDataStream;
                    try {
                        textureDataStream = Assembly.GetExecutingAssembly()
                            .GetManifestResourceStream(ImagePathPrefix + textureName + TextureDataSuffix);
                    } catch {
                        // No data found for this texture
                        Logger.Error($"Error while getting resource stream for: {name}");
                        continue;
                    }

                    if (textureDataStream == null) {
                        var sprite = CreateSpriteFromTexture(texture);
                        SetSpriteVariableByName(textureName, sprite);
                        continue;
                    }

                    var slicedSprite = CreateSlicedSpriteFromTexture(
                        texture,
                        GetTextureBorderDataFromStream(textureDataStream)
                    );
                    SetSpriteVariableByName(textureName, slicedSprite);
                } catch (Exception e) {
                    Logger.Error($"Could not load resource with name \"{name}\":\n{e}");
                }
            }
        }
    }

    /// <summary>
    /// Get a Vector4 containing border data for a given stream.
    /// </summary>
    /// <param name="textureDataStream">The texture data stream.</param>
    /// <returns>A Vector4 containing border data.</returns>
    private static Vector4 GetTextureBorderDataFromStream(Stream textureDataStream) {
        var dataString = new StreamReader(textureDataStream).ReadToEnd();
        var splitData = dataString.Split(',');
        if (splitData.Length != 4) {
            Logger.Error("Texture data does not contain 4 entries");
            return Vector4.zero;
        }

        var borderFloats = new float[4];
        for (var i = 0; i < 4; i++) {
            if (!float.TryParse(splitData[i], out borderFloats[i])) {
                Logger.Error("Could not parse texture border floats");
                return Vector4.zero;
            }
        }

        return new Vector4(
            borderFloats[0],
            borderFloats[1],
            borderFloats[2],
            borderFloats[3]
        );
    }

    /// <summary>
    /// Get a texture from a manifest resource name.
    /// </summary>
    /// <param name="manifestResourceName">The name of the manifest resource.</param>
    /// <returns>The Texture2D instance if it could be loaded; otherwise null.</returns>
    private static Texture2D GetTextureFromManifestResource(string manifestResourceName) {
        // Get the texture stream from assembly by name
        Stream textureStream;

        try {
            textureStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestResourceName);
        } catch (Exception e) {
            Logger.Error(
                $"Could not get manifest resource stream for name \"{manifestResourceName}\":\n{e}");
            return null;
        }

        if (textureStream == null) {
            Logger.Error($"Could not load resource with name {manifestResourceName}, textureStream was null");
            return null;
        }

        // Read texture stream to byte buffer
        var byteBuffer = new byte[textureStream.Length];

        try {
            textureStream.Read(byteBuffer, 0, byteBuffer.Length);
        } catch (Exception e) {
            Logger.Error(
                $"Could not read resource stream for texture with name \"{manifestResourceName}\":\n{e}");
            return null;
        }

        // Create texture object and load buffer into texture
        var texture = new Texture2D(1, 1);
        texture.LoadImage(byteBuffer.ToArray(), true);

        return texture;
    }

    /// <summary>
    /// Sets the static variable in this class to the sprite based on the texture name.
    /// </summary>
    /// <param name="textureName">The name of the texture.</param>
    /// <param name="sprite">The sprite to set.</param>
    private static void SetSpriteVariableByName(string textureName, Sprite sprite) {
        switch (textureName) {
            case "button_background_neutral":
                ButtonBg.Neutral = sprite;
                return;
            case "button_background_hover":
                ButtonBg.Hover = sprite;
                return;
            case "button_background_active":
                ButtonBg.Active = sprite;
                return;
            case "button_background_disabled":
                ButtonBg.Disabled = sprite;
                return;
            case "input_field_background_neutral":
                InputFieldBg.Neutral = sprite;
                return;
            case "input_field_background_hover":
                InputFieldBg.Hover = sprite;
                return;
            case "input_field_background_active":
                InputFieldBg.Active = sprite;
                return;
            case "input_field_background_disabled":
                InputFieldBg.Disabled = sprite;
                return;
            case "radio_button_neutral":
                RadioButtonBg.Neutral = sprite;
                return;
            case "radio_button_hover":
                RadioButtonBg.Hover = sprite;
                return;
            case "radio_button_active":
                RadioButtonBg.Active = sprite;
                return;
            case "radio_button_disabled":
                RadioButtonBg.Disabled = sprite;
                return;
            case "radio_button_toggle":
                RadioButtonToggle = sprite;
                return;
            case "check_box_toggle":
                CheckBoxToggle = sprite;
                return;
            case "close_neutral":
                CloseButtonBg.Neutral = sprite;
                return;
            case "close_hover":
                CloseButtonBg.Hover = sprite;
                return;
            case "close_active":
                CloseButtonBg.Active = sprite;
                return;
            case "close_disabled":
                CloseButtonBg.Disabled = sprite;
                return;
            case "hkmp_logo":
                HkmpLogo = sprite;
                return;
            case "network_icon":
                NetworkIcon = sprite;
                return;
            default:
                Logger.Warn(
                    $"Encountered resource that is not recognised, and thus not loaded with name: '{textureName}'");
                return;
        }
    }

    /// <summary>
    /// Create a spliced sprite given a texture a border data.
    /// </summary>
    /// <param name="texture">The Texture2D.</param>
    /// <param name="border">The Vector4 containing border data.</param>
    /// <returns>The sliced sprite.</returns>
    private static Sprite CreateSlicedSpriteFromTexture(Texture2D texture, Vector4 border) {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(texture.width / 2f, texture.height / 2f),
            100,
            1,
            SpriteMeshType.FullRect,
            border
        );
    }

    /// <summary>
    /// Create a sprite given a texture.
    /// </summary>
    /// <param name="texture">The Texture2D.</param>
    /// <returns>The sprite.</returns>
    private static Sprite CreateSpriteFromTexture(Texture2D texture) {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(texture.width / 2f, texture.height / 2f)
        );
    }
}
