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

    public class VisionResult
    {
        public VisionHitType HitType { get; set; }
        public double Distance { get; set; }
        public Object HitObject { get; set; }
    }
}
