using System.Collections.Generic;

namespace Hkmp.Util;

/// <summary>
/// Class for utilities regarding scenes and scene names.
/// </summary>
internal static class SceneUtil {
    /// <summary>
    /// List of scene names that are considered to be non-gameplay.
    /// </summary>
    private static readonly List<string> NonGameplayScenes = new List<string> {
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

    /// <summary>
    /// Check whether the scene with the given name is non-gameplay.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    /// <returns>true if the scene is non-gameplay; otherwise false.</returns>
    public static bool IsNonGameplayScene(string sceneName) {
        return NonGameplayScenes.Contains(sceneName);
    }

    /// <summary>
    /// Get the name of the currently active scene.
    /// </summary>
    /// <returns>The name of the active scene.</returns>
    public static string GetCurrentSceneName() {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
}
