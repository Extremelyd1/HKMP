using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using HKMP.Concurrency;
using HKMP.Networking.Packet.Custom;
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

        public Vector3 LastMapPosition { get; set; }

        public ushort LastAnimationClip { get; set; }

        public ConcurrentDictionary<int, ConcurrentQueue<AnimationInfo>> AnimationInfoToSend { get; }

        public Stopwatch HeartBeatStopwatch { get; }

        public PlayerData(
            string name, 
            string currentScene, 
            Vector3 lastPosition, 
            Vector3 lastScale,
            ushort lastAnimationClip
        ) {
            Name = name;
            CurrentScene = currentScene;
            LastPosition = lastPosition;
            LastScale = lastScale;
            LastAnimationClip = lastAnimationClip;

            AnimationInfoToSend = new ConcurrentDictionary<int, ConcurrentQueue<AnimationInfo>>();

            // Create a new heart beat stopwatch and start it
            HeartBeatStopwatch = new Stopwatch();
            HeartBeatStopwatch.Start();
        }
    }
}