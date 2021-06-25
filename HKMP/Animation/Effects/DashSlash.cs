using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class DashSlash : DamageAnimationEffect {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Obtain the Nail Arts FSM from the Hero Controller
            var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");

            // Get an audio source relative to the player
            var audioObject = AudioUtil.GetAudioSourceObject(playerObject);
            var audioSource = audioObject.GetComponent<AudioSource>();

            // Get the audio clip of the Great Slash
            var dashSlashClip = (AudioClip) nailArts.GetAction<AudioPlay>("Dash Slash", 1).oneShotClip.Value;
            audioSource.PlayOneShot(dashSlashClip);

            Object.Destroy(audioObject, dashSlashClip.length);

            // Get the attacks gameObject from the player object
            var localPlayerAttacks = HeroController.instance.gameObject.FindGameObjectInChildren("Attacks");
            var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");

            // Get the prefab for the Dash Slash and instantiate it relative to the remote player object
            var dashSlashObject = localPlayerAttacks.FindGameObjectInChildren("Dash Slash");
            var dashSlash = Object.Instantiate(
                dashSlashObject,
                playerAttacks.transform
            );
            
            ChangeAttackTypeOfFsm(dashSlash);

            // For some reason the bounds of the original collider are incorrect, making the area of the collider
            // 0, so we recreate the collider with the original points, which will recalculate the bounds correctly
            var collider = dashSlash.GetComponent<PolygonCollider2D>();
            var colliderPoints = collider.points;

            Object.Destroy(collider);
            
            var newCollider = dashSlash.AddComponent<PolygonCollider2D>();
            newCollider.points = colliderPoints;
            newCollider.isTrigger = true;

            // Remove audio source component that exists on the dash slash object
            Object.Destroy(dashSlash.GetComponent<AudioSource>());

            dashSlash.SetActive(true);
            
            // Set the newly instantiate collider to state Init, to reset it
            // in case the local player was already performing it
            dashSlash.LocateMyFSM("Control Collider").SetState("Init");

            var damage = GameSettings.DashSlashDamage;
            if (GameSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
                dashSlash.AddComponent<DamageHero>().damageDealt = damage;
            }

            // Get the animator, figure out the duration of the animation and destroy the object accordingly afterwards
            var dashSlashAnimator = dashSlash.GetComponent<tk2dSpriteAnimator>();
            var dashSlashAnimationDuration = dashSlashAnimator.DefaultClip.frames.Length / dashSlashAnimator.ClipFps;

            Object.Destroy(dashSlash, dashSlashAnimationDuration);
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}