using UnityEngine;

namespace Build
{
    public static class EnvironmentConfigProvider
    {
        public static EnvironmentConfig Load()
        {
#if UNITY_EDITOR
            return LoadForEditor();
#else
            return Resources.Load<EnvironmentConfig>("ActiveEnvironment");
#endif
        }

#if UNITY_EDITOR
        private static EnvironmentConfig LoadForEditor()
        {
            return EnvironmentConfigFinder.Find(EditorEnvironmentSettings.Current);
        }
#endif
    }
}