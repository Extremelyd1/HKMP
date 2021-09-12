namespace Hkmp.Ui.Component {
    public interface IInputComponent : IComponent {
        void SetInput(string input);
    
        string GetInput();
    }
}