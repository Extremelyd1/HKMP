using HKMP.Game.Client;
using HKMP.Game.Settings;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Game;
using UnityEngine;

namespace HKMP.UI {
    public class ClientSettingsUI {
        private readonly ModSettings _modSettings;
        private readonly Game.Settings.GameSettings _clientGameSettings;
        private readonly ClientManager _clientManager;

        private readonly UIGroup _settingsGroup;
        private readonly UIGroup _connectGroup;

        private readonly PingUI _pingUi;

        public ClientSettingsUI(
            ModSettings modSettings,
            Game.Settings.GameSettings clientGameSettings,
            ClientManager clientManager,
            UIGroup settingsGroup, 
            UIGroup connectGroup,
            PingUI pingUi
        ) {
            _modSettings = modSettings;
            _clientManager = clientManager;
            _clientGameSettings = clientGameSettings;
            
            _settingsGroup = settingsGroup;
            _connectGroup = connectGroup;

            _pingUi = pingUi;
            
            CreateSettingsUI();
        }

        private void CreateSettingsUI() {
            _settingsGroup.SetActive(false);

            var x = 1920f - 210f;
            var y = 1080f - 75f;
            
            CreateTeamSelectionUI(x, ref y);
            
            CreateSkinSelectionUI(x, ref y);

            CreatePingUiToggle(x, ref y);
            
            new ButtonComponent(
                _settingsGroup,
                new Vector2(x, y),
                "Back"
            ).SetOnPress(() => {
                _settingsGroup.SetActive(false);
                _connectGroup.SetActive(true);
            });
        }

        private void CreateTeamSelectionUI(float x, ref float y) {
            new TextComponent(
                _settingsGroup,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Team Selection",
                FontManager.UIFontRegular,
                18
            );

            y -= 35;

            var radioButtonBox = new RadioButtonBoxComponent(
                _settingsGroup,
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
            radioButtonBox.SetInteractable(false);

            y -= 200;

            // If we connect, make the radio button box interactable
            _clientManager.RegisterOnConnect(() => radioButtonBox.SetInteractable(true));
            
            // If we disconnect, we reset it and make it non-interactable
            _clientManager.RegisterOnDisconnect(() => {
                radioButtonBox.SetInteractable(false);
                radioButtonBox.Reset();
            });
            
            _clientManager.RegisterTeamSettingChange(() => {
                if (_clientGameSettings.TeamsEnabled) {
                    // If the team settings becomes enabled, we make it interactable again
                    radioButtonBox.SetInteractable(true);   
                } else {
                    // If the team settings becomes disabled, we reset it and make it non-interactable
                    radioButtonBox.SetInteractable(false);
                    radioButtonBox.Reset();
                }
            });

            radioButtonBox.SetOnChange(value => {
                if (!_clientGameSettings.TeamsEnabled) {
                    return;
                }
                
                _clientManager.ChangeTeam((Team) value);
            });
        }

        private void CreateSkinSelectionUI(float x, ref float y) {
            var skinSetting = new SettingsUIEntry(
                _settingsGroup,
                new Vector2(x, y),
                "Player skin ID",
                typeof(byte),
                0,
                0,
                o => {
                    _clientManager.ChangeSkin((byte) o);
                }
            );

            y -= 100;

            new ButtonComponent(
                _settingsGroup,
                new Vector2(x, y),
                "Apply skin"
            ).SetOnPress(skinSetting.ApplySetting);

            y -= 40;
        }

        private void CreatePingUiToggle(float x, ref float y) {
            new SettingsUIEntry(
                _settingsGroup,
                new Vector2(x, y),
                "Display ping",
                typeof(bool),
                false,
                _modSettings.DisplayPing,
                o => {
                    var newValue = (bool) o;
                    _modSettings.DisplayPing = newValue;
                    
                    _pingUi.SetEnabled(newValue);
                },
                autoApply: true
            );

            y -= 75;
        }
    }
}