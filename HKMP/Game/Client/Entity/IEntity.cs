using System.Collections.Generic;
using Hkmp.Math;

namespace Hkmp.Game.Client.Entity {
    public interface IEntity {
        void InitializeAsSceneHost();

        void InitializeAsSceneClient(byte? state);

        void SwitchToSceneHost();

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
         * Updates the state of the entity
         */
        void UpdateState(byte state);

        /**
         * Destroys the entity handling
         */
        void Destroy();

    }
}