using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class VisionInputTests
{
    [Fact]
    public void Network_has_17_inputs_and_4_outputs()
    {
        var eco = new Ecosystem(2000, 2000);
        var animal = new Animal(eco);

        // 2 base inputs (AvailableEnergy, LookingAngle) + 5 rays * 3 per-ray + 4 recurrent = 21
        // 4 outputs: Speed, TurnDeltaPerTick, EatDesire, BreedDesire
        var brain = animal.Brain;
        Assert.Equal(21, brain.InputCount);
        Assert.Equal(4, brain.OutputCount);
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
    public void Wall_detected_in_center_ray_when_wall_ahead()
    {
        var eco = CreateEcosystem(200, 200);
        var animal = CreateAnimalAt(eco, 180, 100);
        animal.LookingAngle = 0; // looking right toward wall
        animal.VisionDistance = 100;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        var center = animal.RayResults[animal.RayResults.Length / 2];
        Assert.Equal(0.25, center.ObjectType); // wall
    }

    [Fact]
    public void AliveCreature_detected_in_center_ray()
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

        var center = looker.RayResults[looker.RayResults.Length / 2];
        Assert.Equal(1.0, center.ObjectType); // alive creature
    }

    [Fact]
    public void DeadCreature_detected_in_center_ray()
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

        var center = looker.RayResults[looker.RayResults.Length / 2];
        Assert.Equal(0.75, center.ObjectType); // dead creature
    }

    [Fact]
    public void All_ray_results_are_empty_when_nothing_detected()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.LookingAngle = 0;
        animal.VisionDistance = 50; // short range, nothing nearby
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        var center = animal.RayResults[animal.RayResults.Length / 2];
        Assert.Equal(0, center.ObjectType);
        Assert.Equal(0, center.ObjectDistance);
        Assert.Equal(0, center.ObjectEnergy);
    }

    [Fact]
    public void ObjectDistance_is_inverted_normalized_by_VisionDistance()
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

        var center = looker.RayResults[looker.RayResults.Length / 2];
        // Distance ~50 out of 200 = 0.25 normalized, inverted = 0.75
        Assert.True(center.ObjectDistance > 0.5, $"Expected > 0.5, got {center.ObjectDistance}");
        Assert.True(center.ObjectDistance <= 1.0);
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

        var center = animal.RayResults[animal.RayResults.Length / 2];
        Assert.Equal(0, center.ObjectType);
        Assert.Equal(0, center.ObjectDistance);
    }

    [Fact]
    public void ObjectDistance_does_not_cause_division_by_zero_when_VisionDistance_is_zero()
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
        var center = animal.RayResults[animal.RayResults.Length / 2];
        Assert.Equal(0, center.ObjectDistance);
    }
}
