using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    internal static class VariedPlanner
    {
        private static readonly LiminalModule[] poolChoices={LiminalModule.Pool,LiminalModule.Columns,LiminalModule.Arch,LiminalModule.Screen,LiminalModule.Bench,LiminalModule.Platform};
        private static readonly LiminalModule[] playChoices={LiminalModule.PlayFrame,LiminalModule.Tunnel,LiminalModule.BallPit,LiminalModule.Slide,LiminalModule.Platform,LiminalModule.Bench};
        internal static ProceduralPlan Plan(Room room,LayoutConfig c,int attempt)
        {
            var random=new StableRandom(LiminalRegions.Hash(c.Seed,room.X,room.Z,0xC10u+(uint)attempt));
            PlanCirculation circulation=PlanCirculation.Create(room,c,ref random,attempt);
            var bounds=new PlanRect(-room.Width*.5f+1.25f,-room.Depth*.5f+1.25f,room.Width*.5f-1.25f,room.Depth*.5f-1.25f);
            var blocked=new List<PlanRect>();foreach(PlanRect path in circulation.Reserved)blocked.Add(path.Expanded(.25f));
            foreach(PlanRect cut in circulation.Cuts)blocked.Add(cut.Expanded(.35f));
            var free=Bays(bounds,blocked);
            // Subdivide only oversized bays. No quarter-grid and no fixed sequence of room types.
            for(int i=0;i<free.Count&&free.Count<40;i++)
            {
                PlanRect b=free[i];bool horizontal=b.Width>b.Depth;float longest=horizontal?b.Width:b.Depth;
                if(longest<13||Math.Min(b.Width,b.Depth)<4.4f||random.Value()<.35f)continue;
                float fraction=random.Range(.4f,.6f);
                if(horizontal){float split=b.MinX+b.Width*fraction;free[i]=new PlanRect(b.MinX,b.MinZ,split,b.MaxZ);free.Add(new PlanRect(split,b.MinZ,b.MaxX,b.MaxZ));}
                else{float split=b.MinZ+b.Depth*fraction;free[i]=new PlanRect(b.MinX,b.MinZ,b.MaxX,split);free.Add(new PlanRect(b.MinX,split,b.MaxX,b.MaxZ));}
            }
            free.Sort((a,b)=>{int order=(b.Width*b.Depth).CompareTo(a.Width*a.Depth);if(order!=0)return order;order=a.MinX.CompareTo(b.MinX);return order!=0?order:a.MinZ.CompareTo(b.MinZ);});
            var modules=new List<PlannedModule>();var pools=new List<WaterRegion>();
            int target=c.AutonomousGeneration?4+random.Range(9):8;
            foreach(PlanRect bay in free)
            {
                if(modules.Count>=target)break;
                float width=bay.Width-1.1f,depth=bay.Depth-1.1f;
                if(width<2.4f||depth<2.4f)continue;
                bool first=modules.Count==0;if(!first&&random.Value()<.18f)continue;
                LiminalModule kind=room.Environment==LiminalEnvironment.Poolrooms?
                    (first?LiminalModule.Pool:poolChoices[random.Range(poolChoices.Length)]):
                    (first?LiminalModule.PlayFrame:playChoices[random.Range(playChoices.Length)]);
                float cap=kind==LiminalModule.Pool?14:8;
                if(c.AutonomousGeneration)cap=random.Range(kind==LiminalModule.Pool?6:4,cap);
                width=Math.Min(width,cap);depth=Math.Min(depth,cap);
                float x=bay.X+random.Range(-(bay.Width-width-1.1f)*.5f,(bay.Width-width-1.1f)*.5f);
                float z=bay.Z+random.Range(-(bay.Depth-depth-1.1f)*.5f,(bay.Depth-depth-1.1f)*.5f);
                int turns=Facing(circulation,x,z);float lw=(turns&1)==0?width:depth,ld=(turns&1)==0?depth:width;
                if((kind==LiminalModule.Platform||kind==LiminalModule.PlayFrame||kind==LiminalModule.Slide)&&(lw<3.4f||ld<4))kind=LiminalModule.Tunnel;
                if(kind==LiminalModule.Slide&&(lw<4.4f||ld<5))kind=LiminalModule.PlayFrame;
                if(kind==LiminalModule.Tunnel&&lw<3)kind=LiminalModule.Bench;
                float rise=0,radius=0;
                if(kind==LiminalModule.Pool)
                {
                    int maxSteps=Math.Min(6,(int)((Math.Min(width,depth)-1.4f)/.6f));
                    rise=(1+random.Range(Math.Max(1,maxSteps)))*.21f;radius=random.Range(0,Math.Min(width,depth)*.48f);
                    pools.Add(new WaterRegion{CenterX=x,CenterZ=z,Width=width,Depth=depth,Radius=radius,Bottom=-rise,Level=c.WaterLevel});
                }
                if(kind==LiminalModule.Platform||kind==LiminalModule.PlayFrame||kind==LiminalModule.Slide)
                    rise=(3+random.Range(Math.Max(1,Math.Min(6,(int)((ld-2)*.4f/.2f))-2)))*.2f;
                modules.Add(new PlannedModule(kind,x,z,width,depth,rise,radius,turns,random.Next()));
            }
            return new ProceduralPlan(room.Environment,modules.ToArray(),pools.ToArray(),c.DoorWidth*.5f+.8f,random.Range(3),random.Range(3),false,circulation);
        }
        internal static List<PlanRect> Bays(PlanRect bounds,List<PlanRect> blocked)
        {
            // Greedy maximal empty rectangles. Sequential subtraction fragments otherwise
            // useful bays into thin strips when several circulation strokes overlap.
            var xs=new List<float>{bounds.MinX,bounds.MaxX};var zs=new List<float>{bounds.MinZ,bounds.MaxZ};
            foreach(PlanRect b in blocked)
            {
                if(b.MinX>bounds.MinX&&b.MinX<bounds.MaxX)xs.Add(b.MinX);if(b.MaxX>bounds.MinX&&b.MaxX<bounds.MaxX)xs.Add(b.MaxX);
                if(b.MinZ>bounds.MinZ&&b.MinZ<bounds.MaxZ)zs.Add(b.MinZ);if(b.MaxZ>bounds.MinZ&&b.MaxZ<bounds.MaxZ)zs.Add(b.MaxZ);
            }
            Unique(xs);Unique(zs);int nx=xs.Count-1,nz=zs.Count-1;var occupied=new bool[nx,nz];
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)foreach(PlanRect b in blocked)
                if(b.Contains((xs[x]+xs[x+1])*.5f,(zs[z]+zs[z+1])*.5f)){occupied[x,z]=true;break;}
            var result=new List<PlanRect>();var available=new bool[nx];
            for(int bay=0;bay<12;bay++)
            {
                float best=0;int bx0=0,bx1=0,bz0=0,bz1=0;
                for(int z0=0;z0<nz;z0++)
                {
                    for(int x=0;x<nx;x++)available[x]=true;
                    for(int z1=z0;z1<nz;z1++)
                    {
                        int run=-1;float depth=zs[z1+1]-zs[z0];
                        for(int x=0;x<=nx;x++)
                        {
                            if(x<nx)available[x]&=!occupied[x,z1];
                            if(x<nx&&available[x]){if(run<0)run=x;continue;}
                            if(run<0)continue;
                            float width=xs[x]-xs[run],area=width*depth;
                            if(width>=3.5f&&depth>=3.5f&&area>best){best=area;bx0=run;bx1=x;bz0=z0;bz1=z1+1;}
                            run=-1;
                        }
                    }
                }
                if(best==0)break;result.Add(new PlanRect(xs[bx0],zs[bz0],xs[bx1],zs[bz1]));
                for(int x=bx0;x<bx1;x++)for(int z=bz0;z<bz1;z++)occupied[x,z]=true;
            }
            return result;
        }
        private static void Unique(List<float> values)
        {
            values.Sort();for(int i=values.Count-1;i>0;i--)if(values[i]-values[i-1]<.0001f)values.RemoveAt(i);
        }
        private static int Facing(PlanCirculation circulation,float x,float z)
        {
            float distance=float.MaxValue;float dx=0,dz=-1;
            foreach(PlanRect r in circulation.Reserved)
            {
                float px=Math.Max(r.MinX,Math.Min(r.MaxX,x)),pz=Math.Max(r.MinZ,Math.Min(r.MaxZ,z));
                float d=(px-x)*(px-x)+(pz-z)*(pz-z);if(d>=distance)continue;
                distance=d;dx=px-x;dz=pz-z;
            }
            // Module entrance is local -Z. MeshData rotates +Z towards +X.
            return Math.Abs(dx)>Math.Abs(dz)?(dx<0?1:3):(dz<0?0:2);
        }
    }
}
