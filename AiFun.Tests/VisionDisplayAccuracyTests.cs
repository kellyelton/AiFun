using System.Text.RegularExpressions;
using System.Windows;
using AiFun;

namespace AiFun.Tests;

/// <summary>
/// Tests verifying that the visual display of vision rays accurately reflects
/// the internal vision state. Investigates potential timing/staleness bugs where
/// _effectiveVisionDistance and _activeRayCount cached in UpdateVision() might
/// not match what BuildVisionRaysGeometry() renders.
/// </summary>
public class VisionDisplayAccuracyTests
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

    // ---------- Test 1: Cached values match computed values after UpdateVision() ----------

    [Fact]
    public void Cached_activeRayCount_matches_ComputeActiveRayCount_after_UpdateVision()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        foreach (var speed in new[] { 0, 3, 5, 10, 15, 20 })
        {
            animal.Speed = speed;
            animal.UpdateVision();

            var expected = animal.ComputeActiveRayCount();
            Assert.True(expected == animal._activeRayCount,
                $"At speed {speed}: cached _activeRayCount ({animal._activeRayCount}) " +
                $"should equal ComputeActiveRayCount() ({expected})");
        }
    }

    [Fact]
    public void Cached_effectiveVisionDistance_matches_ComputeEffectiveVisionDistance_after_UpdateVision()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        foreach (var speed in new[] { 0.0, 3.0, 5.0, 10.0, 15.0, 20.0 })
        {
            animal.Speed = speed;
            animal.UpdateVision();

            var expected = animal.ComputeEffectiveVisionDistance();
            Assert.True(Math.Abs(expected - animal._effectiveVisionDistance) < 0.0001,
                $"At speed {speed}: cached _effectiveVisionDistance ({animal._effectiveVisionDistance}) " +
                $"should equal ComputeEffectiveVisionDistance() ({expected})");
        }
    }

    // ---------- Test 2: BuildVisionRaysGeometry ray segment count matches _activeRayCount ----------

    [Fact]
    public void VisionRaysGeometry_segment_count_matches_activeRayCount_at_zero_speed()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 0;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();
        animal.RefreshBindings();

        var segmentCount = CountGeometrySegments(animal.VisionRaysGeometry);
        Assert.Equal(5, animal._activeRayCount);
        Assert.True(animal._activeRayCount == segmentCount,
            $"Geometry has {segmentCount} segments but _activeRayCount is {animal._activeRayCount}");
    }

    [Fact]
    public void VisionRaysGeometry_segment_count_matches_activeRayCount_at_medium_speed()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 10; // should disable outermost pair -> 3 active rays

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();
        animal.RefreshBindings();

        var segmentCount = CountGeometrySegments(animal.VisionRaysGeometry);
        Assert.Equal(3, animal._activeRayCount);
        Assert.True(animal._activeRayCount == segmentCount,
            $"Geometry has {segmentCount} segments but _activeRayCount is {animal._activeRayCount}");
    }

    [Fact]
    public void VisionRaysGeometry_segment_count_matches_activeRayCount_at_max_speed()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 170, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;
        animal.Speed = 20; // should disable all pairs -> 1 active ray (center only)

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();
        animal.RefreshBindings();

        var segmentCount = CountGeometrySegments(animal.VisionRaysGeometry);
        Assert.Equal(1, animal._activeRayCount);
        Assert.True(animal._activeRayCount == segmentCount,
            $"Geometry has {segmentCount} segments but _activeRayCount is {animal._activeRayCount}");
    }

    // ---------- Test 3: Cached values reflect CURRENT speed after speed change ----------

    [Fact]
    public void Cached_values_update_when_speed_changes_between_UpdateVision_calls()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 200;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        // First: low speed
        animal.Speed = 0;
        animal.UpdateVision();
        Assert.Equal(5, animal._activeRayCount);
        Assert.Equal(200, animal._effectiveVisionDistance, precision: 1);

        // Now change speed WITHOUT calling UpdateVision -- cached values are stale
        animal.Speed = 20;
        // Cached values should still reflect old speed (0)
        Assert.Equal(5, animal._activeRayCount);
        Assert.Equal(200, animal._effectiveVisionDistance, precision: 1);

        // Compute fresh values -- these should reflect new speed
        Assert.Equal(1, animal.ComputeActiveRayCount());
        Assert.Equal(50, animal.ComputeEffectiveVisionDistance(), precision: 1);

        // NOW call UpdateVision -- cached values should update
        animal.UpdateVision();
        Assert.Equal(1, animal._activeRayCount);
        Assert.Equal(50, animal._effectiveVisionDistance, precision: 1);
    }

    // ---------- Test 4: RefreshBindings uses stale cached values if Speed changed ----------

    [Fact]
    public void RefreshBindings_geometry_matches_last_UpdateVision_not_current_Speed()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        // Simulate: UpdateVision at low speed, then Speed changes but no UpdateVision
        animal.Speed = 0;
        animal.UpdateVision(); // caches: 5 rays, 200 distance

        // Speed changes (as NN would set it) but UpdateVision not called again
        animal.Speed = 20;

        // RefreshBindings will build geometry from CACHED values (stale -- from speed 0)
        animal.RefreshBindings();

        var segmentCount = CountGeometrySegments(animal.VisionRaysGeometry);
        // Geometry should have 5 segments (from cached values at speed 0)
        // even though current Speed is 20 (which would give 1 ray)
        Assert.Equal(5, segmentCount);
    }

    // ---------- Test 5: After full Update(), cached values match the NN-set Speed ----------

    [Fact]
    public void After_Update_cached_values_are_consistent_with_current_Speed()
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

        // Run Update() which calls _mapper.Update() then UpdateVision()
        animal.Update(0.1);

        // After Update, Speed was set by NN, then UpdateVision cached values for THAT speed
        var currentSpeed = animal.Speed;
        var expectedRays = animal.ComputeActiveRayCount();
        var expectedVision = animal.ComputeEffectiveVisionDistance();

        Assert.True(expectedRays == animal._activeRayCount,
            $"After Update(), cached _activeRayCount ({animal._activeRayCount}) must match " +
            $"ComputeActiveRayCount() ({expectedRays}) for Speed={currentSpeed}");
        Assert.True(Math.Abs(expectedVision - animal._effectiveVisionDistance) < 0.0001,
            $"After Update(), cached _effectiveVisionDistance ({animal._effectiveVisionDistance}) must match " +
            $"ComputeEffectiveVisionDistance() ({expectedVision}) for Speed={currentSpeed}");
    }

    // ---------- Test 6: The visual confusion scenario ----------

    [Fact]
    public void Spinning_creature_has_reduced_vision_like_fast_creature()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;

        // Creature A: spinning fast, low forward speed
        // turnFraction = 10/10 = 1.0, speedFraction = 2/20 = 0.1 -> combined = 1.0
        var spinner = CreateAnimalAt(eco, 170, 50);
        spinner.LookingAngle = 0;
        spinner.VisionDistance = 200;
        spinner.Speed = 2;
        spinner.TurnDeltaPerTick = 10; // max turn speed

        // Creature B: moving fast in straight line, not spinning
        // speedFraction = 18/20 = 0.9, turnFraction = 0 -> combined = 0.9
        var sprinter = CreateAnimalAt(eco, 170, 150);
        sprinter.LookingAngle = 0;
        sprinter.VisionDistance = 200;
        sprinter.Speed = 18;
        sprinter.TurnDeltaPerTick = 0;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(spinner);
        eco.AnimateObjects.Add(sprinter);

        spinner.UpdateVision();
        sprinter.UpdateVision();

        // Both should have heavily reduced vision
        // Spinner: combined fraction 1.0 -> 1 ray, 50px vision
        // Sprinter: combined fraction 0.9 -> 1 ray, 65px vision
        Assert.Equal(1, spinner._activeRayCount);
        Assert.Equal(1, sprinter._activeRayCount);

        // Spinner with max turn actually has slightly less vision than the fast-moving sprinter
        Assert.True(spinner._effectiveVisionDistance <= sprinter._effectiveVisionDistance,
            $"Spinner vision ({spinner._effectiveVisionDistance}) should not exceed sprinter vision ({sprinter._effectiveVisionDistance})");

        // Build geometry and verify both have 1 segment
        spinner.RefreshBindings();
        sprinter.RefreshBindings();

        var spinnerSegments = CountGeometrySegments(spinner.VisionRaysGeometry);
        var sprinterSegments = CountGeometrySegments(sprinter.VisionRaysGeometry);

        Assert.Equal(1, spinnerSegments);
        Assert.Equal(1, sprinterSegments);
    }

    // ---------- Test 7: Multiple simulation ticks between renders ----------

    [Fact]
    public void Multiple_updates_before_RefreshBindings_cached_values_match_final_state()
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

        // Run 10 updates without any RefreshBindings (simulating suppressed frames)
        for (int i = 0; i < 10; i++)
        {
            animal.Update(0.1);
        }

        // Now check that cached values match current Speed
        var finalSpeed = animal.Speed;
        var expectedRays = animal.ComputeActiveRayCount();
        var expectedVision = animal.ComputeEffectiveVisionDistance();

        Assert.True(expectedRays == animal._activeRayCount,
            $"After 10 updates, cached _activeRayCount ({animal._activeRayCount}) should match for Speed={finalSpeed}");
        Assert.True(Math.Abs(expectedVision - animal._effectiveVisionDistance) < 0.0001,
            $"After 10 updates, cached _effectiveVisionDistance should match for Speed={finalSpeed}");

        // Now call RefreshBindings and verify geometry matches
        animal.RefreshBindings();

        var segmentCount = CountGeometrySegments(animal.VisionRaysGeometry);
        Assert.True(expectedRays == segmentCount,
            $"Geometry segments ({segmentCount}) should match _activeRayCount ({expectedRays}) after batched updates");
    }

    // ---------- Test 8: BuildVisionRaysGeometry uses _effectiveVisionDistance not VisionDistance ----------

    [Fact]
    public void Geometry_ray_length_scales_with_effective_vision_not_base_vision()
    {
        var eco = CreateEcosystem(2000, 2000);
        eco.VisionRayCount = 1; // single ray for simplicity
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.LookingAngle = 0;
        animal.VisionDistance = 200;

        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        // At speed 0: effectiveVision = 200, ray with no hit -> length = 200
        animal.Speed = 0;
        animal.UpdateVision();
        animal.RefreshBindings();
        var geometryAtZeroSpeed = animal.VisionRaysGeometry;
        var maxLengthAtZero = ExtractMaxRayLength(geometryAtZeroSpeed);

        // At speed 20: effectiveVision = 50, ray with no hit -> length = 50
        animal.Speed = 20;
        animal.UpdateVision();
        animal.RefreshBindings();
        var geometryAtMaxSpeed = animal.VisionRaysGeometry;
        var maxLengthAtMax = ExtractMaxRayLength(geometryAtMaxSpeed);

        Assert.True(maxLengthAtZero > maxLengthAtMax,
            $"Ray length at speed 0 ({maxLengthAtZero:F1}) should be longer than at speed 20 ({maxLengthAtMax:F1})");

        // Verify approximate ratio: 200/50 = 4x
        if (maxLengthAtMax > 0)
        {
            var ratio = maxLengthAtZero / maxLengthAtMax;
            Assert.True(ratio > 3.0 && ratio < 5.0,
                $"Ray length ratio should be ~4x, got {ratio:F1}");
        }
    }

    // ---------- Helper methods ----------

    private int CountGeometrySegments(string geometry)
    {
        if (string.IsNullOrEmpty(geometry)) return 0;
        return Regex.Matches(geometry, @"M\s+0,0\s+L\s+").Count;
    }

    private double ExtractMaxRayLength(string geometry)
    {
        if (string.IsNullOrEmpty(geometry)) return 0;
        double maxLength = 0;
        var matches = Regex.Matches(geometry, @"L\s+([-\d.]+),([-\d.]+)");
        foreach (Match m in matches)
        {
            var x = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var y = double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            var length = Math.Sqrt(x * x + y * y);
            if (length > maxLength) maxLength = length;
        }
        return maxLength;
    }
}
