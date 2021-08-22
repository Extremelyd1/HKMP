using System.Collections.Generic;
using Hkmp.Math;

namespace Hkmp.Game.Client.Entity {
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

        /**
         * Notifies the entity to send its initial state to the server
         */
        void SendInitialState();

        /**
         * Updates the position of the entity to the given vector
         */
        void UpdatePosition(Vector2 position);

        /**
         * Updates the scale of the entity to the given bool
         */
        void UpdateScale(bool scale);

        /**
         * Updates the animation of the entity with the given animation index
         */
        void UpdateAnimation(byte animationIndex, byte[] animationInfo);

        /**
         * Initializes this entity with the given state. Used when entering the scene where entities already
         * exist and need to be initialized to be in a certain state of their existence.
         */
        void InitializeWithState(byte state);

        /**
         * Destroys the entity handling
         */
        void Destroy();

    }
}