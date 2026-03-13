using Object = AiFun.Entities.Object;

namespace AiFun
{
    public enum VisionHitType
    {
        None,
        Wall,
        AliveCreature,
        DeadCreature,
        Food
    }

    public struct VisionResult
    {
        public VisionHitType HitType { get; set; }
        public double Distance { get; set; }
        public Object HitObject { get; set; }
    }

    public struct RayResult
    {
        /// <summary>
        /// What was hit: 0=nothing, 0.25=wall, 0.5=food, 0.75=creature(dead), 1.0=creature(alive)
        /// </summary>
        public double ObjectType;

        /// <summary>
        /// Inverted distance (closer = higher), 0 if nothing
        /// </summary>
        public double ObjectDistance;

        /// <summary>
        /// Energy of hit object (food/corpse), 0 for walls/alive/nothing
        /// </summary>
        public double ObjectEnergy;
    }
}
