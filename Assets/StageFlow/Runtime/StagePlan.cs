using System;
using System.Collections.Generic;
using UnityEngine;

namespace StageFlow
{
    // Immutable run input copied from authored assets. Runtime counters never live in ScriptableObjects.
    public sealed class StagePlan
    {
        public sealed class Group
        {
            public string DisplayName { get; }
            public GameObject Prefab { get; }
            public float Speed { get; }
            public int Count { get; }
            public float StartDelay { get; }
            public float SpawnInterval { get; }
            public string RouteName { get; }
            public IReadOnlyList<Vector3> Waypoints { get; }

            internal Group(string displayName, GameObject prefab, float speed, int count,
                float startDelay, float spawnInterval, string routeName, Vector3[] waypoints)
            {
                DisplayName = displayName;
                Prefab = prefab;
                Speed = speed;
                Count = count;
                StartDelay = startDelay;
                SpawnInterval = spawnInterval;
                RouteName = routeName;
                Waypoints = Array.AsReadOnly(waypoints);
            }
        }

        public sealed class Wave
        {
            public string DisplayName { get; }
            public IReadOnlyList<Group> Groups { get; }
            public int Total { get; }

            internal Wave(string displayName, List<Group> groups, int total)
            {
                DisplayName = displayName;
                Groups = groups.AsReadOnly();
                Total = total;
            }
        }

        public IReadOnlyList<Wave> Waves { get; }
        public IReadOnlyList<Group> Items { get; }
        public int Total { get; }
        public int TotalGroups { get; }

        private StagePlan(List<Wave> waves, List<Group> groups, int total)
        {
            Waves = waves.AsReadOnly();
            Items = groups.AsReadOnly();
            Total = total;
            TotalGroups = groups.Count;
        }

        public static bool TryCreate(StageDefinition stage, out StagePlan plan, out string error)
        {
            plan = null;
            error = "";
            if (stage == null || string.IsNullOrWhiteSpace(stage.Id) || string.IsNullOrWhiteSpace(stage.DisplayName))
            {
                error = "Select a stage with an ID and display name.";
                return false;
            }

            var waves = new List<Wave>();
            var groups = new List<Group>();
            long total = 0;
            if (stage.Waves != null && stage.Waves.Length > 0)
            {
                foreach (var sourceWave in stage.Waves)
                {
                    if (sourceWave?.Groups == null || sourceWave.Groups.Length == 0)
                    {
                        error = "Each wave needs at least one spawn group.";
                        return false;
                    }

                    var waveGroups = new List<Group>();
                    long waveTotal = 0;
                    foreach (var sourceGroup in sourceWave.Groups)
                    {
                        if (!TryCreateGroup(sourceGroup, out var group, out error)) return false;
                        if (!AddCount(group.Count, ref total) || !AddCount(group.Count, ref waveTotal))
                        {
                            error = "The stage total exceeds the supported count.";
                            return false;
                        }
                        waveGroups.Add(group);
                        groups.Add(group);
                    }
                    waves.Add(new Wave(sourceWave.DisplayName, waveGroups, (int)waveTotal));
                }
            }
            else
            {
                if (!Finite(stage.SpawnInterval) || stage.SpawnInterval < 0f)
                {
                    error = "Spawn interval must be finite and non-negative.";
                    return false;
                }
                if (stage.Entries == null || stage.Entries.Length == 0)
                {
                    error = "The stage needs at least one wave or legacy spawn entry.";
                    return false;
                }

                var waveGroups = new List<Group>();
                var delay = 0f;
                foreach (var entry in stage.Entries)
                {
                    var source = new SpawnGroupDefinition
                    {
                        DisplayName = entry?.Enemy == null ? "Legacy Group" : entry.Enemy.DisplayName,
                        Enemy = entry?.Enemy,
                        Count = entry?.Count ?? 0,
                        StartDelay = delay,
                        SpawnInterval = stage.SpawnInterval
                    };
                    if (!TryCreateGroup(source, out var group, out error, false)) return false;
                    if (!AddCount(group.Count, ref total))
                    {
                        error = "The stage total exceeds the supported count.";
                        return false;
                    }
                    waveGroups.Add(group);
                    groups.Add(group);
                    delay += group.Count * stage.SpawnInterval;
                }
                waves.Add(new Wave("Stage", waveGroups, (int)total));
            }

            plan = new StagePlan(waves, groups, (int)total);
            return true;
        }

        private static bool TryCreateGroup(SpawnGroupDefinition source, out Group group, out string error,
            bool requireRoute = true)
        {
            group = null;
            error = "";
            var enemy = source?.Enemy;
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.Id) ||
                string.IsNullOrWhiteSpace(enemy.DisplayName) || enemy.Prefab == null ||
                !enemy.Prefab.activeSelf || enemy.Prefab.GetComponent<EnemyMover>() == null ||
                !enemy.Prefab.GetComponent<EnemyMover>().enabled || !Finite(enemy.MoveSpeed) ||
                enemy.MoveSpeed <= 0f || source.Count <= 0)
            {
                error = "Each group needs a valid enemy, active mover prefab, positive speed and count.";
                return false;
            }
            if (!Finite(source.StartDelay) || source.StartDelay < 0f ||
                !Finite(source.SpawnInterval) || source.SpawnInterval < 0f)
            {
                error = "Group delays and intervals must be finite and non-negative.";
                return false;
            }

            var route = source.Route;
            var points = Array.Empty<Vector3>();
            var routeName = "Scene Direct Route";
            if (route != null)
            {
                if (string.IsNullOrWhiteSpace(route.Id) || string.IsNullOrWhiteSpace(route.DisplayName) ||
                    route.Waypoints == null || route.Waypoints.Length < 2)
                {
                    error = "Each route needs an ID, display name and at least two waypoints.";
                    return false;
                }
                points = (Vector3[])route.Waypoints.Clone();
                foreach (var point in points)
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z))
                    {
                        error = "Route waypoints must be finite.";
                        return false;
                    }
                routeName = route.DisplayName;
            }
            else if (requireRoute)
            {
                error = "Each authored spawn group needs a route.";
                return false;
            }

            group = new Group(source.DisplayName, enemy.Prefab, enemy.MoveSpeed, source.Count,
                source.StartDelay, source.SpawnInterval, routeName, points);
            return true;
        }

        private static bool AddCount(int count, ref long total)
        {
            total += count;
            return total <= int.MaxValue;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
