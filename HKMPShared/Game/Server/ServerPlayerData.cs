using System.Diagnostics;
using HKMP.Concurrency;
using HKMP.Math;
using HKMP.Networking.Packet.Data;

namespace HKMP.Game.Server {
    /**
     * A class containing all the relevant data managed by the server about a player.
     */
    public class ServerPlayerData {
        public string Username { get; }
        public string CurrentScene { get; set; }

        public Vector2 LastPosition { get; set; }
        public bool LastScale { get; set; }

        public Vector2 LastMapPosition { get; set; }

        public ushort LastAnimationClip { get; set; }

        public Team Team { get; set; }

        public byte SkinId { get; set; }

        public ConcurrentDictionary<int, ConcurrentQueue<AnimationInfo>> AnimationInfoToSend { get; }
        
        public ConcurrentQueue<EntityUpdate> EntityUpdates { get; }

        public bool IsSceneHost { get; set; }

        public Stopwatch HeartBeatStopwatch { get; }

        public ServerPlayerData(
            string username, 
            string currentScene, 
            Vector2 lastPosition, 
            bool lastScale,
            ushort lastAnimationClip
        ) {
            Username = username;
            CurrentScene = currentScene;
            LastPosition = lastPosition;
            LastScale = lastScale;
            LastAnimationClip = lastAnimationClip;

            Team = Team.None;
            SkinId = 0;

            AnimationInfoToSend = new ConcurrentDictionary<int, ConcurrentQueue<AnimationInfo>>();

            EntityUpdates = new ConcurrentQueue<EntityUpdate>();

            // Create a new heart beat stopwatch and start it
            HeartBeatStopwatch = new Stopwatch();
            HeartBeatStopwatch.Start();
        }
    }
}