using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using UnityEngine;
using UnityEngine.Audio;

namespace Hkmp.Fsm {
    public static class ActionExtensions {
        private static readonly BindingFlags BindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod;

        public static void Execute(this SetGameObject instance) {
            instance.variable.Value = instance.gameObject.Value;
        }

        /**
         * Only works with single activations, no recursive, reset on exit or every frame instances
         */
        public static void Execute(this ActivateGameObject instance) {
            instance.gameObject.GameObject.Value.SetActive(instance.activate.Value);
        }

        public static void Execute(this SetFsmBool instance) {
            typeof(SetFsmBool).InvokeMember(
                "DoSetFsmBool",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetFsmString instance) {
            typeof(SetFsmString).InvokeMember(
                "DoSetFsmString",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this DestroyObject instance) {
            if (instance.delay.Value <= 0.0) {
                Object.Destroy(instance.gameObject.Value);
            } else {
                Object.Destroy(instance.gameObject.Value, instance.delay.Value);
            }

            if (instance.detachChildren.Value) {
                instance.gameObject.Value.transform.DetachChildren();
            }
        }

        public static void Execute(this SendEventByName instance) {
            if (instance.delay.Value < 1.0 / 1000.0) {
                instance.Fsm.Event(instance.eventTarget, instance.sendEvent.Value);
            } else {
                instance.Fsm.DelayedEvent(
                    instance.eventTarget,
                    FsmEvent.GetFsmEvent(instance.sendEvent.Value),
                    instance.delay.Value
                );
            }
        }

        public static void Execute(this Tk2dPlayAnimation instance) {
            var animator = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject).GetComponent<tk2dSpriteAnimator>();
            animator.Play(instance.clipName.Value);
        }

        public static void Execute(this AudioPlay instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);
            var audio = ownerDefaultTarget.GetComponent<AudioSource>();
            var clip = instance.oneShotClip.Value as AudioClip;

            if (clip == null) {
                audio.Play();

                if (!instance.volume.IsNone) {
                    audio.volume = instance.volume.Value;
                }

                return;
            }

            if (instance.volume.IsNone) {
                audio.PlayOneShot(clip);
                return;
            }
            
            audio.PlayOneShot(clip, instance.volume.Value);
        }

        public static void Execute(this TransitionToAudioSnapshot instance) {
            var audioMixerSnapshot = (AudioMixerSnapshot) instance.snapshot.Value;
            audioMixerSnapshot.TransitionTo(instance.transitionTime.Value);
        }

        public static void Execute(this ApplyMusicCue instance) {
            var musicCue = (MusicCue) instance.musicCue.Value;
            var gm = GameManager.instance;
            
            gm.AudioManager.ApplyMusicCue(
                musicCue, 
                instance.delayTime.Value,
                instance.transitionTime.Value,
                false
            );
        }

        public static void Execute(this SetAudioClip instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);
            var audioSource = ownerDefaultTarget.GetComponent<AudioSource>();
            audioSource.clip = (AudioClip) instance.audioClip.Value;
        }

        public static void Execute(this AudioStop instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);
            var audioSource = ownerDefaultTarget.GetComponent<AudioSource>();
            audioSource.Stop();
        }

        public static void Execute(this AudioPlayerOneShotSingle instance) {
            var audioPlayerObject = instance.audioPlayer.Value.Spawn(
                instance.spawnPoint.Value.transform.position,
                Quaternion.Euler(Vector3.up)
            );
            var audioSource = audioPlayerObject.GetComponent<AudioSource>();
            var audioClip = (AudioClip) instance.audioClip.Value;

            audioSource.pitch = Random.Range(
                instance.pitchMin.Value,
                instance.pitchMax.Value
            );
            audioSource.volume = instance.volume.Value;

            audioSource.PlayOneShot(audioClip);
        }

        public static void Execute(this CreateObject instance) {
            var original = instance.gameObject.Value;

            var position = Vector3.zero;
            var euler = Vector3.zero;

            if (instance.spawnPoint.Value != null) {
                position = instance.spawnPoint.Value.transform.position;
                if (!instance.position.IsNone) {
                    position += instance.position.Value;
                }

                if (instance.rotation.IsNone) {
                    euler = instance.spawnPoint.Value.transform.eulerAngles;
                } else {
                    euler = instance.rotation.Value;
                }
            } else {
                if (!instance.position.IsNone) {
                    position = instance.position.Value;
                }

                if (!instance.rotation.IsNone) {
                    euler = instance.rotation.Value;
                }
            }

            instance.storeObject.Value = Object.Instantiate(
                original,
                position,
                Quaternion.Euler(euler)
            );
        }

        public static void Execute(this SpawnRandomObjects instance) {
            var original = instance.gameObject.Value;

            var position = Vector3.zero;

            if (instance.spawnPoint.Value != null) {
                position = instance.spawnPoint.Value.transform.position;
                if (!instance.position.IsNone) {
                    position += instance.position.Value;
                }
            } else if (!instance.position.IsNone) {
                position = instance.position.Value;
            }

            var spawnNum = Random.Range(instance.spawnMin.Value, instance.spawnMax.Value + 1);
            for (var i = 1; i <= spawnNum; i++) {
                var go = Object.Instantiate(
                    original,
                    position,
                    Quaternion.Euler(Vector3.zero)
                );

                if (instance.originVariation != null) {
                    var goPosition = go.transform.position;
                    
                    var x = goPosition.x + Random.Range(
                        -instance.originVariation.Value, 
                        instance.originVariation.Value
                    );
                    var y = goPosition.y + Random.Range(
                        -instance.originVariation.Value, 
                        instance.originVariation.Value
                    );
                    var z = goPosition.z;
                    go.transform.position = new Vector3(x, y, z);
                }
                
                typeof(RigidBody2dActionBase).InvokeMember(
                    "CacheRigidBody2d",
                    BindingFlags,
                    null,
                    instance,
                    new object[] {
                        go
                    }
                );
                
                var num2 = Random.Range(
                    instance.speedMin.Value,
                    instance.speedMax.Value
                );
                var num3 = Random.Range(
                    instance.angleMin.Value, 
                    instance.angleMax.Value
                );

                var vectorX = num2 * Mathf.Cos(num3 * ((float) System.Math.PI / 180f));
                var vectorY = num2 * Mathf.Sin(num3 * ((float) System.Math.PI / 180f));

                ReflectionHelper.SetAttr(instance, "vectorX", vectorX);
                ReflectionHelper.SetAttr(instance, "vectorY", vectorY);
                
                var rb2d = ReflectionHelper.GetAttr<RigidBody2dActionBase, Rigidbody2D>(
                    instance,
                    "rb2d"
                );

                rb2d.velocity = new Vector2(
                    vectorX,
                    vectorY
                );
            }
        }
    }
}