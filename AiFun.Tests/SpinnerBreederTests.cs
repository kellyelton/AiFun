using System.Windows;
using AiFun;

namespace AiFun.Tests;

/// <summary>
/// Proves that the simulation is vulnerable to "spinner-breeder" exploitation:
/// creatures that sit still, spin in place, and breed as fast as pregnancy allows
/// can cause unbounded population growth because there is no density-dependent
/// pressure to counteract it.
///
/// These tests are RED by design — they describe the desired behavior (population
/// should be controlled) and fail because the system currently lacks the mechanics
/// to enforce it. Once density pressure is added, these tests should go green.
///
/// Tests deliberately avoid neural network specifics — they force spinner-breeder
/// behavior by manually setting desires and triggering breeding, making them
/// resilient to any NN architecture/IO changes.
/// </summary>
public class SpinnerBreederTests
{
    private Ecosystem CreateEcosystem(double width = 5000, double height = 5000)
    {
        return new Ecosystem(width, height);
    }

    private Animal CreateAnimalAt(Ecosystem eco, double x, double y)
    {
        var animal = new Animal(eco);
        animal.Location = new Rect(x, y, 5, 5);
        return animal;
    }

    /// <summary>
    /// Ensures we have at least the requested number of males and females
    /// by creating animals until the quota is met. Returns all created animals.
    /// </summary>
    private List<Animal> SeedBreedingPopulation(Ecosystem eco, double x, double y, int minMales, int minFemales, double energy = 100000)
    {
        var animals = new List<Animal>();
        int males = 0, females = 0;

        while (males < minMales || females < minFemales)
        {
            var a = CreateAnimalAt(eco, x, y);
            a.AvailableEnergy = energy;
            eco.AnimateObjects.Add(a);
            animals.Add(a);
            if (a.IsMale) males++;
            if (a.IsFemale) females++;
        }

        return animals;
    }

    /// <summary>
    /// Forces all available male/female pairs to breed by manually setting desires
    /// and triggering HandleTouching. Returns the number of successful impregnations.
    /// </summary>
    private int ForceBreedAllPairs(Ecosystem eco, double minEnergy = 50000)
    {
        var alive = eco.AnimateObjects.OfType<Animal>().Where(a => !a.IsDead).ToList();
        var females = alive.Where(a => a.IsFemale && !a.IsPregnant).ToList();
        var males = alive.Where(a => a.IsMale).ToList();

        int bred = 0;
        int pairs = Math.Min(females.Count, males.Count);
        for (int i = 0; i < pairs; i++)
        {
            var female = females[i];
            var male = males[i];

            // Ensure both have energy to breed
            female.AvailableEnergy = Math.Max(female.AvailableEnergy, minEnergy);
            male.AvailableEnergy = Math.Max(male.AvailableEnergy, minEnergy);

            // Force spinner-breeder behavior: high breed desire, no eat desire
            female.BreedDesire = 1.0;
            female.EatDesire = 0.0;
            male.BreedDesire = 1.0;
            male.EatDesire = 0.0;

            // Clear any stale touching entries before adding our target
            female.Touching.Clear();
            female.Touching.Add(male);
            female.HandleTouching();

            if (female.IsPregnant)
                bred++;
        }

        return bred;
    }

    /// <summary>
    /// Delivers all pregnant females' babies via PopBaby() and adds them to the ecosystem.
    /// Bypasses eco.Update entirely so no NN side effects occur.
    /// </summary>
    private int DeliverAllBabies(Ecosystem eco)
    {
        var pregnant = eco.AnimateObjects.OfType<Animal>()
            .Where(a => a.IsPregnant && !a.IsDead).ToList();

        int born = 0;
        foreach (var mom in pregnant)
        {
            var baby = mom.PopBaby();
            baby.AvailableEnergy = 100000;
            eco.AnimateObjects.Add(baby);
            born++;
        }

        return born;
    }

    // --- Core population explosion test ---

    [Fact]
    public void Stationary_breeders_should_not_cause_unbounded_population_growth()
    {
        // This test bypasses the neural network and eco.Update entirely.
        // It directly exercises the breeding mechanics to prove that
        // spinner-breeders can cause exponential population growth
        // with no system mechanism to stop it.
        //
        // Each round: breed all available pairs → deliver babies → repeat.
        // This models a cluster of spinner-breeders that sit in place,
        // breed whenever possible, and immediately breed again after birth.
        var eco = CreateEcosystem();
        eco.AnimateObjects.Clear();

        // Seed with breeding population
        SeedBreedingPopulation(eco, 2500, 2500, minMales: 8, minFemales: 8);
        int initialPop = eco.AnimateObjects.OfType<Animal>().Count();

        // Run 10 breed-deliver cycles with no NN involvement
        for (int round = 0; round < 10; round++)
        {
            ForceBreedAllPairs(eco);
            DeliverAllBabies(eco);
        }

        int finalPop = eco.AnimateObjects.OfType<Animal>().Count();

        // DESIRED BEHAVIOR: A healthy ecosystem should have density-dependent
        // pressure that prevents spinner-breeders from causing unbounded growth.
        // Population should stabilize near a carrying capacity, not explode.
        //
        // This test is RED because the system currently has NO density control.
        // With ~16-20 initial animals and 10 rounds of forced breeding,
        // population grows exponentially (~1.5x per round = 50-100x growth).
        //
        // When density-dependent mechanics are added, this should go GREEN.
        int maxHealthyPopulation = initialPop * 3;

        Assert.True(finalPop <= maxHealthyPopulation,
            $"Spinner-breeders overran the simulation! Population grew from {initialPop} to {finalPop} " +
            $"(limit: {maxHealthyPopulation}). System lacks density-dependent pressure to prevent " +
            "runaway stationary breeding. Consider: crowding energy costs, breeding cooldowns, " +
            "minimum energy thresholds, or movement-linked fertility.");
    }

    // --- Breeding rate is unrestricted ---

    [Fact]
    public void Female_should_not_breed_again_immediately_after_giving_birth()
    {
        // In nature, females have a recovery/refractory period after birth.
        // Currently, a female can get pregnant again the instant she gives birth.
        // This test bypasses eco.Update to avoid NN interference.
        var eco = CreateEcosystem();
        eco.AnimateObjects.Clear();

        // Create a guaranteed breeding pair
        Animal? female = null, male = null;
        for (int i = 0; i < 100 && (female == null || male == null); i++)
        {
            var a = CreateAnimalAt(eco, 500, 500);
            a.AvailableEnergy = 100000;
            eco.AnimateObjects.Add(a);
            if (a.IsFemale && female == null) female = a;
            if (a.IsMale && male == null) male = a;
        }
        if (female == null || male == null) return;

        // First breeding
        female.BreedDesire = 1.0;
        female.EatDesire = 0.0;
        female.Touching.Clear();
        female.Touching.Add(male);
        female.HandleTouching();
        Assert.True(female.IsPregnant, "First breeding should succeed");

        // Deliver baby directly (no eco.Update needed)
        var baby = female.PopBaby();
        eco.AnimateObjects.Add(baby);
        Assert.False(female.IsPregnant, "Should have given birth");

        // Attempt immediate re-breeding
        female.BreedDesire = 1.0;
        female.EatDesire = 0.0;
        male.AvailableEnergy = 100000;
        female.AvailableEnergy = 100000;
        female.Touching.Clear();
        female.Touching.Add(male);
        female.HandleTouching();

        // DESIRED BEHAVIOR: Female should NOT be able to breed again immediately.
        // There should be a refractory/recovery period after giving birth.
        //
        // This test is RED because the system allows instant re-breeding.
        Assert.False(female.IsPregnant,
            "Female should not be able to breed again immediately after giving birth. " +
            "A refractory period is needed to prevent spinner-breeder exploitation.");
    }

    // --- No density-dependent energy cost ---

    [Fact]
    public void Crowded_creatures_should_pay_higher_energy_costs()
    {
        // In nature, overcrowding causes stress that increases metabolic cost.
        // Currently, 100 creatures in the same pixel cost exactly the same
        // per-creature as 1 creature alone.
        var eco = CreateEcosystem(1000, 1000);
        eco.BaseEnergyDrainPerSecond = 30;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;
        eco.PregnancyEnergyCostMultiplier = 0;
        eco.FoodTargetCount = 0;
        eco.AnimateObjects.Clear();

        // Create a lone creature far from others
        var loner = CreateAnimalAt(eco, 100, 100);
        loner.AvailableEnergy = 10000;
        eco.AnimateObjects.Add(loner);

        // Create a crowded cluster of 20 creatures
        var crowded = new List<Animal>();
        for (int i = 0; i < 20; i++)
        {
            var a = CreateAnimalAt(eco, 500, 500);
            a.AvailableEnergy = 10000;
            eco.AnimateObjects.Add(a);
            crowded.Add(a);
        }

        // Run one tick — use animal.Update directly to avoid eco.Update side effects
        loner.Update(1.0);
        crowded[0].Update(1.0);

        double lonerDrain = 10000 - loner.AvailableEnergy;
        double crowdedDrain = 10000 - crowded[0].AvailableEnergy;

        // DESIRED BEHAVIOR: Crowded creature should drain MORE energy than lone creature
        // due to density-dependent stress costs.
        //
        // This test is RED because there is no density-dependent energy cost.
        // Both creatures drain exactly BaseEnergyDrainPerSecond regardless of neighbors.
        Assert.True(crowdedDrain > lonerDrain,
            $"Crowded creature drained {crowdedDrain:F1} energy, lone creature drained {lonerDrain:F1}. " +
            "Crowded creatures should pay a density-dependent stress cost, but currently " +
            "there is no penalty for overcrowding.");
    }

    // --- No minimum energy threshold for breeding ---

    [Fact]
    public void Breeding_should_require_minimum_energy_reserves()
    {
        // In nature, organisms need substantial energy reserves before
        // they can reproduce. Currently breeding only costs 100 * MovementEfficiency,
        // which can be as low as 0-100 energy — trivial relative to max energy of 10000.
        var eco = CreateEcosystem();
        eco.AnimateObjects.Clear();

        Animal? female = null, male = null;
        for (int i = 0; i < 100 && (female == null || male == null); i++)
        {
            var a = CreateAnimalAt(eco, 500, 500);
            a.AvailableEnergy = 500; // Very low energy (5% of max)
            eco.AnimateObjects.Add(a);
            if (a.IsFemale && female == null) female = a;
            if (a.IsMale && male == null) male = a;
        }
        if (female == null || male == null) return;

        female.AvailableEnergy = 500; // Only 5% energy
        male.AvailableEnergy = 500;
        female.BreedDesire = 1.0;
        female.EatDesire = 0.0;
        female.Touching.Clear();
        female.Touching.Add(male);
        female.HandleTouching();

        // DESIRED BEHAVIOR: Creatures with very low energy reserves should NOT
        // be able to breed. Reproduction should require demonstrated foraging ability.
        //
        // This test is RED because there is no minimum energy threshold for breeding.
        Assert.False(female.IsPregnant,
            $"Creature with only 500 energy (5% of max) should not be able to breed. " +
            "A minimum energy threshold is needed to ensure only fit creatures reproduce.");
    }
}
