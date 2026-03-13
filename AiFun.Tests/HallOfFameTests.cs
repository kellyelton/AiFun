using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class HallOfFameTests
{
    private Ecosystem CreateEcosystem(double width = 10000, double height = 10000)
    {
        var eco = new Ecosystem(width, height);
        eco.FoodTargetCount = 0;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;
        eco.PregnancyEnergyCostMultiplier = 0;
        eco.CorpseDecaySeconds = 0.5;
        return eco;
    }

    private void KillAllAndAdvanceGeneration(Ecosystem eco)
    {
        foreach (var a in eco.AnimateObjects.OfType<Animal>().ToList())
            a.AvailableEnergy = 0;
        eco.Update(0.001);
        eco.Update(1.0);
    }

    // --- Property existence tests ---

    [Fact]
    public void Ecosystem_has_HallOfFameSize_property_defaulting_to_5()
    {
        var eco = CreateEcosystem();
        Assert.Equal(5, eco.HallOfFameSize);
    }

    [Fact]
    public void Ecosystem_has_HallOfFameGenerations_property_defaulting_to_5()
    {
        var eco = CreateEcosystem();
        Assert.Equal(5, eco.HallOfFameGenerations);
    }

    [Fact]
    public void HallOfFameSize_clamped_to_minimum_0()
    {
        var eco = CreateEcosystem();
        eco.HallOfFameSize = -1;
        Assert.True(eco.HallOfFameSize >= 0);
    }

    [Fact]
    public void HallOfFameGenerations_clamped_to_minimum_0()
    {
        var eco = CreateEcosystem();
        eco.HallOfFameGenerations = -1;
        Assert.True(eco.HallOfFameGenerations >= 0);
    }

    // --- Clone spawning tests ---

    [Fact]
    public void NewGeneration_spawns_hall_of_fame_clones_from_previous_generation()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 20;
        eco.ElitePopulation = 10;
        eco.RandomPopulation = 5;
        eco.HallOfFameSize = 5;
        eco.HallOfFameGenerations = 5;
        eco.Reset();

        // Give animals different fitness levels so top 5 are distinct
        var animals = eco.AnimateObjects.OfType<Animal>().ToList();
        for (int i = 0; i < animals.Count; i++)
            animals[i].FoodEaten = i * 100; // Higher index = higher fitness

        KillAllAndAdvanceGeneration(eco);

        // Should have elite + random + hall of fame clones
        var hofAnimals = eco.AnimateObjects.OfType<Animal>().Where(a => a.Origin == AnimalOrigin.HallOfFame).ToList();
        Assert.Equal(5, hofAnimals.Count);
    }

    [Fact]
    public void Hall_of_fame_clones_have_HallOfFame_origin()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 2;
        eco.HallOfFameSize = 3;
        eco.HallOfFameGenerations = 1;
        eco.Reset();

        KillAllAndAdvanceGeneration(eco);

        var hofAnimals = eco.AnimateObjects.OfType<Animal>().Where(a => a.Origin == AnimalOrigin.HallOfFame).ToList();
        Assert.Equal(3, hofAnimals.Count);
        Assert.All(hofAnimals, a => Assert.Equal(AnimalOrigin.HallOfFame, a.Origin));
    }

    [Fact]
    public void Hall_of_fame_clones_preserve_brain_weights_exactly()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 0;
        eco.HallOfFameSize = 1;
        eco.HallOfFameGenerations = 1;
        eco.Reset();

        // Make one animal clearly the best
        var animals = eco.AnimateObjects.OfType<Animal>().ToList();
        var best = animals[0];
        best.FoodEaten = 999999;
        var originalWeights = best.Brain.GetFNData().Select(f => f.Weight).ToArray();
        var originalMovEff = best.MovementEfficency;
        var originalVision = best.VisionDistance;

        KillAllAndAdvanceGeneration(eco);

        var hofClone = eco.AnimateObjects.OfType<Animal>().Single(a => a.Origin == AnimalOrigin.HallOfFame);
        var cloneWeights = hofClone.Brain.GetFNData().Select(f => f.Weight).ToArray();

        Assert.Equal(originalWeights.Length, cloneWeights.Length);
        for (int i = 0; i < originalWeights.Length; i++)
            Assert.Equal(originalWeights[i], cloneWeights[i], 10); // exact match

        Assert.Equal(originalMovEff, hofClone.MovementEfficency, 10);
        Assert.Equal(originalVision, hofClone.VisionDistance, 10);
    }

    [Fact]
    public void Hall_of_fame_clones_preserve_pregnancy_gene()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 0;
        eco.HallOfFameSize = 1;
        eco.HallOfFameGenerations = 1;
        eco.Reset();

        var animals = eco.AnimateObjects.OfType<Animal>().ToList();
        var best = animals[0];
        best.FoodEaten = 999999;
        var origPregnancy = best.PregnancyGene;

        KillAllAndAdvanceGeneration(eco);

        var hofClone = eco.AnimateObjects.OfType<Animal>().Single(a => a.Origin == AnimalOrigin.HallOfFame);
        Assert.Equal(origPregnancy, hofClone.PregnancyGene, 10);
    }

    [Fact]
    public void Hall_of_fame_clones_preserve_genetic_color()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 0;
        eco.HallOfFameSize = 1;
        eco.HallOfFameGenerations = 1;
        eco.Reset();

        var animals = eco.AnimateObjects.OfType<Animal>().ToList();
        var best = animals[0];
        best.FoodEaten = 999999;
        var origR = best.ColorR;
        var origG = best.ColorG;
        var origB = best.ColorB;

        KillAllAndAdvanceGeneration(eco);

        var hofClone = eco.AnimateObjects.OfType<Animal>().Single(a => a.Origin == AnimalOrigin.HallOfFame);
        Assert.Equal(origR, hofClone.ColorR, 10);
        Assert.Equal(origG, hofClone.ColorG, 10);
        Assert.Equal(origB, hofClone.ColorB, 10);
    }

    // --- Rolling buffer tests ---

    [Fact]
    public void Hall_of_fame_accumulates_across_generations()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 0;
        eco.HallOfFameSize = 2;
        eco.HallOfFameGenerations = 5;
        eco.Reset();

        // Gen 0 -> Gen 1: should have 2 HoF clones (from gen 0)
        KillAllAndAdvanceGeneration(eco);
        var hof1 = eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame);
        Assert.Equal(2, hof1);

        // Gen 1 -> Gen 2: should have 4 HoF clones (from gen 0 + gen 1)
        KillAllAndAdvanceGeneration(eco);
        var hof2 = eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame);
        Assert.Equal(4, hof2);
    }

    [Fact]
    public void Hall_of_fame_drops_oldest_generation_when_buffer_full()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 0;
        eco.HallOfFameSize = 1;
        eco.HallOfFameGenerations = 2; // Only keep 2 generations
        eco.Reset();

        // Gen 0 -> Gen 1: 1 HoF clone
        KillAllAndAdvanceGeneration(eco);
        Assert.Equal(1, eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame));

        // Gen 1 -> Gen 2: 2 HoF clones (gen 0 + gen 1)
        KillAllAndAdvanceGeneration(eco);
        Assert.Equal(2, eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame));

        // Gen 2 -> Gen 3: still 2 HoF clones (gen 1 + gen 2, gen 0 dropped)
        KillAllAndAdvanceGeneration(eco);
        Assert.Equal(2, eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame));
    }

    // --- Edge cases ---

    [Fact]
    public void HallOfFameSize_zero_spawns_no_clones()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 5;
        eco.HallOfFameSize = 0;
        eco.HallOfFameGenerations = 5;
        eco.Reset();

        KillAllAndAdvanceGeneration(eco);

        var hofAnimals = eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame);
        Assert.Equal(0, hofAnimals);
    }

    [Fact]
    public void HallOfFameGenerations_zero_spawns_no_clones()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 5;
        eco.HallOfFameSize = 5;
        eco.HallOfFameGenerations = 0;
        eco.Reset();

        KillAllAndAdvanceGeneration(eco);

        var hofAnimals = eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame);
        Assert.Equal(0, hofAnimals);
    }

    [Fact]
    public void Reset_clears_hall_of_fame()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 0;
        eco.HallOfFameSize = 3;
        eco.HallOfFameGenerations = 5;
        eco.Reset();

        // Build up some HoF history
        KillAllAndAdvanceGeneration(eco);
        Assert.True(eco.AnimateObjects.OfType<Animal>().Any(a => a.Origin == AnimalOrigin.HallOfFame));

        // Reset should clear all HoF history
        eco.Reset();
        var hofAnimals = eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame);
        Assert.Equal(0, hofAnimals);

        // Next generation should only have HoF from the reset generation, not older
        KillAllAndAdvanceGeneration(eco);
        var hofAfterReset = eco.AnimateObjects.OfType<Animal>().Count(a => a.Origin == AnimalOrigin.HallOfFame);
        Assert.Equal(3, hofAfterReset); // Only from the one generation since reset
    }

    // --- HallOfFameRank property ---

    [Fact]
    public void Hall_of_fame_clones_have_rank_based_on_fitness_order()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 0;
        eco.HallOfFameSize = 3;
        eco.HallOfFameGenerations = 1;
        eco.Reset();

        // Give distinct fitness values
        var animals = eco.AnimateObjects.OfType<Animal>().ToList();
        for (int i = 0; i < animals.Count; i++)
            animals[i].FoodEaten = i * 1000;

        KillAllAndAdvanceGeneration(eco);

        var hofAnimals = eco.AnimateObjects.OfType<Animal>()
            .Where(a => a.Origin == AnimalOrigin.HallOfFame)
            .OrderBy(a => a.HallOfFameRank)
            .ToList();

        Assert.Equal(3, hofAnimals.Count);
        Assert.Equal(1, hofAnimals[0].HallOfFameRank); // Best
        Assert.Equal(2, hofAnimals[1].HallOfFameRank);
        Assert.Equal(3, hofAnimals[2].HallOfFameRank); // Worst of top 3
    }

    [Fact]
    public void Non_hall_of_fame_animals_have_rank_zero()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 3;
        eco.HallOfFameSize = 2;
        eco.HallOfFameGenerations = 1;
        eco.Reset();

        KillAllAndAdvanceGeneration(eco);

        var nonHof = eco.AnimateObjects.OfType<Animal>().Where(a => a.Origin != AnimalOrigin.HallOfFame).ToList();
        Assert.All(nonHof, a => Assert.Equal(0, a.HallOfFameRank));
    }

    // --- Total population count ---

    [Fact]
    public void NewGeneration_total_count_includes_hall_of_fame()
    {
        var eco = CreateEcosystem();
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 5;
        eco.RandomPopulation = 2;
        eco.HallOfFameSize = 3;
        eco.HallOfFameGenerations = 1;
        eco.Reset();

        KillAllAndAdvanceGeneration(eco);

        // Total should be elite + random + hof = 5 + 2 + 3 = 10
        Assert.Equal(10, eco.AnimateObjects.OfType<Animal>().Count());
    }
}
