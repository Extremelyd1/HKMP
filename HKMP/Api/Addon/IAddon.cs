namespace Hkmp.Api.Addon {
    public interface IAddon {
        string Identifier { get; }
        
        string Version { get; }
        
        bool NeedsNetwork { get; }
        
        void Initialize();
    }
}