# Simulation Roadmap V2

## Goal
Creatures that actively seek food and mates, sustain a population through natural reproduction, and exhibit recognizable evolved behaviors — not just passive survival.

---

## Current State (end of V1)

### Neural Network Inputs (8)
| # | Property | Range | Normalization |
|---|----------|-------|---------------|
| 1 | `AvailableEnergy` | 0 – 10,000 | Clamped then scaled to [0, 1] |
| 2 | `LookingAngle` | 0° – 360° | Scaled to [0, 1] |
| 3 | `WallAhead` | 0 or 1 | Binary — first hit is world boundary |
| 4 | `AliveCreatureAhead` | 0 or 1 | Binary — first hit is living animal |
| 5 | `DeadCreatureAhead` | 0 or 1 | Binary — first hit is dead animal |
| 6 | `FoodAhead` | 0 or 1 | Binary — first hit is food pellet |
| 7 | `FoodEnergyAhead` | [0, 1] | Energy of detected food/corpse, normalized |
| 8 | `DistanceToObjectAhead` | [0, 1] | Inverted (closer = higher), 0 if nothing |

### Neural Network Outputs (4)
| # | Property | Raw Range | Denormalized To |
|---|----------|-----------|-----------------|
| 1 | `Speed` | [-1, 1] | [0, 20] |
| 2 | `TurnDeltaPerTick` | [-1, 1] | [-10, +10] degrees/tick |
| 3 | `EatDesire` | [-1, 1] | [0, 1] |
| 4 | `BreedDesire` | [-1, 1] | [0, 1] |

### Genetic Traits
| Trait | Range | Notes |
|-------|-------|-------|
| `MovementEfficency` | [0, 1] | Multiplier on movement energy cost |
| `VisionDistance` | [0, MaxVisionDistance] | How far the single forward ray reaches |
| `PregnancyGene` | [0, 1] | Maps to gestation duration within ecosystem bounds |
| `HiddenNeurons` | 0 – 4 | Number of hidden layers (each has 8 neurons) |
| `Sex` | [0, 1] | <0.5 = male, ≥0.5 = female |
| `ColorR/G/B` | [0, 1] | Visual phenotype, no functional effect |
| Neural network weights | [-1, 1] | Per-synapse crossover with mutation |

### Vision System
- Single forward-facing ray, marches in 5px steps along `LookingAngle`
- Stops at first hit (wall, creature, corpse, food) or `VisionDistance`
- Uses spatial hash grid (40px cells) for efficient lookups
- Energy cost: `VisionDistance * VisionEnergyCostMultiplier * time`

### Interactions (on contact)
- Food pellets: auto-eat `FoodBiteSize` per contact
- Dead creatures: auto-eat in bite-sized chunks
- Live creatures: `BreedDesire > EatDesire` → attempt breed; `EatDesire > BreedDesire` → energy-comparison fight

### Selection / Evolution
- Generational: when all creatures die, top `TopBreeders` (30) by fitness become breeding pool
- Fitness: `LengthOfLife * 1B + BabiesCreated * 10M + DistanceTraveled * 1`
- Crossover: 50/50 random selection per gene, per weight
- Mutation: 0.1% chance per weight → complete random replacement in [-1, 1]
- Each generation: 75 elite offspring + 25 fully random creatures

### Known Issues
- Vision is a single forward ray — creatures can't locate anything unless it's directly ahead
- Mutation destroys weights instead of nudging them — any useful patterns get obliterated
- Vision energy cost makes high vision a net negative since the single ray can't be used effectively
- No memory between ticks — creatures can't sustain turning toward something they saw
- Energy economics are punishing — base drain alone consumes a full food pellet in 5 seconds
- Fitness over-weights survival time — a creature that hoards energy beats one that reproduces
- Fixed low mutation rate (0.1%) causes slow convergence with no adaptation
- `HiddenNeurons` as a gene creates incompatible topologies during crossover

---

## Improvement Steps

### Step 6: Multi-Ray Vision
**Status:** Done
**Effort:** Medium | **Impact:** 🔥🔥🔥🔥🔥

This is the single most important change. Currently creatures have one forward-facing ray. If food is 10° to the left, they receive zero information about it. They must randomly scan 360° hoping to catch something in a narrow beam. This makes vision nearly useless — evolution correctly discovers this by optimizing vision distance to zero.

#### Problem
A creature facing north with food to its northeast gets these inputs:
```
FoodAhead: 0     ← can't see it
Distance:  0     ← nothing detected
```
It has no idea food exists. The only way to find food is to randomly turn until the ray happens to sweep across it, then somehow sustain that turn — which requires memory it doesn't have (see Step 7).

With multiple rays, the same scenario:
```
Ray[Forward]:   FoodAhead: 0, Distance: 0
Ray[Right30]:   FoodAhead: 1, Distance: 0.7
```
Now the NN can learn: "food signal on right ray → turn right." This is a solvable problem for even a simple feedforward network.

#### Design

Add `VisionRayCount` as an ecosystem-level parameter (not genetic — all creatures share the same sensor layout). Default: 5 rays.

**Ray angles** are evenly spread across a configurable `VisionFieldOfView` (default: 120°), centered on `LookingAngle`:

| VisionRayCount | Offsets from heading (for 120° FOV) |
|----------------|--------------------------------------|
| 3 | -60°, 0°, +60° |
| 5 | -60°, -30°, 0°, +30°, +60° |
| 7 | -60°, -40°, -20°, 0°, +20°, +40°, +60° |

Each ray produces its own set of detection inputs. To keep the input count manageable, each ray produces a **compact 3-input summary** instead of the current 5 separate flags:

| # | Input | Range | Meaning |
|---|-------|-------|---------|
| 1 | `ObjectType` | [0, 1] | What was hit: 0=nothing, 0.25=wall, 0.5=food, 0.75=creature(dead), 1.0=creature(alive) |
| 2 | `ObjectDistance` | [0, 1] | Inverted distance (closer = higher), 0 if nothing |
| 3 | `ObjectEnergy` | [0, 1] | Energy of hit object (food/corpse), 0 for walls/alive/nothing |

**Total NN inputs** = 2 (AvailableEnergy, LookingAngle) + VisionRayCount × 3 (per-ray)

| Rays | Vision Inputs | Total Inputs |
|------|---------------|--------------|
| 3 | 9 | 11 |
| 5 | 15 | 17 |
| 7 | 21 | 23 |

#### Neural network input layout (example: 5 rays)
```
 0: AvailableEnergy         [0, 1]
 1: LookingAngle            [0, 1]
 2: Ray[-60°] ObjectType    [0, 1]
 3: Ray[-60°] ObjectDistance [0, 1]
 4: Ray[-60°] ObjectEnergy  [0, 1]
 5: Ray[-30°] ObjectType    [0, 1]
 6: Ray[-30°] ObjectDistance [0, 1]
 7: Ray[-30°] ObjectEnergy  [0, 1]
 8: Ray[  0°] ObjectType    [0, 1]
 9: Ray[  0°] ObjectDistance [0, 1]
10: Ray[  0°] ObjectEnergy  [0, 1]
11: Ray[+30°] ObjectType    [0, 1]
12: Ray[+30°] ObjectDistance [0, 1]
13: Ray[+30°] ObjectEnergy  [0, 1]
14: Ray[+60°] ObjectType    [0, 1]
15: Ray[+60°] ObjectDistance [0, 1]
16: Ray[+60°] ObjectEnergy  [0, 1]
```

#### Vision energy cost adjustment
Current cost is per-unit-distance, making high vision very expensive. With multiple rays, the cost should scale with ray count but be cheaper per ray:

`AvailableEnergy -= VisionDistance * VisionRayCount * VisionEnergyCostMultiplier * time`

Reduce `VisionEnergyCostMultiplier` default from 0.5 to 0.05 to compensate.

#### Hidden layer sizing
Currently hidden layers always have `_inmaps.Count` neurons (matching input count). With 17 inputs, that's 17 neurons per hidden layer — potentially too large. Change hidden layer size to a fixed configurable value (e.g., 12) or `Math.Min(inputCount, 16)` so the network stays tractable.

#### Implementation plan
1. Add `VisionRayCount` (int, default 5) and `VisionFieldOfView` (double, default 120°) to `Ecosystem` with UI sliders
2. Create a `RayResult` struct with `ObjectType`, `ObjectDistance`, `ObjectEnergy` fields
3. Add `RayResults` array property to `Animal` (length = `VisionRayCount`)
4. In `Animal.UpdateVision()`: cast `VisionRayCount` rays at evenly spaced angles, populate `RayResults`
5. Replace the 5 individual vision input mappings in `SetupNetwork()` with a loop over `RayResults`
6. Remove `WallAhead`, `AliveCreatureAhead`, `DeadCreatureAhead`, `FoodAhead`, `FoodEnergyAhead`, `DistanceToObjectAhead` properties (replaced by `RayResults`)
7. Adjust `VisionEnergyCostMultiplier` default
8. Update hidden layer sizing in `NetworkMapper.CreateNetwork()`

**Files:** `Animal.cs`, `Ecosystem.cs`, `NetworkMapper.cs`, `VisionResult.cs`

**Alternatives considered:**
- ~~Smell/scent gradient (distance + direction to nearest food)~~ — Too easy; removes need for scanning behavior. Creatures would just follow the gradient with no vision needed.
- ~~360° vision (many rays in all directions)~~ — Too many inputs, too expensive. A forward-facing cone with a blind spot behind is more natural and creates interesting trade-offs.
- ✅ Multi-ray cone — natural, scalable, gives the NN enough info to learn directional pursuit without making it trivial.

---

### Step 7: Speed–Vision Coupling
**Status:** Done
**Effort:** Small | **Impact:** 🔥🔥🔥

Creatures moving at full speed should not have perfect long-range, wide-angle vision. In nature, fast movement reduces the ability to process peripheral detail — you get tunnel vision. This step ties speed to both vision range and peripheral ray count, creating a natural "scan mode" (slow, wide vision) vs "chase mode" (fast, narrow vision) trade-off that creatures must evolve around.

#### Design

Two scaling effects, both based on the creature's current `Speed` relative to max speed (20):

**1. Vision distance scales down with speed**

```
speedFraction = Speed / MaxSpeed                          // [0, 1]
effectiveVision = VisionDistance * (1 - speedFraction * 0.75)
```

| Speed | speedFraction | Effective Vision (% of max) |
|-------|---------------|----------------------------|
| 0 | 0.0 | 100% |
| 5 | 0.25 | 81% |
| 10 | 0.5 | 62% |
| 15 | 0.75 | 44% |
| 20 | 1.0 | 25% |

At full sprint, a creature with 200px max vision can only see 50px ahead. Standing still gives the full 200px.

**2. Active ray count reduces with speed**

At low speed, all rays fire. As speed increases, outer rays are disabled (set to zero inputs), simulating tunnel vision. The center ray always fires.

For 5 rays at offsets [-60°, -30°, 0°, +30°, +60°]:

| Speed Range | Active Rays | Description |
|-------------|-------------|-------------|
| 0–5 | All 5 | Full peripheral vision |
| 5–10 | Center 3 | [-30°, 0°, +30°] |
| 10–15 | Center 3 | [-30°, 0°, +30°] (shorter range) |
| 15–20 | Center 1 | [0°] only — pure tunnel vision |

The thresholds scale with `VisionRayCount` — always symmetrically disable the outermost pair first. Disabled rays send all-zero inputs to the NN (ObjectType=0, ObjectDistance=0, ObjectEnergy=0), which the network can learn to interpret as "I can't see there right now."

**3. Vision energy cost uses effective values**

Since fewer rays fire shorter distances at speed, the energy cost naturally drops when moving fast:

```
AvailableEnergy -= effectiveVision * activeRayCount * VisionEnergyCostMultiplier * time
```

This means sprinting is cheap on vision but blind, while scanning is expensive but informative. The creature pays for the vision it actually uses.

#### Evolutionary pressure
This creates a clean behavioral loop that the NN can learn:
1. **Search:** Move slowly → wide vision, long range → spot food
2. **Approach:** Speed up toward food → vision narrows but food is already in the center ray
3. **Arrive:** Slow down near food → vision widens again → precise positioning for contact

Creatures that evolve to sprint blindly will miss food. Creatures that creep everywhere will be energy-inefficient. The optimal behavior is a speed profile that matches the situation — exactly the kind of emergent behavior we want.

#### Implementation plan
1. Add speed-to-vision scaling in `Animal.UpdateVision()` — compute `effectiveVision` and `activeRayCount` before casting rays
2. Only cast rays that are active; zero out inputs for disabled rays
3. Use `effectiveVision` instead of `VisionDistance` in the `ObjectAlongLine()` call
4. Use `effectiveVision * activeRayCount` for vision energy cost calculation
5. No new ecosystem parameters needed — the 0.75 scaling factor and ray-disable thresholds can be constants initially

**Files:** `Animal.cs`

**Alternatives considered:**
- ~~Speed affects only vision distance, not ray count~~ — Misses the tunnel vision effect. A creature at full speed with 5 rays and short range isn't really "tunnel vision," it's just near-sighted. Reducing ray count captures the perceptual narrowing.
- ~~Make the coupling strength genetic~~ — Adds complexity without clear benefit. All creatures should face the same physics; evolution happens in how they respond to it.
- ✅ Both distance and ray count scale with speed — clean physics-like constraint that rewards evolved speed modulation.

---

### Step 8: Recurrent Memory Inputs
**Status:** Done
**Effort:** Small–Medium | **Impact:** 🔥🔥🔥🔥

Even with multi-ray vision, the neural network is pure feedforward — every tick is a completely fresh decision with zero context from the previous tick. A creature that saw food to its right and started turning has no memory that it was turning. Next tick, if the food leaves its vision cone, it stops turning. It can never sustain a behavior across ticks.

#### Problem
Smooth pursuit requires temporal continuity: "I was turning right toward food → keep turning right." Without memory, creatures can only react to what they see *right now*. This limits behavior to instantaneous reflexes, which isn't enough for navigation.

#### Design

Feed a subset of the previous tick's **outputs** back as **inputs** on the next tick. This creates a simple recurrent loop without changing the network architecture — the NN is still feedforward, but with a few extra inputs that carry state.

**Recurrent inputs** (added after vision inputs):

| # | Input | Source | Range |
|---|-------|--------|-------|
| 1 | `PrevSpeed` | Previous tick's Speed output | [0, 1] (normalized from [0, 20]) |
| 2 | `PrevTurnDelta` | Previous tick's TurnDeltaPerTick output | [0, 1] (normalized from [-10, 10]) |
| 3 | `PrevEatDesire` | Previous tick's EatDesire output | [0, 1] |
| 4 | `PrevBreedDesire` | Previous tick's BreedDesire output | [0, 1] |

This adds 4 inputs. Total with 5-ray vision: 17 + 4 = 21 inputs.

**Initialization:** All `Prev*` values start at 0.5 (neutral) on first tick.

**Storage:** After each `_mapper.Update()`, store the current output values into `_prev*` fields. These fields are read as inputs on the next tick via the mapped properties.

#### Why this works
The NN can now learn patterns like:
- "I was turning right (PrevTurnDelta high) and I see food on the right → keep turning right"
- "I was moving fast (PrevSpeed high) and I see a wall ahead → slow down"
- "I was in eat mode and I still see food → stay in eat mode"

This is the simplest form of recurrence — no LSTM cells, no separate memory architecture, just feeding outputs back as inputs. It's enough for sustained behavior without adding architectural complexity.

#### Implementation plan
1. Add `PrevSpeed`, `PrevTurnDelta`, `PrevEatDesire`, `PrevBreedDesire` properties to `Animal`
2. Initialize all to 0.5 in constructors
3. Add 4 new `MapInput` calls in `SetupNetwork()` (after vision inputs, before outputs)
4. At the end of `Animal.Update()`, after `_mapper.Update()`, store current outputs into `Prev*` fields (normalized back to [0, 1])
5. No changes to crossover/mutation — these are just extra input connections with weights that evolve normally

**Files:** `Animal.cs`

**Alternatives considered:**
- ~~LSTM or GRU cells~~ — Requires a completely different network library or custom implementation. Massive complexity for marginal gain over simple recurrence.
- ~~Explicit memory buffer (last N observations)~~ — Multiplies input count by N. Quickly explodes the network size.
- ✅ Output-to-input feedback — minimal complexity, no new architecture, gives enough temporal state for sustained behavior.

---

### Step 9: Gaussian Mutation
**Status:** Not started
**Effort:** Trivial | **Impact:** 🔥🔥🔥🔥

Currently when a weight mutates (`ExtensionMethods.SetToRandom` with bias), it's completely replaced with a random value in [-1, 1]. This is catastrophic — a weight that evolved over hundreds of generations to a useful value of 0.73 gets randomly replaced with -0.41. Any useful patterns are destroyed by a single mutation event.

#### Problem
The current mutation in `CrossoverBrainWeights()`:
```csharp
if (random < MutationRate)
    f.Weight = _rnd.NextDouble().DenormalizeFromUnit(-1, 1);  // FULL REPLACEMENT
```

This is equivalent to a mutation step size of σ ≈ 1.15 (uniform over [-1, 1]). In neuroevolution, typical step sizes are σ = 0.05 to 0.2. The current approach is ~10x too aggressive, preventing incremental refinement.

#### Design

Replace full-replacement mutation with Gaussian perturbation:

```csharp
if (random < MutationRate)
    f.Weight = Math.Clamp(parentWeight + Gaussian(0, MutationStepSize), -1, 1);
```

**New ecosystem parameter:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| `MutationStepSize` | 0.1 | Standard deviation of Gaussian perturbation |

The parent weight is chosen first (50/50 from either parent), then perturbed. This means mutations are small nudges around inherited values, not random jumps.

#### Genetic trait mutation
Currently genetic traits (`MovementEfficency`, `VisionDistance`, etc.) use `SetToRandom` — a 50/50 coin flip between parents with no mutation at all. These traits should also have a small chance of Gaussian perturbation:

```csharp
MovementEfficency = SetToRandom(a1.MovementEfficency, a2.MovementEfficency);
if (random < MutationRate)
    MovementEfficency = Math.Clamp(MovementEfficency + Gaussian(0, 0.05), 0, 1);
```

#### Implementation plan
1. Add a `Gaussian(double mean, double stddev)` helper to `ExtensionMethods` (Box-Muller transform)
2. Add `MutationStepSize` parameter to `Ecosystem` with UI slider (default 0.1)
3. Change `SetToRandom(double, double, double bias)` to apply Gaussian perturbation instead of full replacement
4. Add trait mutation to `Breed()` and `BreedFromSnapshot()` for all continuous genetic traits

**Files:** `ExtensionMethods.cs`, `Animal.cs`, `Ecosystem.cs`

---

### Step 10: Increase Mutation Rate
**Status:** Not started
**Effort:** Trivial | **Impact:** 🔥🔥🔥

With Gaussian mutation (Step 8), a higher mutation rate is safe — each mutation is a small nudge, not a catastrophe. The current 0.1% rate means most offspring are exact copies of one parent's weights with zero variation. Evolution is glacially slow.

#### Design

Change `MutationRate` default from 0.001 to 0.03 (3%).

For a network with ~150 weights (8 inputs, 1 hidden layer of 12, 4 outputs), this means ~4-5 weights get nudged per offspring instead of ~0.15. Combined with Gaussian perturbation (σ=0.1), each child is a slightly different version of its parents rather than a clone.

#### Implementation plan
1. Change `MutationRate` default value in `Ecosystem` from 0.001 to 0.03

**Files:** `Ecosystem.cs`

---

### Step 11: Rebalance Energy Economics
**Status:** Not started
**Effort:** Small | **Impact:** 🔥🔥🔥

The current energy budget is hostile to survival. A creature with no vision and zero movement still burns 100 energy/second from base drain alone. Food pellets start at 50 energy and max at 500 — meaning a stationary creature needs to find and eat a full-grown pellet every 5 seconds just to break even. With movement and vision costs on top, the math is deeply unfavorable.

#### Problem (energy budget per second at current defaults)
```
Base drain:           100
Movement (speed 10):  ~50  (10 * MovementEfficency * 10, varies)
Vision (150px):        75  (150 * 0.5)
Pregnancy:             50  (if pregnant)
─────────────────────────
Total:             175-275 energy/second
```

Food available: 100 pellets × 500 max energy = 50,000 total energy in the world. With 100 creatures burning 175+ energy/second each, the world's entire food supply is consumed in ~3 seconds. Food regrows at 10/sec per pellet = 1,000/sec total, against 17,500+/sec demand.

#### Design

Adjust defaults to make survival achievable for creatures that find food:

| Parameter | Current | New | Rationale |
|-----------|---------|-----|-----------|
| `BaseEnergyDrainPerSecond` | 100 | 30 | Creatures should survive ~30s without food, not ~10s |
| `FoodMinStartEnergy` | 50 | 200 | New food is worth eating immediately, not just a snack |
| `FoodGrowthRate` | 10 | 30 | Food reaches max faster, supporting larger populations |
| `FoodTargetCount` | 100 | 150 | More food available to find |
| `VisionEnergyCostMultiplier` | 0.5 | 0.05 | Vision should be cheap (see Step 6 rationale) |
| `MovementEnergyCostMultiplier` | 10 | 3 | Moving should be affordable — it's how you find food |

**New energy budget (per second):**
```
Base drain:            30
Movement (speed 10):  ~15  (10 * 0.5 * 3)
Vision (5 rays, 150px): ~4  (150 * 5 * 0.05)
Pregnancy:             50  (unchanged)
─────────────────────────
Total:              49-99 energy/second
```

A single food pellet (200-500 energy) now sustains a creature for 4-10 seconds. This is still challenging — creatures must find food regularly — but it's not impossible.

#### Implementation plan
1. Update default values for the parameters listed above in `Ecosystem`

**Files:** `Ecosystem.cs`

---

### Step 12: Fix Fitness Function
**Status:** Not started
**Effort:** Trivial | **Impact:** 🔥🔥

The current fitness formula makes survival time worth 1 billion per second and babies worth 10 million each. A creature that lives 1 extra second but never breeds outscores one that breeds 99 times. This selects for energy-hoarding hermits, not reproductive success.

#### Design

Rebalance to make reproduction the co-primary driver alongside survival:

```csharp
return LengthOfLife * 1000
     + BabiesCreated * 5000
     + FoodEaten * 1;
```

Now a creature that lives 10 seconds with 0 babies (fitness: 10,000) is beaten by one that lives 8 seconds with 2 babies (fitness: 18,000). Reproduction is rewarded without completely ignoring survival. `FoodEaten` replaces `DistanceTraveled` as the tiebreaker — eating food is a better proxy for useful behavior than random movement.

#### Implementation plan
1. Update `Fitness` property in `Animal.cs`

**Files:** `Animal.cs`

---

### Step 13: Fix Hidden Layer Topology
**Status:** Not started
**Effort:** Small | **Impact:** 🔥🔥

`HiddenNeurons` (0–4) controls the number of hidden layers, not neurons. Each hidden layer always has `_inmaps.Count` neurons. This creates two problems:

1. **Crossover incompatibility:** A 1-layer parent crossed with a 3-layer parent produces a child with either 1 or 3 layers. The 3-layer parent's deep weights have no counterpart in the 1-layer parent, so they're either lost or randomly filled.
2. **Topology explosion:** With 17+ inputs (after multi-ray vision), each hidden layer has 17+ neurons. A 4-layer network has 17×17×4 = 1,156+ hidden weights. This is far too many parameters for evolution to optimize.

#### Design

**Fix the number of hidden layers to 1.** Remove `HiddenNeurons` as a genetic trait.

**Make hidden layer size a configurable ecosystem parameter** instead of matching input count:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `HiddenLayerSize` | 12 | Neurons in the single hidden layer |

This gives a fixed, tractable network topology that evolution can optimize efficiently. With 21 inputs (5-ray vision + 4 recurrent), 12 hidden, 4 outputs: 21×12 + 12×4 = 300 weights. Reasonable for evolutionary optimization.

#### Implementation plan
1. Add `HiddenLayerSize` parameter to `Ecosystem` with UI slider (default 12)
2. Change `NetworkMapper.CreateNetwork()` to accept hidden layer size, always create exactly 1 hidden layer
3. Remove `HiddenNeurons` from `Animal` genetic traits
4. Remove `HiddenNeurons` from `Breed()`, `BreedFromSnapshot()`, `Impregnate()`, constructors
5. Update `CrossoverBrainWeights()` — since all networks now have identical topology, remove the null-check fallback for missing weights

**Files:** `Animal.cs`, `NetworkMapper.cs`, `Ecosystem.cs`

**Alternatives considered:**
- ~~Keep HiddenNeurons as a gene but constrain range (1–2)~~ — Still causes crossover incompatibility. The benefit of variable topology is small compared to the cost of broken inheritance.
- ~~NEAT-style topology evolution~~ — Requires a completely different crossover mechanism (innovation numbers, speciation). Massive complexity increase for a project at this stage.
- ✅ Fixed 1-layer topology with configurable size — simple, predictable, all creatures are crossover-compatible.

---

### Step 14: Tournament Selection
**Status:** Not started
**Effort:** Small | **Impact:** 🔥🔥

Currently the top 30 creatures by fitness become the entire breeding pool. This causes rapid convergence — after a few generations, all creatures are descendants of the same 2-3 high-fitness individuals. Diversity collapses, and the population gets stuck in local optima (usually: be blind, sit still, die slowly).

#### Design

Replace global top-N selection with **tournament selection** in `NewGeneration()`:

For each of the `ElitePopulation` offspring:
1. Pick `TournamentSize` (default 5) random dead creatures
2. Select the best-fitness creature from that group as parent 1
3. Repeat for parent 2 (different tournament)
4. Breed parent 1 × parent 2

This gives weaker creatures a chance to reproduce if they happen to be in a weak tournament. It maintains selection pressure (better creatures still win more often) while preserving genetic diversity.

**New ecosystem parameter:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TournamentSize` | 5 | Creatures per tournament bracket |

Remove `TopBreeders` parameter (no longer needed).

#### Implementation plan
1. Add `TournamentSize` to `Ecosystem` with UI slider
2. Rewrite `NewGeneration()` to use tournament selection instead of top-N
3. Remove `TopBreeders` parameter

**Files:** `Ecosystem.cs`

---

### Step 15: Reduce Random Population Waste
**Status:** Not started
**Effort:** Trivial | **Impact:** 🔥

25% of each generation (25 creatures) are fully random — random weights, random traits. These creatures almost always die immediately, contributing nothing to the gene pool. They exist to add diversity, but they're too different from the evolved population to survive long enough to breed.

#### Design

Two changes:

**1. Reduce random fraction:** Change default from 25 to 10 (10% of 100).

**2. Mutant injection instead of fully random:** Instead of creating fully random creatures, create "heavy mutants" — take a random member of the breeding pool and apply 10× the normal mutation rate to all weights. This produces creatures that are different enough to add diversity but similar enough to the evolved population to have a chance at survival.

```csharp
// Instead of: new Animal(this)  // fully random
// Do: new Animal(this, randomBreeder, mutationMultiplier: 10)
```

#### Implementation plan
1. Change `RandomPopulation` default from 25 to 10
2. Add a constructor or factory method to `Animal` that creates a heavy-mutant copy of a parent
3. Update `NewGeneration()` to use mutant injection for the random population slots

**Files:** `Animal.cs`, `Ecosystem.cs`

---

## Future Ideas (not yet prioritized)
- Adaptive mutation rate (increase during stagnation, decrease during progress)
- Speciation / niching (creatures in different regions evolve independently)
- Sound/pheromone signals between creatures
- Stamina / rest cycles
- Graphs/charts tracking fitness, diversity, and trait distributions over generations
- Save/load best genomes
- Size as a genetic trait (larger = more energy, slower, easier to detect)
- Pack hunting / cooperative behaviors
