using TMPro;
using UnityEngine;

namespace HKMP.UI.Resources {
    public class FontManager {
        public static Font UIFontRegular;
        public static Font UIFontBold;
        public static TMP_FontAsset InGameNameFont;

        public static void LoadFonts() {
            foreach (var font in UnityEngine.Resources.FindObjectsOfTypeAll<Font>()) {
                switch (font.name) {
                    case "TrajanPro-Regular":
                        UIFontRegular = font;
                        break;
                    case "TrajanPro-Bold":
                        UIFontBold = font;
                        break;
                }
            }

            foreach (var textMeshProFont in UnityEngine.Resources.FindObjectsOfTypeAll<TMP_FontAsset>()) {
                switch (textMeshProFont.name) {
                    case "TrajanPro-Bold SDF":
                        InGameNameFont = textMeshProFont;
                        break;
                }
            }

            if (UIFontRegular == null) {
                Logger.Get().Error("FontManager", "UI font regular is missing!");
            }
            
            if (UIFontBold == null) {
                Logger.Get().Error("FontManager", "UI font bold is missing!");
            }
            
            if (InGameNameFont == null) {
                Logger.Get().Error("FontManager", "In-game name font is missing!");
            }
        }
        
    }
}