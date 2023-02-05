using System.Collections.Generic;
using Hkmp.Math;

namespace Hkmp.Game.Client.Entity;

internal interface IEntity {
    bool IsControlled { get; }
    bool AllowEventSending { get; set; }

    void TakeControl();

    void ReleaseControl();

    void UpdatePosition(Vector2 position);

    void UpdateState(byte state, List<byte> variables);

    void Destroy();
}
