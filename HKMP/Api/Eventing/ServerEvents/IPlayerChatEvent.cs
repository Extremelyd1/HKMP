using Hkmp.Api.Server;

namespace Hkmp.Api.Eventing.ServerEvents; 

/// <summary>
/// Event for when a player sends a chat message.
/// </summary>
public interface IPlayerChatEvent : Cancellable {
    /// <summary>
    /// The player that sent the chat message.
    /// </summary>
    public IServerPlayer Player { get; }
    
    /// <summary>
    /// The message that was sent.
    /// </summary>
    public string Message { get; set; }
}
