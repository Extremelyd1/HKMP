namespace Hkmp.Game.Client.Entity; 

internal class HostClientPair<T> {
    public T Client { get; set; }
    public T Host { get; set; }
}