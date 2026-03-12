using AiFun;

namespace AiFun.Tests;

public class SetToRandomTests
{
    [Fact]
    public void SetToRandom_with_low_bias_produces_mostly_parent_values()
    {
        // With bias=0.02 (2% mutation), the vast majority of results
        // should be one of the two parent values, not random.
        double parent1 = 0.1;
        double parent2 = 0.9;
        int parentValueCount = 0;
        int totalTrials = 1000;

        for (int i = 0; i < totalTrials; i++)
        {
            double result = 0.0.SetToRandom(parent1, parent2, 0.02);
            if (Math.Abs(result - parent1) < 0.0001 || Math.Abs(result - parent2) < 0.0001)
                parentValueCount++;
        }

        // With 2% mutation, ~98% should be parent values
        Assert.True(parentValueCount > 900,
            $"Expected >900 parent values out of {totalTrials}, got {parentValueCount}. " +
            "Bias direction may be inverted — low bias should mean low mutation rate.");
    }

    [Fact]
    public void SetToRandom_with_high_bias_produces_mostly_random_values()
    {
        // With bias=0.98 (98% mutation), most results should NOT be parent values.
        double parent1 = 0.1;
        double parent2 = 0.9;
        int randomValueCount = 0;
        int totalTrials = 1000;

        for (int i = 0; i < totalTrials; i++)
        {
            double result = 0.0.SetToRandom(parent1, parent2, 0.98);
            if (Math.Abs(result - parent1) > 0.0001 && Math.Abs(result - parent2) > 0.0001)
                randomValueCount++;
        }

        // With 98% mutation, ~98% should be random values
        Assert.True(randomValueCount > 900,
            $"Expected >900 random values out of {totalTrials}, got {randomValueCount}. " +
            "Bias direction may be inverted — high bias should mean high mutation rate.");
    }
}
