using JetBrains.Annotations;

namespace Hkmp.Api.Command.Client {
    /// <summary>
    /// Interface for managing commands for client-side.
    /// </summary>
    [PublicAPI]
    public interface IClientCommandManager : ICommandManager<IClientCommand> {
    }
}
