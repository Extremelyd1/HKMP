using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
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

        public static string GetHash(HashAlgorithm hashAlgorithm, byte[] input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public static void ProcessNewSkins(string skinsPath){
            string[] allSkins = Directory.GetDirectories(skinsPath);
            for(int i=0; i < allSkins.Length;i++){
                if(!allSkins[i].StartsWith(skinsPath+"/hashed") && !allSkins[i].EndsWith(".skip") && File.Exists(allSkins[i]+"/Knight.png")){
                    // generate hash and use it
                    byte[] texBytes = File.ReadAllBytes(allSkins[i]+"/Knight.png");
                    var hash = skinUtils.GetHash(SHA256.Create(),texBytes);
                    Logger.Info(new System.Object(),"hashed new skin :" + hash);
                    if(!Directory.Exists(skinsPath+"/hashed"+hash)){
                        Directory.Move(allSkins[i], skinsPath+"/hashed"+hash);
                    } else {
                        Directory.Move(allSkins[i], allSkins[i] + ".skip");
                    }
                }
            }
        }
    }

    public enum SkinLoadingState{
        None,
        added,
        downloading,
        reading,

        notfound,
        inmemory
    }
    public class SkinLoader{
        public Dictionary<string,SkinLoadingState> loadStarted = new Dictionary<string,SkinLoadingState>();
        public Dictionary<string,string> skinToLoad = new Dictionary<string,string>();
        public Dictionary<string,clientSkin> KnightMap = new Dictionary<string,clientSkin>();

        public bool loadedInMemory = false;
        public bool loadInMemory = false;

        private string DATA_DIR;
        private string SKINS_FOLDER = "ServerKnights";
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
            skinCachePath = DATA_DIR + "/skins";
            skinUtils.ProcessNewSkins(skinCachePath);
            ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
        }

        //load a skin json
        public void addSkin(string skinId){
            if(skinId == "" || skinId == null) { return; }
            loadStarted[skinId] = SkinLoadingState.added;
            skinToLoad[skinId] = skinId;
        }

        public void loadSkins( bool preloadingMode = false){
            foreach( KeyValuePair<string, string> kvp in skinToLoad )
            {
                string skinid = kvp.Value;
                string[] cachedSkins = Directory.GetDirectories(skinCachePath);

                if(!cachedSkins.Contains(skinCachePath+'/'+skinid)){
                    //download this skin
                    skinUtils.UILog(this,$"Skin not found : {skinid}");
                    skinUtils.UILog(this,$"Please Install skin to use");
                    loadStarted[skinid] = SkinLoadingState.notfound;
                } else {
                    //load skins from files
                    loadSkinFromDisk(skinid);
                }
            }
            
        }
        
        // load sprites into memory 
        public void checkIfAllSkinsInMemory(){
            bool loaded = true;
            foreach( KeyValuePair<string, SkinLoadingState> kvp in loadStarted )
            {
                if(kvp.Value != SkinLoadingState.inmemory && kvp.Value != SkinLoadingState.notfound){
                    loaded = false;
                }
            }
            loadedInMemory = loaded;
        }
        public void loadSkinFromDisk(string skinId){
            if(KnightMap.ContainsKey(skinId)){
                return;
            }
            loadStarted[skinId] = SkinLoadingState.reading;

            var skinPath = ($"{skinCachePath}/{skinId}/skin.json").Replace("\\", "/");
            if(!File.Exists(skinPath)){
                return;
            }

            skinUtils.UILog(this,"Loading Skin from disk");

            string skinjson = File.ReadAllText(skinPath);
            skin currentSkin = JsonUtility.FromJson<skin>(skinjson);

            skinUtils.UILog(this,currentSkin.Name);
                    
            var KnightPath = ($"{skinCachePath}/{skinId}/Knight.png").Replace("\\", "/");
            var SprintPath = ($"{skinCachePath}/{skinId}/Sprint.png").Replace("\\", "/");

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

            KnightMap.Add(skinId,skinUtils.patchSkinWithDefault(currentSkin.loadedSkin));
            loadStarted[skinId] = SkinLoadingState.inmemory;
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
   
    }
}