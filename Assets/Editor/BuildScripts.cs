using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ClickerGenesis.EditorTools
{
    /// <summary>
    /// Per-platform build entry points, callable via script-execute or Unity's own
    /// "Tools" menu. Always uses BuildOptions.CleanBuildCache - a script-triggered
    /// BuildPipeline.BuildPlayer call was found (2026-08-11) to silently reuse a stale
    /// incremental cache otherwise, producing a build missing months of scene work.
    ///
    /// Desktop targets (Windows/Mac/Linux) build from the shared EditorBuildSettings
    /// scene list (Assets/_Scenes/*). Android builds from a SEPARATE scene list
    /// (Assets/_ScenesAndroid/*) - same scene names, independent GUIDs, independently
    /// editable layout - per the 2026-08-13 decision to stop a single shared scene file
    /// from forcing Android-motivated layout tweaks onto the desktop build and vice
    /// versa. Any layout-affecting change made to a desktop scene must be manually
    /// mirrored into its Assets/_ScenesAndroid counterpart as part of the same task.
    /// </summary>
    public static class BuildScripts
    {
        private static readonly string[] AndroidScenes =
        {
            "Assets/_ScenesAndroid/MainMenu.unity",
            "Assets/_ScenesAndroid/ClickerScreen.unity",
            "Assets/_ScenesAndroid/BuyVerseScreen.unity",
            "Assets/_ScenesAndroid/SettingsScreen.unity",
            "Assets/_ScenesAndroid/PrestigeScreen.unity",
            "Assets/_ScenesAndroid/AchievementScreen.unity",
            "Assets/_ScenesAndroid/CreditsScreen.unity",
            "Assets/_ScenesAndroid/StatsScreen.unity",
            "Assets/_ScenesAndroid/StoreScreen.unity",
            "Assets/_ScenesAndroid/SaveSlotScreen.unity",
            "Assets/_ScenesAndroid/NewGameSetupScreen.unity",
        };

        private static string[] DesktopScenes =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem("Tools/Build/Windows")]
        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = DesktopScenes,
                locationPathName = "Builds/Windows/ClickerGenesis.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache,
            });
            LogResult("Windows", report);
        }

        [MenuItem("Tools/Build/macOS")]
        public static void BuildMac()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = DesktopScenes,
                locationPathName = "Builds/Mac/ClickerGenesis.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CleanBuildCache,
            });
            LogResult("Mac", report);
        }

        [MenuItem("Tools/Build/Linux")]
        public static void BuildLinux()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = DesktopScenes,
                locationPathName = "Builds/Linux/ClickerGenesis.x86_64",
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.CleanBuildCache,
            });
            LogResult("Linux", report);
        }

        [MenuItem("Tools/Build/Android")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = AndroidScenes,
                locationPathName = "Builds/Android/ClickerGenesis.apk",
                target = BuildTarget.Android,
                options = BuildOptions.CleanBuildCache,
            });
            LogResult("Android", report);
        }

        private static void LogResult(string platform, UnityEditor.Build.Reporting.BuildReport report)
        {
            Debug.Log($"BUILD_RESULT_{platform.ToUpperInvariant()}:{report.summary.result} SIZE:{report.summary.totalSize} ERRORS:{report.summary.totalErrors}");
        }
    }
}
