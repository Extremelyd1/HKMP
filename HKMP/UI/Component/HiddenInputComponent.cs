using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class HiddenInputComponent : InputComponent {
        public HiddenInputComponent(
            GameObject parent, 
            Vector2 position, 
            string defaultValue, 
            string placeholderText, 
            int fontSize = 18, 
            InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None
        ) : this(
            parent, 
            position,
            new Vector2(200, 30),
            defaultValue, 
            placeholderText,
            TextureManager.InputFieldBackground,
            FontManager.UIFontRegular,
            fontSize, 
            characterValidation
        ) {
        }

        public HiddenInputComponent(
            GameObject parent, 
            Vector2 position, 
            Vector2 size, 
            string defaultValue, 
            string placeholderText, 
            Texture2D texture, 
            Font font, 
            int fontSize = 13, 
            InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None
        ) : base(
            parent, 
            position, 
            size, 
            defaultValue, 
            placeholderText, 
            texture, 
            font, 
            fontSize, 
            characterValidation
        ) {
            // Create a new object so we can switch between the block object
            // and the input object to manage the hidden state
            var blockObject = new GameObject();
            blockObject.AddComponent<CanvasRenderer>();
            Object.DontDestroyOnLoad(blockObject);
            
            var rectTransform = blockObject.AddComponent<RectTransform>();
            rectTransform.position = position;
            rectTransform.sizeDelta = size;
            
            blockObject.transform.SetParent(parent.transform);

            // Add the background image to the block object
            var blockImage = blockObject.AddComponent<Image>();
            blockImage.sprite = CreateSpriteFromTexture(texture);
            blockImage.type = Image.Type.Simple;

            // Add the show text to the block object
            var showText = new GameObject();
            showText.AddComponent<RectTransform>().sizeDelta = size;
            var placeholderTextComponent = showText.AddComponent<Text>();
            placeholderTextComponent.text = "Click to show";
            placeholderTextComponent.font = font;
            placeholderTextComponent.fontSize = fontSize;
            placeholderTextComponent.alignment = TextAnchor.MiddleCenter;
            placeholderTextComponent.color = new Color(0, 0, 0, 0.5f);

            // Set the transform parent
            showText.transform.SetParent(blockObject.transform, false);
            Object.DontDestroyOnLoad(showText);
            
            // Disable the original input component object
            GameObject.SetActive(false);

            // Add a button component so we can click it to show the input component
            var blockButton = blockObject.AddComponent<Button>();
            
            // Hide our block object and show the input component on click
            blockButton.onClick.AddListener(() => {
                blockObject.SetActive(false);
                GameObject.SetActive(true);
            });
            
            // Add a handler for when we leave the component with our cursor,
            // which is when we enable the block object again and hide the input component
            var hiddenButtonLeaveHandler = GameObject.AddComponent<HiddenButtonLeaveHandler>();
            hiddenButtonLeaveHandler.DeactivateObject = GameObject;
            hiddenButtonLeaveHandler.ActivateObject = blockObject;
        }
    }
}