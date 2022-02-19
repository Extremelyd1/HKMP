using System;
using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hkmp.Ui.Component {
    public class ButtonComponent : Component, IButtonComponent {
        private const float DefaultWidth = 240f;
        public const float DefaultHeight = 38f;

        private readonly MultiStateSprite _bgSprite;
        private readonly Text _text;
        private readonly Image _image;

        private Action _onPress;
        private bool _interactable;

        private bool _isHover;
        private bool _isMouseDown;

        public ButtonComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            string text
        ) : this(
            componentGroup,
            position,
            new Vector2(DefaultWidth, DefaultHeight),
            text,
            TextureManager.ButtonBg,
            FontManager.UIFontRegular,
            UiManager.NormalFontSize) {
        }

        public ButtonComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            Vector2 size,
            string text,
            MultiStateSprite bgSprite,
            Font font,
            int fontSize
        ) : this(
            componentGroup,
            position,
            size,
            text,
            bgSprite,
            font,
            Color.white,
            fontSize
        ) {
        }

        public ButtonComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            Vector2 size,
            string text,
            MultiStateSprite bgSprite,
            Font font,
            Color textColor,
            int fontSize
        ) : base(componentGroup, position, size) {
            _bgSprite = bgSprite;
            _interactable = true;
            
            // Create background image
            _image = GameObject.AddComponent<Image>();
            _image.sprite = bgSprite.Neutral;
            _image.type = Image.Type.Sliced;

            // Create the text component in the button
            var textObject = new GameObject();
            textObject.AddComponent<RectTransform>().sizeDelta = size;
            _text = textObject.AddComponent<Text>();
            _text.text = text;
            _text.font = font;
            _text.fontSize = fontSize;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = textColor;

            // Set the transform parent to the ButtonComponent gameObject
            textObject.transform.SetParent(GameObject.transform, false);
            Object.DontDestroyOnLoad(textObject);

            var eventTrigger = GameObject.AddComponent<EventTrigger>();
            _isMouseDown = false;
            _isHover = false;
            
            AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, data => {
                _isHover = true;

                if (_interactable) {
                    _image.sprite = bgSprite.Hover;
                }
            });
            AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, data => {
                _isHover = false;
                if (_interactable && !_isMouseDown) {
                    _image.sprite = bgSprite.Neutral;
                }
            });
            AddEventTrigger(eventTrigger, EventTriggerType.PointerDown, data => {
                _isMouseDown = true;

                if (_interactable) {
                    _image.sprite = bgSprite.Active;
                }
            });
            AddEventTrigger(eventTrigger, EventTriggerType.PointerUp, data => {
                _isMouseDown = false;

                if (_interactable) {
                    if (_isHover) {
                        _image.sprite = bgSprite.Hover;
                        _onPress?.Invoke();
                    } else {
                        _image.sprite = bgSprite.Neutral;
                    }
                }
            });
        }

        public void SetText(string text) {
            _text.text = text;
        }

        public void SetOnPress(Action action) {
            _onPress = action;
        }

        public void SetInteractable(bool interactable) {
            _interactable = interactable;
            
            var color = _text.color;
            
            if (interactable) {
                _image.sprite = _bgSprite.Neutral;
                color.a = 1f;
            } else {
                _image.sprite = _bgSprite.Disabled;
                color.a = NotInteractableOpacity;
            }

            _text.color = color;
        }

        private void EvaluateState() {
            if (GameObject == null || _image == null) {
                return;
            }
            
            if (!GameObject.activeSelf) {
                _image.sprite = _interactable ? _bgSprite.Neutral : _bgSprite.Disabled;

                _isHover = false;
                _isMouseDown = false;
            }
        }

        public override void SetGroupActive(bool groupActive) {
            base.SetGroupActive(groupActive);

            EvaluateState();
        }

        public override void SetActive(bool active) {
            base.SetActive(active);

            EvaluateState();
        }
    }
}