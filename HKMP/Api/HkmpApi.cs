namespace Hkmp.Api {
    public class HkmpApi : IHkmpApi {

        private readonly Game.GameManager _gameManager;
        
        public HkmpApi(Game.GameManager gameManager) {
            _gameManager = gameManager;
        }
        
        public IGameManager GetGameManager() {
            return _gameManager;
        }
    }
}