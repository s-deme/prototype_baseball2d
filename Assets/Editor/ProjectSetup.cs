using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiracleDiamond.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string MainScene = "Assets/Scenes/Main.unity";
        private const string AppIcon = "Assets/Art/AppIcon.png";
        private const string ProductName = "ミラクル・ダイヤモンド";

        static ProjectSetup()
        {
            EditorApplication.delayCall += EnsureProject;
        }

        [MenuItem("Miracle Diamond/Prepare Project")]
        public static void EnsureProject()
        {
            EnsureMainScene();
            PlayerSettings.companyName = "s-deme";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = GameRules.ProductVersion;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIcon);
            if (icon != null)
            {
                int[] iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
                var icons = new Texture2D[iconSizes.Length];
                for (int index = 0; index < icons.Length; index++) icons[index] = icon;
                PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, IconKind.Application);
            }
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Miracle Diamond/Build/Windows x64")]
        public static void BuildWindows()
        {
            EnsureProject();
            string output = Path.GetFullPath("Builds/Windows/MiracleDiamond.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.StrictMode
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            }

            string outputDirectory = Path.GetDirectoryName(output);
            File.Copy("LICENSE.txt", Path.Combine(outputDirectory, "LICENSE.txt"), true);
            File.Copy("THIRD_PARTY_NOTICES.md", Path.Combine(outputDirectory, "THIRD_PARTY_NOTICES.txt"), true);
            File.Copy("Assets/Resources/Fonts/OFL.txt", Path.Combine(outputDirectory, "NotoSansJP-OFL.txt"), true);
            Debug.Log("Windows build ready: " + output + " (" + report.summary.totalSize + " bytes)");
        }

        private static void EnsureMainScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(MainScene))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, MainScene);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
        }
    }
}
