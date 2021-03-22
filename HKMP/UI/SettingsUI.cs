using System.Collections.Generic;
using HKMP.Game.Server;
using HKMP.Game.Settings;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using UnityEngine;

namespace HKMP.UI {
    public class SettingsUI {
        private readonly Game.Settings.GameSettings _gameSettings;
        private readonly ModSettings _modSettings;
        private readonly ServerManager _serverManager;
        
        private readonly GameObject _settingsUiObject;
        private readonly GameObject _connectUiObject;

        private SettingsEntry[] _settingsEntries;

        private int _currentPage = 1;

        public SettingsUI(
            Game.Settings.GameSettings gameSettings, 
            ModSettings modSettings, 
            ServerManager serverManager, 
            GameObject settingsUiObject, 
            GameObject connectUiObject
        ) {
            _gameSettings = gameSettings;
            _modSettings = modSettings;
            _serverManager = serverManager;
            _settingsUiObject = settingsUiObject;
            _connectUiObject = connectUiObject;
            
            CreateSettings();
            CreateSettingsUI();
        }

        private void CreateSettingsUI() {
            _settingsUiObject.SetActive(false);

            const float pageYLimit = 250;
            
            var x = Screen.width - 210.0f;
            var y = pageYLimit;

            const int boolMargin = 75;
            const int doubleBoolMargin = 100;
            const int intMargin = 100;
            const int doubleIntMargin = 125;

            var settingsUIEntries = new List<SettingsUIEntry>();
            var pages = new Dictionary<int, GameObject>();

            var currentPage = 0;
            GameObject currentPageObject = null;

            foreach (var settingsEntry in _settingsEntries) {
                if (y <= pageYLimit) {
                    currentPage++;
                    currentPageObject = new GameObject($"Settings Page {currentPage}");
                    currentPageObject.SetActive(currentPage == 1);
                    currentPageObject.transform.SetParent(_settingsUiObject.transform);
                    
                    pages.Add(currentPage, currentPageObject);

                    y = Screen.height - 50.0f;
                }
                
                var nameChars = settingsEntry.Name.ToCharArray();
                var font = FontManager.UIFontRegular;

                var nameWidth = 0;
                foreach (var nameChar in nameChars) {
                    font.GetCharacterInfo(nameChar, out var characterInfo, 18);
                    nameWidth += characterInfo.advance;
                }

                var doubleLine = nameWidth >= SettingsUIEntry.TextWidth;

                settingsUIEntries.Add(new SettingsUIEntry(
                    currentPageObject,
                    new Vector2(x, y),
                    settingsEntry.Name,
                    settingsEntry.Type,
                    settingsEntry.DefaultValue,
                    settingsEntry.InitialValue,
                    settingsEntry.ApplySetting,
                    doubleLine
                ));

                if (doubleLine) {
                    y -= settingsEntry.Type == typeof(bool) ? doubleBoolMargin : doubleIntMargin;
                } else {
                    y -= settingsEntry.Type == typeof(bool) ? boolMargin : intMargin;
                }
            }

            y = pageYLimit - 80;

            var nextPageButton = new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Next page"
            );
            nextPageButton.SetOnPress(() => {
                // Disable old current page
                pages[_currentPage].SetActive(false);
                
                // Increment page if we can
                if (_currentPage < pages.Count) {
                    _currentPage++;
                }

                // Enable new current page
                pages[_currentPage].SetActive(true);
            });

            y -= 40;
            
            var previousPageButton = new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Previous page"
            );
            previousPageButton.SetOnPress(() => {
                // Disable old current page
                pages[_currentPage].SetActive(false);
                
                // Decrement page if we can
                if (_currentPage > 1) {
                    _currentPage--;
                }

                // Enable new current page
                pages[_currentPage].SetActive(true);
            });

            y -= 40;

            var saveSettingsButton = new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Save settings"
            );
            saveSettingsButton.SetOnPress(() => {
                // TODO: check if there are actually changes, otherwise this button will
                // bombard clients with packets
                foreach (var settingsUIEntry in settingsUIEntries) {
                    settingsUIEntry.ApplySetting();
                }

                _modSettings.GameSettings = _gameSettings;

                _serverManager.OnUpdateGameSettings();
            });

            y -= 40;

            new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Back"
            ).SetOnPress(() => {
                _settingsUiObject.SetActive(false);
                _connectUiObject.SetActive(true);
            });
        }

        private void CreateSettings() {
            _settingsEntries = new[] {
                new SettingsEntry(
                    "Is PvP Enabled", 
                    typeof(bool),
                    false,
                    _gameSettings.IsPvpEnabled, 
                    o => _gameSettings.IsPvpEnabled = (bool) o
                ),
                new SettingsEntry(
                    "Is body damage enabled", 
                    typeof(bool), 
                    true,
                    _gameSettings.IsBodyDamageEnabled, 
                    o => _gameSettings.IsBodyDamageEnabled = (bool) o
                ),
                new SettingsEntry(
                    "Always show map locations", 
                    typeof(bool), 
                    false,
                    _gameSettings.AlwaysShowMapIcons, 
                    o => _gameSettings.AlwaysShowMapIcons = (bool) o
                ),
                new SettingsEntry(
                    "Only broadcast map with Wayward Compass", 
                    typeof(bool), 
                    true,
                    _gameSettings.OnlyBroadcastMapIconWithWaywardCompass, 
                    o => _gameSettings.OnlyBroadcastMapIconWithWaywardCompass = (bool) o
                ),
                new SettingsEntry(
                    "Display names above players",
                    typeof(bool),
                    true,
                    _gameSettings.DisplayNames,
                    o => _gameSettings.DisplayNames = (bool) o
                ),
                new SettingsEntry(
                    "Nail damage",
                    typeof(int),
                    1,
                    _gameSettings.NailDamage,
                    o => _gameSettings.NailDamage = (int) o
                ),
                new SettingsEntry(
                    "Grubberfly's Elegy beam damage",
                    typeof(int),
                    1,
                    _gameSettings.GrubberyFlyElegyDamage,
                    o => _gameSettings.GrubberyFlyElegyDamage = (int) o
                ),
                new SettingsEntry(
                    "Vengeful Spirit damage",
                    typeof(int),
                    1,
                    _gameSettings.VengefulSpiritDamage,
                    o => _gameSettings.VengefulSpiritDamage = (int) o
                ),
                new SettingsEntry(
                    "Shade Soul damage",
                    typeof(int),
                    2,
                    _gameSettings.ShadeSoulDamage,
                    o => _gameSettings.ShadeSoulDamage = (int) o
                ),
                new SettingsEntry(
                    "Desolate Dive damage",
                    typeof(int),
                    1,
                    _gameSettings.DesolateDiveDamage,
                    o => _gameSettings.DesolateDiveDamage = (int) o
                ),
                new SettingsEntry(
                    "Descending Dark damage",
                    typeof(int),
                    2,
                    _gameSettings.DescendingDarkDamage,
                    o => _gameSettings.DescendingDarkDamage = (int) o
                ),
                new SettingsEntry(
                    "Howling Wraiths damage",
                    typeof(int),
                    1,
                    _gameSettings.HowlingWraithDamage,
                    o => _gameSettings.HowlingWraithDamage = (int) o
                ),
                new SettingsEntry(
                    "Abyss Shriek damage",
                    typeof(int),
                    2,
                    _gameSettings.AbyssShriekDamage,
                    o => _gameSettings.AbyssShriekDamage = (int) o
                ),
                new SettingsEntry(
                    "Great Slash damage",
                    typeof(int),
                    2,
                    _gameSettings.GreatSlashDamage,
                    o => _gameSettings.GreatSlashDamage = (int) o
                ),
                new SettingsEntry(
                    "Dash Slash damage",
                    typeof(int),
                    2,
                    _gameSettings.DashSlashDamage,
                    o => _gameSettings.DashSlashDamage = (int) o
                ),
                new SettingsEntry(
                    "Cyclone Slash damage",
                    typeof(int),
                    1,
                    _gameSettings.CycloneSlashDamage,
                    o => _gameSettings.CycloneSlashDamage = (int) o
                ),
                new SettingsEntry(
                    "Spore Shroom cloud damage",
                    typeof(int),
                    1,
                    _gameSettings.SporeShroomDamage,
                    o => _gameSettings.SporeShroomDamage = (int) o
                ),
                new SettingsEntry(
                    "Spore Dung Shroom cloud damage",
                    typeof(int),
                    1,
                    _gameSettings.SporeDungShroomDamage,
                    o => _gameSettings.SporeDungShroomDamage = (int) o
                ),
                new SettingsEntry(
                    "Thorns of Agony damage",
                    typeof(int),
                    1,
                    _gameSettings.ThornOfAgonyDamage,
                    o => _gameSettings.ThornOfAgonyDamage = (int) o
                ),
            };
        }
    }
}