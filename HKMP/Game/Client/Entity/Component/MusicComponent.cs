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
    
    /// <summary>
    /// Static list of MusicCueData instances that is loaded from an embedded JSON file.
    /// Used for coupling IDs to music cues that can then be used for bidirectional lookups.
    /// </summary>
    private static readonly List<MusicCueData> MusicCueDataList;
    /// <summary>
    /// Static list of AudioMixerSnapshotData instances that is loaded from an embedded JSON file.
    /// Used for coupling IDs to audio snapshots that can then be used for bidirectional lookups.
    /// </summary>
    private static readonly List<AudioMixerSnapshotData> SnapshotDataList;

    /// <summary>
    /// The singleton instance of MusicComponent to ensure we only have one MusicComponent responsible for
    /// synchronising music in a scene.
    /// </summary>
    private static MusicComponent _instance;

    /// <summary>
    /// The index of the last played music cue, so we don't restart them unnecessarily.
    /// </summary>
    private byte _lastMusicCueIndex;
    /// <summary>
    /// The index of the last played audio snapshot, so we don't restart them unnecessarily.
    /// </summary>
    private byte _lastSnapshotIndex;

    /// <summary>
    /// Static constructor responsible for loading data from the JSON and registering static hooks.
    /// </summary>
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

    /// <summary>
    /// Try to create a new instance of MusicComponent if it doesn't exist yet. This will prevent the creation of
    /// more instances by keeping track of a singleton instance.
    /// </summary>
    /// <param name="netClient">The NetClient instance for networking data.</param>
    /// <param name="entityId">The entity ID that this component is attached to.</param>
    /// <param name="gameObject">The host-client pair of game objects of the entity.</param>
    /// <param name="musicComponent">The created instance of MusicComponent if successful, otherwise null.</param>
    /// <returns>True if a new component could be created, false if a component already existed.</returns>
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

    /// <summary>
    /// Clear the current singleton instance of the component.
    /// </summary>
    public static void ClearInstance() {
        _instance = null;
    }
    
    /// <summary>
    /// Get the MusicCueData instance from the list for which the given predicate holds.
    /// </summary>
    /// <param name="predicate">The predicate function that should return true for the MusicCueData that is
    /// requested.</param>
    /// <param name="musicCueData">The MusicCueData for which the predicate holds, or null if no such instance could
    /// be found.</param>
    /// <returns>True if the MusicCueData was found, false otherwise.</returns>
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
    
    /// <summary>
    /// Get the AudioMixerSnapshotData instance from the list for which the given predicate holds.
    /// </summary>
    /// <param name="predicate">The predicate function that should return true for the AudioMixerSnapshotData that is
    /// requested.</param>
    /// <param name="snapshotData">The AudioMixerSnapshotData for which the predicate holds, or null if no such
    /// instance could be found.</param>
    /// <returns>True if the AudioMixerSnapshotData was found, false otherwise.</returns>
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
        CustomHooks.ApplyMusicCueFromFsmAction += OnApplyMusicCue;
        CustomHooks.TransitionToAudioSnapshotFromFsmAction += OnTransitionToAudioSnapshot;
    }

    /// <summary>
    /// Hook that is called when the AudioManager.ApplyMusicCue is called from an ApplyMusicCue FSM action.
    /// Used to network the starting of a music cue for the scene host.
    /// </summary>
    /// <param name="action">The ApplyMusicCue FSM action responsible for the call.</param>
    private void OnApplyMusicCue(ApplyMusicCue action) {
        Logger.Debug($"OnApplyMusicCue: {action.Fsm.GameObject.gameObject.name}, {action.Fsm.Name}");

        if (IsControlled) {
            return;
        }
        
        var musicCue = action.musicCue.Value;
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
    
    /// <summary>
    /// Hook that is called when the AudioMixerSnapshot.TransitionTo is called from an TransitionToAudioSnapshot FSM
    /// action. Used to network the starting of a audio snapshot for the scene host.
    /// </summary>
    /// <param name="action">The TransitionToAudioSnapshot FSM action responsible for the call.</param>
    private void OnTransitionToAudioSnapshot(TransitionToAudioSnapshot action) {
        Logger.Debug($"OnTransitionToAudioSnapshot: {action.Fsm.GameObject.gameObject.name}, {action.Fsm.Name}");

        if (action.Fsm.Name.Equals("Door Control")) {
            Logger.Debug("  Was door control, allowing");
            return;
        }

        if (IsControlled) {
            return;
        }
        
        var snapshot = action.snapshot.Value;
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
    public override void Update(EntityNetworkData data, bool alreadyInSceneUpdate) {
        Logger.Debug("Update MusicComponent");
        
        if (!IsControlled) {
            Logger.Debug("  Not controlled, skipping");
            return;
        }

        var musicCueIndex = data.Packet.ReadByte();
        var snapshotIndex = data.Packet.ReadByte();
        
        Logger.Debug($"Applying entity network data for music component with indices: {musicCueIndex}, {snapshotIndex}");

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
        CustomHooks.ApplyMusicCueFromFsmAction -= OnApplyMusicCue;
        CustomHooks.TransitionToAudioSnapshotFromFsmAction -= OnTransitionToAudioSnapshot;
    }

    /// <summary>
    /// Hook for when an FSM becomes enabled. Used to check for ApplyMusicCue or TransitionToAudioSnapshot actions
    /// such that their audio data can be added to the lists of data.
    /// </summary>
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

    /// <summary>
    /// Data for music cues, used for looking up the index or the music cue from index for networking purposes.
    /// </summary>
    private class MusicCueData {
        public MusicCueType Type { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public byte Index { get; set; }
        [JsonIgnore]
        public MusicCue MusicCue { get; set; }
    }

    /// <summary>
    /// Data for audio snapshots, used for looking up the index or the audio snapshot from index for networking
    /// purposes.
    /// </summary>
    private class AudioMixerSnapshotData {
        public AudioMixerSnapshotType Type { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public byte Index { get; set; }
        [JsonIgnore]
        public AudioMixerSnapshot Snapshot { get; set; }
    }

    /// <summary>
    /// Enum for music cue types.
    /// </summary>
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

    /// <summary>
    /// Enum for audio snapshot types.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    private enum AudioMixerSnapshotType {
        Silent,
        None,
        Off,
        Normal
    }
}
