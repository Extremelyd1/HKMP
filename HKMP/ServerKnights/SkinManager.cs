using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using UnityEngine;
using System.Linq;


namespace HKMP.ServerKnights {
    public class skinSources{
        public string[] skins;
    }

    public class skin{
        public string Name;
        public string Author;
        public string Knight;
    }

    public class clientSkin{

        public string Name;
        public string Author;
        public Texture2D Knight ;

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
        private int pendingDownloads = 0;

        private bool showDownloadUpdates = false;

        private string serverJson;

        public Texture2D defaultSkin;
        public Dictionary<string,clientSkin> KnightMap = new Dictionary<string,clientSkin>();
        public string[] skinsArray = new string[10];


        public Texture2D getSkinForIndex(int i){
            if(skinsArray.Length > i && skinsArray[i].Length > 0){
                return KnightMap[skinsArray[i]].Knight;
            }
            return KnightMap["defaultSkin"].Knight;
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

        public void getServerJsonOnClient(string host,int port){
            using (WebClient client = new WebClient()) 
            {
                string reqUrl = $"http://{host}:{port+1}/";
                Logger.Info(this,reqUrl);
                string sessionjson = client.DownloadString(reqUrl);
                Logger.Info(this,sessionjson);


                //string sessionjson = "{\"Name\" : \"HKMP ServerKnights Test Server\",\"Host\" : \"Dandy\",\"skin_1\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\",\"skin_2\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\",\"skin_3\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\",\"skin_4\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\",\"skin_5\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\",\"skin_6\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\",\"skin_7\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\",\"skin_8\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\",\"skin_9\" : \"https://drive.google.com/uc?export=download&id=11a98SjXIkImBYe-cvusL1rF8bOvF8K1j\"}";
                serverJson currentSession = JsonUtility.FromJson<serverJson>(sessionjson);
                UI.UIManager.InfoBox.AddMessage($"Welcome to {currentSession.Name}");
                UI.UIManager.InfoBox.AddMessage($"Hosted by {currentSession.Host}");
                UI.UIManager.InfoBox.AddMessage($"Checking skins");

                Logger.Info(this,currentSession.skin_1);
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
        public SkinManager(){
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
            if(pendingDownloads < 1){
                for(int i=1; i < 10 ; i++){
                    Logger.Info(this,$"downloaded skins, loading {skinsArray[i]}");
                    loadSkinFromDisk(skinsArray[i]);
                }
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
        }
        public void downloadSkin(string skinUrl, string base64){
            Logger.Info(this,$"downloading {skinUrl}");
            try	{
                WebClient client = new WebClient();
                string skinjson = client.DownloadString(skinUrl);
                //Logger.Info(this,skinjson);
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
            Material knightMaterial = anim.GetClipByName("Idle").frames[0].spriteCollection.spriteDefinitions[0].material;
            defaultSkin = knightMaterial.mainTexture as Texture2D;
            if(!KnightMap.ContainsKey("defaultSkin")){
                KnightMap.Add("defaultSkin",new clientSkin());
                KnightMap["defaultSkin"].Knight = defaultSkin;
                KnightMap["defaultSkin"].Name = "Default Knight";
                KnightMap["defaultSkin"].Author = "Team Cherry";
             }
            skinsArray[0] = "defaultSkin";
        }

        public void loadSkinFromDisk(string base64){
            if(base64 == null){
                return;
            }
            if(KnightMap.ContainsKey(base64)){
                return;
            }
            Logger.Info(this,"Loading Skin from disk");

            var skinPath = ($"{skinCachePath}/{base64}/skin.json").Replace("\\", "/");
            var skinKnightPath = ($"{skinCachePath}/{base64}/Knight.png").Replace("\\", "/");
            
            if(File.Exists(skinPath) && File.Exists(skinKnightPath)){
                 KnightMap.Add(base64,new clientSkin());

                string skinjson = File.ReadAllText(skinPath);
                skin currentSkin = JsonUtility.FromJson<skin>(skinjson);

                KnightMap[base64].Name = currentSkin.Name;
                KnightMap[base64].Author = currentSkin.Author;

                Logger.Info(this,skinKnightPath);
                byte[] texBytes = File.ReadAllBytes(skinKnightPath);
                Logger.Info(this,KnightMap[base64].Name);
                Logger.Info(this,KnightMap[base64].Author);
                Logger.Info(this,$"{texBytes.Length} {texBytes.ToString()}");
                Logger.Info(this,Texture2D.blackTexture.format.ToString());
                Logger.Info(this,"made new");
                KnightMap[base64].Knight =  new Texture2D(1,1);;
                Logger.Info(this,"assigned");
                KnightMap[base64].Knight.LoadImage(texBytes, true);
                Logger.Info(this,skinKnightPath);
            }

            Logger.Info(this,"Done Loading Skin from disk");
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