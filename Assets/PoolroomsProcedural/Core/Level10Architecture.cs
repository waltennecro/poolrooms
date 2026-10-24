using System;

namespace Poolrooms.Core
{
    public static class Level10Architecture
    {
        public static RoomGeometry Build(Room room,LayoutConfig config)
        {
            var p=room.Plan?.Level10;if(p==null)throw new ArgumentException("Level10 plan required.");
            var g=new RoomGeometry();g.Structure.Theme=SurfaceTheme.Level10Ground;
            g.Structure.Append(Level10Terrain.Build(p));Level10Terrain.Water(g,p);
            var wood=new MeshData{Theme=SurfaceTheme.Level10Timber};var siding=new MeshData{Theme=SurfaceTheme.Level10Siding};
            if(p.Building)Farm(g,wood,siding,p);
            else LandscapeFeature(g,wood,p);
            Rest(g,wood,p);Hedges(g,p,room,config);
            g.VisualMeshes.Add(wood);g.CollisionOnly.Append(wood);
            g.VisualMeshes.Add(siding);g.CollisionOnly.Append(siding);
            // Structure owns the physical ground; wood/siding use a single visual
            // owner and physics-only copies, so no competing coplanar materials.
            if(config.EncounterExploration)
            {
                g.Life=new RoomLifePlan{Vignette=new RoomVignette("Nivel 10 · "+Level10Activities.Title(p.Place),Level10Activities.Story(p.Place),RoomActivity.Rest,(int)(p.Seed%144))};
                var anchor=Level10Activities.Panel(g,p.Rest.X-2.0f,p.Rest.Z+1.45f);
                g.Life.Notice=anchor;
                g.Life.Scenery=SceneryPlanner.Build(room,g,config);
            }
            foreach(var tree in p.Trees)Forest39Oak.Collider(g.CollisionOnly,tree);
            g.Forest39Canopy=Forest39Oak.Build(p.Trees);
            g.Level10Crops=Level10Crops.Build(p);
            return g;
        }
        public static void Box(MeshData m,float x,float z,float w,float d,float y,float top,Surface s=Surface.Accent)
        {if(w>0&&d>0&&top>y)ZoneShapes.Box(m,x,z,w,d,y,top,s,1.5f);}
        static void Farm(RoomGeometry g,MeshData wood,MeshData siding,Level10Plan p)
        {
            float x=p.Farm.X,z=p.Farm.Z,w=p.Farm.Width,d=p.Farm.Depth;
            float eaves=p.Loft?5.2f:3.6f,ridge=eaves+1.6f+p.RoofStyle*.25f;
            // Flattened earth owns the ground floor; the timber loft is a separate storey.
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                Box(wood,x+sx*(w*.5f-.14f),z+sz*(d*.5f-.14f),.22f,.22f,0,eaves);
            bool open=p.Place==Level10Place.HayShelter||p.Place==Level10Place.LoadingPorch;
            if(!open)
            {
                WallWithWindows(siding,x-w*.5f,z,.16f,d,eaves,true,p.Place==Level10Place.SeedHouse);
                WallWithWindows(siding,x+w*.5f,z,.16f,d,eaves,true,p.Place==Level10Place.SeedHouse);
                Box(siding,x,z+d*.5f,w,.16f,0,eaves,Surface.Tile);
                float door=2.4f,side=(w-door)*.5f;
                Box(siding,x-(door+side)*.5f,z-d*.5f,side,.16f,0,eaves,Surface.Tile);
                Box(siding,x+(door+side)*.5f,z-d*.5f,side,.16f,0,eaves,Surface.Tile);
                Box(siding,x,z-d*.5f,door,.16f,2.65f,eaves,Surface.Tile);
                foreach(int sign in new[]{-1,1})
                {
                    // Doors are parked against the facade, not solid in the opening.
                    Box(wood,x+sign*(door*.5f+.65f),z-d*.5f-.14f,1.2f,.08f,.06f,2.5f);
                    Diagonal(wood,new V3(x+sign*(door*.5f+.16f),.2f,z-d*.5f-.19f),new V3(x+sign*(door*.5f+1.1f),2.3f,z-d*.5f-.19f),.045f);
                }
                Gable(siding,x,z-d*.5f,w,eaves,ridge,-1,p.RoofStyle);Gable(siding,x,z+d*.5f,w,eaves,ridge,1,p.RoofStyle);
            }
            Roof(wood,x,z,w+.7f,d+.7f,eaves,ridge,p.Place==Level10Place.SeedHouse,g,p.RoofStyle);
            for(float zz=z-d*.5f+.35f;zz<z+d*.5f;zz+=1.55f)
            {
                Box(wood,x,zz,w,.12f,eaves-.15f,eaves-.03f);
            }
            if(p.Loft)Loft(wood,p);
            switch(p.Place)
            {
                case Level10Place.Stable:Stalls(wood,p);break;
                case Level10Place.Granary:Bins(wood,p);break;
                case Level10Place.ToolShed:ToolRacks(wood,p);break;
                case Level10Place.SeedHouse:SeedShelves(wood,p);break;
                case Level10Place.PumpCourt:PumpCourt(wood,p);break;
                default:Barrels(wood,p);break;
            }
            // Reused attributed source furniture with real measured dimensions.
            // Everything sits on the ground beside the centre aisle, never in it.
            float tableX=x+w*.5f-1.15f,tableZ=z-d*.5f+.72f;
            g.SourceProps.Add(new SourceFurniturePlacement(100001,7,new V3(tableX,.003f,tableZ),0));
            g.SourceProps.Add(new SourceFurniturePlacement(100002,11,new V3(tableX,.846f,tableZ),0));
            g.SourceProps.Add(new SourceFurniturePlacement(100003,30,new V3(x-.7f,.004f,z+.7f),0));
            if(p.Loft&&d-.55f-Math.Min(4.8f,d-1.4f)-.18f>1.8f)
            {
                g.SourceProps.Add(new SourceFurniturePlacement(100020,7,new V3(x+.35f,p.LoftY+.003f,z+d*.5f-1.0f),0));
                g.SourceProps.Add(new SourceFurniturePlacement(100021,11,new V3(x+.35f,p.LoftY+.846f,z+d*.5f-1.0f),0));
                g.SourceProps.Add(new SourceFurniturePlacement(100022,8,new V3(x+1.4f,p.LoftY+.003f,z+d*.5f-1.0f),90));
            }
        }
        static void WallWithWindows(MeshData m,float x,float z,float thickness,float length,float top,bool alongZ,bool glassHouse)
        {
            float opening=glassHouse?length-.8f:Math.Min(2.2f,length*.38f);
            Box(m,x,z,thickness,length,0,1.0f,Surface.Tile);
            Box(m,x,z,thickness,length,2.3f,top,Surface.Tile);
            float piece=(length-opening)*.5f;
            Box(m,x,z-(opening+piece)*.5f,thickness,piece,1,2.3f,Surface.Tile);
            Box(m,x,z+(opening+piece)*.5f,thickness,piece,1,2.3f,Surface.Tile);
            // Exterior windows really are holes; sash bars cross only the view.
            Box(m,x,z,.22f,.08f,1,2.3f);Box(m,x,z,.23f,opening,1.62f,1.69f);
            foreach(int sign in new[]{-1,1})Box(m,x,z+sign*opening*.5f,.23f,.08f,.95f,2.35f);
        }
        public static V2[] RoofProfile(float width,float eaves,float ridge,int style)
        {
            if(style==1)return new[]{new V2(-width*.5f,eaves),new V2(-width*.23f,eaves+(ridge-eaves)*.76f),new V2(0,ridge),new V2(width*.23f,eaves+(ridge-eaves)*.76f),new V2(width*.5f,eaves)};
            if(style==2)return new[]{new V2(-width*.5f,eaves),new V2(-width*.18f,eaves+.75f),new V2(-width*.18f,ridge),new V2(width*.18f,ridge),new V2(width*.18f,eaves+.75f),new V2(width*.5f,eaves)};
            return new[]{new V2(-width*.5f,eaves),new V2(0,ridge),new V2(width*.5f,eaves)};
        }
        static void Gable(MeshData m,float x,float z,float w,float eaves,float ridge,int sign,int style)
        {
            var points=RoofProfile(w,eaves,ridge,style);
            for(int i=1;i<points.Length;i++)
            {
                var left=points[i-1];var right=points[i];if(right.X-left.X<.001f)continue;
                foreach(int face in new[]{-1,1})
                {
                    float zz=z+face*.035f;V3 n=new V3(0,0,face);
                    m.Quad(new V3(x+left.X,eaves,zz),new V3(x+right.X,eaves,zz),new V3(x+right.X,right.Y,zz),new V3(x+left.X,left.Y,zz),n,Surface.Tile,2);
                }
            }
        }
        static void Roof(MeshData wood,float x,float z,float w,float d,float eaves,float ridge,bool glazed,RoomGeometry g,int style)
        {
            var points=RoofProfile(w,eaves,ridge,style);
            foreach(var point in points)Box(wood,x+point.X,z,.10f,d,point.Y-.04f,point.Y+.04f);
            for(int i=1;i<points.Length;i++)
            {
                var left=points[i-1];var right=points[i];bool monitor=Math.Abs(left.X-right.X)<.001f;
                bool glass=glazed||monitor;var target=glass?g.Glass:wood;
                V3 a=new V3(x+left.X,left.Y,z-d*.5f),b=new V3(x+left.X,left.Y,z+d*.5f),c=new V3(x+right.X,right.Y,z+d*.5f),dd=new V3(x+right.X,right.Y,z-d*.5f);
                V3 n=V3.Cross(b-a,dd-a).Unit;
                target.Quad(a,b,c,dd,n,glass?Surface.Glow:Surface.Tile,2);
                if(glass)
                {
                    var collision=new MeshData();collision.Quad(a,b,c,dd,n,Surface.Tile,2);collision.Quad(a,b,c,dd,n*-1,Surface.Tile,2);g.CollisionOnly.Append(collision);
                }
                else target.Quad(a-V3.Up*.10f,b-V3.Up*.10f,c-V3.Up*.10f,dd-V3.Up*.10f,n*-1,Surface.Tile,2);
                for(float zz=z-d*.5f;zz<=z+d*.5f;zz+=1.4f)Diagonal(wood,new V3(x+left.X,left.Y+.025f,zz),new V3(x+right.X,right.Y+.025f,zz),.065f);
            }
        }
        static void Loft(MeshData wood,Level10Plan p)
        {
            float x=p.Farm.X,z=p.Farm.Z,w=p.Farm.Width,d=p.Farm.Depth,y=p.LoftY;
            // The staircase climbs along the west wall into the back loft; the
            // loft starts beyond its top landing, leaving real overhead clearance.
            float stepsStart=z-d*.5f+.55f,run=Math.Min(4.8f,d-1.4f),stairX=x-w*.5f+.85f;
            int count=16;float tread=run/count;
            for(int i=0;i<count;i++)Box(wood,stairX,stepsStart+(i+.5f)*tread,1.2f,tread,0,y*(i+1)/count);
            float landing=stepsStart+run;
            Box(wood,x,(landing+z+d*.5f-.18f)*.5f,w-.38f,z+d*.5f-.18f-landing,y-.14f,y);
            // Side gallery is offset from the stair footprint and has its own
            // guarded view down the central double-height barn.
            float galleryX=x+w*.5f-1.0f;
            float galleryStart=z-d*.5f+.65f;
            Box(wood,galleryX,(galleryStart+landing)*.5f,1.6f,landing-galleryStart,y-.14f,y);
            for(float zz=galleryStart;zz<landing;zz+=.9f)
                Box(wood,galleryX-.8f,zz,.075f,.075f,y,y+1.05f);
            Box(wood,galleryX-.8f,(galleryStart+landing)*.5f,.08f,landing-galleryStart,y+.94f,y+1.04f);
            Box(wood,galleryX,galleryStart,1.6f,.08f,y+.94f,y+1.04f);
            // The back loft's front rail leaves the stair landing and side walk open.
            float railStart=stairX+.75f,railEnd=galleryX-.9f;
            if(railEnd>railStart)
            {
                Box(wood,(railStart+railEnd)*.5f,landing,railEnd-railStart,.08f,y+.95f,y+1.05f);
                for(float xx=railStart;xx<=railEnd;xx+=.9f)Box(wood,xx,landing,.075f,.075f,y,y+1.05f);
            }
            // Continuous handrail beside the stair, never across a tread.
            Diagonal(wood,new V3(stairX-.67f,1.0f,stepsStart),new V3(stairX-.67f,y+1.0f,landing),.055f);
            for(int i=0;i<=4;i++)Box(wood,stairX-.67f,stepsStart+run*i/4,.06f,.06f,y*i/4,y*i/4+1.0f);
        }
        static void Stalls(MeshData wood,Level10Plan p)
        {
            for(int i=0;i<3;i++)
            {
                float zz=p.Farm.MinZ+1.8f+i*(p.Farm.Depth-2.2f)/3;
                Box(wood,p.Farm.MaxX-1.05f,zz,1.8f,.10f,0,1.3f);
                Box(wood,p.Farm.MaxX-1.95f,zz+.65f,.10f,1.1f,0,.75f);
                Box(wood,p.Farm.MaxX-.65f,zz+.40f,.6f,.45f,0,.35f);
            }
        }
        static void Bins(MeshData wood,Level10Plan p)
        {
            for(int i=0;i<2;i++)
            {
                float xx=p.Farm.X+.55f+i*.9f,zz=p.Farm.MaxZ-1.4f;
                Box(wood,xx,zz,.75f,.75f,0,.06f);
                foreach(int side in new[]{-1,1})Box(wood,xx+side*.36f,zz,.055f,.75f,.06f,.70f);
                Box(wood,xx,zz+.36f,.75f,.055f,.06f,.70f);
            }
        }
        static void ToolRacks(MeshData wood,Level10Plan p)
        {
            float x=p.Farm.MaxX-.6f,z=p.Farm.Z;
            foreach(int side in new[]{-1,1})Box(wood,x,z+side*1.1f,.12f,.12f,0,2.3f);
            for(int shelf=0;shelf<3;shelf++)Box(wood,x,z,.8f,2.4f,.4f+shelf*.65f,.47f+shelf*.65f);
        }
        static void SeedShelves(MeshData wood,Level10Plan p)
        {ToolRacks(wood,p);for(int i=0;i<5;i++)Shapes.Column(wood,p.Farm.MaxX-.6f,p.Farm.Z-.8f+i*.4f,.11f,.47f,.67f,12,Surface.Plaster,1);}
        static void PumpCourt(MeshData wood,Level10Plan p)
        {
            Box(wood,p.Farm.X,p.Farm.MaxZ-1.1f,2.3f,.7f,0,.14f);
            foreach(int side in new[]{-1,1})Box(wood,p.Farm.X+side*1.05f,p.Farm.MaxZ-1.1f,.12f,.7f,.14f,.7f);
            Box(wood,p.Farm.X,p.Farm.MaxZ-.8f,2.2f,.1f,.14f,.7f);
        }
        static void Barrels(MeshData wood,Level10Plan p)
        {
            for(int i=0;i<2;i++)
            {
                float x=p.Farm.MaxX-.65f,z=p.Farm.MaxZ-1.35f-i*1.05f;
                for(int band=0;band<7;band++)
                {
                    float y=band*.12f,r=.30f+.06f*(float)Math.Sin(Math.PI*(band+.5f)/7);
                    Shapes.Column(wood,x,z,r,y,y+.12f,18,Surface.Accent,1);
                }
                foreach(float y in new[]{.15f,.65f})Shapes.Column(wood,x,z,.365f,y,y+.035f,18,Surface.Metal,1);
            }
        }
        static void LandscapeFeature(RoomGeometry g,MeshData wood,Level10Plan p)
        {
            if(p.Place==Level10Place.Lakeside)
            {
                float x=p.Lake.CenterX+p.Lake.Radius+.3f,z=p.Lake.CenterZ;
                for(int i=0;i<5;i++)Box(wood,x,z-1+i*.4f,1.3f,.35f,-.08f,.03f);
                foreach(int side in new[]{-1,1})Box(wood,x-.7f,z+side*.9f,.10f,.10f,-.25f,1.05f);
                Box(wood,x-.7f,z,.08f,1.9f,.96f,1.05f);
            }
            else if(p.Place==Level10Place.HarvestCourt)
            {
                for(int i=0;i<3;i++)
                {
                    var box=new MeshData{Theme=SurfaceTheme.Level10Grain};Box(box,p.Farm.X+(i-1)*1.2f,p.Farm.Z,.85f,1.4f,0,.6f);
                    g.VisualMeshes.Add(box);g.CollisionOnly.Append(box);
                }
            }
            else
            {
                foreach(int side in new[]{-1,1})Box(wood,p.Farm.X+side*1.4f,p.Farm.Z,.14f,.14f,0,2.0f);
                Box(wood,p.Farm.X,p.Farm.Z,3.0f,.2f,1.8f,2.0f);
            }
        }
        static void Rest(RoomGeometry g,MeshData wood,Level10Plan p)
        {
            float x=p.Rest.X,z=p.Rest.Z;
            foreach(int side in new[]{-1,1})
            {Box(wood,x+side*1.4f,z-1.1f,.16f,.45f,0,.45f);Box(wood,x+side*1.4f,z-1.35f,.12f,.12f,.45f,1.0f);}
            for(int slat=0;slat<4;slat++)Box(wood,x,z-1.32f+slat*.15f,3.2f,.12f,.45f,.51f);
            for(int slat=0;slat<3;slat++)Box(wood,x,z-1.43f,3.2f,.07f,.62f+slat*.15f,.72f+slat*.15f);
            g.SourceProps.Add(new SourceFurniturePlacement(100010,7,new V3(x-.95f,.003f,z+.65f),0));
            g.SourceProps.Add(new SourceFurniturePlacement(100011,11,new V3(x-.95f,.846f,z+.65f),0));
            g.SourceProps.Add(new SourceFurniturePlacement(100012,30,new V3(x+2.05f,.004f,z-.65f),0));
        }
        static void Hedges(RoomGeometry g,Level10Plan p,Room room,LayoutConfig c)
        {
            var leaf=new MeshData{Theme=SurfaceTheme.Forest39CanopyLeaves};var frame=new MeshData{Theme=SurfaceTheme.Level10Timber};
            for(int side=0;side<4;side++)
            {
                bool same=Level10Planner.Same(c,room.X,room.Z,side);
                // A shared hedge has a north/east owner, so equal surfaces never
                // compete at the seam. Other-biome boundaries keep an earth bank.
                if(same&&side>=2)continue;
                float opening=room.HasExit(side)?Math.Max(c.DoorWidth*.5f+.3f,p.TrackHalf):0;
                var local=new MeshData{Theme=SurfaceTheme.Forest39CanopyLeaves};
                var barrier=new MeshData();
                for(float at=-p.Half+.4f;at<p.Half-.2f;at+=.8f)
                {
                    if(Math.Abs(at)<opening+.45f)continue;
                    float top=same?1.0f:2.5f;
                    V3 center=new V3(at,top*.52f,p.Half-.22f);
                    for(int face=0;face<2;face++)
                    {
                        V3 right=face==0?new V3(.5f,0,0):new V3(0,0,.3f);V3 up=V3.Up*(top*.56f);
                        V3 normal=face==0?new V3(0,0,-1):new V3(1,0,0);
                        local.QuadUV(center-right-up,center+right-up,center+right+up,center-right+up,normal,normal,normal,normal,new V2(0,0),new V2(1,0),new V2(1,1),new V2(0,1),Surface.Tile);
                    }
                    Box(barrier,at,p.Half-.2f,.8f,.22f,0,same?.8f:2.3f);
                }
                local.TranslateRotate(new V3(),side*Shapes.Pi*.5f);barrier.TranslateRotate(new V3(),side*Shapes.Pi*.5f);
                leaf.Append(local);g.CollisionOnly.Append(barrier);
                if(!same&&room.HasExit(side))
                {
                    float ex=Directions.DX[side]*(p.Half-.3f),ez=Directions.DZ[side]*(p.Half-.3f);
                    foreach(int sign in new[]{-1,1})Box(frame,ex+Directions.DZ[side]*sign*opening,ez-Directions.DX[side]*sign*opening,.15f,.15f,0,2.8f);
                }
            }
            g.VisualMeshes.Add(leaf);g.VisualMeshes.Add(frame);g.CollisionOnly.Append(frame);
        }
        static void Diagonal(MeshData mesh,V3 a,V3 b,float radius)
        {Level10Crops.Stem(mesh,a,b,radius,6,Surface.Accent);}
    }
}
