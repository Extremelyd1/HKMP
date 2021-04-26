namespace HKMP.Game.Settings {
    /**
     * Settings file that stores last used addresses and ports
     */
    public class ModSettings : Modding.ModSettings {

        public int HideUiKey {
            get;
            set;
        } = 307;
        
        public string JoinAddress {
            get;
            set;
        }

        public int JoinPort {
            get;
            set;
        } = -1;

        public string Username {
            get;
            set;
        }

        public int HostPort {
            get;
            set;
        } = -1;

        public bool DisplayPing {
            get;
            set;
        }

        public Game.Settings.GameSettings GameSettings {
            get;
            set;
        }
        
    }
}