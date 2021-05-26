﻿using UnityEngine;

namespace HKMP.UI.Component {
    public abstract class Component : IComponent {
        protected readonly GameObject GameObject;

        private readonly RectTransform _transform;

        private bool _activeSelf;
        
        public UIGroup UiGroup { private get; set; }

        protected Component(UIGroup uiGroup, Vector2 position, Vector2 size) {
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
            
            GameObject.transform.SetParent(UIManager.UiGameObject.transform, false);

            _activeSelf = true;

            UiGroup = uiGroup;
            uiGroup?.AddComponent(this);
        }

        public void SetGroupActive(bool groupActive) {
            GameObject.SetActive(_activeSelf && groupActive);
        }

        public void SetActive(bool active) {
            _activeSelf = active;
            
            GameObject.SetActive(_activeSelf && UiGroup.IsActive());
        }

        public Vector2 GetPosition() {
            return _transform.position;
        }

        public void SetPosition(Vector2 position) {
            _transform.position = position;
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