namespace HKMP.UI.Component {
    public interface IInputComponent : IComponent {
        string GetInput();

        void SetPlaceholder(string text);
    }
}