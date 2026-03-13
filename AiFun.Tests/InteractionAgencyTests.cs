using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class InteractionAgencyTests
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

    // --- Neural network output count ---

    [Fact]
    public void Network_has_4_outputs_with_desires()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);

        // Speed + TurnDeltaPerTick + EatDesire + BreedDesire = 4
        Assert.Equal(4, animal.Brain.OutputCount);
    }

    [Fact]
    public void Network_has_17_inputs()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);

        // 2 base + 5*3 rays = 17
        Assert.Equal(17, animal.Brain.InputCount);
    }

    // --- EatDesire / BreedDesire properties exist and are in [0, 1] ---

    [Fact]
    public void EatDesire_is_between_0_and_1()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        animal.Location = new Rect(500, 500, 5, 5);
        animal.Update(0.016);

        Assert.InRange(animal.EatDesire, 0, 1);
    }

    [Fact]
    public void BreedDesire_is_between_0_and_1()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        animal.Location = new Rect(500, 500, 5, 5);
        animal.Update(0.016);

        Assert.InRange(animal.BreedDesire, 0, 1);
    }

    // --- HandleTouching with EatDesire > BreedDesire: attempt eat ---

    [Fact]
    public void EatDesire_higher_kills_weaker_creature()
    {
        var eco = CreateEcosystem();
        var strong = CreateAnimalAt(eco, 100, 100);
        var weak = CreateAnimalAt(eco, 100, 100);
        strong.AvailableEnergy = 5000;
        weak.AvailableEnergy = 1000;
        strong.EatDesire = 0.8;
        strong.BreedDesire = 0.2;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(strong);
        eco.AnimateObjects.Add(weak);

        strong.Touching.Add(weak);
        strong.HandleTouching();

        Assert.True(weak.IsDead);
        Assert.False(weak.WasEaten, "Corpse should persist for scavenging, not instantly removed");
        Assert.True(weak.AvailableEnergy > 0, "Corpse retains energy for scavenging");
    }

    [Fact]
    public void Dead_creature_eaten_in_chunks_automatically()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var eater = CreateAnimalAt(eco, 100, 100);
        var corpse = CreateAnimalAt(eco, 100, 100);
        eater.AvailableEnergy = 3000;
        corpse.AvailableEnergy = 500;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(eater);
        eco.AnimateObjects.Add(corpse);

        eater.Touching.Add(corpse);
        var energyBefore = eater.AvailableEnergy;
        eater.HandleTouching();

        Assert.Equal(energyBefore + 100, eater.AvailableEnergy, precision: 1);
        Assert.Equal(400, corpse.AvailableEnergy, precision: 1);
    }

    // --- HandleTouching with BreedDesire > EatDesire: attempt breed ---

    [Fact]
    public void BreedDesire_higher_attempts_breeding_when_compatible()
    {
        var eco = CreateEcosystem();
        var female = CreateAnimalAt(eco, 100, 100);
        var male = CreateAnimalAt(eco, 100, 100);
        female.AvailableEnergy = 5000;
        male.AvailableEnergy = 5000;
        female.BreedDesire = 0.8;
        female.EatDesire = 0.2;
        male.BreedDesire = 0.8;
        male.EatDesire = 0.2;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(female);
        eco.AnimateObjects.Add(male);

        if (female.IsFemale && male.IsMale && !female.IsPregnant)
        {
            female.Touching.Add(male);
            female.HandleTouching();
            Assert.False(male.IsDead, "With BreedDesire > EatDesire, should breed not kill");
        }
    }

    // --- HandleTouching with equal desires: do nothing ---

    [Fact]
    public void Equal_desires_does_nothing_to_creature()
    {
        var eco = CreateEcosystem();
        var a = CreateAnimalAt(eco, 100, 100);
        var b = CreateAnimalAt(eco, 100, 100);
        a.AvailableEnergy = 5000;
        b.AvailableEnergy = 1000;
        a.EatDesire = 0.5;
        a.BreedDesire = 0.5;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(a);
        eco.AnimateObjects.Add(b);

        a.Touching.Add(b);
        a.HandleTouching();

        Assert.False(b.IsDead, "Equal desires should result in no action");
        Assert.False(a.IsPregnant);
    }

    // --- Food eating stays automatic (no agency) ---

    [Fact]
    public void Food_eating_is_automatic_regardless_of_desires()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        eco.FoodMinStartEnergy = 200;
        var animal = CreateAnimalAt(eco, 100, 100);
        animal.AvailableEnergy = 500;
        animal.EatDesire = 0.0;
        animal.BreedDesire = 1.0;
        var food = new FoodPellet(eco);
        food.Location = new Rect(100, 100, 5, 5);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);
        eco.AnimateObjects.Add(food);

        animal.Touching.Add(food);
        animal.HandleTouching();

        Assert.Equal(600, animal.AvailableEnergy, precision: 1);
    }

    // --- EatDesire high but weaker: can't kill stronger ---

    [Fact]
    public void EatDesire_high_but_weaker_creature_gets_killed_instead()
    {
        var eco = CreateEcosystem();
        var weak = CreateAnimalAt(eco, 100, 100);
        var strong = CreateAnimalAt(eco, 100, 100);
        weak.AvailableEnergy = 1000;
        strong.AvailableEnergy = 5000;
        weak.EatDesire = 0.9;
        weak.BreedDesire = 0.1;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(weak);
        eco.AnimateObjects.Add(strong);

        weak.Touching.Add(strong);
        weak.HandleTouching();

        Assert.True(weak.IsDead);
    }
}
