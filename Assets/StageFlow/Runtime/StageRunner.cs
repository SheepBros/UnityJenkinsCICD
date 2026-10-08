using System;
using System.Collections.Generic;
using UnityEngine;

namespace StageFlow
{
    public enum RunState { Idle, Running, Waiting, Paused, Completed }

    public sealed class StageRunner : MonoBehaviour
    {
        private readonly struct StageRun
        {
            public readonly int StageIndex;
            public readonly StagePlan Plan;

            public StageRun(int stageIndex, StagePlan plan)
            { StageIndex = stageIndex; Plan = plan; }
        }

        [SerializeField] private StageDefinition[] stages = Array.Empty<StageDefinition>();
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform destination;
        private readonly List<EnemyMover> active = new List<EnemyMover>();
        private readonly List<StageRun> runs = new List<StageRun>();
        private StagePlan plan;
        private WaveRuntime wave;
        private int selectedIndex;
        private int currentWaveIndex;
        private int completedGroupsBeforeWave;
        private float remaining;
        private RunState stateBeforePause;

        public RunState State { get; private set; }
        public float DeltaTime => State == RunState.Paused ? 0f : Time.deltaTime;
        public int CurrentIndex { get; private set; }
        public int CurrentWave => plan == null ? 0 : currentWaveIndex + 1;
        public int TotalWaves => plan?.Waves.Count ?? PreviewPlan()?.Waves.Count ?? 0;
        public int CompletedGroups => completedGroupsBeforeWave + (wave?.CompletedGroups ?? 0);
        public int TotalGroups => plan?.TotalGroups ?? PreviewPlan()?.TotalGroups ?? 0;
        public bool CanPause => State == RunState.Running || State == RunState.Waiting;
        public bool CanResume => State == RunState.Paused;
        public bool IsBusy => CanPause || CanResume;
        public int Spawned { get; private set; }
        public int Arrived { get; private set; }
        public int ActiveCount => active.Count;
        public string LastError { get; private set; } = "";
        public int StageCount => stages.Length;
        public int SelectedIndex => selectedIndex;
        public StageDefinition SelectedStage => stages.Length == 0 ? null : stages[selectedIndex];
        public int Planned => plan?.Total ?? PreviewPlan()?.Total ?? 0;
        public string StageName(int index) => stages[index] == null ? "Missing stage" : stages[index].DisplayName;

        public void Configure(StageDefinition[] definitions, Transform spawn, Transform target)
        {
            if (IsBusy) throw new InvalidOperationException("Reset before configuring.");
            ResetRun();
            stages = definitions == null ? Array.Empty<StageDefinition>() : (StageDefinition[])definitions.Clone();
            selectedIndex = 0;
            spawnPoint = spawn;
            destination = target;
        }

        public bool SelectStage(int index)
        {
            if (IsBusy || index < 0 || index >= stages.Length) return false;
            ResetRun();
            selectedIndex = index;
            return true;
        }

        public bool StartRun() => StartRuns(new[] { selectedIndex });

        // Stage One and Stage Two remain the fixed regression sequence.
        public bool StartSequence()
        {
            if (IsBusy) return false;
            if (stages.Length < 2)
            { LastError = "The sequence requires Stage One and Stage Two first in scene order."; return false; }
            return StartRuns(new[] { 0, 1 });
        }

        private bool StartRuns(int[] stageIndices)
        {
            if (IsBusy) return false;
            if (!isActiveAndEnabled || spawnPoint == null || destination == null)
            { LastError = "An active runner, spawn point and destination are required."; return false; }
            var nextRuns = new List<StageRun>();
            foreach (var index in stageIndices)
            {
                var stage = index >= 0 && index < stages.Length ? stages[index] : null;
                if (!StagePlan.TryCreate(stage, out var nextPlan, out var error))
                { LastError = error; return false; }
                nextRuns.Add(new StageRun(index, nextPlan));
            }
            ResetRun();
            runs.AddRange(nextRuns);
            BeginStage();
            return true;
        }

        private void BeginStage()
        {
            var run = runs[CurrentIndex];
            selectedIndex = run.StageIndex;
            plan = run.Plan;
            Spawned = Arrived = completedGroupsBeforeWave = currentWaveIndex = 0;
            BeginWave();
        }

        private void BeginWave()
        {
            wave = new WaveRuntime(plan.Waves[currentWaveIndex]);
            State = RunState.Running;
        }

        public bool Pause()
        {
            if (!CanPause) return false;
            stateBeforePause = State;
            State = RunState.Paused;
            return true;
        }

        public bool Resume()
        {
            if (!CanResume) return false;
            State = stateBeforePause;
            stateBeforePause = RunState.Idle;
            return true;
        }

        public void ResetRun()
        {
            State = stateBeforePause = RunState.Idle;
            CurrentIndex = 0;
            runs.Clear();
            foreach (var mover in active)
            {
                if (mover == null) continue;
                mover.Cancel();
                mover.gameObject.SetActive(false);
                Destroy(mover.gameObject);
            }
            active.Clear();
            plan = null;
            wave = null;
            Spawned = Arrived = currentWaveIndex = completedGroupsBeforeWave = 0;
            remaining = 0f;
            LastError = "";
        }

        private void Update()
        {
            var deltaTime = DeltaTime;
            if (deltaTime <= 0f) return;
            if (State == RunState.Waiting)
            {
                remaining -= deltaTime;
                if (remaining > 0f) return;
                BeginStage();
            }
            if (State != RunState.Running) return;
            wave.Tick(deltaTime, Spawn);
            TryCompleteWave();
        }

        private void Spawn(StagePlan.Group group)
        {
            if (group.Prefab == null || spawnPoint == null || destination == null)
            {
                FailRun("A runtime reference was removed. Run reset.");
                return;
            }

            Vector3[] route;
            if (group.Waypoints.Count == 0)
                route = new[] { spawnPoint.position, destination.position };
            else
            {
                route = new Vector3[group.Waypoints.Count];
                for (var i = 0; i < route.Length; i++) route[i] = group.Waypoints[i];
            }

            var instance = Instantiate(group.Prefab, route[0], Quaternion.identity, transform);
            var mover = instance.GetComponent<EnemyMover>();
            active.Add(mover);
            mover.Initialize(route, group.Speed, OnArrival, () => DeltaTime);
            Spawned++;
        }

        private void OnArrival(EnemyMover mover)
        {
            if (!active.Remove(mover)) return;
            Arrived++;
            mover.gameObject.SetActive(false);
            Destroy(mover.gameObject);
            TryCompleteWave();
        }

        private void TryCompleteWave()
        {
            if (State != RunState.Running || wave == null || !wave.IsSpawningComplete || active.Count != 0) return;
            completedGroupsBeforeWave += wave.TotalGroups;
            wave = null;
            currentWaveIndex++;
            if (currentWaveIndex < plan.Waves.Count)
            {
                BeginWave();
                return;
            }

            CurrentIndex++;
            if (CurrentIndex < runs.Count)
            {
                State = RunState.Waiting;
                remaining = 1f;
                return;
            }

            State = RunState.Completed;
            currentWaveIndex = plan.Waves.Count - 1;
            CurrentIndex = 0;
            runs.Clear();
        }

        private StagePlan PreviewPlan()
        {
            return StagePlan.TryCreate(SelectedStage, out var preview, out _) ? preview : null;
        }

        private void FailRun(string error)
        {
            ResetRun();
            LastError = error;
        }

        private void OnDisable() => ResetRun();
    }
}
