using Hkmp.Api.Client;
using UnityEngine;

namespace Hkmp.Game.Client {
    public class ClientPlayerData : IClientPlayer {
        public ushort Id { get; }
        public string Username { get; }

        public bool IsInLocalScene { get; set; }

        public GameObject PlayerContainer { get; set; }
        public GameObject PlayerObject { get; set; }

        public Team Team { get; set; }
        public byte SkinId { get; set; }

        public ClientPlayerData(
            ushort id,
            string username
        ) {
            Id = id;
            Username = username;
        }
    }
}