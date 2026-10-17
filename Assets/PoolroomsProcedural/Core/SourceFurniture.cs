using System;

namespace Poolrooms.Core
{
    // Existing city IDs 0..2 retain their meaning; only new source choices append.
    // Dimensions are normalized from the adapted model bounds and audited against
    // the actual resource JSON, so placement uses the same scale as native loading.
    public static class SourceFurniture
    {
        public static string Id(int source)
        {
            switch(source){case 0:return "WaterBottle";case 1:return "BoomBox";case 2:return "SheenChair";case 3:return "I_GlamVelvetSofa";case 4:return "I_ChairDamaskPurplegold";default:throw new ArgumentOutOfRangeException(nameof(source));}
        }
        public static float Longest(int source){return source==0?.28f:source==1?.4f:source==3?2:1;}
        public static float Longest(string source)
        {if(source=="Lantern")return 1.3f;for(int i=0;i<5;i++)if(Id(i)==source)return Longest(i);throw new ArgumentException("Unknown placed source: "+source);}
        public static V3 Dimensions(int source)
        {
            switch(source)
            {
                case 0:return new V3(.11707850f,.28f,.11707854f);
                case 1:return new V3(.39384329f,.38790759f,.4f);
                case 2:return new V3(1,.83024678f,.68992799f);
                case 3:return new V3(2,.71972764f,.93475457f);
                case 4:return new V3(1,.82952308f,.69010345f);
                default:throw new ArgumentOutOfRangeException(nameof(source));
            }
        }
    }
}
