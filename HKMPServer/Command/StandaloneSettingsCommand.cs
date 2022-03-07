using Hkmp.Api.Command.Server;
using Hkmp.Game.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;

namespace HkmpServer.Command {
    public class StandaloneSettingsCommand : SettingsCommand {
        public StandaloneSettingsCommand(
            ServerManager serverManager, 
            GameSettings gameSettings
        ) : base(serverManager, gameSettings) {
        }

        public override void Execute(ICommandSender commandSender, string[] args) {
            base.Execute(commandSender, args);

            ConfigManager.SaveGameSettings(GameSettings);
        }
        
        
    }
}