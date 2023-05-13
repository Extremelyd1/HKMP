using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GlobalEnums;
using Hkmp.Animation.Effects;
using Hkmp.Collection;
using Hkmp.Fsm;
using Hkmp.Game;
using Hkmp.Game.Client;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Animation;

/// <summary>
/// Class that manages all forms of animation from clients.
/// </summary>
internal class AnimationManager {
    /// <summary>
    /// The distance threshold for playing certain effects.
    /// </summary>
    public const float EffectDistanceThreshold = 25f;

    /// <summary>
    /// Animations that are allowed to loop, because they need to transmit the effect.
    /// </summary>
    private static readonly string[] AllowedLoopAnimations = { "Focus Get", "Run" };

    /// <summary>
    /// Clip names of animations that are handled by the animation controller.
    /// </summary>
    private static readonly string[] AnimationControllerClipNames = {
        "Airborne"
    };

    /// <summary>
    /// The animation effect for cancelling the Crystal Dash Charge. Stored since it needs to be called
    /// manually sometimes.
    /// </summary>
    public static readonly CrystalDashChargeCancel CrystalDashChargeCancel = new CrystalDashChargeCancel();

    /// <summary>
    /// The animation effect for the focus. Stored since it needs to be called manually sometimes.
    /// </summary>
    private static readonly Focus Focus = new Focus();

    /// <summary>
    /// The animation effect for the focus burst. Stored since it needs to be called manually sometimes.
    /// </summary>
    private static readonly FocusBurst FocusBurst = new FocusBurst();

    /// <summary>
    /// The animation effect for the focus end. Stored since it needs to be called manually sometimes.
    /// </summary>
    public static readonly FocusEnd FocusEnd = new FocusEnd();

    /// <summary>
    /// The animation effect for the nail art charge end. Stored since it needs to be called manually sometimes.
    /// </summary>
    public static readonly NailArtEnd NailArtEnd = new NailArtEnd();

    /// <summary>
    /// Bi-directional lookup table for linking animation clip names with their respective animation clip enum
    /// values.
    /// </summary>
    private static readonly BiLookup<string, AnimationClip> ClipEnumNames = new BiLookup<string, AnimationClip> {
        { "Idle", AnimationClip.Idle },
        { "Dash", AnimationClip.Dash },
        { "Airborne", AnimationClip.Airborne },
        { "SlashAlt", AnimationClip.SlashAlt },
        { "Run", AnimationClip.Run },
        { "Slash", AnimationClip.Slash },
        { "SlashEffect", AnimationClip.SlashEffect },
        { "SlashEffectAlt", AnimationClip.SlashEffectAlt },
        { "UpSlash", AnimationClip.UpSlash },
        { "DownSlash", AnimationClip.DownSlash },
        { "Land", AnimationClip.Land },
        { "HardLand", AnimationClip.HardLand },
        { "LookDown", AnimationClip.LookDown },
        { "LookUp", AnimationClip.LookUp },
        { "UpSlashEffect", AnimationClip.UpSlashEffect },
        { "DownSlashEffect", AnimationClip.DownSlashEffect },
        { "Death", AnimationClip.Death },
        { "SD Crys Grow", AnimationClip.SDCrysGrow },
        { "Death Head Normal", AnimationClip.DeathHeadNormal },
        { "Death Head Cracked", AnimationClip.DeathHeadCracked },
        { "Recoil", AnimationClip.Recoil },
        { "DN Charge", AnimationClip.DNCharge },
        { "Lantern Idle", AnimationClip.LanternIdle },
        { "Acid Death", AnimationClip.AcidDeath },
        { "Spike Death", AnimationClip.SpikeDeath },
        { "Hit Crack Appear", AnimationClip.HitCrackAppear },
        { "DN Cancel", AnimationClip.DNCancel },
        { "Collect Magical 1", AnimationClip.CollectMagical1 },
        { "Collect Magical 2", AnimationClip.CollectMagical2 },
        { "Collect Magical 3", AnimationClip.CollectMagical3 },
        { "Collect Normal 1", AnimationClip.CollectNormal1 },
        { "Collect Normal 2", AnimationClip.CollectNormal2 },
        { "Collect Normal 3", AnimationClip.CollectNormal3 },
        { "NA Charge", AnimationClip.NACharge },
        { "Idle Wind", AnimationClip.IdleWind },
        { "Enter", AnimationClip.Enter },
        { "Roar Lock", AnimationClip.RoarLock },
        { "Sit", AnimationClip.Sit },
        { "Sit Lean", AnimationClip.SitLean },
        { "Wake", AnimationClip.Wake },
        { "Get Off", AnimationClip.GetOff },
        { "Sitting Asleep", AnimationClip.SittingAsleep },
        { "Prostrate", AnimationClip.Prostrate },
        { "Prostrate Rise", AnimationClip.ProstrateRise },
        { "Stun", AnimationClip.Stun },
        { "Turn", AnimationClip.Turn },
        { "Run To Idle", AnimationClip.RunToIdle },
        { "Focus", AnimationClip.Focus },
        { "Focus Get", AnimationClip.FocusGet },
        { "Focus End", AnimationClip.FocusEnd },
        { "Wake Up Ground", AnimationClip.WakeUpGround },
        { "Focus Get Once", AnimationClip.FocusGetOnce },
        { "Hazard Respawn", AnimationClip.HazardRespawn },
        { "Walk", AnimationClip.Walk },
        { "Respawn Wake", AnimationClip.RespawnWake },
        { "Sit Fall Asleep", AnimationClip.SitFallAsleep },
        { "Map Idle", AnimationClip.MapIdle },
        { "Map Away", AnimationClip.MapAway },
        { "Fall", AnimationClip.Fall },
        { "TurnToIdle", AnimationClip.TurnToIdle },
        { "Collect Magical Fall", AnimationClip.CollectMagicalFall },
        { "Collect Magical Land", AnimationClip.CollectMagicalLand },
        { "LookUpEnd", AnimationClip.LookUpEnd },
        { "Idle Hurt", AnimationClip.IdleHurt },
        { "LookDownEnd", AnimationClip.LookDownEnd },
        { "Collect Heart Piece", AnimationClip.CollectHeartPiece },
        { "Collect Heart Piece End", AnimationClip.CollectHeartPieceEnd },
        { "Fireball1 Cast", AnimationClip.Fireball1Cast },
        { "Fireball Antic", AnimationClip.FireballAntic },
        { "SD Crys Idle", AnimationClip.SDCrysIdle },
        { "Scream 2 Get", AnimationClip.Scream2Get },
        { "Sit Map Open", AnimationClip.SitMapOpen },
        { "Wake To Sit", AnimationClip.WakeToSit },
        { "Sit Map Close", AnimationClip.SitMapClose },
        { "Dash To Idle", AnimationClip.DashToIdle },
        { "Dash Down", AnimationClip.DashDown },
        { "Dash Down Land", AnimationClip.DashDownLand },
        { "Dash Effect", AnimationClip.DashEffect },
        { "Lantern Run", AnimationClip.LanternRun },
        { "Wall Slide", AnimationClip.WallSlide },
        { "SD Charge Ground", AnimationClip.SDChargeGround },
        { "SD Dash", AnimationClip.SDDash },
        { "SD Fx Charge", AnimationClip.SDFxCharge },
        { "SD Charge Ground End", AnimationClip.SDChargeGroundEnd },
        { "SD Fx Bling", AnimationClip.SDFxBling },
        { "SD Fx Burst", AnimationClip.SDFxBurst },
        { "SD Hit Wall", AnimationClip.SDHitWall },
        { "Quake Antic", AnimationClip.QuakeAntic },
        { "Quake Fall", AnimationClip.QuakeFall },
        { "Quake Land", AnimationClip.QuakeLand },
        { "Map Walk", AnimationClip.MapWalk },
        { "SD Air Brake", AnimationClip.SDAirBrake },
        { "SD Wall Charge", AnimationClip.SDWallCharge },
        { "Double Jump", AnimationClip.DoubleJump },
        { "Double Jump Wings 2", AnimationClip.DoubleJumpWings2 },
        { "Fireball2 Cast", AnimationClip.Fireball2Cast },
        { "Map Open", AnimationClip.MapOpen },
        { "NA Big Slash", AnimationClip.NABigSlash },
        { "NA Big Slash Effect", AnimationClip.NABigSlashEffect },
        { "TurnToBG", AnimationClip.TurnToBG },
        { "NA Charged Effect", AnimationClip.NAChargedEffect },
        { "NA Cyclone", AnimationClip.NACyclone },
        { "NA Cyclone End", AnimationClip.NACycloneEnd },
        { "NA Cyclone Start", AnimationClip.NACycloneStart },
        { "Quake Fall 2", AnimationClip.QuakeFall2 },
        { "Quake Land 2", AnimationClip.QuakeLand2 },
        { "Scream", AnimationClip.Scream },
        { "Scream End 2", AnimationClip.ScreamEnd2 },
        { "Scream End", AnimationClip.ScreamEnd },
        { "Scream Start", AnimationClip.ScreamStart },
        { "Scream 2", AnimationClip.Scream2 },
        { "SD Break", AnimationClip.SDBreak },
        { "SD Trail", AnimationClip.SDTrail },
        { "SD Trail End", AnimationClip.SDTrailEnd },
        { "Shadow Dash", AnimationClip.ShadowDash },
        { "Shadow Dash Burst", AnimationClip.ShadowDashBurst },
        { "Shadow Dash Down", AnimationClip.ShadowDashDown },
        { "Shadow Recharge", AnimationClip.ShadowRecharge },
        { "Wall Slash", AnimationClip.WallSlash },
        { "DG Set Charge", AnimationClip.DGSetCharge },
        { "Cyclone Effect", AnimationClip.CycloneEffect },
        { "Cyclone Effect End", AnimationClip.CycloneEffectEnd },
        { "Surface Swim", AnimationClip.SurfaceSwim },
        { "Surface Idle", AnimationClip.SurfaceIdle },
        { "Surface In", AnimationClip.SurfaceIn },
        { "DG Set End", AnimationClip.DGSetEnd },
        { "Walljump", AnimationClip.Walljump },
        { "Walljump Puff", AnimationClip.WalljumpPuff },
        { "LookUpToIdle", AnimationClip.LookUpToIdle },
        { "ToProne", AnimationClip.ToProne },
        { "GetUpToIdle", AnimationClip.GetUpToIdle },
        { "Challenge Start", AnimationClip.ChallengeStart },
        { "Collect Magical 3b", AnimationClip.CollectMagical3b },
        { "Challenge End", AnimationClip.ChallengeEnd },
        { "Collect SD 1", AnimationClip.CollectSD1 },
        { "Collect SD 2", AnimationClip.CollectSD2 },
        { "Collect SD 3", AnimationClip.CollectSD3 },
        { "Collect SD 4", AnimationClip.CollectSD4 },
        { "Thorn Attack", AnimationClip.ThornAttack },
        { "DN Slash Antic", AnimationClip.DNSlashAntic },
        { "DN Slash", AnimationClip.DNSlash },
        { "Collect Shadow", AnimationClip.CollectShadow },
        { "NA Dash Slash", AnimationClip.NADashSlash },
        { "NA Dash Slash Effect", AnimationClip.NADashSlashEffect },
        { "Slug Idle", AnimationClip.SlugIdle },
        { "UpSlashEffect M", AnimationClip.UpSlashEffectM },
        { "DownSlashEffect M", AnimationClip.DownSlashEffectM },
        { "SlashEffect M", AnimationClip.SlashEffectM },
        { "Slug Walk Quick", AnimationClip.SlugWalkQuick },
        { "Slug Turn", AnimationClip.SlugTurn },
        { "SlashEffectAlt M", AnimationClip.SlashEffectAltM },
        { "SlashEffect F", AnimationClip.SlashEffectF },
        { "SlashEffectAlt F", AnimationClip.SlashEffectAltF },
        { "UpSlashEffect F", AnimationClip.UpSlashEffectF },
        { "DownSlashEffect F", AnimationClip.DownSlashEffectF },
        { "DN Start", AnimationClip.DNStart },
        { "Death Dream", AnimationClip.DeathDream },
        { "Dreamer Land", AnimationClip.DreamerLand },
        { "Dreamer Lift", AnimationClip.DreamerLift },
        { "SD Crys Flash", AnimationClip.SDCrysFlash },
        { "SD Crys Shrink", AnimationClip.SDCrysShrink },
        { "DJ Get Land", AnimationClip.DJGetLand },
        { "Collect Acid", AnimationClip.CollectAcid },
        { "Shadow Dash Sharp", AnimationClip.ShadowDashSharp },
        { "Shadow Dash Down Sharp", AnimationClip.ShadowDashDownSharp },
        { "Slug Burst", AnimationClip.SlugBurst },
        { "Slug Down", AnimationClip.SlugDown },
        { "Slug Up", AnimationClip.SlugUp },
        { "Slug Turn Quick", AnimationClip.SlugTurnQuick },
        { "Slug Walk", AnimationClip.SlugWalk },
        { "Slug Idle S", AnimationClip.SlugIdleS },
        { "Slug Idle B", AnimationClip.SlugIdleB },
        { "Slug Idle BS", AnimationClip.SlugIdleBS },
        { "Slug Turn B", AnimationClip.SlugTurnB },
        { "Slug Turn B Quick", AnimationClip.SlugTurnBQuick },
        { "Slug Turn S", AnimationClip.SlugTurnS },
        { "Slug Turn S Quick", AnimationClip.SlugTurnSQuick },
        { "Slug Turn BS", AnimationClip.SlugTurnBS },
        { "Map Update", AnimationClip.MapUpdate },
        { "Slug Burst B", AnimationClip.SlugBurstB },
        { "Slug Burst S", AnimationClip.SlugBurstS },
        { "Slug Burst BS", AnimationClip.SlugBurstBS },
        { "Slug Walk B", AnimationClip.SlugWalkB },
        { "Slug Walk B Quick", AnimationClip.SlugWalkBQuick },
        { "Slug Walk S", AnimationClip.SlugWalkS },
        { "Slug Walk S Quick", AnimationClip.SlugWalkSQuick },
        { "Sit Idle", AnimationClip.SitIdle },
        { "Slug Walk BS", AnimationClip.SlugWalkBS },
        { "Slug Walk BS Quick", AnimationClip.SlugWalkBSQuick },
        { "Slug Turn BS Quick", AnimationClip.SlugTurnBSQuick },
        { "Collect SD 1 Back", AnimationClip.CollectSD1Back },
        { "Collect StandToIdle", AnimationClip.CollectStandToIdle },
        { "TurnFromBG", AnimationClip.TurnFromBG },
        { "Map Turn", AnimationClip.MapTurn },
        { "Exit", AnimationClip.Exit },
        { "Exit Door To Idle", AnimationClip.ExitDoorToIdle },
        { "Super Hard Land", AnimationClip.SuperHardLand },
        { "LookDownToIdle", AnimationClip.LookDownToIdle },
        { "DG Warp Charge", AnimationClip.DGWarpCharge },
        { "DG Warp", AnimationClip.DGWarp },
        { "DG Warp Cancel", AnimationClip.DGWarpCancel },
        { "DG Warp In", AnimationClip.DGWarpIn },
        { "Surface InToIdle", AnimationClip.SurfaceInToIdle },
        { "Surface InToSwim", AnimationClip.SurfaceInToSwim },
        { "Sprint", AnimationClip.Sprint },
        { "Look At King", AnimationClip.LookAtKing },
        { "Spike Death Antic", AnimationClip.SpikeDeathAntic },
    };

    /// <summary>
    /// Dictionary mapping animation clip enum values to IAnimationEffect instantiations.
    /// </summary>
    private static readonly Dictionary<AnimationClip, IAnimationEffect> AnimationEffects =
        new Dictionary<AnimationClip, IAnimationEffect> {
            { AnimationClip.SDChargeGround, new CrystalDashGroundCharge() },
            { AnimationClip.SDChargeGroundEnd, CrystalDashChargeCancel },
            { AnimationClip.SDWallCharge, new CrystalDashWallCharge() },
            { AnimationClip.SDDash, new CrystalDash() },
            { AnimationClip.SDAirBrake, new CrystalDashAirCancel() },
            { AnimationClip.SDHitWall, new CrystalDashHitWall() },
            { AnimationClip.Slash, new Slash() },
            { AnimationClip.SlashAlt, new AltSlash() },
            { AnimationClip.DownSlash, new DownSlash() },
            { AnimationClip.UpSlash, new UpSlash() },
            { AnimationClip.WallSlash, new WallSlash() },
            { AnimationClip.Fireball1Cast, new VengefulSpirit() },
            { AnimationClip.Fireball2Cast, new ShadeSoul() },
            { AnimationClip.QuakeAntic, new DiveAntic() },
            { AnimationClip.QuakeFall, new DesolateDiveDown() },
            { AnimationClip.QuakeFall2, new DescendingDarkDown() },
            { AnimationClip.QuakeLand, new DesolateDiveLand() },
            { AnimationClip.QuakeLand2, new DescendingDarkLand() },
            { AnimationClip.Scream, new HowlingWraiths() },
            { AnimationClip.Scream2, new AbyssShriek() },
            { AnimationClip.NACyclone, new CycloneSlash() },
            { AnimationClip.NACycloneEnd, new CycloneSlashEnd() },
            { AnimationClip.NABigSlash, new GreatSlash() },
            { AnimationClip.NADashSlash, new DashSlash() },
            { AnimationClip.Stun, new Stun() },
            { AnimationClip.Focus, Focus },
            { AnimationClip.FocusGet, FocusBurst },
            { AnimationClip.FocusGetOnce, FocusEnd },
            { AnimationClip.FocusEnd, FocusEnd },
            { AnimationClip.SlugDown, Focus },
            { AnimationClip.SlugBurst, FocusBurst },
            { AnimationClip.SlugBurstS, FocusBurst }, // Shape of Unn + Spore Shroom
            { AnimationClip.SlugBurstB, FocusBurst }, // Shape of Unn + Baldur Shell
            { AnimationClip.SlugBurstBS, FocusBurst }, // Shape of Unn + Spore Shroom + Baldur Shell
            { AnimationClip.SlugUp, FocusEnd },
            { AnimationClip.Dash, new Dash() },
            { AnimationClip.DashDown, new DashDown() },
            { AnimationClip.ShadowDash, new ShadowDash() },
            { AnimationClip.ShadowDashSharp, new ShadowDashSharp() },
            { AnimationClip.ShadowDashDown, new ShadowDashDown() },
            { AnimationClip.ShadowDashDownSharp, new ShadowDashSharpDown() },
            { AnimationClip.DashEnd, new DashEnd() },
            { AnimationClip.NailArtCharge, new NailArtCharge() },
            { AnimationClip.NailArtCharged, new NailArtCharged() },
            { AnimationClip.NailArtChargeEnd, NailArtEnd },
            { AnimationClip.WallSlide, new WallSlide() },
            { AnimationClip.WallSlideEnd, new WallSlideEnd() },
            { AnimationClip.Walljump, new WallJump() },
            { AnimationClip.DoubleJump, new MonarchWings() },
            { AnimationClip.HardLand, new HardLand() },
            { AnimationClip.HazardDeath, new HazardDeath() },
            { AnimationClip.DungTrail, new DungTrail() },
            { AnimationClip.DungTrailEnd, new DungTrailEnd() },
            { AnimationClip.ThornAttack, new ThornsOfAgony() },
            { AnimationClip.SurfaceIn, new SurfaceIn() }
        };

    /// <summary>
    /// The net client for sending animation updates.
    /// </summary>
    private readonly NetClient _netClient;

    /// <summary>
    /// The player manager to get player objects.
    /// </summary>
    private readonly PlayerManager _playerManager;

    /// <summary>
    /// The last animation clip sent.
    /// </summary>
    private string _lastAnimationClip;

    /// <summary>
    /// Whether the animation controller was responsible for the last clip that was sent.
    /// </summary>
    private bool _animationControllerWasLastSent;

    /// <summary>
    /// Whether we should stop sending animations until the scene has changed.
    /// </summary>
    private bool _stopSendingAnimationUntilSceneChange;

    /// <summary>
    /// Whether the current dash has ended and we can start a new one.
    /// </summary>
    private bool _dashHasEnded = true;

    /// <summary>
    /// Whether the player has sent that they stopped crystal dashing.
    /// </summary>
    private bool _hasSentCrystalDashEnd = true;

    /// <summary>
    /// Whether the charge effect was last update active.
    /// </summary>
    private bool _lastChargeEffectActive;

    /// <summary>
    /// Whether the charged effect was last update active
    /// </summary>
    private bool _lastChargedEffectActive;

    /// <summary>
    /// Stopwatch to keep track of a delay before being able to send another update for the charged effect.
    /// </summary>
    private readonly Stopwatch _chargedEffectStopwatch;

    /// <summary>
    /// Stopwatch to keep track of a delay before being able to send another update for the charged end effect.
    /// </summary>
    private readonly Stopwatch _chargedEndEffectStopwatch;

    /// <summary>
    /// Whether the player was wall sliding last update.
    /// </summary>
    private bool _lastWallSlideActive;

    public AnimationManager(
        NetClient netClient,
        PlayerManager playerManager,
        PacketManager packetManager,
        ServerSettings serverSettings
    ) {
        _netClient = netClient;
        _playerManager = playerManager;

        _chargedEffectStopwatch = new Stopwatch();
        _chargedEndEffectStopwatch = new Stopwatch();

        // Register packet handler
        packetManager.RegisterClientPacketHandler<GenericClientData>(ClientPacketId.PlayerDeath,
            OnPlayerDeath);

        // Register scene change, which is where we update the animation event handler
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;

        // Register callbacks for the hero animation controller for the Airborne animation
        On.HeroAnimationController.Play += HeroAnimationControllerOnPlay;
        On.HeroAnimationController.PlayFromFrame += HeroAnimationControllerOnPlayFromFrame;

        // Register callbacks for tracking to start of playing animation clips
        On.tk2dSpriteAnimator.WarpClipToLocalTime += Tk2dSpriteAnimatorOnWarpClipToLocalTime;
        On.tk2dSpriteAnimator.ProcessEvents += Tk2dSpriteAnimatorOnProcessEvents;

        // Register a callback so we know when the dash has finished
        On.HeroController.CancelDash += HeroControllerOnCancelDash;

        // Register a callback so we can check the nail art charge status
        ModHooks.HeroUpdateHook += OnHeroUpdateHook;

        // Register a callback for when we get hit by a hazard
        On.HeroController.DieFromHazard += HeroControllerOnDieFromHazard;
        // Also register a callback from when we respawn from a hazard
        On.GameManager.HazardRespawn += GameManagerOnHazardRespawn;

        // Register when the HeroController starts, so we can register dung trail events
        On.HeroController.Start += HeroControllerOnStart;

        // Relinquish Control cancels a lot of effects, so we need to broadcast the end of these effects
        On.HeroController.RelinquishControl += HeroControllerOnRelinquishControl;

        // Register when the player dies to send the animation
        ModHooks.BeforePlayerDeadHook += OnDeath;

        // Set the server settings for all animation effects
        foreach (var effect in AnimationEffects.Values) {
            effect.SetServerSettings(serverSettings);
        }
    }

    /// <summary>
    /// Callback method when a player animation update is received. Will update the player object with the new
    /// animation.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="clipId">The ID of the animation clip.</param>
    /// <param name="frame">The frame that the animation should play from.</param>
    /// <param name="effectInfo">A boolean array containing effect info for the animation.</param>
    public void OnPlayerAnimationUpdate(ushort id, int clipId, int frame, bool[] effectInfo) {
        UpdatePlayerAnimation(id, clipId, frame);

        var animationClip = (AnimationClip) clipId;

        if (AnimationEffects.ContainsKey(animationClip)) {
            var playerObject = _playerManager.GetPlayerObject(id);
            if (playerObject == null) {
                // Logger.Get().Warn(this, $"Tried to play animation effect {clipName} with ID: {id}, but player object doesn't exist");
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

    /// <summary>
    /// Update the animation of the player sprite animator.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="clipId">The ID of the animation clip.</param>
    /// <param name="frame">The frame that the animation should play from.</param>
    public void UpdatePlayerAnimation(ushort id, int clipId, int frame) {
        var playerObject = _playerManager.GetPlayerObject(id);
        if (playerObject == null) {
            // Logger.Get().Warn(this, $"Tried to update animation, but there was not matching player object for ID {id}");
            return;
        }

        var animationClip = (AnimationClip) clipId;
        if (!ClipEnumNames.ContainsSecond(animationClip)) {
            // This happens when we send custom clips, that can't be played by the sprite animator, so for now we
            // don't log it. This warning might be useful if we seem to be missing animations from the Knights
            // sprite animator.

            // Logger.Get().Warn(this, $"Tried to update animation, but there was no entry for clip ID: {clipId}, enum: {animationClip}");
            return;
        }

        var clipName = ClipEnumNames[animationClip];

        // Get the sprite animator and check whether this clip can be played before playing it
        var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
        if (spriteAnimator.GetClipByName(clipName) != null) {
            spriteAnimator.PlayFromFrame(clipName, frame);
        }
    }

    /// <summary>
    /// Callback method when the scene changes.
    /// </summary>
    /// <param name="oldScene">The old scene instance.</param>
    /// <param name="newScene">The name scene instance.</param>
    private void OnSceneChange(Scene oldScene, Scene newScene) {
        // A scene change occurs, so we can send again
        _stopSendingAnimationUntilSceneChange = false;
    }

    /// <summary>
    /// Callback method when an animation fires in the sprite animator.
    /// </summary>
    /// <param name="clip">The sprite animation clip.</param>
    private void OnAnimationEvent(tk2dSpriteAnimationClip clip) {
        // Logger.Info($"Animation event with name: {clip.name}");

        // If we are not connected, there is nothing to send to
        if (!_netClient.IsConnected) {
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

        // Skip clips that do not have the wrap mode loop, loop-section or once
        if (clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Loop &&
            clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.LoopSection &&
            clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once) {
            return;
        }

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

        // Keep track of when the player sends the start and end of the crystal dash animation
        if (clip.name.Equals("SD Dash")) {
            _hasSentCrystalDashEnd = false;
        }

        if (clip.name.Equals("SD Air Brake") || clip.name.Equals("SD Hit Wall")) {
            _hasSentCrystalDashEnd = true;
        }

        if (!ClipEnumNames.ContainsFirst(clip.name)) {
            Logger.Warn($"Player sprite animator played unknown clip, name: {clip.name}");
            return;
        }

        var animationClip = ClipEnumNames[clip.name];

        // Check whether there is an effect that adds info to this packet
        if (AnimationEffects.ContainsKey(animationClip)) {
            var effectInfo = AnimationEffects[animationClip].GetEffectInfo();

            _netClient.UpdateManager.UpdatePlayerAnimation(animationClip, 0, effectInfo);
        } else {
            _netClient.UpdateManager.UpdatePlayerAnimation(animationClip);
        }

        // Update the last clip name, since it changed
        _lastAnimationClip = clip.name;

        // We have sent a different clip, so we can reset this
        _animationControllerWasLastSent = false;
    }

    /// <summary>
    /// Callback method on the HeroAnimationController#Play method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The hero animation controller instance.</param>
    /// <param name="clipName">The name of the clip to play.</param>
    private void HeroAnimationControllerOnPlay(On.HeroAnimationController.orig_Play orig,
        HeroAnimationController self, string clipName) {
        orig(self, clipName);
        OnAnimationControllerPlay(clipName, 0);
    }

    /// <summary>
    /// Callback method on the HeroAnimationController#PlayFromFrame method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The hero animation controller instance.</param>
    /// <param name="clipName">The name of the clip to play.</param>
    /// <param name="frame">The frame from which to play the clip.</param>
    private void HeroAnimationControllerOnPlayFromFrame(On.HeroAnimationController.orig_PlayFromFrame orig,
        HeroAnimationController self, string clipName, int frame) {
        orig(self, clipName, frame);
        OnAnimationControllerPlay(clipName, frame);
    }

    /// <summary>
    /// Callback method when the HeroAnimationController plays an animation.
    /// </summary>
    /// <param name="clipName">The name of the clip to play.</param>
    /// <param name="frame">The frame from which to play the clip.</param>
    private void OnAnimationControllerPlay(string clipName, int frame) {
        // If we are not connected, there is nothing to send to
        if (!_netClient.IsConnected) {
            return;
        }

        // If this is not a clip that should be handled by the animation controller hook, we return
        if (!AnimationControllerClipNames.Contains(clipName)) {
            return;
        }

        // If the animation controller is responsible for the last sent clip, we skip
        // this is to ensure that we don't spam packets of the same clip
        if (!_animationControllerWasLastSent) {
            if (!ClipEnumNames.ContainsFirst(clipName)) {
                Logger.Warn($"Player animation controller played unknown clip, name: {clipName}");
                return;
            }

            var clipId = ClipEnumNames[clipName];

            _netClient.UpdateManager.UpdatePlayerAnimation(clipId, frame);

            // This was the last clip we sent
            _animationControllerWasLastSent = true;
        }
    }

    /// <summary>
    /// Callback method on the HeroController#CancelDash method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The HeroController instance.</param>
    private void HeroControllerOnCancelDash(On.HeroController.orig_CancelDash orig, HeroController self) {
        orig(self);

        // If we are not connected, there is nothing to send to
        if (!_netClient.IsConnected) {
            return;
        }

        _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.DashEnd);

        // The dash has ended, so we can send a new one when we dash
        _dashHasEnded = true;
    }

    /// <summary>
    /// Callback method for when the hero updates.
    /// </summary>
    private void OnHeroUpdateHook() {
        // If we are not connected, there is nothing to send to
        if (!_netClient.IsConnected) {
            return;
        }

        var chargeEffectActive = HeroController.instance.artChargeEffect.activeSelf;
        var chargedEffectActive = HeroController.instance.artChargedEffect.activeSelf;

        if (chargeEffectActive && !_lastChargeEffectActive) {
            // Charge effect is now active, which wasn't last update, so we can send the charge animation packet
            _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.NailArtCharge);
        }

        if (chargedEffectActive && !_lastChargedEffectActive) {
            if (!_chargedEffectStopwatch.IsRunning || _chargedEffectStopwatch.ElapsedMilliseconds > 100) {
                // Charged effect is now active, which wasn't last update, so we can send the charged animation packet
                _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.NailArtCharged);

                // Start the stopwatch to make sure this animation is not triggered repeatedly
                _chargedEffectStopwatch.Restart();
            }
        }

        if (!chargeEffectActive && _lastChargeEffectActive && !chargedEffectActive) {
            // The charge effect is now inactive and we are not fully charged
            // This means that we cancelled the nail art charge
            _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.NailArtChargeEnd);
        }

        if (!chargedEffectActive && _lastChargedEffectActive) {
            if (!_chargedEndEffectStopwatch.IsRunning || _chargedEndEffectStopwatch.ElapsedMilliseconds > 100) {
                // The charged effect is now inactive, so we are done with the nail art
                _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.NailArtChargeEnd);

                // Set the delay variable to make sure this animation is not triggered repeatedly
                _chargedEndEffectStopwatch.Restart();
            }
        }

        // Update the latest states
        _lastChargeEffectActive = chargeEffectActive;
        _lastChargedEffectActive = chargedEffectActive;

        // Obtain the current wall slide state
        var wallSlideActive = HeroController.instance.cState.wallSliding;

        if (!wallSlideActive && _lastWallSlideActive) {
            // We were wall sliding last update, but not anymore, so we send a wall slide end animation
            _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.WallSlideEnd);
        }

        // Update the last state
        _lastWallSlideActive = wallSlideActive;
    }

    /// <summary>
    /// Callback method on the tk2dSpriteAnimator#WarpClipToLocalTime method. This method executes
    /// the animation event for clips and we want to know when those clips start playing.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The tk2dSpriteAnimator instance.</param>
    /// <param name="clip">The tk2dSpriteAnimationClip instance.</param>
    /// <param name="time">The time to warp to.</param>
    private void Tk2dSpriteAnimatorOnWarpClipToLocalTime(
        On.tk2dSpriteAnimator.orig_WarpClipToLocalTime orig,
        tk2dSpriteAnimator self,
        tk2dSpriteAnimationClip clip,
        float time
    ) {
        orig(self, clip, time);

        var localPlayer = HeroController.instance;
        if (localPlayer == null) {
            return;
        }

        var spriteAnimator = localPlayer.GetComponent<tk2dSpriteAnimator>();
        if (self != spriteAnimator) {
            return;
        }

        var clipTime = ReflectionHelper.GetField<tk2dSpriteAnimator, float>(self, "clipTime");
        var index = (int) clipTime & clip.frames.Length;
        var frame = clip.frames[index];

        if (index == 0 || frame.triggerEvent || AllowedLoopAnimations.Contains(clip.name)) {
            OnAnimationEvent(clip);
        }
    }

    /// <summary>
    /// Callback method on the tk2dSpriteAnimator#OnProcessEvents method. This method executes
    /// the animation event for clips and we want to know when those clips start playing.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The tk2dSpriteAnimator instance.</param>
    /// <param name="start">The start of frames to process.</param>
    /// <param name="last">The last frame to process.</param>
    /// <param name="direction">The direction in which to process.</param>
    private void Tk2dSpriteAnimatorOnProcessEvents(
        On.tk2dSpriteAnimator.orig_ProcessEvents orig,
        tk2dSpriteAnimator self,
        int start,
        int last,
        int direction
    ) {
        orig(self, start, last, direction);

        var localPlayer = HeroController.instance;
        if (localPlayer == null) {
            return;
        }

        var spriteAnimator = localPlayer.GetComponent<tk2dSpriteAnimator>();
        if (self != spriteAnimator) {
            return;
        }

        if (start == last) {
            return;
        }

        var num = last + direction;
        var frames = self.CurrentClip.frames;

        var ignoreClipNames = new[] { "Quake Land 2" };

        for (var i = start + direction; i != num; i += direction) {
            if (i != 0 && !frames[i].triggerEvent || ignoreClipNames.Contains(self.CurrentClip.name)) {
                continue;
            }

            OnAnimationEvent(self.CurrentClip);
        }
    }

    /// <summary>
    /// Callback method on the HeroController#DieFromHazard method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The HeroController instance.</param>
    /// <param name="hazardType">The type of hazard.</param>
    /// <param name="angle">The angle at which the hero entered the hazard.</param>
    /// <returns>An enumerator for this coroutine.</returns>
    private IEnumerator HeroControllerOnDieFromHazard(On.HeroController.orig_DieFromHazard orig,
        HeroController self, HazardType hazardType, float angle) {
        // If we are not connected, there is nothing to send to
        if (!_netClient.IsConnected) {
            return orig(self, hazardType, angle);
        }

        _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.HazardDeath, 0, new[] {
            hazardType.Equals(HazardType.SPIKES),
            hazardType.Equals(HazardType.ACID)
        });

        // Execute the original method and return its value
        return orig(self, hazardType, angle);
    }

    /// <summary>
    /// Callback method on the GameManager#HazardRespawn method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The GameManager instance.</param>
    private void GameManagerOnHazardRespawn(On.GameManager.orig_HazardRespawn orig, GameManager self) {
        orig(self);

        // If we are not connected, there is nothing to send to
        if (!_netClient.IsConnected) {
            return;
        }

        _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.HazardRespawn);
    }

    /// <summary>
    /// Callback method for when a player death is received.
    /// </summary>
    /// <param name="data">The generic client data for this event.</param>
    private void OnPlayerDeath(GenericClientData data) {
        // And play the death animation for the ID in the packet
        MonoBehaviourUtil.Instance.StartCoroutine(PlayDeathAnimation(data.Id));
    }

    /// <summary>
    /// Callback method for when the local player dies.
    /// </summary>
    private void OnDeath() {
        // If we are not connected, there is nothing to send to
        if (!_netClient.IsConnected) {
            return;
        }

        Logger.Debug("Client has died, sending PlayerDeath data");

        // Let the server know that we have died            
        _netClient.UpdateManager.SetDeath();
    }

    /// <summary>
    /// Play the death animation for the player with the given ID.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator PlayDeathAnimation(ushort id) {
        Logger.Debug("Starting death animation");

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
        var localPlayerObject = HeroController.instance.gameObject;

        // Get the FSM for the Hero Death
        var heroDeathAnimFsm = localPlayerObject
            .FindGameObjectInChildren("Hero Death")
            .LocateMyFSM("Hero Death Anim");

        // Get the nail fling object from the Blow state
        var nailObject = heroDeathAnimFsm.GetFirstAction<FlingObjectsFromGlobalPool>("Blow");

        // Spawn it relative to the player
        var nailGameObject = nailObject.gameObject.Value.Spawn(
            playerObject.transform.position,
            Quaternion.Euler(Vector3.zero)
        );

        // Get the rigidbody component that we need to throw around
        var nailRigidBody = nailGameObject.GetComponent<Rigidbody2D>();

        // Get a random speed and angle and calculate the rigidbody velocity
        var speed = Random.Range(18, 22);
        float angle = Random.Range(50, 130);
        var velX = speed * Mathf.Cos(angle * ((float) System.Math.PI / 180f));
        var velY = speed * Mathf.Sin(angle * ((float) System.Math.PI / 180f));

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
        var headObject = heroDeathAnimFsm.GetFirstAction<CreateObject>(stateName);
        var headGameObject = Object.Instantiate(
            headObject.gameObject.Value,
            playerObject.transform.position + new Vector3(facingRight ? 0.2f : -0.2f, -0.02f, -0.01f),
            Quaternion.identity
        );

        // Get the rigidbody component of the head object
        var headRigidBody = headGameObject.GetComponent<Rigidbody2D>();

        // Calculate the angle at which we are going to throw 
        var headAngle = 15f * Mathf.Cos((facingRight ? 100f : 80f) * ((float) System.Math.PI / 180f));

        // Now set the velocity as this angle
        headRigidBody.velocity = new Vector2(headAngle, headAngle);

        // Finally add required torque (according to the FSM)
        headRigidBody.AddTorque(facingRight ? 20f : -20f);
    }

    /// <summary>
    /// Callback method on the HeroController#Start method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The HeroController instance.</param>
    private void HeroControllerOnStart(On.HeroController.orig_Start orig, HeroController self) {
        // Execute original method
        orig(self);

        SetDescendingDarkLandEffectDelay();
        RegisterDefenderCrestEffects();
    }

    /// <summary>
    /// Callback method on the HeroController#RelinquishControl method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The HeroController instance.</param>
    private void HeroControllerOnRelinquishControl(On.HeroController.orig_RelinquishControl orig,
        HeroController self) {
        orig(self);

        // If we are not connected, there is no need to send
        if (!_netClient.IsConnected) {
            return;
        }

        // If we need to stop sending until a scene change occurs, we skip
        if (_stopSendingAnimationUntilSceneChange) {
            return;
        }

        // If the player has not sent the end of the crystal dash animation then we need to do it now,
        // because crystal dash is cancelled when relinquishing control
        if (!_hasSentCrystalDashEnd) {
            _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.SDAirBrake);
        }

        _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.DashEnd);
    }

    /// <summary>
    /// Sets the delay for the descending dark land effect to trigger, since if we overwrite the
    /// AnimationTriggerEvent, it will fallback to 0.75s, which is too long. The event normally triggers
    /// at frame index 7, which is the 8th frame. The FPS of the animation is 20, which means 8/20 = 0.4s
    /// after the animation starts is when we need to finish the action in the FSM. If this is confusing
    /// check the "Spell Control" FSM of the knight and look at the "Q2 Land" state.
    /// </summary>
    private void SetDescendingDarkLandEffectDelay() {
        var spellControl = HeroController.instance.spellControl;
        var waitAction = spellControl.GetFirstAction<Wait>("Q2 Land");
        waitAction.time.Value = 0.4f;
    }

    /// <summary>
    /// Register/insert some method in the FSM for the Defenders Crest charm to send appropriate events
    /// based on when the charm is equipped/unequipped.
    /// </summary>
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
        var sendDungTrailEvent = new SendDungTrailEvent(_netClient);

        // Keep track of whether we subscribed to the update event already,
        // so we don't subscribe multiple times, with no way to unsubscribe those instances
        var isSubscribed = false;

        // Register the Update method of the SendDungTrailEvent class
        // when the Defender's Crest charm is equipped
        dungControlFsm.InsertMethod("Equipped", 1, () => {
            Logger.Debug("Defender's Crest is equipped, starting dung trail event sending");

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

            Logger.Debug("Defender's Crest is unequipped, stopping dung trail event sending");

            MonoBehaviourUtil.Instance.OnUpdateEvent -= sendDungTrailEvent.Update;
            sendDungTrailEvent.Reset();
            isSubscribed = false;

            if (!_netClient.IsConnected) {
                return;
            }

            _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.DungTrailEnd);
        });
    }

    /// <summary>
    /// Get the AnimationClip enum value for the currently playing animation of the local player.
    /// </summary>
    /// <returns>An AnimationClip enum value.</returns>
    public static AnimationClip GetCurrentAnimationClip() {
        var currentClipName = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name;

        if (ClipEnumNames.ContainsFirst(currentClipName)) {
            return ClipEnumNames[currentClipName];
        }

        return 0;
    }
}
