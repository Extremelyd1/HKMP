using System.Linq;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using Modding;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Save;

/// <summary>
/// Class that handles incoming save changes that have an immediate effect in the current scene for the local player.
/// E.g. breakable walls that also break in another scene, tollgates that are being paid, stag station being bought.
/// </summary>
internal class SaveChanges {
    /// <summary>
    /// Apply a change in player data from a save update for the given name immediately. This checks whether
    /// the local player is in a scene where the changes in player data have an effect on the environment.
    /// For example, a breakable wall that also opens up in another scene or a stag station being bought.
    /// </summary>
    /// <param name="name">The name of the PlayerData entry.</param>
    public void ApplyPlayerDataSaveChange(string name) {
        Logger.Debug($"ApplyPlayerData for name: {name}");

        // If we receive the dash from a save update, we need to also set the 'canDash' boolean to ensure that
        // the input for dashing is accepted
        if (name == "hasDash") {
            PlayerData.instance.SetBool("canDash", true);
            return;
        }
        
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (name == "crossroadsMawlekWall" && currentScene == "Crossroads_33") {
            GameObject breakWall = null;
            PlayMakerFSM breakWallFsm = null;
            PlayMakerFSM fullWallFsm = null;

            var found = 0;
            
            var fsms = Object.FindObjectsOfType<PlayMakerFSM>(true);
            foreach (var fsm in fsms) {
                if (fsm.Fsm.Name.Equals("playerdata_activation")) {
                    if (fsm.name.Equals("break_wall_left")) {
                        breakWall = fsm.gameObject;
                        breakWallFsm = fsm;

                        found++;
                        if (found == 2) {
                            break;
                        }
                    } else if (fsm.name.Equals("full_wall_left")) {
                        fullWallFsm = fsm;

                        found++;
                        if (found == 2) {
                            break;
                        }
                    }
                }
            }

            if (found < 2) {
                Logger.Error("Could not find breakable wall objects for 'crossroadsMawlekWall' player data change");
                return;
            }

            // Activate the breakWall object and set variable and state in the FSM
            breakWall!.SetActive(true);
            breakWallFsm.FsmVariables.GetFsmBool("Activate").Value = true;
            breakWallFsm.SetState("Check Activation");
            
            // Disable the full wall
            fullWallFsm!.SetState("Check Activation");
            return;
        }

        if (name == "dungDefenderWallBroken" && currentScene == "Abyss_01") {
            GameObject wall = null;
            GameObject wallBroken = null;

            var found = 0;

            var fsms = Object.FindObjectsOfType<PlayMakerFSM>(true);
            foreach (var fsm in fsms) {
                if (fsm.name.Equals("dung_defender_wall")) {
                    wall = fsm.gameObject;

                    found++;
                    if (found == 2) {
                        break;
                    }
                } else if (fsm.name.Equals("dung_defender_wall_broken")) {
                    wallBroken = fsm.gameObject;

                    found++;
                    if (found == 2) {
                        break;
                    }
                }
            }
            
            if (found < 2) {
                Logger.Error("Could not find breakable wall objects for 'dungDefenderWallBroken' player data change");
                return;
            }

            wall!.SetActive(false);
            wallBroken!.SetActive(true);
            return;
        }

        if (
            name == "openedCrossroads" && currentScene == "Crossroads_47" || 
            name == "openedGreenpath" && currentScene == "Fungus1_16_alt" ||
            name == "openedFungalWastes" && currentScene == "Fungus2_02" ||
            name == "openedRuins1" && currentScene == "Ruins1_29" ||
            name == "openedRuins2" && currentScene == "Ruins2_08" ||
            name == "openedDeepnest" && currentScene == "Deepnest_09" ||
            name == "openedRoyalGardens" && currentScene == "Fungus3_40" ||
            name == "openedHiddenStation" && currentScene == "Abyss_22"
        ) {
            var go = GameObject.Find("Station Bell");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Stag Bell");
            if (fsm == null) {
                return;
            }

            // Add additional actions from different states to ensure the player gets control back if they are already
            // in the FSM flow
            if (IsInTollMachineDialogue(fsm.Fsm.ActiveStateName)) {
                var action1 = fsm.GetFirstAction<CallMethodProper>("Yes");
                fsm.InsertAction("Box Disappear Anim", action1, 0);
                var action2 = fsm.GetFirstAction<Tk2dPlayAnimationWithEvents>("Yes");
                action2.animationCompleteEvent = null;
                fsm.InsertAction("Box Disappear Anim", action2, 0);
                var action3 = fsm.GetFirstAction<SendEventByName>("Yes");
                fsm.InsertAction("Box Disappear Anim", action3, 0);
                var action4 = fsm.GetFirstAction<CallMethodProper>("Pause Before Box Drop");
                fsm.InsertAction("Box Disappear Anim", action4, 0);
                var action5 = fsm.GetFirstAction<SetPlayerDataBool>("Pause Before Box Drop");
                fsm.InsertAction("Box Disappear Anim", action5, 0);
                var action6 = fsm.GetAction<CallMethodProper>("Pause Before Box Drop", 2);
                fsm.InsertAction("Box Disappear Anim", action6, 0);

                HideDialogueBox();
            }

            fsm.SetState("Box Disappear Anim");
            return;
        }

        if (
            name == "tollBenchCity" && currentScene == "Ruins1_31" ||
            name == "tollBenchAbyss" && currentScene == "Abyss_18" ||
            name == "tollBenchQueensGardens" && currentScene == "Fungus3_50"
        ) {
            var go = GameObject.Find("Toll Machine Bench");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Toll Machine Bench");
            if (fsm == null) {
                return;
            }

            // Add additional actions from different states to ensure the player gets control back if they are already
            // in the FSM flow
            if (IsInTollMachineDialogue(fsm.Fsm.ActiveStateName)) {
                var action1 = fsm.GetFirstAction<Tk2dPlayAnimationWithEvents>("Yes");
                action1.animationCompleteEvent = null;
                fsm.InsertAction("Box Down", action1, 0);
                var action2 = fsm.GetFirstAction<SendEventByName>("Yes");
                fsm.InsertAction("Box Down", action2, 0);
                var action3 = fsm.GetFirstAction<CallMethodProper>("Pause Before Box Drop");
                fsm.InsertAction("Box Down", action3, 0);
                var action4 = fsm.GetFirstAction<SetPlayerDataBool>("Pause Before Box Drop");
                fsm.InsertAction("Box Down", action4, 0);
                var action5 = fsm.GetAction<CallMethodProper>("Pause Before Box Drop", 3);
                fsm.InsertAction("Box Down", action5, 0);

                HideDialogueBox();
            }

            fsm.SetState("Box Down");
            return;
        }

        if (name == "openedRestingGrounds02" && currentScene == "RestingGrounds_02") {
            var go = GameObject.Find("Bottom Gate Collider");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("FSM");
            if (fsm == null) {
                return;
            }

            fsm.SetState("Destroy");
            return;
        }

        if (name == "waterwaysGate" && currentScene == "Fungus2_23") {
            var go = GameObject.Find("Waterways Gate");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Gate Control");
            if (fsm == null) {
                return;
            }

            fsm.SetState("Destroy");
            return;
        }

        if (name == "deepnestWall") {
            GameObject go = null;
            if (currentScene == "Deepnest_01") {
                go = GameObject.Find("Breakable Wall");
            } else if (currentScene == "Fungus2_20") {
                go = GameObject.Find("Breakable Wall Waterways");
            }

            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("breakable_wall_v2");
            if (fsm == null) {
                return;
            }

            fsm.SetState("Pause Frame");
            return;
        }

        if (name == "oneWayArchive" && currentScene == "Fungus3_02") {
            var go = GameObject.Find("One Way Wall Exit");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("FSM");
            if (fsm == null) {
                return;
            }

            fsm.SetState("Destroy");
            return;
        }

        if (name == "openedGardensStagStation" && currentScene == "Fungus3_13") {
            var go = GameObject.Find("royal_garden_slide_door");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("FSM");
            if (fsm == null) {
                return;
            }

            fsm.SetState("Destroy");
            return;
        }

        if (name == "openedCityGate" && currentScene == "Fungus2_21") {
            var go = GameObject.Find("City Gate Control");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Conversation Control");
            if (fsm == null) {
                return;
            }
            
            string[] inDialogueStateNames = [
                "Hero Anim", "Key?", "Box Up YN", "Send Text", "Box Up", "No Key"
            ];
            if (inDialogueStateNames.Contains(fsm.Fsm.ActiveStateName)) {
                var action1 = fsm.GetFirstAction<SendEventByName>("Yes");
                fsm.InsertAction("Activate", action1, 0);
                
                HideDialogueBox();
            }
            
            fsm.RemoveFirstAction<SetPlayerDataBool>("Activate");
            fsm.RemoveFirstAction<SetPlayerDataBool>("Activate");

            fsm.SetState("Activate");
            return;
        }

        if (name == "openedWaterwaysManhole" && currentScene == "Ruins1_05b") {
            var go = GameObject.Find("Waterways Machine");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Conversation Control");
            if (fsm == null) {
                return;
            }

            string[] inDialogueStateNames = [
                "Hero Anim", "Key?", "Box Up YN", "Send Text", "Box Up", "No Key"
            ];
            if (inDialogueStateNames.Contains(fsm.Fsm.ActiveStateName)) {
                var action1 = fsm.GetFirstAction<SendEventByName>("Yes");
                fsm.InsertAction("Activate", action1, 0);
                
                HideDialogueBox();
            }

            fsm.RemoveFirstAction<SetPlayerDataInt>("Activate");
            fsm.RemoveFirstAction<SetPlayerDataBool>("Activate");
            fsm.RemoveFirstAction<SetPlayerDataBool>("Activate");
            
            fsm.SetState("Activate");
            return;
        }

        if (name == "xunFlowerGiven" && currentScene == "Fungus3_49") {
            var go = GameObject.Find("Inspect Region");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Conversation Control");
            if (fsm == null) {
                return;
            }

            // Remove a bunch of actions that only apply to the placing player
            fsm.RemoveFirstAction<SetPlayerDataBool>("Flowers");
            fsm.RemoveFirstAction<Tk2dPlayAnimationWithEvents>("Ghost Appear");
            fsm.RemoveFirstAction<FaceObject>("Ghost Appear");
            fsm.RemoveFirstAction<Tk2dPlayAnimation>("Look Up");
            fsm.RemoveFirstAction<Tk2dPlayAnimation>("Get Up");

            fsm.SetState("Glow");
            return;
        }
        
        if (name == "openedMageDoor_v2" && currentScene == "Ruins1_31") {
            var go = GameObject.Find("Mage Door");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Conversation Control");
            if (fsm == null) {
                return;
            }
            
            string[] inDialogueStateNames = [
                "Hero Anim", "Check Key", "Box Up YN", "Send Text", "Box Up", "No Key"
            ];
            if (inDialogueStateNames.Contains(fsm.Fsm.ActiveStateName)) {
                HideDialogueBox();
            } else {
                fsm.RemoveFirstAction<Tk2dPlayAnimationWithEvents>("Yes");
            }
            
            fsm.RemoveFirstAction<SetPlayerDataBool>("Yes");

            fsm.SetState("Yes");
            return;
        }

        if (name == "cityLift1" && currentScene == "Crossroads_49b") {
            var go = GameObject.Find("Toll Machine Lift");
            var fsm = go.LocateMyFSM("Toll Machine");

            // Hide the dialogue box if the local player is in the dialogue flow
            if (IsInTollMachineDialogue(fsm.Fsm.ActiveStateName)) {
                HideDialogueBox();
            }
            
            fsm.RemoveFirstAction<SetPlayerDataBool>("Send Message");

            fsm.SetState("Yes");
            return;
        }
        
        if (name == "nightmareLanternAppeared" && currentScene == "Cliffs_06") {
            var go = GameObject.Find("Sycophant Dream");
            var fsm = go.LocateMyFSM("Activate Lantern");
            
                        fsm.SetState("Impact");
        }
    }

    /// <summary>
    /// Apply a change in persistent values from a save update for the given name immediately. This checks whether
    /// the local player is in a scene where the changes in player data have an effect on the environment.
    /// For example, a breakable wall that also opens up in another scene or a stag station being bought.
    /// </summary>
    /// <param name="itemKey">The persistent item key containing the ID and scene name of the changed object.</param>
    public void ApplyPersistentValueSaveChange(PersistentItemKey itemKey) {
        Logger.Debug($"ApplyPersistent for item data: {itemKey}");

        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (
            itemKey.Id.StartsWith("Toll Gate Machine") && ( 
            itemKey.SceneName == "Mines_33" && currentScene == "Mines_33" ||
            itemKey.SceneName == "Fungus1_31" && currentScene == "Fungus1_31"
        )) {
            var go = GameObject.Find("Toll Gate Machine");
            var fsm = go.LocateMyFSM("Toll Machine");

            // Add additional actions from different states to ensure the player gets control back if they are already
            // in the FSM flow
            if (IsInTollMachineDialogue(fsm.Fsm.ActiveStateName)) {
                var action1 = fsm.GetFirstAction<Tk2dPlayAnimationWithEvents>("Yes");
                action1.animationCompleteEvent = null;
                fsm.InsertAction("Box Disappear Anim", action1, 0);
                var action2 = fsm.GetFirstAction<SendEventByName>("Yes");
                fsm.InsertAction("Box Disappear Anim", action2, 0);
                var action3 = fsm.GetFirstAction<CallMethodProper>("Pause Before Box Drop");
                fsm.InsertAction("Box Disappear Anim", action3, 0);
                var action4 = fsm.GetFirstAction<SetPlayerDataBool>("Pause Before Box Drop");
                fsm.InsertAction("Box Disappear Anim", action4, 0);
                var action5 = fsm.GetAction<CallMethodProper>("Pause Before Box Drop", 2);
                fsm.InsertAction("Box Disappear Anim", action5, 0);

                HideDialogueBox();
            }

            fsm.RemoveFirstAction<SetBoolValue>("Open Gates");

            fsm.SetState("Box Disappear Anim");
            return;
        }
        
        if (itemKey.Id == "Collapser Tute 01" && itemKey.SceneName == "Tutorial_01" && currentScene == "Tutorial_01") {
            var go = GameObject.Find("Collapser Tute 01");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("collapse tute");
            fsm.RemoveFirstAction<SendMessage>("Force Hard Landing");
            fsm.SetState("Crumble");
            return;
        }

        if (itemKey.Id.StartsWith("Collapser Small") && itemKey.SceneName == currentScene) {
            var go = GameObject.Find(itemKey.Id);
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("collapse small");
            if (fsm == null) {
                return;
            }

            fsm.SetState("Split");
            return;
        }

        if (itemKey.Id.StartsWith("Quake Floor") && itemKey.SceneName == currentScene) {
            var go = GameObject.Find(itemKey.Id);
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("quake_floor");
            if (fsm == null) {
                return;
            }

            fsm.SetState("Audio");
        }

        if (itemKey.Id == "Bone Gate" && itemKey.SceneName == currentScene) {
            var go = GameObject.Find(itemKey.Id);
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Bone Gate");
            if (fsm == null) {
                return;
            }

            fsm.SetState("Open Audio");
        }
    }

    /// <summary>
    /// Whether the local player is currently in a toll machine dialogue prompt that has claimed control of the
    /// character.
    /// </summary>
    /// <param name="currentStateName">The name of the current state of the dialogue FSM.</param>
    /// <returns>true if the player is in dialogue, false otherwise.</returns>
    private bool IsInTollMachineDialogue(string currentStateName) {
        string[] outOfDialogueStateNames = [
            "Out Of Range", "In Range", "Can Inspect?", "Cancel Frame", "Pause", "Activated?", "Paid?", "Get Price", "Init", 
            "Regain Control"
        ];

        return !outOfDialogueStateNames.Contains(currentStateName);
    }

    /// <summary>
    /// Hide the currently active dialogue box by setting the state of the 'Dialogue Page Control' FSM of the 'Text YN'
    /// game object. Needs to be amended if this method should also hide dialogue boxes of other dialogue types.
    /// </summary>
    private void HideDialogueBox() {
        var gc = GameCameras.instance;
        if (gc == null) {
            Logger.Warn("Could not find GameCameras instance");
            return;
        }

        var hudCamera = gc.hudCamera;
        if (hudCamera == null) {
            Logger.Warn("Could not find hudCamera");
            return;
        }

        var dialogManager = hudCamera.gameObject.FindGameObjectInChildren("DialogueManager");
        if (dialogManager == null) {
            Logger.Warn("Could not find dialogueManager");
            return;
        }

        void HideDialogueObject(string objectName, string heroDmgState) {
            var obj = dialogManager.FindGameObjectInChildren(objectName);
            if (obj != null) {
                var dialogueBox = obj.GetComponent<DialogueBox>();
                if (dialogueBox == null) {
                    Logger.Warn($"Could not find {objectName} DialogueBox");
                    return;
                }

                var hidden = ReflectionHelper.GetField<DialogueBox, bool>(dialogueBox, "hidden");
                if (hidden) {
                    return;
                }
            
                var pageControlFsm = obj.LocateMyFSM("Dialogue Page Control");
                if (pageControlFsm == null) {
                    Logger.Warn($"Could not find {objectName} DialoguePageControl FSM");
                    return;
                }
                pageControlFsm.SetState(heroDmgState);
            }
        }
        
        HideDialogueObject("Text YN", "Hero Damaged");
        HideDialogueObject("Text", "Pause");
    }
}
