using System;

namespace Poolrooms.Core
{
    // Additional built ceiling programs. Their lowest point clears every doorway and
    // leaves the existing pools, furnishings, stairs and protected ground routes intact.
    public static class RoomPrograms
    {
        public static void Describe(Room room,LayoutConfig c)
        {
            room.ExpansionProgram=0;
            if(!c.ExpandedVariety||room.MallGateway||room.TerrainGateway)return;
            var p=room.Plan;
            if(p?.Interior!=null){if(p.Interior.Variant>=3)room.ExpansionProgram=p.Interior.Variant-2;return;}
            if(p?.SoftPlay!=null){if(p.SoftPlay.RoofStyle>=3)room.ExpansionProgram=p.SoftPlay.RoofStyle-2;return;}
            if(p?.Quiet!=null)
            {
                int original=QuietLevelPlanner.IsCivic(p.Environment)?8:6;
                if(p.Quiet.Variant>=original&&p.Environment!=LiminalEnvironment.Coast48&&p.Environment!=LiminalEnvironment.Courtyard188)
                    room.ExpansionProgram=1+(p.Quiet.Variant-original)%2;
                return;
            }
            if(p?.Solar!=null){if(p.Solar.Recipe>=SolarCatalog.BaseCount)room.ExpansionProgram=p.Solar.Recipe/192-3;return;}
            if(p?.Garden!=null){if(p.Garden.Recipe>=OrangeriePlan.BaseCatalogCount)room.ExpansionProgram=p.Garden.Canopy-3;return;}
            if(p?.Baths!=null){if(p.Baths.Recipe>=ThermalBathPlan.BaseCatalogCount)room.ExpansionProgram=p.Baths.DepthClass-3;return;}
            if(p?.Rain!=null||p?.Aquatic!=null||p?.City!=null)return;
            if(room.ZoneId>=0){room.ExpansionProgram=ZoneCatalog.Get(room.ZoneId).ExpansionStyle;return;}
            uint hash=LiminalRegions.Hash(c.Seed,room.X,room.Z,0x310B10u);
            if(hash%3==2)room.ExpansionProgram=1+(int)((hash>>8)%2);
        }
        public static void Append(RoomGeometry output,Room room,LayoutConfig c)
        {
            int program=room.ExpansionProgram;
            if(room.ZoneId>=0)program=ZoneCatalog.Get(room.ZoneId).ExpansionStyle;
            if(program<=0)return;
            float uv=c.TextureWorldSize,h=room.Height,w=room.Width,d=room.Depth;
            float spring=Math.Max(c.DoorHeight+.35f,h-.95f),crown=h-.2f;
            if(crown<=spring+.08f)return;
            void Box(float x,float z,float bw,float bd,float low,float high)
            {ZoneShapes.Box(output.Structure,x,z,bw,bd,low,high,Surface.Accent,uv);}
            if(program==1)
            {
                // Broad transverse segmental ribs, embedded into the actual side walls.
                foreach(int sign in new[]{-1,1})
                {
                    float z=sign*d*.18f,half=w*.5f-.05f;const int sections=16;
                    for(int i=0;i<sections;i++)
                    {
                        float a=-1+2f*i/sections,b=-1+2f*(i+1)/sections;
                        var from=new V3(a*half,spring+(crown-spring)*(1-a*a),z);
                        var to=new V3(b*half,spring+(crown-spring)*(1-b*b),z);
                        ZoneShapes.Beam(output.Structure,from,to,.075f,Surface.Accent,uv,4);
                    }
                }
            }
            else
            {
                // Three stepped perimeter soffits leave a large central ceiling opening.
                for(int tier=0;tier<3;tier++)
                {
                    float inset=(room.Plan?.Solar!=null?room.Plan.Solar.OutlineRadius+.22f:.22f)+tier*.24f,y=spring+tier*(crown-spring)/3;
                    foreach(int sign in new[]{-1,1})
                    {
                        Box(sign*(w*.5f-inset),0,.16f,d-inset*2,y,y+.11f);
                        Box(0,sign*(d*.5f-inset),w-inset*2-.2f,.16f,y+.015f,y+.125f);
                    }
                }
            }
        }
    }
}
