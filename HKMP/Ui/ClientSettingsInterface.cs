using System;
using Hkmp.Game;
using Hkmp.Game.Settings;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Ui;

/// <summary>
/// Class for creating and managing the client settings interface.
/// </summary>
internal class ClientSettingsInterface {
    /// <summary>
    /// Event that is called when the team is changed through the radio buttons.
    /// </summary>
    public event Action<Team> OnTeamRadioButtonChange;

    /// <summary>
    /// Event that is called when the skin ID is changed.
    /// </summary>
    public event Action<byte> OnSkinIdChange;

    /// <summary>
    /// The client <see cref="ServerSettings"/> instance.
    /// </summary>
    private readonly ServerSettings _clientServerSettings;

    /// <summary>
    /// Compound condition for whether the team setting should be enabled.
    /// </summary>
    private readonly CompoundCondition _teamCondition;

    /// <summary>
    /// Compound condition for whether the skin setting should be enabled.
    /// </summary>
    private readonly CompoundCondition _skinCondition;

    public ClientSettingsInterface(
        ModSettings modSettings,
        ServerSettings clientServerSettings,
        ComponentGroup settingsGroup,
        ComponentGroup connectGroup,
        PingInterface pingInterface
    ) {
        settingsGroup.SetActive(false);

        _clientServerSettings = clientServerSettings;

        var x = 1920f - 210f;
        var y = 1080f - 100f;

        new TextComponent(
            settingsGroup,
            new Vector2(x, y),
            new Vector2(240f, ButtonComponent.DefaultHeight),
            "Settings",
            UiManager.HeaderFontSize,
            alignment: TextAnchor.MiddleLeft
        );

        var closeButton = new ButtonComponent(
            settingsGroup,
            new Vector2(x + 240f / 2f - ButtonComponent.DefaultHeight / 2f, y),
            new Vector2(ButtonComponent.DefaultHeight, ButtonComponent.DefaultHeight),
            "",
            TextureManager.CloseButtonBg,
            FontManager.UIFontRegular,
            UiManager.NormalFontSize
        );
        closeButton.SetOnPress(() => {
            settingsGroup.SetActive(false);
            connectGroup.SetActive(true);
        });

        y -= ButtonComponent.DefaultHeight + 30f;

        var skinSetting = new SettingsEntryInterface(
            settingsGroup,
            new Vector2(x, y),
            "Player skin ID",
            typeof(byte),
            0,
            0,
            o => { OnSkinIdChange?.Invoke((byte) o); },
            true
        );
        skinSetting.SetInteractable(false);
        _skinCondition = new CompoundCondition(
            () => skinSetting.SetInteractable(true),
            () => skinSetting.SetInteractable(false),
            false, true
        );

        y -= InputComponent.DefaultHeight + 8f;

        new SettingsEntryInterface(
            settingsGroup,
            new Vector2(x, y),
            "Display ping",
            typeof(bool),
            false,
            modSettings.DisplayPing,
            o => {
                var newValue = (bool) o;
                modSettings.DisplayPing = newValue;

                pingInterface.SetEnabled(newValue);
            },
            true
        );

        y -= SettingsEntryInterface.CheckboxSize + 8f;

        var teamRadioButton = new RadioButtonBoxComponent(
            settingsGroup,
            new Vector2(x, y),
            "Team selection",
            new[] {
                "None",
                "Moss",
                "Hive",
                "Grimm",
                "Lifeblood",
            },
            0
        );
        // Make it non-interactable by default
        teamRadioButton.SetInteractable(false);
        _teamCondition = new CompoundCondition(
            () => teamRadioButton.SetInteractable(true),
            () => {
                teamRadioButton.SetInteractable(false);
                teamRadioButton.Reset();
            },
            false, false, true
        );

        teamRadioButton.SetOnChange(value => {
            if (!_clientServerSettings.TeamsEnabled) {
                return;
            }

            OnTeamRadioButtonChange?.Invoke((Team) value);
        });
    }

    /// <summary>
    /// Callback method for when the client successfully connects.
    /// </summary>
    public void OnSuccessfulConnect() {
        _teamCondition.SetCondition(0, true);
        _skinCondition.SetCondition(0, true);
    }

    /// <summary>
    /// Callback method for when the client disconnects.
    /// </summary>
    public void OnDisconnect() {
        _teamCondition.SetCondition(0, false);
        _skinCondition.SetCondition(0, false);
    }

    /// <summary>
    /// Callback method for when the team setting in <see cref="ServerSettings"/> is changed.
    /// </summary>
    public void OnTeamSettingChange() {
        _teamCondition.SetCondition(1, _clientServerSettings.TeamsEnabled);
    }

    /// <summary>
    /// Callback method for when an addon sets the availability of team selection.
    /// </summary>
    /// <param name="value"></param>
    public void OnAddonSetTeamSelection(bool value) {
        _teamCondition.SetCondition(2, value);
    }

    /// <summary>
    /// Callback method for when an sets the availability of skin selection.
    /// </summary>
    /// <param name="value"></param>
    public void OnAddonSetSkinSelection(bool value) {
        _skinCondition.SetCondition(1, value);
    }
}
