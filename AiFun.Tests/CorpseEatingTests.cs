using System.Windows;
using AiFun;

namespace AiFun.Tests;

public class CorpseEatingTests
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

    // --- Bite mechanic: dead creatures eaten in chunks ---

    [Fact]
    public void Eating_dead_creature_takes_FoodBiteSize_energy()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var eater = CreateAnimalAt(eco, 100, 100);
        eater.AvailableEnergy = 500;
        var corpse = CreateAnimalAt(eco, 100, 100);
        corpse.AvailableEnergy = 300;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(eater);
        eco.AnimateObjects.Add(corpse);

        eater.Touching.Add(corpse);
        eater.HandleTouching();

        // Eater gains FoodBiteSize (100), not flat 1000
        Assert.Equal(600, eater.AvailableEnergy, precision: 1);
        // Corpse loses that energy
        Assert.Equal(200, corpse.AvailableEnergy, precision: 1);
    }

    [Fact]
    public void Eating_dead_creature_takes_remaining_if_less_than_bite()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var eater = CreateAnimalAt(eco, 100, 100);
        eater.AvailableEnergy = 500;
        var corpse = CreateAnimalAt(eco, 100, 100);
        corpse.AvailableEnergy = 30;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(eater);
        eco.AnimateObjects.Add(corpse);

        eater.Touching.Add(corpse);
        eater.HandleTouching();

        // Only 30 available, so gains 30
        Assert.Equal(530, eater.AvailableEnergy, precision: 1);
        Assert.Equal(0, corpse.AvailableEnergy, precision: 1);
    }

    [Fact]
    public void Corpse_marked_WasEaten_when_energy_depleted()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 200;
        var eater = CreateAnimalAt(eco, 100, 100);
        eater.AvailableEnergy = 500;
        var corpse = CreateAnimalAt(eco, 100, 100);
        corpse.AvailableEnergy = 50;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(eater);
        eco.AnimateObjects.Add(corpse);

        eater.Touching.Add(corpse);
        eater.HandleTouching();

        Assert.True(corpse.WasEaten);
    }

    [Fact]
    public void Corpse_not_marked_WasEaten_when_energy_remains()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var eater = CreateAnimalAt(eco, 100, 100);
        eater.AvailableEnergy = 500;
        var corpse = CreateAnimalAt(eco, 100, 100);
        corpse.AvailableEnergy = 500;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(eater);
        eco.AnimateObjects.Add(corpse);

        eater.Touching.Add(corpse);
        eater.HandleTouching();

        Assert.False(corpse.WasEaten, "Corpse still has energy, should not be marked eaten");
    }

    [Fact]
    public void Corpse_with_zero_energy_is_not_eaten()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var eater = CreateAnimalAt(eco, 100, 100);
        eater.AvailableEnergy = 500;
        var corpse = CreateAnimalAt(eco, 100, 100);
        corpse.AvailableEnergy = 0;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(eater);
        eco.AnimateObjects.Add(corpse);

        eater.Touching.Add(corpse);
        eater.HandleTouching();

        // Nothing to eat — no energy gained
        Assert.Equal(500, eater.AvailableEnergy, precision: 1);
    }

    [Fact]
    public void FoodEaten_tracks_energy_from_corpses()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var eater = CreateAnimalAt(eco, 100, 100);
        eater.AvailableEnergy = 500;
        var corpse = CreateAnimalAt(eco, 100, 100);
        corpse.AvailableEnergy = 300;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(eater);
        eco.AnimateObjects.Add(corpse);

        eater.Touching.Add(corpse);
        eater.HandleTouching();

        Assert.Equal(100, eater.FoodEaten, precision: 1);
    }

    // --- Vision: DeadCreatureAhead stays as separate input ---

    [Fact]
    public void DeadCreatureAhead_is_1_when_dead_creature_detected()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var target = CreateAnimalAt(eco, 150, 100);
        target.AvailableEnergy = 0;
        target.Update(0.01); // kills it
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        looker.UpdateVision();

        Assert.Equal(1, looker.DeadCreatureAhead);
        Assert.Equal(0, looker.FoodAhead);
        Assert.Equal(0, looker.AliveCreatureAhead);
    }

    [Fact]
    public void FoodEnergyAhead_reports_actual_corpse_energy()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        looker.LookingAngle = 0;
        looker.VisionDistance = 200;
        var corpse = CreateAnimalAt(eco, 150, 100);
        corpse.AvailableEnergy = 5000;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(corpse);

        looker.UpdateVision();

        // 5000 / 10000 = 0.5
        Assert.Equal(0.5, looker.FoodEnergyAhead, precision: 2);
    }

    // --- Kill creates a scavengeable corpse, not instant removal ---

    [Fact]
    public void Killed_creature_becomes_scavengeable_corpse()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var killer = CreateAnimalAt(eco, 100, 100);
        var victim = CreateAnimalAt(eco, 100, 100);
        killer.AvailableEnergy = 5000;
        killer.EatDesire = 0.9;
        killer.BreedDesire = 0.1;
        victim.AvailableEnergy = 1000;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(killer);
        eco.AnimateObjects.Add(victim);

        killer.Touching.Add(victim);
        killer.HandleTouching();

        // Victim is dead but NOT marked WasEaten — corpse persists for scavenging
        Assert.True(victim.IsDead);
        Assert.False(victim.WasEaten, "Killed creature should persist as scavengeable corpse");
        // Victim retains energy for scavenging (energy set to 0 by kill is fine,
        // but the corpse shouldn't be instantly removed)
    }

    [Fact]
    public void Killed_creature_retains_energy_for_scavenging()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var killer = CreateAnimalAt(eco, 100, 100);
        var victim = CreateAnimalAt(eco, 100, 100);
        killer.AvailableEnergy = 5000;
        killer.EatDesire = 0.9;
        killer.BreedDesire = 0.1;
        victim.AvailableEnergy = 3000;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(killer);
        eco.AnimateObjects.Add(victim);

        killer.Touching.Add(victim);
        killer.HandleTouching();

        // Victim dies but keeps its energy for others to scavenge
        Assert.True(victim.IsDead);
        Assert.True(victim.AvailableEnergy > 0, "Corpse should retain energy for scavenging");
    }

    [Fact]
    public void Killer_does_not_gain_flat_energy_on_kill()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var killer = CreateAnimalAt(eco, 100, 100);
        var victim = CreateAnimalAt(eco, 100, 100);
        killer.AvailableEnergy = 5000;
        killer.EatDesire = 0.9;
        killer.BreedDesire = 0.1;
        victim.AvailableEnergy = 1000;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(killer);
        eco.AnimateObjects.Add(victim);

        killer.Touching.Add(victim);
        killer.HandleTouching();

        // Killer doesn't get flat +20, energy stays the same (must scavenge corpse next tick)
        Assert.Equal(5000, killer.AvailableEnergy, precision: 1);
    }

    [Fact]
    public void Creature_stops_acting_after_dying_in_combat()
    {
        var eco = CreateEcosystem();
        eco.FoodBiteSize = 100;
        var weak = CreateAnimalAt(eco, 100, 100);
        var strong = CreateAnimalAt(eco, 100, 100);
        var corpse = CreateAnimalAt(eco, 100, 100);
        weak.AvailableEnergy = 1000;
        strong.AvailableEnergy = 5000;
        weak.EatDesire = 0.9;
        weak.BreedDesire = 0.1;
        corpse.AvailableEnergy = 300;
        corpse.IsDead = true;
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(weak);
        eco.AnimateObjects.Add(strong);
        eco.AnimateObjects.Add(corpse);

        // Weak touches strong (dies) and corpse (should NOT scavenge after dying)
        weak.Touching.Add(strong);
        weak.Touching.Add(corpse);
        weak.HandleTouching();

        Assert.True(weak.IsDead);
        // Dead creature should not have eaten the corpse
        Assert.Equal(300, corpse.AvailableEnergy, precision: 1);
        Assert.Equal(0, weak.FoodEaten, precision: 1);
    }

    [Fact]
    public void Network_has_8_inputs_with_DeadCreatureAhead()
    {
        var eco = CreateEcosystem();
        var animal = new Animal(eco);

        // AvailableEnergy, LookingAngle, WallAhead, AliveCreatureAhead,
        // DeadCreatureAhead, FoodAhead, FoodEnergyAhead, DistanceToObjectAhead = 8
        Assert.Equal(8, animal.Brain.InputCount);
    }
}
