namespace Hkmp.Api {
    public interface INetworkManager {

        /**
         * Get the currently managed networking client
         */
        INetClient GetNetClient();

        /**
         * Get the currently managed networking server
         */
        INetServer GetNetServer();

    }
}