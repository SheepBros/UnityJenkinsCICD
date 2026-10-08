# StageFlow Runtime Challenge Baseline

## Run the sample

1. Open `Assets/StageFlow/Scenes/StageFlowDemo.unity`.
2. Enter Play Mode.
3. Select Stage One, Stage Two, Stage Runtime Normal, or Stage Runtime Stress.
4. Select **Start**. Use **Reset** at any point to cancel the current run.

**Start Sequence (One > Two)** remains the regression path for the two original sample stages. Normal and Stress are independent scenarios selected with their own buttons.

The panel reports the selected scenario, stage state, current and total waves, completed and total spawn groups, and planned, spawned, active, and arrived enemy counts. Pause and Resume remain local to StageFlow and do not change the global time scale.

## Runtime structure

- `StageDefinition` owns authored Waves. Stage One and Stage Two retain their original Entries and are adapted to a single runtime Wave.
- Each `WaveDefinition` owns two or more `SpawnGroupDefinition` values. Groups advance independently from their start delay, so several groups can be active together.
- Each Spawn Group selects an enemy archetype, count, start delay, spawn interval, and route.
- `RouteDefinition` contains an authored waypoint sequence. `EnemyMover` traverses every waypoint before reporting arrival.
- `StagePlan` validates and snapshots authored data before a run. `WaveRuntime` and `SpawnGroupRuntime` own per-run progression.
- `StageRunner` owns stage and wave progression, active instances, counters, Reset, restart, and the original One-to-Two sequence.

A Wave advances only after all its groups have finished spawning and every enemy from that Wave has arrived. The final Wave transitions the Stage to Completed. Reset cancels the run, removes active enemies, clears runtime state, and leaves the selected source assets unchanged.

## Scenarios

| Scenario | Waves | Groups | Enemies | Archetypes | Routes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Stage One | 1 | 2 | 5 | 2 | Scene direct |
| Stage Two | 1 | 2 | 8 | 2 | Scene direct |
| Stage Runtime Normal | 4 | 12 | 150 | 3 | 3 |
| Stage Runtime Stress | 10 | 60 | 2,400 | 3 | 3 |

Normal is the readable Game View demonstration. Stress uses the same runtime with six overlapping groups per Wave and a larger authored workload spread across all archetypes and routes.

## Functional verification

EditMode tests validate plans, sample sizes, source snapshots, authored route validation, and overlapping group timing. PlayMode tests cover Stage One and Two, Normal and Stress completion, Wave ordering, route completion, Reset during a run, restart after Reset, restart after Completed, repeated Run/Reset, active-enemy cleanup, pause/resume behavior, and source asset integrity.
