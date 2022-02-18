using System;
using Hkmp.Game;
using Hkmp.Game.Settings;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Ui {
    public class ClientSettingsInterface {
        public event Action<Team> OnTeamRadioButtonChange;
        public event Action<byte> OnSkinIdChange;

        private readonly Game.Settings.GameSettings _clientGameSettings;

        private readonly CompoundCondition _teamCondition;
        private readonly CompoundCondition _skinCondition;

        public ClientSettingsInterface(
            ModSettings modSettings,
            Game.Settings.GameSettings clientGameSettings,
            ComponentGroup settingsGroup,
            ComponentGroup connectGroup,
            PingInterface pingInterface
        ) {
            settingsGroup.SetActive(false);
            
            _clientGameSettings = clientGameSettings;

            var x = 1920f - 210f;
            var y = 1080f - 100f;

            new TextComponent(
                settingsGroup,
                new Vector2(x, y),
                new Vector2(180f, ButtonComponent.DefaultHeight),
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
                o => {
                    OnSkinIdChange?.Invoke((byte) o);
                },
                true
            );
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
                new[] {
                    "No team",
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
                if (!_clientGameSettings.TeamsEnabled) {
                    return;
                }

                OnTeamRadioButtonChange?.Invoke((Team) value);
            });
        }
        
        public void OnSuccessfulConnect() {
            _teamCondition.SetCondition(0, true);
            _skinCondition.SetCondition(0, true);
        }

        public void OnDisconnect() {
            _teamCondition.SetCondition(0, false);
            _skinCondition.SetCondition(0, false);
        }

        public void OnTeamSettingChange() {
            _teamCondition.SetCondition(1, _clientGameSettings.TeamsEnabled);
        }

        public void OnAddonSetTeamSelection(bool value) {
            _teamCondition.SetCondition(2, value);
        }

        public void OnAddonSetSkinSelection(bool value) {
            _skinCondition.SetCondition(1, value);
        }
    }
}