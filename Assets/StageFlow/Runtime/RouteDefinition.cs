using System;
using UnityEngine;

namespace StageFlow
{
    [CreateAssetMenu(menuName = "StageFlow/Route Definition")]
    public sealed class RouteDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public Vector3[] Waypoints = Array.Empty<Vector3>();
    }
}
