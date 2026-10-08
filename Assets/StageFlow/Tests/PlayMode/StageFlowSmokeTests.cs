using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace StageFlow.Tests
{
    public sealed class StageFlowSmokeTests
    {
#if UNITY_EDITOR
        private Scene scene;
        private StageRunner runner;

        [UnitySetUp]
        public IEnumerator OpenSample()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/StageFlow/Scenes/StageFlowDemo.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath("Assets/StageFlow/Scenes/StageFlowDemo.unity");
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<StageRunner>(out var found)) runner = found;
            Assert.That(runner, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator CloseSample()
        {
            if (runner != null) runner.ResetRun();
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator BothStagesCompleteAndResetClearsAnInFlightRun()
        {
            Assert.That(runner.State, Is.EqualTo(RunState.Idle));
            var firstData = JsonUtility.ToJson(runner.SelectedStage);
            Assert.That(runner.StartRun(), Is.True);
            Assert.That(runner.StartRun(), Is.False);
            Assert.That(runner.SelectStage(1), Is.False);
            yield return null;
            yield return null;
            Assert.That(runner.Spawned, Is.GreaterThan(0));
            Assert.That(runner.ActiveCount, Is.GreaterThan(0));
            runner.ResetRun();
            yield return null;
            Assert.That(runner.State, Is.EqualTo(RunState.Idle));
            Assert.That(runner.Spawned + runner.Arrived + runner.ActiveCount, Is.Zero);
            Assert.That(runner.GetComponentsInChildren<EnemyMover>(true), Is.Empty);
            Assert.That(JsonUtility.ToJson(runner.SelectedStage), Is.EqualTo(firstData));

            for (var index = 0; index < 2; index++)
            {
                Assert.That(runner.SelectStage(index), Is.True);
                var stage = runner.SelectedStage;
                var before = JsonUtility.ToJson(stage);
                var enemyBefore = JsonUtility.ToJson(stage.Entries[0].Enemy);
                var expected = index == 0 ? 5 : 8;
                Assert.That(runner.StartRun(), Is.True);
                var deadline = Time.realtimeSinceStartup + 20f;
                while (runner.State == RunState.Running && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(runner.Spawned, Is.EqualTo(runner.ActiveCount + runner.Arrived));
                    yield return null;
                }
                Assert.That(runner.State, Is.EqualTo(RunState.Completed), "Stage completion timed out.");
                Assert.That(runner.Planned, Is.EqualTo(expected));
                Assert.That(runner.Spawned, Is.EqualTo(expected));
                Assert.That(runner.Arrived, Is.EqualTo(expected));
                Assert.That(runner.ActiveCount, Is.Zero);
                Assert.That(JsonUtility.ToJson(stage), Is.EqualTo(before));
                Assert.That(JsonUtility.ToJson(stage.Entries[0].Enemy), Is.EqualTo(enemyBefore));
                runner.ResetRun();
                yield return null;
                Assert.That(runner.GetComponentsInChildren<EnemyMover>(true), Is.Empty);
            }
        }
        private IEnumerator WaitFor(Func<bool> predicate, float timeout = 25f)
        {
            var deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, "Run transition timed out.");
        }

        private void AssertBusy()
        {
            Assert.That(runner.IsBusy, Is.True);
            Assert.That(runner.StartRun(), Is.False);
            Assert.That(runner.StartSequence(), Is.False);
            Assert.That(runner.SelectStage(1), Is.False);
            Assert.Throws<InvalidOperationException>(() => runner.Configure(null, null, null));
        }

        [UnityTest]
        public IEnumerator SequenceWaitsForAllArrivalsThenOneSecondAndPreservesAllData()
        {
            var originals = new Dictionary<UnityEngine.Object, string>();
            for (var i = 0; i < runner.StageCount; i++)
            {
                runner.SelectStage(i);
                var stage = runner.SelectedStage;
                originals[stage] = JsonUtility.ToJson(stage);
                foreach (var entry in stage.Entries)
                    originals[entry.Enemy] = JsonUtility.ToJson(entry.Enemy);
            }
            Assert.That(runner.StartSequence(), Is.True);
            Assert.That(runner.SelectedIndex, Is.Zero, "Must start at One even when Two was selected.");
            Assert.That(runner.GetComponent<StageFlowPanel>().enabled, Is.True);
            AssertBusy();
            yield return WaitFor(() =>
            {
                Assert.That(runner.SelectedIndex, Is.Zero);
                Assert.That(runner.Spawned, Is.EqualTo(runner.ActiveCount + runner.Arrived));
                return runner.State == RunState.Waiting;
            });
            var completedAt = Time.time;
            Assert.That(runner.State, Is.EqualTo(RunState.Waiting));
            Assert.That(runner.CurrentIndex, Is.EqualTo(1));
            Assert.That(runner.Arrived, Is.EqualTo(5));
            Assert.That(runner.ActiveCount, Is.Zero);
            AssertBusy();
            yield return WaitFor(() => runner.SelectedIndex == 1);
            var delay = Time.time - completedAt;
            Assert.That(delay, Is.InRange(1f - Time.deltaTime - 0.02f, 1f + Time.deltaTime + 0.1f));
            Assert.That(runner.State, Is.EqualTo(RunState.Running));
            Assert.That(runner.SelectedStage.DisplayName, Does.Contain("Stage Two"));
            yield return WaitFor(() => runner.State == RunState.Completed);
            Assert.That(runner.State, Is.EqualTo(RunState.Completed));
            Assert.That(runner.Arrived, Is.EqualTo(8));
            Assert.That(runner.ActiveCount, Is.Zero);
            Assert.That(runner.IsBusy, Is.False);
            Assert.That(runner.CurrentIndex, Is.Zero);
            foreach (var original in originals)
                Assert.That(JsonUtility.ToJson(original.Key), Is.EqualTo(original.Value));
            Assert.That(runner.StartSequence(), Is.True, "Restart after whole sequence completes.");
            runner.ResetRun();
        }

        [UnityTest]
        public IEnumerator ResetAtEverySequencePhaseCancelsAndAllowsBothKindsOfRestart()
        {
            for (var phase = 0; phase < 3; phase++)
            {
                Assert.That(runner.StartSequence(), Is.True);
                if (phase == 0) yield return WaitFor(() => runner.ActiveCount > 0);
                if (phase == 1) yield return WaitFor(() => runner.State == RunState.Waiting);
                if (phase == 2) yield return WaitFor(() => runner.SelectedIndex == 1 && runner.ActiveCount > 0);
                runner.ResetRun();
                Assert.That(runner.CurrentIndex, Is.Zero);
                Assert.That(runner.State, Is.EqualTo(RunState.Idle));
                Assert.That(runner.Spawned + runner.Arrived + runner.ActiveCount, Is.Zero);
                yield return null;
                Assert.That(runner.GetComponentsInChildren<EnemyMover>(true), Is.Empty);
                // Immediately restart a single run; a stale inter-stage timer must not replace it.
                Assert.That(runner.SelectStage(0), Is.True);
                Assert.That(runner.StartRun(), Is.True);
                yield return new WaitForSeconds(1.2f);
                Assert.That(runner.CurrentIndex, Is.Zero);
                Assert.That(runner.SelectedIndex, Is.Zero);
                yield return WaitFor(() => runner.State == RunState.Completed);
                Assert.That(runner.Arrived, Is.EqualTo(5));
                Assert.That(runner.StartRun(), Is.True, "Single stage can restart after completion.");
                runner.ResetRun();
                Assert.That(runner.StartSequence(), Is.True);
                yield return WaitFor(() => runner.State == RunState.Completed);
                Assert.That(runner.Arrived, Is.EqualTo(8));
                runner.ResetRun();
            }
        }

        [UnityTest]
        public IEnumerator DisablingDuringWaitClearsSequenceBeforeReenable()
        {
            Assert.That(runner.StartSequence(), Is.True);
            yield return WaitFor(() => runner.State == RunState.Waiting);
            runner.enabled = false;
            Assert.That(runner.CurrentIndex, Is.Zero);
            runner.enabled = true;
            yield return new WaitForSeconds(1.2f);
            Assert.That(runner.State, Is.EqualTo(RunState.Idle));
            Assert.That(runner.StartSequence(), Is.True);
            Assert.That(runner.SelectedIndex, Is.Zero);
        }
        private IEnumerator AssertPausedProgressIsFrozen(float seconds)
        {
            Assert.That(runner.State, Is.EqualTo(RunState.Paused));
            Assert.That(runner.CanPause, Is.False);
            Assert.That(runner.CanResume, Is.True);
            Assert.That(runner.Pause(), Is.False);
            AssertBusy();
            var spawned = runner.Spawned;
            var arrived = runner.Arrived;
            var index = runner.CurrentIndex;
            var stageIndex = runner.SelectedIndex;
            var movers = runner.GetComponentsInChildren<EnemyMover>();
            var positions = new Vector3[movers.Length];
            for (var i = 0; i < movers.Length; i++) positions[i] = movers[i].transform.position;
            var scale = Time.timeScale;
            var gameTime = Time.time;
            yield return new WaitForSecondsRealtime(seconds);
            Assert.That(Time.timeScale, Is.EqualTo(scale), "StageFlow must not change global timeScale.");
            Assert.That(Time.time, Is.GreaterThan(gameTime), "External game time must keep advancing.");
            Assert.That(runner.State, Is.EqualTo(RunState.Paused));
            Assert.That(runner.Spawned, Is.EqualTo(spawned));
            Assert.That(runner.Arrived, Is.EqualTo(arrived));
            Assert.That(runner.ActiveCount, Is.EqualTo(movers.Length));
            Assert.That(runner.CurrentIndex, Is.EqualTo(index));
            Assert.That(runner.SelectedIndex, Is.EqualTo(stageIndex));
            for (var i = 0; i < movers.Length; i++)
            {
                Assert.That(movers[i] != null, Is.True);
                Assert.That(movers[i].transform.position, Is.EqualTo(positions[i]));
            }
        }

        private void AssertInactiveControls()
        {
            Assert.That(runner.IsBusy, Is.False);
            Assert.That(runner.CanPause, Is.False);
            Assert.That(runner.CanResume, Is.False);
            Assert.That(runner.Pause(), Is.False);
            Assert.That(runner.Resume(), Is.False);
            Assert.That(runner.CurrentIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator EachSingleStagePausesSpawningAndMovementThenResumesInPlace()
        {
            AssertInactiveControls();
            for (var index = 0; index < 2; index++)
            {
                Assert.That(runner.SelectStage(index), Is.True);
                Assert.That(runner.StartRun(), Is.True);
                Assert.That(runner.CurrentIndex, Is.Zero, "Single Two is still run-list index zero.");
                // Pause immediately before the first Update: even the first spawn must be blocked.
                Assert.That(runner.Pause(), Is.True);
                yield return AssertPausedProgressIsFrozen(0.15f);
                Assert.That(runner.Spawned, Is.Zero);
                Assert.That(runner.Resume(), Is.True);
                yield return WaitFor(() => runner.ActiveCount > 0);
                // Consume part of the spawn interval before pausing.
                yield return new WaitForSeconds(0.2f);
                var mover = runner.GetComponentInChildren<EnemyMover>();
                var position = mover.transform.position;
                var spawned = runner.Spawned;
                var scale = Time.timeScale;
                Assert.That(runner.Pause(), Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(scale));
                yield return AssertPausedProgressIsFrozen(1.2f);
                Assert.That(runner.Resume(), Is.True);
                Assert.That(runner.Resume(), Is.False);
                Assert.That(runner.CanPause, Is.True);
                Assert.That(runner.CanResume, Is.False);
                Assert.That(mover.transform.position, Is.EqualTo(position));
                yield return new WaitForSeconds(0.1f);
                Assert.That(mover.transform.position, Is.Not.EqualTo(position));
                Assert.That(runner.Spawned, Is.EqualTo(spawned), "Spawn interval must resume its remainder.");
                yield return WaitFor(() => runner.State == RunState.Completed);
                Assert.That(runner.Arrived, Is.EqualTo(index == 0 ? 5 : 8));
                AssertInactiveControls();
            }
        }

        [UnityTest]
        public IEnumerator SequencePausesBothStagesAndOnlyConsumesUnpausedWaitTime()
        {
            var originals = new Dictionary<UnityEngine.Object, string>();
            for (var i = 0; i < runner.StageCount; i++)
            {
                runner.SelectStage(i);
                originals[runner.SelectedStage] = JsonUtility.ToJson(runner.SelectedStage);
                foreach (var entry in runner.SelectedStage.Entries)
                    originals[entry.Enemy] = JsonUtility.ToJson(entry.Enemy);
            }
            Assert.That(runner.StartSequence(), Is.True);
            yield return WaitFor(() => runner.ActiveCount > 0);
            Assert.That(runner.CurrentIndex, Is.Zero);
            Assert.That(runner.Pause(), Is.True);
            yield return AssertPausedProgressIsFrozen(1.2f);
            Assert.That(runner.Resume(), Is.True);
            yield return WaitFor(() => runner.State == RunState.Waiting);
            var waitStarted = Time.time;
            Assert.That(runner.CurrentIndex, Is.EqualTo(1));
            yield return new WaitForSeconds(0.3f);
            var consumed = Time.time - waitStarted;
            Assert.That(runner.Pause(), Is.True);
            yield return AssertPausedProgressIsFrozen(1.2f);
            var resumed = Time.time;
            Assert.That(runner.Resume(), Is.True);
            Assert.That(runner.State, Is.EqualTo(RunState.Waiting));
            Assert.That(runner.SelectedIndex, Is.Zero);
            yield return WaitFor(() => runner.State == RunState.Running);
            var remainder = Time.time - resumed;
            Assert.That(remainder, Is.InRange(1f - consumed - 0.08f, 1f - consumed + 0.08f));
            Assert.That(runner.SelectedIndex, Is.EqualTo(1));
            Assert.That(runner.CurrentIndex, Is.EqualTo(1));
            yield return WaitFor(() => runner.ActiveCount > 0);
            Assert.That(runner.Pause(), Is.True);
            yield return AssertPausedProgressIsFrozen(1.2f);
            Assert.That(runner.Resume(), Is.True);
            yield return WaitFor(() => runner.State == RunState.Completed);
            Assert.That(runner.Arrived, Is.EqualTo(8));
            AssertInactiveControls();
            foreach (var original in originals)
                Assert.That(JsonUtility.ToJson(original.Key), Is.EqualTo(original.Value));
        }

        [UnityTest]
        public IEnumerator ResetWhilePausedInEveryPhaseCancelsAndAllowsFreshRuns()
        {
            for (var phase = 0; phase < 3; phase++)
            {
                Assert.That(runner.StartSequence(), Is.True);
                if (phase == 0) yield return WaitFor(() => runner.ActiveCount > 0);
                if (phase == 1) yield return WaitFor(() => runner.State == RunState.Waiting);
                if (phase == 2) yield return WaitFor(() => runner.SelectedIndex == 1 && runner.ActiveCount > 0);
                Assert.That(runner.Pause(), Is.True);
                runner.ResetRun();
                Assert.That(runner.State, Is.EqualTo(RunState.Idle));
                AssertInactiveControls();
                Assert.That(runner.Spawned + runner.Arrived + runner.ActiveCount, Is.Zero);
                yield return null;
                Assert.That(runner.GetComponentsInChildren<EnemyMover>(true), Is.Empty);
                Assert.That(runner.SelectStage(0), Is.True);
                Assert.That(runner.StartRun(), Is.True);
                yield return WaitFor(() => runner.State == RunState.Completed);
                Assert.That(runner.Arrived, Is.EqualTo(5));
                Assert.That(runner.StartSequence(), Is.True);
                yield return WaitFor(() => runner.State == RunState.Completed);
                Assert.That(runner.Arrived, Is.EqualTo(8));
                AssertInactiveControls();
            }
        }

        [UnityTest]
        public IEnumerator PauseIsLocalAndDisableClearsPausedRun()
        {
            var outside = new GameObject("Independent mover", typeof(EnemyMover));
            try
            {
                outside.GetComponent<EnemyMover>().Initialize(new Vector3(100, 0, 0), 1f, _ => { }, () => Time.deltaTime);
                Assert.That(runner.StartRun(), Is.True);
                yield return WaitFor(() => runner.ActiveCount > 0);
                Assert.That(runner.Pause(), Is.True);
                var position = outside.transform.position;
                yield return AssertPausedProgressIsFrozen(0.2f);
                Assert.That(outside.transform.position.x, Is.GreaterThan(position.x));
                runner.enabled = false;
                Assert.That(runner.State, Is.EqualTo(RunState.Idle));
                AssertInactiveControls();
                runner.enabled = true;
                Assert.That(runner.StartRun(), Is.True);
                yield return WaitFor(() => runner.ActiveCount > 0);
                var fresh = runner.GetComponentInChildren<EnemyMover>();
                position = fresh.transform.position;
                yield return new WaitForSeconds(0.2f);
                Assert.That(fresh.transform.position, Is.Not.EqualTo(position));
            }
            finally { UnityEngine.Object.Destroy(outside); }
        }
        [UnityTest]
        public IEnumerator ZeroDeltaTimeDefersArrivalEvenAtDestination()
        {
            var instance = new GameObject("Injected-time mover", typeof(EnemyMover));
            try
            {
                var deltaTime = 0f;
                var arrivals = 0;
                instance.GetComponent<EnemyMover>().Initialize(instance.transform.position, 1f,
                    _ => arrivals++, () => deltaTime);
                yield return null;
                yield return null;
                Assert.That(arrivals, Is.Zero, "Zero time must stop arrival logic, not only movement.");
                deltaTime = 0.1f;
                yield return null;
                yield return null;
                Assert.That(arrivals, Is.EqualTo(1));
                yield return null;
                Assert.That(arrivals, Is.EqualTo(1), "Arrival must remain a one-shot callback.");
            }
            finally { UnityEngine.Object.Destroy(instance); }
        }

        [UnityTest]
        public IEnumerator NormalScenarioCompletesWavesInOrderAcrossMultipleRoutes()
        {
            var originalScale = Time.timeScale;
            try
            {
                Assert.That(runner.SelectStage(2), Is.True);
                var stage = runner.SelectedStage;
                var originals = SnapshotRuntimeData(stage);
                Time.timeScale = 8f;
                Assert.That(runner.StartRun(), Is.True);
                Assert.That(runner.TotalWaves, Is.EqualTo(4));
                Assert.That(runner.TotalGroups, Is.EqualTo(12));
                var highestWave = 1;
                var sawMultipleRoutes = false;
                var deadline = Time.realtimeSinceStartup + 30f;
                while (runner.State != RunState.Completed && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(runner.CurrentWave, Is.GreaterThanOrEqualTo(highestWave));
                    highestWave = runner.CurrentWave;
                    var upper = false;
                    var lower = false;
                    foreach (var mover in runner.GetComponentsInChildren<EnemyMover>())
                    {
                        upper |= mover.transform.position.z < -0.8f;
                        lower |= mover.transform.position.z > 0.8f;
                    }
                    sawMultipleRoutes |= upper && lower;
                    yield return null;
                }
                Assert.That(runner.State, Is.EqualTo(RunState.Completed));
                Assert.That(highestWave, Is.EqualTo(4));
                Assert.That(runner.CompletedGroups, Is.EqualTo(12));
                Assert.That(runner.Spawned, Is.EqualTo(150));
                Assert.That(runner.Arrived, Is.EqualTo(150));
                Assert.That(runner.ActiveCount, Is.Zero);
                Assert.That(sawMultipleRoutes, Is.True);
                AssertRuntimeDataUnchanged(originals);
            }
            finally
            {
                Time.timeScale = originalScale;
                runner.ResetRun();
            }
        }

        [UnityTest]
        public IEnumerator StressResetRestartCompletionAndCompletedRestartAreClean()
        {
            var originalScale = Time.timeScale;
            try
            {
                Assert.That(runner.SelectStage(3), Is.True);
                var originals = SnapshotRuntimeData(runner.SelectedStage);
                Time.timeScale = 12f;
                Assert.That(runner.Planned, Is.EqualTo(60000));
                Assert.That(runner.StartRun(), Is.True);
                yield return WaitFor(() => runner.ActiveCount >= 100, 15f);
                runner.ResetRun();
                yield return null;
                Assert.That(runner.State, Is.EqualTo(RunState.Idle));
                Assert.That(runner.Spawned + runner.Arrived + runner.ActiveCount, Is.Zero);
                Assert.That(runner.GetComponentsInChildren<EnemyMover>(true), Is.Empty);

                Assert.That(runner.StartRun(), Is.True);
                yield return WaitFor(() => runner.State == RunState.Completed, 45f);
                Assert.That(runner.Spawned, Is.EqualTo(60000));
                Assert.That(runner.Arrived, Is.EqualTo(60000));
                Assert.That(runner.ActiveCount, Is.Zero);
                Assert.That(runner.CompletedGroups, Is.EqualTo(60));
                AssertRuntimeDataUnchanged(originals);

                Assert.That(runner.StartRun(), Is.True, "A completed stress scenario must start again.");
                yield return WaitFor(() => runner.ActiveCount >= 100, 15f);
                runner.ResetRun();
                yield return null;
                Assert.That(runner.GetComponentsInChildren<EnemyMover>(true), Is.Empty);
                AssertRuntimeDataUnchanged(originals);
            }
            finally
            {
                Time.timeScale = originalScale;
                runner.ResetRun();
            }
        }

        [UnityTest]
        public IEnumerator EnemyTraversesEveryRouteWaypointBeforeCompleting()
        {
            var instance = new GameObject("Route mover", typeof(EnemyMover));
            try
            {
                var arrived = false;
                var route = new[] { Vector3.zero, Vector3.right, Vector3.one, new Vector3(2f, 1f, 0f) };
                instance.GetComponent<EnemyMover>().Initialize(route, 6f, _ => arrived = true, () => Time.deltaTime);
                yield return WaitFor(() => instance.transform.position.x >= 1f && instance.transform.position.y > 0f, 5f);
                yield return WaitFor(() => arrived, 5f);
                Assert.That(Vector3.Distance(instance.transform.position, route[route.Length - 1]), Is.LessThan(0.001f));
            }
            finally { UnityEngine.Object.Destroy(instance); }
        }

        private static Dictionary<UnityEngine.Object, string> SnapshotRuntimeData(StageDefinition stage)
        {
            var result = new Dictionary<UnityEngine.Object, string> { [stage] = JsonUtility.ToJson(stage) };
            foreach (var wave in stage.Waves)
                foreach (var group in wave.Groups)
                {
                    result[group.Enemy] = JsonUtility.ToJson(group.Enemy);
                    result[group.Route] = JsonUtility.ToJson(group.Route);
                }
            return result;
        }

        private static void AssertRuntimeDataUnchanged(Dictionary<UnityEngine.Object, string> originals)
        {
            foreach (var original in originals)
                Assert.That(JsonUtility.ToJson(original.Key), Is.EqualTo(original.Value));
        }
#endif
    }
}
