
using Modding;

using HKMP.Game.Client;
using HKMP.Networking.Client;

namespace HKMP.ServerKnights {
    public class ServerKnightsManager{

        public SkinManager skinManager;
        public EmoteManager emoteManager;
        public NetClient _netClient;

        private string host;
        private int port;
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
        public void updateConnected(bool clientConnected,string _host, int _port){
            connected = clientConnected;
            host = _host;
            port = _port;
        }
    

        public void OnServerKnightUpdate(ClientPlayerData player,int id,ushort skin,int emote){
            if(skin < 255){
                skinManager.updateRemotePlayerSkin(player,skin);
            }
            if(emote < 255){
                emoteManager.showRemotePlayerEmote(player,emote);
            }
        }

        public void sendServerKnightUpdate( int type, ushort payload){
            if(_netClient == null) {
                Logger.Info(this,"_netClient is null");
                return;
            } 
            Logger.Info(this,$"ushort payload {payload}");

            _netClient.UpdateManager.ServerKnightUpdate(type , payload);
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
                    skinManager.getServerJsonOnClient(host,port);
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