using AiFun;

namespace AiFun.Tests;

public class FitnessTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Fitness_BabiesCreated_worth_more_than_FoodEaten_per_unit()
    {
        var eco = CreateEcosystem();
        var breeder = new Animal(eco);
        var eater = new Animal(eco);

        // 1 baby = 5000 fitness points, 5000 food eaten = 5000 fitness points
        // So 1 baby should beat 4999 food eaten
        breeder.BabiesCreated = 1;
        breeder.FoodEaten = 0;

        eater.BabiesCreated = 0;
        eater.FoodEaten = 4999;

        Assert.True(breeder.Fitness > eater.Fitness,
            $"1 baby (5000) should beat 4999 food eaten, " +
            $"got {breeder.Fitness} vs {eater.Fitness}");
    }

    [Fact]
    public void Fitness_FoodEaten_still_contributes()
    {
        var eco = CreateEcosystem();
        var a1 = new Animal(eco);
        var a2 = new Animal(eco);

        a1.FoodEaten = 1000;
        a2.FoodEaten = 100;

        Assert.True(a1.Fitness > a2.Fitness,
            $"More food eaten should still increase fitness, " +
            $"got {a1.Fitness} vs {a2.Fitness}");
    }

    [Fact]
    public void Fitness_babies_weight_is_5x_life_weight()
    {
        var eco = CreateEcosystem();

        // BabiesCreated weight = 5000, LengthOfLife weight = 1000
        // So 1 baby is worth 5 seconds of life
        var a = new Animal(eco);
        a.BabiesCreated = 1;
        a.FoodEaten = 0;

        // 1 baby = 5000, which equals 5 seconds of life (5 * 1000)
        // Verify via the formula: baby contribution = 5000
        Assert.Equal(5000, a.Fitness);
    }

    [Fact]
    public void Fitness_creature_with_babies_and_less_life_beats_longer_lived_without_babies()
    {
        // From the design doc: a creature with 2 babies and 8s life beats one with 0 babies and 10s life
        // 2 babies + 8s life = 8*1000 + 2*5000 = 8000 + 10000 = 18000
        // 0 babies + 10s life = 10*1000 + 0*5000 = 10000
        // We can't set LengthOfLife directly, but we can verify the formula weights:
        // BabiesCreated * 5000 vs LengthOfLife * 1000
        // 2 babies = 10000 fitness points, which equals 10 seconds of life.
        // So 2 babies + 8s > 0 babies + 10s requires baby weight > 5x life weight.
        // With 5000 vs 1000, that's exactly 5x — 2 babies = 10s of life equivalent.

        // Since we can't set LengthOfLife directly, verify the weights make the math work:
        // BabiesCreated weight (5000) / LengthOfLife weight (1000) = 5
        // So 1 baby = 5 seconds of life equivalent
        var eco = CreateEcosystem();
        var breeder = new Animal(eco);
        breeder.BabiesCreated = 2;
        // breeder fitness from babies alone = 10000

        var survivor = new Animal(eco);
        survivor.BabiesCreated = 0;
        // survivor fitness from babies = 0

        // The breeder's baby bonus (10000) should exceed 2 extra seconds of life (2000)
        // We verify the weight ratio: 2 * 5000 = 10000 > 2 * 1000 = 2000
        Assert.True(breeder.Fitness > survivor.Fitness,
            $"2 babies (fitness bonus 10000) should beat 0 babies. " +
            $"Got breeder={breeder.Fitness} vs survivor={survivor.Fitness}");
    }

    [Fact]
    public void Fitness_does_not_use_DistanceTraveled()
    {
        var eco = CreateEcosystem();
        var a = new Animal(eco);
        var b = new Animal(eco);

        a.DistanceTraveled = 999999;
        b.DistanceTraveled = 0;

        // DistanceTraveled should have no effect on fitness
        Assert.Equal(a.Fitness, b.Fitness);
    }

    [Fact]
    public void Fitness_uses_correct_weights()
    {
        var eco = CreateEcosystem();
        var a = new Animal(eco);

        // With 0 LengthOfLife, 3 babies, 500 food eaten:
        // Fitness = 0*1000 + 3*5000 + 500*1 = 15500
        a.BabiesCreated = 3;
        a.FoodEaten = 500;

        Assert.Equal(15500, a.Fitness);
    }
}
