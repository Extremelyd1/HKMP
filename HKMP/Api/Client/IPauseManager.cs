using System;

namespace Hkmp.Api.Client;

/// <summary>
/// Client-side class that handles pause-related operations.
/// </summary>
public interface IPauseManager {
    /// <summary>
    /// Event that is called when HKMP modifies the game's timescale.
    /// </summary>
    event Action<float> SetTimeScaleEvent;
}
