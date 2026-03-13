using AiFun;

namespace AiFun.Tests;

public class BreedingPoolTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Ecosystem_TopBreeders_no_longer_exists()
    {
        var eco = CreateEcosystem();
        var type = eco.GetType();
        var prop = type.GetProperty("TopBreeders");
        Assert.Null(prop);
    }
}
