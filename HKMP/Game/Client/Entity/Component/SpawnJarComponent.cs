using System;
using System.Collections;
using Hkmp.Game.Client.Entity.Action;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using Modding;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;
using Random = UnityEngine.Random;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the SpawnJarControl behaviour of the entity.
internal class SpawnJarComponent : EntityComponent {
    /// <summary>
    /// The <see cref="SpawnJarControl"/> unity component of the entity.
    /// </summary>
    private readonly HostClientPair<SpawnJarControl> _spawnJar;

    /// <summary>
    /// Whether this is the first spawn for the jar.
    /// </summary>
    private bool _firstSpawn;

    public SpawnJarComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject,
        HostClientPair<SpawnJarControl> spawnJar
    ) : base(netClient, entityId, gameObject) {
        _spawnJar = spawnJar;
        spawnJar.Client.enabled = false;
        
        On.SpawnJarControl.OnEnable += SpawnJarControlOnEnable;

        // We can't simply hook the Behaviour method itself because it returns a state machine for the IEnumerator
        // Instead we get the state machine target with MonoMod and get the hook that way
        HookEndpointManager.Modify(
            MonoMod.Utils.Extensions.GetStateMachineTarget(
                ReflectionHelper.GetMethodInfo(
                    typeof(SpawnJarControl),
                    "Behaviour"
                )
            ),
            SpawnJarControlOnBehaviour
        );

        _firstSpawn = true;
    }
    
    /// <summary>
    /// Hook on the OnEnable method of the SpawnJarControl to network that it should start on the client-side.
    /// </summary>
    private void SpawnJarControlOnEnable(On.SpawnJarControl.orig_OnEnable orig, SpawnJarControl self) {
        orig(self);
        
        if (self != _spawnJar.Host) {
            return;
        }
        
        if (_firstSpawn) {
            return;
        }
        
        var data = new EntityNetworkData {
            Type = EntityComponentType.SpawnJar
        };
        SendData(data);
        
        Logger.Debug("Sending SpawnJarComponent data OnEnable");
    }

    /// <summary>
    /// IL hook for modifying the Behaviour method to grab the game object that is spawned from the jar.
    /// </summary>
    /// <param name="il">The IL context for the method.</param>
    private void SpawnJarControlOnBehaviour(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            // Goto the next call instruction Spawn twice, the first one is a the spawning of a nail strike
            c.GotoNext(i => i.MatchCall(typeof(ObjectPoolExtensions), "Spawn"));
            c.GotoNext(i => i.MatchCall(typeof(ObjectPoolExtensions), "Spawn"));

            // Move the cursor after the call instruction
            c.Index++;

            // Emit a delegate that pops the current game object off the stack (our spawned object) and uses it
            // before putting it back on the stack again
            c.EmitDelegate<Func<GameObject, GameObject>>(gameObject => {
                Logger.Debug($"SpawnJarControl spawned entity: {gameObject.name}");
                EntityFsmActions.CallEntitySpawnEvent(new EntitySpawnDetails {
                    Type = EntitySpawnType.SpawnJarComponent,
                    GameObject = gameObject
                });
                
                return gameObject;
            });
        } catch (Exception e) {
            Logger.Error($"Could not change SpawnJarControl#Behaviour IL:\n{e}");
        }
    }
    
    /// <inheritdoc />
    public override void InitializeHost() {
        var data = new EntityNetworkData {
            Type = EntityComponentType.SpawnJar
        };
        SendData(data);
        
        Logger.Debug("Sending SpawnJarComponent data InitializeHost");

        _firstSpawn = false;
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        Logger.Debug("Received SpawnJarComponent data");
        MonoBehaviourUtil.Instance.StartCoroutine(Behaviour());
        IEnumerator Behaviour() {
            var jar = _spawnJar.Client;

            jar.transform.SetPositionZ(0.01f);
            
            jar.readyDust.Play();

            yield return new WaitForSeconds(0.5f);

            var body = jar.GetComponent<Rigidbody2D>();
            body.angularVelocity = Random.Range(0, 2) > 0 ? -300f : 300f;

            jar.readyDust.Stop();
            jar.dustTrail.Play();

            var sprite = jar.GetComponent<SpriteRenderer>();
            sprite.enabled = true;

            // The same while loop with a slightly higher threshold for breaking, because sometimes it doesn't reach
            // that position quite yet due to network instability
            while (jar.transform.position.y > jar.breakY + 0.1f) {
                yield return null;
            }
            
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

            var position = jar.transform.position;
            
            jar.dustTrail.Stop();
            jar.ptBreakS.Play();
            jar.ptBreakL.Play();
            jar.strikeNailR.Spawn(position);

            body.angularVelocity = 0.0f;

            sprite.enabled = false;
            
            jar.breakSound.SpawnAndPlayOneShot(jar.audioSourcePrefab, position);
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        On.SpawnJarControl.OnEnable -= SpawnJarControlOnEnable;
        
        HookEndpointManager.Unmodify(
            MonoMod.Utils.Extensions.GetStateMachineTarget(
                ReflectionHelper.GetMethodInfo(
                    typeof(SpawnJarControl),
                    "Behaviour"
                )
            ),
            SpawnJarControlOnBehaviour
        );
    }
}