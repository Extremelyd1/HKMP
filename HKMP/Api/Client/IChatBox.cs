
namespace Hkmp.Api.Client;

/// <summary>
/// The message box in the bottom right of the screen that shows information related to HKMP.
/// </summary>
public interface IChatBox {
    /// <summary>
    /// Add a message to the chat box.
    /// </summary>
    /// <param name="message">The string containing the message.</param>
    void AddMessage(string message);
}
