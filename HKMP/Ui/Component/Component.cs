using UnityEngine;

namespace Hkmp.Ui.Component {
    public abstract class Component : IComponent {
        protected readonly GameObject GameObject;

        private readonly RectTransform _transform;

        private bool _activeSelf;

        private readonly ComponentGroup _componentGroup;

        protected Component(ComponentGroup componentGroup, Vector2 position, Vector2 size) {
            // Create a gameobject with the CanvasRenderer component, so we can render as GUI
            GameObject = new GameObject();
            GameObject.AddComponent<CanvasRenderer>();
            // Make sure game object persists
            Object.DontDestroyOnLoad(GameObject);

            // Create a RectTransform with the desired size
            _transform = GameObject.AddComponent<RectTransform>();

            position = new Vector2(
                position.x / 1920f,
                position.y / 1080f
            );
            _transform.anchorMin = _transform.anchorMax = position;

            _transform.sizeDelta = size;

            GameObject.transform.SetParent(UiManager.UiGameObject.transform, false);

            _activeSelf = true;

            _componentGroup = componentGroup;
            componentGroup?.AddComponent(this);
        }

        public void SetGroupActive(bool groupActive) {
            // TODO: figure out why this could be happening
            if (GameObject == null) {
                // Logger.Get().Error(this, 
                //     $"The GameObject belonging to this component (type: {GetType()}) is null, this shouldn't happen");
                return;
            }

            GameObject.SetActive(_activeSelf && groupActive);
        }

        public void SetActive(bool active) {
            _activeSelf = active;

            GameObject.SetActive(_activeSelf && _componentGroup.IsActive());
        }

        public Vector2 GetPosition() {
            var position = _transform.anchorMin;
            return new Vector2(
                position.x * 1920f,
                position.y * 1080f
            );
        }

        public void SetPosition(Vector2 position) {
            _transform.anchorMin = _transform.anchorMax = new Vector2(
                position.x / 1920f,
                position.y / 1080f
            );
        }

        public Vector2 GetSize() {
            return _transform.sizeDelta;
        }

        public void Destroy() {
            Object.Destroy(GameObject);
        }

        protected static Sprite CreateSpriteFromTexture(Texture2D texture) {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(texture.width / 2.0f, texture.height / 2.0f)
            );
        }
    }
}