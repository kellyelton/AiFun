using AiFun;

namespace AiFun.Tests;

public class TopologyCrossoverTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Child_with_mismatched_parent_topology_inherits_weights_not_random()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 0.0; // Zero mutation so we can isolate topology behavior

        // Create many children from parents with potentially different hidden neuron counts.
        // When a weight exists in only one parent, the child should inherit that weight
        // exactly (not randomize it).
        int inheritedCount = 0;
        int totalMismatchedWeights = 0;

        for (int trial = 0; trial < 50; trial++)
        {
            var parent1 = new Animal(eco);
            var parent2 = new Animal(eco);

            // Skip trials where parents have same topology (no mismatch to test)
            if (parent1.HiddenNeurons == parent2.HiddenNeurons) continue;

            var child = new Animal(eco, parent1, parent2);
            var childWeights = child.Brain.GetFNData().ToArray();
            var p1Weights = parent1.Brain.GetFNData().ToArray();
            var p2Weights = parent2.Brain.GetFNData().ToArray();

            foreach (var cw in childWeights)
            {
                var w1 = p1Weights.FirstOrDefault(x => x.Equals(cw));
                var w2 = p2Weights.FirstOrDefault(x => x.Equals(cw));

                // Only care about weights where exactly one parent has it
                if ((w1 == null) != (w2 == null))
                {
                    totalMismatchedWeights++;
                    var parentWeight = w1 ?? w2;
                    if (Math.Abs(cw.Weight - parentWeight!.Weight) < 0.0001)
                        inheritedCount++;
                }
            }
        }

        // If we got mismatched cases, all should be inherited (not random)
        if (totalMismatchedWeights > 0)
        {
            double inheritRate = (double)inheritedCount / totalMismatchedWeights;
            Assert.True(inheritRate > 0.95,
                $"Expected >95% of mismatched weights to be inherited from the parent that has them, " +
                $"got {inheritRate:P1} ({inheritedCount}/{totalMismatchedWeights})");
        }
    }

    [Fact]
    public void Child_never_gets_random_weight_when_one_parent_has_it()
    {
        // Even with zero mutation, the old code randomized weights when both parents
        // were null (neither had the weight position). This test ensures that when
        // at least one parent has the weight, the child inherits it.
        var eco = CreateEcosystem();
        eco.MutationRate = 0.0;

        bool foundMismatch = false;
        for (int trial = 0; trial < 100; trial++)
        {
            var p1 = new Animal(eco);
            var p2 = new Animal(eco);
            if (p1.HiddenNeurons == p2.HiddenNeurons) continue;

            foundMismatch = true;
            var child = new Animal(eco, p1, p2);
            var childWeights = child.Brain.GetFNData().ToArray();
            var p1w = p1.Brain.GetFNData().ToArray();
            var p2w = p2.Brain.GetFNData().ToArray();

            foreach (var cw in childWeights)
            {
                var w1 = p1w.FirstOrDefault(x => x.Equals(cw));
                var w2 = p2w.FirstOrDefault(x => x.Equals(cw));

                if (w1 != null && w2 == null)
                {
                    Assert.Equal(w1.Weight, cw.Weight);
                }
                else if (w2 != null && w1 == null)
                {
                    Assert.Equal(w2.Weight, cw.Weight);
                }
            }
        }
        // Ensure we actually tested something
        Assert.True(foundMismatch, "No topology mismatches found in 100 trials - test inconclusive");
    }
}
