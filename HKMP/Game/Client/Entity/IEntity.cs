using System.Collections.Generic;
using HKMP.Math;

namespace HKMP.Game.Client.Entity {
    public interface IEntity {

        /**
         * Whether the entity is controlled, meaning that transitions have been removed
         * to prevent autonomous behaviour. If controlled, it will only execute actions after
         * receiving events
         */
        bool IsControlled { get; }
        
        /**
         * Whether this entity is allowed to send events
         */
        bool AllowEventSending { get; set; }
        
        /**
         * Take control of this entity, preventing it from progressing its state on its own
         */
        void TakeControl();

        /**
         * Release control of this entity, allowing it to progress on its own again
         */
        void ReleaseControl();

        void UpdatePosition(Vector2 position);

        void UpdateScale(bool scale);

        void UpdateState(byte state, List<byte> variables);

        void Destroy();

    }
}