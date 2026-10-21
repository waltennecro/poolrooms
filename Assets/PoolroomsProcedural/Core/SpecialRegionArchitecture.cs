using System;

namespace Poolrooms.Core
{
    public static partial class RestfulLevelArchitecture
    {
        // Side bays leave the full central cross and every perimeter portal clear.
        // Detail is batched into the room meshes, never an object or light per slat.
        static void SpecialBay(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            switch(p.Environment)
            {
                case LiminalEnvironment.SolarObservatory:Observatory(o,b,p);break;
                case LiminalEnvironment.CoveredMarket:Market(o,b,p);break;
                case LiminalEnvironment.TextileAtelier:Atelier(o,b,p);break;
                case LiminalEnvironment.PostalArcade:Postal(o,b,p);break;
                case LiminalEnvironment.IndoorMarina:Marina(o,b,p);break;
                case LiminalEnvironment.ClockGallery:Clocks(o,b,p);break;
                case LiminalEnvironment.PaperWorkshop:Paper(o,b,p);break;
                case LiminalEnvironment.ChessPavilion:Chess(o,b,p);break;
                case LiminalEnvironment.RadioSalon:Radio(o,b,p);break;
                case LiminalEnvironment.MirrorArcade:Mirrors(o,b,p);break;
                default:throw new ArgumentOutOfRangeException(nameof(p.Environment));
            }
        }
        static void Beam(MeshData mesh,V3 a,V3 b,float radius,Surface s=Surface.Metal)
        {ZoneShapes.Beam(mesh,a,b,radius,s,1.2f,12);}
        static void Workbench(RoomGeometry o,float x,float z,float w,float d,float height)
        {
            ChamferedTop(o.Structure,x,z,w,d,height-.085f,height,.018f,Surface.Accent);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box(o.Structure,x+sx*(w*.5f-.1f),z+sz*(d*.5f-.1f),.08f,.08f,0,height-.085f,Surface.Metal);
        }
        static void Observatory(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float x=b.X,z=b.Z,h=1.25f+p.Layout*.07f;
            foreach(int foot in new[]{0,1,2})
            {float a=foot*Shapes.Pi*2/3;Beam(o.Structure,new V3(x+Shapes.Cos(a)*.6f,0,z+Shapes.Sin(a)*.6f),new V3(x,h,z),.04f);}
            V3 a0=new V3(x-.45f,h-.1f,z),a1=new V3(x+.75f,h+.55f,z);
            Beam(o.Structure,a0,a1,.14f,Surface.Accent);Beam(o.Decoration,a0-(a1-a0).Unit*.15f,a0,.065f);
            // Separate glazing at the objective, perpendicular to the barrel axis.
            var n=(a1-a0).Unit;var u=new V3(0,0,.132f);var v=V3.Cross(n,u).Unit*.132f;
            for(int i=0;i<24;i++)
            {float a=i*Shapes.Pi*2/24,bb=(i+1)*Shapes.Pi*2/24;var c=a1+n*.008f;var e=c+u*Shapes.Cos(a)+v*Shapes.Sin(a);var f=c+u*Shapes.Cos(bb)+v*Shapes.Sin(bb);o.Glass.Triangle(c,e,f,n,n,n,new V2(.5f,.5f),new V2(.5f+.5f*Shapes.Cos(a),.5f+.5f*Shapes.Sin(a)),new V2(.5f+.5f*Shapes.Cos(bb),.5f+.5f*Shapes.Sin(bb)),Surface.Tile);}
            for(int i=0;i<9+p.Layout;i++)
            {float a=i*Shapes.Pi*2/(9+p.Layout),radius=Math.Min(b.Width,b.Depth)*.42f;Box(o.Decoration,x+Shapes.Cos(a)*radius,z+Shapes.Sin(a)*radius,.1f,.1f,.004f,.016f,Surface.Metal);}
            Workbench(o,b.X,b.MaxZ-.36f,Math.Min(2.2f,b.Width-.2f),.6f,.85f);
        }
        static void Market(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float w=b.Width*.84f,d=Math.Min(1.1f,b.Depth*.35f),z=b.MaxZ-.8f;
            Workbench(o,b.X,z,w,d,.88f);
            foreach(int s in new[]{-1,1})Shapes.Column(o.Structure,b.X+s*(w*.5f-.05f),z,.06f,0,2.65f,12,Surface.Accent,1.2f);
            for(int i=0;i<9+p.Layout;i++)Box(o.Decoration,b.X-w*.5f+(i+.5f)*w/(9+p.Layout),z,w/(9+p.Layout)*.98f,1.5f,2.62f,2.69f,i%2==0?Surface.Slide:Surface.Plaster);
            for(int i=0;i<3+p.Layout;i++)
            {
                float x=b.X-w*.4f+w*.8f*i/(2+p.Layout);
                Box(o.Structure,x,z,.44f,.48f,.88f,.91f,Surface.Accent);
                foreach(int side in new[]{-1,1}){Box(o.Decoration,x+side*.22f,z,.025f,.48f,.91f,1.08f,Surface.Accent);Box(o.Decoration,x,z+side*.24f,.44f,.025f,.91f,1.08f,Surface.Accent);}
                for(int fruit=0;fruit<3;fruit++)Shapes.Column(o.Decoration,x+(fruit-1)*.11f,z,.045f,.91f,1.005f,8,Surface.Slide,1.2f);
            }
        }
        static void Atelier(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float w=b.Width*.72f,z=b.MaxZ-.5f;
            foreach(int s in new[]{-1,1})Box(o.Structure,b.X+s*w*.5f,z,.1f,.16f,0,2.35f,Surface.Accent);
            foreach(float h in new[]{.32f,2.3f})Box(o.Structure,b.X,z,w,.15f,h,h+.1f,Surface.Accent);
            int threads=16+p.Layout*4;
            for(int i=0;i<threads;i++)Beam(o.Decoration,new V3(b.X-w*.46f+w*.92f*i/(threads-1),.4f,z),new V3(b.X-w*.46f+w*.92f*i/(threads-1),2.3f,z),.006f,Surface.Slide);
            for(int row=0;row<12;row++)Box(o.Decoration,b.X,z-.012f,w*.9f,.018f,.42f+row*.065f,.46f+row*.065f,Surface.Slide);
            Workbench(o,b.X,b.MinZ+.5f,Math.Min(2.2f,b.Width-.2f),.7f,.8f);
            for(int spool=0;spool<3+p.Layout;spool++)
            {float x=b.X-.6f+spool*.24f;Shapes.Column(o.Decoration,x,b.MinZ+.5f,.075f,.8f,1.05f,12,Surface.Slide,1.2f);Shapes.Column(o.Decoration,x,b.MinZ+.5f,.095f,1.05f,1.08f,12,Surface.Accent,1.2f);}
        }
        static void Postal(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float w=b.Width*.86f,z=b.MaxZ-.25f;int cols=3+p.Layout;
            Box(o.Structure,b.X,z,w,.36f,0,2.1f,Surface.Accent);
            for(int x=0;x<cols;x++)for(int y=0;y<5;y++)
            {float px=b.X-w*.5f+(x+.5f)*w/cols,h=.2f+y*.36f;ChamferedTop(o.Decoration,px,z-.2f,w/cols-.065f,.08f,h,h+.3f,.012f,Surface.Plaster);Box(o.Decoration,px,z-.248f,.16f,.012f,h+.12f,h+.14f,Surface.Metal);Box(o.Decoration,px,z-.249f,.11f,.015f,h+.21f,h+.25f,Surface.Slide);}
            Workbench(o,b.X,b.MinZ+.5f,b.Width*.72f,.65f,.86f);
            for(int i=0;i<4;i++)Box(o.Decoration,b.X-.6f+i*.3f,b.MinZ+.5f,.24f,.17f,.86f,.868f+i*.002f,Surface.Slide);
        }
        static void Marina(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            // Walkable timber jetty supported above a real floor cut-out and water.
            float w=Math.Min(1.65f,b.Width*.4f);
            Box(o.Structure,b.X,b.Z,w,b.Depth,-.1f,.045f,Surface.Accent);
            int boards=Math.Max(4,(int)Math.Ceiling(b.Depth/.28f));
            for(int i=1;i<boards;i++)Box(o.Decoration,b.X,b.MinZ+b.Depth*i/boards,w,.014f,.045f,.049f,Surface.Metal);
            foreach(int s in new[]{-1,1})for(int i=0;i<3;i++)
            {
                float x=b.X+s*(w*.5f-.09f),z=b.MinZ+.3f+i*(b.Depth-.6f)/2;
                Shapes.Column(o.Structure,x,z,.08f,-.72f,.76f,12,Surface.Accent,1.2f);
                if(i<2)Beam(o.Structure,new V3(x,.68f,z),new V3(x,.68f,z+(b.Depth-.6f)/2),.018f,Surface.Metal);
            }
            // Small sealed display dinghies occupy water beside the jetty, not its approach.
            float boatW=Math.Max(.18f,(b.Width-.6f-w)*.5f-.22f),boatZ=b.Z;
            foreach(int side in new[]{-1,1})
            {
                float x=b.X+side*(w*.5f+boatW*.5f+.1f),d=Math.Min(2.2f,b.Depth*.55f);
                ChamferedTop(o.Structure,x,boatZ,boatW,d,-.25f,-.07f,Math.Min(.06f,boatW*.15f),Surface.Accent);
                foreach(int edge in new[]{-1,1})Box(o.Structure,x+edge*(boatW*.5f-.025f),boatZ,.05f,d,-.07f,.24f,Surface.Accent);
                foreach(int end in new[]{-1,1})Box(o.Structure,x,boatZ+end*(d*.5f-.03f),boatW,.06f,-.07f,.24f,Surface.Accent);
                foreach(int seat in new[]{-1,1})Box(o.Structure,x,boatZ+seat*d*.25f,boatW-.1f,.22f,.04f,.08f,Surface.Slide);
                Beam(o.Decoration,new V3(x-boatW*.35f,.25f,boatZ-d*.3f),new V3(x+boatW*.35f,.25f,boatZ+d*.3f),.02f,Surface.Metal);
            }
        }
        // Upright inset clock/speaker faces with a physical rim and explicit normals.
        static void Disc(RoomGeometry o,float x,float y,float z,float radius,Surface face)
        {
            for(int i=0;i<32;i++)
            {
                float a=i*Shapes.Pi*2/32,b=(i+1)*Shapes.Pi*2/32;var n=new V3(0,0,-1);
                V3 p=new V3(x+radius*Shapes.Cos(a),y+radius*Shapes.Sin(a),z),q=new V3(x+radius*Shapes.Cos(b),y+radius*Shapes.Sin(b),z);
                o.Decoration.Triangle(new V3(x,y,z),q,p,n,n,n,new V2(.5f,.5f),new V2(.5f+.5f*Shapes.Cos(b),.5f+.5f*Shapes.Sin(b)),new V2(.5f+.5f*Shapes.Cos(a),.5f+.5f*Shapes.Sin(a)),face);
                var po=new V3(x+(radius+.035f)*Shapes.Cos(a),y+(radius+.035f)*Shapes.Sin(a),z-.012f);var qo=new V3(x+(radius+.035f)*Shapes.Cos(b),y+(radius+.035f)*Shapes.Sin(b),z-.012f);
                o.Decoration.Quad(p,q,qo,po,n,Surface.Metal,1.2f);
            }
        }
        static void Clocks(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            int count=2+p.Layout;float w=Math.Min(.85f,b.Width/count*.85f);
            for(int i=0;i<count;i++)
            {
                float x=b.MinX+(i+.5f)*b.Width/count,z=b.MaxZ-.36f;
                Box(o.Structure,x,z,w,.42f,0,2.4f,Surface.Accent);Disc(o,x,1.95f,z-.221f,w*.38f,Surface.Slide);
                for(int tick=0;tick<12;tick++){float a=tick*Shapes.Pi/6;Box(o.Decoration,x+Shapes.Cos(a)*w*.31f,z-.24f,.023f,.016f,1.95f+Shapes.Sin(a)*w*.31f-.015f,1.95f+Shapes.Sin(a)*w*.31f+.015f,Surface.Metal);}
                float angle=(i+p.Rhythm)*Shapes.Pi/6;
                Beam(o.Decoration,new V3(x,1.95f,z-.25f),new V3(x+Shapes.Sin(angle)*w*.25f,1.95f+Shapes.Cos(angle)*w*.25f,z-.25f),.012f);
                Beam(o.Decoration,new V3(x,1.95f,z-.251f),new V3(x-w*.1f,2.02f,z-.251f),.016f);
                Box(o.Decoration,x,z-.22f,.025f,.025f,.4f,1.5f,Surface.Metal);Disc(o,x,.43f,z-.24f,w*.14f,Surface.Metal);
            }
        }
        static void Paper(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Workbench(o,b.X,b.MinZ+.6f,b.Width*.85f,.85f,.83f);
            for(int i=0;i<5+p.Layout;i++)Box(o.Decoration,b.X-.7f+i*.2f,b.MinZ+.58f,.16f,.55f,.831f,.842f+i*.002f,Surface.Slide);
            foreach(int s in new[]{-1,1})Box(o.Structure,b.X+s*b.Width*.42f,b.MaxZ-.3f,.09f,.12f,0,2.5f,Surface.Accent);
            for(int row=0;row<3;row++)
            {
                float h=1.1f+row*.5f;Beam(o.Decoration,new V3(b.X-b.Width*.42f,h,b.MaxZ-.3f),new V3(b.X+b.Width*.42f,h,b.MaxZ-.3f),.006f,Surface.Metal);
                for(int sheet=0;sheet<3+p.Layout;sheet++){float x=b.MinX+b.Width*(sheet+1)/(4+p.Layout);Box(o.Decoration,x,b.MaxZ-.3f,.26f,.008f,h-.35f,h-.02f,Surface.Slide);Box(o.Decoration,x,b.MaxZ-.32f,.035f,.03f,h-.06f,h+.02f,Surface.Accent);}
            }
        }
        static void Chess(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float size=Math.Min(2.1f,Math.Min(b.Width,b.Depth)*.65f);Workbench(o,b.X,b.Z,size+.14f,size+.14f,.76f);
            for(int x=0;x<8;x++)for(int z=0;z<8;z++)
            {float px=b.X+(x-3.5f)*size/8,pz=b.Z+(z-3.5f)*size/8;Box(o.Decoration,px,pz,size/8*.99f,size/8*.99f,.761f,.775f,(x+z)%2==0?Surface.Slide:Surface.Accent);if(z<2||z>5){float radius=size/8*.27f,hi=.89f+(z==0||z==7?.1f:0);Shapes.Column(o.Decoration,px,pz,radius,.775f,.8f,12,z<2?Surface.Plaster:Surface.Metal,1.2f);Shapes.Column(o.Decoration,px,pz,radius*.55f,.8f,hi,12,z<2?Surface.Plaster:Surface.Metal,1.2f);Shapes.Column(o.Decoration,px,pz,radius*.8f,hi,hi+.03f,12,z<2?Surface.Plaster:Surface.Metal,1.2f);}}
            foreach(int s in new[]{-1,1})
            {float z=b.Z+s*(size*.5f+.65f);Workbench(o,b.X,z,size*.8f,.38f,.44f);Box(o.Structure,b.X,z+s*.16f,size*.8f,.065f,.44f,.86f,Surface.Accent);}
        }
        static void Radio(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Workbench(o,b.X,b.MaxZ-.65f,b.Width*.85f,.85f,.8f);
            float w=Math.Min(2,b.Width*.7f),z=b.MaxZ-.65f;
            Box(o.Structure,b.X,z,w,.48f,.8f,1.42f,Surface.Accent);Disc(o,b.X-w*.31f,1.12f,z-.249f,.19f,Surface.Slide);
            Box(o.Decoration,b.X+w*.17f,z-.25f,w*.44f,.025f,1.14f,1.32f,Surface.Plaster);
            for(int mark=0;mark<12;mark++)Box(o.Decoration,b.X+w*.01f+mark*w*.027f,z-.27f,.008f,.008f,1.19f,1.25f+(mark%3)*.015f,Surface.Metal);
            foreach(int knob in new[]{-1,1})Disc(o,b.X+w*.18f+knob*w*.13f,.99f,z-.27f,.045f,Surface.Metal);
            for(int slat=0;slat<7+p.Layout;slat++)Box(o.Decoration,b.MinX+.15f+slat*(b.Width-.3f)/(6+p.Layout),b.MinZ+.12f,.065f,.1f,.5f,2.5f,Surface.Slide);
        }
        static void Mirrors(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            int panels=2+p.Layout;float z=b.MaxZ-.2f,w=(b.Width-.3f)/panels;
            var reflection=new MeshData{Theme=SurfaceTheme.SpecialMirror};
            for(int i=0;i<panels;i++)
            {
                float x=b.MinX+.15f+(i+.5f)*w;
                Box(o.Structure,x,z,w-.04f,.15f,0,2.65f,Surface.Accent);
                Box(reflection,x,z-.083f,w-.13f,.012f,.25f,2.51f,Surface.Metal);
                foreach(int s in new[]{-1,1})Box(o.Decoration,x+s*(w*.5f-.045f),z-.1f,.035f,.035f,.18f,2.58f,Surface.Metal);
                foreach(float h in new[]{.18f,2.55f})Box(o.Decoration,x,z-.1f,w-.055f,.035f,h,h+.035f,Surface.Metal);
            }
            o.VisualMeshes.Add(reflection);
            Workbench(o,b.X,b.MinZ+.4f,b.Width*.8f,.48f,.44f);
        }
    }
}
