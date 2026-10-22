using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class ArchiveDistrictArchitecture
    {
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            var p=r.Plan.Quiet.Archive;if(p==null)throw new ArgumentException("A Cygnus library plan is required.");
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.CygnusWood;
            o.Glass.Theme=SurfaceTheme.CygnusGlass;o.CollisionOnly.Theme=SurfaceTheme.CygnusWood;
            o.Decoration.EmissionTint=new V3(1,.83f,.58f);o.Decoration.EmissionStrength=.04f;
            var carpet=new MeshData{Theme=SurfaceTheme.CygnusVelvet};var books=new MeshData{Theme=SurfaceTheme.CygnusBindings};
            Box(o.Structure,0,0,p.Cell,p.Cell,-.3f,0,Surface.Tile);
            Floors(o,p);Boundary(o,r,c,p);Vault(o,p);Stairs(o,p);
            if(p.Nave)Nave(o,p,carpet,books);else Wing(o,p,carpet,books);
            Lighting(o,p);if(c.EncounterExploration)Activities(o,p);
            if(carpet.TriangleCount>0)o.VisualMeshes.Add(carpet);
            if(books.TriangleCount>0)o.VisualMeshes.Add(books);
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);
            foreach(var m in o.VisualMeshes)ProceduralArchitecture.Prune(m);
            return o;
        }
        static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface s)
        {if(w>.0001f&&d>.0001f&&top>bottom)ZoneShapes.Box(m,x,z,w,d,bottom,top,s,1.2f);}
        static void Floors(RoomGeometry o,ArchiveDistrictPlan p)
        {
            float h=p.Cell*.5f;
            for(int f=1;f<ArchiveDistrictPlan.Floors;f++)
            {
                var parts=new List<PlanRect>{new PlanRect(-h,-h,h,h)};
                if(p.Nave)
                {
                    PlanRect.Subtract(parts,new PlanRect(-h+ArchiveDistrictPlan.Gallery,-h,h-ArchiveDistrictPlan.Gallery,-1.6f));
                    PlanRect.Subtract(parts,new PlanRect(-h+ArchiveDistrictPlan.Gallery,1.6f,h-ArchiveDistrictPlan.Gallery,h));
                }
                PlanRect.Subtract(parts,p.Stair);
                foreach(var b in parts)Box(o.Structure,b.X,b.Z,b.Width,b.Depth,p.FloorY(f)-.22f,p.FloorY(f),Surface.Tile);
                if(p.Nave)
                {
                    float inner=h-ArchiveDistrictPlan.Gallery,y=p.FloorY(f);
                    foreach(int s in new[]{-1,1})foreach(int z in new[]{-1,1})
                        Rail(o,s*inner,z*(h+1.6f)*.5f,.05f,h-1.6f,y);
                    foreach(int s in new[]{-1,1})Rail(o,0,s*1.6f,2*inner,.05f,y);
                }
                // Stair floor openings remain physical holes with guarded outer edges.
                Rail(o,p.Stair.MinX,p.Stair.Z,.05f,p.Stair.Depth,p.FloorY(f));
                Rail(o,p.Stair.MaxX,p.Stair.Z,.05f,p.Stair.Depth,p.FloorY(f));
                Rail(o,p.Stair.X,p.Stair.MaxZ,p.Stair.Width,.05f,p.FloorY(f));
            }
        }
        static void Boundary(RoomGeometry o,Room r,LayoutConfig c,ArchiveDistrictPlan p)
        {
            float h=p.Cell*.5f;
            for(int side=0;side<4;side++)
            {
                if(ArchiveDistrictPlanner.Same(c,r.X,r.Z,side))continue;
                var wall=new MeshData();float y=0;
                if(r.HasExit(side))Shapes.Portal(wall,p.Cell,p.Spring,0,c.DoorWidth,c.DoorHeight,.28f,12,1.2f);
                else Box(wall,0,0,p.Cell,.28f,0,p.Spring,Surface.Plaster);
                // The external gable fills the vault silhouette; a rectangular cap
                // alone would leave an opening to the void above its spring line.
                if(side%2==0)
                {
                    int n=24;
                    for(int i=0;i<n;i++)
                    {
                        float x0=-h+i*p.Cell/n,x1=-h+(i+1)*p.Cell/n;
                        float a=VaultY(p,x0),b=VaultY(p,x1);
                        V3 a0=new V3(x0,p.Spring,-.14f),b0=new V3(x1,p.Spring,-.14f),a1=new V3(x0,a,-.14f),b1=new V3(x1,b,-.14f);
                        wall.Quad(a0,a1,b1,b0,new V3(0,0,-1),Surface.Plaster,1.2f);
                        wall.Quad(a0+new V3(0,0,.28f),b0+new V3(0,0,.28f),b1+new V3(0,0,.28f),a1+new V3(0,0,.28f),new V3(0,0,1),Surface.Plaster,1.2f);
                    }
                }
                wall.TranslateRotate(new V3(Directions.DX[side]*(h-.14f),y,Directions.DZ[side]*(h-.14f)),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
        }
        static float VaultY(ArchiveDistrictPlan p,float x)
        {return p.Spring+(p.Roof-p.Spring)*(float)Math.Sqrt(Math.Max(0,1-x*x/(p.Cell*p.Cell*.25f)));}
        static void Vault(RoomGeometry o,ArchiveDistrictPlan p)
        {
            float h=p.Cell*.5f;int segments=32;
            var cuts=new List<float>();for(int i=0;i<=segments;i++)cuts.Add(-h+i*p.Cell/segments);
            if(p.Nave)foreach(float x in new[]{-h+1.2f,-h+3,h-3,h-1.2f})cuts.Add(x);
            cuts.Sort();
            int windows=Math.Max(3,(int)(p.Cell/7));
            for(int i=0;i<cuts.Count-1;i++)
            {
                float x0=cuts[i],x1=cuts[i+1];if(x1-x0<.0001f)continue;
                float y0=VaultY(p,x0),y1=VaultY(p,x1),mid=(x0+x1)*.5f;
                bool glazing=p.Nave&&Math.Abs(mid)>h-3&&Math.Abs(mid)<h-1.2f;
                var lengths=new List<float>{-h,h};
                if(glazing)for(int bay=0;bay<windows;bay++)
                {float z=-h+(bay+.5f)*p.Cell/windows;lengths.Add(z-1.1f);lengths.Add(z+1.1f);}
                lengths.Sort();
                for(int j=0;j<lengths.Count-1;j++)
                {
                    float z0=lengths[j],z1=lengths[j+1],zm=(z0+z1)*.5f;bool glass=false;
                    if(glazing)for(int bay=0;bay<windows;bay++)if(Math.Abs(zm-(-h+(bay+.5f)*p.Cell/windows))<1.1f)glass=true;
                    var a=new V3(x0,y0,z0);var b=new V3(x1,y1,z0);var cc=new V3(x1,y1,z1);var d=new V3(x0,y0,z1);
                    var n=V3.Cross(b-a,cc-a).Unit;
                    if(glass)o.Glass.Quad(a,b,cc,d,n,Surface.Tile,1.2f);
                    else
                    {
                        // A vaulted shell with actual holes for coloured glazing.
                        o.Structure.Quad(a,b,cc,d,n,Surface.Plaster,1.2f);
                        o.Structure.Quad(a+V3.Up*.18f,d+V3.Up*.18f,cc+V3.Up*.18f,b+V3.Up*.18f,n*-1,Surface.Plaster,1.2f);
                    }
                }
            }
            int bays=Math.Max(3,(int)(p.Cell/7));
            for(int bay=0;bay<bays;bay++)
            {
                float z=-h+(bay+.5f)*p.Cell/bays;
                for(int i=0;i<segments;i++)
                {
                    float x0=-h+i*p.Cell/segments,x1=-h+(i+1)*p.Cell/segments;
                    ZoneShapes.Beam(o.Decoration,new V3(x0,VaultY(p,x0)-.09f,z),new V3(x1,VaultY(p,x1)-.09f,z),.08f,Surface.Accent,1.2f,6);
                }
                foreach(int side in new[]{-1,1})
                {
                    float x=side*(h-ArchiveDistrictPlan.Gallery-.25f);
                    if(!p.Nave)continue;
                    Shapes.Column(o.Structure,x,z,.21f,0,p.Spring,12,Surface.Plaster,1.2f);
                    for(int f=0;f<3;f++)Shapes.Column(o.Decoration,x,z,.29f,p.FloorY(f)+.05f,p.FloorY(f)+.2f,12,Surface.Metal,1.2f);
                }
            }
            // Lead and perimeter ribs follow the curved glazing. The roof holes
            // were subtracted above, so these are windows rather than glass props.
            if(p.Nave)foreach(int side in new[]{-1,1})for(int bay=0;bay<bays;bay++)
            {
                float x0=side*(h-3),x1=side*(h-1.2f),z=-h+(bay+.5f)*p.Cell/bays;
                foreach(float x in new[]{x0,x1,(x0+x1)*.5f})
                    ZoneShapes.Beam(o.Decoration,new V3(x,VaultY(p,x)-.02f,z-1.1f),new V3(x,VaultY(p,x)-.02f,z+1.1f),.025f,Surface.Metal,1.2f,6);
                foreach(int sz in new[]{-1,1})for(int step=0;step<8;step++)
                {
                    float a=x0+(x1-x0)*step/8,b=x0+(x1-x0)*(step+1)/8;
                    ZoneShapes.Beam(o.Decoration,new V3(a,VaultY(p,a)-.02f,z+sz*1.1f),new V3(b,VaultY(p,b)-.02f,z+sz*1.1f),.035f,Surface.Metal,1.2f,6);
                }
            }
        }
        static void Nave(RoomGeometry o,ArchiveDistrictPlan p,MeshData carpet,MeshData books)
        {
            float h=p.Cell*.5f,inner=h-ArchiveDistrictPlan.Gallery;
            Carpet(carpet,0,0,Math.Min(6,p.Aisle*2),p.Cell,0);
            foreach(int sign in new[]{-1,1})for(int f=0;f<3;f++)
            {
                float y=p.FloorY(f),x=sign*(h-.8f);
                for(float z=-h+2.2f;z<h-1.8f;z+=Math.Max(3.2f,p.Cell/6))
                {
                    if(Math.Abs(z)<p.Aisle+1.6f||sign<0&&z<p.Stair.MaxZ+.9f)continue;
                    Shelf(o,books,x,z,y,2.5f,sign>0?90:-90,p.Seed+(uint)(f*79+(int)(z+h)));
                }
                if(f>0)Carpet(carpet,sign*(inner+2.5f),h*.45f,2,p.Cell*.28f,y);
            }
            // Reading islands lie outside both through routes and stair approaches.
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*inner*.51f,z=sz*h*.55f;
                Reading(o,x,z,0,p.Seed+(uint)(sx+sz+5),false);
            }
            // Bridges have a warm runner and can be crossed on both upper storeys.
            for(int f=1;f<3;f++)Carpet(carpet,0,0,2*inner,2.2f,p.FloorY(f));
        }
        static void Wing(RoomGeometry o,ArchiveDistrictPlan p,MeshData carpet,MeshData books)
        {
            float h=p.Cell*.5f;
            for(int f=0;f<3;f++)
            {
                float y=p.FloorY(f);ArchiveWingUse use=(ArchiveWingUse)(((int)p.Use+f)%5);
                Carpet(carpet,0,0,p.Aisle*2,p.Cell,y);
                // Full-height pointed arcades divide long reading wings, not four
                // isolated square furniture bays. The broad central cross stays free.
                foreach(int side in new[]{-1,1})
                {
                    if(side*h*.49f>p.Stair.MinZ-.4f&&side*h*.49f<p.Stair.MaxZ+.4f)continue;
                    var arch=new MeshData();Shapes.Portal(arch,p.Cell,p.Rise(f),0,p.Aisle*2,p.Rise(f)-.35f,.22f,16,1.2f);
                    arch.TranslateRotate(new V3(0,y,side*h*.49f),0);o.Structure.Append(arch);
                }
                for(float x=-h+2;x<h-1.5f;x+=Math.Max(3.3f,p.Cell/6))foreach(int sign in new[]{-1,1})
                {
                    float z=sign*(h-1.0f);
                    if(Math.Abs(x)<p.Aisle+1.6f||x<p.Stair.MaxX+1.6f&&z<p.Stair.MaxZ+1.2f)continue;
                    Shelf(o,books,x,z,y,2.5f,sign>0?0:180,p.Seed+(uint)(f*53+(int)(x+h)));
                }
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                {
                    float x=sx*h*.48f,z=sz*h*.7f;
                    if(sx<0&&sz<0&&new PlanRect(x-3,z-2,x+3,z+2).Overlaps(p.Stair.Expanded(.7f)))continue;
                    if(use==ArchiveWingUse.Guest)Guest(o,carpet,x,z,y);
                    else if(use==ArchiveWingUse.Stacks)
                    {
                        Shelf(o,books,x,z,y,3.4f,90,p.Seed+(uint)(f*47+sx+sz+5));
                        Shelf(o,books,x+sx*2.8f,z,y,3.4f,90,p.Seed+(uint)(f*43+sx+sz+11));
                    }
                    else Reading(o,x,z,y,p.Seed+(uint)(f*31+sx+sz+5),use==ArchiveWingUse.Tea);
                }
                Source(o,9,h*.75f,h*.3f,y);
                if(use==ArchiveWingUse.Tea)Kitchen(o,p,y);
                if(use==ArchiveWingUse.Scriptorium)
                {
                    // Real pigeonholes, drawers, paper stacks and a returning desk.
                    float x=h*.65f,z=h*.7f;
                    for(int row=0;row<3;row++)for(int col=0;col<4;col++)
                    {
                        Box(o.Structure,x-1.1f+col*.55f,z+1.4f,.5f,.45f,y+.65f+row*.4f,y+.7f+row*.4f,Surface.Accent);
                        Box(o.Decoration,x-1.1f+col*.55f,z+1.35f,.35f,.24f,y+.71f+row*.4f,y+.77f+row*.4f,Surface.Slide);
                    }
                }
            }
        }
        static void Carpet(MeshData m,float x,float z,float w,float d,float y)
        {
            Box(m,x,z,w,d,y+.003f,y+.009f,Surface.Tile);
            foreach(int side in new[]{-1,1})Box(m,x+side*(w*.5f-.08f),z,.045f,d,y+.01f,y+.013f,Surface.Metal);
        }
        static void Shelf(RoomGeometry o,MeshData books,float x,float z,float y,float width,float yaw,uint seed)
        {
            var wood=new MeshData();var binding=new MeshData();
            foreach(int s in new[]{-1,1})Box(wood,s*(width*.5f-.055f),0,.11f,.52f,0,2.85f,Surface.Accent);
            Box(wood,0,.245f,width,.035f,0,2.85f,Surface.Accent);
            Box(wood,0,0,width+.14f,.6f,2.8f,2.92f,Surface.Accent);
            var random=new StableRandom(seed);
            for(int shelf=0;shelf<6;shelf++)
            {
                float sy=.12f+shelf*.44f;Box(wood,0,0,width,.52f,sy,sy+.045f,Surface.Accent);
                float xx=-width*.5f+.15f;int book=0;
                while(xx<width*.5f-.18f)
                {
                    float bw=Math.Min(random.Range(.055f,.115f),width*.5f-.13f-xx),height=random.Range(.22f,.36f),depth=random.Range(.25f,.37f);
                    Surface colour=(Surface)(book%4==0?2:book%4==1?1:book%4==2?5:0);
                    Box(binding,xx+bw*.5f,-.04f,bw-.008f,depth,sy+.047f,sy+.047f+height,colour);
                    foreach(float band in new[]{.07f,.18f})
                    {
                        float left=xx+bw*.14f,right=xx+bw*.86f,zz=-.04f-depth*.5f-.004f,yy=sy+.05f+band;
                        binding.Quad(new V3(left,yy,zz),new V3(left,yy+.008f,zz),new V3(right,yy+.008f,zz),new V3(right,yy,zz),new V3(0,0,-1),Surface.Metal,.2f);
                    }
                    xx+=bw+.018f;book++;
                }
            }
            wood.TranslateRotate(new V3(x,y,z),yaw*Shapes.Pi/180);binding.TranslateRotate(new V3(x,y,z),yaw*Shapes.Pi/180);
            o.Structure.Append(wood);books.Append(binding);
        }
        static void Reading(RoomGeometry o,float x,float z,float y,uint seed,bool tea)
        {
            Table(o,x,z,y,2.8f,1.05f,.78f);
            foreach(int sign in new[]{-1,1})
            {
                Source(o,22,x+sign*.85f,z-1.15f,y,180);
                Source(o,tea?11:18,x+sign*.8f,z+.12f,y+.789f);
            }
            Source(o,tea?12:0,x,z,y+.789f);
            // Original loose sheets have visible edge thickness and remain on the tabletop.
            Box(o.Decoration,x,z-.22f,.42f,.28f,y+.781f,y+.785f,Surface.Slide);
        }
        static void Guest(RoomGeometry o,MeshData cloth,float x,float z,float y)
        {
            // Guest alcoves have physical side walls and a real arched entrance.
            // They face the inner reading hall and retain a clear approach.
            int sign=z>0?1:-1;
            foreach(int side in new[]{-1,1})Box(o.Structure,x+side*2.6f,z,.14f,4,y,y+3.1f,Surface.Plaster);
            Box(o.Structure,x,z+sign*2,5.2f,.14f,y,y+3.1f,Surface.Plaster);
            var entry=new MeshData();Shapes.Portal(entry,5.2f,3.1f,0,1.4f,2.5f,.14f,12,1.2f);
            entry.TranslateRotate(new V3(x,y,z-sign*2),sign>0?0:Shapes.Pi);o.Structure.Append(entry);
            // Framed divan, cushions and a bedside table are inside the alcove.
            Box(o.Structure,x,z,2.1f,1.1f,y+.15f,y+.38f,Surface.Accent);
            Box(o.Structure,x,z+.51f,2.12f,.12f,y+.25f,y+1.1f,Surface.Accent);
            Box(o.CollisionOnly,x,z,2.03f,.96f,y+.38f,y+.55f,Surface.Tile);
            Box(cloth,x,z,2.03f,.96f,y+.38f,y+.55f,Surface.Tile);
            foreach(int s in new[]{-1,1})Box(cloth,x+s*.6f,z+.22f,.6f,.45f,y+.55f,y+.7f,Surface.Tile);
            Table(o,x+2.0f,z,y,.7f,.65f,.65f);Source(o,11,x+2,z,y+.659f);
        }
        static void Kitchen(RoomGeometry o,ArchiveDistrictPlan p,float y)
        {
            float x=p.Cell*.5f-1,z=p.Cell*.14f;
            // Separate carcass, inset cupboards, brass pulls, stone worktop,
            // recessed sink and a hob. The tea activity handles pouring/serving.
            Box(o.Structure,x,z,.85f,3.2f,y+.08f,y+.82f,Surface.Accent);
            var worktop=new List<PlanRect>{new PlanRect(x-.46f,z-1.64f,x+.46f,z+1.64f)};
            PlanRect.Subtract(worktop,new PlanRect(x-.27f,z+.42f,x+.27f,z+.88f));
            foreach(var b in worktop)Box(o.Structure,b.X,b.Z,b.Width,b.Depth,y+.82f,y+.88f,Surface.Plaster);
            for(int door=0;door<4;door++)
            {
                float dz=z-1.2f+door*.8f;
                Box(o.Decoration,x-.44f,dz,.035f,.72f,y+.13f,y+.75f,Surface.Accent);
                Box(o.Decoration,x-.47f,dz+.22f,.035f,.16f,y+.59f,y+.62f,Surface.Metal);
            }
            // Sink rim has an open centre; its bottom is recessed into the top.
            foreach(int side in new[]{-1,1})
            {Box(o.Decoration,x+side*.29f,z+.65f,.045f,.55f,y+.881f,y+.91f,Surface.Metal);Box(o.Decoration,x,z+.65f+side*.255f,.58f,.045f,y+.881f,y+.91f,Surface.Metal);}
            ZoneShapes.Beam(o.Decoration,new V3(x+.28f,y+.9f,z+.65f),new V3(x+.28f,y+1.14f,z+.65f),.018f,Surface.Metal,1.2f,8);
            ZoneShapes.Beam(o.Decoration,new V3(x+.28f,y+1.14f,z+.65f),new V3(x,y+1.14f,z+.65f),.018f,Surface.Metal,1.2f,8);
            foreach(int side in new[]{-1,1})Shapes.Column(o.Decoration,x,z-.85f+side*.22f,.15f,y+.882f,y+.889f,18,Surface.Metal,1.2f);
        }
        static void Table(RoomGeometry o,float x,float z,float y,float width,float depth,float height)
        {
            Box(o.Structure,x,z,width,depth,y+height-.065f,y+height,Surface.Accent);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box(o.Structure,x+sx*(width*.5f-.15f),z+sz*(depth*.5f-.14f),.085f,.085f,y,y+height-.065f,Surface.Accent);
            foreach(int s in new[]{-1,1})Box(o.Decoration,x+s*(width*.5f-.1f),z,.04f,depth-.08f,y+height+.001f,y+height+.006f,Surface.Metal);
        }
        static void Source(RoomGeometry o,int source,float x,float z,float y,float yaw=0)
        {
            if(o.SourceProps.Count+o.SourceSeats.Count>=48)return;
            var p=new SourceFurniturePlacement(906000+o.SourceProps.Count+o.SourceSeats.Count,source,new V3(x,y+.006f,z),yaw);
            if(SourceFurniture.IsSeat(source))o.SourceSeats.Add(p);else o.SourceProps.Add(p);
        }
        static void Rail(RoomGeometry o,float x,float z,float w,float d,float y)
        {
            Box(o.CollisionOnly,x,z,w,d,y,y+1.08f,Surface.Metal);
            Box(o.Decoration,x,z,w+.025f,d+.025f,y+1.025f,y+1.08f,Surface.Metal);
            int count=Math.Max(1,(int)(Math.Max(w,d)/.32f));
            for(int i=0;i<=count;i++)Box(o.Decoration,w>d?x-w*.5f+w*i/count:x,d>=w?z-d*.5f+d*i/count:z,.025f,.025f,y,y+1.025f,Surface.Metal);
        }
        public static void Stairs(RoomGeometry o,ArchiveDistrictPlan p)
        {
            var b=p.Stair;float start=b.MinZ+ArchiveDistrictPlan.Landing,end=start+ArchiveDistrictPlan.Flight;
            for(int f=0;f<2;f++)
            {
                float y=p.FloorY(f),rise=p.Rise(f);int steps=p.Steps(f);float tread=ArchiveDistrictPlan.Flight/steps;
                for(int flight=0;flight<2;flight++)
                {
                    float x=b.X+(flight==0?-.9f:.9f);
                    for(int step=0;step<steps;step++)
                    {
                        float z=flight==0?start+(step+.5f)*tread:end-(step+.5f)*tread;
                        float top=y+flight*rise*.5f+(step+1)*rise*.5f/steps;
                        Box(o.Structure,x,z,1.62f,tread+.002f,top-.14f,top,Surface.Accent);
                        Box(o.Decoration,x,z+(flight==0?-1:1)*(tread*.5f-.025f),1.6f,.035f,top+.001f,top+.005f,Surface.Metal);
                    }
                    foreach(int s in new[]{-1,1})
                    {
                        float rx=x+s*.79f;
                        V3 a=new V3(rx,y+flight*rise*.5f+1.04f,flight==0?start:end),bb=new V3(rx,y+(flight+1)*rise*.5f+1.04f,flight==0?end:start);
                        ZoneShapes.Beam(o.Decoration,a,bb,.035f,Surface.Metal,1.2f,8);
                        // A continuous sloping collision sheet closes the spaces between balusters.
                        V3 baseA=a-V3.Up*1.04f,baseB=bb-V3.Up*1.04f;
                        o.CollisionOnly.Quad(baseA,baseB,bb,a,new V3(s,0,0),Surface.Metal,1.2f);
                        o.CollisionOnly.Quad(baseA,a,bb,baseB,new V3(-s,0,0),Surface.Metal,1.2f);
                        for(int post=0;post<=steps;post+=2)
                        {
                            float z=flight==0?start+post*tread:end-post*tread,by=y+flight*rise*.5f+post*rise*.5f/steps;
                            Box(o.Decoration,rx,z,.025f,.025f,by,by+1.04f,Surface.Metal);
                        }
                    }
                }
                Box(o.Structure,b.X,(end+b.MaxZ)*.5f,b.Width,b.MaxZ-end,y+rise*.5f-.16f,y+rise*.5f,Surface.Accent);
                Box(o.Structure,b.X,(b.MinZ+start)*.5f,b.Width,start-b.MinZ,y+rise-.16f,y+rise,Surface.Accent);
            }
        }
        static void Lighting(RoomGeometry o,ArchiveDistrictPlan p)
        {
            float h=p.Cell*.5f;
            for(int f=0;f<3;f++)foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,0,1})
            {
                float x=p.Nave?sx*(h-2.5f):sx*p.Cell*.25f,z=sz*p.Cell/3,y=p.FloorY(f)+p.Rise(f)-.5f;
                float range=Math.Max(12,p.Cell*(p.Nave?.25f:.4f)),power=4200*p.Cell*p.Cell/(36*36);
                Box(o.Decoration,x,z,.5f,.35f,y,y+.045f,Surface.Metal);
                Box(o.Decoration,x,z,.4f,.26f,y-.035f,y,Surface.Glow);
                float ceiling=f==2?VaultY(p,x):p.FloorY(f+1)-.22f;
                ZoneShapes.Beam(o.Decoration,new V3(x,y+.045f,z),new V3(x,ceiling-.04f,z),.018f,Surface.Metal,1.2f,6);
                o.Fixtures.Add(new Fixture{Position=new V3(x,y-.07f,z),Width=.4f,Depth=.26f,Power=power,Range=range,Temperature=2950,MallLocal=true,FloorOffset=p.Rise(f)-.57f,StoreyHeight=p.Rise(f),ShadowlessRange=range});
            }
            if(!p.Nave)return;
            // Chandeliers have visible suspension, a brass ring and wax candles.
            foreach(int sz in new[]{-1,1})
            {
                float z=sz*h*.48f,y=6.4f;
                ZoneShapes.Beam(o.Decoration,new V3(0,y+.5f,z),new V3(0,p.Roof-.25f,z),.022f,Surface.Metal,1.2f,8);
                for(int i=0;i<12;i++)
                {
                    float a=i*Shapes.Pi/6,b=(i+1)*Shapes.Pi/6;
                    var aa=new V3(1.4f*Shapes.Cos(a),y,z+1.4f*Shapes.Sin(a));var bb=new V3(1.4f*Shapes.Cos(b),y,z+1.4f*Shapes.Sin(b));
                    ZoneShapes.Beam(o.Decoration,aa,bb,.045f,Surface.Metal,1.2f,8);
                    Shapes.Column(o.Decoration,aa.X,aa.Z,.045f,y,y+.25f,8,Surface.Plaster,1.2f);
                    Shapes.Column(o.Decoration,aa.X,aa.Z,.018f,y+.25f,y+.29f,6,Surface.Glow,1.2f);
                }
                o.Fixtures.Add(new Fixture{Position=new V3(0,y-.08f,z),Width=2.8f,Depth=2.8f,Power=10500,Range=p.Cell*.8f,Temperature=2850});
            }
        }
        static void Activities(RoomGeometry o,ArchiveDistrictPlan p)
        {
            string[] names={"Los estantes de otras lenguas","Una mesa reservada","El taller de los copistas","La cocina del té","El descanso de los huéspedes"};
            var life=new RoomLifePlan{Vignette=new RoomVignette(p.Nave?"La gran nave de Cygnus":names[(int)p.Use],
                "Una taza espera junto al catálogo. Los marcadores no señalan libros perdidos, sino lecturas que alguien quiere compartir. Las galerías superiores siguen abiertas; el archivo continúa al otro lado de los arcos.",RoomActivity.Reading,(int)(p.Seed%144))};
            float x=p.Aisle+.85f,z=-p.Cell*.15f;
            life.Notice=Panel(o,x,z,0,0);
            life.Stations.Add(new StationPlacement(Panel(o,-x,z,0,0),StationKind.Books,9060,0));
            life.Stations.Add(new StationPlacement(Panel(o,x,z,1,0),((int)p.Use+1)%5==(int)ArchiveWingUse.Tea?StationKind.Tea:StationKind.Bookbinding,9061,1));
            life.Stations.Add(new StationPlacement(Panel(o,x,z,2,0),StationKind.StarChart,9062,2));
            o.Life=life;
            DressingAnchor Panel(RoomGeometry geometry,float px,float pz,int floor,float yaw)
            {
                float y=p.FloorY(floor);
                if(floor>0&&p.Nave){px=p.Cell*.5f-2;pz=p.Cell*.18f;}
                Box(geometry.Structure,px,pz,1.4f,.1f,y+.75f,y+1.9f,Surface.Accent);
                Box(geometry.Structure,px,pz,.1f,.1f,y,y+.75f,Surface.Metal);
                var a=new V3(px-.68f,y+.78f,pz-.052f);var b=new V3(px+.68f,y+.78f,pz-.052f);var cc=new V3(px-.68f,y+1.87f,pz-.052f);
                return new DressingAnchor(new V3(px-.36f,y+1.15f,pz-.052f),new V3(0,0,-1),a,cc,b,0,0);
            }
        }
    }
}
