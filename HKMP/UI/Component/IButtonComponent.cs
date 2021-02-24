using System;

namespace HKMP.UI.Component {
    public interface IButtonComponent : IComponent {
        void SetOnPress(Action action);
    }
}