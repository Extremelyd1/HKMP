using Hkmp.Api.Server;
using Hkmp.Math;

namespace Hkmp.Game.Server {
    public class ServerPlayerData : IServerPlayer {
        public ushort Id { get; }
        
        public string Username { get; }
        public string CurrentScene { get; set; }

        public Vector2 Position { get; set; }
        public bool Scale { get; set; }

        // TODO: if this field is not used, then it is not sent to newly connecting players
        public Vector2 MapPosition { get; set; }

        public ushort AnimationId { get; set; }

        public Team Team { get; set; }

        public byte SkinId { get; set; }

        public ServerPlayerData(
            ushort id,
            string username,
            string currentScene,
            Vector2 position,
            bool scale,
            ushort animationId
        ) {
            Id = id;
            Username = username;
            CurrentScene = currentScene;
            Position = position;
            Scale = scale;
            AnimationId = animationId;

            Team = Team.None;
            SkinId = 0;
        }
    }
}