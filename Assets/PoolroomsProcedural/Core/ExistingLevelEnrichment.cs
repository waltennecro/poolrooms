using System;

namespace Poolrooms.Core
{
    public static class ExistingLevelEnrichment
    {
        // Additional corner joinery for enclosed programmes. Existing support,
        // skylights, slides, floors, stairs and central sightlines keep ownership.
        public static void Append(RoomGeometry o,Room room,LayoutConfig c)
        {
            if(!c.ExpandedLevelDetails||room.Plan==null||room.Plan.Restful!=null||room.MallGateway||room.TerrainGateway)return;
            if(room.Plan.Rain!=null||room.Plan.City!=null||room.Environment==LiminalEnvironment.Coast48||room.Environment==LiminalEnvironment.Courtyard188)return;
            float y=room.Height-.35f;
            if(y<c.DoorHeight+.65f)return;
            int style=(int)(LiminalRegions.Hash(c.Seed,room.X,room.Z,0x510060u)%4);
            float w=room.Width*.12f,d=room.Depth*.12f;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*room.Width*.36f,z=sz*room.Depth*.36f;
                for(int slat=0;slat<4;slat++)
                {
                    float a=x-w*.5f+(slat+.5f)*w/4;
                    if(style==0)ZoneShapes.Box(o.Decoration,a,z,.045f,d,y-.15f,y,Surface.Accent,c.TextureWorldSize);
                    else if(style==1)ZoneShapes.Box(o.Decoration,x,z-d*.5f+(slat+.5f)*d/4,w,.045f,y-.07f,y,Surface.Plaster,c.TextureWorldSize);
                    else if(style==2)ZoneShapes.Beam(o.Decoration,new V3(a,y,z-d*.5f),new V3(a+w*.15f,y-.17f,z+d*.5f),.025f,Surface.Metal,c.TextureWorldSize,8);
                    else if(slat<2)ZoneShapes.Box(o.Decoration,x,z+(slat-.5f)*d*.5f,w*.85f,d*.4f,y-.08f,y,Surface.Slide,c.TextureWorldSize);
                }
            }
        }
    }
}
