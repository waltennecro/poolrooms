using System;

namespace Poolrooms.Core
{
    // An original calm dreamcore adaptation of Level 11's everyday urban fabric.
    // One genuinely open public pavilion replaces one non-imported building;
    // the other three retain their floors, stairs and enterable shops.
    public static class City11Dreamscape
    {
        public const int ProgramCount=3;
        public static int Pavilion(City11Plan plan)
        {for(int i=0;i<plan.Buildings.Length;i++)if(plan.Buildings[i].ExteriorSource==0)return i;return -1;}
        public static int Program(CityBuildingPlan b){return (int)(b.Seed%ProgramCount);}
        private static void Box(MeshData m,float x,float z,float w,float d,float lo,float hi,Surface s)
        {ZoneShapes.Box(m,x,z,w,d,lo,hi,s,1.2f);}
        public static SurfaceTheme Theme(CityBuildingPlan b)
        {return (b.Seed&1)==0?SurfaceTheme.DreamCityLime:SurfaceTheme.DreamCityClay;}
        public static void BuildPavilion(RoomGeometry o,CityBuildingPlan b,LayoutConfig c)
        {
            float w=b.Width,d=b.Depth;int program=Program(b);
            var body=new MeshData{Theme=Theme(b)};var trim=new MeshData{Theme=Theme(b)};
            var glass=new MeshData{Theme=SurfaceTheme.Glass};var soil=new MeshData{Theme=SurfaceTheme.ConservatorySoil};
            Box(body,0,0,w,d,0,.12f,Surface.Tile);
            // The same real furniture supports remain beneath the pre-existing
            // city pickup anchors; no floating cup or sofa after the replacement.
            float ax=w*.23f;
            EncounterFurniture.Chamfer(body,ax,0,1.25f,.75f,.84f,.88f,.009f,Surface.Accent);
            foreach(int sign in new[]{-1,1})Box(body,ax+sign*.48f,0,.07f,.55f,.12f,.84f,Surface.Metal);
            Box(body,ax,d*.28f,1.4f,.55f,.12f,.92f,Surface.Accent);
            // Three deep arches give a real sequence of thresholds, rather than
            // a label attached to another closed square furnished room.
            int bays=program==0?4:3;
            for(int i=0;i<bays;i++)
            {
                float z=-d*.5f+.28f+i*(d-.56f)/(bays-1);
                var arch=new MeshData();Shapes.Portal(arch,w,3.8f,0,w*.77f,3.48f,.18f,Math.Max(16,c.CurveSegments),1.2f);
                arch.TranslateRotate(new V3(0,.12f,z),0);body.Append(arch);
                foreach(int sign in new[]{-1,1})
                {
                    float x=sign*w*.44f;
                    EncounterFurniture.Chamfer(trim,x,z,.44f,.48f,.12f,.26f,.02f,Surface.Tile);
                    Box(trim,x,z,.45f,.5f,3.58f,3.68f,Surface.Accent);
                }
            }
            // Ribs / alternating real open slots admit the existing sun. The
            // garden gets a pergola; the waiting arcade gets narrow glazed strips.
            for(int i=0;i<7;i++)
            {
                float x=(i-3)*w*.115f;
                Box(body,x,0,.085f,d,3.92f,4.02f,Surface.Metal);
                if(program!=1&&i%2==0)Box(glass,x,0,w*.095f,d-.32f,3.918f,3.927f,Surface.Tile);
            }
            foreach(int sign in new[]{-1,1})Box(body,sign*w*.47f,0,.18f,d,3.79f,3.96f,Surface.Plaster);
            if(program==1||program==2)
            {
                // Garden is on the left of the central passage; original city
                // furniture and front-door crossing stay on the right.
                float px=-w*.26f,pz=program==2?d*.16f:0,pw=w*.23f,pd=d*(program==2?.26f:.5f);
                Box(body,px,pz,pw+.12f,pd+.12f,.12f,.38f,Surface.Tile);
                Box(soil,px,pz,pw,pd,.38f,.395f,Surface.Tile);
                for(int i=0;i<3;i++)
                {
                    float z=pz+(i-1)*pd*.28f;
                    o.SourceSeats.Add(new SourceFurniturePlacement(970100+i,9,b.Point(px,.399f,z),b.Turn?180:0));
                }
            }
            else
            {
                // A long wooden waiting bench and a handrail-sized backrest.
                EncounterFurniture.Chamfer(body,-w*.3f,0,.62f,d*.57f,.45f,.53f,.025f,Surface.Accent);
                foreach(int side in new[]{-1,1})Box(body,-w*.3f,side*d*.23f,.4f,.12f,.12f,.45f,Surface.Metal);
                Box(body,-w*.34f,0,.08f,d*.57f,.52f,.95f,Surface.Accent);
            }
            // Warm practical luminaires are bounded and use the existing light
            // budget / reflection queue. They do not introduce a second sun.
            for(int i=0;i<2;i++)
            {
                float z=(i==0?-1:1)*d*.26f;
                Box(trim,0,z,.75f,.08f,3.75f,3.78f,Surface.Glow);
                o.Fixtures.Add(new Fixture{Position=b.Point(0,3.73f,z),Power=650,Width=.75f,Depth=.08f,Range=5,CustomColor=true,LightColor=new V3(1,.89f,.73f)});
            }
            float angle=b.Turn?Shapes.Pi:0;V3 origin=new V3(b.X,0,b.Z);
            body.TranslateRotate(origin,angle);trim.TranslateRotate(origin,angle);glass.TranslateRotate(origin,angle);soil.TranslateRotate(origin,angle);
            // Collision and floor support live in Structure exactly once; trim
            // carries the separately coloured ceramic/wood finish.
            o.Structure.Append(body);o.VisualMeshes.Add(trim);o.VisualMeshes.Add(soil);o.Glass.Append(glass);o.CollisionOnly.Append(glass);
        }
        public static void Facade(RoomGeometry o,CityBuildingPlan b)
        {
            if(b.ExteriorSource!=0)return;
            var details=new MeshData{Theme=Theme(b)};
            float w=b.Width,d=b.Depth;
            // Projecting ceramic sunshades and rounded entrance surrounds create
            // deep, softly shadowed repeated urban thresholds.
            for(int f=0;f<b.Floors;f++)
            {
                float y=.12f+f*3.2f;
                foreach(int side in new[]{-1,1})
                {
                    float x=side*w*.35f;
                    EncounterFurniture.Chamfer(details,x,-d*.5f-.2f,.15f,.22f,y+.35f,y+2.65f,.014f,Surface.Plaster);
                }
                EncounterFurniture.Chamfer(details,0,-d*.5f-.24f,w*.76f,.4f,y+2.67f,y+2.79f,.012f,Surface.Accent);
            }
            details.TranslateRotate(new V3(b.X,0,b.Z),b.Turn?Shapes.Pi:0);o.VisualMeshes.Add(details);
        }
        public static void Append(RoomGeometry o,Room room,LayoutConfig c)
        {
            if(!c.DreamCity11||room.Plan?.City==null||room.Plan.City.Airfield!=null)return;
            // Softly coloured pavement islands, planting and real licensed cafe
            // objects complement, rather than replace, the drivable street grid.
            int p=Pavilion(room.Plan.City);if(p<0)return;
            var b=room.Plan.City.Buildings[p];float yaw=b.Turn?180:0;
            // Floor/clearance of these anchors is checked against the generated
            // open pavilion in the standalone check and native tests separately.
            o.SourceSeats.Add(new SourceFurniturePlacement(970200,12,b.Point(-b.Width*.28f,.12f,-b.Depth*.32f),yaw));
            o.SourceSeats.Add(new SourceFurniturePlacement(970201,27,b.Point(b.Width*.38f,.12f,-b.Depth*.3f),yaw));
        }
    }
}
