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
    public void Vision_drains_energy_proportional_to_VisionDistance()
    {
        var eco = CreateEcosystem();
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
        // Vision drain should be VisionDistance * VisionEnergyCostMultiplier * time = 100 * 1.0 * 1.0 = 100
        Assert.InRange(drain, 95, 105); // allow small tolerance for floating point
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
        var eco = CreateEcosystem();
        eco.VisionEnergyCostMultiplier = 1.0;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;

        var shortSight = CreateAnimalAt(eco, 500, 500);
        shortSight.AvailableEnergy = 10000;
        shortSight.VisionDistance = 50;
        shortSight.Speed = 0;

        var longSight = CreateAnimalAt(eco, 600, 600);
        longSight.AvailableEnergy = 10000;
        longSight.VisionDistance = 200;
        longSight.Speed = 0;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(shortSight);
        eco.AnimateObjects.Add(longSight);

        shortSight.Update(1.0);
        longSight.Update(1.0);

        // longSight should have lost more energy
        Assert.True(longSight.AvailableEnergy < shortSight.AvailableEnergy);
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

        // Vision cost = 200 * 5.0 * 1.0 = 1000, far exceeds 10 energy
        animal.Update(1.0);
        Assert.True(animal.AvailableEnergy <= 0);

        // Death is detected at the start of the next tick
        animal.Update(0.01);
        Assert.True(animal.IsDead);
    }
}
