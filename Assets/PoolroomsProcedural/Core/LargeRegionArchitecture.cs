namespace Poolrooms.Core
{
    public static partial class RestfulLevelArchitecture
    {
        static void RegionalDetails(RoomGeometry o,Room r,LayoutConfig c)
        {
            var p=r.Plan.Restful;float reach=p.Clear+.2f;
            switch(p.Section)
            {
                case RegionSection.Forum:
                    foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                    {
                        float x=sx*reach,z=sz*reach;
                        Shapes.Column(o.Structure,x,z,.09f,0,8.4f,12,Surface.Accent,1.2f);
                        Shapes.Column(o.Decoration,x,z,.15f,8.22f,8.4f,12,Surface.Metal,1.2f);
                    }
                    foreach(int side in new[]{-1,1})
                    {
                        Box(o.Structure,0,side*reach,reach*2,.16f,8.4f,8.58f,Surface.Accent);
                        Box(o.Structure,side*reach,0,.16f,reach*2,8.4f,8.58f,Surface.Accent);
                    }
                    break;
                case RegionSection.Workshops:
                    foreach(var bay in p.Bays)for(int beam=0;beam<4;beam++)
                    {
                        float x=bay.MinX+(beam+.5f)*bay.Width/4;
                        Box(o.Decoration,x,bay.Z,.075f,bay.Depth,5.9f,6.03f,Surface.Accent);
                        foreach(int side in new[]{-1,1})Box(o.Decoration,x,bay.Z+side*bay.Depth*.4f,.015f,.015f,6.03f,r.Height,Surface.Metal);
                    }
                    break;
                case RegionSection.Exhibition:
                    foreach(var bay in p.Bays)
                    {
                        float z=bay.MinZ+.15f,w=bay.Width*.65f;
                        Box(o.Decoration,bay.X,z,w,.035f,4.2f,5.3f,Surface.Slide);
                        foreach(int side in new[]{-1,1})Box(o.Decoration,bay.X+side*w*.4f,z,.014f,.014f,5.3f,r.Height,Surface.Metal);
                        foreach(float h in new[]{4.2f,5.3f})Box(o.Decoration,bay.X,z,w+.07f,.06f,h,h+.035f,Surface.Metal);
                    }
                    break;
                case RegionSection.Rest:
                    foreach(var bay in p.Bays)for(int panel=0;panel<5;panel++)
                    {
                        float x=bay.MinX+(panel+.5f)*bay.Width/5;
                        Box(o.Decoration,x,bay.Z,.1f,bay.Depth*.8f,5.9f,6.35f,Surface.Slide);
                        foreach(int side in new[]{-1,1})Box(o.Decoration,x,bay.Z+side*bay.Depth*.3f,.012f,.012f,6.35f,r.Height,Surface.Metal);
                    }
                    break;
                default:
                    for(int rib=0;rib<3;rib++)
                    {
                        float z=(rib-1)*reach;
                        Box(o.Decoration,0,z,reach*2,.1f,6.5f,6.63f,Surface.Accent);
                        foreach(int side in new[]{-1,1})Box(o.Decoration,side*reach,z,.018f,.018f,6.63f,r.Height,Surface.Metal);
                    }
                    break;
            }
        }
    }
}
