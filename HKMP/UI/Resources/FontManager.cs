using System.Collections.Generic;
using UnityEngine;

namespace HKMP.UI.Resources {
    public class FontManager {
        private readonly Dictionary<string, Font> _fonts;

        public FontManager() {
            _fonts = new Dictionary<string, Font>();
        }

        public void LoadFonts() {
            foreach (var font in UnityEngine.Resources.FindObjectsOfTypeAll<Font>()) {
                if (!_fonts.ContainsKey(font.name)) {
                    _fonts.Add(font.name, font);
                }
            }
            
            Logger.Info(this, $"Successfully loaded {_fonts.Count} fonts");
        }

        public Font GetFont(string fontName) {
            if (!_fonts.ContainsKey(fontName)) {
                Logger.Warn(this, $"Tried to load font with name {fontName}, which does not exist");
                return null;
            }

            return _fonts[fontName];
        }
        
    }
}