using UnityEngine;

namespace HKMP.Game.Client.Entity {
    public interface IEntity {

        bool IsControlled { get; }
        bool AllowEventSending { get; set; }
        
        void TakeControl();

        void ReleaseControl();

        void UpdatePosition(Vector2 position);

        void UpdateState(byte stateIndex);

        void UpdateVariables(byte[] variableArray);

        void Destroy();

    }
}