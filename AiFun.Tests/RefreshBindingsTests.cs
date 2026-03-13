using System.ComponentModel;
using AiFun;
using AiFun.Entities;

namespace AiFun.Tests;

public class RefreshBindingsTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    [Fact]
    public void Animal_RefreshBindings_RaisesIsDeadPropertyChanged()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        var raisedProperties = new List<string>();
        animal.PropertyChanged += (s, e) => raisedProperties.Add(e.PropertyName!);

        animal.RefreshBindings();

        Assert.Contains("IsDead", raisedProperties);
    }

    [Fact]
    public void Animal_RefreshBindings_RaisesIsPregnantPropertyChanged()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        var raisedProperties = new List<string>();
        animal.PropertyChanged += (s, e) => raisedProperties.Add(e.PropertyName!);

        animal.RefreshBindings();

        Assert.Contains("IsPregnant", raisedProperties);
    }

    [Fact]
    public void Animal_RefreshBindings_RaisesAvailableEnergyPropertyChanged()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        var raisedProperties = new List<string>();
        animal.PropertyChanged += (s, e) => raisedProperties.Add(e.PropertyName!);

        animal.RefreshBindings();

        Assert.Contains("AvailableEnergy", raisedProperties);
    }

    [Fact]
    public void Animal_RefreshBindings_StillRaisesVisualProperties()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        var raisedProperties = new List<string>();
        animal.PropertyChanged += (s, e) => raisedProperties.Add(e.PropertyName!);

        animal.RefreshBindings();

        Assert.Contains("LookingAngle", raisedProperties);
        Assert.Contains("BodyColor", raisedProperties);
        Assert.Contains("StrokeColor", raisedProperties);
        Assert.Contains("Left", raisedProperties);
        Assert.Contains("Top", raisedProperties);
    }

    [Fact]
    public void Animal_StateChange_DuringSuppression_VisibleAfterRefreshBindings()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);

        // Suppress notifications and change energy
        AiFun.Entities.Object.SuppressNotifications = true;
        animal.AvailableEnergy = 42;
        AiFun.Entities.Object.SuppressNotifications = false;

        // Now collect what RefreshBindings raises
        var raisedProperties = new List<string>();
        animal.PropertyChanged += (s, e) => raisedProperties.Add(e.PropertyName!);
        animal.RefreshBindings();

        Assert.Contains("AvailableEnergy", raisedProperties);
        Assert.Equal(42, animal.AvailableEnergy);
    }

    [Fact]
    public void FoodPellet_RefreshBindings_RaisesEnergyAndDisplayProperties()
    {
        var eco = CreateEcosystem();
        var food = new FoodPellet(eco);
        var raisedProperties = new List<string>();
        food.PropertyChanged += (s, e) => raisedProperties.Add(e.PropertyName!);

        food.RefreshBindings();

        Assert.Contains("Energy", raisedProperties);
        Assert.Contains("DisplaySize", raisedProperties);
        Assert.Contains("FillColor", raisedProperties);
    }
}
