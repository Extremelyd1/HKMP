using UnityEngine;

namespace HKMP.UI.Component {
    public interface ITextComponent : IComponent {
        void SetText(string text);

        void SetColor(Color color);
    }
}