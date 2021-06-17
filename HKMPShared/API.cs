namespace HKMP {
    //class that exposes some of HKMP as an API
    public static class API {
        public static bool isClientConnected = false;
        public static bool isServerConnected = false;

    }

    public static class APIManager{
        public static void ClientConnected(){
            API.isClientConnected = true;
        }
        public static void ClientDisconnected(){
            API.isClientConnected = false;
        }
        public static void ServerConnected(){
            API.isServerConnected = true;
        }
        public static void ServerDisconnected(){
            API.isServerConnected = false;
        }
    }
}