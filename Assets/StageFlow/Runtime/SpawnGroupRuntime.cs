using System;

namespace StageFlow
{
    public sealed class SpawnGroupRuntime
    {
        private readonly StagePlan.Group group;
        private float elapsed;

        public int Spawned { get; private set; }
        public bool IsComplete => Spawned == group.Count;

        public SpawnGroupRuntime(StagePlan.Group group)
        {
            this.group = group ?? throw new ArgumentNullException(nameof(group));
        }

        public void Tick(float deltaTime, Action<StagePlan.Group> spawn)
        {
            if (IsComplete || deltaTime <= 0f) return;
            elapsed += deltaTime;
            if (elapsed < group.StartDelay) return;

            var groupTime = elapsed - group.StartDelay;
            while (!IsComplete && groupTime >= Spawned * group.SpawnInterval)
            {
                spawn(group);
                Spawned++;
            }
        }
    }
}
