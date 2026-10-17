using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public readonly struct PlayFramePlan
    {
        public const float Module=4.2f,Storey=3.2f;
        public readonly float X,Z;
        public readonly int Columns,Rows,Levels;
        public readonly uint Seed;
        public float Width { get { return Columns*Module; } }
        public float Depth { get { return Rows*Module; } }
        public float StairX { get { return X-Width*.5f+Module*.5f; } }
        public float StairZ { get { return Z-Depth*.5f+Module; } }
        public PlanRect Bounds { get { return new PlanRect(X-Width*.5f,Z-Depth*.5f,X+Width*.5f,Z+Depth*.5f); } }
        public PlanRect Shaft { get { return new PlanRect(X-Width*.5f,StairZ-3.6f,X-Width*.5f+Module,StairZ+3.6f); } }
        public PlayFramePlan(float x,float z,int columns,int rows,int levels,uint seed)
        {X=x;Z=z;Columns=columns;Rows=rows;Levels=levels;Seed=seed;}
        public V3 Port(bool south,int level)
        {return new V3(X+Width*.5f-Module*.5f,level*Storey,Z+(south?-1:1)*Depth*.5f);}
    }
    public readonly struct PlayPropPlan
    {
        public readonly V3 Position;
        public readonly int Kind,Colour;
        public PlayPropPlan(V3 position,int kind,int colour){Position=position;Kind=kind;Colour=colour;}
    }
    public readonly struct PlaySlidePath
    {
        public readonly V3 Start,End;
        public readonly float Width;
        public PlaySlidePath(V3 start,V3 end,float width){Start=start;End=end;Width=width;}
        public V3 Point(float t)
        {t=Math.Max(0,Math.Min(1,t));float ease=t*t*(3-2*t);return new V3(Start.X+(End.X-Start.X)*t,Start.Y+(End.Y-Start.Y)*ease,Start.Z+(End.Z-Start.Z)*t);}
        // A supported player can leave by jumping or stepping off; never teleports or captures input.
        public bool Glide(V3 feet,float radius,out V3 direction)
        {
            direction=new V3();V3 flat=new V3(End.X-Start.X,0,End.Z-Start.Z);float length=flat.Length;
            if(length<1)return false;
            float t=V3.Dot(feet-Start,flat)/(length*length);
            if(t<.025f||t>.965f)return false;
            V3 point=Point(t);float lateral=new V3(feet.X-point.X,0,feet.Z-point.Z).Length;
            if(lateral>Width*.5f-radius-.05f||Math.Abs(feet.Y-point.Y)>.23f)return false;
            direction=(Point(Math.Min(1,t+.01f))-Point(Math.Max(0,t-.01f))).Unit;return true;
        }
    }
    public sealed class SoftPlayPlan
    {
        public readonly PlayFramePlan[] Frames;
        public readonly PlaySlidePath Slide;
        public readonly V3 BridgeStart,BridgeEnd;
        public readonly bool EnclosedBridge;
        public readonly PlayPropPlan[] Props;
        public readonly PlanRect BallPit;
        public readonly int RoofStyle,Lighting;
        public readonly float Clear;
        public SoftPlayPlan(PlayFramePlan[] frames,PlaySlidePath slide,V3 a,V3 b,bool tube,PlayPropPlan[] props,PlanRect pit,int roof,int lighting,float clear)
        {Frames=frames;Slide=slide;BridgeStart=a;BridgeEnd=b;EnclosedBridge=tube;Props=props;BallPit=pit;RoofStyle=roof;Lighting=lighting;Clear=clear;}
        public bool Valid(Room room,LayoutConfig c)
        {
            if(room.Environment!=LiminalEnvironment.SoftPlay||Frames==null||Frames.Length<2||Frames.Length>3||Props==null||Props.Length>8||room.Width!=c.CellSize||room.Depth!=c.CellSize)return false;
            for(int i=0;i<Frames.Length;i++)
            {
                var f=Frames[i];
                if(f.Columns<2||f.Columns>3||f.Rows<2||f.Rows>3||f.Levels<2||f.Levels>4||
                    Math.Abs(f.X)+f.Width*.5f>room.Width*.5f-.7f||Math.Abs(f.Z)+f.Depth*.5f>room.Depth*.5f-.7f||
                    Math.Abs(f.X)-f.Width*.5f<Clear||Math.Abs(f.Z)-f.Depth*.5f<Clear||room.Height<(f.Levels-1)*PlayFramePlan.Storey+2.8f)return false;
                for(int j=0;j<i;j++)if(f.Bounds.Overlaps(Frames[j].Bounds,.5f))return false;
            }
            return Frames[0].Levels>=3&&Frames[1].Levels>=3&&BridgeStart.Y==6.4f&&BridgeEnd.Y==6.4f&&Slide.Start.Y==3.2f&&Slide.End.Y==0&&
                Math.Abs(Slide.End.X)+Slide.Width*.5f<room.Width*.5f-.5f&&Math.Abs(Slide.End.Z)<room.Depth*.5f-.5f;
        }
    }
    public static class SoftPlayPlanner
    {
        public static void Describe(Room room,LayoutConfig c)
        {
            room.ZoneId=-1;room.Environment=LiminalEnvironment.SoftPlay;room.Width=room.Depth=c.CellSize;
            uint seed=LiminalRegions.Hash(c.Seed,room.X,room.Z,0x39001u);room.DetailSeed=seed;
            var random=new StableRandom(seed);float clear=Math.Max(3,c.DoorWidth*.5f+.8f),gap=clear+1.1f;
            int max=(int)((c.CellSize*.5f-gap-.8f)/PlayFramePlan.Module);max=Math.Max(2,Math.Min(3,max));
            int nx=2+random.Range(max-1),nz=2+random.Range(max-1),levels=3+random.Range(2);
            // Small continuous layout variations stay outside the four ground routes and keep the slide above them.
            float extra=Math.Max(0,Math.Min(.9f,Math.Min(5.1f-gap,c.CellSize*.5f-Math.Max(nx,nz)*4.2f-.8f-gap)));
            gap+=random.Range(0,extra);
            var frames=new List<PlayFramePlan>();
            frames.Add(new PlayFramePlan(gap+nx*2.1f,gap+nz*2.1f,nx,nz,levels,random.Next()));
            frames.Add(new PlayFramePlan(-gap-4.2f,-gap-nz*2.1f,2,nz,3+random.Range(2),random.Next()));
            if(c.CellSize>=44&&random.Range(2)==0)frames.Add(new PlayFramePlan(gap+4.2f,-gap-4.2f,2,2,2+random.Range(2),random.Next()));
            int highest=0;foreach(var f in frames)highest=Math.Max(highest,f.Levels);
            room.Height=Math.Max(c.DoorHeight+1,(highest-1)*3.2f+3.7f);
            V3 slideStart=frames[1].Port(false,1);slideStart.Z-=.15f;
            float endZ=Math.Min(c.CellSize*.5f-1.1f,slideStart.Z+15);
            var slide=new PlaySlidePath(slideStart,new V3(slideStart.X,0,endZ),1.9f);
            V3 a=frames[0].Port(true,2),b=frames[1].Port(false,2);
            var props=new List<PlayPropPlan>();
            // NW play lounge: sparse actual pickups, many batched visual balls, no rigidbody crowd.
            float px=-c.CellSize*.5f+4.5f,pz=c.CellSize*.5f-4.5f;
            var pit=new PlanRect(px-2.5f,pz-2.5f,px+2.5f,pz+2.5f);
            for(int i=0;i<8;i++)props.Add(new PlayPropPlan(new V3(px+(i%4-1.5f)*.7f,i%3==0?.22f:i%3==1?.19f:.23f,pz+(i/4-.5f)*1.1f),i%3,random.Range(3)));
            var plan=new SoftPlayPlan(frames.ToArray(),slide,a,b,random.Range(3)!=0,props.ToArray(),pit,random.Range(3),random.Range(3),clear);
            room.Plan=new ProceduralPlan(room.Environment,new PlannedModule[0],new WaterRegion[0],clear,0,0,softPlay:plan);
            if(!plan.Valid(room,c))throw new ArgumentException("Soft-play hall cannot fit its protected circulation.");
        }
        public static bool Adjacent(LayoutConfig c,int x,int z,int side)
        {
            long nx=(long)x+Directions.DX[side],nz=(long)z+Directions.DZ[side];
            if(nx<int.MinValue||nx>int.MaxValue||nz<int.MinValue||nz>int.MaxValue)return false;
            if(!c.Continuous&&(nx<0||nz<0||nx>=c.Columns||nz>=c.Rows))return false;
            ConnectedMallSite mall;ConnectedTerrainSite terrain;
            return !ConnectedMallTopology.At(c,x,z,out mall)&&!ConnectedMallTopology.At(c,(int)nx,(int)nz,out mall)&&!ConnectedMallTopology.Gateway(c,x,z,out mall)&&!ConnectedMallTopology.Gateway(c,(int)nx,(int)nz,out mall)&&
                !ConnectedTerrainTopology.Reserved(c,x,z)&&!ConnectedTerrainTopology.Reserved(c,(int)nx,(int)nz)&&
                !ConnectedTerrainTopology.Gateway(c,x,z,out terrain)&&!ConnectedTerrainTopology.Gateway(c,(int)nx,(int)nz,out terrain)&&
                LiminalRegions.At(c,x,z)==LiminalEnvironment.SoftPlay&&LiminalRegions.At(c,(int)nx,(int)nz)==LiminalEnvironment.SoftPlay;
        }
        public static float DoorWidth(Room room,int side,LayoutConfig c)
        {return Adjacent(c,room.X,room.Z,side)?Math.Min(14,c.CellSize*.42f):c.DoorWidth;}
    }
}
