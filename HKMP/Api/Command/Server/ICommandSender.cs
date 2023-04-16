
namespace Hkmp.Api.Command.Server;

/// <summary>
/// Interface for an entity that can execute commands.
/// </summary>
public interface ICommandSender {
    /// <summary>
    /// Whether this user is authorized, meaning they have high-level permission.
    /// </summary>
    bool IsAuthorized { get; }

    /// <summary>
    /// The type of this command sender.
    /// </summary>
    CommandSenderType Type { get; }

    /// <summary>
    /// Send a message to this command sender.
    /// </summary>
    /// <param name="message">The message in string form.</param>
    void SendMessage(string message);
}

/// <summary>
/// Enum containing all possible types of command senders.
/// </summary>
public enum CommandSenderType {
    /// <summary>
    /// Player as sender of a command.
    /// </summary>
    Player,

    /// <summary>
    /// Console as sender of a command.
    /// </summary>
    Console
}
