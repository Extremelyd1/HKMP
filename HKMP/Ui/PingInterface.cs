using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Ui;

/// <summary>
/// Class for creating and managing the ping interface.
/// </summary>
internal class PingInterface {
    /// <summary>
    /// The margin between the image and text, and the borders of the screen.
    /// </summary>
    private const float ScreenBorderMargin = 20f;

    /// <summary>
    /// The margin between the icon and the text.
    /// </summary>
    private const float IconTextMargin = 25f;

    /// <summary>
    /// The maximum width of the text component.
    /// </summary>
    private const float TextWidth = 50f;

    /// <summary>
    /// The maximum height of the text component.
    /// </summary>
    private const float TextHeight = 25f;

    /// <summary>
    /// The size (width and height) of the icon displayed in front of the text.
    /// </summary>
    private const float IconSize = 20f;

    /// <summary>
    /// The component group for the ping display.
    /// </summary>
    private readonly ComponentGroup _pingComponentGroup;

    /// <summary>
    /// The mod settings.
    /// </summary>
    private readonly ModSettings _modSettings;

    /// <summary>
    /// The net client instance for retrieving the current ping.
    /// </summary>
    private readonly NetClient _netClient;

    public PingInterface(
        ComponentGroup pingComponentGroup,
        ModSettings modSettings,
        NetClient netClient
    ) {
        _pingComponentGroup = pingComponentGroup;
        _modSettings = modSettings;
        _netClient = netClient;

        // Since we are initially not connected, we disable the object by default
        pingComponentGroup.SetActive(false);

        new ImageComponent(
            pingComponentGroup,
            new Vector2(
                ScreenBorderMargin, 1080f - ScreenBorderMargin),
            new Vector2(IconSize, IconSize),
            TextureManager.NetworkIcon
        );

        var pingTextComponent = new TextComponent(
            pingComponentGroup,
            new Vector2(
                ScreenBorderMargin + IconSize + IconTextMargin, 1080f - ScreenBorderMargin),
            new Vector2(TextWidth, TextHeight),
            "",
            UiManager.NormalFontSize,
            alignment: TextAnchor.MiddleLeft
        );

        // Register on update so we can set the text to the latest average RTT
        MonoBehaviourUtil.Instance.OnUpdateEvent += () => {
            if (!netClient.IsConnected) {
                return;
            }

            pingTextComponent.SetText(netClient.UpdateManager.AverageRtt.ToString());
        };
    }

    /// <summary>
    /// Set whether the display is enabled or not.
    /// </summary>
    /// <param name="enabled">Whether the display should be enabled.</param>
    public void SetEnabled(bool enabled) {
        _pingComponentGroup.SetActive(enabled && _netClient.IsConnected && _modSettings.DisplayPing);
    }
}
