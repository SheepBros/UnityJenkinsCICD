#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Build
{
    public static class EditorEnvironmentSettings
    {
        private const string Key = "UnityJenkinsCICD.EditorEnvironment";

        public static BuildEnvironment Current
        {
            get
            {
                string value = EditorPrefs.GetString(Key, BuildEnvironment.Dev.ToString());
                return Enum.TryParse(value, out BuildEnvironment environment) ? environment : BuildEnvironment.Dev;
            }
            set => EditorPrefs.SetString(Key, value.ToString());
        }
    }
}
#endif