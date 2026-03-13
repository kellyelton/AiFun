using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class SpeedVisionCouplingTests
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

    // --- Effective vision distance scales with speed ---

    [Fact]
    public void Standing_still_gives_full_vision_distance()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 0; // speedFraction = 0

        // Place target at 180px ahead — should be within full 200px vision
        var target = CreateAnimalAt(eco, 280, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(target);

        animal.UpdateVision();

        var centerRay = animal.RayResults[2];
        Assert.Equal(1.0, centerRay.ObjectType); // alive creature detected
    }

    [Fact]
    public void Full_speed_reduces_vision_to_25_percent()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 20; // speedFraction = 1.0, effectiveVision = 200 * 0.25 = 50

        // Place target at 80px ahead — beyond effective vision of 50px
        var target = CreateAnimalAt(eco, 180, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(target);

        animal.UpdateVision();

        var centerRay = animal.RayResults[2];
        Assert.Equal(0, centerRay.ObjectType); // cannot see — out of effective range
    }

    [Fact]
    public void Full_speed_can_still_see_nearby_objects()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 20; // effectiveVision = 50

        // Place target at 30px ahead — within effective vision of 50px
        var target = CreateAnimalAt(eco, 130, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(target);

        animal.UpdateVision();

        var centerRay = animal.RayResults[2];
        Assert.Equal(1.0, centerRay.ObjectType); // alive creature detected
    }

    [Fact]
    public void Half_speed_gives_625_percent_vision()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 10; // speedFraction = 0.5, effectiveVision = 200 * 0.625 = 125

        // Place target at 130px ahead — beyond effective 125px
        var target = CreateAnimalAt(eco, 230, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(target);

        animal.UpdateVision();

        var centerRay = animal.RayResults[2];
        Assert.Equal(0, centerRay.ObjectType); // cannot see — out of effective range
    }

    [Fact]
    public void Half_speed_can_see_within_effective_range()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 10; // effectiveVision = 125

        // Place target at 100px ahead — within effective 125px
        var target = CreateAnimalAt(eco, 200, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(target);

        animal.UpdateVision();

        var centerRay = animal.RayResults[2];
        Assert.Equal(1.0, centerRay.ObjectType); // alive creature detected
    }

    // --- Active ray count reduces with speed ---

    [Fact]
    public void Low_speed_activates_all_5_rays()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 0; // all rays active

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // All 5 rays should detect the wall (small world)
        for (int i = 0; i < 5; i++)
        {
            Assert.True(animal.RayResults[i].ObjectType > 0,
                $"Ray {i} should detect something at speed 0, got ObjectType={animal.RayResults[i].ObjectType}");
        }
    }

    [Fact]
    public void Medium_speed_disables_outermost_rays_for_5_rays()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 10; // speedFraction = 0.5 -> center 3 rays active

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Outermost rays (0, 4) should be zeroed
        Assert.Equal(0, animal.RayResults[0].ObjectType);
        Assert.Equal(0, animal.RayResults[0].ObjectDistance);
        Assert.Equal(0, animal.RayResults[0].ObjectEnergy);

        Assert.Equal(0, animal.RayResults[4].ObjectType);
        Assert.Equal(0, animal.RayResults[4].ObjectDistance);
        Assert.Equal(0, animal.RayResults[4].ObjectEnergy);

        // Center 3 rays (1, 2, 3) should still detect wall
        Assert.True(animal.RayResults[1].ObjectType > 0, "Ray 1 should be active");
        Assert.True(animal.RayResults[2].ObjectType > 0, "Ray 2 (center) should be active");
        Assert.True(animal.RayResults[3].ObjectType > 0, "Ray 3 should be active");
    }

    [Fact]
    public void High_speed_leaves_only_center_ray_for_5_rays()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        // At speed 18, effectiveVision = 200 * (1 - 0.9*0.75) = 65px
        // Place animal close to wall so center ray detects it within effective range
        var animal = CreateAnimalAt(eco, 170, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 18; // speedFraction = 0.9 -> center 1 ray only

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Only center ray (2) should be active
        Assert.Equal(0, animal.RayResults[0].ObjectType);
        Assert.Equal(0, animal.RayResults[1].ObjectType);
        Assert.True(animal.RayResults[2].ObjectType > 0, "Center ray should still be active");
        Assert.Equal(0, animal.RayResults[3].ObjectType);
        Assert.Equal(0, animal.RayResults[4].ObjectType);
    }

    [Fact]
    public void Speed_just_below_first_threshold_keeps_all_rays_active()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 4; // speedFraction = 0.2 < 0.25 threshold -> all rays active

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // All rays should be active
        for (int i = 0; i < 5; i++)
        {
            Assert.True(animal.RayResults[i].ObjectType > 0,
                $"Ray {i} should detect something at speed 4");
        }
    }

    [Fact]
    public void Speed_at_first_threshold_disables_outer_pair()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 5; // speedFraction = 0.25 -> threshold hit, outer pair disabled

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Outermost pair (0, 4) disabled
        Assert.Equal(0, animal.RayResults[0].ObjectType);
        Assert.Equal(0, animal.RayResults[4].ObjectType);
        // Center 3 still active
        Assert.True(animal.RayResults[1].ObjectType > 0, "Ray 1 should be active");
        Assert.True(animal.RayResults[2].ObjectType > 0, "Ray 2 should be active");
        Assert.True(animal.RayResults[3].ObjectType > 0, "Ray 3 should be active");
    }

    // --- Ray disabling with different ray counts ---

    [Fact]
    public void Single_ray_always_active_at_any_speed()
    {
        // At max speed, effectiveVision = 200 * 0.25 = 50px
        // Place animal 30px from wall so wall is within effective range
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 1;
        var animal = CreateAnimalAt(eco, 170, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 20; // max speed

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Center (only) ray should still detect wall
        Assert.True(animal.RayResults[0].ObjectType > 0, "Single ray should always be active");
    }

    [Fact]
    public void Three_rays_outer_pair_disabled_at_medium_speed()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 3;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 10; // speedFraction = 0.5 -> outer pair disabled

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Outer rays (0, 2) disabled for 3-ray setup
        Assert.Equal(0, animal.RayResults[0].ObjectType);
        Assert.Equal(0, animal.RayResults[2].ObjectType);
        // Center ray (1) active
        Assert.True(animal.RayResults[1].ObjectType > 0, "Center ray should be active");
    }

    [Fact]
    public void Seven_rays_progressive_disabling()
    {
        // 7 rays: pairs to disable are (0,6), (1,5), (2,4) — center is 3
        // With 3 pairs, thresholds at 0.25, 0.5, 0.75
        // At max speed effectiveVision = 200 * 0.25 = 50px, so place animal near wall
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 7;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 170, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        // At moderate speed, outermost pair disabled
        // speedFraction = 0.35 >= 0.25 (first threshold), < 0.5 (second threshold)
        animal.Speed = 7;
        animal.UpdateVision();
        Assert.Equal(0, animal.RayResults[0].ObjectType);
        Assert.Equal(0, animal.RayResults[6].ObjectType);
        Assert.True(animal.RayResults[3].ObjectType > 0, "Center ray should be active");

        // At high speed, only center ray
        // speedFraction = 1.0 >= 0.75 (third threshold) -> all pairs disabled
        animal.Speed = 20;
        animal.UpdateVision();
        Assert.Equal(0, animal.RayResults[0].ObjectType);
        Assert.Equal(0, animal.RayResults[1].ObjectType);
        Assert.Equal(0, animal.RayResults[2].ObjectType);
        Assert.True(animal.RayResults[3].ObjectType > 0, "Center ray should always be active");
        Assert.Equal(0, animal.RayResults[4].ObjectType);
        Assert.Equal(0, animal.RayResults[5].ObjectType);
        Assert.Equal(0, animal.RayResults[6].ObjectType);
    }

    // --- ComputeEffectiveVisionDistance tests ---

    [Fact]
    public void EffectiveVision_at_zero_speed_equals_full_VisionDistance()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;
        animal.Speed = 0;

        Assert.Equal(200, animal.ComputeEffectiveVisionDistance(), precision: 2);
    }

    [Fact]
    public void EffectiveVision_at_full_speed_is_25_percent()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;
        animal.Speed = 20;

        Assert.Equal(50, animal.ComputeEffectiveVisionDistance(), precision: 2);
    }

    [Fact]
    public void EffectiveVision_at_half_speed_is_625_percent()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;
        animal.Speed = 10;

        Assert.Equal(125, animal.ComputeEffectiveVisionDistance(), precision: 2);
    }

    // --- ComputeActiveRayCount tests ---

    [Fact]
    public void ActiveRayCount_all_5_at_zero_speed()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.Speed = 0;

        Assert.Equal(5, animal.ComputeActiveRayCount());
    }

    [Fact]
    public void ActiveRayCount_3_at_medium_speed_for_5_rays()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.Speed = 10; // fraction 0.5 >= 0.25 threshold

        Assert.Equal(3, animal.ComputeActiveRayCount());
    }

    [Fact]
    public void ActiveRayCount_1_at_high_speed_for_5_rays()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.Speed = 15; // fraction 0.75 >= 0.75 threshold

        Assert.Equal(1, animal.ComputeActiveRayCount());
    }

    [Fact]
    public void ActiveRayCount_1_at_full_speed_for_5_rays()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.Speed = 20;

        Assert.Equal(1, animal.ComputeActiveRayCount());
    }

    // --- Vision energy cost uses effective values ---

    [Fact]
    public void Vision_energy_cost_formula_uses_effective_values()
    {
        // Test via Update() but only set Speed = 0 so mapper output doesn't affect much
        // At speed 0, effectiveVision = full, activeRays = all — same as before
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        eco.VisionEnergyCostMultiplier = 1.0;
        eco.BaseEnergyDrainPerSecond = 0;
        eco.MovementEnergyCostMultiplier = 0;

        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10000;
        animal.VisionDistance = 100;
        animal.Speed = 0;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        var energyBefore = animal.AvailableEnergy;
        animal.Update(1.0);
        var drain = energyBefore - animal.AvailableEnergy;

        // The mapper will set Speed to some NN output, but vision cost is computed using that speed.
        // Since NN is randomized, the exact drain varies. But it should be <= 500 (max when speed=0)
        // effectiveVision <= 100, activeRays <= 5, so drain <= 500
        Assert.True(drain <= 505, $"Vision drain ({drain}) should be at most ~500");
        Assert.True(drain > 0, "There should be some vision energy drain");
    }

    [Fact]
    public void Sprinting_is_cheaper_on_vision_than_standing_still()
    {
        // Test the helper methods directly since Update() changes Speed via mapper
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        eco.VisionEnergyCostMultiplier = 1.0;

        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 100;

        // Compute cost at speed 0
        animal.Speed = 0;
        var stationaryCost = animal.ComputeEffectiveVisionDistance() * animal.ComputeActiveRayCount() * eco.VisionEnergyCostMultiplier;

        // Compute cost at speed 20
        animal.Speed = 20;
        var sprintCost = animal.ComputeEffectiveVisionDistance() * animal.ComputeActiveRayCount() * eco.VisionEnergyCostMultiplier;

        // Sprinting should be much cheaper: 25 * 1 = 25 vs 100 * 5 = 500
        Assert.Equal(500, stationaryCost, precision: 1);
        Assert.Equal(25, sprintCost, precision: 1);
        Assert.True(sprintCost < stationaryCost);
    }

    // --- Disabled rays send zero inputs ---

    [Fact]
    public void Disabled_rays_have_all_zero_values()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 18; // high speed -> only center ray

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // All disabled rays should have ObjectType=0, ObjectDistance=0, ObjectEnergy=0
        for (int i = 0; i < 5; i++)
        {
            if (i == 2) continue; // center ray is active
            Assert.Equal(0, animal.RayResults[i].ObjectType);
            Assert.Equal(0, animal.RayResults[i].ObjectDistance);
            Assert.Equal(0, animal.RayResults[i].ObjectEnergy);
        }
    }

    // --- Speed clamping ---

    [Fact]
    public void Speed_above_max_is_treated_as_max_for_vision()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 25; // above max of 20

        // Should not crash and should behave like max speed
        var target = CreateAnimalAt(eco, 180, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(target);

        animal.UpdateVision();

        // Should behave like full speed (effectiveVision = 25% of 200 = 50)
        Assert.Equal(0, animal.RayResults[2].ObjectType); // target at 80px, beyond 50px effective range
    }

    [Fact]
    public void Negative_speed_treated_as_zero_for_vision()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = -5; // negative

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Should behave like speed 0 — all rays active, full vision
        for (int i = 0; i < 5; i++)
        {
            Assert.True(animal.RayResults[i].ObjectType > 0,
                $"Ray {i} should be active at negative speed (treated as 0)");
        }
    }
}
