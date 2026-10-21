using System;

namespace Poolrooms.Core
{
    public static partial class RestfulLevelArchitecture
    {
        // The envelope, support tables and all four through-routes are shared with
        // the tested gallery system. Each programme builds its own physical bays.
        static void JourneyBay(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            switch(p.Environment)
            {
                case LiminalEnvironment.AquariumGalleries:Aquarium(o,b,p);break;
                case LiminalEnvironment.BambooRetreat:Bamboo(o,b,p);break;
                case LiminalEnvironment.DesertCaravanserai:Caravanserai(o,b,p);break;
                case LiminalEnvironment.CrystalMuseum:Crystals(o,b,p);break;
                case LiminalEnvironment.CloudBaths:CloudBath(o,b,p);break;
                case LiminalEnvironment.SunkenLibrary:WaterLibrary(o,b,p);break;
                case LiminalEnvironment.RailwayWintergarden:Railway(o,b,p);break;
                case LiminalEnvironment.CeramicVillage:CeramicVillage(o,b,p);break;
                case LiminalEnvironment.LunarGreenhouse:LunarGarden(o,b,p);break;
                case LiminalEnvironment.AmberRestaurant:Restaurant(o,b,p);break;
                case LiminalEnvironment.MapmakersArchive:MapArchive(o,b,p);break;
                case LiminalEnvironment.SculptureCourts:Sculptures(o,b,p);break;
                case LiminalEnvironment.PaperLanternTown:LanternTown(o,b,p);break;
                case LiminalEnvironment.WatermillHalls:Watermill(o,b,p);break;
                case LiminalEnvironment.IndoorBoardwalk:Boardwalk(o,b,p);break;
                case LiminalEnvironment.VelvetTheatre:Theatre(o,b,p);break;
                case LiminalEnvironment.HangingOrchards:Orchard(o,b,p);break;
                case LiminalEnvironment.RainyBookshops:Bookshop(o,b,p);break;
                case LiminalEnvironment.MosaicCistern:Cistern(o,b,p);break;
                case LiminalEnvironment.AeronauticalGallery:AirGallery(o,b,p);break;
                default:throw new ArgumentOutOfRangeException(nameof(p.Environment));
            }
        }
        static void Frame(RoomGeometry o,PlanRect b,float height,float inset=.18f,Surface material=Surface.Accent)
        {
            foreach(int side in new[]{-1,1})
            {Box(o.Structure,b.X+side*(b.Width*.5f-inset),b.Z,.13f,b.Depth, height-.15f,height,material);
             foreach(int end in new[]{-1,1})Shapes.Column(o.Structure,b.X+side*(b.Width*.5f-inset),b.Z+end*(b.Depth*.5f-inset),.09f,0,height,12,material,1.2f);}
            foreach(int end in new[]{-1,1})Box(o.Structure,b.X,b.Z+end*(b.Depth*.5f-inset),b.Width,.13f,height-.15f,height,material);
        }
        static void SlattedBench(RoomGeometry o,float x,float z,float width,float depth=.6f)
        {
            int slats=Math.Max(3,(int)Math.Ceiling(depth/.12f));
            for(int i=0;i<slats;i++)ChamferedTop(o.Structure,x,z-depth*.5f+(i+.5f)*depth/slats,width,depth/slats-.012f,.41f,.47f,.012f,Surface.Accent);
            foreach(int side in new[]{-1,1})Box(o.Structure,x+side*(width*.5f-.15f),z,.12f,depth-.08f,0,.41f,Surface.Metal);
        }
        static void Aquarium(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float d=Math.Min(1.45f,b.Depth*.32f),z=b.MaxZ-d*.5f,h=2.4f+p.Layout*.2f;
            ChamferedTop(o.Structure,b.X,z,b.Width,d,0,.48f,.025f,Surface.Accent);
            Box(o.Structure,b.X,z+d*.5f-.06f,b.Width,.12f,.48f,h,Surface.Plaster);
            foreach(int side in new[]{-1,1})Box(o.Structure,b.X+side*(b.Width*.5f-.06f),z,.12f,d,.48f,h,Surface.Accent);
            Box(o.Structure,b.X,z,b.Width,d,h,h+.15f,Surface.Accent);
            // Separate front and inner liner, no coplanar glass over a solid front.
            Box(o.Glass,b.X,z-d*.5f+.035f,b.Width-.24f,.025f,.51f,h-.03f,Surface.Tile);
            for(int rock=0;rock<3+p.Layout;rock++)
            {float x=b.MinX+.35f+(rock+.5f)*(b.Width-.7f)/(3+p.Layout);Shapes.Column(o.Decoration,x,z,.12f+(rock%2)*.08f,.49f,.85f+(rock%3)*.16f,7,Surface.Slide,1.2f);}
            var water=new WaterRegion{CenterX=b.X,CenterZ=z,Width=b.Width-.25f,Depth=d-.18f,Bottom=.49f,Level=h-.12f};
            var fill=new MeshData();Shapes.Fill(fill,Shapes.RoundedRect(water.Width,water.Depth,0,3),water.Level,Surface.Tile,1.2f);fill.TranslateRotate(new V3(water.CenterX,0,water.CenterZ),0);o.Water.Append(fill);o.Regions.Add(water);
            SlattedBench(o,b.X,b.MinZ+.38f,b.Width*.72f);
        }
        static void Bamboo(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Frame(o,b,3.25f+p.Layout*.12f);
            int stems=5+p.Rhythm;float z=b.MaxZ-.3f;
            for(int i=0;i<stems;i++)
            {
                float x=b.MinX+.25f+(i+.5f)*(b.Width-.5f)/stems,h=2.6f+(i%3)*.22f;
                Shapes.Column(o.Structure,x,z,.045f,0,h,10,Surface.Accent,1.2f);
                for(float y=.35f;y<h;y+=.42f)Shapes.Column(o.Decoration,x,z,.052f,y,y+.025f,10,Surface.Slide,1.2f);
            }
            ChamferedTop(o.Structure,b.X,b.Z,b.Width*.64f,b.Depth*.5f,0,.035f,.012f,Surface.Slide);
            SlattedBench(o,b.X,b.MinZ+.45f,b.Width*.75f);
        }
        static void Caravanserai(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float height=3.2f+p.Layout*.18f;
            Frame(o,b,height,.24f,Surface.Plaster);
            int ribs=3+p.Layout;
            for(int i=0;i<ribs;i++)
            {var arch=new MeshData();Shapes.Portal(arch,b.Width-.5f,height,0,Math.Max(1.3f,b.Width-1.2f),height-.35f,.18f,16,1.2f);arch.TranslateRotate(new V3(b.X,0,b.MinZ+.3f+i*(b.Depth-.6f)/Math.Max(1,ribs-1)),0);o.Structure.Append(arch);}
            SlattedBench(o,b.X,b.MaxZ-.65f,b.Width*.58f,.75f);
        }
        static void Crystals(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            int count=2+p.Layout;
            for(int i=0;i<count;i++)
            {
                float x=b.MinX+(i+.5f)*b.Width/count,z=b.Z+(i%2==0?-.3f:.3f),size=Math.Min(.65f,b.Width/count*.65f);
                ChamferedTop(o.Structure,x,z,size,size,0,.65f,.028f,Surface.Plaster);
                Shapes.Column(o.Structure,x,z,size*.3f,.65f,1.5f+(i%3)*.27f,5+i%3,Surface.Metal,1.2f);
                float baseY=1.5f+(i%3)*.27f;
                for(int face=0;face<6;face++)
                {float a=face*Shapes.Pi/3,aa=(face+1)*Shapes.Pi/3;var va=new V3(x+size*.3f*Shapes.Cos(a),baseY,z+size*.3f*Shapes.Sin(a));var vb=new V3(x+size*.3f*Shapes.Cos(aa),baseY,z+size*.3f*Shapes.Sin(aa));var top=new V3(x,baseY+.4f,z);var n=V3.Cross(vb-va,top-va).Unit;o.Decoration.Triangle(va,vb,top,n,n,n,new V2(0,0),new V2(1,0),new V2(.5f,1),Surface.Slide);}
            }
            SlattedBench(o,b.X,b.MinZ+.38f,b.Width*.65f);
        }
        static void CloudBath(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            foreach(int sign in new[]{-1,1})
            {float x=b.X+sign*(b.Width*.5f-.24f);Shapes.Column(o.Structure,x,b.Z,.16f,-.72f,3.8f,20,Surface.Plaster,1.2f);}
            for(int i=0;i<3+p.Layout;i++)
            {float h=3.3f+i*.16f;Box(o.Decoration,b.X,b.Z,b.Width,.12f,h,h+.07f,Surface.Accent);}
            // Timber ledge remains over the basin, leaving a dry entry all round.
            Box(o.Structure,b.X,b.MinZ+.4f,b.Width-.4f,.5f,-.12f,.04f,Surface.Accent);
        }
        static void WaterLibrary(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float deck=Math.Min(1.4f,b.Width*.35f);
            Box(o.Structure,b.X,b.Z,deck,b.Depth,-.12f,.035f,Surface.Accent);
            foreach(int side in new[]{-1,1})
            {
                float x=b.X+side*(b.Width*.5f-.42f);
                Box(o.Structure,x,b.Z,.65f,b.Depth,-.72f,.08f,Surface.Plaster);
                // Four narrow bookcases flank, rather than obstruct, the bridge.
                for(int row=0;row<4;row++)Box(o.Structure,x,b.Z,.62f,b.Depth-.4f,.12f+row*.42f,.18f+row*.42f,Surface.Accent);
                for(int book=0;book<8+p.Layout;book++)Box(o.Decoration,x,b.MinZ+.3f+(book+.5f)*(b.Depth-.6f)/(8+p.Layout),.4f,.065f,.18f,.5f+(book%3)*.07f,Surface.Slide);
            }
        }
        static void Railway(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float gauge=Math.Min(1.435f,b.Width*.4f);
            for(int i=0;i<8+p.Layout;i++)Box(o.Structure,b.X,b.MinZ+(i+.5f)*b.Depth/(8+p.Layout),gauge+.6f,.15f,0,.065f,Surface.Accent);
            foreach(int side in new[]{-1,1})Box(o.Structure,b.X+side*gauge*.5f,b.Z,.065f,b.Depth,.065f,.13f,Surface.Metal);
            Frame(o,b,3.7f,.2f,Surface.Metal);
            for(int row=0;row<3+p.Layout;row++)Box(o.Decoration,b.X,b.MinZ+.2f+(row+.5f)*(b.Depth-.4f)/(3+p.Layout),b.Width,.05f,3.75f,3.82f,Surface.Metal);
            SlattedBench(o,b.MinX+.6f,b.MaxZ-.4f,Math.Min(1,b.Width*.2f));
        }
        static void CeramicVillage(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float w=b.Width*.72f,d=b.Depth*.72f,h=2.8f+p.Layout*.17f;
            var front=new MeshData();Shapes.Portal(front,w,h,0,Math.Min(1.6f,w*.65f),2.25f,.16f,16,1.2f);
            front.TranslateRotate(new V3(b.X,0,b.MaxZ-d),0);o.Structure.Append(front);
            foreach(int side in new[]{-1,1})Box(o.Structure,b.X+side*(w*.5f-.08f),b.MaxZ-d*.5f,.16f,d,0,h,Surface.Plaster);
            // Open rear courtyard: a real walk-in pavilion rather than a sealed box.
            for(int i=0;i<5;i++)Box(o.Structure,b.X,b.MaxZ-d*.5f,w+.2f,d+.2f-i*(d*.12f),h+i*.12f,h+(i+1)*.12f,Surface.Accent);
            ChamferedTop(o.Structure,b.X,b.MaxZ-.3f,w*.65f,.5f,0,.45f,.025f,Surface.Slide);
        }
        static void LunarGarden(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Frame(o,b,3.6f,.12f,Surface.Metal);
            int channels=2+p.Layout;
            for(int row=0;row<channels;row++)
            {
                float z=b.MinZ+.3f+(row+.5f)*(b.Depth-.6f)/channels;
                ChamferedTop(o.Structure,b.X,z,b.Width*.8f,.36f,.55f,.72f,.025f,Surface.Plaster);
                foreach(int side in new[]{-1,1})Box(o.Structure,b.X+side*b.Width*.3f,z,.07f,.22f,0,.55f,Surface.Metal);
                for(int pot=0;pot<4;pot++)Shapes.Column(o.Decoration,b.X+(pot-1.5f)*b.Width*.16f,z,.09f,.72f,.94f,10,Surface.Slide,1.2f);
            }
        }
        static void Restaurant(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            int tables=1+p.Layout%2;
            for(int i=0;i<tables;i++)
            {float x=b.X+(i-(tables-1)*.5f)*b.Width*.46f;Workbench(o,x,b.Z,Math.Min(1.5f,b.Width/tables*.72f),Math.Min(1.1f,b.Depth*.3f),.76f);
             foreach(int side in new[]{-1,1})SlattedBench(o,x,b.Z+side*Math.Min(1,b.Depth*.33f),Math.Min(1.3f,b.Width/tables*.65f),.43f);}
            for(int slat=0;slat<6+p.Rhythm;slat++)Box(o.Decoration,b.MinX+(slat+.5f)*b.Width/(6+p.Rhythm),b.MaxZ-.13f,.055f,.1f,1.2f,3.4f,Surface.Accent);
        }
        static void MapArchive(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Workbench(o,b.X,b.Z,b.Width*.76f,Math.Min(1.6f,b.Depth*.4f),.88f);
            for(int i=0;i<3+p.Layout;i++)
            {float x=b.MinX+.3f+(i+.5f)*(b.Width-.6f)/(3+p.Layout);Shapes.Column(o.Decoration,x,b.MaxZ-.35f,.11f,0,1.8f,16,Surface.Slide,1.2f);}
            for(int sheet=0;sheet<3;sheet++)Box(o.Decoration,b.X+(sheet-1)*b.Width*.18f,b.Z,b.Width*.16f,.65f,.881f+sheet*.004f,.884f+sheet*.004f,Surface.Plaster);
            Box(o.Structure,b.X,b.MaxZ-.2f,b.Width,.24f,0,1.1f,Surface.Accent);
            for(int row=0;row<5;row++)Box(o.Decoration,b.X,b.MaxZ-.33f,b.Width-.1f,.025f,.1f+row*.2f,.105f+row*.2f,Surface.Metal);
        }
        static void Sculptures(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float radius=Math.Min(b.Width,b.Depth)*.24f;
            Shapes.Column(o.Structure,b.X,b.Z,radius,0,.3f,24,Surface.Plaster,1.2f);
            for(int ring=0;ring<2+p.Layout;ring++)
            {
                float y=.6f+ring*.5f,r=radius*(.95f-ring*.12f);
                for(int i=0;i<24;i++)
                {float a=i*Shapes.Pi/12,aa=(i+1)*Shapes.Pi/12;Beam(o.Decoration,new V3(b.X+r*Shapes.Cos(a),y+r*.35f*Shapes.Sin(a),b.Z+r*Shapes.Sin(a)),new V3(b.X+r*Shapes.Cos(aa),y+r*.35f*Shapes.Sin(aa),b.Z+r*Shapes.Sin(aa)),.065f,Surface.Metal);}
            }
            Shapes.Column(o.Structure,b.X,b.Z,.065f,.3f,2.2f,12,Surface.Accent,1.2f);
        }
        static void LanternTown(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Frame(o,b,3.6f);
            for(int i=0;i<3+p.Layout;i++)
            {
                float x=b.MinX+(i+.5f)*b.Width/(3+p.Layout);
                Beam(o.Decoration,new V3(x,3.6f,b.Z),new V3(x,2.8f,b.Z),.007f);
                Shapes.Column(o.Decoration,x,b.Z,.21f,2.25f,2.8f,20,Surface.Slide,1.2f);
                foreach(float h in new[]{2.26f,2.79f})Shapes.Column(o.Decoration,x,b.Z,.23f,h,h+.025f,20,Surface.Accent,1.2f);
            }
            float w=b.Width*.8f;
            foreach(int side in new[]{-1,1})
            {Box(o.Structure,b.X+side*w*.5f,b.MaxZ-.2f,.14f,.28f,0,2.4f,Surface.Accent);
             Box(o.Structure,b.X+side*w*.3f,b.MaxZ-.2f,w*.35f,.12f,.2f,2.2f,Surface.Plaster);}
            SlattedBench(o,b.X,b.MinZ+.35f,w*.65f);
        }
        static void Watermill(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float r=Math.Min(1.3f,Math.Min(b.Width,b.Depth)*.25f),x=b.X,z=b.Z;
            float centerY=r-.1f;
            // Static display wheel with hub, two rims and actual paddles.
            for(int i=0;i<24;i++)
            {
                float a=i*Shapes.Pi/12,aa=(i+1)*Shapes.Pi/12;
                foreach(int side in new[]{-1,1})
                {float zz=z+side*.3f;Beam(o.Decoration,new V3(x+r*Shapes.Cos(a),centerY+r*Shapes.Sin(a),zz),new V3(x+r*Shapes.Cos(aa),centerY+r*Shapes.Sin(aa),zz),.055f,Surface.Accent);
                 if(i%3==0)Beam(o.Decoration,new V3(x,centerY,zz),new V3(x+r*Shapes.Cos(a),centerY+r*Shapes.Sin(a),zz),.035f,Surface.Accent);}
                if(i%2==0)Box(o.Structure,x+r*Shapes.Cos(a),z,.17f,.72f,centerY+r*Shapes.Sin(a)-.08f,centerY+r*Shapes.Sin(a)+.08f,Surface.Accent);
            }
            foreach(int side in new[]{-1,1})Box(o.Structure,x,z+side*.47f,.2f,.2f,-.72f,centerY+.08f,Surface.Plaster);
            Beam(o.Structure,new V3(x,centerY,z-.55f),new V3(x,centerY,z+.55f),.07f);
            Box(o.Structure,b.MinX+.55f,b.Z,.8f,b.Depth,-.12f,.035f,Surface.Accent);
        }
        static void Boardwalk(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float w=Math.Min(1.7f,b.Width*.42f);Box(o.Structure,b.X,b.Z,w,b.Depth,-.12f,.035f,Surface.Accent);
            for(int i=1;i<12+p.Layout;i++)Box(o.Decoration,b.X,b.MinZ+b.Depth*i/(12+p.Layout),w,.012f,.035f,.04f,Surface.Metal);
            foreach(int side in new[]{-1,1})for(int i=0;i<3;i++)
            {float x=b.X+side*(w*.5f-.09f),z=b.MinZ+.2f+i*(b.Depth-.4f)/2;Shapes.Column(o.Structure,x,z,.06f,-.72f,.95f,12,Surface.Plaster,1.2f);
             if(i<2)Beam(o.Structure,new V3(x,.85f,z),new V3(x,.85f,z+(b.Depth-.4f)/2),.025f);}
        }
        static void Theatre(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            int rows=2+p.Layout;float width=b.Width*.85f,depth=b.Depth*.8f;
            for(int i=0;i<rows;i++)
            {float z=b.MinZ+.3f+(i+.5f)*depth/rows;SlattedBench(o,b.X,z,width,.45f);}
            float z0=b.MaxZ-.16f;
            foreach(int side in new[]{-1,1})for(int fold=0;fold<5;fold++)
            {float x=b.X+side*(b.Width*.25f+fold*b.Width*.045f);Shapes.Column(o.Structure,x,z0,.065f,0,3.6f,12,Surface.Slide,1.2f);}
            Box(o.Structure,b.X,z0,b.Width,.18f,3.6f,3.85f,Surface.Accent);
        }
        static void Orchard(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Frame(o,b,3.7f,.15f,Surface.Accent);
            int baskets=3+p.Layout;
            for(int i=0;i<baskets;i++)
            {
                float x=b.MinX+.2f+(i+.5f)*(b.Width-.4f)/baskets,z=b.Z+(i%2==0?-.35f:.35f);
                Beam(o.Decoration,new V3(x,3.7f,z),new V3(x,2.65f,z),.012f);
                Shapes.Column(o.Decoration,x,z,.24f,2.4f,2.65f,16,Surface.Accent,1.2f);
                for(int leaf=0;leaf<6;leaf++){float a=leaf*Shapes.Pi/3;Beam(o.Decoration,new V3(x,2.65f,z),new V3(x+.28f*Shapes.Cos(a),2.55f,z+.28f*Shapes.Sin(a)),.026f,Surface.Slide);}
            }
            ChamferedTop(o.Structure,b.X,b.MaxZ-.4f,b.Width*.8f,.6f,0,.52f,.035f,Surface.Plaster);
            SlattedBench(o,b.X,b.MinZ+.4f,b.Width*.75f);
        }
        static void Bookshop(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Library(o,b,p);
            for(int r=0;r<5+p.Layout;r++)Box(o.Decoration,b.X,b.Z-.16f+r*.08f,b.Width*.82f,.045f,3.2f,3.24f,Surface.Accent);
            foreach(int side in new[]{-1,1})Box(o.Glass,b.X+side*(b.Width*.5f-.05f),b.Z,.02f,b.Depth*.75f,1.15f,2.8f,Surface.Tile);
        }
        static void Cistern(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            foreach(int side in new[]{-1,1})
            {float x=b.X+side*(b.Width*.5f-.3f);Shapes.Column(o.Structure,x,b.Z,.19f,-.72f,3.5f,24,Surface.Plaster,1.2f);Shapes.Column(o.Decoration,x,b.Z,.3f,3.35f,3.52f,24,Surface.Accent,1.2f);}
            var arch=new MeshData();Shapes.Portal(arch,b.Width-.2f,4.1f,0,Math.Max(1.3f,b.Width-1.1f),3.8f,.2f,24,1.2f);arch.TranslateRotate(new V3(b.X,0,b.Z),0);o.Structure.Append(arch);
            Box(o.Structure,b.X,b.MinZ+.35f,b.Width-.4f,.5f,-.15f,.035f,Surface.Slide);
        }
        static void AirGallery(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float z=b.Z,h=2.8f+p.Layout*.17f,length=b.Depth*.65f,span=b.Width*.74f;
            Frame(o,b,4.4f,.15f,Surface.Metal);
            foreach(int side in new[]{-1,1})Beam(o.Decoration,new V3(b.X+side*span*.35f,4.4f,z),new V3(b.X+side*span*.35f,h,z),.007f);
            // Original suspended aerodynamic study, not counted as a downloaded aircraft.
            Beam(o.Decoration,new V3(b.X,h,z-length*.5f),new V3(b.X,h,z+length*.5f),.11f,Surface.Plaster);
            ChamferedTop(o.Decoration,b.X,z,span,.55f,h-.035f,h+.035f,.02f,Surface.Accent);
            ChamferedTop(o.Decoration,b.X,z-length*.36f,span*.32f,.35f,h-.03f,h+.045f,.015f,Surface.Plaster);
            Box(o.Decoration,b.X,z-length*.36f,.035f,.38f,h,h+.4f,Surface.Accent);
            Workbench(o,b.X,b.MinZ+.6f,b.Width*.7f,.7f,.85f);
        }
        static void ExpandedJoinery(RoomGeometry o,Room r,LayoutConfig c)
        {
            var p=r.Plan.Restful;float half=r.Width*.5f;
            int treatment=(int)LiminalRegions.Hash(c.Seed,ContinuousTopology.FloorDivide(r.X,4),ContinuousTopology.FloorDivide(r.Z,4),0x510050u)%4;
            // Overhead construction expands existing programmes without narrowing
            // routes, spawning per-slat GameObjects or adding shadow-casting lights.
            foreach(var b in p.Bays)
            {
                int count=3+p.Rhythm;float ceiling=r.Height-.5f;
                for(int i=0;i<count;i++)
                {
                    float x=b.MinX+(i+.5f)*b.Width/count;
                    if(treatment==0)Box(o.Decoration,x,b.Z,.07f,b.Depth,ceiling-.16f,ceiling,Surface.Accent);
                    else if(treatment==1)Box(o.Decoration,b.X,b.MinZ+(i+.5f)*b.Depth/count,b.Width,.06f,ceiling-.1f,ceiling,Surface.Plaster);
                    else if(treatment==2)Beam(o.Decoration,new V3(x,ceiling,b.MinZ),new V3(Math.Min(b.MaxX,x+.4f),ceiling-.24f,b.MaxZ),.035f);
                    else if(i%2==0)Shapes.Column(o.Decoration,x,b.Z,Math.Min(.35f,b.Width/count*.35f),ceiling-.09f,ceiling,24,Surface.Slide,1.2f);
                }
            }
            // Alternating wall pilasters and panel recess borders, only where the
            // envelope is closed. All members remain beyond the perimeter route.
            for(int side=0;side<4;side++)
            {
                if(LargeRegions.OpenEdge(c,r.X,r.Z,side,r.Environment))continue;
                var trim=new MeshData();int panels=2+p.Layout;
                foreach(int sign in new[]{-1,1})for(int j=0;j<panels;j++)
                {float x=sign*(p.Clear+.4f+(j+.5f)*(half-p.Clear-.8f)/panels);Box(trim,x,-half+.17f,.075f,.08f,.22f,Math.Min(3.5f,r.Height-1),Surface.Accent);}
                trim.TranslateRotate(new V3(),side*Shapes.Pi*.5f);o.Decoration.Append(trim);
            }
        }
    }
}
