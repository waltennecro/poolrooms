namespace Poolrooms.Core
{
    public static class InteriorPalette
    {
        // 4..15 are reserved for four new programs, with three material moods each.
        public static bool Uses(int variant){return variant>=4&&variant<=15;}
        public static V3 Color(Surface surface,int variant)
        {
            int family=(variant-4)/3,mood=(variant-4)%3;
            V3 floor,wall,wood,fabric;
            switch(family)
            {
                case 0:floor=new V3(.35f,.35f,.28f);wall=new V3(.76f,.74f,.58f);wood=new V3(.46f,.34f,.19f);fabric=new V3(.26f,.32f,.28f);break;
                case 1:floor=new V3(.35f,.28f,.21f);wall=new V3(.78f,.67f,.46f);wood=new V3(.43f,.26f,.13f);fabric=new V3(.16f,.22f,.28f);break;
                case 2:floor=new V3(.33f,.4f,.31f);wall=new V3(.73f,.8f,.7f);wood=new V3(.62f,.48f,.26f);fabric=new V3(.18f,.38f,.22f);break;
                default:floor=new V3(.51f,.44f,.33f);wall=new V3(.8f,.75f,.58f);wood=new V3(.53f,.29f,.12f);fabric=new V3(.61f,.28f,.16f);break;
            }
            float shift=mood==1?.88f:mood==2?1.06f:1;
            switch(surface){case Surface.Tile:return floor*shift;case Surface.Plaster:return wall*shift;case Surface.Accent:return wood*shift;
                case Surface.Slide:return fabric*shift;case Surface.Metal:return new V3(.47f,.49f,.48f);default:return wall;}
        }
        public static float Smoothness(Surface surface,int variant)
        {
            if(surface==Surface.Metal)return .52f;
            if(surface==Surface.Tile)return (variant-4)/3==3?.32f:.07f;
            if(surface==Surface.Slide)return (variant-4)/3==3?.3f:.12f;
            return surface==Surface.Accent?.26f:.16f;
        }
    }
}
