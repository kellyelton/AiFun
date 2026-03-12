using AiFun;

namespace AiFun.Tests;

public class MutationRateTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Ecosystem_has_MutationRate_property()
    {
        var eco = CreateEcosystem();
        // Should have a default mutation rate
        Assert.True(eco.MutationRate > 0, "MutationRate should have a positive default");
        Assert.True(eco.MutationRate <= 0.10, "MutationRate default should not exceed 10%");
    }

    [Fact]
    public void Ecosystem_MutationRate_defaults_to_0_001()
    {
        var eco = CreateEcosystem();
        Assert.Equal(0.001, eco.MutationRate);
    }

    [Fact]
    public void Ecosystem_MutationRate_can_be_set()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 0.05;
        Assert.Equal(0.05, eco.MutationRate);
    }

    [Fact]
    public void Ecosystem_MutationRate_clamped_to_minimum_zero()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = -0.1;
        Assert.True(eco.MutationRate >= 0, "MutationRate should not go below 0");
    }

    [Fact]
    public void Low_mutation_rate_preserves_parent_brain_weights()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 0.001; // 0.1% mutation

        var parent1 = new Animal(eco);
        var parent2 = new Animal(eco);

        // Create many children and check that network weights mostly match parent values
        int matchCount = 0;
        int totalWeights = 0;

        for (int trial = 0; trial < 10; trial++)
        {
            var child = new Animal(eco, parent1, parent2);
            var childWeights = child.Brain.GetFNData().ToArray();
            var p1Weights = parent1.Brain.GetFNData().ToArray();
            var p2Weights = parent2.Brain.GetFNData().ToArray();

            foreach (var cw in childWeights)
            {
                var w1 = p1Weights.FirstOrDefault(x => x.Equals(cw));
                var w2 = p2Weights.FirstOrDefault(x => x.Equals(cw));

                if (w1 == null && w2 == null) continue; // topology mismatch, skip
                totalWeights++;

                bool matchesParent = false;
                if (w1 != null && Math.Abs(cw.Weight - w1.Weight) < 0.0001) matchesParent = true;
                if (w2 != null && Math.Abs(cw.Weight - w2.Weight) < 0.0001) matchesParent = true;
                if (matchesParent) matchCount++;
            }
        }

        // With 0.1% mutation, >95% of weights should match a parent
        double matchRate = (double)matchCount / totalWeights;
        Assert.True(matchRate > 0.95,
            $"With 0.1% mutation rate, expected >95% parent-matching weights, got {matchRate:P1} ({matchCount}/{totalWeights})");
    }
}
