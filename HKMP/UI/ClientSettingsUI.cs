using HKMP.Game;
using HKMP.Game.Client;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using UnityEngine;

namespace HKMP.UI {
    public class ClientSettingsUI {
        private readonly Game.Settings.GameSettings _clientGameSettings;
        private readonly ClientManager _clientManager;

        private readonly GameObject _settingsUiObject;
        private readonly GameObject _connectUiObject;

        public ClientSettingsUI(
            Game.Settings.GameSettings clientGameSettings,
            ClientManager clientManager,
            GameObject settingsUiObject, 
            GameObject connectUiObject
        ) {
            _clientManager = clientManager;
            _clientGameSettings = clientGameSettings;
            
            _settingsUiObject = settingsUiObject;
            _connectUiObject = connectUiObject;
            
            CreateSettingsUI();
        }

        private void CreateSettingsUI() {
            _settingsUiObject.SetActive(false);

            var x = Screen.width - 210f;
            var y = Screen.height - 75f;
            
            CreateTeamSelectionUI(x, ref y);
            
            CreateSkinSelectionUI(x, ref y);
            
            new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Back"
            ).SetOnPress(() => {
                _settingsUiObject.SetActive(false);
                _connectUiObject.SetActive(true);
            });
        }

        private void CreateTeamSelectionUI(float x, ref float y) {
            new TextComponent(
                _settingsUiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Team Selection",
                FontManager.UIFontRegular,
                18
            );

            y -= 35;

            var radioButtonBox = new RadioButtonBoxComponent(
                _settingsUiObject,
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
                _settingsUiObject,
                new Vector2(x, y),
                "Player skin ID",
                typeof(int),
                0,
                0,
                o => {
                    var value = (int) o;
                    if (value >= 0 && value < 256) {
                        _clientManager.ChangeSkin((byte) value);
                    } else {
                        // TODO: somehow notify the user that this is an invalid ID
                    }
                }
            );

            y -= 100;

            new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Apply skin"
            ).SetOnPress(skinSetting.ApplySetting);

            y -= 40;
        }
    }
}