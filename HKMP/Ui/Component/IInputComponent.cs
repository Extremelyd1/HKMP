using System;

namespace Hkmp.Ui.Component {
    public interface IInputComponent : IComponent {
        void SetInput(string input);
    
        string GetInput();

        void SetInteractable(bool interactable);

        void SetOnChange(Action<string> onChange);
    }
}