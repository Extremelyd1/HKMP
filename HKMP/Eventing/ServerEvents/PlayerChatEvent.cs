using System;
using Hkmp.Api.Eventing;
using Hkmp.Api.Eventing.ServerEvents;
using Hkmp.Api.Server;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Eventing.ServerEvents;

/// <inheritdoc cref="Hkmp.Api.Eventing.ServerEvents.IPlayerChatEvent" />
internal class PlayerChatEvent : ServerEvent, IPlayerChatEvent {
    /// <inheritdoc />
    public bool Cancelled { get; set; }
    
    /// <inheritdoc />
    public IServerPlayer Player { get; }

    private string _message;
    
    /// <inheritdoc />
    public string Message {
        get => _message;
        set {
            if (value == null) {
                throw new ArgumentNullException(nameof(value), "Message cannot be null");
            }

            if (value.Length > ChatMessage.MaxMessageLength) {
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"Message cannot be longer than {ChatMessage.MaxMessageLength} characters");
            }

            _message = value;
        }
    }

    /// <inheritdoc />
    public PlayerChatEvent(IServerPlayer player, string message) {
        Player = player;
        Message = message;
    }
}
