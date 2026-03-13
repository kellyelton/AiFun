using AiFun;
using AiFun.Entities;

namespace AiFun.Tests;

public class TournamentSelectionTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Ecosystem_has_TournamentSize_property_defaulting_to_5()
    {
        var eco = CreateEcosystem();
        Assert.Equal(5, eco.TournamentSize);
    }

    [Fact]
    public void Ecosystem_TournamentSize_can_be_set()
    {
        var eco = CreateEcosystem();
        eco.TournamentSize = 3;
        Assert.Equal(3, eco.TournamentSize);
    }

    [Fact]
    public void Ecosystem_TournamentSize_clamped_to_minimum_2()
    {
        var eco = CreateEcosystem();
        eco.TournamentSize = 1;
        Assert.True(eco.TournamentSize >= 2, "TournamentSize should not go below 2");
    }

    [Fact]
    public void NewGeneration_produces_offspring_using_tournament_selection()
    {
        // Use a large world so animals don't hit walls
        var eco = CreateEcosystem(10000, 10000);
        eco.InitialPopulation = 10;
        eco.ElitePopulation = 6;
        eco.RandomPopulation = 4;
        eco.TournamentSize = 3;
        eco.HallOfFameSize = 0;
        eco.FoodTargetCount = 0;
        eco.CorpseDecaySeconds = 0.5;
        eco.BaseEnergyDrainPerSecond = 0; // prevent energy loss from drain
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;
        eco.Reset();

        // Kill all animals by zeroing energy
        foreach (var a in eco.AnimateObjects.OfType<Animal>().ToList())
            a.AvailableEnergy = 0;

        // Update 1: animals die, become corpses in AnimateObjects
        eco.Update(0.001);
        // Update 2: SimulationTime > TimeOfDeath + CorpseDecaySeconds triggers decay
        eco.Update(1.0);

        // All corpses should have decayed and NewGeneration should have been called
        Assert.Equal(10, eco.AnimateObjects.OfType<Animal>().Count());
        Assert.Equal(1, eco.GenerationCount);
    }

    [Fact]
    public void NewGeneration_works_when_dead_count_less_than_TournamentSize()
    {
        var eco = CreateEcosystem(10000, 10000);
        eco.InitialPopulation = 3;
        eco.ElitePopulation = 2;
        eco.RandomPopulation = 1;
        eco.TournamentSize = 10; // Way more than the 3 dead creatures
        eco.HallOfFameSize = 0;
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

        // Should still produce offspring even with fewer dead than tournament size
        Assert.Equal(3, eco.AnimateObjects.OfType<Animal>().Count());
        Assert.Equal(1, eco.GenerationCount);
    }

    [Fact]
    public void NewGeneration_falls_back_to_Reset_when_fewer_than_2_dead()
    {
        var eco = CreateEcosystem(10000, 10000);
        eco.InitialPopulation = 5;
        eco.ElitePopulation = 3;
        eco.RandomPopulation = 2;
        eco.FoodTargetCount = 0;
        eco.CorpseDecaySeconds = 0.5;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;
        eco.Reset();

        // Kill only 1 animal, remove all others without going through dead list
        var animals = eco.AnimateObjects.OfType<Animal>().ToList();
        animals[0].AvailableEnergy = 0;
        for (int i = 1; i < animals.Count; i++)
            eco.AnimateObjects.Remove(animals[i]);

        // Update 1: the one animal dies
        eco.Update(0.001);
        // Update 2: corpse decays, only 1 in _deadObjects -> Reset fallback
        eco.Update(1.0);

        // Reset creates InitialPopulation new random animals and resets generation to 0
        Assert.Equal(5, eco.AnimateObjects.OfType<Animal>().Count());
        Assert.Equal(0, eco.GenerationCount);
    }

    [Fact]
    public void Ecosystem_does_not_have_TopBreeders_property()
    {
        var eco = CreateEcosystem();
        var type = eco.GetType();
        var prop = type.GetProperty("TopBreeders");
        Assert.Null(prop);
    }
}
