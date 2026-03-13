using AiFun;
using AiFun.Entities;

namespace AiFun.Tests;

public class HeavyMutantInjectionTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void RandomPopulation_defaults_to_10()
    {
        var eco = CreateEcosystem();
        Assert.Equal(10, eco.RandomPopulation);
    }

    [Fact]
    public void HeavyMutant_constructor_creates_animal_with_Random_origin()
    {
        var eco = CreateEcosystem();
        var parent = new Animal(eco);
        var mutant = new Animal(eco, parent, mutationMultiplier: 10.0);
        Assert.Equal(AnimalOrigin.Random, mutant.Origin);
    }

    [Fact]
    public void HeavyMutant_constructor_creates_living_animal()
    {
        var eco = CreateEcosystem();
        var parent = new Animal(eco);
        var mutant = new Animal(eco, parent, mutationMultiplier: 10.0);
        Assert.False(mutant.IsDead);
        Assert.True(mutant.AvailableEnergy > 0);
    }

    [Fact]
    public void HeavyMutant_has_valid_brain_network()
    {
        var eco = CreateEcosystem();
        var parent = new Animal(eco);
        var mutant = new Animal(eco, parent, mutationMultiplier: 10.0);
        Assert.NotNull(mutant.Brain);
    }

    [Fact]
    public void HeavyMutant_inherits_traits_from_parent_with_mutation()
    {
        // Run multiple times to verify traits are sometimes different (due to mutation)
        var eco = CreateEcosystem();
        var parent = new Animal(eco);
        bool anyDifference = false;

        for (int i = 0; i < 50; i++)
        {
            var mutant = new Animal(eco, parent, mutationMultiplier: 10.0);
            if (Math.Abs(mutant.MovementEfficency - parent.MovementEfficency) > 0.001 ||
                Math.Abs(mutant.VisionDistance - parent.VisionDistance) > 0.001)
            {
                anyDifference = true;
                break;
            }
        }

        Assert.True(anyDifference, "Heavy mutants should sometimes differ from parent due to 10x mutation");
    }

    [Fact]
    public void NewGeneration_creates_mutants_not_fully_random_for_random_slots()
    {
        var eco = CreateEcosystem(10000, 10000);
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 5;
        eco.FoodTargetCount = 0;
        eco.CorpseDecaySeconds = 0.5;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;
        eco.Reset();

        // Kill all animals
        foreach (var a in eco.AnimateObjects.OfType<Animal>().ToList())
            a.AvailableEnergy = 0;

        eco.Update(0.001);
        eco.Update(1.0);

        // NewGeneration should have fired — verify the random-origin animals exist
        var animals = eco.AnimateObjects.OfType<Animal>().ToList();
        Assert.Equal(10, animals.Count);

        var randomOrigin = animals.Where(a => a.Origin == AnimalOrigin.Random).ToList();
        Assert.Equal(5, randomOrigin.Count);

        var eliteOrigin = animals.Where(a => a.Origin == AnimalOrigin.Elite).ToList();
        Assert.Equal(5, eliteOrigin.Count);
    }

    [Fact]
    public void NewGeneration_total_population_equals_elite_plus_random()
    {
        var eco = CreateEcosystem(10000, 10000);
        eco.InitialPopulation = 20;
        eco.ElitePopulation = 15;
        eco.RandomPopulation = 5;
        eco.FoodTargetCount = 0;
        eco.CorpseDecaySeconds = 0.5;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;
        eco.Reset();

        foreach (var a in eco.AnimateObjects.OfType<Animal>().ToList())
            a.AvailableEnergy = 0;

        eco.Update(0.001);
        eco.Update(1.0);

        Assert.Equal(20, eco.AnimateObjects.OfType<Animal>().Count());
    }
}
