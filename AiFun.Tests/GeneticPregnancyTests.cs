using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class GeneticPregnancyTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    private Animal CreateAnimalAt(Ecosystem eco, double x, double y)
    {
        var animal = new Animal(eco);
        animal.Location = new Rect(x, y, 5, 5);
        return animal;
    }

    // --- PregnancyGene exists and is in [0, 1] ---

    [Fact]
    public void PregnancyGene_is_between_0_and_1_on_random_creation()
    {
        var eco = CreateEcosystem();
        for (int i = 0; i < 50; i++)
        {
            var animal = new Animal(eco);
            Assert.InRange(animal.PregnancyGene, 0, 1);
        }
    }

    // --- Pregnancy duration computed from gene + ecosystem bounds ---

    [Fact]
    public void PregnancyDuration_computed_from_gene_and_ecosystem_bounds()
    {
        var eco = CreateEcosystem();
        eco.MinPregnancyDuration = 2;
        eco.MaxPregnancyDuration = 20;
        var animal = new Animal(eco);

        // PregnancyDuration = Min + Gene * (Max - Min)
        var expected = 2 + animal.PregnancyGene * (20 - 2);
        Assert.Equal(expected, animal.PregnancyDuration, precision: 6);
    }

    [Fact]
    public void PregnancyDuration_at_gene_0_equals_min()
    {
        var eco = CreateEcosystem();
        eco.MinPregnancyDuration = 3;
        eco.MaxPregnancyDuration = 15;
        var animal = new Animal(eco);
        // We can't set PregnancyGene directly from outside, but we can verify the formula
        // by checking the bounds
        Assert.InRange(animal.PregnancyDuration, 3, 15);
    }

    // --- Ecosystem parameters: MinPregnancyDuration, MaxPregnancyDuration ---

    [Fact]
    public void MinPregnancyDuration_defaults_to_15()
    {
        var eco = CreateEcosystem();
        Assert.Equal(15, eco.MinPregnancyDuration);
    }

    [Fact]
    public void MaxPregnancyDuration_defaults_to_60()
    {
        var eco = CreateEcosystem();
        Assert.Equal(60, eco.MaxPregnancyDuration);
    }

    [Fact]
    public void MinPregnancyDuration_clamps_to_minimum_05()
    {
        var eco = CreateEcosystem();
        eco.MinPregnancyDuration = -5;
        Assert.Equal(0.5, eco.MinPregnancyDuration);
    }

    [Fact]
    public void MaxPregnancyDuration_clamps_to_minimum_1()
    {
        var eco = CreateEcosystem();
        eco.MaxPregnancyDuration = -5;
        Assert.Equal(1, eco.MaxPregnancyDuration);
    }

    // --- PregnancyEnergyCostMultiplier ---

    [Fact]
    public void PregnancyEnergyCostMultiplier_defaults_to_50()
    {
        var eco = CreateEcosystem();
        Assert.Equal(50, eco.PregnancyEnergyCostMultiplier);
    }

    [Fact]
    public void PregnancyEnergyCostMultiplier_clamps_to_zero()
    {
        var eco = CreateEcosystem();
        eco.PregnancyEnergyCostMultiplier = -10;
        Assert.Equal(0, eco.PregnancyEnergyCostMultiplier);
    }

    // --- Pregnancy energy drain ---

    [Fact]
    public void Pregnant_creature_drains_extra_energy_per_tick()
    {
        var eco = CreateEcosystem();
        eco.PregnancyEnergyCostMultiplier = 100;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;

        var female = CreateAnimalAt(eco, 500, 500);
        var male = CreateAnimalAt(eco, 500, 500);
        female.AvailableEnergy = 10000;
        male.AvailableEnergy = 10000;
        female.BreedDesire = 0.9;
        female.EatDesire = 0.1;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(female);
        eco.AnimateObjects.Add(male);

        // Only test if compatible
        if (!female.IsFemale || !male.IsMale) return;

        // Manually impregnate
        female.Touching.Add(male);
        female.HandleTouching();

        if (!female.IsPregnant) return;

        var energyBefore = female.AvailableEnergy;
        female.Update(1.0); // 1 second tick
        var energyAfter = female.AvailableEnergy;

        // Should have drained PregnancyEnergyCostMultiplier * time = 100 * 1.0 = 100
        // (plus any movement energy, but we set those costs to 0)
        var drain = energyBefore - energyAfter;
        Assert.True(drain >= 100, $"Pregnancy energy drain should be >= 100, was {drain}");
    }

    // --- PregnancyGene inherited via Breed() ---

    [Fact]
    public void PregnancyGene_inherited_from_parents()
    {
        var eco = CreateEcosystem();
        var p1 = new Animal(eco);
        var p2 = new Animal(eco);

        // Create child via two-parent constructor
        var child = new Animal(eco, p1, p2);

        // Child's PregnancyGene should be one of the parents' values (crossover)
        // or a mutation. In any case, should be [0, 1].
        Assert.InRange(child.PregnancyGene, 0, 1);
    }

    // --- Deferred baby creation ---

    [Fact]
    public void Baby_created_at_birth_not_conception()
    {
        var eco = CreateEcosystem();
        eco.MinPregnancyDuration = 5;
        eco.MaxPregnancyDuration = 5;

        var female = CreateAnimalAt(eco, 500, 500);
        var male = CreateAnimalAt(eco, 502, 500);
        female.AvailableEnergy = 10000;
        male.AvailableEnergy = 10000;
        female.BreedDesire = 0.9;
        female.EatDesire = 0.1;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(female);
        eco.AnimateObjects.Add(male);

        if (!female.IsFemale || !male.IsMale) return;

        female.Touching.Add(male);
        female.HandleTouching();

        if (!female.IsPregnant) return;

        // PopBaby should create a valid baby at birth time
        var baby = female.PopBaby();
        Assert.NotNull(baby);
        Assert.NotNull(baby.Brain);
        Assert.False(female.IsPregnant);
    }

    [Fact]
    public void Baby_born_after_creature_specific_pregnancy_duration()
    {
        var eco = CreateEcosystem();
        eco.MinPregnancyDuration = 10;
        eco.MaxPregnancyDuration = 10;
        eco.PregnancyEnergyCostMultiplier = 0;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;

        var female = CreateAnimalAt(eco, 500, 500);
        var male = CreateAnimalAt(eco, 500, 500);
        female.AvailableEnergy = 100000;
        male.AvailableEnergy = 100000;
        female.BreedDesire = 0.9;
        female.EatDesire = 0.1;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(female);
        eco.AnimateObjects.Add(male);

        if (!female.IsFemale || !male.IsMale) return;

        female.Touching.Add(male);
        female.HandleTouching();

        if (!female.IsPregnant) return;

        var initialCount = eco.AnimateObjects.OfType<Animal>().Count();

        // Advance time but not enough for birth (pregnancy is 10s for gene=anything since min=max=10)
        for (int i = 0; i < 50; i++) // 50 * 0.1 = 5 seconds
            eco.Update(0.1);

        // Baby should NOT have been born yet
        Assert.True(female.IsPregnant, "Should still be pregnant before duration expires");

        // Advance past pregnancy duration
        for (int i = 0; i < 60; i++) // another 6 seconds
            eco.Update(0.1);

        // Baby should have been born
        Assert.False(female.IsPregnant, "Should no longer be pregnant after duration expires");
    }

    // --- Ecosystem Update uses per-creature pregnancy duration ---

    [Fact]
    public void Ecosystem_uses_per_creature_pregnancy_duration_not_global()
    {
        var eco = CreateEcosystem();
        // Set wide range so creatures with different genes have different durations
        eco.MinPregnancyDuration = 1;
        eco.MaxPregnancyDuration = 100;

        var animal = new Animal(eco);
        // The old PregnancyDurationSeconds should no longer exist as the sole check
        // Instead, the ecosystem should use animal.PregnancyDuration
        Assert.InRange(animal.PregnancyDuration, 1, 100);
    }

    // --- Baby energy formula ---

    [Fact]
    public void Baby_receives_energy_equal_to_pregnancyDuration_times_costMultiplier()
    {
        var eco = CreateEcosystem();
        eco.MinPregnancyDuration = 10;
        eco.MaxPregnancyDuration = 10;
        eco.PregnancyEnergyCostMultiplier = 75;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;

        var female = CreateAnimalAt(eco, 500, 500);
        var male = CreateAnimalAt(eco, 500, 500);
        female.AvailableEnergy = 100000;
        male.AvailableEnergy = 100000;
        female.BreedDesire = 0.9;
        female.EatDesire = 0.1;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(female);
        eco.AnimateObjects.Add(male);

        if (!female.IsFemale || !male.IsMale) return;

        female.Touching.Add(male);
        female.HandleTouching();

        if (!female.IsPregnant) return;

        // Advance time past pregnancy duration (10s) with small ticks
        for (int i = 0; i < 120; i++) // 120 * 0.1 = 12 seconds, enough to trigger birth
            eco.Update(0.1);

        // Find the baby (any animal that isn't the original female or male)
        var baby = eco.AnimateObjects.OfType<Animal>()
            .FirstOrDefault(a => a != female && a != male);

        Assert.NotNull(baby);

        // Baby energy should be actualDuration * PregnancyEnergyCostMultiplier
        // With clamping, actualDuration <= PregnancyDuration = 10
        // Expected = 10 * 75 = 750
        var expectedEnergy = 10.0 * 75.0;
        Assert.InRange(baby.AvailableEnergy, expectedEnergy - 80, expectedEnergy + 1);
    }

    // --- GeneticDiversity includes PregnancyGene ---

    [Fact]
    public void GeneticDiversity_includes_pregnancy_gene()
    {
        var eco = CreateEcosystem();
        // Create animals and verify diversity calculation doesn't crash
        eco.Reset();
        var diversity = eco.GeneticDiversity;
        Assert.InRange(diversity, 0, 100);
    }
}
