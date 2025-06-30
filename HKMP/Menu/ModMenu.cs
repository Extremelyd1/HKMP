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
/// Class for building the HKMP mod menu.
/// </summary>
internal class ModMenu {
    /// <summary>
    /// The menu screen for the game's mod list created by the Modding API.
    /// </summary>
    private readonly MenuScreen _modListMenu;

    /// <summary>
    /// The HKMP mod settings instance.
    /// </summary>
    private readonly ModSettings _modSettings;
    
    /// <summary>
    /// The client manager instance.
    /// </summary>
    private readonly ClientManager _clientManager;

    /// <summary>
    /// The server manager instance.
    /// </summary>
    private readonly ServerManager _serverManager;

    /// <summary>
    /// The net client instance.
    /// </summary>
    private readonly NetClient _netClient;
    
    /// <summary>
    /// The top-level HKMP mod menu.
    /// </summary>
    private MenuScreen _hkmpMenu;
    /// <summary>
    /// The menu containing the client settings. Needs to be a static variable here to allow it to be accessed by
    /// lambdas and modified.
    /// </summary>
    private MenuScreen _clientSettingsMenu;

    /// <summary>
    /// The menu containing the server settings. Needs to be a static variable here to allow it to be accessed by
    /// lambdas and modified.
    /// </summary>
    private MenuScreen _serverSettingsMenu;

    /// <summary>
    /// A local copy of the server settings for modification through the menu that will be used to either network to
    /// the server or modify our own hosted servers.
    /// </summary>
    private ServerSettings _localServerSettings;

    /// <summary>
    /// Coroutine that delayed applying new server settings until no more changes are made within a certain time
    /// period.
    /// </summary>
    private Coroutine _currentDelayedApplyRoutine;

    /// <summary>
    /// Constructs the mod menu class for HKMP.
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
    public ModMenu(
        MenuScreen modListMenu, 
        ModSettings modSettings, 
        ClientManager clientManager, 
        ServerManager serverManager, 
        NetClient netClient
    ) {
        _modListMenu = modListMenu;
        _modSettings = modSettings;
        _clientManager = clientManager;
        _serverManager = serverManager;
        _netClient = netClient;
    }

    /// <summary>
    /// Create the mod menu for HKMP. This consists of client-side settings (such as HUD elements and keybinds) and
    /// server settings.
    /// </summary>
    /// <returns>The built HKMP menu screen.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the menu could not be created due to missing
    /// implementation for a type in the server settings.</exception>
    public MenuScreen CreateMenu() {
        var builder = MenuUtils.CreateMenuBuilderWithBackButton("HKMP", _modListMenu, out _);

        builder.AddContent(
            RegularGridLayout.CreateVerticalLayout(150f),
            c => {
                c.AddMenuButton("Client Settings", new MenuButtonConfig {
                    Label = "Client Settings",
                    SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(_clientSettingsMenu),
                    Proceed = true,
                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(_modListMenu),
                    Description = new DescriptionInfo {
                        Text = "Menu for changing the settings of the client"
                    }
                });

                c.AddMenuButton("Server Settings", new MenuButtonConfig {
                    Label = "Server Settings",
                    SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(_serverSettingsMenu),
                    Proceed = true,
                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(_modListMenu),
                    Description = new DescriptionInfo {
                        Text = "Menu for changing the settings of the server for authorized players"
                    }
                });
            }
        );

        _hkmpMenu = builder.Build();

        _clientSettingsMenu = CreateClientSettingsMenu();
        _serverSettingsMenu = CreateServerSettingsMenu();

        return _hkmpMenu;
    }

    /// <summary>
    /// Create the client settings menu.
    /// </summary>
    /// <returns>A <see cref="MenuScreen"/> for the client settings menu.</returns>
    private MenuScreen CreateClientSettingsMenu() {
        var builder = MenuUtils.CreateMenuBuilderWithBackButton("HKMP Client Settings", _hkmpMenu, out _);

        builder.AddContent(
            RegularGridLayout.CreateVerticalLayout(150f),
            c => {
                MenuUtils.AddModMenuContent(
                    [
                        new IMenuMod.MenuEntry {
                            Name = "Full Synchronisation",
                            Description = "Synchronise enemies, bosses, world changes, and saves in multiplayer games",
                            Values = ["Off", "On"],
                            Saver = index => _modSettings.FullSynchronisation = index == 1,
                            Loader = () => _modSettings.FullSynchronisation ? 1 : 0
                        },
                        new IMenuMod.MenuEntry {
                            Name = "Ping Display",
                            Description = "HUD element that shows the player's ping in multiplayer games",
                            Values = ["Off", "On"],
                            Saver = index => _modSettings.DisplayPing = index == 1,
                            Loader = () => _modSettings.DisplayPing ? 1 : 0
                        }
                    ],
                    c,
                    _hkmpMenu
                );

                c.AddKeybind(
                    "OpenChatKeybind",
                    _modSettings.Keybinds.OpenChat,
                    new KeybindConfig {
                        Label = "Key to open the chat",
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(_hkmpMenu)
                    }
                );
            }
        );

        return builder.Build();
    }

    /// <summary>
    /// Create the server settings menu.
    /// </summary>
    /// <returns>A <see cref="MenuScreen"/> for the server settings menu.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the menu could not be created due to missing
    /// implementation for a type in the server settings.</exception>
    private MenuScreen CreateServerSettingsMenu() {
        // We keep track of an instance of server settings specifically for this mod menu
        // This will initially be a copy of the server manager settings, which come from the mod settings
        _localServerSettings = _serverManager.InternalServerSettings.GetCopy();

        var serverSettingsProps = typeof(ServerSettings).GetProperties();

        var builder = MenuUtils.CreateMenuBuilderWithBackButton(
            "HKMP Server Settings", 
            _hkmpMenu, 
            out var serverSettingsBackButton
        );

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
                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(_hkmpMenu)
                },
                new RelLength(serverSettingsProps.Length * 150f),
                RegularGridLayout.CreateVerticalLayout(150f),
                scrollGridContentArea => {
                    foreach (var propInfo in serverSettingsProps) {
                        var name = propInfo.Name;
                        var type = propInfo.PropertyType;

                        // Define variables for the eventual creation of an individual setting that depend on the
                        // type of the setting (for example, the options will be different for a byte than for a bool)
                        string[] options;
                        Action<int> saver;
                        Func<int> loader;
                        // This action will be invoked whenever the server settings are changed externally, so outside
                        // the mod menu
                        Action<ServerSettings, MenuOptionHorizontal> serverSettingsChangedAction;

                        if (type == typeof(bool)) {
                            options = ["Off", "On"];

                            saver = i => {
                                ReflectionHelper.SetProperty(_localServerSettings, name, i == 1);
                                HandleUpdateServerSettings();
                            };
                            loader = () => ReflectionHelper.GetProperty<ServerSettings, bool>(_localServerSettings, name)
                                ? 1
                                : 0;

                            serverSettingsChangedAction = (newSettings, horizontalOptionToChange) => {
                                // Get the old and new values and check whether there is a change
                                var oldValue = ReflectionHelper.GetProperty<ServerSettings, bool>(_localServerSettings, name);
                                var newValue = ReflectionHelper.GetProperty<ServerSettings, bool>(newSettings, name);

                                if (oldValue != newValue) {
                                    // Set the mod menu option and update our local server settings instance
                                    horizontalOptionToChange.SetOptionTo(newValue ? 1 : 0);
                                    ReflectionHelper.SetProperty(_localServerSettings, name, newValue);
                                }
                            };
                        } else if (type == typeof(byte) && name.EndsWith("Damage")) {
                            // If the field is for the amount of damage for something, we fill the values with 0 through 20
                            options = new string[21];
                            for (var i = 0; i <= 20; i++) {
                                options[i] = i.ToString();
                            }

                            saver = i => {
                                ReflectionHelper.SetProperty(_localServerSettings, name, (byte) i);
                                HandleUpdateServerSettings();
                            };
                            loader = () => ReflectionHelper.GetProperty<ServerSettings, byte>(_localServerSettings, name);

                            serverSettingsChangedAction = (newSettings, horizontalOptionToChange) => {
                                // Get the old and new values and check whether there is a change
                                var oldValue = ReflectionHelper.GetProperty<ServerSettings, byte>(_localServerSettings, name);
                                var newValue = ReflectionHelper.GetProperty<ServerSettings, byte>(newSettings, name);

                                if (oldValue != newValue) {
                                    // Set the mod menu option and update our local server settings instance
                                    horizontalOptionToChange.SetOptionTo(newValue);
                                    ReflectionHelper.SetProperty(_localServerSettings, name, newValue);
                                }
                            };
                        } else {
                            throw new InvalidOperationException(
                                $"Could not make menu entry for unknown field type: {type}, for field: {name}");
                        }

                        // Try to obtain the label and description of the setting using the mod menu attribute
                        var label = name;
                        DescriptionInfo? descriptionInfo = null;
                        var menuSettingAttr = propInfo.GetCustomAttribute<ModMenuSettingAttribute>();
                        if (menuSettingAttr != null) {
                            label = menuSettingAttr.Name;
                            if (menuSettingAttr.Description != null) {
                                descriptionInfo = new DescriptionInfo {
                                    Text = menuSettingAttr.Description
                                };
                            }
                        }

                        scrollGridContentArea.AddHorizontalOption(
                            name,
                            new HorizontalOptionConfig {
                                Options = options,
                                Label = label,
                                ApplySetting = (_, i) => saver.Invoke(i),
                                RefreshSetting = (s, _) => s.optionList.SetOptionTo(loader.Invoke()),
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(_hkmpMenu),
                                Description = descriptionInfo
                            },
                            out var horizontalOption
                        );
                        horizontalOption.menuSetting.RefreshValueFromGameSettings();

                        _clientManager.ServerSettingsChangedEvent += newSettings =>
                            serverSettingsChangedAction.Invoke(newSettings, horizontalOption);
                    }
                }
            );
        });

        return builder.Build();
    }

    /// <summary>
    /// Handles updates to the server settings. Checks whether we are connected to a server and only sends updates
    /// if no changes were made to the local server settings in this class for a certain period of time to prevent
    /// sending unnecessary updates.
    /// </summary>
    private void HandleUpdateServerSettings() {
        if (!_netClient.IsConnected) {
            // If the client is not connected to any server, we simply update the server settings of the 
            // server manager so that when we host a server, it will have the same settings as in this menu
            _serverManager.InternalServerSettings.SetAllProperties(_localServerSettings);
            return;
        }
            
        // We check whether there is already a coroutine present and stop it if so
        // This will make it so that if we rapidly change settings, we only apply them after a set time has passed
        // without any modifications
        // Otherwise, we would be sending a server settings update to the server more often than necessary
        if (_currentDelayedApplyRoutine != null) {
            MonoBehaviourUtil.Instance.StopCoroutine(_currentDelayedApplyRoutine);
        }

        _currentDelayedApplyRoutine = MonoBehaviourUtil.Instance.StartCoroutine(RunActionWithDelay(
            () => {
                _netClient.UpdateManager.SetServerSettingsUpdate(_localServerSettings);
            },
            1.5f
        ));
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
