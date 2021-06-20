using UnityEngine;

namespace Hkmp.Ui.Component {
    public interface ITextComponent : IComponent {
        void SetText(string text);

        void SetColor(Color color);
    }
}