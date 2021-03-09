using System.Diagnostics;
using UnityEngine;

namespace HKMP.Game.Server {
    /**
     * A class containing all the relevant data managed by the server about a player.
     */
    public class PlayerData {
        public string Name { get; }
        public string CurrentScene { get; set; }

        public Vector3 LastPosition { get; set; }
        public Vector3 LastScale { get; set; }

        public Vector3 LastMapLocation { get; set; }

        public string LastAnimationClip { get; set; }
        
        public Stopwatch HeartBeatStopwatch { get; }

        public PlayerData(
            string name, 
            string currentScene, 
            Vector3 lastPosition, 
            Vector3 lastScale,
            string lastAnimationClip
        ) {
            Name = name;
            CurrentScene = currentScene;
            LastPosition = lastPosition;
            LastScale = lastScale;
            LastAnimationClip = lastAnimationClip;

            // Create a new heart beat stopwatch and start it
            HeartBeatStopwatch = new Stopwatch();
            HeartBeatStopwatch.Start();
        }
    }
}