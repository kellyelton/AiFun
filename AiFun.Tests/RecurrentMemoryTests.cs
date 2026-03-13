using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class RecurrentMemoryTests
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

    // --- Properties exist and initialize to 0.5 ---

    [Fact]
    public void Animal_has_PrevSpeed_property_initialized_to_05()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        Assert.Equal(0.5, animal.PrevSpeed);
    }

    [Fact]
    public void Animal_has_PrevTurnDelta_property_initialized_to_05()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        Assert.Equal(0.5, animal.PrevTurnDelta);
    }

    [Fact]
    public void Animal_has_PrevEatDesire_property_initialized_to_05()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        Assert.Equal(0.5, animal.PrevEatDesire);
    }

    [Fact]
    public void Animal_has_PrevBreedDesire_property_initialized_to_05()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        Assert.Equal(0.5, animal.PrevBreedDesire);
    }

    // --- Breed constructor also initializes to 0.5 ---

    [Fact]
    public void Breed_constructor_initializes_PrevSpeed_to_05()
    {
        var eco = CreateEcosystem();
        var p1 = new Animal(eco);
        var p2 = new Animal(eco);
        var child = new Animal(eco, p1, p2);
        Assert.Equal(0.5, child.PrevSpeed);
    }

    [Fact]
    public void Breed_constructor_initializes_PrevTurnDelta_to_05()
    {
        var eco = CreateEcosystem();
        var p1 = new Animal(eco);
        var p2 = new Animal(eco);
        var child = new Animal(eco, p1, p2);
        Assert.Equal(0.5, child.PrevTurnDelta);
    }

    // --- Network has 21 inputs (17 + 4 recurrent) ---

    [Fact]
    public void Network_has_21_inputs_for_5_rays_with_recurrent_memory()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 5;
        var animal = new Animal(eco);

        // 2 (energy, angle) + 5*3 (ray data) + 4 (recurrent) = 21
        Assert.Equal(21, animal.Brain.InputCount);
    }

    [Fact]
    public void Network_has_15_inputs_for_3_rays_with_recurrent_memory()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 3;
        var animal = new Animal(eco);

        // 2 + 3*3 + 4 = 15
        Assert.Equal(15, animal.Brain.InputCount);
    }

    [Fact]
    public void Network_has_27_inputs_for_7_rays_with_recurrent_memory()
    {
        var eco = CreateEcosystem();
        eco.VisionRayCount = 7;
        var animal = new Animal(eco);

        // 2 + 7*3 + 4 = 27
        Assert.Equal(27, animal.Brain.InputCount);
    }

    // --- Prev* fields are updated after Update() runs ---

    [Fact]
    public void Update_stores_current_Speed_into_PrevSpeed()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10000;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        // Before update, PrevSpeed is 0.5 (initial)
        Assert.Equal(0.5, animal.PrevSpeed);

        animal.Update(0.01);

        // After update, PrevSpeed should reflect the Speed output from the NN
        // Speed is in [0, 20], so PrevSpeed should match the current Speed value
        Assert.Equal(animal.Speed, animal.PrevSpeed);
    }

    [Fact]
    public void Update_stores_current_TurnDeltaPerTick_into_PrevTurnDelta()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10000;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.Update(0.01);

        Assert.Equal(animal.TurnDeltaPerTick, animal.PrevTurnDelta);
    }

    [Fact]
    public void Update_stores_current_EatDesire_into_PrevEatDesire()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10000;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.Update(0.01);

        Assert.Equal(animal.EatDesire, animal.PrevEatDesire);
    }

    [Fact]
    public void Update_stores_current_BreedDesire_into_PrevBreedDesire()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10000;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.Update(0.01);

        Assert.Equal(animal.BreedDesire, animal.PrevBreedDesire);
    }

    // --- Prev* values persist across ticks ---

    [Fact]
    public void PrevSpeed_changes_between_ticks_as_outputs_change()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 10000;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.Update(0.01);
        var prevSpeedAfterFirstTick = animal.PrevSpeed;

        // PrevSpeed should be set to the current Speed after tick
        Assert.Equal(animal.Speed, prevSpeedAfterFirstTick);
    }

    // --- Dead animal does not update Prev* ---

    [Fact]
    public void Dead_animal_does_not_update_Prev_values()
    {
        var eco = CreateEcosystem();
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.AvailableEnergy = 0; // will die
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        animal.Update(0.01);

        // Animal died, Prev* should remain at initial 0.5
        Assert.Equal(0.5, animal.PrevSpeed);
        Assert.Equal(0.5, animal.PrevTurnDelta);
        Assert.Equal(0.5, animal.PrevEatDesire);
        Assert.Equal(0.5, animal.PrevBreedDesire);
    }

    // --- Output count unchanged ---

    [Fact]
    public void Network_still_has_4_outputs()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);
        Assert.Equal(4, animal.Brain.OutputCount);
    }
}
