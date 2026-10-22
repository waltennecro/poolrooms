using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // A city block uses the existing room streamer and two combined meshes. No new scene,
    // portal camera, physics world, or unbounded per-window GameObjects are introduced.
    public static class City11Architecture
    {
        static readonly AssemblyKind[] Programs={AssemblyKind.Bookcase,AssemblyKind.Desk,AssemblyKind.Locker,AssemblyKind.SeatGroup,AssemblyKind.DisplayRack,AssemblyKind.PrinterStation,AssemblyKind.FilingCabinet,AssemblyKind.VendingMachine};
        static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface s)
        {ZoneShapes.Box(m,x,z,w,d,bottom,top,s,1.2f);}
        public static RoomGeometry Build(Room room,LayoutConfig c)
        {
            if(!room.Plan.City.Valid(room,c))throw new ArgumentException("Invalid city block.");
            if(room.Plan.City.Airfield!=null)return CityAirfieldArchitecture.Build(room,c);
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.CityFacade;o.Glass.Theme=SurfaceTheme.Glass;
            o.Decoration.EmissionTint=new V3(1,.91f,.77f);o.Decoration.EmissionStrength=.4f;
            Box(o.Structure,0,0,room.Width,room.Depth,-.3f,0,Surface.Tile);
            var road=new MeshData{Theme=SurfaceTheme.CityStreet};
            Road(road,new PlanRect(-3,-room.Depth*.5f,3,room.Depth*.5f));
            Road(road,new PlanRect(-room.Width*.5f,-3,-3,3));Road(road,new PlanRect(3,-3,room.Width*.5f,3));o.VisualMeshes.Add(road);
            double u=(double)room.X*c.CellSize/4,v=(double)room.Z*c.CellSize/4;
            for(int i=0;i<road.UV.Count;i++){var uv=road.UV[i];road.UV[i]=new V2(uv.X+(float)(u-Math.Floor(u)),uv.Y+(float)(v-Math.Floor(v)));}
            int pavilion=c.DreamCity11?City11Dreamscape.Pavilion(room.Plan.City):-1;
            for(int i=0;i<room.Plan.City.Buildings.Length;i++)
            {
                var b=room.Plan.City.Buildings[i];
                if(i==pavilion)City11Dreamscape.BuildPavilion(o,b,c);
                else {Building(o,b,c);if(c.DreamCity11)City11Dreamscape.Facade(o,b);}
            }
            City11Dreamscape.Append(o,room,c);
            CityStreetDetails.Append(o,room,c);
            // Limits retain the graph's actual openings; no opening leads to an absent connector.
            for(int side=0;side<4;side++)
            {
                var edge=new MeshData();float span=room.Width;
                if(room.HasExit(side))
                {
                    float h=CityStreets.OpeningWidth(room,side,c)*.5f;
                    edge.Box(new V3(-span*.5f,0,-.12f),new V3(-h,2.4f,.12f),Surface.Plaster);
                    edge.Box(new V3(h,0,-.12f),new V3(span*.5f,2.4f,.12f),Surface.Plaster);
                }
                else Box(edge,0,0,span,.24f,0,2.4f,Surface.Plaster);
                edge.TranslateRotate(new V3(Directions.DX[side]*room.Width*.5f,0,Directions.DZ[side]*room.Depth*.5f),side*Shapes.Pi*.5f);o.Structure.Append(edge);
            }
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);return o;
        }
        static void Road(MeshData m,PlanRect r)
        {
            Shapes.Horizontal(m,new V3(r.MinX,.003f,r.MinZ),new V3(r.MaxX,.003f,r.MinZ),new V3(r.MaxX,.003f,r.MaxZ),new V3(r.MinX,.003f,r.MaxZ),Surface.Tile,4);
        }
        static void Building(RoomGeometry o,CityBuildingPlan b,LayoutConfig c)
        {
            float w=b.Width,d=b.Depth,h=b.Floors*3.2f+.12f;
            var solid=new MeshData();var detail=new MeshData();var glass=new MeshData();
            Box(solid,0,0,w,d,0,.12f,Surface.Tile);
            // Stair layout reuses the proven 9+9-step dimensions. Floors subtract the shaft.
            float sx=-w*.5f+2.2f;var shaft=new PlanRect(sx-2.1f,-3.6f,sx+2.1f,3.6f);
            for(int level=0;level<b.Floors;level++)
            {
                float y=.12f+level*3.2f;
                if(level>0)
                {
                    var parts=new List<PlanRect>{new PlanRect(-w*.5f,-d*.5f,w*.5f,d*.5f)};PlanRect.Subtract(parts,shaft);
                    foreach(var part in parts)Box(solid,part.X,part.Z,part.Width,part.Depth,y-.15f,y,Surface.Tile);
                }
                // Clear shop aisle on the right, with furniture kept outside the stair landing.
                float ax=w*.23f;
                Box(solid,ax,0,1.25f,.75f,y+.72f,y+.88f,Surface.Accent);
                foreach(int sign in new[]{-1,1})Box(solid,ax+sign*.48f,0,.07f,.55f,y,y+.72f,Surface.Metal);
                Box(solid,ax,d*.28f,1.4f,.55f,y,y+.92f,Surface.Accent);
                for(int row=0;row<3;row++)Box(detail,ax,d*.28f-.29f,1.2f,.035f,y+.15f+row*.22f,y+.17f+row*.22f,Surface.Metal);
                // Programs change actual furniture, not only a building name or paint colour.
                var bounds=new PlanRect(ax-.65f,-d*.28f-.45f,ax+.65f,-d*.28f+.45f);
                var a=new GeneratedAssembly(Programs[(b.Program+level)%Programs.Length],bounds,0,b.Seed+(uint)level);
                var furnish=new MeshData();var ornaments=new MeshData();AutonomousArchitecture.Build(furnish,ornaments,a,c.CurveSegments);
                furnish.TranslateRotate(new V3(0,y,0),0);ornaments.TranslateRotate(new V3(0,y,0),0);solid.Append(furnish);detail.Append(ornaments);
                if(level<b.Floors-1)Stair(solid,detail,sx,y);
                for(int side=0;side<4;side++)
                {
                    float span=side%2==0?w:d;var wall=new MeshData();
                    if(level==0&&side==2)Shapes.Portal(wall,span,3.2f,0,2.2f,2.65f,.22f,8,1.2f);
                    else
                    {
                        // Real window aperture with sill and lintel; not a black rectangle on a wall.
                        Box(wall,0,0,span,.22f,0,.9f,Surface.Plaster);Box(wall,0,0,span,.22f,2.45f,3.2f,Surface.Plaster);
                        float gap=Math.Min(2.8f,span*.45f);
                        Box(wall,-span*.25f-gap*.25f,0,(span-gap)*.5f,.22f,.9f,2.45f,Surface.Plaster);
                        Box(wall,span*.25f+gap*.25f,0,(span-gap)*.5f,.22f,.9f,2.45f,Surface.Plaster);
                        var pane=new MeshData();Box(pane,0,0,gap,.025f,.92f,2.43f,Surface.Tile);
                        pane.TranslateRotate(new V3(Directions.DX[side]*w*.5f,y,Directions.DZ[side]*d*.5f),side*Shapes.Pi*.5f);glass.Append(pane);
                        Box(detail,Directions.DX[side]*w*.5f,Directions.DZ[side]*d*.5f,side%2==0?gap+.2f:.12f,side%2==0?.12f:gap+.2f,y+.86f,y+.94f,Surface.Metal);
                    }
                    wall.TranslateRotate(new V3(Directions.DX[side]*w*.5f,y,Directions.DZ[side]*d*.5f),side*Shapes.Pi*.5f);solid.Append(wall);
                }
                if(level==0||level==b.Floors-1)o.Fixtures.Add(new Fixture{Position=b.Point(w*.23f,y+2.8f,0),Power=1600,Width=1.2f,Depth=.35f,Range=6,CustomColor=true,LightColor=o.Decoration.EmissionTint});
                Box(detail,ax,0,1.2f,.35f,y+2.78f,y+2.82f,Surface.Glow);
                if(b.Facade==2)Box(detail,0,-d*.5f-.12f,w+.15f,.2f,y+3.02f,y+3.14f,Surface.Plaster);
            }
            if(b.Facade==1)foreach(int sign in new[]{-1,1})Box(detail,sign*(w*.5f-.12f),-d*.5f-.1f,.2f,.18f,.12f,h,Surface.Accent);
            if(b.Facade==3)for(float x=-w*.5f+.4f;x<w*.5f-.3f;x+=.38f)if(Math.Abs(x)>1.4f)Box(detail,x,-d*.5f-.14f,.045f,.08f,.2f,2.7f,Surface.Accent);
            Box(solid,0,0,w+.25f,d+.25f,h,h+.18f,Surface.Plaster);
            Box(detail,0,-d*.5f-.08f,w,.12f,2.85f,3.06f,(Surface)(b.Facade%2==0?1:5));
            CityStreetDetails.Facade(solid,detail,b);
            // Physical rail around the stair opening, with the right-side landing exit kept open.
            float angle=b.Turn?Shapes.Pi:0;V3 origin=new V3(b.X,0,b.Z);
            solid.TranslateRotate(origin,angle);detail.TranslateRotate(origin,angle);glass.TranslateRotate(origin,angle);
            o.Structure.Append(solid);o.Decoration.Append(detail);o.Glass.Append(glass);o.CollisionOnly.Append(glass);
        }
        static void Stair(MeshData solid,MeshData detail,float x,float y)
        {
            for(int half=0;half<2;half++)for(int step=0;step<9;step++)
            {
                float z=half==0?-1.35f+(step+.5f)*.3f:1.35f-(step+.5f)*.3f;
                float top=y+half*1.6f+(step+1)*1.6f/9;
                Box(solid,x+(half==0?-.95f:.95f),z,1.5f,.305f,top-.13f,top,Surface.Tile);
            }
            Box(solid,x,1.8f,4.2f,.9f,y+1.44f,y+1.6f,Surface.Tile);
            Box(solid,x,-2,4.2f,1.3f,y+3.04f,y+3.2f,Surface.Tile);
            foreach(var edge in new[]{new PlanRect(x+2.07f,-3.6f,x+2.13f,-2.65f),new PlanRect(x+2.07f,-1.35f,x+2.13f,3.6f),new PlanRect(x-2.1f,-2.68f,x+2.1f,-2.62f)})
            {Box(solid,edge.X,edge.Z,edge.Width,edge.Depth,y+3.2f,y+4.3f,Surface.Metal);}
        }
        public static CorridorGeometry Join(Room a,int direction,LayoutConfig c)
        {
            var o=new CorridorGeometry{Style=CorridorStyle.Straight};o.Structure.Theme=SurfaceTheme.CityStreet;
            float half=CityStreets.OpeningWidth(a,direction,c)*.5f,z=c.CellSize*.5f;
            o.Structure.Box(new V3(-half,-.2f,z-.2f),new V3(half,-.002f,z+.2f),Surface.Tile);
            if(direction==1)o.Structure.TranslateRotate(new V3(),Shapes.Pi*.5f);return o;
        }
    }
}
