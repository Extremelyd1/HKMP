using System.Collections;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using Modding;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the flipping of platforms in Crystal Peak.
internal class FlipPlatformComponent : EntityComponent {
    /// <summary>
    /// Host-client pair of the FlipPlatform behaviours.
    /// </summary>
    private readonly HostClientPair<FlipPlatform> _platform;

    /// <summary>
    /// The last boolean value of the 'hitCancel' boolean in the behaviour. 
    /// </summary>
    private bool _lastHitCancel;
    
    public FlipPlatformComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        _platform = new HostClientPair<FlipPlatform> {
            Client = gameObject.Client.GetComponent<FlipPlatform>(),
            Host = gameObject.Host.GetComponent<FlipPlatform>()
        };
        
        On.FlipPlatform.Flip += FlipPlatformOnFlip;
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// Hook method that fires when the Flip method is called on the behaviour. Will network that the platform
    /// should be flipped regardless of scene host.
    /// </summary>
    private IEnumerator FlipPlatformOnFlip(On.FlipPlatform.orig_Flip orig, FlipPlatform self) {
        if (self != _platform.Client && self != _platform.Host) {
            yield return orig(self);
            yield break;
        }
        
        var data = new EntityNetworkData {
            Type = EntityComponentType.FlipPlatform
        };
        
        data.Packet.Write((byte) 0);

        SendData(data);
        
        yield return orig(self);
    }
    
    /// <summary>
    /// Update method that checks the value of the 'hitCancel' boolean and conditionally networks it indicating that
    /// the platform should be flipped back.
    /// </summary>
    private void OnUpdate() {
        var platform = IsControlled ? _platform.Client : _platform.Host;
        
        var hitCancel = ReflectionHelper.GetField<FlipPlatform, bool>(platform, "hitCancel");
        if (hitCancel == _lastHitCancel) {
            return;
        }
        
        _lastHitCancel = hitCancel;

        if (!hitCancel) {
            return;
        }
        
        var data = new EntityNetworkData {
            Type = EntityComponentType.FlipPlatform
        };

        data.Packet.Write((byte) 1);

        SendData(data);
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        var platform = IsControlled ? _platform.Client : _platform.Host;
        
        var type = data.Packet.ReadByte();
        
        if (type == 0) {
            var idleRoutine = ReflectionHelper.GetField<FlipPlatform, Coroutine>(platform, "idleRoutine");
            var flipRoutine = ReflectionHelper.GetField<FlipPlatform, Coroutine>(platform, "flipRoutine");

            if (idleRoutine != null) {
                platform.StopCoroutine(idleRoutine);
            }

            if (flipRoutine != null) {
                return;
            }

            var flipCall = ReflectionHelper.CallMethod<FlipPlatform, IEnumerator>(platform, "Flip");
            flipRoutine = platform.StartCoroutine(flipCall);
            ReflectionHelper.SetField(platform, "flipRoutine", flipRoutine);
        } else if (type == 1) {
            ReflectionHelper.SetField(platform, "hitCancel", true);
        } else {
            Logger.Error("Received unknown type of data for FlipPlatform");
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        On.FlipPlatform.Flip -= FlipPlatformOnFlip;
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}
