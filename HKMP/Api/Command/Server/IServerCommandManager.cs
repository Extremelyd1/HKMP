using JetBrains.Annotations;

namespace Hkmp.Api.Command.Server {
    /// <summary>
    /// Interface for managing commands for server-side.
    /// </summary>
    [PublicAPI]
    public interface IServerCommandManager : ICommandManager<IServerCommand> {
    }
}
