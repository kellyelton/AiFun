using System.Windows;
using AiFun;

namespace AiFun.Tests;

/// <summary>
/// Tests that verify the DIRECTION of speed-vision coupling:
/// - High Speed (NN output) -> fewer active rays, shorter vision distance (tunnel vision)
/// - Low Speed (NN output) -> more active rays, full vision distance
/// - High TurnDeltaPerTick (spinning) should also reduce vision (combined with Speed)
/// </summary>
public class SpeedVisionCouplingDirectionTests
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

    // --- Direction verification: ensure coupling goes the RIGHT way ---

    [Fact]
    public void High_speed_produces_fewer_rays_than_low_speed()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;

        animal.Speed = 0;
        var raysAtZeroSpeed = animal.ComputeActiveRayCount();

        animal.Speed = 20;
        var raysAtMaxSpeed = animal.ComputeActiveRayCount();

        Assert.True(raysAtMaxSpeed < raysAtZeroSpeed,
            $"High speed should have fewer rays ({raysAtMaxSpeed}) than low speed ({raysAtZeroSpeed})");
    }

    [Fact]
    public void High_speed_produces_shorter_vision_than_low_speed()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;

        animal.Speed = 0;
        var visionAtZeroSpeed = animal.ComputeEffectiveVisionDistance();

        animal.Speed = 20;
        var visionAtMaxSpeed = animal.ComputeEffectiveVisionDistance();

        Assert.True(visionAtMaxSpeed < visionAtZeroSpeed,
            $"High speed should have shorter vision ({visionAtMaxSpeed}) than low speed ({visionAtZeroSpeed})");
    }

    // --- Spinning (TurnDeltaPerTick) DOES reduce vision ---

    [Fact]
    public void Spinning_in_place_with_low_speed_reduces_vision()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;
        animal.Speed = 0;
        animal.TurnDeltaPerTick = 10; // spinning at max turn speed -> turnFraction = 1.0

        var rays = animal.ComputeActiveRayCount();
        var vision = animal.ComputeEffectiveVisionDistance();

        // Max turn speed should reduce vision the same as max forward speed
        Assert.Equal(1, rays);
        Assert.Equal(50, vision, precision: 1);
    }

    [Fact]
    public void Spinning_in_place_disables_outer_rays_in_UpdateVision()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        // Place animal near right wall so center ray can detect wall within reduced vision
        var animal = CreateAnimalAt(eco, 170, 100);
        animal.LookingAngle = 0; // facing right wall
        animal.VisionDistance = 200;
        animal.Speed = 0;
        animal.TurnDeltaPerTick = 10; // spinning at max -> only center ray active

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Only center ray should be active
        Assert.Equal(0, animal.RayResults[0].ObjectType);
        Assert.Equal(0, animal.RayResults[1].ObjectType);
        Assert.True(animal.RayResults[2].ObjectType > 0, "Center ray should still be active");
        Assert.Equal(0, animal.RayResults[3].ObjectType);
        Assert.Equal(0, animal.RayResults[4].ObjectType);
    }

    // --- Cache consistency after Update() ---

    [Fact]
    public void Cached_vision_values_match_speed_after_multiple_updates()
    {
        var eco = CreateEcosystem(2000, 2000);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;
        eco.VisionEnergyCostMultiplier = 0;

        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;
        animal.AvailableEnergy = 100000;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        for (int tick = 0; tick < 10; tick++)
        {
            animal.Update(0.1);

            var expectedRays = animal.ComputeActiveRayCount();
            var expectedVision = animal.ComputeEffectiveVisionDistance();

            Assert.Equal(expectedRays, animal._activeRayCount);
            Assert.Equal(expectedVision, animal._effectiveVisionDistance, precision: 5);
        }
    }

    // --- Monotonic relationship: as Speed increases, rays decrease or stay same ---

    [Fact]
    public void Active_ray_count_never_increases_as_speed_increases()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;

        int prevRays = int.MaxValue;
        for (double speed = 0; speed <= 20; speed += 0.5)
        {
            animal.Speed = speed;
            var rays = animal.ComputeActiveRayCount();
            Assert.True(rays <= prevRays,
                $"At speed {speed}, rays ({rays}) should not exceed rays at lower speed ({prevRays})");
            prevRays = rays;
        }
    }

    [Fact]
    public void Effective_vision_distance_never_increases_as_speed_increases()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;

        double prevVision = double.MaxValue;
        for (double speed = 0; speed <= 20; speed += 0.5)
        {
            animal.Speed = speed;
            var vision = animal.ComputeEffectiveVisionDistance();
            Assert.True(vision <= prevVision,
                $"At speed {speed}, vision ({vision}) should not exceed vision at lower speed ({prevVision})");
            prevVision = vision;
        }
    }
}
