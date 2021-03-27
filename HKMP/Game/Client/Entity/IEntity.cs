namespace HKMP.Game.Client.Entity {
    public interface IEntity {

        bool IsControlled { get; }
        bool AllowEventSending { get; set; }
        
        void TakeControl();

        void ReleaseControl();

        void UpdateState(byte stateIndex);

        void UpdateVariables(byte[] variableArray);

        void Destroy();

    }
}