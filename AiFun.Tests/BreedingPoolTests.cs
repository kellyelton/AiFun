using AiFun;

namespace AiFun.Tests;

public class BreedingPoolTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Ecosystem_has_TopBreeders_property()
    {
        var eco = CreateEcosystem();
        Assert.True(eco.TopBreeders >= 2, "TopBreeders should be at least 2");
    }

    [Fact]
    public void Ecosystem_TopBreeders_defaults_to_10()
    {
        var eco = CreateEcosystem();
        Assert.Equal(30, eco.TopBreeders);
    }

    [Fact]
    public void Ecosystem_TopBreeders_can_be_set()
    {
        var eco = CreateEcosystem();
        eco.TopBreeders = 20;
        Assert.Equal(20, eco.TopBreeders);
    }

    [Fact]
    public void Ecosystem_TopBreeders_clamped_to_minimum_2()
    {
        var eco = CreateEcosystem();
        eco.TopBreeders = 1;
        Assert.True(eco.TopBreeders >= 2, "TopBreeders should not go below 2");
    }
}
