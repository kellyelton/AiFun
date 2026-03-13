using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class MultiRayVisionTests
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

    // --- Ecosystem parameters ---

    [Fact]
    public void Ecosystem_has_VisionRayCount_default_5()
    {
        var eco = CreateEcosystem();
        Assert.Equal(5, eco.VisionRayCount);
    }

    [Fact]
    public void Ecosystem_has_VisionFieldOfView_default_120()
    {
        var eco = CreateEcosystem();
        Assert.Equal(120, eco.VisionFieldOfView);
    }

    [Fact]
    public void VisionRayCount_minimum_is_1()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 0;
        Assert.Equal(1, eco.VisionRayCount);
    }

    [Fact]
    public void VisionFieldOfView_minimum_is_1()
    {
        var eco = CreateEcosystem();
        eco.VisionFieldOfView = 0;
        Assert.Equal(1, eco.VisionFieldOfView);
    }

    // --- RayResult struct ---

    [Fact]
    public void RayResult_has_ObjectType_ObjectDistance_ObjectEnergy()
    {
        var result = new RayResult();
        Assert.Equal(0, result.ObjectType);
        Assert.Equal(0, result.ObjectDistance);
        Assert.Equal(0, result.ObjectEnergy);
    }

    [Fact]
    public void RayResult_ObjectType_values_match_design()
    {
        // 0=nothing, 0.25=wall, 0.5=food, 0.75=dead creature, 1.0=alive creature
        var nothing = new RayResult { ObjectType = 0 };
        var wall = new RayResult { ObjectType = 0.25 };
        var food = new RayResult { ObjectType = 0.5 };
        var dead = new RayResult { ObjectType = 0.75 };
        var alive = new RayResult { ObjectType = 1.0 };

        Assert.Equal(0, nothing.ObjectType);
        Assert.Equal(0.25, wall.ObjectType);
        Assert.Equal(0.5, food.ObjectType);
        Assert.Equal(0.75, dead.ObjectType);
        Assert.Equal(1.0, alive.ObjectType);
    }

    // --- Animal RayResults array ---

    [Fact]
    public void Animal_has_RayResults_array_matching_VisionRayCount()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = new Animal(eco);

        Assert.NotNull(animal.RayResults);
        Assert.Equal(5, animal.RayResults.Length);
    }

    [Fact]
    public void Animal_RayResults_length_matches_ecosystem_VisionRayCount_3()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 3;
        var animal = new Animal(eco);

        Assert.Equal(3, animal.RayResults.Length);
    }

    [Fact]
    public void Animal_RayResults_length_matches_ecosystem_VisionRayCount_7()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 7;
        var animal = new Animal(eco);

        Assert.Equal(7, animal.RayResults.Length);
    }

    // --- Neural network input/output counts ---

    [Fact]
    public void Network_has_17_inputs_for_5_rays_and_4_outputs()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = new Animal(eco);

        // 2 (energy, angle) + 5*3 (ray data) = 17
        Assert.Equal(17, animal.Brain.InputCount);
        Assert.Equal(4, animal.Brain.OutputCount);
    }

    [Fact]
    public void Network_has_11_inputs_for_3_rays()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 3;
        var animal = new Animal(eco);

        // 2 + 3*3 = 11
        Assert.Equal(11, animal.Brain.InputCount);
    }

    [Fact]
    public void Network_has_23_inputs_for_7_rays()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 7;
        var animal = new Animal(eco);

        // 2 + 7*3 = 23
        Assert.Equal(23, animal.Brain.InputCount);
    }

    // --- Multi-ray vision detection ---

    [Fact]
    public void Center_ray_detects_object_directly_ahead()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0; // looking right
        looker.VisionDistance = 200;
        var target = CreateAnimalAt(eco, 150, 100); // directly ahead
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        // Center ray is index 2 (for 5 rays: 0,1,2,3,4)
        var centerRay = looker.RayResults[2];
        Assert.Equal(1.0, centerRay.ObjectType); // alive creature
        Assert.True(centerRay.ObjectDistance > 0);
    }

    [Fact]
    public void Wall_ahead_produces_ObjectType_025()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 180, 100);
        animal.LookingAngle = 0; // looking right toward wall
        animal.VisionDistance = 100;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        var centerRay = animal.RayResults[2];
        Assert.Equal(0.25, centerRay.ObjectType);
        Assert.True(centerRay.ObjectDistance > 0);
    }

    [Fact]
    public void Food_ahead_produces_ObjectType_05()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var food = new FoodPellet(eco);
        food.Location = new Rect(150, 100, 5, 5);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(food);

        looker.UpdateVision();

        var centerRay = looker.RayResults[2];
        Assert.Equal(0.5, centerRay.ObjectType);
    }

    [Fact]
    public void Dead_creature_ahead_produces_ObjectType_075()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
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

        var centerRay = looker.RayResults[2];
        Assert.Equal(0.75, centerRay.ObjectType);
    }

    [Fact]
    public void Nothing_ahead_produces_ObjectType_0()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.LookingAngle = 0;
        animal.VisionDistance = 50;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        var centerRay = animal.RayResults[2];
        Assert.Equal(0, centerRay.ObjectType);
        Assert.Equal(0, centerRay.ObjectDistance);
        Assert.Equal(0, centerRay.ObjectEnergy);
    }

    // --- ObjectEnergy ---

    [Fact]
    public void Food_ObjectEnergy_is_normalized_energy()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        eco.FoodMaxEnergy = 500;
        eco.FoodMinStartEnergy = 250;
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var food = new FoodPellet(eco);
        food.Location = new Rect(150, 100, 5, 5);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(food);

        looker.UpdateVision();

        var centerRay = looker.RayResults[2];
        Assert.Equal(0.5, centerRay.ObjectEnergy, precision: 2);
    }

    [Fact]
    public void Wall_ObjectEnergy_is_0()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 180, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 100;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        var centerRay = animal.RayResults[2];
        Assert.Equal(0, centerRay.ObjectEnergy);
    }

    [Fact]
    public void AliveCreature_ObjectEnergy_is_0()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var target = CreateAnimalAt(eco, 150, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        var centerRay = looker.RayResults[2];
        Assert.Equal(0, centerRay.ObjectEnergy);
    }

    [Fact]
    public void DeadCreature_ObjectEnergy_is_normalized()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var target = CreateAnimalAt(eco, 150, 100);
        target.AvailableEnergy = 5000; // half of 10000 max
        target.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        var centerRay = looker.RayResults[2];
        Assert.Equal(0.5, centerRay.ObjectEnergy, precision: 2);
    }

    // --- Peripheral ray detection ---

    [Fact]
    public void Peripheral_ray_detects_object_to_the_side()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        eco.VisionFieldOfView = 120;
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0; // looking right
        looker.VisionDistance = 200;

        // Place target at ~30 degrees below (sin(30)=0.5), at about 100px away
        // At angle=30 from heading, ray[3] should be at +30 degrees
        var targetX = 100 + Math.Cos(30 * Math.PI / 180) * 80; // ~169
        var targetY = 100 + Math.Sin(30 * Math.PI / 180) * 80; // ~140
        var target = CreateAnimalAt(eco, targetX, targetY);
        target.Location = new Rect(targetX, targetY, 10, 10); // larger for reliable hit
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        // Ray index 3 is +30 degrees for 5 rays at 120 FOV
        var rightRay = looker.RayResults[3];
        Assert.Equal(1.0, rightRay.ObjectType); // alive creature
    }

    // --- Vision energy cost formula ---

    [Fact]
    public void Vision_energy_cost_includes_ray_count()
    {
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

        // Vision drain = effectiveVision * activeRayCount * VisionEnergyCostMultiplier * time
        // After mapper runs, Speed changes so effective values vary.
        // Max drain (speed=0): 100 * 5 * 1.0 * 1.0 = 500
        // Min drain (speed=20): 25 * 1 * 1.0 * 1.0 = 25
        Assert.True(drain > 0, "Vision should cost some energy");
        Assert.True(drain <= 505, $"Vision drain ({drain}) should not exceed max");
    }

    // --- VisionEnergyCostMultiplier default ---

    [Fact]
    public void VisionEnergyCostMultiplier_default_is_005()
    {
        var eco = CreateEcosystem();
        Assert.Equal(0.05, eco.VisionEnergyCostMultiplier);
    }

    // --- Hidden layer capped at 16 ---

    [Fact]
    public void Hidden_layer_size_capped_at_16()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 7; // 23 inputs
        var animal = new Animal(eco);

        // Hidden layers should be capped at 16, not the full input count (23)
        var brain = animal.Brain;
        // The hidden layer (if any) should have at most 16 neurons
        if (brain.LayerCount > 2)
        {
            var hiddenSize = brain.GetLayerNeuronCount(1);
            Assert.True(hiddenSize <= 16, $"Hidden layer size {hiddenSize} exceeds cap of 16");
        }
    }

    // --- Old properties removed ---

    [Fact]
    public void VisionRayColor_uses_center_ray_for_display()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 50;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        animal.UpdateVision();

        // Should not throw, and should return a valid color
        var color = animal.VisionRayColor;
        Assert.NotNull(color);
    }

    [Fact]
    public void VisionRayDisplayLength_uses_center_ray()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 100;
        animal.Speed = 0; // ensure full effective vision
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        animal.UpdateVision();

        // When nothing detected and speed=0, display length = VisionDistance
        Assert.Equal(100, animal.VisionRayDisplayLength, precision: 1);
    }

    // --- Zero vision distance ---

    [Fact]
    public void Zero_VisionDistance_produces_empty_ray_results()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.VisionDistance = 0;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        foreach (var ray in animal.RayResults)
        {
            Assert.Equal(0, ray.ObjectType);
            Assert.Equal(0, ray.ObjectDistance);
            Assert.Equal(0, ray.ObjectEnergy);
        }
    }

    // --- 3-ray angle spread ---

    [Fact]
    public void Three_rays_spread_at_correct_angles()
    {
        // With 3 rays and 120 FOV: offsets = -60, 0, +60
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 3;
        eco.VisionFieldOfView = 120;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.LookingAngle = 0; // looking right
        animal.VisionDistance = 200;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // All 3 rays should detect the wall (small world)
        foreach (var ray in animal.RayResults)
        {
            Assert.True(ray.ObjectType == 0.25, $"Each ray should see wall, got {ray.ObjectType}");
        }
    }

    // --- FocusingObject from center ray (no redundant ray cast) ---

    [Fact]
    public void FocusingObject_set_from_center_ray_when_creature_ahead()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var target = CreateAnimalAt(eco, 150, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        Assert.Same(target, looker.FocusingObject);
    }

    [Fact]
    public void FocusingObject_null_when_nothing_ahead()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.LookingAngle = 0;
        animal.VisionDistance = 50;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        Assert.Null(animal.FocusingObject);
    }

    [Fact]
    public void FocusingObject_null_when_wall_ahead()
    {
        var eco = CreateEcosystem(200, 200);
        eco.VisionRayCount = 5;
        var animal = CreateAnimalAt(eco, 180, 100);
        animal.LookingAngle = 0;
        animal.VisionDistance = 100;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        // Walls have no HitObject
        Assert.Null(animal.FocusingObject);
    }

    // --- VisionResult is a value type (struct) ---

    [Fact]
    public void VisionResult_is_a_struct()
    {
        Assert.True(typeof(VisionResult).IsValueType, "VisionResult should be a struct to avoid GC pressure");
    }
}
