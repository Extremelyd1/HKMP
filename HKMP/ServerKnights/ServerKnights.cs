
using Modding;

using HKMP.Game.Client;
using HKMP.Networking;
using HKMP.Networking.Client;
using UnityEngine;

namespace HKMP.ServerKnights {
    public class ServerSession{}

    public class ServerKnightsManager{

        public SkinManager skinManager;
        public EmoteManager emoteManager;
        public NetworkManager _networkManager;

        private bool savedDefaultSkins = false;
        private bool skinsInit = false;
        private bool loadedInMemory = false;
        private bool connected = false;
        public ServerKnightsManager(){
            skinManager = new SkinManager(this);
            emoteManager = new EmoteManager(this);
            ModHooks.Instance.HeroUpdateHook += onHeroUpdate;
        }
        public void saveDefaultSkins()
        {
            if(savedDefaultSkins == false) {
                skinManager.saveDefaultSkin();
                savedDefaultSkins = true;
            }
            return;
        }

        public void disconnected(){
            skinManager.LocalPlayerSkin = 0;
            skinManager.updateLocalPlayerSkin(skinManager.LocalPlayerSkin);
        }
        public void updateConnected(bool clientConnected){
            connected = clientConnected;
        }
        public serverJson loadSession(){
            string sessionjson = skinManager.getServerJson();
            serverJson currentSession = JsonUtility.FromJson<serverJson>(sessionjson);
            return currentSession;
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
        
        private void handleInputs(){
            skinManager.listenForInput();
            emoteManager.listenForInput();
        }
        private void onHeroUpdate(){
            if(connected){
                if(!skinsInit){
                    // Download skin hashes from the server
                    skinsInit = true;
                } else {
                    if(skinManager.pendingDownloads < 1 && loadedInMemory == false){
                        skinManager.loadSkinsIntoMemory();
                        loadedInMemory = true;
                    }
                }
                if(loadedInMemory){
                    handleInputs();
                }
            } else {
                skinsInit = false;
                saveDefaultSkins();
            }
        }
    }
}