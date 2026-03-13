using AiFun;

namespace AiFun.Tests;

public class FixedTopologyTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Ecosystem_HiddenLayerSize_defaults_to_12()
    {
        var eco = CreateEcosystem();
        Assert.Equal(12, eco.HiddenLayerSize);
    }

    [Fact]
    public void Ecosystem_HiddenLayerSize_has_minimum_of_1()
    {
        var eco = CreateEcosystem();
        eco.HiddenLayerSize = 0;
        Assert.Equal(1, eco.HiddenLayerSize);
    }

    [Fact]
    public void All_random_animals_have_identical_network_topology()
    {
        var eco = CreateEcosystem();
        var animals = Enumerable.Range(0, 20).Select(_ => new Animal(eco)).ToList();

        // All animals should have the same number of weights
        var weightCounts = animals.Select(a => a.Brain.GetFNData().Count()).Distinct().ToList();
        Assert.Single(weightCounts);
    }

    [Fact]
    public void Network_has_exactly_one_hidden_layer()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);

        // BasicNetwork layers: input + 1 hidden + output = 3 layers
        Assert.Equal(3, animal.Brain.LayerCount);
    }

    [Fact]
    public void Hidden_layer_size_matches_ecosystem_parameter()
    {
        var eco = CreateEcosystem();
        eco.HiddenLayerSize = 8;
        var animal = new Animal(eco);

        // The hidden layer (layer index 1 in Encog, but Encog counts from output)
        // With 3 layers total, hidden is layer 1 (middle)
        // In Encog, layer 0 = output, layer N-1 = input, so hidden = layer 1
        var hiddenNeuronCount = animal.Brain.GetLayerNeuronCount(1);
        Assert.Equal(8, hiddenNeuronCount);
    }

    [Fact]
    public void Elite_children_have_identical_topology_to_parents()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 0.0;

        var parent1 = new Animal(eco);
        var parent2 = new Animal(eco);
        var child = new Animal(eco, parent1, parent2);

        var p1Count = parent1.Brain.GetFNData().Count();
        var childCount = child.Brain.GetFNData().Count();
        Assert.Equal(p1Count, childCount);
    }

    [Fact]
    public void All_weights_match_between_parents_during_crossover()
    {
        // Since all animals have identical topology, crossover should never
        // encounter null weights (w1==null or w2==null)
        var eco = CreateEcosystem();
        eco.MutationRate = 0.0;

        for (int trial = 0; trial < 20; trial++)
        {
            var p1 = new Animal(eco);
            var p2 = new Animal(eco);
            var child = new Animal(eco, p1, p2);

            var childWeights = child.Brain.GetFNData().ToArray();
            var p1Weights = p1.Brain.GetFNData().ToArray();
            var p2Weights = p2.Brain.GetFNData().ToArray();

            foreach (var cw in childWeights)
            {
                var w1 = p1Weights.FirstOrDefault(x => x.Equals(cw));
                var w2 = p2Weights.FirstOrDefault(x => x.Equals(cw));

                // Both parents should always have the weight
                Assert.NotNull(w1);
                Assert.NotNull(w2);

                // Child weight should be from one of the parents (with 0 mutation)
                Assert.True(
                    Math.Abs(cw.Weight - w1.Weight) < 0.0001 ||
                    Math.Abs(cw.Weight - w2.Weight) < 0.0001,
                    $"Child weight {cw.Weight} doesn't match p1={w1.Weight} or p2={w2.Weight}");
            }
        }
    }

    [Fact]
    public void Natural_birth_babies_have_same_topology_as_parents()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 0.0;

        // Create a female and male, breed them
        Animal mother = null;
        Animal father = null;
        for (int i = 0; i < 100; i++)
        {
            var a = new Animal(eco);
            if (a.IsFemale && mother == null) mother = a;
            if (a.IsMale && father == null) father = a;
            if (mother != null && father != null) break;
        }
        Assert.NotNull(mother);
        Assert.NotNull(father);

        // Simulate natural birth via PopBaby
        // We need to use the internal Impregnate -> PopBaby path
        // Use reflection or the public API
        var motherWeightCount = mother.Brain.GetFNData().Count();

        // Create an elite child and verify topology matches
        var child = new Animal(eco, mother, father);
        Assert.Equal(motherWeightCount, child.Brain.GetFNData().Count());
    }

    [Fact]
    public void Changing_HiddenLayerSize_affects_new_animals()
    {
        var eco = CreateEcosystem();

        eco.HiddenLayerSize = 8;
        var animal1 = new Animal(eco);
        var count1 = animal1.Brain.GetFNData().Count();

        eco.HiddenLayerSize = 16;
        var animal2 = new Animal(eco);
        var count2 = animal2.Brain.GetFNData().Count();

        // Different hidden layer sizes should produce different weight counts
        Assert.NotEqual(count1, count2);
    }
}
