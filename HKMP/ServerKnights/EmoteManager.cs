
using Modding;

using UnityEngine;
using InControl;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

using HKMP.Game.Client;

namespace HKMP.ServerKnights {

    public enum Emote {
        None = 0,
        emote_faceHappy = 1,//j
        emote_exclamation, //g
        emote_laugh, //l
        emote_heart, //h
        emote_sleeps //k
    }
    public class EmoteScript : MonoBehaviour {
        private float scale = 1.5f;
        private float opacity = 0.5f;
        private bool hiding = false;
        private SpriteRenderer renderer;
        private GameObject playerObject;
        private DateTime emitTime;

        public void setPlayer(GameObject po){
            playerObject = po;
        }
        void Reset(){
            scale = 1.5f;
            opacity = 0.5f;
            hiding = false;
        }

        void OnDisable() {
            Reset();
        }

        void OnEnable() {
            if(playerObject == null) {return;}
            transform.position = playerObject.transform.position + new Vector3(0,1.5f,0);
            transform.localScale = new Vector2(1f,1f);
            renderer.sortingOrder = 5;
            emitTime = DateTime.Now;
        }

        void Start(){
            if(playerObject == null) {return;}
            transform.position = playerObject.transform.position + new Vector3(0,1.5f,0);
            transform.localScale = new Vector2(1f,1f);
            renderer = gameObject.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 5;
            emitTime = DateTime.Now;
        }
        void Update() {
            if((DateTime.Now - emitTime).TotalMilliseconds > 1000){
                transform.Translate(0,  Time.deltaTime * 3f, 0);
                if(hiding){
                    renderer.sortingOrder = 3;
                    opacity -= 3f * Time.deltaTime;
                    scale += 3f * Time.deltaTime;
                }    
            } else {
                if((DateTime.Now - emitTime).TotalMilliseconds > 300){
                    renderer.sortingOrder = 4;
                }
                transform.position = playerObject.transform.position + new Vector3(0,1.5f,0);
            }

            transform.localScale = new Vector3(scale,scale,0);
            Color color = renderer.material.color;
            color.a = opacity; 
            renderer.material.color = color; 

            if(scale < 2.0f){
                scale += 3f * Time.deltaTime;
            } 
            
            
            if(!hiding){
                opacity += 3f * Time.deltaTime;
                if(scale > 2.0f){
                    scale = 2.0f;
                }
            }

            if(opacity >= 1f) {
                hiding = true;
                opacity = 1f;
            }
            if(opacity <= 0f){
                gameObject.SetActive(false);
            }
        }
    }
    public class EmoteManager{
        public InputDevice device;
        public int Index = -1;
        public float segment = (360 / 5f);

        public Dictionary<string,Sprite> emoteTex;
        public GameObject chooser;
        public SpriteRenderer chooseRenderer;

        private DateTime lastEmoteTime;
        public string[] emoteNames = new string[10];

        private bool enabled = true;

        private GameObject[] pool = new GameObject[25];
        private int poolIndex = 0;
        private int poolSize = 25;
        public ServerKnightsManager _serverKnightsManager;
        public EmoteManager(ServerKnightsManager serverKnightsManager){
            _serverKnightsManager = serverKnightsManager;
            InputManager.OnDeviceAttached += inputDevice => Logger.Info(this, "Attached: " + inputDevice.Name );
            emoteTex = new Dictionary<string, Sprite>();
            emoteNames[0] = "empty";
            loadEmotes();
            ensureChooser();
            populatePool();
        }

        public void ensureChooser(){
            if(chooser != null) {return;}
            chooser = new GameObject("Choose Emote");
            chooseRenderer = chooser.AddComponent<SpriteRenderer>();
            Color color = chooseRenderer.material.color;
            color.a = 0.5f;
            chooseRenderer.material.color = color;
            chooseRenderer.sortingOrder = 0;
        }
        public void populatePool(){
            for(var i =0; i < poolSize; i++){
                var go= new GameObject();
                UnityEngine.Object.DontDestroyOnLoad(go);
                var rend = go.AddComponent<SpriteRenderer>();
                go.AddComponent<EmoteScript>();
                go.SetActive(false);
                rend.sortingOrder = 5;
                pool[i] = go;
            }
        }

        public void destroyPool(){
            for(var i =0; i < poolSize; i++){
                UnityEngine.Object.Destroy(pool[i]);
            }
        }
        public void choosingEmote(int Index){
            ensureChooser();
            chooser.transform.position = HeroController.instance.gameObject.transform.position + new Vector3(0,1.5f,0);
            if(Index == 0) {
                chooser.SetActive(false);
            } else {
                chooseRenderer.sprite = emoteTex[emoteNames[Index]];
                chooser.SetActive(true);
            }

        }
        public GameObject getFromPool(){
            poolIndex+=1;
            if(poolIndex >= poolSize) {
                poolIndex = 0;
            }
            return pool[poolIndex];
        }
        public void showEmote(int Index,GameObject playerObject){
            if(!enabled) { return; }
            if(Index == 0) {return;}
            chooser.SetActive(false);
            GameObject go = getFromPool();
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            EmoteScript es = go.GetComponent<EmoteScript>();
            es.setPlayer(playerObject);
            renderer.sprite = emoteTex[emoteNames[Index]];
            go.SetActive(true);
            //fire a network call here
            //_serverKnightsManager.sendServerKnightUpdate(1,Index);
        }

        public void showRemotePlayerEmote(ClientPlayerData playerData, int Index){
            showEmote(Index,playerData.PlayerObject);
        }

        public void loadEmotes(){
            string emote_exclamation = "emote_exclamation.png";
            string emote_faceHappy = "emote_faceHappy.png";
            string emote_laugh = "emote_laugh.png";
            string emote_heart = "emote_heart.png";
            string emote_sleeps = "emote_sleeps.png";
            Assembly asm = Assembly.GetExecutingAssembly();
            var i = 1;
            foreach (string res in asm.GetManifestResourceNames())
            {
                using (Stream s = asm.GetManifestResourceStream(res))
                {
                    if (s == null) continue;
                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();
                    var arr = Path.GetFileName(res).Split('.');
                    string bundleName = arr[arr.Length - 2];
                    Logger.Info(this,"Found bundle " + bundleName);
                    if(bundleName.StartsWith("emote_")){
                        Texture2D tex = new Texture2D(1, 1);
                        tex.LoadImage(buffer);
                        Sprite spr = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
                        emoteTex.Add(bundleName,spr);
                        emoteNames[i] = bundleName;
                        i+=1;
                    }
                }
            }
        }

        public void listenForInput(){
            if(!enabled) { return; }
            if(lastEmoteTime != null){
               var ms = (DateTime.Now - lastEmoteTime).TotalMilliseconds;
                if(ms < 500) {
                    //Dont spam emotes
                    return;
                }
            }
            bool choosing = false;
            InputManager.ActiveDevice.RightStick.LowerDeadZone = 0.2f;
            if(InputManager.ActiveDevice.RightStick.IsPressed){
                Index = Convert.ToInt32(Math.Floor((InputManager.ActiveDevice.RightStick.Angle - segment/2) / segment)) + 1;
                choosing = true;
            } 

            if(Input.GetKeyDown(KeyCode.G)){
                choosing = true;
                Index = 2;
            } else if(Input.GetKeyDown(KeyCode.H)){
                choosing = true;
                Index = 4;
            } else if(Input.GetKeyDown(KeyCode.J)){
                choosing = true;
                Index = 1;
            } else if(Input.GetKeyDown(KeyCode.K)){
                choosing = true;
                Index = 5;
            } else if(Input.GetKeyDown(KeyCode.L)){
                choosing = true;
                Index = 3;
            } 

            if(choosing){
                choosingEmote(Index);
            } else {
                if(Index != -1){
                    showEmote(Index,HeroController.instance.gameObject);
                    lastEmoteTime = DateTime.Now;
                    Index = -1;
                }
            }
        }

        public void disable(){
            enabled = false;
        }

        public void enable(){
            enabled = true;
        }
    }
}