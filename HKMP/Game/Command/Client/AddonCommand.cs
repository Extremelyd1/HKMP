using System;
using System.Linq;
using Hkmp.Api.Client;
using Hkmp.Api.Command.Client;
using Hkmp.Networking.Client;
using Hkmp.Ui;

namespace Hkmp.Game.Command.Client;

/// <summary>
/// Command for managing client-side addons, such as enabling and disabling them.
/// </summary>
internal class AddonCommand : IClientCommand {
    /// <inheritdoc />
    public string Trigger => "/addon";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <summary>
    /// The client addon manager instance.
    /// </summary>
    private readonly ClientAddonManager _addonManager;

    /// <summary>
    /// The net client instance.
    /// </summary>
    private readonly NetClient _netClient;

    public AddonCommand(ClientAddonManager addonManager, NetClient netClient) {
        _addonManager = addonManager;
        _netClient = netClient;
    }

    /// <inheritdoc />
    public void Execute(string[] arguments) {
        if (arguments.Length < 2) {
            SendUsage();
            return;
        }

        var action = arguments[1];

        if (action == "list") {
            var message = "Loaded addons: ";
            message += string.Join(
                ", ", 
                _addonManager.GetLoadedAddons().Select(addon => {
                    var msg = $"{addon.GetName()} {addon.GetVersion()}";
                    if (addon is TogglableClientAddon {Disabled: true }) {
                        msg += " (disabled)";
                    }

                    return msg; 
                })
            );

            UiManager.InternalChatBox.AddMessage(message);
            return;
        }

        if ((action != "enable" && action != "disable") || arguments.Length < 3) {
            SendUsage();
            return;
        }

        if (_netClient.IsConnected || _netClient.IsConnecting) {
            UiManager.InternalChatBox.AddMessage("Cannot toggle addons while connecting or connected to a server.");
            return;
        }
        
        if (action == "enable") {
            for (var i = 2; i < arguments.Length; i++) {
                var addonName = arguments[i];

                if (_addonManager.TryEnableAddon(addonName)) {
                    UiManager.InternalChatBox.AddMessage($"Successfully enabled '{addonName}'");
                } else {
                    UiManager.InternalChatBox.AddMessage($"Could not enable addon '{addonName}'");
                }
            }
        } else if (action == "disable") {
            for (var i = 2; i < arguments.Length; i++) {
                var addonName = arguments[i];

                if (_addonManager.TryDisableAddon(addonName)) {
                    UiManager.InternalChatBox.AddMessage($"Successfully disabled '{addonName}'");
                } else {
                    UiManager.InternalChatBox.AddMessage($"Could not disable addon '{addonName}'");
                }
            }
        }
    }

    /// <summary>
    /// Sends the command usage to the chat box.
    /// </summary>
    private void SendUsage() {
        UiManager.InternalChatBox.AddMessage($"Usage: {Trigger} <enable|disable|list> [addon(s)]");
    }
}
