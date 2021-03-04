namespace HKMP.Game {
    /**
     * Settings file that stores last used addresses and ports
     */
    public class ModSettings : Modding.ModSettings {
        public string JoinAddress {
            get;
            set;
        }

        public int JoinPort {
            get;
            set;
        }

        public string Username {
            get;
            set;
        }

        public int HostPort {
            get;
            set;
        }
        
    }
}