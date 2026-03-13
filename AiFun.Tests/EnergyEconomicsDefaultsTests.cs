using AiFun;

namespace AiFun.Tests;

public class EnergyEconomicsDefaultsTests
{
    private Ecosystem CreateEcosystem()
    {
        return new Ecosystem(2000, 2000);
    }

    [Fact]
    public void BaseEnergyDrainPerSecond_defaults_to_30()
    {
        var eco = CreateEcosystem();
        Assert.Equal(30, eco.BaseEnergyDrainPerSecond);
    }

    [Fact]
    public void FoodMinStartEnergy_defaults_to_200()
    {
        var eco = CreateEcosystem();
        Assert.Equal(200, eco.FoodMinStartEnergy);
    }

    [Fact]
    public void FoodGrowthRate_defaults_to_10()
    {
        var eco = CreateEcosystem();
        Assert.Equal(10, eco.FoodGrowthRate);
    }

    [Fact]
    public void FoodMaxEnergy_defaults_to_2000()
    {
        var eco = CreateEcosystem();
        Assert.Equal(2000, eco.FoodMaxEnergy);
    }

    [Fact]
    public void FoodBiteSize_defaults_to_400()
    {
        var eco = CreateEcosystem();
        Assert.Equal(400, eco.FoodBiteSize);
    }

    [Fact]
    public void FoodTargetCount_defaults_to_150()
    {
        var eco = CreateEcosystem();
        Assert.Equal(150, eco.FoodTargetCount);
    }

    [Fact]
    public void MovementEnergyCostMultiplier_defaults_to_3()
    {
        var eco = CreateEcosystem();
        Assert.Equal(3, eco.MovementEnergyCostMultiplier);
    }
}
