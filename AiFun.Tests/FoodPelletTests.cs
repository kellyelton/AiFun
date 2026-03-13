using AiFun;

namespace AiFun.Tests;

public class FoodPelletTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    // --- Growth ---

    [Fact]
    public void Food_grows_energy_over_time()
    {
        var eco = CreateEcosystem();
        var food = new FoodPellet(eco);
        var initialEnergy = food.Energy;

        food.Update(1.0); // 1 second

        Assert.True(food.Energy > initialEnergy,
            $"Food should grow. Was {initialEnergy}, now {food.Energy}");
    }

    [Fact]
    public void Food_growth_uses_ecosystem_rate()
    {
        var eco = CreateEcosystem();
        eco.FoodGrowthRate = 20;
        var food = new FoodPellet(eco);
        var initialEnergy = food.Energy;

        food.Update(1.0);

        Assert.Equal(initialEnergy + 20, food.Energy, precision: 1);
    }

    [Fact]
    public void Food_energy_caps_at_max()
    {
        var eco = CreateEcosystem();
        eco.FoodMaxEnergy = 100;
        eco.FoodMinStartEnergy = 90;
        var food = new FoodPellet(eco);

        food.Update(100.0); // way past max

        Assert.Equal(100, food.Energy, precision: 1);
    }

    [Fact]
    public void Food_starts_at_min_energy()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 75;
        var food = new FoodPellet(eco);

        Assert.Equal(75, food.Energy, precision: 1);
    }

    // --- Bite mechanics ---

    [Fact]
    public void Bite_removes_energy_from_pellet()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 200;
        var food = new FoodPellet(eco);

        var consumed = food.Bite(100);

        Assert.Equal(100, consumed, precision: 1);
        Assert.Equal(100, food.Energy, precision: 1);
    }

    [Fact]
    public void Bite_returns_remaining_energy_if_less_than_bite_size()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 30;
        var food = new FoodPellet(eco);

        var consumed = food.Bite(100);

        Assert.Equal(30, consumed, precision: 1);
        Assert.Equal(0, food.Energy, precision: 1);
    }

    [Fact]
    public void Food_is_consumed_when_energy_reaches_zero()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 50;
        var food = new FoodPellet(eco);

        food.Bite(50);

        Assert.True(food.IsConsumed);
    }

    [Fact]
    public void Food_does_not_grow_when_consumed()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 50;
        var food = new FoodPellet(eco);
        food.Bite(50);

        food.Update(10.0);

        Assert.Equal(0, food.Energy, precision: 1);
    }

    // --- Display ---

    [Fact]
    public void DisplaySize_scales_with_energy()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 50;
        eco.FoodMaxEnergy = 500;
        var food = new FoodPellet(eco);
        var smallSize = food.DisplaySize;

        food.Energy = 500;
        var bigSize = food.DisplaySize;

        Assert.True(bigSize > smallSize,
            $"Full energy size ({bigSize}) should be bigger than min energy size ({smallSize})");
    }

    [Fact]
    public void FillColor_gets_darker_with_more_energy()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 50;
        eco.FoodMaxEnergy = 500;
        var food = new FoodPellet(eco);
        var lightColor = food.FillColor;

        food.Energy = 500;
        var darkColor = food.FillColor;

        // Darker green = lower R value
        Assert.True(darkColor.R < lightColor.R,
            $"Full energy R ({darkColor.R}) should be less than min energy R ({lightColor.R})");
    }

    // --- Spawning in ecosystem ---

    [Fact]
    public void Ecosystem_spawns_food_to_meet_target_count()
    {
        var eco = CreateEcosystem();
        eco.FoodTargetCount = 5;
        // No food exists yet
        Assert.Equal(0, eco.FoodCount);

        eco.SpawnFoodToTarget();

        Assert.Equal(5, eco.FoodCount);
    }

    [Fact]
    public void Ecosystem_does_not_overspawn_food()
    {
        var eco = CreateEcosystem();
        eco.FoodTargetCount = 3;

        eco.SpawnFoodToTarget();
        eco.SpawnFoodToTarget(); // call again

        Assert.Equal(3, eco.FoodCount);
    }

    [Fact]
    public void Ecosystem_respawns_food_after_consumption()
    {
        var eco = CreateEcosystem();
        eco.FoodTargetCount = 5;
        eco.SpawnFoodToTarget();

        // Consume one pellet
        var pellet = eco.AnimateObjects.OfType<FoodPellet>().First();
        pellet.Bite(pellet.Energy);

        // Remove consumed food and respawn
        eco.RemoveConsumedFood();
        eco.SpawnFoodToTarget();

        Assert.Equal(5, eco.FoodCount);
    }

    // --- Location size scales with energy ---

    [Fact]
    public void Location_width_and_height_match_DisplaySize()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 50;
        eco.FoodMaxEnergy = 500;
        var food = new FoodPellet(eco);

        Assert.Equal(food.DisplaySize, food.Location.Width, precision: 1);
        Assert.Equal(food.DisplaySize, food.Location.Height, precision: 1);
    }

    [Fact]
    public void Location_grows_when_energy_increases()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 50;
        eco.FoodMaxEnergy = 500;
        eco.FoodGrowthRate = 450;
        var food = new FoodPellet(eco);
        var initialSize = food.Location.Width;

        food.Update(1.0); // grows to max

        Assert.True(food.Location.Width > initialSize,
            $"Location width should grow. Was {initialSize}, now {food.Location.Width}");
        Assert.True(food.Location.Height > initialSize,
            $"Location height should grow. Was {initialSize}, now {food.Location.Height}");
    }

    [Fact]
    public void Location_shrinks_when_bitten()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 400;
        eco.FoodMaxEnergy = 500;
        var food = new FoodPellet(eco);
        var bigSize = food.Location.Width;

        food.Bite(300);
        var smallSize = food.Location.Width;

        Assert.True(smallSize < bigSize,
            $"Location should shrink after bite. Was {bigSize}, now {smallSize}");
    }

    [Fact]
    public void Location_preserves_center_when_size_changes()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 50;
        eco.FoodMaxEnergy = 500;
        eco.FoodGrowthRate = 450;
        var food = new FoodPellet(eco);
        food.Location = new System.Windows.Rect(100, 200, food.DisplaySize, food.DisplaySize);
        var centerX = food.Location.Left + food.Location.Width / 2;
        var centerY = food.Location.Top + food.Location.Height / 2;

        food.Update(1.0); // grows to max

        var newCenterX = food.Location.Left + food.Location.Width / 2;
        var newCenterY = food.Location.Top + food.Location.Height / 2;

        Assert.Equal(centerX, newCenterX, precision: 1);
        Assert.Equal(centerY, newCenterY, precision: 1);
    }

    [Fact]
    public void Grown_food_is_easier_to_see_by_vision_ray()
    {
        // A ray that would miss a tiny pellet should hit a large one
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 500; // starts at max = largest size
        eco.FoodMaxEnergy = 500;
        var food = new FoodPellet(eco);
        // Place food at (100, 100) with large size (~16px)
        food.Location = new System.Windows.Rect(100, 100, food.DisplaySize, food.DisplaySize);
        eco.AnimateObjects.Add(food);

        // Ray at y=108 would miss a 5px-tall pellet at y=100, but hits a 16px-tall one
        var result = eco.ObjectAlongLine(0, new System.Windows.Point(50, 108), 300);

        Assert.Equal(VisionHitType.Food, result.HitType);
    }

    [Fact]
    public void Small_food_is_harder_to_see()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 1; // tiny
        eco.FoodMaxEnergy = 500;
        var food = new FoodPellet(eco);
        // Place food at (100, 100) with tiny size (~4px)
        food.Location = new System.Windows.Rect(100, 100, food.DisplaySize, food.DisplaySize);
        eco.AnimateObjects.Add(food);

        // Ray at y=108 should miss a 4px-tall pellet at y=100
        var result = eco.ObjectAlongLine(0, new System.Windows.Point(50, 108), 300);

        Assert.NotEqual(VisionHitType.Food, result.HitType);
    }

    // --- Vision detection ---

    [Fact]
    public void Vision_ray_detects_food_pellet()
    {
        var eco = CreateEcosystem();
        // Place food directly ahead (to the right, angle=0)
        var food = new FoodPellet(eco);
        food.Location = new System.Windows.Rect(100, 100, 5, 5);
        eco.AnimateObjects.Add(food);

        var result = eco.ObjectAlongLine(0, new System.Windows.Point(50, 102), 300);

        Assert.Equal(VisionHitType.Food, result.HitType);
        Assert.Same(food, result.HitObject);
    }

    [Fact]
    public void Vision_ray_returns_food_energy_via_hit_object()
    {
        var eco = CreateEcosystem();
        eco.FoodMinStartEnergy = 250;
        var food = new FoodPellet(eco);
        food.Location = new System.Windows.Rect(100, 100, 5, 5);
        eco.AnimateObjects.Add(food);

        var result = eco.ObjectAlongLine(0, new System.Windows.Point(50, 102), 300);

        Assert.Equal(VisionHitType.Food, result.HitType);
        var hitFood = result.HitObject as FoodPellet;
        Assert.NotNull(hitFood);
        Assert.Equal(250, hitFood.Energy, precision: 1);
    }
}
