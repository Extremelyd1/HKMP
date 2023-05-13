using Hkmp.Game.Command.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using Hkmp.Ui;
using Modding;

namespace Hkmp.Game.Server;

/// <summary>
/// Specialization of <see cref="ServerManager"/> that adds handlers for the mod specific things.
/// </summary>
internal class ModServerManager : ServerManager {
    public ModServerManager(
        NetServer netServer,
        ServerSettings serverSettings,
        PacketManager packetManager,
        UiManager uiManager
    ) : base(netServer, serverSettings, packetManager) {
        // Start addon loading once all mods have finished loading
        ModHooks.FinishedLoadingModsHook += AddonManager.LoadAddons;

        // Register handlers for UI events
        uiManager.ConnectInterface.StartHostButtonPressed += Start;
        uiManager.ConnectInterface.StopHostButtonPressed += Stop;

        // Register application quit handler
        ModHooks.ApplicationQuitHook += Stop;
    }

    /// <inheritdoc />
    protected override void RegisterCommands() {
        base.RegisterCommands();

        CommandManager.RegisterCommand(new SettingsCommand(this, InternalServerSettings));
    }
}
