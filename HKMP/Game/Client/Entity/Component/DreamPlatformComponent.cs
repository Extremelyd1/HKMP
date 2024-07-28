using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the platforms that (dis)appear in dream sequences.
internal class DreamPlatformComponent : EntityComponent {
    /// <summary>
    /// Host-client pair of the DreamPlatform components.
    /// </summary>
    private readonly HostClientPair<DreamPlatform> _platform;

    /// <summary>
    /// The number of players currently in range of the platform.
    /// </summary>
    private ushort _numInRange;
    /// <summary>
    /// Whether the local player is in range of the platform.
    /// </summary>
    private bool _isInRange;
    
    public DreamPlatformComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        _platform = new HostClientPair<DreamPlatform> {
            Client = gameObject.Client.GetComponent<DreamPlatform>(),
            Host = gameObject.Host.GetComponent<DreamPlatform>()
        };
        
        if (!_platform.Client.showOnEnable) {
            _platform.Client.outerCollider.OnTriggerExited += OuterColliderOnTriggerExited;
            _platform.Host.outerCollider.OnTriggerExited += OuterColliderOnTriggerExited;

            _platform.Client.innerCollider.OnTriggerEntered += InnerColliderOnTriggerEntered;
            _platform.Host.innerCollider.OnTriggerEntered += InnerColliderOnTriggerEntered;

            On.DreamPlatform.Start += DreamPlatformOnStart;
        }
    }

    /// <summary>
    /// Hook for the Start method of DreamPlatform. Used to prevent the original method from registering event
    /// handlers to the trigger enter/exit.
    /// </summary>
    private void DreamPlatformOnStart(On.DreamPlatform.orig_Start orig, DreamPlatform self) {
        if (self == _platform.Client || self == _platform.Host) {
            return;
        }

        orig(self);
    }

    /// <summary>
    /// Show the correct platform based on whether the entity is controlled or not.
    /// </summary>
    private void Show() {
        if (IsControlled) {
            _platform.Client.Show();
        } else {
            _platform.Host.Show();
        }
    }

    /// <summary>
    /// Hide the correct platform based on whether the entity is controlled or not.
    /// </summary>
    private void Hide() {
        if (IsControlled) {
            _platform.Client.Hide();
        } else {
            _platform.Host.Hide();
        }
    }

    /// <summary>
    /// Event handler for when the trigger for the outer collider of the platform is exited.
    /// </summary>
    private void OuterColliderOnTriggerExited(Collider2D collider, GameObject sender) {
        // If we haven't been in range of the platform but trigger the exit, we do not want to update anything
        if (!_isInRange) {
            return;
        }

        _isInRange = false;

        _numInRange--;

        // If the number of players in range is now 0 (or lower), we can hide the platform 
        if (_numInRange == 0) {
            Hide();
        }
        
        var data = new EntityNetworkData {
            Type = EntityComponentType.DreamPlatform
        };
        
        data.Packet.Write(_numInRange);
        data.Packet.Write(0);
        
        SendData(data);
    }

    /// <summary>
    /// Event handler for when the trigger for the inner collider of the platform is entered.
    /// </summary>
    private void InnerColliderOnTriggerEntered(Collider2D collider, GameObject sender) {
        _isInRange = true;
        
        EnterPlatform();
        
        var data = new EntityNetworkData {
            Type = EntityComponentType.DreamPlatform
        };
        
        data.Packet.Write(_numInRange);
        data.Packet.Write(1);
        
        SendData(data);
    }

    /// <summary>
    /// Exit the platform and decrease the number of players in range. If the number hits zero, the platform will be
    /// hidden.
    /// </summary>
    private void ExitPlatform() {
        _numInRange--;
        
        // If the number of players in range is now 0 (or lower), we can hide the platform 
        if (_numInRange == 0) {
            Hide();
        }
    }

    /// <summary>
    /// Enter the platform and increase the number of players in range. If the number is exactly 1, the platform will
    /// be shown.
    /// </summary>
    private void EnterPlatform() {
        _numInRange++;
        
        // If the number of players in range is exactly 1 now, we show the platform
        if (_numInRange == 1) {
            Show();
        }
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data, bool alreadyInSceneUpdate) {
        var numInRange = data.Packet.ReadUShort();
        var action = data.Packet.ReadByte();

        if (alreadyInSceneUpdate) {
            _numInRange = numInRange;

            if (_numInRange > 0) {
                Show();
            }

            return;
        }

        if (action == 0) {
            // Action 0 is exited collider
            ExitPlatform();
        } else if (action == 1) {
            // Action 1 is entered collider
            EnterPlatform();
        } else {
            Logger.Error($"Could not process unknown action for DreamPlatformComponent update: {action}");
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        if (_platform.Client != null) {
            _platform.Client.outerCollider.OnTriggerExited -= OuterColliderOnTriggerExited;
            _platform.Client.innerCollider.OnTriggerEntered -= InnerColliderOnTriggerEntered;
        }

        if (_platform.Host != null) {
            _platform.Host.outerCollider.OnTriggerExited -= OuterColliderOnTriggerExited;
            _platform.Host.innerCollider.OnTriggerEntered -= InnerColliderOnTriggerEntered;
        }

        On.DreamPlatform.Start -= DreamPlatformOnStart;
    }
}
