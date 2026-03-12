namespace AiFun
{
    public class GenerationStats
    {
        public int Generation { get; set; }
        public double BestSurvivalTime { get; set; }
        public double AvgSurvivalTime { get; set; }
        public double BestDistance { get; set; }
        public double AvgDistance { get; set; }
        public double AvgVisionDistance { get; set; }
        public int TotalBabies { get; set; }
        public int PopulationPeak { get; set; }
    }
}
