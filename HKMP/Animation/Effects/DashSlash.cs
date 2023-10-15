using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Dash Slash ability.
/// </summary>
internal class DashSlash : ParryableEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Cancel the nail art charge animation if it exists
        AnimationManager.NailArtEnd.Play(playerObject);

        // Obtain the Nail Arts FSM from the Hero Controller
        var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");

        // Get an audio source relative to the player
        var audioObject = AudioUtil.GetAudioSourceObject(playerObject);
        var audioSource = audioObject.GetComponent<AudioSource>();

        // Get the audio clip of the Great Slash
        var dashSlashClip = (AudioClip) nailArts.GetFirstAction<AudioPlay>("Dash Slash").oneShotClip.Value;
        audioSource.PlayOneShot(dashSlashClip);

        Object.Destroy(audioObject, dashSlashClip.length);

        // Get the attacks gameObject from the player object
        var localPlayerAttacks = HeroController.instance.gameObject.FindGameObjectInChildren("Attacks");

        // Get the prefab for the Dash Slash and instantiate it relative to the remote player object
        var dashSlashObject = localPlayerAttacks.FindGameObjectInChildren("Dash Slash");
        var dashSlash = Object.Instantiate(
            dashSlashObject,
            playerObject.transform.parent
        );
        dashSlash.layer = 17;

        // Since we anchor the dash slash on the player container instead of the player object
        // (to prevent it from flipping when the knight turns around) we need to adjust the scale based
        // on which direction the knight is facing
        var localScale = playerObject.transform.localScale;
        var playerScaleX = localScale.x;
        var dashSlashTransform = dashSlash.transform;
        var dashSlashScale = dashSlashTransform.localScale;
        dashSlashTransform.localScale = new Vector3(
            dashSlashScale.x * playerScaleX,
            dashSlashScale.y,
            dashSlashScale.z
        );

        // Check which direction the knight is facing for the damages_enemy FSM
        var facingRight = playerScaleX > 0;
        ChangeAttackTypeOfFsm(dashSlash, facingRight ? 180f : 0f);

        // Add rigid body and set it to be kinematic so it doesn't do physics, but still counts certain collisions
        var rigidBody = dashSlash.AddComponent<Rigidbody2D>();
        rigidBody.isKinematic = true;

        var controlColliderFsm = dashSlash.LocateMyFSM("Control Collider");
        // If the player is not facing right, the local position set by the FSM is not right given that we are spawning
        // the dash slash on the player container instead of on the player object
        if (!facingRight) {
            var startPosVar = controlColliderFsm.FsmVariables.GetFsmVector3("Start Pos");
            startPosVar.Value = new Vector3(
                startPosVar.Value.x * -1,
                startPosVar.Value.y,
                startPosVar.Value.z
            );
        }

        dashSlash.SetActive(true);

        // Remove audio source component that exists on the dash slash object
        Object.Destroy(dashSlash.GetComponent<AudioSource>());

        // Set the newly instantiate collider to state Init, to reset it
        // in case the local player was already performing it
        if (controlColliderFsm.ActiveStateName != "Init") {
            controlColliderFsm.SetState("Init");
        }

        var damage = ServerSettings.DashSlashDamage;
        if (ServerSettings.IsPvpEnabled && ShouldDoDamage) {
            // Since the dash slash should deal damage to other players, we create a separate object for that purpose
            var pvpCollider = new GameObject("PvP Collider", typeof(PolygonCollider2D));

            var transform = pvpCollider.transform;
            transform.SetParent(dashSlash.transform);
            transform.localPosition = new Vector3(0, 0, 0);
            transform.localScale = new Vector3(1, 1, 0);

            pvpCollider.SetActive(true);
            pvpCollider.layer = 22;

            // Copy over the polygon collider points
            pvpCollider.GetComponent<PolygonCollider2D>().points =
                dashSlash.GetComponent<PolygonCollider2D>().points;
            
            if (ServerSettings.AllowParries) {
                AddParryFsm(pvpCollider);
            }

            if (damage != 0) {
                pvpCollider.AddComponent<DamageHero>().damageDealt = damage;
            }
        }

        // Get the animator, figure out the duration of the animation and destroy the object accordingly afterwards
        var dashSlashAnimator = dashSlash.GetComponent<tk2dSpriteAnimator>();
        var defaultClip = dashSlashAnimator.DefaultClip;
        var dashSlashAnimationDuration = defaultClip.frames.Length / defaultClip.fps;

        Object.Destroy(dashSlash, dashSlashAnimationDuration);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
