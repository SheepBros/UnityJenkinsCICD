using System;
using UnityEngine;

namespace StageFlow
{
    public sealed class EnemyMover : MonoBehaviour
    {
        private Vector3[] waypoints;
        private int waypointIndex;
        private float speed;
        private Func<float> getDeltaTime;
        private Action<EnemyMover> onArrival;

        public void Initialize(Vector3 target, float moveSpeed, Action<EnemyMover> callback,
            Func<float> deltaTime)
        {
            Initialize(new[] { transform.position, target }, moveSpeed, callback, deltaTime);
        }

        public void Initialize(Vector3[] route, float moveSpeed, Action<EnemyMover> callback,
            Func<float> deltaTime)
        {
            if (route == null || route.Length < 2) throw new ArgumentException("A route needs at least two waypoints.", nameof(route));
            waypoints = (Vector3[])route.Clone();
            speed = moveSpeed;
            onArrival = callback ?? throw new ArgumentNullException(nameof(callback));
            getDeltaTime = deltaTime ?? throw new ArgumentNullException(nameof(deltaTime));
            waypointIndex = 1;
            transform.position = waypoints[0];
        }

        public void Cancel()
        {
            onArrival = null;
            getDeltaTime = null;
            waypoints = null;
        }

        private void Update()
        {
            if (onArrival == null) return;
            var deltaTime = getDeltaTime();
            if (deltaTime <= 0f) return;

            var distance = speed * deltaTime;
            while (distance > 0f && waypointIndex < waypoints.Length)
            {
                var before = transform.position;
                var target = waypoints[waypointIndex];
                transform.position = Vector3.MoveTowards(before, target, distance);
                distance -= Vector3.Distance(before, transform.position);
                if ((transform.position - target).sqrMagnitude <= 0.0001f)
                {
                    transform.position = target;
                    waypointIndex++;
                }
                else break;
            }

            if (waypointIndex < waypoints.Length) return;
            var callback = onArrival;
            Cancel();
            callback(this);
        }

        private void OnDisable()
        {
            if (!gameObject.activeSelf) Cancel();
        }
    }
}
