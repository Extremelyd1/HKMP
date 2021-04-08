using UnityEngine;

namespace HKMP.Game.Client {
    public class ClientPlayerData {
        
        public ushort Id { get; set; }
        public string Username { get; set; }
        public GameObject PlayerContainer { get; set; }
        public GameObject PlayerObject { get; set; }

        public Team Team { get; set; }

        public int Skin { get; set; }

        public ClientPlayerData(ushort id, string name,GameObject playerContainer, GameObject playerObject, Team team,int skin = 0) {
            Logger.Info(this,$"{id} id skin {skin}");
            Id = id;
            Username = name;
            PlayerContainer = playerContainer;
            PlayerObject = playerObject;
            Team = team;
            Skin = skin;
        }
    }
}