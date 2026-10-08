using NUnit.Framework;
using UnityEngine;

namespace StageFlow.Tests
{
    public sealed class StagePlanTests
    {
        private GameObject prefab;
        private EnemyDefinition enemy;
        private StageDefinition stage;

        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("Test Enemy", typeof(EnemyMover));
            enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.Id = "enemy"; enemy.DisplayName = "Enemy"; enemy.Prefab = prefab; enemy.MoveSpeed = 2f;
            stage = ScriptableObject.CreateInstance<StageDefinition>();
            stage.Id = "test"; stage.DisplayName = "Test"; stage.SpawnInterval = 0.25f;
            stage.Entries = new[] { new SpawnEntry { Enemy = enemy, Count = 3 }, new SpawnEntry { Enemy = enemy, Count = 2 } };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(stage);
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void PlanPreservesOrderCountsAndSnapshotWithoutChangingSource()
        {
            Assert.That(StagePlan.TryCreate(stage, out var plan, out var error), Is.True, error);
            Assert.That(plan.Total, Is.EqualTo(5));
            Assert.That(plan.Items[0].Count, Is.EqualTo(3));
            Assert.That(plan.Items[1].Count, Is.EqualTo(2));
            Assert.That(stage.Entries[0].Count, Is.EqualTo(3));
            stage.Entries[0].Count = 7;
            enemy.MoveSpeed = 9f;
            stage.SpawnInterval = 4f;
            Assert.That(plan.Total, Is.EqualTo(5));
            Assert.That(plan.Items[0].Speed, Is.EqualTo(2f));
            Assert.That(plan.Waves.Count, Is.EqualTo(1));
            Assert.That(plan.Items[0].SpawnInterval, Is.EqualTo(0.25f));
        }

        [Test]
        public void MissingPrefabRejectsWholePlan()
        {
            enemy.Prefab = null;
            Assert.That(StagePlan.TryCreate(stage, out var plan, out var error), Is.False);
            Assert.That(plan, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveCountIsRejected(int count)
        {
            stage.Entries[1].Count = count;
            Assert.That(StagePlan.TryCreate(stage, out _, out _), Is.False);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidIntervalIsRejected(float interval)
        {
            stage.SpawnInterval = interval;
            Assert.That(StagePlan.TryCreate(stage, out _, out _), Is.False);
        }

        [Test]
        public void TotalOverflowIsRejectedWithoutAllocatingEverySpawn()
        {
            stage.Entries[0].Count = int.MaxValue;
            Assert.That(StagePlan.TryCreate(stage, out _, out _), Is.False);
        }

        [Test]
        public void AuthoredWavesSnapshotGroupsRoutesAndSourceValues()
        {
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.Id = "route";
            route.DisplayName = "Route";
            route.Waypoints = new[] { Vector3.zero, Vector3.right, Vector3.one };
            stage.Entries = System.Array.Empty<SpawnEntry>();
            stage.Waves = new[]
            {
                new WaveDefinition
                {
                    DisplayName = "Wave 1",
                    Groups = new[]
                    {
                        new SpawnGroupDefinition
                        {
                            DisplayName = "A", Enemy = enemy, Count = 3, StartDelay = 0f,
                            SpawnInterval = 0.2f, Route = route
                        },
                        new SpawnGroupDefinition
                        {
                            DisplayName = "B", Enemy = enemy, Count = 2, StartDelay = 0.1f,
                            SpawnInterval = 0.3f, Route = route
                        }
                    }
                }
            };

            Assert.That(StagePlan.TryCreate(stage, out var plan, out var error), Is.True, error);
            Assert.That(plan.Waves.Count, Is.EqualTo(1));
            Assert.That(plan.TotalGroups, Is.EqualTo(2));
            Assert.That(plan.Total, Is.EqualTo(5));
            Assert.That(plan.Items[1].StartDelay, Is.EqualTo(0.1f));
            Assert.That(plan.Items[0].Waypoints, Is.EqualTo(route.Waypoints));

            route.Waypoints[1] = Vector3.up;
            stage.Waves[0].Groups[0].Count = 99;
            Assert.That(plan.Items[0].Waypoints[1], Is.EqualTo(Vector3.right));
            Assert.That(plan.Items[0].Count, Is.EqualTo(3));
            Object.DestroyImmediate(route);
        }

        [Test]
        public void ConcurrentGroupRuntimeHonorsIndependentStartDelays()
        {
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.Id = "route"; route.DisplayName = "Route";
            route.Waypoints = new[] { Vector3.zero, Vector3.right };
            stage.Entries = System.Array.Empty<SpawnEntry>();
            stage.Waves = new[]
            {
                new WaveDefinition
                {
                    Groups = new[]
                    {
                        new SpawnGroupDefinition { Enemy = enemy, Count = 3, SpawnInterval = 1f, Route = route },
                        new SpawnGroupDefinition { Enemy = enemy, Count = 2, StartDelay = 0.5f, SpawnInterval = 1f, Route = route }
                    }
                }
            };
            Assert.That(StagePlan.TryCreate(stage, out var plan, out var error), Is.True, error);
            var runtime = new WaveRuntime(plan.Waves[0]);
            var spawned = new System.Collections.Generic.List<StagePlan.Group>();
            runtime.Tick(0.1f, spawned.Add);
            Assert.That(spawned, Has.Count.EqualTo(1));
            runtime.Tick(0.4f, spawned.Add);
            Assert.That(spawned, Has.Count.EqualTo(2));
            Assert.That(spawned[0], Is.Not.SameAs(spawned[1]));
            Assert.That(runtime.CompletedGroups, Is.Zero);
            Object.DestroyImmediate(route);
        }
    }
}
