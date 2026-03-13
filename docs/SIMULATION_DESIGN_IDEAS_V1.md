# Simulation Roadmap

## Goal
A simulation where creatures reproduce and move around naturally, with emergent behavior driven by neural networks and natural selection.

---

## Current State

### Neural Network Inputs (4)
| # | Property | Range | Normalization |
|---|----------|-------|---------------|
| 1 | `AvailableEnergy` | 0 – 10,000 | Clamped then scaled to [0, 1] |
| 2 | `LookingAngle` | 0° – 360° | Scaled to [0, 1] |
| 3 | `IsFocusingOnObject` | 0 or 1 | Raw binary — is something in line of sight? |
| 4 | `DistanceToFocusingObject` | 0 – MaxValue | Scaled to [0, 1] |

### Neural Network Outputs (2)
| # | Property | Raw Range | Denormalized To |
|---|----------|-----------|-----------------|
| 1 | `Speed` | [-1, 1] | [0, 20] — allows stopping |
| 2 | `TurnDeltaPerTick` | [-1, 1] | [-10, +10] degrees/tick |

### Interactions (all automatic on contact, no creature agency)
- **Other is dead?** → Eat (+1000 energy)
- **Both can breed?** (male+female, neither pregnant) → Breed (both lose energy, female gets pregnant)
- **Otherwise?** → Higher energy creature kills the other (+20 energy)

### Selection / Evolution
- Two competing models exist:
  - **Continuous**: Creatures breed during lifetime via pregnancy. Babies born into the live world.
  - **Generational**: `NewGeneration()` wipes everyone, picks top 2 dead creatures, spawns new population.
- Generational dominates — triggers automatically when all creatures die.
- Selection sorts dead creatures by: `DistanceTraveled` > `DeltaTurn` > `LengthOfLife` > `BabiesCreated`
- Top 2 become parents for `ElitePopulation` offspring + `RandomPopulation` random creatures.

### Known Issues
- Walls kill instantly but creatures have no wall-sensing inputs.
- Selection rewards sprinting distance, not survival or reproduction.
- `Fitness` property (returns `LengthOfLife`) is defined but unused.
- Creatures have no agency over fight/flee/mate decisions.
- Vision is limited to a single ray along heading — no peripheral awareness, no info about what's detected.

---

## Improvement Steps

### Step 1: Vision System (merged wall detection + object identification)
**Status:** ✅ Complete
**Effort:** Small–Medium | **Impact:** 🔥🔥🔥

Creatures die instantly at walls they can't detect, and can't distinguish what's ahead of them (food, mate, threat, wall). This step introduces a unified vision system that solves both problems.

#### 1a. `VisionDistance` — genetic trait
A per-creature property controlling how far ahead the ray march looks, inherited and mutated during breeding like `MovementEfficency`.

- **Range:** 0 to `MaxVisionDistance` (ecosystem-level constant, TBD — maybe 200–400px?)
- **Energy cost:** Vision drains energy each tick proportional to `VisionDistance`. Creatures evolve toward the sweet spot — enough vision to survive, not so much that it drains them dry.
- **Energy formula:** `AvailableEnergy -= VisionDistance * VisionEnergyCostMultiplier * time` (new ecosystem param)
- **Inherited:** Crossed over + mutated in `Breed()` like other traits

#### 1b. Reworked vision inputs
Replace the current `IsFocusingOnObject` / `DistanceToFocusingObject` with type-aware detection. The existing ray march in `ObjectAlongLine()` already steps along the heading and terminates at objects or boundaries — we extend it to classify what it hit.

Only one thing can be "first hit" (you can't see through objects — realistic):

| # | Input | Range | Meaning |
|---|-------|-------|---------|
| 1 | `AvailableEnergy` | [0, 1] | Existing, keep |
| 2 | `LookingAngle` | [0, 1] | Existing, keep |
| 3 | `WallAhead` | 0 or 1 | First thing hit is a world boundary |
| 4 | `AliveCreatureAhead` | 0 or 1 | First thing hit is a living animal |
| 5 | `DeadCreatureAhead` | 0 or 1 | First thing hit is a dead animal (food source) |
| 6 | `DistanceToObjectAhead` | [0, 1] | Normalized by `VisionDistance`, **inverted** (closer = higher) |

At most one of inputs 3–5 is 1 at a time. If nothing is within `VisionDistance`, all three are 0 and distance is 0.

#### 1c. Optimization via spatial hashing
The existing spatial hash grid (`SpatialCellSize = 40`, `ComposeSpatialKey`) is already used by `ObjectAlongLine()`. No extra spatial work needed — the ray march already uses it. The only change is capping the march distance at `VisionDistance` instead of `max(WorldWidth, WorldHeight)`, which actually **reduces** work for short-sighted creatures.

#### Implementation plan
1. Add `VisionDistance` property to `Animal`, randomize in constructor, inherit in `Breed()`
2. Add `MaxVisionDistance` and `VisionEnergyCostMultiplier` to `Ecosystem` (with UI sliders)
3. Add `WallAhead`, `AliveCreatureAhead`, `DeadCreatureAhead` properties to `Animal`
4. Modify `ObjectAlongLine()` to return a result struct (type + distance) capped at `VisionDistance`
5. In `Animal.Update()`: set vision properties from result, apply vision energy cost
6. Replace old inputs in `SetupNetwork()` with new ones
7. Add vision energy drain: `AvailableEnergy -= VisionDistance * VisionEnergyCostMultiplier * time`

**Files:** `Animal.cs`, `Ecosystem.cs`

**Alternatives considered:**
- ~~4 edge distances (left/right/top/bottom)~~ — Not natural; creatures don't have GPS
- ~~Heading-only wall distance without energy cost~~ — No trade-off, every creature maxes vision
- ✅ Genetic `VisionDistance` with energy cost + type-aware ray cast — natural, efficient, evolvable

---

### Step 2: Align Selection Pressure
**Status:** ✅ Complete
**Effort:** Trivial | **Impact:** 🔥🔥

Selection currently rewards `DistanceTraveled` first, which selects for creatures that sprint in straight lines until they die. This opposes the goal of natural survival behavior.

**Approach:** Changed the composite `Fitness` property weights to prioritize `LengthOfLife` > `BabiesCreated` > `DistanceTraveled`:
```csharp
return LengthOfLife * 1_000_000_000
     + BabiesCreated * 10_000_000
     + DistanceTraveled * 1;
```

`NewGeneration()` already sorts by `.OrderByDescending(x => x.Fitness)`, so reweighting the composite score was sufficient — no sort changes needed.

**Files:** `Animal.cs` (`Fitness` property)

**Alternatives considered:**
- A) Swap to `LengthOfLife` primary ✅ chosen as first step
- B) Composite fitness: `LengthOfLife + BabiesCreated * bonus` — good future enhancement
- C) Remove generational reset entirely, rely on natural selection — larger change, revisit later

---

### Step 2b: Food Pellets
**Status:** ✅ Complete
**Effort:** Medium | **Impact:** 🔥🔥🔥

Without an external food source, the optimal survival strategy is "go blind, sit still" — vision and movement are pure energy costs with no payoff. Food pellets give creatures a reason to see, move, and navigate.

#### Design

**Food pellet** — a static object that spawns randomly, grows in energy over time, and can be nibbled by creatures on contact.

| Parameter (slider) | Default | Description |
|---------------------|---------|-------------|
| `FoodTargetCount` | 30 | Ecosystem maintains this many pellets; respawns when eaten |
| `FoodMinStartEnergy` | 50 | Energy when freshly spawned |
| `FoodMaxEnergy` | 500 | Growth cap — fully grown pellet = 5 seconds of life |
| `FoodGrowthRate` | 10/sec | Energy gained per second; ~45s from min to cap |
| `FoodBiteSize` | 100 | Energy consumed per contact (global, not genetic) |

**Eating mechanic:** On contact, creature absorbs `min(FoodBiteSize, pellet.Energy)`. Pellet energy decreases by the same amount. Pellet disappears when energy reaches 0. A new one spawns to maintain `FoodTargetCount`.

**Visuals:** Green circle. Radius scales with energy (4px at min, 16px at max). Color darkens from light green to forest green as it grows.

**Spawn location:** Anywhere in the world randomly, including near walls — rewards smarter navigation.

#### Neural network inputs (2 new, total inputs: 6 → 8)

| # | Input | Range | Meaning |
|---|-------|-------|---------|
| 7 | `FoodAhead` | 0 or 1 | First thing hit by vision ray is a food pellet |
| 8 | `FoodEnergyAhead` | [0, 1] | Energy of detected food/corpse, normalized (pellets by `FoodMaxEnergy`, corpses by 10000) |

At most one of `WallAhead`, `AliveCreatureAhead`, `DeadCreatureAhead`, `FoodAhead` is 1 at a time. `FoodEnergyAhead` is set for both food pellets and dead creatures.

#### Implementation plan
1. Create `FoodPellet` class (extends `AnimateObject`, speed=0)
2. Add ecosystem parameters with UI sliders
3. Add `FoodAhead`, `FoodEnergyAhead` properties to `Animal`
4. Update `VisionHitType` enum to include `Food`
5. Update `ObjectAlongLine()` ray march to detect `FoodPellet`
6. Update `Animal.UpdateVision()` to set `FoodAhead` / `FoodEnergyAhead`
7. Add food inputs to `SetupNetwork()`
8. Add food eating to `Animal.HandleTouching()` (before creature interactions)
9. Add food spawning/removal to `Ecosystem.Update()`
10. Add `FoodPellet` DataTemplate to `MainWindow.xaml`

**Files:** `FoodPellet.cs` (new), `Animal.cs`, `Ecosystem.cs`, `VisionResult.cs`, `MainWindow.xaml`

---

### Step 3: Interaction Agency
**Status:** ✅ Complete
**Effort:** Medium | **Impact:** 🔥

Give creatures neural network outputs to control what they do on contact, instead of automatic outcomes.

#### Design

Two new scalar outputs from the neural net: `EatDesire` and `BreedDesire` (both [0, 1]).

On contact with another creature:
- Whichever desire is **higher** determines what the creature attempts
- If equal → do nothing (no action)
- Each creature decides independently — only the acting creature's intent matters

No "flee" output — fleeing is already handled by the creature turning away and moving. Adding a discrete flee intent would be redundant and would break once the creature loses sight of the threat.

#### `HandleTouching()` rework
```
if other is dead:
    bite for FoodBiteSize energy (same mechanic as food pellets)
    mark WasEaten when corpse energy reaches 0
else if BreedDesire > EatDesire:
    attempt to breed (compatibility checks)
    if breeding fails: do nothing (creature chose peace)
else if EatDesire > BreedDesire:
    attempt to eat (energy-comparison fight)
else:
    do nothing
```

Dead creatures use the same bite mechanic as food pellets — eaten in `FoodBiteSize` chunks, energy transfers based on what the corpse actually has left. `FoodEnergyAhead` reports the corpse's real remaining energy. The desire system only applies to **live creature encounters**, where the stakes (fight vs. breed) actually matter.

#### Visual feedback
Nose dot color reflects the creature's current dominant desire:
- **Red** = EatDesire dominant (aggressive)
- **Yellow** = BreedDesire dominant (social)
- **Gray** = neutral (desires roughly equal)

#### Interaction with existing pregnancy
Currently `CanBreed()` blocks breeding entirely when pregnant. Updated behavior:
- Pregnant creatures **can** still attempt to breed, but it just costs energy without result
- This makes high `BreedDesire` while pregnant a wasteful trait — natural selection will push against it

**Files:** `Animal.cs` (new outputs in `SetupNetwork()`, rework `HandleTouching()`)

**Considerations:**
- Requires Step 1 first so creatures have vision inputs to base decisions on
- Creatures may evolve to always eat or always breed — that's fine, selection pressure will sort it out
- Future enhancement: more desire outputs as new interaction types are added

---

### Step 4: Genetic Pregnancy Duration
**Status:** ✅ Complete
**Effort:** Small | **Impact:** 🔥

Currently `PregnancyDurationSeconds` is a single ecosystem-wide constant. Make it a per-creature genetic trait so creatures evolve their own gestation period.

#### Design

- `PregnancyGene` — per-creature scalar [0, 1], inherited and mutated via `Breed()`, randomized in constructor
- `MinPregnancyDuration` / `MaxPregnancyDuration` — ecosystem-level bounds (configurable via UI sliders)
- **Actual duration** = `MinPregnancyDuration + PregnancyGene * (MaxPregnancyDuration - MinPregnancyDuration)`
  - e.g., gene=0.0 with min=2s/max=20s → 2s pregnancy; gene=1.0 → 20s pregnancy
- Pregnant creatures use significantly more energy per tick: `AvailableEnergy -= PregnancyEnergyCostMultiplier * time`
- `PregnancyEnergyCostMultiplier` — new ecosystem parameter with UI slider

#### Deferred baby creation
Currently `Impregnate()` creates the baby `Animal` object immediately at conception, freezing both parents' genetics at that moment. The father could be long dead by the time the baby is born.

**New approach:**
- At conception, store a reference to the **father's `BasicNetwork`** (brain) and genetic traits (e.g., `MovementEfficency`, `HiddenNeurons`, `VisionDistance`, `PregnancyDuration`)
- The baby `Animal` is only created at **birth** (when pregnancy timer completes)
- At birth, `Breed()` uses the mother's **current** genetics + the stored father snapshot
- This means the mother's state at birth matters — if she's evolved or changed traits mid-simulation (not currently possible, but future-proofs it)
- If the father is dead, that's fine — we have his genetics stored

**Implementation:**
- Replace `private Animal _baby` with a stored genetics snapshot (e.g., a small struct/class holding the father's network weights + trait values)
- `Impregnate()` stores the snapshot instead of creating a full `Animal`
- `PopBaby()` creates the `Animal` at birth time using mother + snapshot

#### Trade-offs creatures must evolve around
- **Short pregnancy** = babies faster, but each pregnancy still costs a burst of energy
- **Long pregnancy** = fewer babies, but more time to accumulate energy between births
- **Being pregnant at all** = expensive, so creatures need enough energy reserves before breeding is worth it

**Files:** `Animal.cs` (genetic trait + energy drain), `Ecosystem.cs` (min/max bounds + cost multiplier), `MainWindow.xaml` (sliders)

---

### Step 5: Creature Visuals
**Status:** ✅ Complete
**Effort:** Small | **Impact:** 🔥 (debugging + fun)

#### 5a. Genetic color
- Store `ColorR`, `ColorG`, `ColorB` as [0, 1] scalars in the genome
- Random creatures get random RGB
- Offspring: crossover per channel using same `SetToRandom` as other traits — colors drift slowly, family clusters become visible
- Bind fill color of the creature polygon to these values

#### 5b. Origin indicator
- Creatures should be visually distinguishable by how they were spawned (random, elite, natural offspring)
- Not genetic — set once in constructor, never inherited
- Could be stroke color, shape variation, outline thickness, or similar — exact approach TBD at implementation time

**Files:** `Animal.cs` (color properties + origin enum), `MainWindow.xaml` (data template bindings)

---

## Future Ideas (not yet prioritized)
- Peripheral vision / multiple sensing rays
- Flocking behavior inputs (nearby creature count/density)
- Energy from food sources (plants/static objects) instead of just eating each other
- Graphs/charts tracking fitness over generations
- Save/load best genomes
