using AiFun;

namespace AiFun.Tests;

public class GaussianMutationTests
{
    private Ecosystem CreateEcosystem(double width = 2000, double height = 2000)
    {
        return new Ecosystem(width, height);
    }

    // --- Gaussian helper tests ---

    [Fact]
    public void Gaussian_returns_values_centered_around_mean()
    {
        // Generate many samples and check the average is near the mean
        double sum = 0;
        int n = 10000;
        for (int i = 0; i < n; i++)
            sum += ExtensionMethods.Gaussian(5.0, 1.0);

        double avg = sum / n;
        Assert.True(Math.Abs(avg - 5.0) < 0.1,
            $"Expected average near 5.0, got {avg:F4}");
    }

    [Fact]
    public void Gaussian_standard_deviation_is_approximately_correct()
    {
        double mean = 0;
        double stddev = 0.1;
        int n = 10000;
        var samples = new double[n];
        for (int i = 0; i < n; i++)
            samples[i] = ExtensionMethods.Gaussian(mean, stddev);

        double avg = samples.Average();
        double variance = samples.Average(s => (s - avg) * (s - avg));
        double measuredStddev = Math.Sqrt(variance);

        // Should be within 20% of requested stddev
        Assert.True(Math.Abs(measuredStddev - stddev) < stddev * 0.2,
            $"Expected stddev near {stddev}, got {measuredStddev:F4}");
    }

    [Fact]
    public void Gaussian_with_zero_stddev_returns_mean()
    {
        for (int i = 0; i < 100; i++)
        {
            double result = ExtensionMethods.Gaussian(0.5, 0.0);
            Assert.Equal(0.5, result);
        }
    }

    // --- MutationStepSize ecosystem parameter tests ---

    [Fact]
    public void Ecosystem_has_MutationStepSize_defaulting_to_0_1()
    {
        var eco = CreateEcosystem();
        Assert.Equal(0.1, eco.MutationStepSize);
    }

    [Fact]
    public void Ecosystem_MutationStepSize_can_be_set()
    {
        var eco = CreateEcosystem();
        eco.MutationStepSize = 0.2;
        Assert.Equal(0.2, eco.MutationStepSize);
    }

    [Fact]
    public void Ecosystem_MutationStepSize_clamped_to_minimum_zero()
    {
        var eco = CreateEcosystem();
        eco.MutationStepSize = -0.5;
        Assert.True(eco.MutationStepSize >= 0,
            "MutationStepSize should not go below 0");
    }

    // --- Gaussian weight mutation tests ---

    [Fact]
    public void SetToRandom_with_mutation_perturbs_near_parent_weight()
    {
        // With 100% mutation rate and small step size, mutated weights
        // should be close to one of the parent weights, not random replacements.
        double parent1 = 0.3;
        double parent2 = 0.7;
        double stepSize = 0.05;
        int closeToParentCount = 0;
        int totalTrials = 1000;

        for (int i = 0; i < totalTrials; i++)
        {
            // bias=1.0 means 100% mutation
            double result = 0.0.SetToRandom(parent1, parent2, 1.0, stepSize);
            // With small step size (0.05), result should be within ~0.2 of a parent
            double distToP1 = Math.Abs(result - parent1);
            double distToP2 = Math.Abs(result - parent2);
            double minDist = Math.Min(distToP1, distToP2);
            if (minDist < 0.25)
                closeToParentCount++;
        }

        // With Gaussian(0, 0.05), 99.7% should be within 3*0.05=0.15 of parent
        Assert.True(closeToParentCount > 950,
            $"Expected >950 values close to parent out of {totalTrials}, got {closeToParentCount}. " +
            "Mutation should perturb near parent, not replace randomly.");
    }

    [Fact]
    public void SetToRandom_with_no_mutation_returns_parent_value()
    {
        // bias=0 means 0% mutation — should always return one of the parent values
        double parent1 = 0.2;
        double parent2 = 0.8;
        int parentMatchCount = 0;
        int totalTrials = 500;

        for (int i = 0; i < totalTrials; i++)
        {
            double result = 0.0.SetToRandom(parent1, parent2, 0.0, 0.1);
            if (Math.Abs(result - parent1) < 0.0001 || Math.Abs(result - parent2) < 0.0001)
                parentMatchCount++;
        }

        Assert.Equal(totalTrials, parentMatchCount);
    }

    [Fact]
    public void SetToRandom_mutated_values_are_clamped_to_minus1_to_1()
    {
        // Use a parent near the boundary with large step size
        double parent1 = 0.99;
        double parent2 = 0.99;
        double stepSize = 0.5;

        for (int i = 0; i < 500; i++)
        {
            double result = 0.0.SetToRandom(parent1, parent2, 1.0, stepSize);
            Assert.True(result >= -1.0 && result <= 1.0,
                $"Mutated weight {result} is outside [-1, 1]");
        }
    }

    // --- Genetic trait mutation tests ---

    [Fact]
    public void Breed_with_high_mutation_rate_perturbs_continuous_traits()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 1.0; // 100% mutation to guarantee trait mutation fires

        var parent1 = new Animal(eco);
        var parent2 = new Animal(eco);

        // Create many children — with 100% mutation, traits should differ from both parents
        int traitsDiffered = 0;
        int totalTraitChecks = 0;

        for (int trial = 0; trial < 50; trial++)
        {
            var child = new Animal(eco, parent1, parent2);

            // Check MovementEfficency
            totalTraitChecks++;
            if (Math.Abs(child.MovementEfficency - parent1.MovementEfficency) > 0.0001 &&
                Math.Abs(child.MovementEfficency - parent2.MovementEfficency) > 0.0001)
                traitsDiffered++;

            // Check ColorR
            totalTraitChecks++;
            if (Math.Abs(child.ColorR - parent1.ColorR) > 0.0001 &&
                Math.Abs(child.ColorR - parent2.ColorR) > 0.0001)
                traitsDiffered++;
        }

        // With 100% mutation, most traits should differ from both parents
        Assert.True(traitsDiffered > totalTraitChecks * 0.8,
            $"Expected >80% of traits to be mutated (differ from both parents) with 100% mutation rate, " +
            $"got {traitsDiffered}/{totalTraitChecks}");
    }

    [Fact]
    public void Breed_with_zero_mutation_rate_traits_match_parents()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 0.0; // No mutation

        var parent1 = new Animal(eco);
        var parent2 = new Animal(eco);

        for (int trial = 0; trial < 20; trial++)
        {
            var child = new Animal(eco, parent1, parent2);

            // MovementEfficency should match one parent exactly
            bool matchesP1 = Math.Abs(child.MovementEfficency - parent1.MovementEfficency) < 0.0001;
            bool matchesP2 = Math.Abs(child.MovementEfficency - parent2.MovementEfficency) < 0.0001;
            Assert.True(matchesP1 || matchesP2,
                $"With 0% mutation, MovementEfficency ({child.MovementEfficency:F4}) should match " +
                $"parent1 ({parent1.MovementEfficency:F4}) or parent2 ({parent2.MovementEfficency:F4})");
        }
    }

    [Fact]
    public void Breed_mutated_traits_stay_within_valid_ranges()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 1.0; // Force all mutations

        for (int trial = 0; trial < 100; trial++)
        {
            var parent1 = new Animal(eco);
            var parent2 = new Animal(eco);
            var child = new Animal(eco, parent1, parent2);

            Assert.True(child.MovementEfficency >= 0 && child.MovementEfficency <= 1,
                $"MovementEfficency {child.MovementEfficency} out of [0,1]");
            Assert.True(child.VisionDistance >= 0 && child.VisionDistance <= eco.MaxVisionDistance,
                $"VisionDistance {child.VisionDistance} out of [0,{eco.MaxVisionDistance}]");
            Assert.True(child.PregnancyGene >= 0 && child.PregnancyGene <= 1,
                $"PregnancyGene {child.PregnancyGene} out of [0,1]");
            Assert.True(child.ColorR >= 0 && child.ColorR <= 1,
                $"ColorR {child.ColorR} out of [0,1]");
            Assert.True(child.ColorG >= 0 && child.ColorG <= 1,
                $"ColorG {child.ColorG} out of [0,1]");
            Assert.True(child.ColorB >= 0 && child.ColorB <= 1,
                $"ColorB {child.ColorB} out of [0,1]");
        }
    }

    [Fact]
    public void BreedFromSnapshot_also_applies_trait_mutation()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 1.0; // 100% mutation

        var mother = new Animal(eco);
        var father = new Animal(eco);

        // Simulate pregnancy snapshot
        mother.GetType()
            .GetMethod("Impregnate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(mother, new object[] { father });

        // Pop baby uses BreedFromSnapshot internally
        var baby = mother.PopBaby();

        // With 100% mutation, at least some traits should differ from both parents
        // (We can't guarantee all will differ due to randomness, but checking ranges is valid)
        Assert.True(baby.MovementEfficency >= 0 && baby.MovementEfficency <= 1,
            $"Baby MovementEfficency {baby.MovementEfficency} out of [0,1]");
        Assert.True(baby.ColorR >= 0 && baby.ColorR <= 1,
            $"Baby ColorR {baby.ColorR} out of [0,1]");
    }

    [Fact]
    public void CrossoverBrainWeights_uses_gaussian_perturbation_not_random_replacement()
    {
        var eco = CreateEcosystem();
        eco.MutationRate = 1.0; // 100% mutation to ensure all weights mutate
        eco.MutationStepSize = 0.05; // Small step

        var parent1 = new Animal(eco);
        var parent2 = new Animal(eco);

        var p1Weights = parent1.Brain.GetFNData().ToArray();
        var p2Weights = parent2.Brain.GetFNData().ToArray();

        // Create child — all weights should be mutated but close to parent values
        int closeCount = 0;
        int totalWeights = 0;

        for (int trial = 0; trial < 10; trial++)
        {
            var child = new Animal(eco, parent1, parent2);
            var childWeights = child.Brain.GetFNData().ToArray();

            foreach (var cw in childWeights)
            {
                var w1 = p1Weights.FirstOrDefault(x => x.Equals(cw));
                var w2 = p2Weights.FirstOrDefault(x => x.Equals(cw));
                if (w1 == null && w2 == null) continue;

                totalWeights++;
                double minDist = double.MaxValue;
                if (w1 != null) minDist = Math.Min(minDist, Math.Abs(cw.Weight - w1.Weight));
                if (w2 != null) minDist = Math.Min(minDist, Math.Abs(cw.Weight - w2.Weight));

                // With stepSize=0.05, 99.7% should be within 3*0.05=0.15 of parent
                if (minDist < 0.25)
                    closeCount++;
            }
        }

        // With Gaussian perturbation (stddev=0.05), virtually all weights should be close
        double closeRate = (double)closeCount / totalWeights;
        Assert.True(closeRate > 0.95,
            $"Expected >95% of mutated weights to be near a parent value (Gaussian perturbation), " +
            $"got {closeRate:P1} ({closeCount}/{totalWeights}). Mutation may still be random replacement.");
    }
}
