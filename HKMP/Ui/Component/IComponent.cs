using UnityEngine;

namespace Hkmp.Ui.Component {
    public interface IComponent {
        void SetGroupActive(bool groupActive);

        void SetActive(bool active);

        Vector2 GetPosition();

        void SetPosition(Vector2 position);

        Vector2 GetSize();
    }
}