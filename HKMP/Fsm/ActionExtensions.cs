using System;
using System.Reflection;
using Hkmp.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Hkmp.Fsm {
    public static class ActionExtensions {
        private static readonly BindingFlags BindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod;

        private static void ExecuteAction(FsmStateAction action) {
            var type = action.GetType();
            
            var methodInfo = typeof(ActionExtensions).GetMethod("Execute", new [] {type});
            if (methodInfo == null) {
                Logger.Get().Warn("ActionExtensions", $"Could not find Execute method for type: {type}");
                return;
            }

            methodInfo.Invoke(null, new object[] {action});
        }

        private static void ExecuteAction(this PlayMakerFSM fsm, string stateName, int actionIndex) {
            var action = fsm.GetAction(stateName, actionIndex);
            if (action == null) {
                Logger.Get().Warn("ActionExtensions", $"Could not find action of FSM for state name: {stateName} and index: {actionIndex}");
                return;
            }

            ExecuteAction(action);
        }

        public static void ExecuteActions(this PlayMakerFSM fsm, string stateName, params int[] actionIndices) {
            foreach (var index in actionIndices) {
                ExecuteAction(fsm, stateName, index);
            }
        }
        
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

                ReflectionHelper.SetField(instance, "vectorX", vectorX);
                ReflectionHelper.SetField(instance, "vectorY", vectorY);
                
                var rb2d = ReflectionHelper.GetField<RigidBody2dActionBase, Rigidbody2D>(
                    instance,
                    "rb2d"
                );

                rb2d.velocity = new Vector2(
                    vectorX,
                    vectorY
                );
            }
        }

        public static void Execute(this GetOwner instance) {
            instance.storeGameObject.Value = instance.Owner;
        }

        public static void Execute(this SetCollider instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);
            var boxCollider = ownerDefaultTarget.GetComponent<BoxCollider2D>();

            if (boxCollider != null) {
                boxCollider.enabled = instance.active.Value;
            }
        }

        public static void Execute(this Tk2dPlayAnimationWithEvents instance) {
            // We can't use an optional parameter in the other method since we need to call it with reflection
            Execute(instance, null);
        }

        public static void Execute(this Tk2dPlayAnimationWithEvents instance, Action animationCompleteAction) {
            var animator = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject).GetComponent<tk2dSpriteAnimator>();
            animator.Play(instance.clipName.Value);

            if (animationCompleteAction != null) {
                animator.AnimationCompleted = (spriteAnimator, clip) => {
                    animationCompleteAction();

                    animator.AnimationCompleted = null;
                };
            }
        }

        public static void Execute(this Tk2dPlayFrame instance) {
            var animator = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject).GetComponent<tk2dSpriteAnimator>();
            animator.PlayFromFrame(instance.frame.Value);
        }

        public static void Execute(this SetMeshRenderer instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);
            var meshRenderer = ownerDefaultTarget.GetComponent<MeshRenderer>();

            meshRenderer.enabled = instance.active.Value;
        }

        public static void Execute(this PlayParticleEmitter instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);
            var particleSystem = ownerDefaultTarget.GetComponent<ParticleSystem>();

            if (particleSystem != null && !particleSystem.isPlaying && instance.emit.Value <= 0) {
                particleSystem.Play();
            } else if (instance.emit.Value > 0) {
                particleSystem.Emit(instance.emit.Value);
            }
        }

        public static void Execute(this SetIsKinematic2d instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);
            var rigidBody = ownerDefaultTarget.GetComponent<Rigidbody2D>();

            rigidBody.isKinematic = instance.isKinematic.Value;
        }

        public static void Execute(this SetInvincible instance) {
            var target = instance.target.GetSafe(instance);
            var healthManager = target.GetComponent<HealthManager>();

            if (!instance.Invincible.IsNone) {
                healthManager.IsInvincible = instance.Invincible.Value;
            }

            if (!instance.InvincibleFromDirection.IsNone) {
                healthManager.InvincibleFromDirection = instance.InvincibleFromDirection.Value;
            }
        }

        public static void Execute(this SetDamageHeroAmount instance) {
            var target = instance.target.GetSafe(instance);
            var damageHero = target.GetComponent<DamageHero>();

            if (!instance.damageDealt.IsNone) {
                damageHero.damageDealt = instance.damageDealt.Value;
            }
        }

        public static void Execute(this SpawnFromPool instance) {
            var pool = instance.pool.Value;
            var num = Random.Range(instance.spawnMin.Value, instance.spawnMax.Value + 1);

            for (var i = 1; i <= num; i++) {
                var childCount = pool.transform.childCount;
                if (childCount == 0) {
                    return;
                }

                var child = pool.transform.GetChild(Random.Range(0, childCount)).gameObject;
                child.SetActive(true);

                var speed = Random.Range(instance.speedMin.Value, instance.speedMax.Value);
                var angle = Random.Range(instance.angleMin.Value, instance.angleMax.Value);

                var velocity = new Vector2(
                    speed * Mathf.Cos(angle * ((float) System.Math.PI / 180f)),
                    speed * Mathf.Sin(angle * ((float) System.Math.PI / 180f))
                );

                var rigidBody = child.GetComponent<Rigidbody2D>();
                rigidBody.velocity = velocity;

                if (!instance.adjustPosition.IsNone) {
                    child.transform.position += instance.adjustPosition.Value;
                }

                child.transform.parent = null;
            }
        }

        public static void Execute(this StopParticleEmitter instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);

            var particleSystem = ownerDefaultTarget.GetComponent<ParticleSystem>();

            if (particleSystem.isPlaying) {
                particleSystem.Stop();
            }
        }

        public static void Execute(this Tk2dWatchAnimationEvents instance, Action animationCompleteAction) {
            var animator = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject).GetComponent<tk2dSpriteAnimator>();
            animator.AnimationCompleted = (spriteAnimator, clip) => {
                animationCompleteAction();

                animator.AnimationCompleted = null;
            };
        }

        public static void Execute(this SetBoolValue instance) {
            instance.boolVariable.Value = instance.boolValue.Value;
        }

        public static void Execute(this AudioPlayerOneShot instance) {
            typeof(AudioPlayerOneShot).InvokeMember(
                "DoPlayRandomClip",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this AudioPlaySimple instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);
            var audio = ownerDefaultTarget.GetComponent<AudioSource>();
            var clip = (AudioClip) instance.oneShotClip.Value;

            if (clip == null) {
                if (!audio.isPlaying) {
                    audio.Play();
                }

                if (!instance.volume.IsNone) {
                    audio.volume = instance.volume.Value;
                }
            }

            if (!instance.volume.IsNone) {
                audio.PlayOneShot(clip, instance.volume.Value);
            } else {
                audio.PlayOneShot(clip);
            }
        }

        public static void Execute(this SpawnObjectFromGlobalPool instance) {
            var position = Vector3.zero;
            var euler = Vector3.up;

            if (instance.spawnPoint.Value != null) {
                position = instance.spawnPoint.Value.transform.position;
                if (!instance.position.IsNone) {
                    position += instance.position.Value;
                }

                euler = instance.rotation.IsNone
                    ? instance.spawnPoint.Value.transform.eulerAngles
                    : instance.rotation.Value;
            } else {
                if (!instance.position.IsNone) {
                    position = instance.position.Value;
                }

                if (!instance.rotation.IsNone) {
                    euler = instance.rotation.Value;
                }
            }

            if (instance.gameObject != null) {
                instance.storeObject.Value = instance.gameObject.Value.Spawn(position, Quaternion.Euler(euler));
            }
        }

        public static void Execute(this RandomInt instance) {
            instance.storeResult.Value = !instance.inclusiveMax
                ? Random.Range(instance.min.Value, instance.max.Value)
                : Random.Range(instance.min.Value, instance.max.Value + 1);
        }

        public static void Execute(this SetFsmInt instance) {
            typeof(SetFsmInt).InvokeMember(
                "DoSetFsmInt",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetVector3XYZ instance) {
            typeof(SetVector3XYZ).InvokeMember(
                "DoSetVector3XYZ",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetScale instance) {
            typeof(SetScale).InvokeMember(
                "DoSetScale",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetFsmFloat instance) {
            typeof(SetFsmFloat).InvokeMember(
                "DoSetFsmFloat",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetParticleEmissionRate instance) {
            typeof(SetParticleEmissionRate).InvokeMember(
                "DoSetEmitRate",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetPosition instance) {
            typeof(SetPosition).InvokeMember(
                "DoSetPosition",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetPlayerDataBool instance) {
            GameManager.instance.SetPlayerDataBool(instance.boolName.Value, instance.value.Value);
        }

        public static void Execute(this SpawnBlood instance) {
            typeof(SpawnBlood).InvokeMember(
                "Spawn",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetPlayerDataInt instance) {
            GameManager.instance.SetPlayerDataInt(instance.intName.Value, instance.value.Value);
        }

        public static void Execute(this SpawnBloodTime instance) {
            Execute((SpawnBlood) instance);
        }

        public static void Execute(this SetParticleEmissionSpeed instance) {
            var emitter = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject).GetComponent<ParticleSystem>();

#pragma warning disable 618
            emitter.startSpeed = instance.emissionSpeed.Value;
#pragma warning restore 618
        }

        public static void Execute(this FloatClamp instance) {
            typeof(FloatClamp).InvokeMember(
                "DoClamp",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this FloatAdd instance) {
            typeof(FloatAdd).InvokeMember(
                "DoFloatAdd",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetParent instance) {
            var ownerDefaultTarget = instance.Fsm.GetOwnerDefaultTarget(instance.gameObject);

            ownerDefaultTarget.transform.parent =
                instance.parent.Value != null ? instance.parent.Value.transform : null;

            if (instance.resetLocalPosition.Value) {
                ownerDefaultTarget.transform.localPosition = Vector3.zero;
            }

            if (instance.resetLocalRotation.Value) {
                ownerDefaultTarget.transform.localRotation = Quaternion.identity;
            }
        }

        public static void Execute(this IntAdd instance) {
            instance.intVariable.Value += instance.add.Value;
        }

        public static void Execute(this SetIntValue instance) {
            instance.intVariable.Value = instance.intValue.Value;
        }

        public static void Execute(this IntOperator instance) {
            typeof(IntOperator).InvokeMember(
                "DoIntOperator",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this FlingObjectsFromGlobalPool instance) {
            var position = Vector3.zero;

            if (instance.spawnPoint.Value != null) {
                position = instance.spawnPoint.Value.transform.position;

                if (!instance.position.IsNone) {
                    position += instance.position.Value;
                }
            } else if (!instance.position.IsNone) {
                position = instance.position.Value;
            }

            var num = Random.Range(instance.spawnMin.Value, instance.spawnMax.Value + 1);
            for (var i = 1; i <= num; i++) {
                var go = instance.gameObject.Value.Spawn(position, Quaternion.Euler(Vector3.zero));

                var goTransform = go.transform;
                var goPosition = goTransform.position;
                
                var x = goPosition.x;
                var y = goPosition.y;
                var z = goPosition.z;
                if (instance.originVariationX != null) {
                    x += Random.Range(-instance.originVariationX.Value, instance.originVariationX.Value);
                }

                if (instance.originVariationY != null) {
                    y += Random.Range(-instance.originVariationY.Value, instance.originVariationY.Value);
                }

                goTransform.position = new Vector3(x, y, z);
                
                var speed = Random.Range(instance.speedMin.Value, instance.speedMax.Value);
                var angle = Random.Range(instance.angleMin.Value, instance.angleMax.Value);

                var velocity = new Vector2(
                    speed * Mathf.Cos(angle * ((float) System.Math.PI / 180f)),
                    speed * Mathf.Sin(angle * ((float) System.Math.PI / 180f))
                );

                var rigidBody = go.GetComponent<Rigidbody2D>();
                rigidBody.velocity = velocity;

                if (!instance.FSM.IsNone) {
                    FSMUtility.LocateFSM(go, instance.FSM.Value).SendEvent(instance.FSMEvent.Value);
                }
            }
        }

        public static void Execute(this FindChild instance) {
            typeof(FindChild).InvokeMember(
                "DoFindChild",
                BindingFlags,
                null,
                instance,
                null
            );

            if (instance.childName.Value.Equals("Attack Range")) {
                Logger.Get().Info("ActionExtensions", "Just executed FindChild for Attack Range");

                var storeResultValue = instance.storeResult.Value;

                if (storeResultValue == null) {
                    Logger.Get().Info("ActionExtensions", "  storeResultValue is null");
                } else {
                    Logger.Get().Info("ActionExtensions", $"  storeResultValue is {storeResultValue.name}");
                }
            }
        }

        public static void Execute(this FindAlertRange instance) {
            instance.storeResult.Value = AlertRange.Find(instance.target.GetSafe(instance), instance.childName);
            
            Logger.Get().Info("ActionExtension", $"FindAlertRange result: {instance.storeResult.Value == null}");
        }

        public static void Execute(this GetHero instance) {
            var hc = HeroController.instance;
            instance.storeResult.Value = hc == null ? null : hc.gameObject;
        }

        public static void Execute(this GetPosition instance) {
            typeof(GetPosition).InvokeMember(
                "DoGetPosition",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this BoundsBoxCollider instance) {
            if (instance.gameObject1 == null) {
                Logger.Get().Warn("ActionExtensions", "Executing BoundsBoxCollider, but gameObject1 is null");
                return;
            }
            
            instance.GetEm();
        }

        public static void Execute(this FloatDivide instance) {
            instance.floatVariable.Value /= instance.divideBy.Value;
        }

        public static void Execute(this SetFloatValue instance) {
            instance.floatVariable.Value = instance.floatValue.Value;
        }

        public static void Execute(this FloatOperator instance) {
            typeof(FloatOperator).InvokeMember(
                "DoFloatOperator",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this SetAudioPitch instance) {
            typeof(SetAudioPitch).InvokeMember(
                "DoSetAudioPitch",
                BindingFlags,
                null,
                instance,
                null
            );
        }

        public static void Execute(this AudioPlayRandom instance) {
            typeof(AudioPlayRandom).InvokeMember(
                "DoPlayRandomClip",
                BindingFlags,
                null,
                instance,
                null
            );
        }
    }
}