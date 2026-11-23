using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PlanetOne.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlanetOne.Editor
{
    public static class BuildTools
    {
        public const string ScenePath = "Assets/PlanetOne/Scenes/Prototype.unity";

        [MenuItem("PlanetOne/Prepare Prototype")]
        public static void PrepareProject()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // VERSION and its checked-in references are changed explicitly by tools/Version.ps1.
            // Importing or rebuilding the same revision must never advance a product/build number.
            PlayerSettings.bundleVersion = ReadAndValidateProductVersion();
            Directory.CreateDirectory("Assets/PlanetOne/Scenes");
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (UnityEngine.Object.FindFirstObjectByType<PrototypeController>() == null)
                new GameObject("PlanetOne").AddComponent<PrototypeController>();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "PlanetOne";
            PlayerSettings.productName = "PlanetOne";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            ValidateProductVersion();
            Debug.Log("PlanetOne prototype scene is ready: " + ScenePath);
        }

        [MenuItem("PlanetOne/Validate Product Version")]
        public static void ValidateProductVersion()
        {
            string version = ReadAndValidateProductVersion();
            if (PlayerSettings.bundleVersion != version)
                throw new BuildFailedException("Unity's loaded product version differs from VERSION. Reopen the project after synchronizing versions.");
            Debug.Log("PlanetOne product version: " + version);
        }

        private static string ReadAndValidateProductVersion()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string versionPath = Path.Combine(projectRoot, "VERSION");
            if (!File.Exists(versionPath)) throw new BuildFailedException("VERSION is missing from the project root.");
            var utf8 = new UTF8Encoding(false, true);
            string text = File.ReadAllText(versionPath, utf8);
            var match = Regex.Match(text, @"\A(?<version>(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*))(?:\r?\n)?\z");
            if (!match.Success) throw new BuildFailedException("VERSION must contain one canonical X.Y.Z line.");
            string version = match.Groups["version"].Value;
            string settings = File.ReadAllText(Path.Combine(projectRoot, "ProjectSettings", "ProjectSettings.asset"), utf8);
            foreach (string key in new[] { "bundleVersion", "visionOSBundleVersion", "tvOSBundleVersion" })
            {
                var values = Regex.Matches(settings, @"(?m)^  " + key + @":[ \t]*(?<value>[^\r\n]*)\r?$");
                if (values.Count > 1 || (key == "bundleVersion" && values.Count != 1))
                    throw new BuildFailedException("Unexpected PlayerSettings version references: " + key);
                foreach (Match value in values)
                {
                    string scalar = value.Groups["value"].Value.Trim();
                    if (scalar.Length >= 2 && ((scalar[0] == '\'' && scalar[scalar.Length - 1] == '\'') ||
                        (scalar[0] == '"' && scalar[scalar.Length - 1] == '"')))
                        scalar = scalar.Substring(1, scalar.Length - 2);
                    if (scalar != version)
                        throw new BuildFailedException("Product version drift in " + key + ". Run tools/Version.ps1 -Action Check and resolve it before building.");
                }
            }
            return version;
        }

        [MenuItem("PlanetOne/Build Windows Prototype")]
        public static void BuildWindows()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Start Unity with -buildTarget StandaloneWindows64 before building.");
            PrepareProject();
            string target = Environment.GetEnvironmentVariable("PLANETONE_BUILD_PATH");
            if (string.IsNullOrEmpty(target)) target = Path.Combine("Builds", "Windows", "PlanetOne.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target)));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = target,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("PlanetOne build failed: " + report.summary.result + "; errors: " + report.summary.totalErrors);
            Debug.Log("PlanetOne build succeeded: " + Path.GetFullPath(target));
        }
    }

    // Also protects builds initiated through Unity's standard Build Profiles window.
    public sealed class ProductVersionBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return -1000; } }
        public void OnPreprocessBuild(BuildReport report) { BuildTools.ValidateProductVersion(); }
    }
}
