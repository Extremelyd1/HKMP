using System;
using System.Collections.Generic;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Audio;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Component;

// TODO: document all fields and methods
/// <inheritdoc />
/// This component manages the music that plays for boss fights.
internal class MusicComponent : EntityComponent {
    /// <summary>
    /// The file path of the embedded resource file for music data.
    /// </summary>
    private const string MusicDataFilePath = "Hkmp.Resource.music-data.json";
    
    private static readonly List<MusicCueData> MusicCueDataList;
    private static readonly List<AudioMixerSnapshotData> SnapshotDataList;

    static MusicComponent() {
        var dataPair = FileUtil.LoadObjectFromEmbeddedJson<
            (List<MusicCueData>, List<AudioMixerSnapshotData>)
        >(MusicDataFilePath);
        
        MusicCueDataList = dataPair.Item1;
        SnapshotDataList = dataPair.Item2;

        On.PlayMakerFSM.OnEnable += OnFsmEnable;
    }

    public MusicComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        // TODO: register hooks for entering ApplyMusicCue and TransitionToAudioSnapshot actions
        // TODO: these hooks should network changes in Music and Audio to the server if the player is scene host
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        // TODO: handle receiving Music and Audio updates from the server and applying them
    }

    /// <inheritdoc />
    public override void Destroy() {
    }

    private static void OnFsmEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self) {
        orig(self);
        
        foreach (var state in self.FsmStates) {
            foreach (var action in state.Actions) {
                if (action is ApplyMusicCue applyMusicCue) {
                    var musicCue = applyMusicCue.musicCue.Value as MusicCue;
                    if (musicCue == null) {
                        continue;
                    }
                    
                    Logger.Debug($"Found music cue '{musicCue.name}' in FSM '{self.Fsm.Name}', '{state.Name}'");

                    if (GetMusicCueData(
                        data => data.Name.Equals(musicCue.name), 
                        out var musicCueData
                    )) {
                        Logger.Debug($"  Adding to data with type: {musicCueData.Type}");
                        musicCueData.MusicCue = musicCue;
                    }
                } else if (action is TransitionToAudioSnapshot snapshotAction) {
                    var snapshot = snapshotAction.snapshot.Value as AudioMixerSnapshot;
                    if (snapshot == null) {
                        continue;
                    }
                    
                    Logger.Debug($"Found audio mixer snapshot '{snapshot.name}' in FSM '{self.Fsm.Name}', '{state.Name}'");
                }
            }
        }
    }

    private static bool GetMusicCueData(Func<MusicCueData, bool> predicate, out MusicCueData musicCueData) {
        foreach (var data in MusicCueDataList) {
            if (predicate.Invoke(data)) {
                musicCueData = data;
                return true;
            }
        }

        musicCueData = null;
        return false;
    }
    
    private class MusicCueData {
        public MusicCueType Type { get; set; }
        public string Name { get; set; }
        public byte Index { get; set; }
        [JsonIgnore]
        public MusicCue MusicCue { get; set; }
    }

    private class AudioMixerSnapshotData {
        public AudioMixerSnapshotType Type { get; set; }
        public string Name { get; set; }
        public byte Index { get; set; }
        [JsonIgnore]
        public AudioMixerSnapshot Snapshot { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    private enum MusicCueType {
        None,
        FalseKnight,
        Hornet,
        GGHornet,
        MantisLords,
        SoulMaster,
        SoulMaster2,
        GGHeavy,
        EnemyBattle,
        DreamFight,
        Hive,
        HiveKnight,
        DungDefender,
        BrokenVessel,
        Nosk,
        TheHollowKnight,
        Greenpath,
        Waterways
    }

    [JsonConverter(typeof(StringEnumConverter))]
    private enum AudioMixerSnapshotType {
        Silent,
        None,
        Off,
    }
}
