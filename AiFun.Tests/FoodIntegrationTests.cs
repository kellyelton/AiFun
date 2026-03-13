using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class FoodIntegrationTests
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

    // --- Vision: FoodAhead input ---

    [Fact]
    public void FoodAhead_is_1_when_food_detected()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var food = new FoodPellet(eco);
        food.Location = new Rect(150, 100, 5, 5);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(food);

        looker.UpdateVision();

        Assert.Equal(1, looker.FoodAhead);
        Assert.Equal(0, looker.WallAhead);
        Assert.Equal(0, looker.AliveCreatureAhead);
        Assert.Equal(0, looker.DeadCreatureAhead);
    }

    [Fact]
    public void FoodAhead_is_0_when_no_food_detected()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.LookingAngle = 0;
        animal.VisionDistance = 50;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        Assert.Equal(0, animal.FoodAhead);
    }

    // --- Vision: FoodEnergyAhead input ---

    [Fact]
    public void FoodEnergyAhead_is_normalized_food_energy()
    {
        var eco = CreateEcosystem();
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

        // 250 / 500 = 0.5
        Assert.Equal(0.5, looker.FoodEnergyAhead, precision: 2);
    }

    [Fact]
    public void FoodEnergyAhead_is_0_when_no_food_detected()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.LookingAngle = 0;
        animal.VisionDistance = 50;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.UpdateVision();

        Assert.Equal(0, animal.FoodEnergyAhead);
    }

    // --- Neural network input count ---

    [Fact]
    public void Network_has_8_inputs_with_food()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);

        // 6 original + FoodAhead + FoodEnergyAhead = 8
        Assert.Equal(8, animal.Brain.InputCount);
        Assert.Equal(2, animal.Brain.OutputCount);
    }

    // --- Animal eating food ---

    [Fact]
    public void Animal_gains_energy_when_touching_food()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        eco.FoodMinStartEnergy = 200;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.AvailableEnergy = 500;
        var food = new FoodPellet(eco);
        food.Location = new Rect(100, 100, 5, 5); // overlapping
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(food);

        // Simulate touching
        animal.Touching.Add(food);
        animal.HandleTouching();

        Assert.Equal(600, animal.AvailableEnergy, precision: 1);
        Assert.Equal(100, food.Energy, precision: 1);
    }

    [Fact]
    public void Animal_eats_remaining_energy_if_less_than_bite()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        eco.FoodMinStartEnergy = 30;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.AvailableEnergy = 500;
        var food = new FoodPellet(eco);
        food.Location = new Rect(100, 100, 5, 5);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(food);

        animal.Touching.Add(food);
        animal.HandleTouching();

        Assert.Equal(530, animal.AvailableEnergy, precision: 1);
        Assert.True(food.IsConsumed);
    }

    [Fact]
    public void Food_spawns_during_ecosystem_update()
    {
        var eco = CreateEcosystem();
        eco.FoodTargetCount = 10;
        // Start with just animals
        eco.Reset();

        // After update, food should be spawned
        eco.Update(0.016);

        Assert.True(eco.FoodCount >= 10,
            $"Expected at least 10 food pellets, got {eco.FoodCount}");
    }

    [Fact]
    public void Consumed_food_is_removed_during_update()
    {
        var eco = CreateEcosystem();
        eco.FoodTargetCount = 0; // don't auto-spawn
        var food = new FoodPellet(eco);
        food.Location = new Rect(500, 500, 5, 5);
        eco.AnimateObjects.Add(food);
        food.Bite(food.Energy); // consume it

        eco.Update(0.016);

        Assert.Equal(0, eco.AnimateObjects.OfType<FoodPellet>().Count());
    }
}
