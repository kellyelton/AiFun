using AiFun;

namespace AiFun.Tests;

public class FitnessTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Fitness_incorporates_DistanceTraveled()
    {
        var eco = CreateEcosystem();
        var a1 = new Animal(eco);
        var a2 = new Animal(eco);

        // Simulate different distances
        a1.DistanceTraveled = 1000;
        a2.DistanceTraveled = 100;

        // The animal with more distance should have higher fitness
        Assert.True(a1.Fitness > a2.Fitness,
            $"Animal with DistanceTraveled=1000 should have higher fitness than DistanceTraveled=100, " +
            $"got {a1.Fitness} vs {a2.Fitness}");
    }

    [Fact]
    public void Fitness_is_not_just_LengthOfLife()
    {
        var eco = CreateEcosystem();
        var a = new Animal(eco);
        a.DistanceTraveled = 500;

        // Fitness should not equal LengthOfLife (which is 0 for a new animal)
        // because DistanceTraveled contributes to it
        Assert.True(a.Fitness > 0 || a.DistanceTraveled == 0,
            "Fitness should incorporate DistanceTraveled, not just LengthOfLife");
    }
}
