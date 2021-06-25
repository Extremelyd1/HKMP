using System;

namespace Hkmp.Ui.Component {
    public interface IButtonComponent : IComponent {
        void SetOnPress(Action action);
    }
}