
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
            skinManager.LocalPlayerSkin = 0;
            skinManager.updateLocalPlayerSkin(skinManager.LocalPlayerSkin);
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

        private void onHeroUpdate(){
            if(connected){
                if(!skinsInit){
                    skinsInit = skinManager.checkSessionAndLoadSkins();
                } else {
                    skinManager.listenForInput();
                }
                emoteManager.listenForInput();
            } else {
                skinsInit = false;
                saveDefaultSkins();
            }
        }
    }
}