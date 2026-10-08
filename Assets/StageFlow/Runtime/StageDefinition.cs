using System;
using UnityEngine;

namespace StageFlow
{
    [Serializable]
    public sealed class SpawnEntry
    {
        public EnemyDefinition Enemy;
        [Min(1)] public int Count = 1;
    }

    [Serializable]
    public sealed class SpawnGroupDefinition
    {
        public string DisplayName;
        public EnemyDefinition Enemy;
        [Min(1)] public int Count = 1;
        [Min(0f)] public float StartDelay;
        [Min(0f)] public float SpawnInterval = 0.6f;
        public RouteDefinition Route;
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        public string DisplayName;
        public SpawnGroupDefinition[] Groups = Array.Empty<SpawnGroupDefinition>();
    }

    [CreateAssetMenu(menuName = "StageFlow/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public SpawnEntry[] Entries = Array.Empty<SpawnEntry>();
        [Min(0f)] public float SpawnInterval = 0.6f;
        public WaveDefinition[] Waves = Array.Empty<WaveDefinition>();
    }
}
