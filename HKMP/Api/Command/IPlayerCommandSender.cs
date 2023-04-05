using Hkmp.Api.Command.Server;
using JetBrains.Annotations;

namespace Hkmp.Api.Command; 

/// <summary>
/// Interface for a player that can execute commands.
/// </summary>
[PublicAPI]
public interface IPlayerCommandSender : ICommandSender {
    /// <summary>
    /// The ID of the player.
    /// </summary>
    public ushort Id { get; }
}
