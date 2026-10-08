using System;

namespace StageFlow
{
    public sealed class WaveRuntime
    {
        private readonly SpawnGroupRuntime[] groups;

        public int CompletedGroups { get; private set; }
        public int TotalGroups => groups.Length;
        public bool IsSpawningComplete => CompletedGroups == groups.Length;

        public WaveRuntime(StagePlan.Wave wave)
        {
            if (wave == null) throw new ArgumentNullException(nameof(wave));
            groups = new SpawnGroupRuntime[wave.Groups.Count];
            for (var i = 0; i < groups.Length; i++) groups[i] = new SpawnGroupRuntime(wave.Groups[i]);
        }

        public void Tick(float deltaTime, Action<StagePlan.Group> spawn)
        {
            CompletedGroups = 0;
            foreach (var group in groups)
            {
                group.Tick(deltaTime, spawn);
                if (group.IsComplete) CompletedGroups++;
            }
        }
    }
}
