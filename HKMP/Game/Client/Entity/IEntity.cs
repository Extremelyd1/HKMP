using Hkmp.Math;

namespace Hkmp.Game.Client.Entity {
    public interface IEntity {
        /**
         * Initializes the entity given that the local player is scene host
         */
        void InitializeAsSceneHost();

        /**
         * Initializes the entity given that the local player is scene client (possibly with a given state index)
         */
        void InitializeAsSceneClient(byte? stateIndex);

        /**
         * Switches the entity to reflect that the local player has turned scene host
         */
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
         * Updates the animation of the entity with the given animation index and additional information
         */
        void UpdateAnimation(byte animationIndex, byte[] animationInfo);

        /**
         * Updates the state of the entity
         */
        void UpdateState(byte stateIndex);

        /**
         * Destroys the entity handling
         */
        void Destroy();

    }
}