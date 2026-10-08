#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Build
{
    public static partial class BuildCommand
    {
        private const string GeneratedResourcesDirectory = "Assets/Build/Generated/Resources";
        private const string ActiveEnvironmentConfigPath = "Assets/Build/Generated/Resources/ActiveEnvironment.asset";

        public static void Build()
        {
            try
            {
                BuildArguments buildArguments = GetCommandLineArgs();
                EnvironmentConfig config = EnvironmentConfigFinder.Find(buildArguments.Environment);
                
                Validation(buildArguments, config);
                PrepareEnvironmentConfig(config);
                ConfigureEnvironmentBuild(buildArguments);

                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
                buildPlayerOptions.scenes = GetScenes();
                buildPlayerOptions.target = buildArguments.Target;
                buildPlayerOptions.options = buildArguments.GetBuildOptions();
                buildPlayerOptions.locationPathName = buildArguments.GetOutputPath();

                BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

                if (buildReport.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"Result: {buildReport.summary.result}");
                }
                else
                {
                    throw new Exception($"Build failed: {buildReport.summary.result}");
                }
            }
            finally
            {
                CleanupGeneratedResources();
            }
        }

        private static string[] GetScenes()
        {
            List<string> scenes = new List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            if (scenes.Count == 0)
            {
                Debug.LogError($"No Valid Scenes found");
                throw new Exception("No Valid Scenes found");
            }
            
            return scenes.ToArray();
        }

        private static BuildArguments GetCommandLineArgs()
        {
            return new BuildArguments(Environment.GetCommandLineArgs());
        }

        private static void PrepareEnvironmentConfig(EnvironmentConfig envConfig)
        {
            Directory.CreateDirectory(GeneratedResourcesDirectory);
            AssetDatabase.Refresh();

            try
            {
                if (AssetDatabase.LoadAssetAtPath<EnvironmentConfig>(ActiveEnvironmentConfigPath) != null)
                {
                    AssetDatabase.DeleteAsset(ActiveEnvironmentConfigPath);
                }

                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(envConfig), ActiveEnvironmentConfigPath))
                {
                    throw new Exception($"Couldn't copy environment config {envConfig}");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to copy Active environment configuration asset at {ActiveEnvironmentConfigPath}", e);
            }
        }

        private static void CleanupGeneratedResources()
        {
            AssetDatabase.DeleteAsset(GeneratedResourcesDirectory);
        }

        private static void Validation(BuildArguments buildArguments, EnvironmentConfig config)
        {
            if (buildArguments.Environment != config.Environment)
            {
                throw new Exception($"Project environment and arguements are not equal. " +
                                    $"Arguments: {buildArguments.Environment}, Project: {config.Environment}");
            }
            
            if (string.IsNullOrEmpty(config.ServerUrl))
            {
                throw new Exception($"Build server url is required.");
            }

            if (config.Environment == BuildEnvironment.Prod &&
                (config.DebugMenuEnabled || config.VerboseLogging))
            {
                throw new Exception($"The environment is Prod." +
                                    $"But, DebugMenuEnabled({config.DebugMenuEnabled}) or VerboseLogging({config.VerboseLogging}) is enabled.");
            }
        }

        private static void ConfigureEnvironmentBuild(BuildArguments buildArguments)
        {
            if (!string.IsNullOrWhiteSpace(buildArguments.Version))
            {
                PlayerSettings.bundleVersion = buildArguments.Version;
            }
            
            if (buildArguments.Target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = buildArguments.PackageFormat == BuildAndroidPackageFormat.aab;
                
                PlayerSettings.Android.bundleVersionCode = buildArguments.BuildNumber;
                
                AndroidSigning.Apply();
            }
            else if (buildArguments.Target == BuildTarget.iOS)
            {
                PlayerSettings.iOS.buildNumber = buildArguments.BuildNumber.ToString();
            }
        }
    }
}
#endif