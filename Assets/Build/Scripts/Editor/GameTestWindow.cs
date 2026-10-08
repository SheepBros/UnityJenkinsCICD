#if UNITY_EDITOR
using UnityEditor;

namespace Build
{
    public class GameTestWindow : EditorWindow
    {
        [MenuItem("Test/Open Test Window")]
        public static void Open()
        {
            GetWindow<GameTestWindow>("Game Test");
        }

        private void OnGUI()
        {
            BuildEnvironment current = EditorEnvironmentSettings.Current;
            BuildEnvironment selected = (BuildEnvironment)EditorGUILayout.EnumPopup("Environment", current);
            if (selected != current)
            {
                EditorEnvironmentSettings.Current = selected;
            }
        }
    }
}
#endif