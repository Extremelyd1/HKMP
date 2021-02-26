using System.Collections;
using HKMP.Networking.Packet;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation {
    public abstract class FireballBase : IAnimationEffect {

        public abstract void Play(GameObject playerObject, Packet packet);
        
        public void Play(
            GameObject playerObject, 
            Packet packet, 
            string fireballParentName, 
            int castFireballIndex, 
            int castAudioIndex, 
            int dungFlukeIndex, 
            int dungFlukeAudioIndex, 
            float baseFireballSize,
            bool noFireballFlip
        ) {
            var hasFlukenestCharm = packet.ReadBool();
            var hasDefenderCrestCharm = packet.ReadBool();
            var hasShamanStoneCharm = packet.ReadBool();

            var playerSpells = playerObject.FindGameObjectInChildren("Spells");

            var spellControl = HeroController.instance.spellControl;
            var fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>(fireballParentName, 3).gameObject.Value;
            var fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            var audioAction = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", castAudioIndex);
            var audioPlayerObj = audioAction.audioPlayer.Value;

            AudioClip castClip;
            if (hasFlukenestCharm) {
                castClip = (AudioClip) fireballCast.GetAction<AudioPlayerOneShotSingle>("Fluke R", 0).audioClip.Value;
                if (hasDefenderCrestCharm) {
                    var dungFlukeObj = fireballCast.GetAction<SpawnObjectFromGlobalPool>("Dung R", dungFlukeIndex)
                        .gameObject.Value;
                    
                    var dungFluke = Object.Instantiate(dungFlukeObj, playerSpells.transform.position,
                        Quaternion.identity);
                    dungFluke.SetActive(true);

                    var localScale = playerObject.transform.localScale;
                    dungFluke.transform.rotation = Quaternion.Euler(0, 0, 26 * -localScale.x);
                    dungFluke.layer = 22;
                    
                    var dungFlukeControl = dungFluke.LocateMyFSM("Control");
                    var blowClip = (AudioClip) dungFlukeControl.GetAction<AudioPlayerOneShotSingle>("Blow", dungFlukeAudioIndex).audioClip.Value;
                    Object.Destroy(dungFlukeControl);
                    
                    // TODO: deal with PvP scenarios
                    
                    CoroutineUtil.Instance.StartCoroutine(StartDungFluke(dungFluke, blowClip));

                    dungFluke.GetComponent<Rigidbody2D>().velocity = new Vector2(
                        Random.Range(5, 15) * -localScale.x, 
                        Random.Range(0, 20)
                    );
                    
                    Object.Destroy(dungFlukeControl);
                    Object.Destroy(dungFluke.FindGameObjectInChildren("Damager"));
                } else {
                    var flukeObject = fireballCast.GetAction<FlingObjectsFromGlobalPool>("Flukes", 0).gameObject.Value;
                    var fluke = Object.Instantiate(flukeObject, playerSpells.transform.position, Quaternion.identity);
                    
                    // TODO: deal with PvP scenarios 

                    var localScale = playerObject.transform.localScale;
                    var config = new FlingUtils.Config {
                        Prefab = fluke,
                        AmountMin = 16,
                        AmountMax = 16,
                        AngleMin = localScale.x < 0 ? 20 : 120,
                        AngleMax = localScale.x < 0 ? 60 : 160,
                        SpeedMin = 14,
                        SpeedMax = 22
                    };
                    
                    FlingUtils.SpawnAndFling(config, playerObject.transform, Vector3.zero);
                }
            } else {
                castClip = (AudioClip) audioAction.audioClip.Value;
                
                var fireballObject = fireballCast.GetAction<SpawnObjectFromGlobalPool>("Cast Right", castFireballIndex).gameObject.Value;
                var fireball = Object.Instantiate(fireballObject, playerSpells.transform.position + Vector3.down * 0.5f, Quaternion.identity);
                fireball.SetActive(true);
                fireball.layer = 22;

                var fireballComponent = fireball.AddComponent<Fireball>();
                fireballComponent.xDir = -playerObject.transform.localScale.x;

                fireballComponent.hasShamanStoneCharm = hasShamanStoneCharm;
                fireballComponent.baseFireballSize = baseFireballSize;
                fireballComponent.noFireballFlip = noFireballFlip;
            }
            
            var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
            audioPlayer.GetComponent<AudioSource>().PlayOneShot(castClip);
        }

        public void PreparePacket(Packet packet) {
            var playerData = PlayerData.instance;
            // Write charm values to the packet
            packet.Write(playerData.equippedCharm_11); // Flukenest
            packet.Write(playerData.equippedCharm_10); // Defender's Crest
            packet.Write(playerData.equippedCharm_19); // Shaman Stone
        }

        private IEnumerator StartDungFluke(GameObject dungFluke, AudioClip blowClip) {
            var spriteAnimator = dungFluke.GetComponent<tk2dSpriteAnimator>();
            var audioSource = dungFluke.GetComponent<AudioSource>();

            spriteAnimator.Play("Dung Air");
            audioSource.Play();

            yield return new WaitForSeconds(1.0f);

            spriteAnimator.Play("Dung Antic");
            dungFluke.FindGameObjectInChildren("Pt Antic").GetComponent<ParticleSystem>().Play();

            yield return new WaitForSeconds(1.0f);

            var dungCloudObject = dungFluke.FindGameObjectInChildren("Knight Dung Cloud");
            var dungCloud = Object.Instantiate(
                dungCloudObject,
                dungFluke.transform.position,
                Quaternion.identity
            );
            
            dungCloud.SetActive(true);
            dungCloud.layer = 22;
            
            Object.Destroy(dungCloud.GetComponent<DamageEffectTicker>());

            dungCloud.LocateMyFSM("Control").SetState("Collider On");
            dungCloud.AddComponent<AudioSource>().PlayOneShot(blowClip);
            
            // TODO: deal with PvP scenarios

            Object.Destroy(dungFluke);

            yield return new WaitForSeconds(3.0f);

            Object.Destroy(dungCloud);
        }
    }

    public class Fireball : MonoBehaviour {
        public float xDir;
        public bool hasShamanStoneCharm;
        public float baseFireballSize;
        public bool noFireballFlip;
        
        private const float FireballSpeed = 45;
        
        private tk2dSpriteAnimator _anim;
        private Rigidbody2D _rb;

        private void Awake() {
            _anim = GetComponent<tk2dSpriteAnimator>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start() {
            _anim.PlayFromFrame(0);
            _rb.velocity = Vector2.right * FireballSpeed * xDir;
            
            // TODO: deal with PvP scenarios

            if (noFireballFlip) {
                xDir = 1;
            }
            
            if (hasShamanStoneCharm) {
                transform.localScale = new Vector3(xDir * baseFireballSize * 1.3f, baseFireballSize * 1.6f, 0);
            } else {
                transform.localScale = new Vector3(xDir * baseFireballSize, baseFireballSize, 0);
            }

            Destroy(gameObject, 2);
        }
    }
}