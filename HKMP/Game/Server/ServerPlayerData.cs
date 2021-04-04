using System.Diagnostics;
using HKMP.Concurrency;
using HKMP.Networking.Packet.Data;
using UnityEngine;

namespace HKMP.Game.Server {
    /**
     * A class containing all the relevant data managed by the server about a player.
     */
    public class ServerPlayerData {
        public string Username { get; }
        public string CurrentScene { get; set; }

        public Vector2 LastPosition { get; set; }
        public bool LastScale { get; set; }

        public Vector3 LastMapPosition { get; set; }

        public ushort LastAnimationClip { get; set; }

        public Team Team { get; set; }
        
        public ConcurrentDictionary<int, ConcurrentQueue<AnimationInfo>> AnimationInfoToSend { get; }
        
        public ConcurrentQueue<EntityUpdate> EntityUpdates { get; }

        public Stopwatch HeartBeatStopwatch { get; }

        public ServerPlayerData(
            string username, 
            string currentScene, 
            Vector3 lastPosition, 
            bool lastScale,
            ushort lastAnimationClip
        ) {
            Username = username;
            CurrentScene = currentScene;
            LastPosition = lastPosition;
            LastScale = lastScale;
            LastAnimationClip = lastAnimationClip;

            Team = Team.None;

            AnimationInfoToSend = new ConcurrentDictionary<int, ConcurrentQueue<AnimationInfo>>();

            EntityUpdates = new ConcurrentQueue<EntityUpdate>();

            // Create a new heart beat stopwatch and start it
            HeartBeatStopwatch = new Stopwatch();
            HeartBeatStopwatch.Start();
        }
    }
}