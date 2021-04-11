using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using UnityEngine;
using System.Linq;


using HKMP.Game.Client;

namespace HKMP.ServerKnights {
    public class skinSources{
        public string[] skins;
    }

    public class skin{
        public string Name;
        public string Author;
        public string Knight;
        public string Sprint;
        public clientSkin loadedSkin;
    }

    public class clientSkin{

        public string Name;
        public string Author;
        public Texture2D Knight ;

        public Texture2D Sprint;

        public clientSkin(){}
    }

    public class serverJson{
        public string Name;
        public string Host;
        public string skin_1;
        public string skin_2;
        public string skin_3;
        public string skin_4;
        public string skin_5;
        public string skin_6;
        public string skin_7;
        public string skin_8;
        public string skin_9;
    }

    public class skinKeys{
        public string skin_0;
        public string skin_1;
        public string skin_2;
        public string skin_3;
        public string skin_4;
        public string skin_5;
        public string skin_6;
        public string skin_7;
        public string skin_8;
        public string skin_9;
    }
    
    public class SkinManager{
        public ServerKnightsManager _serverKnightsManager;

        private string DATA_DIR;
        private string serverJsonPath;
        private string sessionJsonPath;
        
        private string serverJson;
        public serverJson session;

        public string[] skinsArray = new string[10];

        public ushort LocalPlayerSkin = 0;

        public static List<string> SpriteNames = new List<string>
        {

            "Knight",
            "Sprint"

            //"Unn",
            //"Wraiths",
            //"VoidSpells",
            //"VS",
            //"Fluke",
            //"Shield",
            //"Baldur",

            //"Hatchling",
            //"Grimm",
            //"Weaver",
            
            //"Hud",
            //"OrbFull",
            //"Geo",
            //"Inventory",

            //"Dreamnail",
            //"DreamArrival",
            //"Wings",
            //"Quirrel",
            //"Webbed",
            //"Cloak",
            //"Shriek",
            //"Hornet",
            //"Birthplace",
        };

        public SkinLoader skinLoader = new SkinLoader();

        private skinKeys Keys;

        public static void updateTextureInMaterialPropertyBlock(GameObject go, Texture t){
            var materialPropertyBlock = new MaterialPropertyBlock();
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if(renderer != null){
                renderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetTexture("_MainTex", t);
                go.GetComponent<MeshRenderer>().SetPropertyBlock(materialPropertyBlock);
            } else {
                Logger.Info(new UnityEngine.Object(),$" no mesh renderer on {go.name}");
            }
        }
        
        public void updateRemotePlayerSkin(ClientPlayerData player, ushort skin){
            var old = player.Skin;
            player.Skin = skin;
            clientSkin playerSkin = getSkinForIndex(skin);
            // Get the player object and update the skin
            SkinManager.updateTextureInMaterialPropertyBlock(player.PlayerObject, playerSkin.Knight);
            if(old != skin){
                UI.UIManager.InfoBox.AddMessage($"Player '{player.Username}' is now {getSkinNameForIndex(skin)}");
            }
        }

        public void updateLocalPlayerSkin(ushort skin){
            var old = LocalPlayerSkin;
            LocalPlayerSkin = skin;

            GameObject player = HeroController.instance.gameObject;
            clientSkin playerSkin = getSkinForIndex(skin);

            var anim = player.GetComponent<tk2dSpriteAnimator>();
            
            anim.GetClipByName("Idle").frames[0].spriteCollection.spriteDefinitions[0].material.mainTexture = playerSkin.Knight;
            anim.GetClipByName("Sprint").frames[0].spriteCollection.spriteDefinitions[0].material.mainTexture = playerSkin.Sprint;
            if(old != skin){
                //todo network call here
                _serverKnightsManager.sendServerKnightUpdate(0,(ushort) skin);
                UI.UIManager.InfoBox.AddMessage($"You are now {getSkinNameForIndex(skin)}");
            }
        }

        public void listenForInput(){
            //todo This is easier than a ui but a ui would be better
            if(Input.GetKeyDown(Keys.skin_0)){
                updateLocalPlayerSkin(0);
            } else if(Input.GetKeyDown(Keys.skin_1)){
                updateLocalPlayerSkin(1);
            } else if(Input.GetKeyDown(Keys.skin_2)){
                updateLocalPlayerSkin(2);
            } else if(Input.GetKeyDown(Keys.skin_3)){
                updateLocalPlayerSkin(3);
            } else if(Input.GetKeyDown(Keys.skin_4)){
                updateLocalPlayerSkin(4);
            } else if(Input.GetKeyDown(Keys.skin_5)){
                updateLocalPlayerSkin(5);
            } else if(Input.GetKeyDown(Keys.skin_6)){
                updateLocalPlayerSkin(6);
            } else if(Input.GetKeyDown(Keys.skin_7)){
                updateLocalPlayerSkin(7);
            } else if(Input.GetKeyDown(Keys.skin_8)){
                updateLocalPlayerSkin(8);
            } else if(Input.GetKeyDown(Keys.skin_9)){
                updateLocalPlayerSkin(9);
            }
            return;
        }

        public clientSkin getSkinForIndex(int i){
            if(!skinLoader.loadedInMemory) {
                return skinLoader.KnightMap["defaultSkin"];
            }
            if(skinsArray.Length > i && skinsArray[i].Length > 0){
                if(skinLoader.KnightMap.ContainsKey(skinsArray[i])){
                    return skinLoader.KnightMap[skinsArray[i]];
                }
            }
            return skinLoader.KnightMap["defaultSkin"];
        }

        public string getSkinNameForIndex(int i){
            if(!skinLoader.loadedInMemory) {return skinLoader.KnightMap["defaultSkin"].Name;}
            if(skinsArray.Length > i && skinsArray[i].Length > 0){
                if(skinLoader.KnightMap.ContainsKey(skinsArray[i])){
                    return skinLoader.KnightMap[skinsArray[i]].Name;
                }
            }
            return skinLoader.KnightMap["defaultSkin"].Name;
        }
        
        public string getServerJson(){
            if(serverJson != null){
                return serverJson;
            }
            serverJson = File.ReadAllText(serverJsonPath);
            return serverJson;
        }

        public void clientSetSession(serverJson currentSession) {
            //write current session to disk for debug
            session = currentSession;
            var sessionjson = JsonUtility.ToJson(currentSession);
            File.WriteAllText(sessionJsonPath,sessionjson);
            UI.UIManager.InfoBox.AddMessage($"Welcome to {currentSession.Name}");
            UI.UIManager.InfoBox.AddMessage($"Hosted by {currentSession.Host}");
        }

        private void addSkin(int index,string skinUrl){
            skinsArray[index] = skinUtils.Base64Encode(skinUrl);
            skinLoader.addSkin(skinUrl);
        }
        public bool checkSessionAndLoadSkins(){
            if(session == null) {return false;}
            UI.UIManager.InfoBox.AddMessage($"Checking skins");
            skinsArray[0] = "defaultSkin";

            // add all skins
            addSkin(1,session.skin_1);
            addSkin(2,session.skin_2);
            addSkin(3,session.skin_3);
            addSkin(4,session.skin_4);
            addSkin(5,session.skin_5);
            addSkin(6,session.skin_6);
            addSkin(7,session.skin_7);
            addSkin(8,session.skin_8);
            addSkin(9,session.skin_9);

            skinLoader.loadSkins();
            return true;
        }

        public SkinManager(ServerKnightsManager serverKnightsManager){
            _serverKnightsManager = serverKnightsManager;
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.MacOSX:
                    DATA_DIR = Path.GetFullPath(Application.dataPath + "/Resources/Data/Managed/Mods/ServerKnights");
                    break;
                default:
                    DATA_DIR = Path.GetFullPath(Application.dataPath + "/Managed/Mods/ServerKnights");
                    break;
            }
            serverJsonPath =  DATA_DIR + "/server.json";
            sessionJsonPath = DATA_DIR + "/session.json"; 
            ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
            Keys = ServerKnightsManager.loadKeyBindings<skinKeys>("bindings.json");
        }

        
    }
}