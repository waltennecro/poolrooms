using System;

namespace Poolrooms.Core
{
    // All four footprints fit conservatively inside the circular cafe tabletop.
    // Shared by generation, native placement and independent support checks.
    public static class Forest39Supplies
    {
        public const int Count = 3;

        public static int Food(int index)
        {
            switch (index)
            {
                case 0: return ProvisionCatalog.AlmondWater;
                case 1: return 69;
                case 2: return 0;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public static V3 Bottom(Forest39Plan plan, int index)
        {
            V2 offset;
            switch (index)
            {
                case 0: offset = new V2(-.21f, .16f); break;
                case 1: offset = new V2(.04f, .18f); break;
                case 2: offset = new V2(.2f, -.07f); break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
            return new V3(plan.Rest.X + offset.X, SourceFurniture.Dimensions(7).Y + .009f,
                plan.Rest.Z + .5f + offset.Y);
        }

        public static V3 Cup(Forest39Plan plan)
        {
            return new V3(plan.Rest.X - .22f, SourceFurniture.Dimensions(7).Y + .008f, plan.Rest.Z + .41f);
        }
    }
}
