using UnityEngine;

namespace HKMP.Game.Client {
    public class ClientPlayerData {
        
        public GameObject PlayerContainer { get; set; }
        public GameObject PlayerObject { get; set; }

        public Team Team { get; set; }

        public int Skin { get; set; }

        public ClientPlayerData(GameObject playerContainer, GameObject playerObject, Team team,int skin = 0) {
            PlayerContainer = playerContainer;
            PlayerObject = playerObject;
            Team = team;
            Skin = skin;
        }
    }
}