using NUnit.Framework;
using StageFlow.Editor;
using UnityEditor;

namespace StageFlow.Tests
{
    public sealed class SampleAssetTests
    {
        [Test]
        public void SampleStagesHaveValidPlansAndExpectedSizes()
        {
            foreach (var sample in new[]
                     {
                         ("StageOne", 5),
                         ("StageTwo", 8),
                         ("StageRuntimeNormal", 150),
                         ("StageRuntimeStress", 60000)
                     })
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(SampleProjectGenerator.Root + "/Data/" + sample.Item1 + ".asset");
                Assert.That(StagePlan.TryCreate(stage, out var plan, out var error), Is.True, error);
                Assert.That(plan.Total, Is.EqualTo(sample.Item2));
                Assert.That(plan.Items[0].Prefab, Is.Not.EqualTo(plan.Items[1].Prefab));
            }
        }

        [Test]
        public void RuntimeScenariosUseThreeArchetypesRoutesAndExpectedWaveShapes()
        {
            var normal = AssetDatabase.LoadAssetAtPath<StageDefinition>(
                SampleProjectGenerator.Root + "/Data/StageRuntimeNormal.asset");
            var stress = AssetDatabase.LoadAssetAtPath<StageDefinition>(
                SampleProjectGenerator.Root + "/Data/StageRuntimeStress.asset");
            Assert.That(StagePlan.TryCreate(normal, out var normalPlan, out var normalError), Is.True, normalError);
            Assert.That(StagePlan.TryCreate(stress, out var stressPlan, out var stressError), Is.True, stressError);
            Assert.That(normalPlan.Waves.Count, Is.EqualTo(4));
            Assert.That(normalPlan.TotalGroups, Is.EqualTo(12));
            Assert.That(stressPlan.Waves.Count, Is.EqualTo(10));
            Assert.That(stressPlan.TotalGroups, Is.EqualTo(60));

            var prefabs = new System.Collections.Generic.HashSet<UnityEngine.GameObject>();
            var routes = new System.Collections.Generic.HashSet<string>();
            foreach (var group in stressPlan.Items)
            {
                prefabs.Add(group.Prefab);
                routes.Add(group.RouteName);
            }
            Assert.That(prefabs, Has.Count.EqualTo(3));
            Assert.That(routes, Has.Count.EqualTo(3));
        }
    }
}
