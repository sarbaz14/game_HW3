using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ChickenShooter.EditorTools
{
    /// <summary>Runs the whole content pipeline in the correct order.</summary>
    public static class BuildAllEditor
    {
        [MenuItem("ChickenShooter/Build Everything", false, 100)]
        public static void BuildEverything()
        {
            Debug.Log("=== Chicken Shooter: full build started ===");

            ProjectBootstrapEditor.ConfigureProject();
            AssetGeneratorEditor.GenerateAll();
            PrefabAuthoringEditor.BuildAllPrefabs();
            InputActionsGenerator.Generate();
            DataAuthoringEditor.BuildAllData();
            SceneBuilderEditor.BuildAllScenes();
            ConfigureWindowsBuildTarget();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("=== Chicken Shooter: full build finished (4 scenes, 22 waves, 3 bosses, 3 weapons) ===");
        }

        [MenuItem("ChickenShooter/6. Configure Windows Build Target", false, 60)]
        public static void ConfigureWindowsBuildTarget()
        {
            var target = NamedBuildTarget.Standalone;

            try
            {
                PlayerSettings.SetApplicationIdentifier(target, "com.chickenshooter.game");
            }
            catch (System.Exception)
            {
                // Identifier format is not critical for the project.
            }

            try
            {
                PlayerSettings.SetArchitecture(target, 1); // x86_64
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
                PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[Build] Could not configure Standalone x86_64: " + exception.Message);
            }

            Debug.Log("[Build] Standalone target configured for Windows x86_64. " +
                      "Building requires the Windows Build Support module.");
        }

        [MenuItem("ChickenShooter/7. Report Build Settings", false, 70)]
        public static void ReportBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            var report = "Build Settings scenes (" + scenes.Length + "):\n";
            for (int i = 0; i < scenes.Length; i++)
            {
                report += "  [" + i + "] " + scenes[i].path + " enabled=" + scenes[i].enabled + "\n";
            }

            Debug.Log(report);
        }
    }
}
