using System;
using System.Collections.Generic;
using Hkmp.Game.Settings;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using UnityEngine;

namespace Hkmp.Ui {
    public class ServerSettingsInterface {
        public event Action OnGameSettingsChange;
        
        public ServerSettingsInterface(
            Game.Settings.GameSettings gameSettings,
            ModSettings modSettings,
            ComponentGroup settingsGroup,
            ComponentGroup connectGroup
        ) {
            var settingsEntries = CreateSettings(gameSettings);
            CreateSettingsUI(
                settingsEntries, 
                gameSettings, 
                modSettings, 
                settingsGroup, 
                connectGroup
            );
        }

        private void CreateSettingsUI(
            SettingsEntry[] settingsEntries,
            Game.Settings.GameSettings gameSettings,
            ModSettings modSettings,
            ComponentGroup settingsGroup,
            ComponentGroup connectGroup
        ) {
            settingsGroup.SetActive(false);

            const float pageYLimit = 250;

            var x = 1920f - 210.0f;
            var y = pageYLimit;

            const int boolMargin = 75;
            const int doubleBoolMargin = 100;
            const int intMargin = 100;
            const int doubleIntMargin = 125;

            var settingsUIEntries = new List<SettingsEntryInterface>();
            var pages = new Dictionary<int, ComponentGroup>();

            var currentPage = 0;
            ComponentGroup currentPageGroup = null;

            foreach (var settingsEntry in settingsEntries) {
                if (y <= pageYLimit) {
                    currentPage++;

                    currentPageGroup = new ComponentGroup(currentPage == 1, settingsGroup);

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

                var doubleLine = nameWidth >= SettingsEntryInterface.TextWidth;

                settingsUIEntries.Add(new SettingsEntryInterface(
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
                settingsGroup,
                new Vector2(x, y),
                "Next page"
            );
            nextPageButton.SetOnPress(() => {
                // Disable old current page
                pages[currentPage].SetActive(false);

                // Increment page if we can
                if (currentPage < pages.Count) {
                    currentPage++;
                }

                // Enable new current page
                pages[currentPage].SetActive(true);
            });

            y -= 40;

            var previousPageButton = new ButtonComponent(
                settingsGroup,
                new Vector2(x, y),
                "Previous page"
            );
            previousPageButton.SetOnPress(() => {
                // Disable old current page
                pages[currentPage].SetActive(false);

                // Decrement page if we can
                if (currentPage > 1) {
                    currentPage--;
                }

                // Enable new current page
                pages[currentPage].SetActive(true);
            });

            y -= 40;

            var saveSettingsButton = new ButtonComponent(
                settingsGroup,
                new Vector2(x, y),
                "Save settings"
            );
            saveSettingsButton.SetOnPress(() => {
                // TODO: check if there are actually changes, otherwise this button will
                // bombard clients with packets
                foreach (var settingsUIEntry in settingsUIEntries) {
                    settingsUIEntry.ApplySetting();
                }

                modSettings.GameSettings = gameSettings;

                OnGameSettingsChange?.Invoke();
            });

            y -= 40;

            new ButtonComponent(
                settingsGroup,
                new Vector2(x, y),
                "Back"
            ).SetOnPress(() => {
                settingsGroup.SetActive(false);
                connectGroup.SetActive(true);
            });
        }

        private SettingsEntry[] CreateSettings(Game.Settings.GameSettings gameSettings) {
            return new[] {
                new SettingsEntry(
                    "Is PvP Enabled",
                    typeof(bool),
                    false,
                    gameSettings.IsPvpEnabled,
                    o => gameSettings.IsPvpEnabled = (bool) o
                ),
                new SettingsEntry(
                    "Is body damage enabled",
                    typeof(bool),
                    true,
                    gameSettings.IsBodyDamageEnabled,
                    o => gameSettings.IsBodyDamageEnabled = (bool) o
                ),
                new SettingsEntry(
                    "Always show map locations",
                    typeof(bool),
                    false,
                    gameSettings.AlwaysShowMapIcons,
                    o => gameSettings.AlwaysShowMapIcons = (bool) o
                ),
                new SettingsEntry(
                    "Only broadcast map with Wayward Compass",
                    typeof(bool),
                    true,
                    gameSettings.OnlyBroadcastMapIconWithWaywardCompass,
                    o => gameSettings.OnlyBroadcastMapIconWithWaywardCompass = (bool) o
                ),
                new SettingsEntry(
                    "Display names above players",
                    typeof(bool),
                    true,
                    gameSettings.DisplayNames,
                    o => gameSettings.DisplayNames = (bool) o
                ),
                new SettingsEntry(
                    "Enable teams",
                    typeof(bool),
                    false,
                    gameSettings.TeamsEnabled,
                    o => gameSettings.TeamsEnabled = (bool) o
                ),
                new SettingsEntry(
                    "Allow skins",
                    typeof(bool),
                    true,
                    gameSettings.AllowSkins,
                    o => gameSettings.AllowSkins = (bool) o
                ),
                new SettingsEntry(
                    "Nail damage",
                    typeof(byte),
                    1,
                    gameSettings.NailDamage,
                    o => gameSettings.NailDamage = (byte) o
                ),
                new SettingsEntry(
                    "Grubberfly's Elegy beam damage",
                    typeof(byte),
                    1,
                    gameSettings.GrubberflyElegyDamage,
                    o => gameSettings.GrubberflyElegyDamage = (byte) o
                ),
                new SettingsEntry(
                    "Vengeful Spirit damage",
                    typeof(byte),
                    1,
                    gameSettings.VengefulSpiritDamage,
                    o => gameSettings.VengefulSpiritDamage = (byte) o
                ),
                new SettingsEntry(
                    "Shade Soul damage",
                    typeof(byte),
                    2,
                    gameSettings.ShadeSoulDamage,
                    o => gameSettings.ShadeSoulDamage = (byte) o
                ),
                new SettingsEntry(
                    "Desolate Dive damage",
                    typeof(byte),
                    1,
                    gameSettings.DesolateDiveDamage,
                    o => gameSettings.DesolateDiveDamage = (byte) o
                ),
                new SettingsEntry(
                    "Descending Dark damage",
                    typeof(byte),
                    2,
                    gameSettings.DescendingDarkDamage,
                    o => gameSettings.DescendingDarkDamage = (byte) o
                ),
                new SettingsEntry(
                    "Howling Wraiths damage",
                    typeof(byte),
                    1,
                    gameSettings.HowlingWraithDamage,
                    o => gameSettings.HowlingWraithDamage = (byte) o
                ),
                new SettingsEntry(
                    "Abyss Shriek damage",
                    typeof(byte),
                    2,
                    gameSettings.AbyssShriekDamage,
                    o => gameSettings.AbyssShriekDamage = (byte) o
                ),
                new SettingsEntry(
                    "Great Slash damage",
                    typeof(byte),
                    2,
                    gameSettings.GreatSlashDamage,
                    o => gameSettings.GreatSlashDamage = (byte) o
                ),
                new SettingsEntry(
                    "Dash Slash damage",
                    typeof(byte),
                    2,
                    gameSettings.DashSlashDamage,
                    o => gameSettings.DashSlashDamage = (byte) o
                ),
                new SettingsEntry(
                    "Cyclone Slash damage",
                    typeof(byte),
                    1,
                    gameSettings.CycloneSlashDamage,
                    o => gameSettings.CycloneSlashDamage = (byte) o
                ),
                new SettingsEntry(
                    "Spore Shroom cloud damage",
                    typeof(byte),
                    1,
                    gameSettings.SporeShroomDamage,
                    o => gameSettings.SporeShroomDamage = (byte) o
                ),
                new SettingsEntry(
                    "Spore Dung Shroom cloud damage",
                    typeof(byte),
                    1,
                    gameSettings.SporeDungShroomDamage,
                    o => gameSettings.SporeDungShroomDamage = (byte) o
                ),
                new SettingsEntry(
                    "Thorns of Agony damage",
                    typeof(byte),
                    1,
                    gameSettings.ThornOfAgonyDamage,
                    o => gameSettings.ThornOfAgonyDamage = (byte) o
                ),
            };
        }
    }
}