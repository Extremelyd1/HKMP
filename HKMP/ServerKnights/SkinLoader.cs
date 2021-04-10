using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using UnityEngine;
using System.Linq;


namespace HKMP.ServerKnights {

    public class skinUtils {

        public static clientSkin defaultSkin = new clientSkin();
        private static bool defaultSkinLoaded = false;
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

        public static void UILog(object origin, string message){
            UI.UIManager.InfoBox.AddMessage(message);
            Logger.Info(origin,message);
        }

        public static clientSkin saveDefaultSkin(){
            GameObject hc = UnityEngine.Object.Instantiate(HeroController.instance.gameObject);
            tk2dSpriteAnimator anim = hc.GetComponent<tk2dSpriteAnimator>();
            defaultSkin = new clientSkin();
            defaultSkin.Name = "Default Knight";
            defaultSkin.Author = "Team Cherry";
            defaultSkin.Knight = anim.GetClipByName("Idle").frames[0].spriteCollection.spriteDefinitions[0].material.mainTexture as Texture2D;
            defaultSkin.Sprint = anim.GetClipByName("Sprint").frames[0].spriteCollection.spriteDefinitions[0].material.mainTexture as Texture2D;
            defaultSkinLoaded = true;
            return defaultSkin;
        }

        public static clientSkin patchSkinWithDefault(clientSkin source){
            if(!defaultSkinLoaded) { return source;}
            if(source.Sprint == null){
                source.Sprint = defaultSkin.Sprint;
            }
            if(source.Knight == null){
                source.Knight = defaultSkin.Knight;
            }
            return source;
        }

        public static void patchAllSkins(Dictionary<string,clientSkin> KnightMap){
            foreach( KeyValuePair<string,clientSkin> kvp in KnightMap ){
                patchSkinWithDefault(kvp.Value);
            }
        }
    }

    public enum SkinLoadingState{
        None,
        added,
        downloading,
        reading,
        inmemory
    }
    public class SkinLoader{
        public Dictionary<string,SkinLoadingState> loadStarted = new Dictionary<string,SkinLoadingState>();
        public Dictionary<string,string> skinToLoad = new Dictionary<string,string>();
        public Dictionary<string,clientSkin> KnightMap = new Dictionary<string,clientSkin>();

        public int pendingDownloads = 0;

        public bool preloading = false;
        public bool loadedInMemory = false;

        public bool loadInMemory = false;

        private string DATA_DIR;
        private string SKINS_FOLDER = "ServerKnights";
        private string skinSourcesPath;
        private string skinCachePath;

        public SkinLoader(){
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
            skinCachePath = DATA_DIR + "/cache";
            ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
        }

        //load a skin json
        public void addSkin(string skinUrl){
            if(skinUrl == "" || skinUrl == null) { return; }
            loadStarted[skinUrl] = SkinLoadingState.added;
            skinToLoad[skinUrl] = skinUtils.Base64Encode(skinUrl);
        }

        public void loadSkins( bool preloadingMode = false){
            foreach( KeyValuePair<string, string> kvp in skinToLoad )
            {
                string skinUrl = kvp.Key;
                string base64 = kvp.Value;
                string[] cachedSkins = Directory.GetDirectories(skinCachePath);

                if(!cachedSkins.Contains(skinCachePath+'/'+base64)){
                    //download this skin
                    if(preloadingMode){
                        preloading = preloadingMode;
                    }
                    pendingDownloads += 1;
                    loadStarted[skinUrl] = SkinLoadingState.downloading;
                    downloadSkin(skinUrl,base64);
                } else {
                    //load skins from files
                    loadSkinFromDisk(base64);
                }
            }
            
        }
        
        // download skin json
        private void downloadSkinCompleted(string base64, System.Object sender, DownloadStringCompletedEventArgs e){
            pendingDownloads -= 1;
            if (e.Cancelled || e.Error != null)
            {
                skinUtils.UILog(this,"File download Failed.");
                if(e.Error != null){
                    skinUtils.UILog(this,e.Error.ToString());
                }
                return;
            }
            string skinjson = (string)e.Result;

            skin currentSkin = JsonUtility.FromJson<skin>(skinjson);
            skinUtils.UILog(this,$"Found Skin {currentSkin.Name} by {currentSkin.Author}");

            //create directory for this skin, download individual file(s) &  write the json
            Directory.CreateDirectory($"{skinCachePath}/{base64}");
            File.WriteAllText($"{skinCachePath}/{base64}/skin.json",skinjson);

            downloadAvailableSkins(currentSkin, base64);
        }

        private void downloadSkin(string skinUrl, string base64){
            pendingDownloads += 1;
            Logger.Info(this,$"downloading {skinUrl}");
            WebClient client = new WebClient();
            client.DownloadStringCompleted +=  new DownloadStringCompletedEventHandler((System.Object sender, DownloadStringCompletedEventArgs e) => { downloadSkinCompleted(base64,sender,e);});
            client.DownloadStringAsync(new Uri(skinUrl));            
        }
        
        // download sprites

        public void downloadSkinFileCompleted(object sender, AsyncCompletedEventArgs e){
            pendingDownloads -= 1;
            if (e.Cancelled || e.Error != null)
            {
                skinUtils.UILog(this,"File download cancelled.");
                if (e.Error != null)
                {
                    skinUtils.UILog(this,e.Error.ToString());
                }
                return;
            }
            
            if(pendingDownloads > 0){
                skinUtils.UILog(this,$"Remaining {pendingDownloads} files");
            } else if(pendingDownloads <= 0){
                skinUtils.UILog(this,$"All downloads completed");
                loadInMemory = true;
                preloading = false;
            }
        }
        public void downloadSkinFile(string skinId,string filename,string url){
            string skinFilePath = $"{skinCachePath}/{skinId}/{filename}";
            WebClient client = new WebClient();
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(downloadSkinFileCompleted);
            client.DownloadFileAsync(new Uri(url), skinFilePath);
        }

        public void downloadAvailableSkins(skin currentSkin, string base64){
            // currently we only use these two files so that's all we load
            pendingDownloads -= 1;
            if (currentSkin.Knight != null) 
            {
                pendingDownloads += 1;
                downloadSkinFile(base64,"Knight.png",currentSkin.Knight);
            }
            if (currentSkin.Sprint != null) 
            {
                pendingDownloads += 1;
                downloadSkinFile(base64,"Sprint.png",currentSkin.Sprint);
            }
        }
 
        // load sprites into memory 
        public void checkIfAllSkinsInMemory(){
            bool loaded = true;
            foreach( KeyValuePair<string, SkinLoadingState> kvp in loadStarted )
            {
                if(kvp.Value != SkinLoadingState.inmemory){
                    loaded = false;
                }
            }
            loadedInMemory = loaded;
        }
        public void loadSkinFromDisk(string base64){
            if(preloading){ return;}
            if(KnightMap.ContainsKey(base64)){
                return;
            }
            loadStarted[skinUtils.Base64Decode(base64)] = SkinLoadingState.reading;

            var skinPath = ($"{skinCachePath}/{base64}/skin.json").Replace("\\", "/");
            if(!File.Exists(skinPath)){
                return;
            }

            skinUtils.UILog(this,"Loading Skin from disk");

            string skinjson = File.ReadAllText(skinPath);
            skin currentSkin = JsonUtility.FromJson<skin>(skinjson);

            skinUtils.UILog(this,currentSkin.Name);
                    
            var KnightPath = ($"{skinCachePath}/{base64}/Knight.png").Replace("\\", "/");
            var SprintPath = ($"{skinCachePath}/{base64}/Sprint.png").Replace("\\", "/");

            // load only if all spritesheets are available
            if((currentSkin.Knight !=null && !File.Exists(KnightPath)) ||
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
                    skinUtils.UILog(this,$"could not load texture : {e}");
                    currentSkin.loadedSkin.Knight = null;
                }
            }
            if(currentSkin.Sprint !=null && File.Exists(SprintPath)){
                byte[] texBytes = File.ReadAllBytes(SprintPath);
                currentSkin.loadedSkin.Sprint = new Texture2D(1,1);
                try{
                    currentSkin.loadedSkin.Sprint.LoadImage(texBytes, true);
                } catch(Exception e){
                    skinUtils.UILog(this,$"could not load texture : {e}");
                    currentSkin.loadedSkin.Sprint = null;
                }
            }

            KnightMap.Add(base64,skinUtils.patchSkinWithDefault(currentSkin.loadedSkin));
            loadStarted[skinUtils.Base64Decode(base64)] = SkinLoadingState.inmemory;
            skinUtils.UILog(this,$"Loaded skin '{currentSkin.Name}' from disk ");
            checkIfAllSkinsInMemory();
        }
 
        public void loadSkinsIntoMemory(){
            foreach( KeyValuePair<string, string> kvp in skinToLoad )
            {
                skinUtils.UILog(this,$"downloaded skins | reading {kvp.Value}");
                loadSkinFromDisk(kvp.Value);

            }
        }
   
        // preloading skins 
        public void preloadSkinSources(){
            skinUtils.UILog(this,$"Checking for new ServerKnight skins");

            if(!File.Exists(skinSourcesPath)){
                skinUtils.UILog(this,"skinsources.json not found");
                return;
            }
            string skinsourcesjson = File.ReadAllText(skinSourcesPath);  
            skinSources jsonObj = JsonUtility.FromJson<skinSources>(skinsourcesjson);

            for (int i = 0; i < jsonObj.skins.Length; i++) 
            {
                var skinUrl= jsonObj.skins[i];
                addSkin(skinUrl);
            }
            loadSkins(true);
        }
    }
}