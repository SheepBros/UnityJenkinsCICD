using UnityEngine;

namespace Build
{
    [CreateAssetMenu(menuName = "Build/EnvironmentConfig")]
    public class EnvironmentConfig : ScriptableObject
    {
        public BuildEnvironment Environment;
        
        public string ServerUrl;

        public bool VerboseLogging;

        public bool DebugMenuEnabled;
    }
}