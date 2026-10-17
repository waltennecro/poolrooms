using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum TerrainHomeRoom { Living, Kitchen, Bedroom, Study, Bathroom, Laundry, Archive, Waiting,
        Workshop, Pantry, Dining, Storage, Music, Cloakroom, EmptyHall, MirrorKitchen }

    // A connected polyomino home. Doors, floors, cellar excavation and flights share one blueprint.
    public static class TerrainVariedHouseArchitecture
    {
        public const int PickupLimit=32;
        public static TerrainHouseGeometry Build(TerrainHousePlan house,bool detailed,TerrainRegionPlan region)
        {
            var b=house.Blueprint;var o=new TerrainHouseGeometry();o.Geometry.Glass.Theme=SurfaceTheme.Glass;
            int lowest=detailed?b.Lowest:0;
            for(int level=lowest;level<=b.Highest;level++)
            {
                float y=level*TerrainHousePlan.FloorHeight;
                float height=b.Attic&&level==b.Floors?2.5f:TerrainHousePlan.FloorHeight;
                for(int z=0;z<b.Rows;z++)for(int x=0;x<b.Columns;x++)if(b.Has(x,z))
                {
                    float px=b.X(x),pz=b.Z(z);bool core=x==1&&z==0;
                    var floor=detailed?o.Interior.Structure:o.Envelope.Structure;
                    var slabs=new List<PlanRect>{new PlanRect(px-3.5f,pz-3.5f,px+3.5f,pz+3.5f)};
                    if(detailed&&core&&level>b.Lowest)PlanRect.Subtract(slabs,b.StairHole);
                    foreach(var r in slabs)Box(floor,r.X,r.Z,r.Width,r.Depth,y-(level==b.Lowest?.6f:.18f),y,Surface.Tile);
                    for(int side=0;side<4;side++)
                    {
                        int nx=x+(side==1?1:side==3?-1:0),nz=z+(side==2?1:side==0?-1:0);
                        bool neighbour=b.Has(nx,nz);
                        if(neighbour&&!detailed)continue;
                        if(neighbour&&(side==0||side==3))continue;
                        // Selected wings merge into large domestic halls, rather than a grid of identical closed rooms.
                        if(neighbour&&b.Merge(level,x,z,side))continue;
                        bool sideWall=side==1||side==3;float wx=px+(side==1?3.5f:side==3?-3.5f:0),wz=pz+(side==2?3.5f:side==0?-3.5f:0);
                        bool entrance=core&&side==0&&level==0;
                        // Side access to the flight uses the generous front landing, never a stair void.
                        float centre=neighbour&&sideWall&&(core||nx==1&&nz==0)?-2.35f:0;
                        Wall(o,wx,wz,y,height,sideWall,neighbour||entrance,level<0,centre,
                            new V3(side==1?1:side==3?-1:0,0,side==2?1:side==0?-1:0),neighbour,detailed,
                            neighbour?b.PortalHalfWidth(level,x,z,side):1.2f);
                    }
                    if(detailed)
                    {
                        if(core&&level<b.Highest)Stairs(o.Interior.Structure,b,y);
                        if(core&&level>b.Lowest)Railings(o.Interior.Structure,b,y);
                        if(!core)Furnish(o,house,b,level,x,z);
                        if(core||x==b.FarX&&z==b.FarZ)
                        {
                            // Registration is bounded; the world's existing nearest-light budget remains authoritative.
                            V3 light=new V3(px,y+height-.3f,pz-(core?2.7f:0));
                            Box(o.Interior.Decoration,light.X,light.Z,.3f,.3f,light.Y-.025f,light.Y+.025f,Surface.Metal);
                            o.Interior.Fixtures.Add(new Fixture{Position=light,Width=.3f,Depth=.3f,Power=level<0?1100:850,Temperature=level<0?3800:4200,Range=11});
                        }
                    }
                    if(level==b.Highest)
                    {
                        if(!b.Attic)Box(floor,px,pz,7,7,(level+1)*TerrainHousePlan.FloorHeight-.18f,(level+1)*TerrainHousePlan.FloorHeight,Surface.Tile);
                        Roof(o,b,px,pz,!b.Has(x,z-1),!b.Has(x,z+1));
                    }
                }
            }
            float door=b.StairX;
            Box(o.Envelope.Structure,door,-b.Depth*.5f-.65f,3.2f,1.3f,-.2f,0,Surface.Tile);
            Box(o.Envelope.Structure,door,-b.Depth*.5f-1.55f,3.2f,.5f,-.35f,-.18f,Surface.Tile);
            DistrictTower(o.Envelope,house,region);
            if(detailed)
            {
                Box(o.Envelope.Decoration,door+1.25f,-b.Depth*.5f-.14f,.35f,.08f,1.05f,1.42f,Surface.Metal);
                TerrainYardArchitecture.Append(o.Envelope,region,house);
            }
            o.Geometry.Structure.Append(o.Envelope.Structure);o.Geometry.Structure.Append(o.WallFinish);
            o.Geometry.Structure.Append(o.Interior.Structure);o.Geometry.Structure.Append(o.Roof);
            o.Geometry.Decoration.Append(o.Envelope.Decoration);o.Geometry.Decoration.Append(o.Interior.Decoration);
            o.Geometry.Fixtures.AddRange(o.Interior.Fixtures);return o;
        }
        private static void Wall(TerrainHouseGeometry o,float x,float z,float y,float height,bool side,bool door,bool cellar,float centre,V3 outward,bool internalWall,bool detailed,float portalHalf)
        {
            var m=internalWall?o.Interior.Structure:o.Envelope.Structure;
            // Far homes do not need internal room partitions; Build calls this only for shell detail below.
            float half=door?(Math.Abs(centre)>0?1:portalHalf):.8f;
            float sill=door?0:.95f,lintel=door?2.45f:2.15f;
            Action<float,float,float,float> piece=(a,b,low,high)=>
            {
                if(b-a<.001f||high-low<.001f)return;
                var wall=new MeshData();Box(wall,side?x:x+(a+b)*.5f,side?z+(a+b)*.5f:z,side?.18f:b-a,side?b-a:.18f,y+low,y+high,Surface.Plaster);
                if(internalWall){m.Append(wall);return;}
                // Plain plaster faces inward, including concave wings. Paint stays exclusively outside.
                foreach(var indices in wall.Indices)for(int i=0;i<indices.Count;i+=3)
                {
                    int a0=indices[i],a1=indices[i+1],a2=indices[i+2];var target=V3.Dot(wall.Normals[a0],outward)<-.9f?o.WallFinish:m;
                    target.Triangle(wall.Positions[a0],wall.Positions[a1],wall.Positions[a2],wall.Normals[a0],wall.Normals[a1],wall.Normals[a2],wall.UV[a0],wall.UV[a1],wall.UV[a2],Surface.Plaster);
                }
            };
            if(cellar&&!door){piece(-3.5f,3.5f,0,height);return;}
            piece(-3.5f,centre-half,0,height);piece(centre+half,3.5f,0,height);
            piece(centre-half,centre+half,0,sill);piece(centre-half,centre+half,lintel,height);
            if(!detailed)
            {
                if(!door)Box(o.Geometry.Glass,x,z,side?.018f:1.52f,side?1.52f:.018f,y+sill+.03f,y+lintel-.03f,Surface.Glow);
                return;
            }
            if(!door)
            {
                Box(o.Geometry.Glass,x,z,side?.018f:1.52f,side?1.52f:.018f,y+sill+.03f,y+lintel-.03f,Surface.Glow);
                foreach(float dy in new[]{sill,lintel})Trim(o.Envelope.Decoration,x,z,1.7f,y+dy-.04f,y+dy+.04f,side);
                Trim(o.Envelope.Decoration,x,z,.065f,y+sill,y+lintel,side);
                foreach(int edge in new[]{-1,1})Trim(o.Envelope.Decoration,side?x:x+edge*.82f,side?z+edge*.82f:z,.065f,y+sill,y+lintel,side);
            }
            else if(!internalWall)
            {
                foreach(int edge in new[]{-1,1})Box(o.Envelope.Decoration,side?x:x+edge*(half+.03f),side?z+edge*(half+.03f):z,side?.23f:.06f,side?.06f:.23f,y,y+lintel,Surface.Metal);
                Box(o.Envelope.Decoration,x,z,side?.23f:half*2+.12f,side?half*2+.12f:.23f,y+lintel,y+lintel+.06f,Surface.Metal);
            }
            if(!internalWall&&y>=0)
                Box(o.Envelope.Decoration,side?x:x+3.36f,side?z+3.36f:z,.08f,.08f,y+.08f,y+height-.06f,Surface.Metal);
        }
        private static void Stairs(MeshData m,TerrainHouseBlueprint b,float y)
        {
            const int steps=9;float run=2.7f;
            for(int i=0;i<steps;i++)
            {
                float rise=(i+1)*1.6f/steps;
                // Thin structural treads preserve headroom beneath the next stacked flight.
                Box(m,b.StairX-.95f,b.StairZ-1.4f+(i+.5f)*run/steps,1.6f,run/steps,y+rise-.18f,y+rise,Surface.Accent);
                Box(m,b.StairX+.95f,b.StairZ+1.3f-(i+.5f)*run/steps,1.6f,run/steps,y+1.6f+rise-.18f,y+1.6f+rise,Surface.Accent);
            }
            Box(m,b.StairX,b.StairZ+1.9f,4,1.2f,y+1.42f,y+1.6f,Surface.Accent);
            // The central separator guides a U-turn without obstructing either 1.6 m flight.
            Box(m,b.StairX,b.StairZ-.05f,.08f,2.7f,y+1.6f,y+2.45f,Surface.Metal);
            foreach(int side in new[]{-1,1})
            {
                V3 a=new V3(b.StairX+side*1.77f,y+(side<0?0:3.2f)+.95f,b.StairZ-1.4f);
                V3 c=new V3(a.X,y+1.6f+.95f,b.StairZ+1.3f);
                ZoneShapes.Beam(m,a,c,.06f,Surface.Metal,1.2f,4);
            }
        }
        private static void Railings(MeshData m,TerrainHouseBlueprint b,float y)
        {
            foreach(int side in new[]{-1,1})
            {
                Box(m,b.StairX+side*2.04f,b.StairZ+.55f,.07f,3.9f,y+.95f,y+1.02f,Surface.Metal);
                foreach(float dz in new[]{-1.35f,.55f,2.45f})Box(m,b.StairX+side*2.04f,b.StairZ+dz,.07f,.07f,y,y+1.02f,Surface.Metal);
            }
            Box(m,b.StairX,b.StairZ+2.54f,4.12f,.07f,y+.95f,y+1.02f,Surface.Metal);
        }
        private static void Roof(TerrainHouseGeometry o,TerrainHouseBlueprint b,float x,float z,bool front,bool back)
        {
            float e=b.RoofEave,peak=e+b.RoofRise;
            foreach(int side in new[]{-1,1})
            {
                V3 a=new V3(x+side*3.54f,e,z-3.54f),d=new V3(x+side*3.54f,e,z+3.54f);
                V3 c=new V3(x,peak,z+3.54f),bb=new V3(x,peak,z-3.54f);
                V3 normal=new V3(side*b.RoofRise/3.54f,1,0).Unit;
                o.Roof.Quad(a,bb,c,d,normal,Surface.Accent,1.2f);
                o.Roof.Quad(a-V3.Up*.12f,bb-V3.Up*.12f,c-V3.Up*.12f,d-V3.Up*.12f,normal*-1,Surface.Tile,1.2f);
                Box(o.Envelope.Decoration,x+side*3.55f,z,.09f,7.1f,e-.12f,e+.05f,Surface.Metal);
            }
            foreach(int side in new[]{-1,1})
            {
                if(side<0?!front:!back)continue;
                float zz=z+side*3.5f;V3 n=new V3(0,0,side);
                o.Envelope.Structure.Triangle(new V3(x-3.5f,e,zz),new V3(x+3.5f,e,zz),new V3(x,peak,zz),n,n,n,new V2(0,0),new V2(7,0),new V2(3.5f,b.RoofRise),Surface.Plaster);
                o.WallFinish.Triangle(new V3(x-3.5f,e-.12f,zz-side*.08f),new V3(x+3.5f,e-.12f,zz-side*.08f),new V3(x,peak-.12f,zz-side*.08f),n*-1,n*-1,n*-1,new V2(0,0),new V2(7,0),new V2(3.5f,b.RoofRise),Surface.Plaster);
            }
            // Global UVs keep the shingle rows consistent across the many roof wings.
            for(int i=o.Roof.UV.Count-24;i<o.Roof.UV.Count;i++)
                if(i>=0){var p=o.Roof.Positions[i];o.Roof.UV[i]=new V2(p.Z/1.2f,Math.Abs(p.X-x)/1.2f);}
        }
        public static TerrainHomeRoom Program(TerrainHouseBlueprint b,int level,int x,int z,int seed)
        {
            if(level<0)return (TerrainHomeRoom)(5+(x+z+b.Family)%7);
            if(level==b.Floors&&b.Attic)return (TerrainHomeRoom)(6+(x+z+b.Layout)%7);
            if(b.Family<8)
            {
                if(level==0)return (TerrainHomeRoom)((x+z)%5==2?1:(x+z)%5);
                return (x+z)%3==0?TerrainHomeRoom.Bedroom:(x+z)%3==1?TerrainHomeRoom.Study:TerrainHomeRoom.Bathroom;
            }
            // Gentle architectural oddities: repeated kitchens, archives, waiting rooms and oversized domestic halls.
            return (TerrainHomeRoom)(StableRandom.Hash(seed,level,x+z*4,0x9435Cu)%16);
        }
        private static void Furnish(TerrainHouseGeometry o,TerrainHousePlan h,TerrainHouseBlueprint b,int level,int x,int z)
        {
            float px=b.X(x),pz=b.Z(z),y=level*TerrainHousePlan.FloorHeight;
            var kind=Program(b,level,x,z,h.Seed);var solid=o.Interior.Structure;var trim=o.Interior.Decoration;
            // An alcove changes side and extent with the structural profile. Cross routes and front landing access stay open.
            float side=b.Layout==0?-1:1;
            Box(solid,px+side*2.55f,pz-.9f,1.65f,.14f,y,y+2.65f,Surface.Plaster);
            Box(trim,px+side*3.37f,pz+2.25f,.035f,2.2f,y+.02f,y+.13f,Surface.Accent);
            Box(trim,px-3.38f,pz+2.35f,.028f,.085f,y+.3f,y+.43f,Surface.Metal);
            // Abstract framed panels remain mesh details, without additional components or lights.
            if((x+z+level+b.Family)%3==0)
            {
                Box(trim,px-3.39f,pz-.25f,.025f,1.1f,y+1.45f,y+2.0f,Surface.Accent);
                Box(trim,px-3.375f,pz-.25f,.014f,1.02f,y+1.5f,y+1.95f,Surface.Slide);
            }
            float left=px-2.25f,right=px+2.25f,rear=pz+2.3f;
            switch(kind)
            {
                case TerrainHomeRoom.Living:
                    Assembly(o,h,level,x,z,AssemblyKind.SeatGroup,left,rear,1.7f,1.3f);
                    Assembly(o,h,level,x,z,AssemblyKind.Sideboard,right,rear,1.7f,1.1f);break;
                case TerrainHomeRoom.Kitchen:case TerrainHomeRoom.MirrorKitchen:
                    Kitchen(solid,trim,left,rear,y);
                    if(kind==TerrainHomeRoom.MirrorKitchen)Kitchen(solid,trim,right,rear,y);
                    else Assembly(o,h,level,x,z,AssemblyKind.Cabinet,right,rear,1.65f,1.1f);break;
                case TerrainHomeRoom.Bedroom:
                    Assembly(o,h,level,x,z,AssemblyKind.Bed,left,rear,1.65f,1.8f);
                    Assembly(o,h,level,x,z,AssemblyKind.ClothesRail,right,rear,1.65f,1.15f);break;
                case TerrainHomeRoom.Study:
                    Assembly(o,h,level,x,z,AssemblyKind.Desk,left,rear,1.7f,1.6f);
                    Assembly(o,h,level,x,z,AssemblyKind.Bookcase,right,rear,1.65f,1.1f);break;
                case TerrainHomeRoom.Bathroom:
                    Assembly(o,h,level,x,z,AssemblyKind.Vanity,left,rear,1.65f,1.3f);
                    Assembly(o,h,level,x,z,AssemblyKind.ShowerStand,right,rear,1.65f,1.3f);break;
                case TerrainHomeRoom.Laundry:
                    foreach(float xx in new[]{left,right})
                    {
                        Box(solid,xx,rear,1.1f,1.15f,y,y+1.05f,Surface.Metal);
                        var rim=new MeshData();Shapes.Column(rim,0,0,.28f,-.025f,.025f,8,Surface.Accent,1.2f);
                        for(int i=0;i<rim.Positions.Count;i++)
                        {var p=rim.Positions[i];rim.Positions[i]=new V3(p.X,-p.Z,p.Y)+new V3(xx,y+.55f,rear-.59f);p=rim.Normals[i];rim.Normals[i]=new V3(p.X,-p.Z,p.Y);}
                        trim.Append(rim);
                    }
                    break;
                case TerrainHomeRoom.Archive:
                    foreach(float xx in new[]{left,right})Assembly(o,h,level,x,z,AssemblyKind.FilingCabinet,xx,rear,1.65f,1.1f);break;
                case TerrainHomeRoom.Waiting:
                    foreach(float xx in new[]{left,right})Assembly(o,h,level,x,z,AssemblyKind.SeatGroup,xx,rear,1.7f,1.3f);break;
                case TerrainHomeRoom.Workshop:
                    Assembly(o,h,level,x,z,AssemblyKind.EquipmentCart,left,rear,1.65f,1.1f);
                    Assembly(o,h,level,x,z,AssemblyKind.Cabinet,right,rear,1.65f,1.1f);break;
                case TerrainHomeRoom.Pantry:
                    foreach(float xx in new[]{left,right})Assembly(o,h,level,x,z,AssemblyKind.DisplayRack,xx,rear,1.65f,1.1f);break;
                case TerrainHomeRoom.Dining:
                    foreach(float xx in new[]{left,right})Assembly(o,h,level,x,z,AssemblyKind.PlayTable,xx,rear,1.65f,1.4f);break;
                case TerrainHomeRoom.Storage:
                    foreach(float xx in new[]{left,right})for(int i=0;i<3;i++)Box(solid,xx+(i%2)*.45f,rear,.65f,.72f,y+i*.45f,y+(i+1)*.45f,Surface.Accent);break;
                case TerrainHomeRoom.Music:
                    Assembly(o,h,level,x,z,AssemblyKind.Sideboard,left,rear,1.7f,1.1f);
                    Box(trim,left,rear,1.35f,.45f,y+.9f,y+1.15f,Surface.Metal);break;
                case TerrainHomeRoom.Cloakroom:
                    foreach(float xx in new[]{left,right})Assembly(o,h,level,x,z,AssemblyKind.ClothesRail,xx,rear,1.65f,1.15f);break;
                case TerrainHomeRoom.EmptyHall:
                    foreach(float xx in new[]{left,right})Shapes.Column(solid,xx,rear,.25f,y,y+2.65f,8,Surface.Plaster,1.2f);break;
            }
            // Dedicated physical tray: every pickup has stable, independently tested support, clear of circulation.
            float tableZ=pz-2.45f,tableX=px+2.3f;
            // The first row reserves its front strip for offset stair doors; tuck its tray into the side alcove.
            if(z==0){tableX=px-2.3f;tableZ=pz-1.25f;}
            Box(solid,tableX,tableZ,.8f,.65f,y+.68f,y+.76f,Surface.Accent);
            foreach(int sx in new[]{-1,1})Box(solid,tableX+sx*.3f,tableZ,.07f,.45f,y,y+.68f,Surface.Accent);
            int slot=1000+(level+1)*64+(z*4+x)*2;
            MallItemShape shape=kind==TerrainHomeRoom.Kitchen||kind==TerrainHomeRoom.MirrorKitchen?MallItemShape.Mug:
                kind==TerrainHomeRoom.Laundry||kind==TerrainHomeRoom.Bathroom?MallItemShape.Soap:
                kind==TerrainHomeRoom.Music?MallItemShape.Radio:kind==TerrainHomeRoom.Pantry?MallItemShape.FoodBox:MallItemShape.Book;
            int id=(int)shape*8+(int)(StableRandom.Hash(h.Seed,slot,0,0x9435Du)%8);
            var size=MallItemCatalog.Get(id).Size;
            if((x+z*4+(level+1)*7)%3==0&&o.Items.Count<PickupLimit)
                o.Items.Add(new TerrainHouseItem(slot,id,new V3(tableX,y+.76f+size.Y*.5f+.012f,tableZ)));
        }
        private static void Kitchen(MeshData m,MeshData t,float x,float z,float y)
        {
            Box(m,x,z,1.7f,1.05f,y,y+.88f,Surface.Metal);Box(m,x,z,1.75f,1.1f,y+.88f,y+.94f,Surface.Accent);
            Box(t,x-.38f,z,.55f,.65f,y+.94f,y+.97f,Surface.Metal);
            foreach(float dx in new[]{.25f,.6f})Shapes.Column(t,x+dx,z,.12f,y+.94f,y+.97f,8,Surface.Metal,1.2f);
            Box(m,x,z+.38f,1.7f,.35f,y+1.65f,y+2.4f,Surface.Accent);
            foreach(float dx in new[]{-.5f,0,.5f})Box(t,x+dx,z-.54f,.06f,.03f,y+.45f,y+.65f,Surface.Accent);
        }
        private static void Assembly(TerrainHouseGeometry o,TerrainHousePlan h,int level,int cx,int cz,AssemblyKind kind,float x,float z,float w,float d)
        {
            var m=new MeshData();var t=new MeshData();uint seed=StableRandom.Hash(h.Seed,level,cx+cz*4+(int)kind*32,0x9435Eu);
            AutonomousArchitecture.Build(m,t,new GeneratedAssembly(kind,new PlanRect(x-w*.5f,z-d*.5f,x+w*.5f,z+d*.5f),0,seed),8,1.2f,false);
            m.TranslateRotate(new V3(0,level*TerrainHousePlan.FloorHeight,0),0);t.TranslateRotate(new V3(0,level*TerrainHousePlan.FloorHeight,0),0);
            o.Interior.Structure.Append(m);o.Interior.Decoration.Append(t);
        }
        private static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface surface)
        {ZoneShapes.Box(m,x,z,w,d,bottom,top,surface,1.2f);}
        private static void Trim(MeshData m,float x,float z,float span,float lo,float hi,bool side)
        {
            // Exterior and interior faces of slender frames; skip the hidden backs/ends of overlapping boxes.
            foreach(int sign in new[]{-1,1})
            {
                V3 a=side?new V3(x+sign*.115f,lo,z-span*.5f):new V3(x-span*.5f,lo,z+sign*.115f);
                V3 b=side?new V3(x+sign*.115f,lo,z+span*.5f):new V3(x+span*.5f,lo,z+sign*.115f);
                m.Quad(a,b,new V3(b.X,hi,b.Z),new V3(a.X,hi,a.Z),side?new V3(sign,0,0):new V3(0,0,sign),Surface.Metal,1.2f);
            }
        }
        private static void DistrictTower(RoomGeometry g,TerrainHousePlan h,TerrainRegionPlan region)
        {
            // Retain the district's occasional water towers; wider homes need collision-safe placement.
            if(region==null||StableRandom.Hash(h.Seed,0,0,0x94F0u)%23!=0)return;
            foreach(int side in new[]{1,-1})
            {
                float x=side*(h.Width*.5f+16),z=18,wx=h.X+x,wz=h.Z+z;bool valid=region.Contains(wx,wz)&&region.PathDistance(wx,wz)>4;
                int cx=TerrainRegionPlan.Cell(wx),cz=TerrainRegionPlan.Cell(wz);
                for(int iz=cz-1;iz<=cz+1;iz++)for(int ix=cx-1;ix<=cx+1;ix++)
                {var other=region.House(ix,iz);if(other!=null&&Math.Abs(wx-other.X)<other.Width*.5f+3.5f&&Math.Abs(wz-other.Z)<other.Depth*.5f+3.5f)valid=false;}
                if(!valid)continue;
                float baseY=region.Height(wx,wz)-h.Base;
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                {
                    float floor=region.Height(wx+sx*1.65f,wz+sz*1.65f)-h.Base;
                    Box(g.Structure,x+sx*1.65f,z+sz*1.65f,.22f,.22f,floor,baseY+12,Surface.Metal);
                }
                Shapes.Column(g.Structure,x,z,2.25f,baseY+11.8f,baseY+15,12,Surface.Metal,1.2f);
                Box(g.Decoration,x,z,4.5f,4.5f,baseY+11.4f,baseY+11.55f,Surface.Metal);return;
            }
        }
    }
}
