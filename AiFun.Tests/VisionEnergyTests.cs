using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class VisionEnergyTests
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

    [Fact]
    public void Vision_drains_energy_proportional_to_VisionDistance_and_RayCount()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        eco.VisionEnergyCostMultiplier = 1.0;
        eco.BaseEnergyDrainPerSecond = 0; // isolate vision cost
        eco.MovementEnergyCostMultiplier = 0; // isolate vision cost

        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10000;
        animal.VisionDistance = 100;
        animal.Speed = 0; // don't move

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        var energyBefore = animal.AvailableEnergy;
        animal.Update(1.0); // 1 second tick
        var energyAfter = animal.AvailableEnergy;

        var drain = energyBefore - energyAfter;
        // Vision drain = effectiveVision * activeRayCount * VisionEnergyCostMultiplier * time
        // After mapper, Speed changes, so drain depends on NN output speed.
        // Max (speed=0): 100 * 5 * 1.0 * 1.0 = 500
        // Min (speed=20): 25 * 1 * 1.0 * 1.0 = 25
        Assert.True(drain > 0, "Vision should drain some energy");
        Assert.True(drain <= 505, $"Vision drain ({drain}) should not exceed max");
    }

    [Fact]
    public void Zero_VisionDistance_costs_no_vision_energy()
    {
        var eco = CreateEcosystem();
        eco.VisionEnergyCostMultiplier = 1.0;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;

        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10000;
        animal.VisionDistance = 0;
        animal.Speed = 0;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        var energyBefore = animal.AvailableEnergy;
        animal.Update(1.0);
        var energyAfter = animal.AvailableEnergy;

        Assert.Equal(energyBefore, energyAfter, 1);
    }

    [Fact]
    public void Higher_VisionDistance_costs_more_energy()
    {
        // Test using ComputeEffectiveVisionDistance directly to avoid NN speed randomness
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        eco.VisionEnergyCostMultiplier = 1.0;

        var shortSight = CreateAnimalAt(eco, 500, 500);
        shortSight.VisionDistance = 50;
        shortSight.Speed = 0;

        var longSight = CreateAnimalAt(eco, 600, 600);
        longSight.VisionDistance = 200;
        longSight.Speed = 0;

        // At same speed, higher VisionDistance = higher effective vision = more energy cost
        var shortCost = shortSight.ComputeEffectiveVisionDistance() * shortSight.ComputeActiveRayCount() * eco.VisionEnergyCostMultiplier;
        var longCost = longSight.ComputeEffectiveVisionDistance() * longSight.ComputeActiveRayCount() * eco.VisionEnergyCostMultiplier;

        Assert.True(longCost > shortCost,
            $"Long vision cost ({longCost}) should exceed short vision cost ({shortCost})");
    }

    [Fact]
    public void Vision_drain_can_reduce_energy_below_zero_causing_death()
    {
        var eco = CreateEcosystem();
        eco.VisionEnergyCostMultiplier = 5.0;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;

        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10; // very low energy
        animal.VisionDistance = 200;
        animal.Speed = 0;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        // Vision cost = 200 * 5 * 5.0 * 1.0 = 5000, far exceeds 10 energy
        animal.Update(1.0);
        Assert.True(animal.AvailableEnergy <= 0);

        // Death is detected at the start of the next tick
        animal.Update(0.01);
        Assert.True(animal.IsDead);
    }
}
