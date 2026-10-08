using UnityEngine;

namespace StageFlow
{
    [CreateAssetMenu(menuName = "StageFlow/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [Min(0.01f)] public float MoveSpeed = 2f;
        public GameObject Prefab;
    }
}
