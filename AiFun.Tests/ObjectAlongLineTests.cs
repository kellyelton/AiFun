using System.Windows;
using AiFun;
using AiFun.Entities;

namespace AiFun.Tests;

public class ObjectAlongLineTests
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

    [Fact]
    public void Returns_None_when_nothing_within_vision_distance()
    {
        var eco = CreateEcosystem();
        // Place a single creature at center — ray should find nothing
        var animal = CreateAnimalAt(eco, 1000, 1000);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        var result = eco.ObjectAlongLine(0, animal.Location.TopLeft, visionDistance: 100);

        Assert.Equal(VisionHitType.None, result.HitType);
        Assert.Equal(0, result.Distance);
        Assert.Null(result.HitObject);
    }

    [Fact]
    public void Returns_Wall_when_ray_hits_boundary_within_vision_distance()
    {
        var eco = CreateEcosystem(200, 200);
        // Place creature near right edge, looking right (0 degrees = east)
        var animal = CreateAnimalAt(eco, 180, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        var result = eco.ObjectAlongLine(0, animal.Location.TopLeft, visionDistance: 100);

        Assert.Equal(VisionHitType.Wall, result.HitType);
        Assert.True(result.Distance > 0);
        Assert.True(result.Distance <= 100);
        Assert.Null(result.HitObject);
    }

    [Fact]
    public void Returns_AliveCreature_when_ray_hits_living_animal()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        var target = CreateAnimalAt(eco, 150, 100); // 50px to the right
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        // Look right (0 degrees)
        var result = eco.ObjectAlongLine(0, looker.Location.TopLeft, visionDistance: 200);

        Assert.Equal(VisionHitType.AliveCreature, result.HitType);
        Assert.True(result.Distance > 0);
        Assert.Equal(target, result.HitObject);
    }

    [Fact]
    public void Returns_DeadCreature_when_ray_hits_dead_animal()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        var target = CreateAnimalAt(eco, 150, 100);
        target.AvailableEnergy = 0;
        target.Update(0.01); // This will set IsDead = true
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        var result = eco.ObjectAlongLine(0, looker.Location.TopLeft, visionDistance: 200);

        Assert.Equal(VisionHitType.DeadCreature, result.HitType);
        Assert.Equal(target, result.HitObject);
    }

    [Fact]
    public void Does_not_detect_objects_beyond_vision_distance()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        var target = CreateAnimalAt(eco, 300, 100); // 200px away
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        // Vision distance is only 50 — should not reach 200px target
        var result = eco.ObjectAlongLine(0, looker.Location.TopLeft, visionDistance: 50);

        Assert.Equal(VisionHitType.None, result.HitType);
    }

    [Fact]
    public void Distance_is_approximately_correct()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        var target = CreateAnimalAt(eco, 150, 100); // ~50px away
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        var result = eco.ObjectAlongLine(0, looker.Location.TopLeft, visionDistance: 200);

        // The ray steps in 5px increments, so distance should be within a step of the real distance
        Assert.InRange(result.Distance, 40, 60);
    }

    [Fact]
    public void Wall_detected_before_creature_if_wall_is_closer()
    {
        var eco = CreateEcosystem(200, 200);
        // Looker near right edge, target beyond the wall
        var looker = CreateAnimalAt(eco, 190, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);

        var result = eco.ObjectAlongLine(0, looker.Location.TopLeft, visionDistance: 200);

        Assert.Equal(VisionHitType.Wall, result.HitType);
    }

    [Fact]
    public void Does_not_detect_self()
    {
        var eco = CreateEcosystem();
        // Only creature in the world — ray should pass through itself
        var animal = CreateAnimalAt(eco, 1000, 1000);
        animal.Location = new Rect(1000, 1000, 10, 10); // larger body to ensure ray overlaps
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        var result = eco.ObjectAlongLine(0, animal.Location.TopLeft, visionDistance: 100);

        // Should not detect itself
        Assert.NotEqual(animal, result.HitObject);
    }

    [Fact]
    public void Nearer_creature_occludes_farther_creature()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        var near = CreateAnimalAt(eco, 150, 100);   // 50px away
        var far = CreateAnimalAt(eco, 250, 100);     // 150px away
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(far);   // add far first to ensure order doesn't matter
        eco.AnimateObjects.Add(near);

        var result = eco.ObjectAlongLine(0, looker.Location.TopLeft, visionDistance: 300);

        Assert.Equal(near, result.HitObject);
    }

    [Theory]
    [InlineData(90)]   // south
    [InlineData(180)]  // west
    [InlineData(270)]  // north
    [InlineData(45)]   // southeast diagonal
    public void Detects_wall_at_non_zero_angles(double angle)
    {
        // Small world so walls are reachable in all directions
        var eco = CreateEcosystem(200, 200);
        var animal = CreateAnimalAt(eco, 100, 100);
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(animal);

        var result = eco.ObjectAlongLine(angle, animal.Location.TopLeft, visionDistance: 300);

        Assert.Equal(VisionHitType.Wall, result.HitType);
        Assert.True(result.Distance > 0);
    }

    [Fact]
    public void Detects_creature_at_diagonal_angle()
    {
        var eco = CreateEcosystem();
        var looker = CreateAnimalAt(eco, 100, 100);
        // Place target at 45 degrees (southeast), ~70px diagonal distance
        var target = CreateAnimalAt(eco, 150, 150);
        target.Location = new Rect(148, 148, 10, 10); // slightly larger to ensure ray hits
        eco.AnimateObjects.Clear();
        eco.AnimateObjects.Add(looker);
        eco.AnimateObjects.Add(target);

        var result = eco.ObjectAlongLine(45, looker.Location.TopLeft, visionDistance: 200);

        Assert.Equal(VisionHitType.AliveCreature, result.HitType);
        Assert.Equal(target, result.HitObject);
    }
}
