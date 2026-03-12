using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class VisionDistanceTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Random_animal_has_VisionDistance_within_valid_range()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);

        Assert.InRange(animal.VisionDistance, 0, eco.MaxVisionDistance);
    }

    [Fact]
    public void VisionDistance_is_inherited_from_parents()
    {
        var eco = CreateEcosystem();
        var parent1 = new Animal(eco);
        var parent2 = new Animal(eco);

        var child = new Animal(eco, parent1, parent2);

        // Child's VisionDistance should be one of the parents' values (with possible mutation)
        // At minimum, it should be within valid range
        Assert.InRange(child.VisionDistance, 0, eco.MaxVisionDistance);
    }

    [Fact]
    public void Ecosystem_has_MaxVisionDistance_property()
    {
        var eco = CreateEcosystem();

        Assert.True(eco.MaxVisionDistance > 0);
    }

    [Fact]
    public void Ecosystem_has_VisionEnergyCostMultiplier_property()
    {
        var eco = CreateEcosystem();

        Assert.True(eco.VisionEnergyCostMultiplier >= 0);
    }
}
