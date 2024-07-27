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

    private static MusicComponent _instance;

    private byte _lastMusicCueIndex;
    private byte _lastSnapshotIndex;

    static MusicComponent() {
        var dataPair = FileUtil.LoadObjectFromEmbeddedJson<
            (List<MusicCueData>, List<AudioMixerSnapshotData>)
        >(MusicDataFilePath);
        
        MusicCueDataList = dataPair.Item1;
        SnapshotDataList = dataPair.Item2;

        byte index = 1;
        foreach (var data in MusicCueDataList) {
            data.Index = index++;
        }

        foreach (var data in SnapshotDataList) {
            data.Index = index++;
        }

        On.PlayMakerFSM.OnEnable += OnFsmEnable;
    }

    public static bool CreateInstance(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject,
        out MusicComponent musicComponent
    ) {
        if (_instance == null) {
            _instance = new MusicComponent(netClient, entityId, gameObject);
            musicComponent = _instance;
            return true;
        }

        musicComponent = null;
        return false;
    }

    public static void ClearInstance() {
        _instance = null;
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
    
    private static bool GetAudioMixerSnapshotData(Func<AudioMixerSnapshotData, bool> predicate, out AudioMixerSnapshotData snapshotData) {
        foreach (var data in SnapshotDataList) {
            if (predicate.Invoke(data)) {
                snapshotData = data;
                return true;
            }
        }

        snapshotData = null;
        return false;
    }

    private MusicComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        On.HutongGames.PlayMaker.Actions.ApplyMusicCue.OnEnter += ApplyMusicCueOnEnter;
        On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter += TransitionToAudioSnapshotOnEnter;
    }

    private void ApplyMusicCueOnEnter(
        On.HutongGames.PlayMaker.Actions.ApplyMusicCue.orig_OnEnter orig, 
        ApplyMusicCue self
    ) {
        
        Logger.Debug($"ApplyMusicCueOnEnter: {self.Fsm.GameObject.gameObject.name}, {self.Fsm.Name}");

        if (IsControlled) {
            return;
        }
        
        orig(self);
        
        var musicCue = self.musicCue.Value;
        if (musicCue == null) {
            return;
        }
        
        foreach (var musicCueData in MusicCueDataList) {
            if (musicCueData.MusicCue == musicCue || musicCueData.Name == musicCue.name) {
                Logger.Debug($"  Sending data, index: {musicCueData.Index}");
                
                var networkData = new EntityNetworkData {
                    Type = EntityComponentType.Music
                };
                networkData.Packet.Write(musicCueData.Index);
                networkData.Packet.Write(_lastSnapshotIndex);

                SendData(networkData);

                _lastMusicCueIndex = musicCueData.Index;

                return;
            }
        }
    }
    
    private void TransitionToAudioSnapshotOnEnter(
        On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.orig_OnEnter orig, 
        TransitionToAudioSnapshot self
    ) {
        Logger.Debug($"TransitionToAudioSnapshotOnEnter: {self.Fsm.GameObject.gameObject.name}, {self.Fsm.Name}");

        if (IsControlled) {
            return;
        }
        
        orig(self);
        
        var snapshot = self.snapshot.Value;
        if (snapshot == null) {
            return;
        }
        
        foreach (var snapshotData in SnapshotDataList) {
            if (snapshotData.Snapshot == snapshot || snapshotData.Name == snapshot.name) {
                Logger.Debug($"  Sending data, index: {snapshotData.Index}");
                
                var networkData = new EntityNetworkData {
                    Type = EntityComponentType.Music
                };
                networkData.Packet.Write(_lastMusicCueIndex);
                networkData.Packet.Write(snapshotData.Index);

                SendData(networkData);

                _lastSnapshotIndex = snapshotData.Index;
                
                return;
            }
        }
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        Logger.Debug("Update MusicComponent");
        
        if (!IsControlled) {
            Logger.Debug("  Not controlled, skipping");
            return;
        }

        var musicCueIndex = data.Packet.ReadByte();
        var snapshotIndex = data.Packet.ReadByte();
        
        Logger.Debug($"Applying entity network data for music component with indices: {musicCueIndex},  {snapshotIndex}");

        if (musicCueIndex != _lastMusicCueIndex) {
            ApplyIndex(musicCueIndex);
            _lastMusicCueIndex = musicCueIndex;
        }

        if (snapshotIndex != _lastSnapshotIndex) {
            ApplyIndex(snapshotIndex);
            _lastSnapshotIndex = snapshotIndex;
        }

        void ApplyIndex(byte index) {
            foreach (var musicCueData in MusicCueDataList) {
                if (musicCueData.Index != index) {
                    continue;
                }

                if (musicCueData.MusicCue == null) {
                    continue;
                }

                Logger.Debug($"  Found music cue ({musicCueData.Name}, {musicCueData.Type}), applying it");

                var gm = global::GameManager.instance;
                gm.AudioManager.ApplyMusicCue(musicCueData.MusicCue, 0f, 0f, false);
                return;
            }

            foreach (var snapshotData in SnapshotDataList) {
                if (snapshotData.Index != index) {
                    continue;
                }

                if (snapshotData.Snapshot == null) {
                    continue;
                }

                Logger.Debug("  Found audio mixer snapshot, transitioning to it");
                snapshotData.Snapshot.TransitionTo(0f);
                return;
            }

            Logger.Debug("  Could not find music cue or audio mixer snapshot matching ID");
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        On.HutongGames.PlayMaker.Actions.ApplyMusicCue.OnEnter -= ApplyMusicCueOnEnter;
        On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter -= TransitionToAudioSnapshotOnEnter;
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

                    if (GetAudioMixerSnapshotData(
                        data => data.Name.Equals(snapshot.name),
                        out var snapshotData
                    )) {
                        Logger.Debug($"  Adding to data with type: {snapshotData.Type}");
                        snapshotData.Snapshot = snapshot;
                    }
                }
            }
        }
    }
    
    private class MusicCueData {
        public MusicCueType Type { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public byte Index { get; set; }
        [JsonIgnore]
        public MusicCue MusicCue { get; set; }
    }

    private class AudioMixerSnapshotData {
        public AudioMixerSnapshotType Type { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
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
        Normal
    }
}
