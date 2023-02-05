using Hkmp.Api.Server;
using Hkmp.Game.Server.Auth;
using Hkmp.Math;

namespace Hkmp.Game.Server;

/// <inheritdoc />
internal class ServerPlayerData : IServerPlayer {
    /// <inheritdoc />
    public ushort Id { get; }

    /// <summary>
    /// The string representation of the IP address of the player.
    /// </summary>
    public string IpAddressString { get; }

    /// <summary>
    /// The authentication key of the player.
    /// </summary>
    public string AuthKey { get; }

    /// <inheritdoc />
    public bool IsAuthorized => _authorizedList.Contains(AuthKey);

    /// <inheritdoc />
    public string Username { get; }

    /// <inheritdoc />
    public string CurrentScene { get; set; }

    /// <inheritdoc />
    public Vector2 Position { get; set; }

    /// <inheritdoc />
    public bool Scale { get; set; }

    /// <inheritdoc />
    public bool HasMapIcon { get; set; }

    /// <inheritdoc />
    public Vector2 MapPosition { get; set; }

    /// <inheritdoc />
    public ushort AnimationId { get; set; }

    /// <inheritdoc />
    public Team Team { get; set; } = Team.None;

    /// <inheritdoc />
    public byte SkinId { get; set; }

    /// <summary>
    /// Reference of the authorized list for checking whether this player is authorized.
    /// </summary>
    private readonly AuthKeyList _authorizedList;

    /// <summary>
    /// Constructs new server player data given ID, name, auth key and reference of authorized list.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="ipAddress">The IP address of the client as a string.</param>
    /// <param name="username">The username of the player.</param>
    /// <param name="authKey">The authentication key of the player.</param>
    /// <param name="authorizedList">A reference to the authorized list of the server.</param>
    public ServerPlayerData(
        ushort id,
        string ipAddress,
        string username,
        string authKey,
        AuthKeyList authorizedList
    ) {
        Id = id;
        IpAddressString = ipAddress;
        Username = username;
        AuthKey = authKey;

        CurrentScene = "";

        _authorizedList = authorizedList;
    }
}
