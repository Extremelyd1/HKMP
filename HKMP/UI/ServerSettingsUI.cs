using System.Collections.Generic;
using HKMP.Game.Settings;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Game.Server;
using UnityEngine;

namespace HKMP.UI {
    public class ServerSettingsUI {
        private readonly Game.Settings.GameSettings _gameSettings;
        private readonly ModSettings _modSettings;
        private readonly ServerManager _serverManager;
        
        private readonly UIGroup _settingsGroup;
        private readonly UIGroup _connectGroup;

        private SettingsEntry[] _settingsEntries;

        private int _currentPage = 1;

        public ServerSettingsUI(
            Game.Settings.GameSettings gameSettings, 
            ModSettings modSettings, 
            ServerManager serverManager, 
            UIGroup settingsGroup, 
            UIGroup connectGroup
        ) {
            _gameSettings = gameSettings;
            _modSettings = modSettings;
            _serverManager = serverManager;
            _settingsGroup = settingsGroup;
            _connectGroup = connectGroup;
            
            CreateSettings();
            CreateSettingsUI();
        }

        private void CreateSettingsUI() {
            _settingsGroup.SetActive(false);

            const float pageYLimit = 250;
            
            var x = 1920f - 210.0f;
            var y = pageYLimit;

            const int boolMargin = 75;
            const int doubleBoolMargin = 100;
            const int intMargin = 100;
            const int doubleIntMargin = 125;

            var settingsUIEntries = new List<SettingsUIEntry>();
            var pages = new Dictionary<int, UIGroup>();

            var currentPage = 0;
            UIGroup currentPageGroup = null;

            foreach (var settingsEntry in _settingsEntries) {
                if (y <= pageYLimit) {
                    currentPage++;

                    currentPageGroup = new UIGroup(currentPage == 1, _settingsGroup);
                    
                    pages.Add(currentPage, currentPageGroup);

                    y = 1080f - 75.0f;
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
                    currentPageGroup,
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
                _settingsGroup,
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
                _settingsGroup,
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
                _settingsGroup,
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
                _settingsGroup,
                new Vector2(x, y),
                "Back"
            ).SetOnPress(() => {
                _settingsGroup.SetActive(false);
                _connectGroup.SetActive(true);
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
                    "Enable teams",
                    typeof(bool),
                    false,
                    _gameSettings.TeamsEnabled,
                    o => _gameSettings.TeamsEnabled = (bool) o
                ),
                new SettingsEntry(
                    "Allow skins",
                    typeof(bool),
                    true,
                    _gameSettings.AllowSkins,
                    o => _gameSettings.AllowSkins = (bool) o
                ),
                new SettingsEntry(
                    "Nail damage",
                    typeof(byte),
                    1,
                    _gameSettings.NailDamage,
                    o => _gameSettings.NailDamage = (byte) o
                ),
                new SettingsEntry(
                    "Grubberfly's Elegy beam damage",
                    typeof(byte),
                    1,
                    _gameSettings.GrubberflyElegyDamage,
                    o => _gameSettings.GrubberflyElegyDamage = (byte) o
                ),
                new SettingsEntry(
                    "Vengeful Spirit damage",
                    typeof(byte),
                    1,
                    _gameSettings.VengefulSpiritDamage,
                    o => _gameSettings.VengefulSpiritDamage = (byte) o
                ),
                new SettingsEntry(
                    "Shade Soul damage",
                    typeof(byte),
                    2,
                    _gameSettings.ShadeSoulDamage,
                    o => _gameSettings.ShadeSoulDamage = (byte) o
                ),
                new SettingsEntry(
                    "Desolate Dive damage",
                    typeof(byte),
                    1,
                    _gameSettings.DesolateDiveDamage,
                    o => _gameSettings.DesolateDiveDamage = (byte) o
                ),
                new SettingsEntry(
                    "Descending Dark damage",
                    typeof(byte),
                    2,
                    _gameSettings.DescendingDarkDamage,
                    o => _gameSettings.DescendingDarkDamage = (byte) o
                ),
                new SettingsEntry(
                    "Howling Wraiths damage",
                    typeof(byte),
                    1,
                    _gameSettings.HowlingWraithDamage,
                    o => _gameSettings.HowlingWraithDamage = (byte) o
                ),
                new SettingsEntry(
                    "Abyss Shriek damage",
                    typeof(byte),
                    2,
                    _gameSettings.AbyssShriekDamage,
                    o => _gameSettings.AbyssShriekDamage = (byte) o
                ),
                new SettingsEntry(
                    "Great Slash damage",
                    typeof(byte),
                    2,
                    _gameSettings.GreatSlashDamage,
                    o => _gameSettings.GreatSlashDamage = (byte) o
                ),
                new SettingsEntry(
                    "Dash Slash damage",
                    typeof(byte),
                    2,
                    _gameSettings.DashSlashDamage,
                    o => _gameSettings.DashSlashDamage = (byte) o
                ),
                new SettingsEntry(
                    "Cyclone Slash damage",
                    typeof(byte),
                    1,
                    _gameSettings.CycloneSlashDamage,
                    o => _gameSettings.CycloneSlashDamage = (byte) o
                ),
                new SettingsEntry(
                    "Spore Shroom cloud damage",
                    typeof(byte),
                    1,
                    _gameSettings.SporeShroomDamage,
                    o => _gameSettings.SporeShroomDamage = (byte) o
                ),
                new SettingsEntry(
                    "Spore Dung Shroom cloud damage",
                    typeof(byte),
                    1,
                    _gameSettings.SporeDungShroomDamage,
                    o => _gameSettings.SporeDungShroomDamage = (byte) o
                ),
                new SettingsEntry(
                    "Thorns of Agony damage",
                    typeof(byte),
                    1,
                    _gameSettings.ThornOfAgonyDamage,
                    o => _gameSettings.ThornOfAgonyDamage = (byte) o
                ),
            };
        }
    }
}