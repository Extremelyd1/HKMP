using System.Collections.Generic;
using UnityEngine;

namespace HKMP.UI.Resources {
    public class FontManager {
        private static readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();

        public static void LoadFonts() {
            foreach (var font in UnityEngine.Resources.FindObjectsOfTypeAll<Font>()) {
                if (!_fonts.ContainsKey(font.name)) {
                    _fonts.Add(font.name, font);
                }
            }
            
            Logger.Info(typeof(FontManager), $"Successfully loaded {_fonts.Count} fonts");
        }

        public static Font GetFont(string fontName) {
            if (!_fonts.ContainsKey(fontName)) {
                Logger.Warn(typeof(FontManager), $"Tried to load font with name {fontName}, which does not exist");
                return null;
            }

            return _fonts[fontName];
        }
        
    }
}