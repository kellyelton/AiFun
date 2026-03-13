using AiFun;

namespace AiFun.Tests;

public class TopologyCrossoverTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void All_children_inherit_weights_from_parents_with_fixed_topology()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 0.0; // Zero mutation so we can isolate crossover behavior

        // With fixed topology, all animals have identical network structure.
        // Every weight in the child should come from one of its parents.
        for (int trial = 0; trial < 50; trial++)
        {
            var parent1 = new Animal(eco);
            var parent2 = new Animal(eco);
            var child = new Animal(eco, parent1, parent2);

            var childWeights = child.Brain.GetFNData().ToArray();
            var p1Weights = parent1.Brain.GetFNData().ToArray();
            var p2Weights = parent2.Brain.GetFNData().ToArray();

            foreach (var cw in childWeights)
            {
                var w1 = p1Weights.FirstOrDefault(x => x.Equals(cw));
                var w2 = p2Weights.FirstOrDefault(x => x.Equals(cw));

                // Both parents should always have the weight (identical topology)
                Assert.NotNull(w1);
                Assert.NotNull(w2);

                // Child weight should be from one of the parents
                Assert.True(
                    Math.Abs(cw.Weight - w1.Weight) < 0.0001 ||
                    Math.Abs(cw.Weight - w2.Weight) < 0.0001,
                    $"Child weight {cw.Weight} doesn't match p1={w1.Weight} or p2={w2.Weight}");
            }
        }
    }

    [Fact]
    public void Child_weight_count_matches_parents()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 0.0;

        for (int trial = 0; trial < 20; trial++)
        {
            var p1 = new Animal(eco);
            var p2 = new Animal(eco);
            var child = new Animal(eco, p1, p2);

            Assert.Equal(p1.Brain.GetFNData().Count(), child.Brain.GetFNData().Count());
            Assert.Equal(p2.Brain.GetFNData().Count(), child.Brain.GetFNData().Count());
        }
    }
}
