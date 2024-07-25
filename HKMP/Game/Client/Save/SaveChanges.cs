using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
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
            var fsm = go.LocateMyFSM("Stag Bell");

            fsm.SetState("Box Disappear Anim");
            return;
        }

        if (
            name == "tollBenchCity" && currentScene == "Ruins1_31" ||
            name == "tollBenchAbyss" && currentScene == "Abyss_18" ||
            name == "tollBenchQueensGardens" && currentScene == "Fungus3_50"
        ) {
            var go = GameObject.Find("Toll Machine Bench");
            var fsm = go.LocateMyFSM("Toll Machine Bench");

            fsm.SetState("Box Down");
            return;
        }

        if (name == "openedRestingGrounds02" && currentScene == "RestingGrounds_02") {
            var go = GameObject.Find("Bottom Gate Collider");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("FSM");
            fsm.SetState("Destroy");
            return;
        }

        if (name == "waterwaysGate" && currentScene == "Fungus2_23") {
            var go = GameObject.Find("Waterways Gate");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("Gate Control");
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
            fsm.SetState("Pause Frame");
            return;
        }

        if (name == "oneWayArchive" && currentScene == "Fungus3_02") {
            var go = GameObject.Find("One Way Wall Exit");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("FSM");
            fsm.SetState("Destroy");
            return;
        }

        if (name == "openedGardensStagStation" && currentScene == "Fungus3_13") {
            var go = GameObject.Find("royal_garden_slide_door");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("FSM");
            fsm.SetState("Destroy");
        }

        if (name == "openedCityGate" && currentScene == "Fungus2_21") {
            var go = GameObject.Find("City Gate Control");
            var fsm = go.LocateMyFSM("Conversation Control");

            fsm.SetState("Activate");
        }
    }

    /// <summary>
    /// Apply a change in persistent values from a save update for the given name immediately. This checks whether
    /// the local player is in a scene where the changes in player data have an effect on the environment.
    /// For example, a breakable wall that also opens up in another scene or a stag station being bought.
    /// </summary>
    /// <param name="itemData">The persistent item data containing the ID and scene name of the changed object.</param>
    public void ApplyPersistentValueSaveChange(PersistentItemData itemData) {
        Logger.Debug($"ApplyPersistent for item data: {itemData}");

        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (
            itemData.Id.StartsWith("Toll Gate Machine") && ( 
            itemData.SceneName == "Mines_33" && currentScene == "Mines_33" ||
            itemData.SceneName == "Fungus1_31" && currentScene == "Fungus1_31"
        )) {
            var go = GameObject.Find("Toll Gate Machine");
            var fsm = go.LocateMyFSM("Toll Machine");
            
            fsm.RemoveFirstAction<SetBoolValue>("Open Gates");
            
            fsm.SetState("Box Disappear Anim");
            return;
        }
        
        if (itemData.Id == "Collapser Tute 01" && itemData.SceneName == "Tutorial_01" && currentScene == "Tutorial_01") {
            var go = GameObject.Find("Collapser Tute 01");
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("collapse tute");
            fsm.RemoveFirstAction<SendMessage>("Force Hard Landing");
            fsm.SetState("Crumble");
            return;
        }

        if (itemData.Id.StartsWith("Collapser Small") && (
            itemData.SceneName == "Crossroads_21" && currentScene == "Crossroads_21" ||
            itemData.SceneName == "Crossroads_36" && currentScene == "Crossroads_36" ||
            itemData.SceneName == "Fungus1_24" && currentScene == "Fungus1_24" ||
            itemData.SceneName == "Fungus2_23" && currentScene == "Fungus2_23" ||
            itemData.SceneName == "Fungus3_28" && currentScene == "Fungus3_28" ||
            itemData.SceneName == "Fungus2_25" && currentScene == "Fungus2_25" ||
            itemData.SceneName == "Mines_06" && currentScene == "Mines_06" ||
            itemData.SceneName == "Deepnest_02" && currentScene == "Deepnest_02" ||
            itemData.SceneName == "Deepnest_03" && currentScene == "Deepnest_03" ||
            itemData.SceneName == "Deepnest_14" && currentScene == "Deepnest_14" ||
            itemData.SceneName == "Deepnest_16" && currentScene == "Deepnest_16" || 
            itemData.SceneName == "Deepnest_30" && currentScene == "Deepnest_30" ||
            itemData.SceneName == "Deepnest_33" && currentScene == "Deepnest_33" ||
            itemData.SceneName == "Deepnest_38" && currentScene == "Deepnest_38" ||
            itemData.SceneName == "Deepnest_39" && currentScene == "Deepnest_39" || 
            itemData.SceneName == "Deepnest_41" && currentScene == "Deepnest_41" ||
            itemData.SceneName == "Deepnest_45_v02" && currentScene == "Deepnest_45_v02" ||
            itemData.SceneName == "RestingGrounds_10" && currentScene == "RestingGrounds_10" ||
            itemData.SceneName == "Deepnest_Spider_Town" && currentScene == "Deepnest_Spider_Town" ||
            itemData.SceneName == "Waterways_09" && currentScene == "Waterways_09" ||
            itemData.SceneName == "Waterways_14" && currentScene == "Waterways_14" ||
            itemData.SceneName == "GG_Pipeway" && currentScene == "GG_Pipeway" ||
            itemData.SceneName == "White_Palace_02" && currentScene == "White_Palace_02" ||
            itemData.SceneName == "White_Palace_17" && currentScene == "White_Palace_17"
        )) {
            var go = GameObject.Find(itemData.Id);
            if (go == null) {
                return;
            }

            var fsm = go.LocateMyFSM("collapse small");
            fsm.SetState("Split");
        }
    }
}
