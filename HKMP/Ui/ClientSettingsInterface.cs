using System;
using Hkmp.Game;
using Hkmp.Game.Settings;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using UnityEngine;

namespace Hkmp.Ui {
    public class ClientSettingsInterface {
        public event Action<Team> OnTeamRadioButtonChange;
        public event Action<byte> OnSkinIdChange;

        private readonly Game.Settings.GameSettings _clientGameSettings;
        private readonly RadioButtonBoxComponent _teamRadioButton;

        public ClientSettingsInterface(
            ModSettings modSettings,
            Game.Settings.GameSettings clientGameSettings,
            ComponentGroup settingsGroup,
            ComponentGroup connectGroup,
            PingInterface pingInterface
        ) {
            var modSettings1 = modSettings;
            _clientGameSettings = clientGameSettings;

            var x = 1920f - 210f;
            var y = 1080f - 75f;
            
            new TextComponent(
                settingsGroup,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Team Selection",
                FontManager.UIFontRegular,
                18
            );

            y -= 35;

            _teamRadioButton = new RadioButtonBoxComponent(
                settingsGroup,
                new Vector2(x, y),
                new Vector2(300, 35),
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
            _teamRadioButton.SetInteractable(false);

            y -= 200;

            _teamRadioButton.SetOnChange(value => {
                if (!_clientGameSettings.TeamsEnabled) {
                    return;
                }

                OnTeamRadioButtonChange?.Invoke((Team) value);
            });
            
            var skinSetting = new SettingsEntryInterface(
                settingsGroup,
                new Vector2(x, y),
                "Player skin ID",
                typeof(byte),
                0,
                0,
                o => {
                    OnSkinIdChange?.Invoke((byte) o);
                }
            );

            y -= 100;

            new ButtonComponent(
                settingsGroup,
                new Vector2(x, y),
                "Apply skin"
            ).SetOnPress(skinSetting.ApplySetting);

            y -= 40;
            
            new SettingsEntryInterface(
                settingsGroup,
                new Vector2(x, y),
                "Display ping",
                typeof(bool),
                false,
                modSettings1.DisplayPing,
                o => {
                    var newValue = (bool) o;
                    modSettings1.DisplayPing = newValue;

                    pingInterface.SetEnabled(newValue);
                },
                autoApply: true
            );

            y -= 75;
            
            new ButtonComponent(
                settingsGroup,
                new Vector2(x, y),
                "Back"
            ).SetOnPress(() => {
                settingsGroup.SetActive(false);
                connectGroup.SetActive(true);
            });
        }
        
        public void OnSuccessfulConnect() {
            _teamRadioButton.SetInteractable(true);
        }

        public void OnDisconnect() {
            _teamRadioButton.SetInteractable(false);
            _teamRadioButton.Reset();
        }

        public void OnTeamSettingChange() {
            if (_clientGameSettings.TeamsEnabled) {
                // If the team settings becomes enabled, we make it interactable again
                _teamRadioButton.SetInteractable(true);
            } else {
                // If the team settings becomes disabled, we reset it and make it non-interactable
                _teamRadioButton.SetInteractable(false);
                _teamRadioButton.Reset();
            }
        }
    }
}