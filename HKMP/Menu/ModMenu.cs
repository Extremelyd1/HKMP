using System;
using System.Collections;
using System.Reflection;
using Hkmp.Game.Client;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Util;
using Modding;
using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Menu;

/// <summary>
/// Static class for building the HKMP mod menu.
/// </summary>
internal static class ModMenu {
    /// <summary>
    /// The menu containing the client settings. Needs to be a static variable here to allow it to be accessed by
    /// lambdas and modified.
    /// </summary>
    private static MenuScreen _clientSettingsMenu;

    /// <summary>
    /// The menu containing the server settings. Needs to be a static variable here to allow it to be accessed by
    /// lambdas and modified.
    /// </summary>
    private static MenuScreen _serverSettingsMenu;

    /// <summary>
    /// Create the mod menu for HKMP. This consists of client-side settings (such as HUD elements and keybinds) and
    /// server settings.
    /// </summary>
    /// <param name="modListMenu">The menu screen for the mod list. Used as reference to return to when a back button
    /// is pressed for example.</param>
    /// <param name="modSettings">The mod settings for HKMP.</param>
    /// <param name="clientManager">The client manager to register a callback for when server settings are updated.
    /// </param>
    /// <param name="serverManager">The server manager to get the initial server settings and update them if we are
    /// not connected to a server.</param>
    /// <param name="netClient">The net client to network changes to the server settings if we are connected to a
    /// server.</param>
    /// <returns>The built HKMP menu screen.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the menu could not be created due to missing
    /// implementation for a type in the server settings.</exception>
    public static MenuScreen CreateMenu(
        MenuScreen modListMenu, 
        ModSettings modSettings, 
        ClientManager clientManager,
        ServerManager serverManager,
        NetClient netClient
    ) {
        var builder = MenuUtils.CreateMenuBuilderWithBackButton("HKMP", modListMenu, out _);

        builder.AddContent(
            RegularGridLayout.CreateVerticalLayout(150f),
            c => {
                c.AddMenuButton("Client Settings", new MenuButtonConfig {
                    Label = "Client Settings",
                    SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(_clientSettingsMenu),
                    Proceed = true,
                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                    Description = new DescriptionInfo {
                        Text = "Menu for changing the settings of the client"
                    }
                });

                c.AddMenuButton("Server Settings", new MenuButtonConfig {
                    Label = "Server Settings",
                    SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(_serverSettingsMenu),
                    Proceed = true,
                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                    Description = new DescriptionInfo {
                        Text = "Menu for changing the settings of the server for authorized players"
                    }
                });
            }
        );

        var hkmpMenu = builder.Build();
        
        builder = MenuUtils.CreateMenuBuilderWithBackButton("HKMP Client Settings", hkmpMenu, out _);

        builder.AddContent(
            RegularGridLayout.CreateVerticalLayout(150f),
            c => {
                MenuUtils.AddModMenuContent(
                    [
                        new IMenuMod.MenuEntry {
                            Name = "Full Synchronisation",
                            Description = "Synchronise enemies, bosses, world changes, and saves in multiplayer games",
                            Values = ["Off", "On"],
                            Saver = index => modSettings.FullSynchronisation = index == 1,
                            Loader = () => modSettings.FullSynchronisation ? 1 : 0
                        },
                        new IMenuMod.MenuEntry {
                            Name = "Ping Display",
                            Description = "HUD element that shows the player's ping in multiplayer games",
                            Values = ["Off", "On"],
                            Saver = index => modSettings.DisplayPing = index == 1,
                            Loader = () => modSettings.DisplayPing ? 1 : 0
                        }
                    ],
                    c,
                    modListMenu
                );

                c.AddKeybind(
                    "OpenChatKeybind",
                    modSettings.Keybinds.OpenChat,
                    new KeybindConfig {
                        Label = "Key to open the chat",
                        CancelAction = _ => { }
                    }
                );
            }
        );

        _clientSettingsMenu = builder.Build();

        // We keep track of an instance of server settings specifically for this mod menu
        // This will initially be a copy of the server manager settings, which come from the mod settings
        var serverSettings = serverManager.InternalServerSettings.GetCopy();
        Coroutine currentDelayedApplyRoutine = null;

        var serverSettingsProps = typeof(ServerSettings).GetProperties();

        builder = MenuUtils.CreateMenuBuilderWithBackButton("HKMP Server Settings", hkmpMenu, out var serverSettingsBackButton);

        builder.AddContent(new NullContentLayout(), nullContentArea => {
            nullContentArea.AddScrollPaneContent(
                new ScrollbarConfig {
                    Navigation = new Navigation {
                        mode = Navigation.Mode.Explicit,
                        selectOnUp = serverSettingsBackButton,
                        selectOnDown = serverSettingsBackButton
                    },
                    Position = new AnchoredPosition {
                        ChildAnchor = new Vector2(0.0f, 1f),
                        ParentAnchor = new Vector2(1f, 1f),
                        Offset = new Vector2(-310f, 0.0f)
                    },
                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(hkmpMenu)
                },
                new RelLength(serverSettingsProps.Length * 150f),
                RegularGridLayout.CreateVerticalLayout(150f),
                scrollGridContentArea => {
                    foreach (var propInfo in serverSettingsProps) {
                        var name = propInfo.Name;
                        var type = propInfo.PropertyType;

                        // TODO: document
                        string[] options;
                        Action<int> saver;
                        Func<int> loader;
                        Action<ServerSettings, MenuOptionHorizontal> serverSettingsChangedAction;

                        if (type == typeof(bool)) {
                            options = ["Off", "On"];

                            saver = i => {
                                ReflectionHelper.SetProperty(serverSettings, name, i == 1);
                                UpdateServerSettings();
                            };
                            loader = () => ReflectionHelper.GetProperty<ServerSettings, bool>(serverSettings, name)
                                ? 1
                                : 0;

                            serverSettingsChangedAction = (newSettings, horizontalOptionToChange) => {
                                var oldValue = ReflectionHelper.GetProperty<ServerSettings, bool>(serverSettings, name);
                                var newValue = ReflectionHelper.GetProperty<ServerSettings, bool>(newSettings, name);

                                if (oldValue != newValue) {
                                    horizontalOptionToChange.SetOptionTo(newValue ? 1 : 0);
                                    ReflectionHelper.SetProperty(serverSettings, name, newValue);
                                }
                            };
                        } else if (type == typeof(byte) && name.EndsWith("Damage")) {
                            // If the field is for the amount of damage for something, we fill the values with 0 through 20
                            options = new string[21];
                            for (var i = 0; i <= 20; i++) {
                                options[i] = i.ToString();
                            }

                            saver = i => {
                                ReflectionHelper.SetProperty(serverSettings, name, (byte) i);
                                UpdateServerSettings();
                            };
                            loader = () => ReflectionHelper.GetProperty<ServerSettings, byte>(serverSettings, name);

                            serverSettingsChangedAction = (newSettings, horizontalOptionToChange) => {
                                var oldValue = ReflectionHelper.GetProperty<ServerSettings, byte>(serverSettings, name);
                                var newValue = ReflectionHelper.GetProperty<ServerSettings, byte>(newSettings, name);

                                if (oldValue != newValue) {
                                    horizontalOptionToChange.SetOptionTo(newValue);
                                    ReflectionHelper.SetProperty(serverSettings, name, newValue);
                                }
                            };
                        } else {
                            throw new InvalidOperationException(
                                $"Could not make menu entry for unknown field type: {type}, for field: {name}");
                        }

                        // Try to obtain the label of the setting using the menu name attribute with fallback to the
                        // property name
                        var label = name;
                        var nameAttribute = propInfo.GetCustomAttribute<MenuNameAttribute>();
                        if (nameAttribute != null) {
                            label = nameAttribute.Name;
                        }

                        // Try to obtain the description of the setting using the menu description attribute
                        DescriptionInfo? descriptionInfo = null;
                        var descriptionAttribute = propInfo.GetCustomAttribute<MenuDescriptionAttribute>();
                        if (descriptionAttribute != null) {
                            descriptionInfo = new DescriptionInfo {
                                Text = descriptionAttribute.Description
                            };
                        }

                        scrollGridContentArea.AddHorizontalOption(
                            name,
                            new HorizontalOptionConfig {
                                Options = options,
                                Label = label,
                                ApplySetting = (_, i) => saver.Invoke(i),
                                RefreshSetting = (s, _) => s.optionList.SetOptionTo(loader.Invoke()),
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(hkmpMenu),
                                Description = descriptionInfo
                            },
                            out var horizontalOption
                        );
                        horizontalOption.menuSetting.RefreshValueFromGameSettings();

                        clientManager.ServerSettingsChangedEvent += newSettings =>
                            serverSettingsChangedAction.Invoke(newSettings, horizontalOption);
                    }
                }
            );
        });

        _serverSettingsMenu = builder.Build();
        
        return hkmpMenu;

        void UpdateServerSettings() {
            if (!netClient.IsConnected) {
                // If the client is not connected to any server, we simply update the server settings of the 
                // server manager so that when we host a server, it will have the same settings as in this menu
                serverManager.InternalServerSettings.SetAllProperties(serverSettings);
                return;
            }
            
            if (currentDelayedApplyRoutine != null) {
                MonoBehaviourUtil.Instance.StopCoroutine(currentDelayedApplyRoutine);
            }

            currentDelayedApplyRoutine = MonoBehaviourUtil.Instance.StartCoroutine(RunActionWithDelay(
                () => {
                    netClient.UpdateManager.SetServerSettingsUpdate(serverSettings);
                },
                1.5f
            ));
        }
    }

    /// <summary>
    /// Run the given action with the given delay.
    /// </summary>
    /// <param name="action">The action to invoke after the delay.</param>
    /// <param name="delay">The delay in seconds.</param>
    /// <returns>The coroutine of the delayed invocation.</returns>
    private static IEnumerator RunActionWithDelay(Action action, float delay) {
        yield return new WaitForSeconds(delay);
        
        action.Invoke();
    }
}
