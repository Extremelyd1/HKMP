using Hkmp.Api.Server;
using Hkmp.Math;

namespace Hkmp.Game.Server {
    public class ServerPlayerData : IServerPlayer {
        public ushort Id { get; }
        public string AuthKey { get; }

        public bool IsAuthorized => !_authorizedList.IsEnabled || _authorizedList.Contains(AuthKey);

        public string Username { get; }
        public string CurrentScene { get; set; }

        public Vector2 Position { get; set; }
        public bool Scale { get; set; }

        // TODO: if this field is not used, then it is not sent to newly connecting players
        public Vector2 MapPosition { get; set; }

        public ushort AnimationId { get; set; }

        public Team Team { get; set; } = Team.None;

        public byte SkinId { get; set; }

        private readonly AuthList _authorizedList;

        public ServerPlayerData(
            ushort id,
            string username,
            string authKey,
            AuthList authorizedList
        ) {
            Id = id;
            Username = username;
            AuthKey = authKey;

            _authorizedList = authorizedList;
        }
    }
}