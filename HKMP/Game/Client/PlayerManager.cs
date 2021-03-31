using System.Reflection;
using System.IO;
using GlobalEnums;
using System.Collections.Generic;
using HKMP.Fsm;
using HKMP.Game.Settings;
using HKMP.Networking;
using HKMP.UI.Resources;
using HKMP.Util;
using ModCommon;
using TMPro;
using UnityEngine;


namespace HKMP.Game.Client {
    /**
     * Class that manages player objects, spawning and destroying thereof.
     */
    public class PlayerManager {
        private readonly Game.Settings.GameSettings _gameSettings;
        
        private readonly Dictionary<ushort, ClientPlayerData> _playerData;

        // The team that our local player is on
        public Team LocalPlayerTeam { get; set; } = Team.None;

        public int LocalPlayerSkin { get; set; } = 0;

        private readonly GameObject _playerPrefab;
        

        private bool init = false;
        public const string SKINS_FOLDER = "ServerKnights";
        public string SKIN_FOLDER = "default";

        public string DATA_DIR;
        public Texture2D defaultSkin;
        public Texture2D[] customSkins = new Texture2D[10];
        public Material _knightMat;

        public PlayerManager(NetworkManager networkManager, Game.Settings.GameSettings gameSettings, ModSettings settings) {
            _gameSettings = gameSettings;
            
            _playerData = new Dictionary<ushort, ClientPlayerData>();
            
            // Create the player prefab, used to instantiate player objects
            _playerPrefab = new GameObject(
                "PlayerPrefab",
                typeof(BoxCollider2D),
                typeof(DamageHero),
                typeof(EnemyHitEffectsUninfected),
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(NonBouncer),
                typeof(SpriteFlash),
                typeof(tk2dSprite),
                typeof(tk2dSpriteAnimator),
                typeof(CoroutineCancelComponent)
            ) {
                layer = 9
            };

            // Add some extra gameObject related to animation effects
            new GameObject("Attacks") {layer = 9}.transform.SetParent(_playerPrefab.transform);
            new GameObject("Effects") {layer = 9}.transform.SetParent(_playerPrefab.transform);
            new GameObject("Spells")  {layer = 9}.transform.SetParent(_playerPrefab.transform);
            
            _playerPrefab.SetActive(false);
            Object.DontDestroyOnLoad(_playerPrefab);
        }

        public void UpdatePosition(ushort id, Vector3 position) {
            if (!_playerData.ContainsKey(id)) {
                // Logger.Warn(this, $"Tried to update position for ID {id} while container or object did not exists");
                return;
            }

            var playerContainer = _playerData[id].PlayerContainer;
            if (playerContainer != null) {
                playerContainer.GetComponent<PositionInterpolation>().SetNewPosition(position);
            }

        }

        public void UpdateScale(ushort id, Vector3 scale) {
            if (!_playerData.ContainsKey(id)) {
                // Logger.Warn(this, $"Tried to update scale for ID {id} while container or object did not exists");
                return;
            }
        
            var playerObject = _playerData[id].PlayerObject;
            if (playerObject != null) {
                playerObject.transform.localScale = scale;
            }
        }

        public GameObject GetPlayerObject(ushort id) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Error(this, $"Tried to get the player object that does not exists for ID {id}");
                return null;
            }

            return _playerData[id].PlayerObject;
        }

        public GameObject GetPlayerContainer(ushort id) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Error(this, $"Tried to get the player container that does not exists for ID {id}");
                return null;
            }

            return _playerData[id].PlayerContainer;
        }

        // TODO: investigate whether it is better to disable/setActive(false) player objects instead of destroying
        // and only destroy when player left server
        public void DestroyPlayer(ushort id) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Tried to destroy player that does not exists for ID {id}");
                return;
            }
            
            // Destroy gameObject and remove from mapping
            Object.Destroy(_playerData[id].PlayerContainer);
            _playerData.Remove(id);
        }

        public void DestroyAllPlayers() {
            foreach (var playerData in _playerData.Values) {
                // Destroy gameObject
                Object.Destroy(playerData.PlayerContainer);
            }
            
            // Clear mapping
            _playerData.Clear();
        }
        
        public void SpawnPlayer(ushort id, string name, Vector3 position, Vector3 scale, Team team,int skin) {
            if (_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"We already have created a player object for ID {id}");
                return;
            }

            // Create a player container
            var playerContainer = new GameObject($"Player Container {id}");
            playerContainer.transform.position = position;
            
            playerContainer.AddComponent<PositionInterpolation>();
            
            // Instantiate the player object from the prefab in the player container
            var playerObject = Object.Instantiate(
                _playerPrefab,
                playerContainer.transform
            );
            Object.DontDestroyOnLoad(playerObject);
            
            playerObject.transform.localScale = scale;

            // Set object and children to active
            playerObject.SetActive(true);
            playerObject.SetActiveChildren(true);
            
            // Now we need to copy over a lot of variables from the local player object
            var localPlayerObject = GameObject.Instantiate(HeroController.instance.gameObject) as GameObject;
            
            // Obtain colliders from both objects
            var collider = playerObject.GetComponent<BoxCollider2D>();
            var localCollider = localPlayerObject.GetComponent<BoxCollider2D>();
            
            // Copy collider offset and size
            collider.isTrigger = true;
            collider.offset = localCollider.offset;
            collider.size = localCollider.size;
            collider.enabled = true;
            
            // Copy collider bounds
            var bounds = collider.bounds;
            var localBounds = localCollider.bounds;
            bounds.min = localBounds.min;
            bounds.max = localBounds.max;

            // Disable DamageHero component unless pvp is enabled
            if (_gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled) {
                playerObject.layer = 11;
                playerObject.GetComponent<DamageHero>().enabled = true;
            } else {
                playerObject.layer = 9;
                playerObject.GetComponent<DamageHero>().enabled = false;
            }

            // Copy over mesh filter variables
            var meshFilter = playerObject.GetComponent<MeshFilter>();
            var mesh = meshFilter.mesh;
            var localMesh = localPlayerObject.GetComponent<MeshFilter>().sharedMesh;
            
            mesh.vertices = localMesh.vertices;
            mesh.normals = localMesh.normals;
            mesh.uv = localMesh.uv;
            mesh.triangles = localMesh.triangles;
            mesh.tangents = localMesh.tangents;
            
            // Copy mesh renderer material
            var meshRenderer = playerObject.GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(localPlayerObject.GetComponent<MeshRenderer>().material);
            
            // Disable non bouncer component
            var nonBouncer = playerObject.GetComponent<NonBouncer>();
            nonBouncer.active = false;
            
            // Copy over animation library
            var anim = playerObject.GetComponent<tk2dSpriteAnimator>();
            anim.Library = Object.Instantiate(localPlayerObject.GetComponent<tk2dSpriteAnimator>().Library);

            AddNameToPlayer(playerContainer, name, team);

            // Store the player data in the mapping
            _playerData[id] = new ClientPlayerData(
                playerContainer,
                playerObject,
                team,
                skin
            );
            
            // Set whether this player should have body damage
            // Only if:
            // PvP is enabled and body damage is enabled AND
            // (the teams are not equal or if either doesn't have a team)
            ToggleBodyDamage(
                _playerData[id],
                _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled &&
                (team != LocalPlayerTeam
                 || team.Equals(Team.None)
                 || LocalPlayerTeam.Equals(Team.None))
            );
        }

        public void AddNameToPlayer(GameObject playerContainer, string name, Team team = Team.None) {
            // Create a name object to set the username to, slightly above the player object
            var nameObject = Object.Instantiate(
                new GameObject("Username"),
                playerContainer.transform.position + new Vector3(0, 1.25f, 0),
                Quaternion.identity
            );
            nameObject.name = "Username";
            nameObject.transform.SetParent(playerContainer.transform);
            nameObject.transform.localScale = new Vector3(0.25f, 0.25f, nameObject.transform.localScale.z);
            nameObject.AddComponent<KeepWorldScalePositive>();

            // Add a TextMeshPro component to it, so we can render text
            var textMeshObject = nameObject.AddComponent<TextMeshPro>();
            textMeshObject.text = name.ToUpper();
            textMeshObject.alignment = TextAlignmentOptions.Center;
            textMeshObject.font = FontManager.InGameNameFont;
            textMeshObject.fontSize = 22;
            textMeshObject.outlineWidth = 0.2f;
            textMeshObject.outlineColor = Color.black;

            ChangeNameColor(textMeshObject, team);

            nameObject.SetActive(_gameSettings.DisplayNames);
        }

        /**
         * This will reset the local player's team to be None
         * and will reset all existing player names and hitboxes to be None again too
         */
        public void ResetAllTeams() {
            OnLocalPlayerTeamUpdate(Team.None);

            foreach (var id in _playerData.Keys) {
                OnPlayerTeamUpdate(id, Team.None);
            }
        }

        public void OnPlayerTeamUpdate(ushort id, Team team) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                return;
            }

            // Update the team in the player data
            playerData.Team = team;

            // Get the name object and update the color based on the new team
            var nameObject = playerData.PlayerContainer.FindGameObjectInChildren("Username");
            var textMeshObject = nameObject.GetComponent<TextMeshPro>();
            
            ChangeNameColor(textMeshObject, team);
            
            // Toggle body damage on if:
            // PvP is enabled and body damage is enabled AND
            // (the teams are not equal or if either doesn't have a team)
            ToggleBodyDamage(
                playerData,
                _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled &&
                (team != LocalPlayerTeam 
                || team.Equals(Team.None) 
                || LocalPlayerTeam.Equals(Team.None))
            );
        }

        public void OnPlayerSkinUpdate(ushort id, int skin) {
            
            if (!_playerData.ContainsKey(id)) {
                // Logger.Warn(this, $"Tried to update scale for ID {id} while container or object did not exists");
                return;
            }
            OnStart();   
            var playerObject = _playerData[id].PlayerObject;
            
            // Update the skin in the player data
            _playerData[id].Skin = skin;

            // Get the player object and update the skin
            var materialPropertyBlock = new MaterialPropertyBlock();;
            playerObject.GetComponent<MeshRenderer>().GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetTexture("_MainTex", customSkins[skin]);
            playerObject.GetComponent<MeshRenderer>().SetPropertyBlock(materialPropertyBlock);
    
        }

        public void OnLocalPlayerSkinUpdate(int skin) { 
            OnStart();   
            LocalPlayerSkin = skin;
            // Update the local player skin
            var materialPropertyBlock = new MaterialPropertyBlock();;
            HeroController.instance.gameObject.GetComponent<MeshRenderer>().GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetTexture("_MainTex", customSkins[skin]);
            HeroController.instance.gameObject.GetComponent<MeshRenderer>().SetPropertyBlock(materialPropertyBlock);

        }


        public void initSkins()
        {
            Logger.Info(this,"Load skins into Memory");
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.MacOSX:
                    DATA_DIR = Path.GetFullPath(Application.dataPath + "/Resources/Data/Managed/Mods/" + SKINS_FOLDER);
                    break;
                default:
                    DATA_DIR = Path.GetFullPath(Application.dataPath + "/Managed/Mods/" + SKINS_FOLDER);
                    break;
            }
        
            for(int i=1;i<10;i++) {
                loadSkin(i);
            }
        
            Logger.Info(this,"Initialized");
        }

        public void loadSkin(int index){
            //todo write skin manager to manage skins loading from server / cache
            Logger.Info(this,"Loading Skin from disk");
            var skinPath = (DATA_DIR + "/" + index.ToString() + "/Knight.png").Replace("\\", "/");
            if(File.Exists(skinPath)){
                byte[] texBytes = File.ReadAllBytes(skinPath);
                customSkins[index] = new Texture2D(2, 2);
                customSkins[index].LoadImage(texBytes, true);
            }
        }

        public void switchSkin(int index){
            if(customSkins[index] != null){
                OnLocalPlayerSkinUpdate(index);
            }
        }

        public void saveDefaultSkin(){
            GameObject hc = Object.Instantiate(HeroController.instance.gameObject);
            tk2dSpriteAnimator anim = hc.GetComponent<tk2dSpriteAnimator>();
            _knightMat = anim.GetClipByName("Idle").frames[0].spriteCollection.spriteDefinitions[0].material;
            defaultSkin = _knightMat.mainTexture as Texture2D;
            customSkins[0] = defaultSkin;
        }

        public void OnStart()
        {

            if(init == false) {     
                saveDefaultSkin();
                initSkins();
                init = true;
            }
            return;
        }
    

        public void OnLocalPlayerTeamUpdate(Team team) {
            LocalPlayerTeam = team;
            
            var nameObject = HeroController.instance.gameObject.FindGameObjectInChildren("Username");
            
            var textMeshObject = nameObject.GetComponent<TextMeshPro>();
            ChangeNameColor(textMeshObject, team);

            foreach (var playerData in _playerData.Values) {
                // Toggle body damage on if:
                // PvP is enabled and body damage is enabled AND
                // (the teams are not equal or if either doesn't have a team)
                ToggleBodyDamage(
                    playerData,
                    _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled &&
                    (playerData.Team != LocalPlayerTeam 
                     || playerData.Team.Equals(Team.None) 
                     || LocalPlayerTeam.Equals(Team.None))
                );
            }
        }

        public Team GetPlayerTeam(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                return Team.None;
            }

            return playerData.Team;
        }

        public int GetPlayerSkin(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                return 0;
            }

            return playerData.Skin;
        }
        private void ChangeNameColor(TextMeshPro textMeshObject, Team team) {
            switch (team) {
                case Team.Moss:
                    textMeshObject.color = new Color(0f / 255f, 150f / 255f, 0f / 255f);
                    break;
                case Team.Hive:
                    textMeshObject.color = new Color(200f / 255f, 150f / 255f, 0f / 255f);
                    break;
                case Team.Grimm:
                    textMeshObject.color = new Color(250f / 255f, 50f / 255f, 50f / 255f);
                    break;
                case Team.Lifeblood:
                    textMeshObject.color = new Color(50f / 255f, 150f / 255f, 200f / 255f);
                    break;
                default:
                    textMeshObject.color = Color.white;
                    break;
            }
        }

        public void RemoveNameFromLocalPlayer() {
            RemoveNameFromPlayer(HeroController.instance.gameObject);
        }

        private void RemoveNameFromPlayer(GameObject playerContainer) {
            // Get the name object
            var nameObject = playerContainer.FindGameObjectInChildren("Username");

            // Destroy it if it exists
            if (nameObject != null) {
                Object.Destroy(nameObject);
            }
        }

        public void OnGameSettingsUpdated(bool pvpOrBodyDamageChanged, bool displayNamesChanged) {
            if (pvpOrBodyDamageChanged) {
                // Loop over all player objects
                foreach (var playerData in _playerData.Values) {
                    // Enable the DamageHero component based on whether both PvP and body damage are enabled
                    ToggleBodyDamage(playerData, _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled);
                }
            }

            if (displayNamesChanged) {
                foreach (var playerData in _playerData.Values) {
                    var nameObject = playerData.PlayerContainer.FindGameObjectInChildren("Username");
                    if (nameObject != null) {
                        nameObject.SetActive(_gameSettings.DisplayNames);
                    }
                }

                var localPlayerObject = HeroController.instance.gameObject;
                if (localPlayerObject != null) {
                    var nameObject = localPlayerObject.FindGameObjectInChildren("Username");
                    if (nameObject != null) {
                        nameObject.SetActive(_gameSettings.DisplayNames);
                    }
                }
            }
        }

        private void ToggleBodyDamage(ClientPlayerData playerData, bool enabled) {
            var playerObject = playerData.PlayerObject;

            // We need to move the player object to the correct layer so it can interact with nail swings etc.
            // Also toggle the enabled state of the DamageHero component
            if (enabled) {
                playerObject.layer = 11;
                playerObject.GetComponent<DamageHero>().enabled = true;
            } else {
                playerObject.layer = 9;
                playerObject.GetComponent<DamageHero>().enabled = false;
            }
        }

    }
}