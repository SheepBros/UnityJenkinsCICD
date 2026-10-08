using Build;
using UnityEngine;

namespace StageFlow
{
    [RequireComponent(typeof(StageRunner))]
    public sealed class StageFlowPanel : MonoBehaviour
    {
        private StageRunner runner;

        private EnvironmentConfig environment;
        
        private void Awake()
        {
            runner = GetComponent<StageRunner>();
            environment = EnvironmentConfigProvider.Load();
        }

        private void OnGUI()
        {
            if (runner == null) return;
            GUILayout.BeginArea(new Rect(16, 16, 340, 100), GUI.skin.box);
            {
                GUILayout.Label("ENVIRONMENTS");
                GUILayout.Space(8);
                GUILayout.Label("Environment: " + environment.Environment);
                GUILayout.Label("ServerUrl: " + environment.ServerUrl);
                
            }
            GUILayout.EndArea();
            
            GUILayout.BeginArea(new Rect(16, 116, 340, 560), GUI.skin.box);
            {
                GUILayout.Label("STAGEFLOW SANDBOX");
                GUILayout.Space(8);
                GUI.enabled = !runner.IsBusy;
                for (var i = 0; i < runner.StageCount; i++)
                    if (GUILayout.Button((runner.SelectedIndex == i ? "> " : "") + runner.StageName(i),
                            GUILayout.Height(32)))
                        runner.SelectStage(i);
                if (GUILayout.Button("Start", GUILayout.Height(34))) runner.StartRun();
                if (GUILayout.Button("Start Sequence (One > Two)", GUILayout.Height(34))) runner.StartSequence();
                GUI.enabled = runner.CanPause;
                if (GUILayout.Button("Pause", GUILayout.Height(34))) runner.Pause();
                GUI.enabled = runner.CanResume;
                if (GUILayout.Button("Resume", GUILayout.Height(34))) runner.Resume();
                GUI.enabled = true;
                if (GUILayout.Button("Reset", GUILayout.Height(34))) runner.ResetRun();
                GUILayout.Space(8);
                GUILayout.Label("Selected Scenario: " +
                                (runner.SelectedStage == null ? "None" : runner.SelectedStage.DisplayName));
                GUILayout.Label("State: " + runner.State);
                GUILayout.Label($"Wave: {runner.CurrentWave} / {runner.TotalWaves}");
                GUILayout.Label($"Completed Groups: {runner.CompletedGroups} / {runner.TotalGroups}");
                GUILayout.Label($"Planned Enemy: {runner.Planned}");
                GUILayout.Label($"Spawned Enemy: {runner.Spawned}");
                GUILayout.Label($"Active Enemy: {runner.ActiveCount}");
                GUILayout.Label($"Arrived Enemy: {runner.Arrived}");
                if (!string.IsNullOrEmpty(runner.LastError)) GUILayout.Label(runner.LastError);
            }
            GUILayout.EndArea();
        }
    }
}
