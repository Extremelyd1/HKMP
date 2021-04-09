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
    
    public class SkinManager{
        public Dictionary<string, string> availableSkins = new Dictionary<string, string>();
        private string DATA_DIR;
        private string SKINS_FOLDER = "ServerKnights";
        private string skinSourcesPath;
        private string serverJsonPath;
        private string sessionJsonPath;

        private string skinCachePath;
        public int pendingDownloads = 0;
        
        private bool showDownloadUpdates = false;

        private string serverJson;

        public clientSkin defaultSkin;
        public Dictionary<string,clientSkin> KnightMap = new Dictionary<string,clientSkin>();
        public string[] skinsArray = new string[10];

        public bool defaultSkinLoaded = false;
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

        public ushort LocalPlayerSkin = 0;

        public static void updateTextureInMaterialPropertyBlock(GameObject go, Texture t){
            var materialPropertyBlock = new MaterialPropertyBlock();
            go.GetComponent<MeshRenderer>().GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetTexture("_MainTex", t);
            go.GetComponent<MeshRenderer>().SetPropertyBlock(materialPropertyBlock);
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
            if(Input.GetKeyDown(KeyCode.Alpha0)){
                updateLocalPlayerSkin(0);
            } else if(Input.GetKeyDown(KeyCode.Alpha1)){
                updateLocalPlayerSkin(1);
            } else if(Input.GetKeyDown(KeyCode.Alpha2)){
                updateLocalPlayerSkin(2);
            } else if(Input.GetKeyDown(KeyCode.Alpha3)){
                updateLocalPlayerSkin(3);
            } else if(Input.GetKeyDown(KeyCode.Alpha4)){
                updateLocalPlayerSkin(4);
            } else if(Input.GetKeyDown(KeyCode.Alpha5)){
                updateLocalPlayerSkin(5);
            } else if(Input.GetKeyDown(KeyCode.Alpha6)){
                updateLocalPlayerSkin(6);
            } else if(Input.GetKeyDown(KeyCode.Alpha7)){
                updateLocalPlayerSkin(7);
            } else if(Input.GetKeyDown(KeyCode.Alpha8)){
                updateLocalPlayerSkin(8);
            } else if(Input.GetKeyDown(KeyCode.Alpha9)){
                updateLocalPlayerSkin(9);
            }
            return;
        }

        private clientSkin patchSkinWithDefault(clientSkin source){
            if(!defaultSkinLoaded) { return source;}
            var defaultSkin = KnightMap["defaultSkin"];
            if(source.Sprint == null){
                source.Sprint = defaultSkin.Sprint;
            }
            if(source.Knight == null){
                source.Knight = defaultSkin.Knight;
            }
            return source;
        }

        public void patchAllSkins(){
            for(int i=1; i < 10 ; i++){
                if(skinsArray[i] != null) {
                    patchSkinWithDefault(KnightMap[skinsArray[i]]);
                }
            }
        }

        public clientSkin getSkinForIndex(int i){
            if(skinsArray.Length > i && skinsArray[i].Length > 0){
                return KnightMap[skinsArray[i]];
            }
            return KnightMap["defaultSkin"];
        }

        public string getSkinNameForIndex(int i){
            if(skinsArray.Length > i && skinsArray[i].Length > 0){
                return KnightMap[skinsArray[i]].Name;
            }
            return KnightMap["defaultSkin"].Name;
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
            var sessionjson = JsonUtility.ToJson(currentSession);
            UI.UIManager.InfoBox.AddMessage($"Welcome to {currentSession.Name}");
            UI.UIManager.InfoBox.AddMessage($"Hosted by {currentSession.Host}");
            UI.UIManager.InfoBox.AddMessage($"Checking skins");

            // check & load all skins
            ensureSkinByUrl(currentSession.skin_1);
            skinsArray[1] = Base64Encode(currentSession.skin_1);
            ensureSkinByUrl(currentSession.skin_2);
            skinsArray[2] = Base64Encode(currentSession.skin_2);
            ensureSkinByUrl(currentSession.skin_3);
            skinsArray[3] = Base64Encode(currentSession.skin_3);
            ensureSkinByUrl(currentSession.skin_4);
            skinsArray[4] = Base64Encode(currentSession.skin_4);
            ensureSkinByUrl(currentSession.skin_5);
            skinsArray[5] = Base64Encode(currentSession.skin_5);
            ensureSkinByUrl(currentSession.skin_6);
            skinsArray[6] = Base64Encode(currentSession.skin_6);
            ensureSkinByUrl(currentSession.skin_7);
            skinsArray[7] = Base64Encode(currentSession.skin_7);
            ensureSkinByUrl(currentSession.skin_8);
            skinsArray[8] = Base64Encode(currentSession.skin_8);
            ensureSkinByUrl(currentSession.skin_9);
            skinsArray[9] = Base64Encode(currentSession.skin_9);
            Logger.Info(this,"ensured skins");
            File.WriteAllText(sessionJsonPath,sessionjson);
        }

        public static string Base64Encode(string plainText) {
            if(plainText == null) {
                return "";
            }
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData) {
            if(base64EncodedData == null) {
                return "";
            }
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public ServerKnightsManager _serverKnightsManager;
        public SkinManager(ServerKnightsManager serverKnightsManager){
            _serverKnightsManager = serverKnightsManager;
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.MacOSX:
                    DATA_DIR = Path.GetFullPath(Application.dataPath + "/Resources/Data/Managed/Mods/" + SKINS_FOLDER);
                    break;
                default:
                    DATA_DIR = Path.GetFullPath(Application.dataPath + "/Managed/Mods/" + SKINS_FOLDER);
                    break;
            }
            skinSourcesPath =  DATA_DIR + "/skinsources.json";
            serverJsonPath =  DATA_DIR + "/server.json";
            sessionJsonPath = DATA_DIR + "/session.json"; 
            skinCachePath = DATA_DIR + "/cache";
            ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
        }


        public void loadSkinsIntoMemory(){
            for(int i=1; i < 10 ; i++){
                Logger.Info(this,$"downloaded skins, loading {skinsArray[i]}");
                loadSkinFromDisk(skinsArray[i]);
            }
        }
        public void downloadCompleted(object sender, AsyncCompletedEventArgs e){
            pendingDownloads -= 1;
            if (e.Cancelled)
            {
                UI.UIManager.InfoBox.AddMessage("File download cancelled.");
                Logger.Info(this,"File download cancelled.");

            }
            if (e.Error != null)
            {
                UI.UIManager.InfoBox.AddMessage(e.Error.ToString());
                Logger.Info(this,e.Error.ToString());
            }
            if(showDownloadUpdates == true && pendingDownloads > 0){
                UI.UIManager.InfoBox.AddMessage($"Remaining {pendingDownloads} files");
            }
            if(showDownloadUpdates == true && pendingDownloads == 0){
                showDownloadUpdates=false;
                UI.UIManager.InfoBox.AddMessage($"All downloads completed");
            }
        }
        public void downloadFile(string skinId,string filename,string url){
            string skinFilePath = $"{skinCachePath}/{skinId}/{filename}";
            if(File.Exists(skinFilePath)) {
                loadSkinFromDisk(skinId);
                return;
            }
            using (WebClient client = new WebClient()) 
            {
                pendingDownloads += 1;
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(downloadCompleted);
                client.DownloadFileAsync(new Uri(url), skinFilePath);
            }
        }
        public void downloadAvailableSkins(skin currentSkin, string base64){
            // currently we only use this one file so that's all we load
            if (currentSkin.Knight != null) 
            {
                downloadFile(base64,"Knight.png",currentSkin.Knight);
            }
            if (currentSkin.Sprint != null) 
            {
                downloadFile(base64,"Sprint.png",currentSkin.Sprint);
            }
        }
        public void downloadSkin(string skinUrl, string base64){
            Logger.Info(this,$"downloading {skinUrl}");
            try	{
                WebClient client = new WebClient();
                string skinjson = client.DownloadString(skinUrl);
                Logger.Info(this,skinjson);
                skin currentSkin = JsonUtility.FromJson<skin>(skinjson);

                UI.UIManager.InfoBox.AddMessage($"Found Skin {currentSkin.Name} by {currentSkin.Author}");
                //create directory for this skin, download individual file(s) &  write the json
                Directory.CreateDirectory($"{skinCachePath}/{base64}");
                downloadAvailableSkins(currentSkin, base64);
                File.WriteAllText($"{skinCachePath}/{base64}/skin.json",skinjson);
            } catch(Exception e){
                    Logger.Error(this,"\nException Caught!");	
                    Logger.Error(this,"Message :{0} " + e.Message);
            }
        }
        

        public void ensureSkinByUrl(string skinUrl){
            if(skinUrl == null){ return;}
            ensureSkin(skinUrl, SkinManager.Base64Encode(skinUrl));
        }
        public void ensureSkinByBase64(string base64){
            if(base64 == null){ return;}
            ensureSkin(SkinManager.Base64Decode(base64), base64);
        }

        public void saveDefaultSkin(){
            GameObject hc = UnityEngine.Object.Instantiate(HeroController.instance.gameObject);
            tk2dSpriteAnimator anim = hc.GetComponent<tk2dSpriteAnimator>();
            defaultSkin = new clientSkin();
            defaultSkin.Name = "Default Knight";
            defaultSkin.Author = "Team Cherry";
            defaultSkin.Knight = anim.GetClipByName("Idle").frames[0].spriteCollection.spriteDefinitions[0].material.mainTexture as Texture2D;
            defaultSkin.Sprint = anim.GetClipByName("Sprint").frames[0].spriteCollection.spriteDefinitions[0].material.mainTexture as Texture2D;

            if(!KnightMap.ContainsKey("defaultSkin")){
                KnightMap.Add("defaultSkin",defaultSkin);
            }
            skinsArray[0] = "defaultSkin";
            defaultSkinLoaded = true;
            patchAllSkins();
        }

        public void loadSkinFromDisk(string base64){
            if(base64 == null){
                return;
            }
            if(KnightMap.ContainsKey(base64)){
                return;
            }
            var skinPath = ($"{skinCachePath}/{base64}/skin.json").Replace("\\", "/");
            if(!File.Exists(skinPath)){
                return;
            }

            Logger.Info(this,"Loading Skin from disk");

            string skinjson = File.ReadAllText(skinPath);
            skin currentSkin = JsonUtility.FromJson<skin>(skinjson);

            Logger.Info(this,currentSkin.Name);
                    
            var KnightPath = ($"{skinCachePath}/{base64}/Knight.png").Replace("\\", "/");
            var SprintPath = ($"{skinCachePath}/{base64}/Sprint.png").Replace("\\", "/");

            // load only if all spritesheets are available
            if((currentSkin.Knight !=null && !File.Exists(KnightPath)) &&
               (currentSkin.Sprint !=null && !File.Exists(SprintPath))
            ) {return;}

            currentSkin.loadedSkin = new clientSkin();
            currentSkin.loadedSkin.Name = currentSkin.Name;
            currentSkin.loadedSkin.Author = currentSkin.Author;

            if(currentSkin.Knight !=null && File.Exists(KnightPath)){
                byte[] texBytes = File.ReadAllBytes(KnightPath);
                currentSkin.loadedSkin.Knight = new Texture2D(1,1);
                try{
                    currentSkin.loadedSkin.Knight.LoadImage(texBytes, true);
                } catch(Exception e){
                    Logger.Info(this,$"could not load texture : {e}");
                    currentSkin.loadedSkin.Knight = null;
                }
            }
            if(currentSkin.Sprint !=null && File.Exists(SprintPath)){
                byte[] texBytes = File.ReadAllBytes(SprintPath);
                currentSkin.loadedSkin.Sprint = new Texture2D(1,1);
                try{
                    currentSkin.loadedSkin.Sprint.LoadImage(texBytes, true);
                } catch(Exception e){
                    Logger.Info(this,$"could not load texture : {e}");
                    currentSkin.loadedSkin.Sprint = null;
                }
            }

            KnightMap.Add(base64,patchSkinWithDefault(currentSkin.loadedSkin));
            Logger.Info(this,$"Loaded skin '{currentSkin.Name}' from disk ");
        }
        public void ensureSkin(string skinUrl,string base64){
            string[] cachedSkins = Directory.GetDirectories(skinCachePath);
            if(!cachedSkins.Contains(skinCachePath+'/'+base64)){
                //download this skin
                downloadSkin(skinUrl,base64);
            } else {
                //load skins from files
                loadSkinFromDisk(base64);
            }
        }
        public void preloadSkinSources(){
            UI.UIManager.InfoBox.AddMessage($"Checking for new ServerKnight skins");

            if(!File.Exists(skinSourcesPath)){
                Logger.Error(this,"skinsources.json not found");
                return;
            }
            string skinsourcesjson = File.ReadAllText(skinSourcesPath);  

            skinSources jsonObj = JsonUtility.FromJson<skinSources>(skinsourcesjson);
            for (int i = 0; i < jsonObj.skins.Length; i++) 
            {
                var skinUrl= jsonObj.skins[i];
                var base64 = SkinManager.Base64Encode(skinUrl);
                availableSkins[base64] = skinUrl;
                string[] cachedSkins = Directory.GetDirectories(skinCachePath);
                //ensureSkin(skinUrl,base64);
                if(!cachedSkins.Contains(skinCachePath+'/'+base64)){
                    //download this skin but do not load all skins in memory
                    downloadSkin(skinUrl,base64);
                }
                Logger.Info(this,$"{base64}");
            }

            if(pendingDownloads > 0){
                UI.UIManager.InfoBox.AddMessage($"Downloading {pendingDownloads} files");
                showDownloadUpdates=true;
            }
        }
    }
}