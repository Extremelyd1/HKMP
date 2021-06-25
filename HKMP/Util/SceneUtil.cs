using System.Collections.Generic;

namespace Hkmp.Util {
    public static class SceneUtil {
        private static List<string> _nonGameplayScenes = new List<string> {
            "BetaEnd",
            "Cinematic_Stag_travel",
            "Cinematic_Ending_A",
            "Cinematic_Ending_B",
            "Cinematic_Ending_C",
            "Cinematic_Ending_D",
            "Cinematic_Ending_E",
            "Cinematic_MrMushroom",
            "Cutscene_Boss_Door",
            "End_Credits",
            "End_Game_Completion",
            "GG_Boss_Door_Entrance",
            "GG_End_Sequence",
            "GG_Entrance_Cutscene",
            "GG_Unlock",
            "Intro_Cutscene",
            "Intro_Cutscene_Prologue",
            "Knight Pickup",
            "Menu_Title",
            "Menu_Credits",
            "Opening_Sequence",
            "PermaDeath_Unlock",
            "Pre_Menu_Intro",
            "PermaDeath",
            "Prologue_Excerpt",
        };

        public static bool IsNonGameplayScene(string sceneName) {
            return _nonGameplayScenes.Contains(sceneName);
        }

        public static string GetCurrentSceneName() {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
    }
}