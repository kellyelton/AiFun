using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class VisionInputTests
{
    [Fact]
    public void Network_has_8_inputs_and_2_outputs()
    {
        var eco = new Ecosystem(2000, 2000);
        var animal = new Animal(eco);

        // 8 inputs: AvailableEnergy, LookingAngle, WallAhead,
        // AliveCreatureAhead, DeadCreatureAhead, FoodAhead, FoodEnergyAhead,
        // DistanceToObjectAhead
        // 2 outputs: Speed, TurnDeltaPerTick
        var brain = animal.Brain;
        Assert.Equal(8, brain.InputCount);
        Assert.Equal(2, brain.OutputCount);
    }

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
    public void WallAhead_is_1_when_wall_detected()
    {
        var eco = CreateEcosystem(200, 200);
        var animal = CreateAnimalAt(eco, 180, 100);
        animal.LookingAngle = 0; // looking right toward wall
        animal.VisionDistance = 100;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        Assert.Equal(1, animal.WallAhead);
        Assert.Equal(0, animal.AliveCreatureAhead);
        Assert.Equal(0, animal.DeadCreatureAhead);
    }

    [Fact]
    public void AliveCreatureAhead_is_1_when_alive_creature_detected()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var target = CreateAnimalAt(eco, 150, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        Assert.Equal(1, looker.AliveCreatureAhead);
        Assert.Equal(0, looker.WallAhead);
        Assert.Equal(0, looker.DeadCreatureAhead);
    }

    [Fact]
    public void DeadCreatureAhead_is_1_when_dead_creature_detected()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var target = CreateAnimalAt(eco, 150, 100);
        target.AvailableEnergy = 0;
        target.Update(0.01); // kills it
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        Assert.Equal(1, looker.DeadCreatureAhead);
        Assert.Equal(0, looker.WallAhead);
        Assert.Equal(0, looker.AliveCreatureAhead);
    }

    [Fact]
    public void All_vision_flags_are_0_when_nothing_detected()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.LookingAngle = 0;
        animal.VisionDistance = 50; // short range, nothing nearby
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        Assert.Equal(0, animal.WallAhead);
        Assert.Equal(0, animal.AliveCreatureAhead);
        Assert.Equal(0, animal.DeadCreatureAhead);
        Assert.Equal(0, animal.DistanceToObjectAhead);
    }

    [Fact]
    public void DistanceToObjectAhead_is_inverted_normalized_by_VisionDistance()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var target = CreateAnimalAt(eco, 150, 100); // ~50px away
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        // Distance ~50 out of 200 = 0.25 normalized, inverted = 0.75
        // With 5px step granularity, allow some tolerance
        Assert.True(looker.DistanceToObjectAhead > 0.5, $"Expected > 0.5, got {looker.DistanceToObjectAhead}");
        Assert.True(looker.DistanceToObjectAhead <= 1.0);
    }

    [Fact]
    public void Zero_VisionDistance_produces_no_detection()
    {
        var eco = CreateEcosystem(200, 200);
        var animal = CreateAnimalAt(eco, 180, 100);
        animal.LookingAngle = 0; // facing wall 20px away
        animal.VisionDistance = 0;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Blind creature should detect nothing
        Assert.Equal(0, animal.WallAhead);
        Assert.Equal(0, animal.AliveCreatureAhead);
        Assert.Equal(0, animal.DeadCreatureAhead);
        Assert.Equal(0, animal.DistanceToObjectAhead);
    }

    [Fact]
    public void DistanceToObjectAhead_does_not_cause_division_by_zero_when_VisionDistance_is_zero()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.LookingAngle = 0;
        animal.VisionDistance = 0;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        // Should not throw
        var ex = Record.Exception(() => animal.UpdateVision());

        Assert.Null(ex);
        Assert.Equal(0, animal.DistanceToObjectAhead);
    }
}
