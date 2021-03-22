using UnityEngine;

namespace HKMP.Game.Client {
    public class ClientPlayerData {
        
        public GameObject PlayerContainer { get; set; }
        public GameObject PlayerObject { get; set; }

        public Team Team { get; set; }

        public ClientPlayerData(GameObject playerContainer, GameObject playerObject, Team team) {
            PlayerContainer = playerContainer;
            PlayerObject = playerObject;
            Team = team;
        }
    }
}