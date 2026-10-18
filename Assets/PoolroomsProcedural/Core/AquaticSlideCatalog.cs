using System;

namespace Poolrooms.Core
{
    public enum AquaticSlideCover { Open, Closed, GlassTube, GlassCrown, Segmented }
    public readonly struct AquaticSlideFamily
    {
        public readonly int Id,SizeProfile,Color;
        public readonly AquaticSlideStyle Route;
        public readonly AquaticSlideCover Cover;
        public AquaticSlideFamily(int id)
        {
            if(id<0||id>=AquaticSlideCatalog.Count)throw new ArgumentOutOfRangeException(nameof(id));
            Id=id;Route=(AquaticSlideStyle)(id%10);Cover=(AquaticSlideCover)(id/10%5);SizeProfile=id/50;
            Color=(id*7+SizeProfile*3)%AquaticSlideCatalog.ColorCount;
        }
        public float Radius {get{return Route==AquaticSlideStyle.FamilyFlume?1.22f+SizeProfile*.025f:1.05f+SizeProfile*.025f;}}
        public float EntryOffset {get{return Route==AquaticSlideStyle.TwinRacer?0:SizeProfile*.18f;}}
        public bool Enclosed {get{return Cover!=AquaticSlideCover.Open;}}
        public float FlowSpeed {get{return AquaticSlideCatalog.FlowSpeed(SizeProfile);}}
        public string Name {get{return Route+" / "+Cover+" / "+(SizeProfile+1);}}
    }
    public static class AquaticSlideCatalog
    {
        public const int Count=250,ColorCount=12;
        public const float FlowTileLength=1.5f;
        public static AquaticSlideFamily Get(int id){return new AquaticSlideFamily(id);}
        public static int At(int recipe){if(recipe<0)throw new ArgumentOutOfRangeException(nameof(recipe));return recipe/7%Count;}
        public static float FlowSpeed(int profile){return .9f+Math.Max(0,Math.Min(4,profile))*.15f;}
        public static float AdvanceFlow(float phase,float dt,int profile,bool paused)
        {
            if(paused||float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return phase;
            return (phase+Math.Min(dt,.25f)*FlowSpeed(profile)/FlowTileLength)%1;
        }
        public static V3 Palette(int id)
        {
            switch((id%ColorCount+ColorCount)%ColorCount)
            {
                case 0:return new V3(.43f,.76f,.64f);case 1:return new V3(.89f,.47f,.53f);
                case 2:return new V3(.38f,.64f,.86f);case 3:return new V3(.92f,.76f,.39f);
                case 4:return new V3(.67f,.53f,.82f);case 5:return new V3(.42f,.81f,.79f);
                case 6:return new V3(.92f,.62f,.41f);case 7:return new V3(.82f,.86f,.8f);
                case 8:return new V3(.36f,.57f,.64f);case 9:return new V3(.75f,.42f,.57f);
                case 10:return new V3(.62f,.76f,.45f);default:return new V3(.93f,.87f,.73f);
            }
        }
    }
}
