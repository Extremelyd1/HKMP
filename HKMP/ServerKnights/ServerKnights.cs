
using Modding;
using System;
using System.IO;
using HKMP.Game.Client;
using HKMP.Networking;
using HKMP.Networking.Client;
using UnityEngine;

namespace HKMP.ServerKnights {


    public class ServerKnightsManager{

        public SkinManager skinManager;
        public EmoteManager emoteManager;
        public NetworkManager _networkManager;

        private bool savedDefaultSkins = false;
        private bool skinsInit = false;
        private bool connected = false;
        public ServerKnightsManager(){
            skinManager = new SkinManager(this);
            emoteManager = new EmoteManager(this);
            ModHooks.Instance.HeroUpdateHook += onHeroUpdate;
        }
        
        public void saveDefaultSkins()
        {
            if(savedDefaultSkins == false) {
                clientSkin defaultSkin = skinUtils.saveDefaultSkin();
                if(!skinManager.skinLoader.KnightMap.ContainsKey("defaultSkin")){
                    skinManager.skinLoader.KnightMap.Add("defaultSkin",defaultSkin);
                }
                skinUtils.patchAllSkins(skinManager.skinLoader.KnightMap);
                savedDefaultSkins = true;
            }
            return;
        }

        public void disconnected(){
            opacityDiff = -0.1f;
            skinManager.LocalPlayerSkin = 0;
            skinManager.updateLocalPlayerSkin(skinManager.LocalPlayerSkin);
            setPlayerOpacity(1.0f);
            skinManager.session = null;
            skinManager.skinLoader = new SkinLoader();
            skinManager.skinLoader.loadedInMemory = false;
            skinManager.skinLoader.loadInMemory = false;
        }
        public void updateConnected(bool clientConnected){
            connected = clientConnected;
        }
        public serverJson loadSession(){
            string sessionjson = skinManager.getServerJson();
            serverJson currentSession = JsonUtility.FromJson<serverJson>(sessionjson);
            return currentSession;
        }

        public static string keybinding;
        public static T loadKeyBindings<T>(string filepath){
            if(keybinding == null){
                string DIR = "./";
                switch (SystemInfo.operatingSystemFamily)
                {
                    case OperatingSystemFamily.MacOSX:
                        DIR = Path.GetFullPath(Application.dataPath + "/Resources/Data/Managed/Mods/ServerKnights");
                        break;
                    default:
                        DIR = Path.GetFullPath(Application.dataPath + "/Managed/Mods/ServerKnights");
                        break;
                }
                keybinding = File.ReadAllText(DIR + '/' + filepath);
            }
            T Keys = JsonUtility.FromJson<T>(keybinding);
            return Keys;
        }

        public void clientSetSession(serverJson session){
            skinManager.clientSetSession(session);
        }

        public void OnServerKnightUpdate(ClientPlayerData player,int id,ushort skin,ushort emote){
            skinManager.updateRemotePlayerSkin(player,skin);
            emoteManager.showRemotePlayerEmote(player,emote);          
        }

        public void sendServerKnightUpdate( int type, ushort payload){
            _networkManager.GetNetClient().UpdateManager.ServerKnightUpdate(type, payload);
        }
        public void OnSceneChange(){
            skinManager.updateLocalPlayerSkin(skinManager.LocalPlayerSkin);
        }
        
        public int lastpdc = 0;
        public float opacityDiff = -0.05f;
        public float lastopacity = 1.0f;
        public DateTime last = DateTime.Now;

        public void setPlayerOpacity(float opacityValue){
            GameObject player = HeroController.instance.gameObject;
            var anim = player.GetComponent<tk2dSpriteAnimator>();
            Material mat = anim.GetClipByName("Idle").frames[0].spriteCollection.spriteDefinitions[0].material;
            var color = mat.color;
            color.a = opacityValue;
            mat.color = color;
        }

        public void blinkPlayerIfLoading(){
            if((DateTime.Now - last).TotalMilliseconds > 60){
                lastopacity += opacityDiff;
                setPlayerOpacity(lastopacity);
                if(lastopacity < 0.2f || lastopacity >= 1f){
                    opacityDiff = -opacityDiff;
                }
                last = DateTime.Now;
            }
        }

        private void onHeroUpdate(){
            if(connected){
                if(!skinsInit){
                    skinsInit = skinManager.checkSessionAndLoadSkins();
                } 
                if(skinManager.skinLoader.loadInMemory && !skinManager.skinLoader.loadedInMemory){
                    skinManager.skinLoader.loadSkinsIntoMemory();
                    setPlayerOpacity(1.0f);
                    skinManager.skinLoader.loadedInMemory = true;
                }
                if(skinManager.skinLoader.loadedInMemory){
                    skinManager.listenForInput();
                } else {
                    blinkPlayerIfLoading();
                }
                emoteManager.listenForInput();
                if(lastpdc != skinManager.skinLoader.pendingDownloads){
                    skinUtils.UILog(this,$"downloads {skinManager.skinLoader.pendingDownloads}");
                    lastpdc = skinManager.skinLoader.pendingDownloads;
                }
            } else {
                skinsInit = false;
                saveDefaultSkins();
            }
        }
    }
}