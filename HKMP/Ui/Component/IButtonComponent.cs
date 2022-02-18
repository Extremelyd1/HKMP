using System;

namespace Hkmp.Ui.Component {
    public interface IButtonComponent : IComponent {
        void SetText(string text);
    
        void SetOnPress(Action action);

        void SetInteractable(bool interactable);
    }
}