using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnums;
using HKMP.Animation.Effects;
using HKMP.Fsm;
using HKMP.Game;
using HKMP.Game.Client;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using HKMP.ServerKnights;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HKMP.Animation {
    /**
     * Class that manages all forms of animation from clients.
     */
    public class AnimationManager {
        public const float EffectDistanceThreshold = 25f;

        // Animations that are allowed to loop, because they need to transmit the effect
        private static readonly string[] AllowedLoopAnimations = {"Focus Get", "Run"};

        private static readonly string[] AnimationControllerClipNames = {
            "Airborne"
        };

        // Initialize animation effects that are used for different keys
        public static readonly CrystalDashChargeCancel CrystalDashChargeCancel = new CrystalDashChargeCancel();

        private static readonly Focus Focus = new Focus();
        private static readonly FocusBurst FocusBurst = new FocusBurst();

        public static readonly FocusEnd FocusEnd = new FocusEnd();

        private static readonly Dictionary<string, AnimationClip> ClipEnumNames = new Dictionary<string, AnimationClip> {
            {"Idle", AnimationClip.Idle},
            {"Dash", AnimationClip.Dash},
            {"Airborne", AnimationClip.Airborne},
            {"SlashAlt", AnimationClip.SlashAlt},
            {"Run", AnimationClip.Run},
            {"Slash", AnimationClip.Slash},
            {"SlashEffect", AnimationClip.SlashEffect},
            {"SlashEffectAlt", AnimationClip.SlashEffectAlt},
            {"UpSlash", AnimationClip.UpSlash},
            {"DownSlash", AnimationClip.DownSlash},
            {"Land", AnimationClip.Land},
            {"HardLand", AnimationClip.HardLand},
            {"LookDown", AnimationClip.LookDown},
            {"LookUp", AnimationClip.LookUp},
            {"UpSlashEffect", AnimationClip.UpSlashEffect},
            {"DownSlashEffect", AnimationClip.DownSlashEffect},
            {"Death", AnimationClip.Death},
            {"SD Crys Grow", AnimationClip.SDCrysGrow},
            {"Death Head Normal", AnimationClip.DeathHeadNormal},
            {"Death Head Cracked", AnimationClip.DeathHeadCracked},
            {"Recoil", AnimationClip.Recoil},
            {"DN Charge", AnimationClip.DNCharge},
            {"Lantern Idle", AnimationClip.LanternIdle},
            {"Acid Death", AnimationClip.AcidDeath},
            {"Spike Death", AnimationClip.SpikeDeath},
            {"Hit Crack Appear", AnimationClip.HitCrackAppear},
            {"DN Cancel", AnimationClip.DNCancel},
            {"Collect Magical 1", AnimationClip.CollectMagical1},
            {"Collect Magical 2", AnimationClip.CollectMagical2},
            {"Collect Magical 3", AnimationClip.CollectMagical3},
            {"Collect Normal 1", AnimationClip.CollectNormal1},
            {"Collect Normal 2", AnimationClip.CollectNormal2},
            {"Collect Normal 3", AnimationClip.CollectNormal3},
            {"NA Charge", AnimationClip.NACharge},
            {"Idle Wind", AnimationClip.IdleWind},
            {"Enter", AnimationClip.Enter},
            {"Roar Lock", AnimationClip.RoarLock},
            {"Sit", AnimationClip.Sit},
            {"Sit Lean", AnimationClip.SitLean},
            {"Wake", AnimationClip.Wake},
            {"Get Off", AnimationClip.GetOff},
            {"Sitting Asleep", AnimationClip.SittingAsleep},
            {"Prostrate", AnimationClip.Prostrate},
            {"Prostrate Rise", AnimationClip.ProstrateRise},
            {"Stun", AnimationClip.Stun},
            {"Turn", AnimationClip.Turn},
            {"Run To Idle", AnimationClip.RunToIdle},
            {"Focus", AnimationClip.Focus},
            {"Focus Get", AnimationClip.FocusGet},
            {"Focus End", AnimationClip.FocusEnd},
            {"Wake Up Ground", AnimationClip.WakeUpGround},
            {"Focus Get Once", AnimationClip.FocusGetOnce},
            {"Hazard Respawn", AnimationClip.HazardRespawn},
            {"Walk", AnimationClip.Walk},
            {"Respawn Wake", AnimationClip.RespawnWake},
            {"Sit Fall Asleep", AnimationClip.SitFallAsleep},
            {"Map Idle", AnimationClip.MapIdle},
            {"Map Away", AnimationClip.MapAway},
            {"Fall", AnimationClip.Fall},
            {"TurnToIdle", AnimationClip.TurnToIdle},
            {"Collect Magical Fall", AnimationClip.CollectMagicalFall},
            {"Collect Magical Land", AnimationClip.CollectMagicalLand},
            {"LookUpEnd", AnimationClip.LookUpEnd},
            {"Idle Hurt", AnimationClip.IdleHurt},
            {"LookDownEnd", AnimationClip.LookDownEnd},
            {"Collect Heart Piece", AnimationClip.CollectHeartPiece},
            {"Collect Heart Piece End", AnimationClip.CollectHeartPieceEnd},
            {"Fireball1 Cast", AnimationClip.Fireball1Cast},
            {"Fireball Antic", AnimationClip.FireballAntic},
            {"SD Crys Idle", AnimationClip.SDCrysIdle},
            {"Scream 2 Get", AnimationClip.Scream2Get},
            {"Sit Map Open", AnimationClip.SitMapOpen},
            {"Wake To Sit", AnimationClip.WakeToSit},
            {"Sit Map Close", AnimationClip.SitMapClose},
            {"Dash To Idle", AnimationClip.DashToIdle},
            {"Dash Down", AnimationClip.DashDown},
            {"Dash Down Land", AnimationClip.DashDownLand},
            {"Dash Effect", AnimationClip.DashEffect},
            {"Lantern Run", AnimationClip.LanternRun},
            {"Wall Slide", AnimationClip.WallSlide},
            {"SD Charge Ground", AnimationClip.SDChargeGround},
            {"SD Dash", AnimationClip.SDDash},
            {"SD Fx Charge", AnimationClip.SDFxCharge},
            {"SD Charge Ground End", AnimationClip.SDChargeGroundEnd},
            {"SD Fx Bling", AnimationClip.SDFxBling},
            {"SD Fx Burst", AnimationClip.SDFxBurst},
            {"SD Hit Wall", AnimationClip.SDHitWall},
            {"Quake Antic", AnimationClip.QuakeAntic},
            {"Quake Fall", AnimationClip.QuakeFall},
            {"Quake Land", AnimationClip.QuakeLand},
            {"Map Walk", AnimationClip.MapWalk},
            {"SD Air Brake", AnimationClip.SDAirBrake},
            {"SD Wall Charge", AnimationClip.SDWallCharge},
            {"Double Jump", AnimationClip.DoubleJump},
            {"Double Jump Wings 2", AnimationClip.DoubleJumpWings2},
            {"Fireball2 Cast", AnimationClip.Fireball2Cast},
            {"Map Open", AnimationClip.MapOpen},
            {"NA Big Slash", AnimationClip.NABigSlash},
            {"NA Big Slash Effect", AnimationClip.NABigSlashEffect},
            {"TurnToBG", AnimationClip.TurnToBG},
            {"NA Charged Effect", AnimationClip.NAChargedEffect},
            {"NA Cyclone", AnimationClip.NACyclone},
            {"NA Cyclone End", AnimationClip.NACycloneEnd},
            {"NA Cyclone Start", AnimationClip.NACycloneStart},
            {"Quake Fall 2", AnimationClip.QuakeFall2},
            {"Quake Land 2", AnimationClip.QuakeLand2},
            {"Scream", AnimationClip.Scream},
            {"Scream End 2", AnimationClip.ScreamEnd2},
            {"Scream End", AnimationClip.ScreamEnd},
            {"Scream Start", AnimationClip.ScreamStart},
            {"Scream 2", AnimationClip.Scream2},
            {"SD Break", AnimationClip.SDBreak},
            {"SD Trail", AnimationClip.SDTrail},
            {"SD Trail End", AnimationClip.SDTrailEnd},
            {"Shadow Dash", AnimationClip.ShadowDash},
            {"Shadow Dash Burst", AnimationClip.ShadowDashBurst},
            {"Shadow Dash Down", AnimationClip.ShadowDashDown},
            {"Shadow Recharge", AnimationClip.ShadowRecharge},
            {"Wall Slash", AnimationClip.WallSlash},
            {"DG Set Charge", AnimationClip.DGSetCharge},
            {"Cyclone Effect", AnimationClip.CycloneEffect},
            {"Cyclone Effect End", AnimationClip.CycloneEffectEnd},
            {"Surface Swim", AnimationClip.SurfaceSwim},
            {"Surface Idle", AnimationClip.SurfaceIdle},
            {"Surface In", AnimationClip.SurfaceIn},
            {"DG Set End", AnimationClip.DGSetEnd},
            {"Walljump", AnimationClip.Walljump},
            {"Walljump Puff", AnimationClip.WalljumpPuff},
            {"LookUpToIdle", AnimationClip.LookUpToIdle},
            {"ToProne", AnimationClip.ToProne},
            {"GetUpToIdle", AnimationClip.GetUpToIdle},
            {"Challenge Start", AnimationClip.ChallengeStart},
            {"Collect Magical 3b", AnimationClip.CollectMagical3b},
            {"Challenge End", AnimationClip.ChallengeEnd},
            {"Collect SD 1", AnimationClip.CollectSD1},
            {"Collect SD 2", AnimationClip.CollectSD2},
            {"Collect SD 3", AnimationClip.CollectSD3},
            {"Collect SD 4", AnimationClip.CollectSD4},
            {"Thorn Attack", AnimationClip.ThornAttack},
            {"DN Slash Antic", AnimationClip.DNSlashAntic},
            {"DN Slash", AnimationClip.DNSlash},
            {"Collect Shadow", AnimationClip.CollectShadow},
            {"NA Dash Slash", AnimationClip.NADashSlash},
            {"NA Dash Slash Effect", AnimationClip.NADashSlashEffect},
            {"Slug Idle", AnimationClip.SlugIdle},
            {"UpSlashEffect M", AnimationClip.UpSlashEffectM},
            {"DownSlashEffect M", AnimationClip.DownSlashEffectM},
            {"SlashEffect M", AnimationClip.SlashEffectM},
            {"Slug Walk Quick", AnimationClip.SlugWalkQuick},
            {"Slug Turn", AnimationClip.SlugTurn},
            {"SlashEffectAlt M", AnimationClip.SlashEffectAltM},
            {"SlashEffect F", AnimationClip.SlashEffectF},
            {"SlashEffectAlt F", AnimationClip.SlashEffectAltF},
            {"UpSlashEffect F", AnimationClip.UpSlashEffectF},
            {"DownSlashEffect F", AnimationClip.DownSlashEffectF},
            {"DN Start", AnimationClip.DNStart},
            {"Death Dream", AnimationClip.DeathDream},
            {"Dreamer Land", AnimationClip.DreamerLand},
            {"Dreamer Lift", AnimationClip.DreamerLift},
            {"SD Crys Flash", AnimationClip.SDCrysFlash},
            {"SD Crys Shrink", AnimationClip.SDCrysShrink},
            {"DJ Get Land", AnimationClip.DJGetLand},
            {"Collect Acid", AnimationClip.CollectAcid},
            {"Shadow Dash Sharp", AnimationClip.ShadowDashSharp},
            {"Shadow Dash Down Sharp", AnimationClip.ShadowDashDownSharp},
            {"Slug Burst", AnimationClip.SlugBurst},
            {"Slug Down", AnimationClip.SlugDown},
            {"Slug Up", AnimationClip.SlugUp},
            {"Slug Turn Quick", AnimationClip.SlugTurnQuick},
            {"Slug Walk", AnimationClip.SlugWalk},
            {"Slug Idle S", AnimationClip.SlugIdleS},
            {"Slug Idle B", AnimationClip.SlugIdleB},
            {"Slug Idle BS", AnimationClip.SlugIdleBS},
            {"Slug Turn B", AnimationClip.SlugTurnB},
            {"Slug Turn B Quick", AnimationClip.SlugTurnBQuick},
            {"Slug Turn S", AnimationClip.SlugTurnS},
            {"Slug Turn S Quick", AnimationClip.SlugTurnSQuick},
            {"Slug Turn BS", AnimationClip.SlugTurnBS},
            {"Map Update", AnimationClip.MapUpdate},
            {"Slug Burst B", AnimationClip.SlugBurstB},
            {"Slug Burst S", AnimationClip.SlugBurstS},
            {"Slug Burst BS", AnimationClip.SlugBurstBS},
            {"Slug Walk B", AnimationClip.SlugWalkB},
            {"Slug Walk B Quick", AnimationClip.SlugWalkBQuick},
            {"Slug Walk S", AnimationClip.SlugWalkS},
            {"Slug Walk S Quick", AnimationClip.SlugWalkSQuick},
            {"Sit Idle", AnimationClip.SitIdle},
            {"Slug Walk BS", AnimationClip.SlugWalkBS},
            {"Slug Walk BS Quick", AnimationClip.SlugWalkBSQuick},
            {"Slug Turn BS Quick", AnimationClip.SlugTurnBSQuick},
            {"Collect SD 1 Back", AnimationClip.CollectSD1Back},
            {"Collect StandToIdle", AnimationClip.CollectStandToIdle},
            {"TurnFromBG", AnimationClip.TurnFromBG},
            {"Map Turn", AnimationClip.MapTurn},
            {"Exit", AnimationClip.Exit},
            {"Exit Door To Idle", AnimationClip.ExitDoorToIdle},
            {"Super Hard Land", AnimationClip.SuperHardLand},
            {"LookDownToIdle", AnimationClip.LookDownToIdle},
            {"DG Warp Charge", AnimationClip.DGWarpCharge},
            {"DG Warp", AnimationClip.DGWarp},
            {"DG Warp Cancel", AnimationClip.DGWarpCancel},
            {"DG Warp In", AnimationClip.DGWarpIn},
            {"Surface InToIdle", AnimationClip.SurfaceInToIdle},
            {"Surface InToSwim", AnimationClip.SurfaceInToSwim},
            {"Sprint", AnimationClip.Sprint},
            {"Look At King", AnimationClip.LookAtKing},
            {"Spike Death Antic", AnimationClip.SpikeDeathAntic},
        };

        private static readonly Dictionary<AnimationClip, string> InverseClipEnumNames =
            new Dictionary<AnimationClip, string> {
                {AnimationClip.Idle, "Idle"},
                {AnimationClip.Dash, "Dash"},
                {AnimationClip.Airborne, "Airborne"},
                {AnimationClip.SlashAlt, "SlashAlt"},
                {AnimationClip.Run, "Run"},
                {AnimationClip.Slash, "Slash"},
                {AnimationClip.SlashEffect, "SlashEffect"},
                {AnimationClip.SlashEffectAlt, "SlashEffectAlt"},
                {AnimationClip.UpSlash, "UpSlash"},
                {AnimationClip.DownSlash, "DownSlash"},
                {AnimationClip.Land, "Land"},
                {AnimationClip.HardLand, "HardLand"},
                {AnimationClip.LookDown, "LookDown"},
                {AnimationClip.LookUp, "LookUp"},
                {AnimationClip.UpSlashEffect, "UpSlashEffect"},
                {AnimationClip.DownSlashEffect, "DownSlashEffect"},
                {AnimationClip.Death, "Death"},
                {AnimationClip.SDCrysGrow, "SD Crys Grow"},
                {AnimationClip.DeathHeadNormal, "Death Head Normal"},
                {AnimationClip.DeathHeadCracked, "Death Head Cracked"},
                {AnimationClip.Recoil, "Recoil"},
                {AnimationClip.DNCharge, "DN Charge"},
                {AnimationClip.LanternIdle, "Lantern Idle"},
                {AnimationClip.AcidDeath, "Acid Death"},
                {AnimationClip.SpikeDeath, "Spike Death"},
                {AnimationClip.HitCrackAppear, "Hit Crack Appear"},
                {AnimationClip.DNCancel, "DN Cancel"},
                {AnimationClip.CollectMagical1, "Collect Magical 1"},
                {AnimationClip.CollectMagical2, "Collect Magical 2"},
                {AnimationClip.CollectMagical3, "Collect Magical 3"},
                {AnimationClip.CollectNormal1, "Collect Normal 1"},
                {AnimationClip.CollectNormal2, "Collect Normal 2"},
                {AnimationClip.CollectNormal3, "Collect Normal 3"},
                {AnimationClip.NACharge, "NA Charge"},
                {AnimationClip.IdleWind, "Idle Wind"},
                {AnimationClip.Enter, "Enter"},
                {AnimationClip.RoarLock, "Roar Lock"},
                {AnimationClip.Sit, "Sit"},
                {AnimationClip.SitLean, "Sit Lean"},
                {AnimationClip.Wake, "Wake"},
                {AnimationClip.GetOff, "Get Off"},
                {AnimationClip.SittingAsleep, "Sitting Asleep"},
                {AnimationClip.Prostrate, "Prostrate"},
                {AnimationClip.ProstrateRise, "Prostrate Rise"},
                {AnimationClip.Stun, "Stun"},
                {AnimationClip.Turn, "Turn"},
                {AnimationClip.RunToIdle, "Run To Idle"},
                {AnimationClip.Focus, "Focus"},
                {AnimationClip.FocusGet, "Focus Get"},
                {AnimationClip.FocusEnd, "Focus End"},
                {AnimationClip.WakeUpGround, "Wake Up Ground"},
                {AnimationClip.FocusGetOnce, "Focus Get Once"},
                {AnimationClip.HazardRespawn, "Hazard Respawn"},
                {AnimationClip.Walk, "Walk"},
                {AnimationClip.RespawnWake, "Respawn Wake"},
                {AnimationClip.SitFallAsleep, "Sit Fall Asleep"},
                {AnimationClip.MapIdle, "Map Idle"},
                {AnimationClip.MapAway, "Map Away"},
                {AnimationClip.Fall, "Fall"},
                {AnimationClip.TurnToIdle, "TurnToIdle"},
                {AnimationClip.CollectMagicalFall, "Collect Magical Fall"},
                {AnimationClip.CollectMagicalLand, "Collect Magical Land"},
                {AnimationClip.LookUpEnd, "LookUpEnd"},
                {AnimationClip.IdleHurt, "Idle Hurt"},
                {AnimationClip.LookDownEnd, "LookDownEnd"},
                {AnimationClip.CollectHeartPiece, "Collect Heart Piece"},
                {AnimationClip.CollectHeartPieceEnd, "Collect Heart Piece End"},
                {AnimationClip.Fireball1Cast, "Fireball1 Cast"},
                {AnimationClip.FireballAntic, "Fireball Antic"},
                {AnimationClip.SDCrysIdle, "SD Crys Idle"},
                {AnimationClip.Scream2Get, "Scream 2 Get"},
                {AnimationClip.SitMapOpen, "Sit Map Open"},
                {AnimationClip.WakeToSit, "Wake To Sit"},
                {AnimationClip.SitMapClose, "Sit Map Close"},
                {AnimationClip.DashToIdle, "Dash To Idle"},
                {AnimationClip.DashDown, "Dash Down"},
                {AnimationClip.DashDownLand, "Dash Down Land"},
                {AnimationClip.DashEffect, "Dash Effect"},
                {AnimationClip.LanternRun, "Lantern Run"},
                {AnimationClip.WallSlide, "Wall Slide"},
                {AnimationClip.SDChargeGround, "SD Charge Ground"},
                {AnimationClip.SDDash, "SD Dash"},
                {AnimationClip.SDFxCharge, "SD Fx Charge"},
                {AnimationClip.SDChargeGroundEnd, "SD Charge Ground End"},
                {AnimationClip.SDFxBling, "SD Fx Bling"},
                {AnimationClip.SDFxBurst, "SD Fx Burst"},
                {AnimationClip.SDHitWall, "SD Hit Wall"},
                {AnimationClip.QuakeAntic, "Quake Antic"},
                {AnimationClip.QuakeFall, "Quake Fall"},
                {AnimationClip.QuakeLand, "Quake Land"},
                {AnimationClip.MapWalk, "Map Walk"},
                {AnimationClip.SDAirBrake, "SD Air Brake"},
                {AnimationClip.SDWallCharge, "SD Wall Charge"},
                {AnimationClip.DoubleJump, "Double Jump"},
                {AnimationClip.DoubleJumpWings2, "Double Jump Wings 2"},
                {AnimationClip.Fireball2Cast, "Fireball2 Cast"},
                {AnimationClip.MapOpen, "Map Open"},
                {AnimationClip.NABigSlash, "NA Big Slash"},
                {AnimationClip.NABigSlashEffect, "NA Big Slash Effect"},
                {AnimationClip.TurnToBG, "TurnToBG"},
                {AnimationClip.NAChargedEffect, "NA Charged Effect"},
                {AnimationClip.NACyclone, "NA Cyclone"},
                {AnimationClip.NACycloneEnd, "NA Cyclone End"},
                {AnimationClip.NACycloneStart, "NA Cyclone Start"},
                {AnimationClip.QuakeFall2, "Quake Fall 2"},
                {AnimationClip.QuakeLand2, "Quake Land 2"},
                {AnimationClip.Scream, "Scream"},
                {AnimationClip.ScreamEnd2, "Scream End 2"},
                {AnimationClip.ScreamEnd, "Scream End"},
                {AnimationClip.ScreamStart, "Scream Start"},
                {AnimationClip.Scream2, "Scream 2"},
                {AnimationClip.SDBreak, "SD Break"},
                {AnimationClip.SDTrail, "SD Trail"},
                {AnimationClip.SDTrailEnd, "SD Trail End"},
                {AnimationClip.ShadowDash, "Shadow Dash"},
                {AnimationClip.ShadowDashBurst, "Shadow Dash Burst"},
                {AnimationClip.ShadowDashDown, "Shadow Dash Down"},
                {AnimationClip.ShadowRecharge, "Shadow Recharge"},
                {AnimationClip.WallSlash, "Wall Slash"},
                {AnimationClip.DGSetCharge, "DG Set Charge"},
                {AnimationClip.CycloneEffect, "Cyclone Effect"},
                {AnimationClip.CycloneEffectEnd, "Cyclone Effect End"},
                {AnimationClip.SurfaceSwim, "Surface Swim"},
                {AnimationClip.SurfaceIdle, "Surface Idle"},
                {AnimationClip.SurfaceIn, "Surface In"},
                {AnimationClip.DGSetEnd, "DG Set End"},
                {AnimationClip.Walljump, "Walljump"},
                {AnimationClip.WalljumpPuff, "Walljump Puff"},
                {AnimationClip.LookUpToIdle, "LookUpToIdle"},
                {AnimationClip.ToProne, "ToProne"},
                {AnimationClip.GetUpToIdle, "GetUpToIdle"},
                {AnimationClip.ChallengeStart, "Challenge Start"},
                {AnimationClip.CollectMagical3b, "Collect Magical 3b"},
                {AnimationClip.ChallengeEnd, "Challenge End"},
                {AnimationClip.CollectSD1, "Collect SD 1"},
                {AnimationClip.CollectSD2, "Collect SD 2"},
                {AnimationClip.CollectSD3, "Collect SD 3"},
                {AnimationClip.CollectSD4, "Collect SD 4"},
                {AnimationClip.ThornAttack, "Thorn Attack"},
                {AnimationClip.DNSlashAntic, "DN Slash Antic"},
                {AnimationClip.DNSlash, "DN Slash"},
                {AnimationClip.CollectShadow, "Collect Shadow"},
                {AnimationClip.NADashSlash, "NA Dash Slash"},
                {AnimationClip.NADashSlashEffect, "NA Dash Slash Effect"},
                {AnimationClip.SlugIdle, "Slug Idle"},
                {AnimationClip.UpSlashEffectM, "UpSlashEffect M"},
                {AnimationClip.DownSlashEffectM, "DownSlashEffect M"},
                {AnimationClip.SlashEffectM, "SlashEffect M"},
                {AnimationClip.SlugWalkQuick, "Slug Walk Quick"},
                {AnimationClip.SlugTurn, "Slug Turn"},
                {AnimationClip.SlashEffectAltM, "SlashEffectAlt M"},
                {AnimationClip.SlashEffectF, "SlashEffect F"},
                {AnimationClip.SlashEffectAltF, "SlashEffectAlt F"},
                {AnimationClip.UpSlashEffectF, "UpSlashEffect F"},
                {AnimationClip.DownSlashEffectF, "DownSlashEffect F"},
                {AnimationClip.DNStart, "DN Start"},
                {AnimationClip.DeathDream, "Death Dream"},
                {AnimationClip.DreamerLand, "Dreamer Land"},
                {AnimationClip.DreamerLift, "Dreamer Lift"},
                {AnimationClip.SDCrysFlash, "SD Crys Flash"},
                {AnimationClip.SDCrysShrink, "SD Crys Shrink"},
                {AnimationClip.DJGetLand, "DJ Get Land"},
                {AnimationClip.CollectAcid, "Collect Acid"},
                {AnimationClip.ShadowDashSharp, "Shadow Dash Sharp"},
                {AnimationClip.ShadowDashDownSharp, "Shadow Dash Down Sharp"},
                {AnimationClip.SlugBurst, "Slug Burst"},
                {AnimationClip.SlugDown, "Slug Down"},
                {AnimationClip.SlugUp, "Slug Up"},
                {AnimationClip.SlugTurnQuick, "Slug Turn Quick"},
                {AnimationClip.SlugWalk, "Slug Walk"},
                {AnimationClip.SlugIdleS, "Slug Idle S"},
                {AnimationClip.SlugIdleB, "Slug Idle B"},
                {AnimationClip.SlugIdleBS, "Slug Idle BS"},
                {AnimationClip.SlugTurnB, "Slug Turn B"},
                {AnimationClip.SlugTurnBQuick, "Slug Turn B Quick"},
                {AnimationClip.SlugTurnS, "Slug Turn S"},
                {AnimationClip.SlugTurnSQuick, "Slug Turn S Quick"},
                {AnimationClip.SlugTurnBS, "Slug Turn BS"},
                {AnimationClip.MapUpdate, "Map Update"},
                {AnimationClip.SlugBurstB, "Slug Burst B"},
                {AnimationClip.SlugBurstS, "Slug Burst S"},
                {AnimationClip.SlugBurstBS, "Slug Burst BS"},
                {AnimationClip.SlugWalkB, "Slug Walk B"},
                {AnimationClip.SlugWalkBQuick, "Slug Walk B Quick"},
                {AnimationClip.SlugWalkS, "Slug Walk S"},
                {AnimationClip.SlugWalkSQuick, "Slug Walk S Quick"},
                {AnimationClip.SitIdle, "Sit Idle"},
                {AnimationClip.SlugWalkBS, "Slug Walk BS"},
                {AnimationClip.SlugWalkBSQuick, "Slug Walk BS Quick"},
                {AnimationClip.SlugTurnBSQuick, "Slug Turn BS Quick"},
                {AnimationClip.CollectSD1Back, "Collect SD 1 Back"},
                {AnimationClip.CollectStandToIdle, "Collect StandToIdle"},
                {AnimationClip.TurnFromBG, "TurnFromBG"},
                {AnimationClip.MapTurn, "Map Turn"},
                {AnimationClip.Exit, "Exit"},
                {AnimationClip.ExitDoorToIdle, "Exit Door To Idle"},
                {AnimationClip.SuperHardLand, "Super Hard Land"},
                {AnimationClip.LookDownToIdle, "LookDownToIdle"},
                {AnimationClip.DGWarpCharge, "DG Warp Charge"},
                {AnimationClip.DGWarp, "DG Warp"},
                {AnimationClip.DGWarpCancel, "DG Warp Cancel"},
                {AnimationClip.DGWarpIn, "DG Warp In"},
                {AnimationClip.SurfaceInToIdle, "Surface InToIdle"},
                {AnimationClip.SurfaceInToSwim, "Surface InToSwim"},
                {AnimationClip.Sprint, "Sprint"},
                {AnimationClip.LookAtKing, "Look At King"},
                {AnimationClip.SpikeDeathAntic, "Spike Death Antic"},
            };

        // TODO: add Dreamshield
        // A static mapping containing the animation effect for each clip name
        private static readonly Dictionary<AnimationClip, IAnimationEffect> AnimationEffects =
            new Dictionary<AnimationClip, IAnimationEffect> {
                {AnimationClip.SDChargeGround, new CrystalDashGroundCharge()},
                {AnimationClip.SDChargeGroundEnd, CrystalDashChargeCancel},
                {AnimationClip.SDWallCharge, new CrystalDashWallCharge()},
                {AnimationClip.SDDash, new CrystalDash()},
                {AnimationClip.SDAirBrake, new CrystalDashAirCancel()},
                {AnimationClip.SDHitWall, new CrystalDashHitWall()},
                {AnimationClip.Slash, new Slash()},
                {AnimationClip.SlashAlt, new AltSlash()},
                {AnimationClip.DownSlash, new DownSlash()},
                {AnimationClip.UpSlash, new UpSlash()},
                {AnimationClip.WallSlash, new WallSlash()},
                {AnimationClip.Fireball1Cast, new VengefulSpirit()},
                {AnimationClip.Fireball2Cast, new ShadeSoul()},
                {AnimationClip.QuakeAntic, new DiveAntic()},
                {AnimationClip.QuakeFall, new DesolateDiveDown()},
                {AnimationClip.QuakeFall2, new DescendingDarkDown()},
                {AnimationClip.QuakeLand, new DesolateDiveLand()},
                {AnimationClip.QuakeLand2, new DescendingDarkLand()},
                {AnimationClip.Scream, new HowlingWraiths()},
                {AnimationClip.Scream2, new AbyssShriek()},
                {AnimationClip.NACyclone, new CycloneSlash()},
                {AnimationClip.NACycloneEnd, new CycloneSlashEnd()},
                {AnimationClip.NABigSlash, new GreatSlash()},
                {AnimationClip.NADashSlash, new DashSlash()},
                {AnimationClip.Recoil, new Effects.Recoil()},
                {AnimationClip.Stun, new Stun()},
                {AnimationClip.Focus, Focus},
                {AnimationClip.FocusGet, FocusBurst},
                {AnimationClip.FocusGetOnce, FocusEnd},
                {AnimationClip.FocusEnd, FocusEnd},
                {AnimationClip.SlugDown, Focus},
                {AnimationClip.SlugBurst, FocusBurst},
                {AnimationClip.SlugBurstS, FocusBurst}, // Shape of Unn + Spore Shroom
                {AnimationClip.SlugBurstB, FocusBurst}, // Shape of Unn + Baldur Shell
                {AnimationClip.SlugBurstBS, FocusBurst}, // Shape of Unn + Spore Shroom + Baldur Shell
                {AnimationClip.SlugUp, FocusEnd},
                {AnimationClip.Dash, new Dash()},
                {AnimationClip.DashDown, new DashDown()},
                {AnimationClip.ShadowDash, new ShadowDash()},
                {AnimationClip.ShadowDashSharp, new ShadowDashSharp()},
                {AnimationClip.ShadowDashDown, new ShadowDashDown()},
                {AnimationClip.ShadowDashDownSharp, new ShadowDashSharpDown()},
                {AnimationClip.DashEnd, new DashEnd()},
                {AnimationClip.NailArtCharge, new NailArtCharge()},
                {AnimationClip.NailArtCharged, new NailArtCharged()},
                {AnimationClip.NailArtChargeEnd, new NailArtEnd()},
                {AnimationClip.WallSlide, new WallSlide()},
                {AnimationClip.WallSlideEnd, new WallSlideEnd()},
                {AnimationClip.Walljump, new WallJump()},
                {AnimationClip.DoubleJump, new MonarchWings()},
                {AnimationClip.HardLand, new HardLand()},
                {AnimationClip.HazardDeath, new HazardDeath()},
                {AnimationClip.HazardRespawn, new HazardRespawn()},
                {AnimationClip.DungTrail, new DungTrail()},
                {AnimationClip.DungTrailEnd, new DungTrailEnd()},
                {AnimationClip.ThornAttack, new ThornsOfAgony()}
            };

        private readonly NetworkManager _networkManager;
        private readonly PlayerManager _playerManager;

        private readonly SkinManager _skinManager;
        
        // The last animation clip sent
        private string _lastAnimationClip;

        /**
         * Whether the animation controller was responsible for the last
         * clip that was sent
         */
        private bool _animationControllerWasLastSent;

        // Whether we should stop sending animations until the scene has changed
        private bool _stopSendingAnimationUntilSceneChange;

        // Whether the current dash has ended and we can start a new one
        private bool _dashHasEnded = true;

        // Whether the charge effect was last update active
        private bool _lastChargeEffectActive;

        // Whether the charged effect was last update active
        private bool _lastChargedEffectActive;

        // Whether the player was wallsliding last update
        private bool _lastWallSlideActive;

        public AnimationManager(
            NetworkManager networkManager,
            PlayerManager playerManager,
            PacketManager packetManager,
            Game.Settings.GameSettings gameSettings,
            SkinManager skinManager
        ) {
            _networkManager = networkManager;
            _playerManager = playerManager;
            _skinManager = skinManager;
            // Register packet handler
            packetManager.RegisterClientPacketHandler<ClientPlayerDeathPacket>(PacketId.PlayerDeath,
                OnPlayerDeath);
            
            // Register scene change, which is where we update the animation event handler
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
            
            // Register callbacks for the hero animation controller for the Airborne animation
            On.HeroAnimationController.Play += HeroAnimationControllerOnPlay;
            On.HeroAnimationController.PlayFromFrame += HeroAnimationControllerOnPlayFromFrame;
            
            // Register a callback so we know when the dash has finished
            On.HeroController.CancelDash += HeroControllerOnCancelDash;
            
            // Register a callback so we can check the nail art charge status
            ModHooks.Instance.HeroUpdateHook += OnHeroUpdateHook;
            
            // Register a callback for when we get hit by a hazard
            On.HeroController.DieFromHazard += HeroControllerOnDieFromHazard;
            // Also register a callback from when we respawn from a hazard
            On.GameManager.HazardRespawn += GameManagerOnHazardRespawn;
            
            // Register when the HeroController starts, so we can register dung trail events
            On.HeroController.Start += HeroControllerOnStart;
            
            // Set the game settings for all animation effects
            foreach (var effect in AnimationEffects.Values) {
                effect.SetGameSettings(gameSettings);
            }
        }

        public void OnPlayerAnimationUpdate(ushort id, int clipId, int frame, bool[] effectInfo) {
            UpdatePlayerAnimation(id, clipId, frame);
            
            var animationClip = (AnimationClip) clipId;

            if (AnimationEffects.ContainsKey(animationClip)) {
                var playerObject = _playerManager.GetPlayerObject(id);
                if (playerObject == null) {
                    // Logger.Warn(this, $"Tried to play animation effect {clipName} with ID: {id}, but player object doesn't exist");
                    return;
                }

                var animationEffect = AnimationEffects[animationClip];
                
                // Check if the animation effect is a DamageAnimationEffect and if so,
                // set whether it should deal damage based on player teams
                if (animationEffect is DamageAnimationEffect damageAnimationEffect) {
                    var localPlayerTeam = _playerManager.LocalPlayerTeam;
                    var otherPlayerTeam = _playerManager.GetPlayerTeam(id);
                    
                    damageAnimationEffect.SetShouldDoDamage(
                        otherPlayerTeam != localPlayerTeam 
                        || otherPlayerTeam.Equals(Team.None) 
                        || localPlayerTeam.Equals(Team.None)
                    );
                }

                animationEffect.Play(
                    playerObject,
                    effectInfo
                );
            }
        }

        public void UpdatePlayerAnimation(ushort id, int clipId, int frame) {
            var playerObject = _playerManager.GetPlayerObject(id);
            if (playerObject == null) {
                // Logger.Warn(this, $"Tried to update animation, but there was not matching player object for ID {id}");
                return;
            }

            var animationClip = (AnimationClip) clipId;
            if (!InverseClipEnumNames.ContainsKey(animationClip)) {
                // This happens when we send custom clips, that can't be played by the sprite animator, so for now we
                // don't log it. This warning might be useful if we seem to be missing animations from the Knights
                // sprite animator.
                
                // Logger.Warn(this, $"Tried to update animation, but there was no entry for clip ID: {clipId}, enum: {animationClip}");
                return;
            }

            var clipName = InverseClipEnumNames[animationClip];

            // Get the sprite animator and check whether this clip can be played before playing it
            var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
            if (spriteAnimator.GetClipByName(clipName) != null) {
                spriteAnimator.PlayFromFrame(clipName, frame);
            }
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            // A scene change occurs, so we can send again
            _stopSendingAnimationUntilSceneChange = false;

            // Only update animation handler if we change from non-gameplay to a gameplay scene
            if (SceneUtil.IsNonGameplayScene(oldScene.name) && !SceneUtil.IsNonGameplayScene(newScene.name)) {
                // Register on death, to send a packet to the server so clients can start the animation
                HeroController.instance.OnDeath += OnDeath;
            }
        }

        private void OnAnimationEvent(tk2dSpriteAnimator spriteAnimator, tk2dSpriteAnimationClip clip,
            int frameIndex) {
            // Logger.Info(this, $"Animation event with name: {clip.name}");
            
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }
            
            // If we need to stop sending until a scene change occurs, we skip
            if (_stopSendingAnimationUntilSceneChange) {
                return;
            }
            
            // If this is a clip that should be handled by the animation controller hook, we return
            if (AnimationControllerClipNames.Contains(clip.name)) {
                // Update the last clip name
                _lastAnimationClip = clip.name;
            
                return;
            }
            
            // Skip event handling when we already handled this clip, unless it is a clip with wrap mode once
            if (clip.name.Equals(_lastAnimationClip)
                && clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once
                && !AllowedLoopAnimations.Contains(clip.name)) {
                return;
            }
            
            // Skip clips that do not have the wrap mode loop, loopsection or once
            if (clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Loop &&
                clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.LoopSection &&
                clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once) {
                return;
            }
            
            // Logger.Info(this, $"Sending animation with name: {clip.name}");
            
            // Make sure that when we enter a building, we don't transmit any more animation events
            // TODO: the same issue applied to exiting a building, but that is less trivial to solve
            if (clip.name.Equals("Enter")) {
                _stopSendingAnimationUntilSceneChange = true;
            }
            
            // Check special case of downwards dashes that trigger the animation event twice
            // We only send it once if the current dash has ended
            if (clip.name.Equals("Dash Down")
                || clip.name.Equals("Shadow Dash Down")
                || clip.name.Equals("Shadow Dash Down Sharp")) {
                if (!_dashHasEnded) {
                    return;
                }
            
                _dashHasEnded = false;
            }
            
            // Get the current frame and associated data
            // TODO: the eventInfo might be same as the clip name in all cases
            var frame = clip.GetFrame(frameIndex);
            var clipName = frame.eventInfo;
            
            if (!ClipEnumNames.ContainsKey(clipName)) {
                Logger.Warn(this, $"Player sprite animator played unknown clip, name: {clipName}");
                return;
            }
            
            var animationClip = ClipEnumNames[clipName];
            
            // Check whether there is an effect that adds info to this packet
            if (AnimationEffects.ContainsKey(animationClip)) {
                var effectInfo = AnimationEffects[animationClip].GetEffectInfo();
            
                _networkManager.GetNetClient().SendAnimationUpdate(animationClip, 0, effectInfo);
            } else {
                _networkManager.GetNetClient().SendAnimationUpdate(animationClip);
            }
            
            // Update the last clip name, since it changed
            _lastAnimationClip = clip.name;
            
            // We have sent a different clip, so we can reset this
            _animationControllerWasLastSent = false;
        }

        private void HeroAnimationControllerOnPlay(On.HeroAnimationController.orig_Play orig,
            HeroAnimationController self, string clipname) {
            orig(self, clipname);
            OnAnimationControllerPlay(clipname, 0);
        }

        private void HeroAnimationControllerOnPlayFromFrame(On.HeroAnimationController.orig_PlayFromFrame orig,
            HeroAnimationController self, string clipname, int frame) {
            orig(self, clipname, frame);
            OnAnimationControllerPlay(clipname, frame);
        }

        private void OnAnimationControllerPlay(string clipName, int frame) {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            // If this is not a clip that should be handled by the animation controller hook, we return
            if (!AnimationControllerClipNames.Contains(clipName)) {
                return;
            }

            // If the animation controller is responsible for the last sent clip, we skip
            // this is to ensure that we don't spam packets of the same clip
            if (!_animationControllerWasLastSent) {
                if (!ClipEnumNames.ContainsKey(clipName)) {
                    Logger.Warn(this, $"Player animation controller played unknown clip, name: {clipName}");
                    return;
                }

                var clipId = ClipEnumNames[clipName];
            
                _networkManager.GetNetClient().SendAnimationUpdate(clipId, frame);

                // This was the last clip we sent
                _animationControllerWasLastSent = true;
            }
        }

        private void HeroControllerOnCancelDash(On.HeroController.orig_CancelDash orig, HeroController self) {
            orig(self);

            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.DashEnd);

            // The dash has ended, so we can send a new one when we dash
            _dashHasEnded = true;
        }

        public bool initSkins = false;
        private void OnHeroUpdateHook() {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                initSkins = false;
                return;
            }
            if(!initSkins){
                // Download skin hashes from the server  
                _skinManager.getServerJsonOnClient(_networkManager.GetNetClient()._lastHost,_networkManager.GetNetClient()._lastPort);
                initSkins = true;
            } else {
                if(_skinManager.pendingDownloads < 1){
                    _skinManager.loadSkinsIntoMemory();
                }
            }
            var chargeEffectActive = HeroController.instance.artChargeEffect.activeSelf;
            var chargedEffectActive = HeroController.instance.artChargedEffect.activeSelf;

            if (chargeEffectActive && !_lastChargeEffectActive) {
                // Charge effect is now active, which wasn't last update, so we can send the charge animation packet
                _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.NailArtCharge);
            }

            if (chargedEffectActive && !_lastChargedEffectActive) {
                // Charged effect is now active, which wasn't last update, so we can send the charged animation packet
                _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.NailArtCharged);
            }

            if (!chargeEffectActive && _lastChargeEffectActive && !chargedEffectActive) {
                // The charge effect is now inactive and we are not fully charged
                // This means that we cancelled the nail art charge
                _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.NailArtChargeEnd);
            }

            if (!chargedEffectActive && _lastChargedEffectActive) {
                // The charged effect is now inactive, so we are done with the nail art
                _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.NailArtChargeEnd);
            }

            // Update the latest states
            _lastChargeEffectActive = chargeEffectActive;
            _lastChargedEffectActive = chargedEffectActive;

            // Obtain the current wall slide state
            var wallSlideActive = HeroController.instance.cState.wallSliding;

            if (!wallSlideActive && _lastWallSlideActive) {
                // We were wall sliding last update, but not anymore, so we send a wall slide end animation
                _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.WallSlideEnd);
            }

            // Update the last state
            _lastWallSlideActive = wallSlideActive;

            // Obtain sprite animator from hero controller
            var localPlayer = HeroController.instance;
            var spriteAnimator = localPlayer.GetComponent<tk2dSpriteAnimator>();

            // Check whether it is non-null
            if (spriteAnimator != null) {
                // Check whether the animation event is still registered to our callback
                if (spriteAnimator.AnimationEventTriggered != OnAnimationEvent) {
                    Logger.Info(this, "Re-registering animation event triggered");
            
                    // For each clip in the animator, we want to make sure it triggers an event
                    foreach (var clip in spriteAnimator.Library.clips) {
                        // Skip clips with no frames
                        if (clip.frames.Length == 0) {
                            continue;
                        }
                    
                        var firstFrame = clip.frames[0];
                        // Enable event triggering on first frame
                        firstFrame.triggerEvent = true;
                        // Also include the clip name as event info, so we can retrieve it later
                        firstFrame.eventInfo = clip.name;
                    }
                    
                    // Now actually register a callback for when the animation event fires
                    spriteAnimator.AnimationEventTriggered = OnAnimationEvent;
                }
            }
        }

        private IEnumerator HeroControllerOnDieFromHazard(On.HeroController.orig_DieFromHazard orig,
            HeroController self, HazardType hazardtype, float angle) {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return orig(self, hazardtype, angle);
            }

            _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.HazardDeath, 0, new[] {
                hazardtype.Equals(HazardType.SPIKES),
                hazardtype.Equals(HazardType.ACID)
            });

            // Execute the original method and return its value
            return orig(self, hazardtype, angle);
        }

        private void GameManagerOnHazardRespawn(On.GameManager.orig_HazardRespawn orig, GameManager self) {
            orig(self);

            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.HazardRespawn);
        }

        private void OnPlayerDeath(ClientPlayerDeathPacket packet) {
            // And play the death animation for the ID in the packet
            MonoBehaviourUtil.Instance.StartCoroutine(PlayDeathAnimation(packet.Id));
        }

        private void OnDeath() {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            Logger.Info(this, "Client has died, sending PlayerDeath packet");

            // Let the server know that we have died            
            var deathPacket = new ServerPlayerDeathPacket();
            deathPacket.CreatePacket();
            _networkManager.GetNetClient().SendTcp(deathPacket);
        }

        private IEnumerator PlayDeathAnimation(ushort id) {
            Logger.Info(this, "Starting death animation");

            // Get the player object corresponding to this ID
            var playerObject = _playerManager.GetPlayerObject(id);

            // Get the sprite animator and start playing the Death animation
            var animator = playerObject.GetComponent<tk2dSpriteAnimator>();
            animator.Stop();
            animator.PlayFromFrame("Death", 0);

            // Obtain the duration for the animation
            var deathAnimationDuration = animator.GetClipByName("Death").Duration;

            // After half a second we want to throw out the nail (as defined in the FSM)
            yield return new WaitForSeconds(0.5f);

            // Calculate the duration remaining until the death animation is finished
            var remainingDuration = deathAnimationDuration - 0.5f;

            // Obtain the local player object, to copy actions from
            var localPlayerObject = Object.Instantiate(HeroController.instance.gameObject);

            // Get the FSM for the Hero Death
            var heroDeathAnimFsm = localPlayerObject
                .FindGameObjectInChildren("Hero Death")
                .LocateMyFSM("Hero Death Anim");

            // Get the nail fling object from the Blow state
            var nailObject = heroDeathAnimFsm.GetAction<FlingObjectsFromGlobalPool>("Blow", 0);

            // Spawn it relative to the player
            var nailGameObject = nailObject.gameObject.Value.Spawn(
                playerObject.transform.position,
                Quaternion.Euler(Vector3.zero)
            );

            // Get the rigidbody component that we need to throw around
            var nailRigidBody = nailGameObject.GetComponent<Rigidbody2D>();

            // Get a random speed and angle and calculate the rigidbody velocity
            var speed = UnityEngine.Random.Range(18, 22);
            float angle = UnityEngine.Random.Range(50, 130);
            var velX = speed * Mathf.Cos(angle * ((float) Math.PI / 180f));
            var velY = speed * Mathf.Sin(angle * ((float) Math.PI / 180f));

            // Set the velocity so it starts moving
            nailRigidBody.velocity = new Vector2(velX, velY);

            // Wait for the remaining duration of the death animation
            yield return new WaitForSeconds(remainingDuration);

            // Now we can disable the player object so it isn't visible anymore
            playerObject.SetActive(false);

            // Check which direction we are facing, we need this in a few variables
            var facingRight = playerObject.transform.localScale.x > 0;

            // Depending on which direction the player was facing, choose a state
            var stateName = "Head Left";
            if (facingRight) {
                stateName = "Head Right";
            }

            // Obtain a head object from the either Head states and instantiate it
            var headObject = heroDeathAnimFsm.GetAction<CreateObject>(stateName, 0);
            var headGameObject = Object.Instantiate(
                headObject.gameObject.Value,
                playerObject.transform.position + new Vector3(facingRight ? 0.2f : -0.2f, -0.02f, -0.01f),
                Quaternion.identity
            );

            // Get the rigidbody component of the head object
            var headRigidBody = headGameObject.GetComponent<Rigidbody2D>();

            // Calculate the angle at which we are going to throw 
            var headAngle = 15f * Mathf.Cos((facingRight ? 100f : 80f) * ((float) Math.PI / 180f));

            // Now set the velocity as this angle
            headRigidBody.velocity = new Vector2(headAngle, headAngle);

            // Finally add required torque (according to the FSM)
            headRigidBody.AddTorque(facingRight ? 20f : -20f);
        }

        private void HeroControllerOnStart(On.HeroController.orig_Start orig, HeroController self) {
            // Execute original method
            orig(self);

            SetDescendingDarkLandEffectDelay();
            RegisterDefenderCrestEffects();
        }

        /**
         * Sets the delay for the descending dark land effect to trigger, since if we overwrite
         * the AnimationTriggerEvent, it will fallback to 0.75s, which is too long.
         * The event normally triggers at frame index 7, which is the 8th frame.
         * The FPS of the animation is 20, which means 8/20 = 0.4s after the animation starts is
         * when we need to finish the action in the FSM. If this is confusing check the "Spell Control" FSM of
         * the knight and look at the "Q2 Land" state.
         */
        private void SetDescendingDarkLandEffectDelay() {
            var spellControl = HeroController.instance.spellControl;
            var waitAction = spellControl.GetAction<Wait>("Q2 Land", 14);
            waitAction.time.Value = 0.4f;
        }

        private void RegisterDefenderCrestEffects() {
            var charmEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Charm Effects");
            if (charmEffects == null) {
                return;
            }

            var dungObject = charmEffects.FindGameObjectInChildren("Dung");
            if (dungObject == null) {
                return;
            }

            var dungControlFsm = dungObject.LocateMyFSM("Control");

            // Create a new dung trail event sending instance
            var sendDungTrailEvent = new SendDungTrailEvent(_networkManager.GetNetClient());

            // Keep track of whether we subscribed to the update event already,
            // so we don't subscribe multiple times, with no way to unsubscribe those instances
            var isSubscribed = false;

            // Register the Update method of the SendDungTrailEvent class
            // when the Defender's Crest charm is equipped
            dungControlFsm.InsertMethod("Equipped", 1, () => {
                Logger.Info(this, "Defender's Crest is equipped, starting dung trail event sending");

                // Subscribe only when we haven't already
                if (!isSubscribed) {
                    MonoBehaviourUtil.Instance.OnUpdateEvent += sendDungTrailEvent.Update;
                    isSubscribed = true;
                }
            });

            // Deregister and reset the SendDungTrailEvent class when
            // the Defender's Crest charm is unequipped
            dungControlFsm.InsertMethod("Unequipped", 2, () => {
                // If we weren't subscribed, we don't need to stop
                if (!isSubscribed) {
                    return;
                }

                Logger.Info(this, "Defender's Crest is unequipped, stopping dung trail event sending");

                MonoBehaviourUtil.Instance.OnUpdateEvent -= sendDungTrailEvent.Update;
                sendDungTrailEvent.Reset();
                isSubscribed = false;

                if (!_networkManager.GetNetClient().IsConnected) {
                    return;
                }

                _networkManager.GetNetClient().SendAnimationUpdate(AnimationClip.DungTrailEnd);
            });
        }

        public AnimationClip GetCurrentAnimationClip() {
            var currentClipName = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name;

            if (ClipEnumNames.ContainsKey(currentClipName)) {
                return ClipEnumNames[currentClipName];
            }

            return 0;
        }
    }
}