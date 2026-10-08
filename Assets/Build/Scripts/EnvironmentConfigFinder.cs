#if UNITY_EDITOR

using System;
using UnityEditor;

namespace Build
{
    public static class EnvironmentConfigFinder
    {
        public static EnvironmentConfig Find(BuildEnvironment environment)
        {
            string[] guids =
                AssetDatabase.FindAssets("t:EnvironmentConfig", new[] { "Assets/Build/Data" });

            EnvironmentConfig environmentConfig = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                EnvironmentConfig config = AssetDatabase.LoadAssetAtPath<EnvironmentConfig>(path);

                if (config == null || config.Environment != environment)
                {
                    continue;
                }

                if (environmentConfig != null)
                {
                    throw new Exception(
                        $"Multiple environment configurations found for {environment}.");
                }

                environmentConfig = config;
            }

            if (environmentConfig == null)
            {
                throw new Exception($"No environment configuration found for {environment}.");
            }

            return environmentConfig;
        }
    }
}

#endif