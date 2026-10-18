using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum AquaticSlideStyle { OpenFlume, EnclosedS, TwinRacer, SpiralTube, GentleWave, FamilyFlume, SlalomTube, SweepingFlume, OffsetCascade, DoubleSweep }
    public sealed class AquaticSlidePath
    {
        public readonly V3[] Points;
        public readonly float[] Distances;
        public readonly float Radius;
        public readonly bool Enclosed;
        public readonly AquaticSlideFamily Family;
        public readonly int Color;
        public float Length {get{return Distances[Distances.Length-1];}}
        public AquaticSlidePath(V3[] points,float radius,bool enclosed,AquaticSlideFamily? family=null,int lane=0)
        {
            Points=points;Radius=radius;Enclosed=enclosed;Family=family??AquaticSlideCatalog.Get(enclosed?10:0);Color=(Family.Color+lane*3)%AquaticSlideCatalog.ColorCount;Distances=new float[points.Length];
            for(int i=1;i<points.Length;i++)Distances[i]=Distances[i-1]+(points[i]-points[i-1]).Length;
        }
        public V3 Sample(float distance,out V3 tangent)
        {
            distance=Math.Max(0,Math.Min(Length,distance));int lo=0,hi=Distances.Length-1;
            while(hi-lo>1){int mid=(lo+hi)/2;if(Distances[mid]<distance)lo=mid;else hi=mid;}
            float span=Distances[hi]-Distances[lo],u=span>0?(distance-Distances[lo])/span:0;
            tangent=(Points[hi]-Points[lo]).Unit;return Points[lo]*(1-u)+Points[hi]*u;
        }
        public V3 Tangent(int i){return (Points[Math.Min(Points.Length-1,i+1)]-Points[Math.Max(0,i-1)]).Unit;}
    }
    // Arc-distance motion, fixed bounded substeps. No rigidbody impulse or unbounded acceleration.
    public sealed class AquaticRideMotion
    {
        public float Distance {get;private set;}
        public float Speed {get;private set;}
        public bool Finished {get;private set;}
        public void Reset(){Distance=0;Speed=1.1f;Finished=false;}
        public V3 Advance(AquaticSlidePath path,float dt)
        {
            dt=ExplorerLocomotion.FrameTime(dt);int steps=Math.Max(1,(int)Math.Ceiling(dt*120));
            for(int i=0;i<steps&&!Finished;i++)
            {
                float step=dt/steps;path.Sample(Distance,out var tangent);
                Speed=Math.Max(1.1f,Math.Min(6.5f,Speed+(1.6f-9.81f*tangent.Y-.25f*Speed)*step));
                Distance=Math.Min(path.Length,Distance+Speed*step);Finished=Distance>=path.Length;
            }
            return path.Sample(Distance,out _);
        }
    }
    public sealed class AquaticParkPlan
    {
        public const int CatalogCount=5600;
        public readonly int Recipe,Hall,PoolShape,Roof,HeightClass,Orientation;
        public readonly AquaticSlideStyle Style;
        public readonly AquaticSlideFamily Family;
        public readonly float Clear,Height,TowerHeight,Half;
        public readonly WaterRegion[] Pools;
        public readonly AquaticSlidePath[] Slides;
        public readonly QuietPropPlan[] Props;
        public readonly V3 Valve;
        internal readonly WaterRegion[] LocalPools;
        internal readonly AquaticSlidePath[] LocalSlides;
        internal readonly V3 LocalValve;
        public V3 Rotate(V3 p)
        {switch(Orientation){case 1:return new V3(p.Z,p.Y,-p.X);case 2:return new V3(-p.X,p.Y,-p.Z);case 3:return new V3(-p.Z,p.Y,p.X);default:return p;}}
        public V3 DeckApproach(AquaticSlidePath path){return path.Points[0]+Rotate(new V3(0,.04f,-.75f));}

        public int FlightSteps {get{return 15+HeightClass+Family.SizeProfile;}}
        public float FlightRun {get{return FlightSteps*.27f;}}
        public AquaticParkPlan(int recipe,float half,float clear,WaterRegion[] pools,AquaticSlidePath[] slides,QuietPropPlan[] props,int familyId=-1,int orientation=0)
        {
            Recipe=recipe;Hall=recipe%7;Family=AquaticSlideCatalog.Get(familyId<0?AquaticSlideCatalog.At(recipe):familyId);Style=Family.Route;PoolShape=recipe/56%5;Roof=recipe/280%5;HeightClass=recipe/1400;
            Orientation=orientation&3;Half=half;Clear=clear;TowerHeight=(15+HeightClass+Family.SizeProfile)*.36f;Height=12.4f+HeightClass*.45f;LocalPools=pools;LocalSlides=slides;
            Pools=new WaterRegion[pools.Length];
            for(int i=0;i<pools.Length;i++)
            {var pool=pools[i];var point=Rotate(new V3(pool.CenterX,0,pool.CenterZ));pool.CenterX=point.X;pool.CenterZ=point.Z;if((Orientation&1)!=0){float span=pool.Width;pool.Width=pool.Depth;pool.Depth=span;}Pools[i]=pool;}
            Slides=new AquaticSlidePath[slides.Length];
            for(int lane=0;lane<slides.Length;lane++)
            {var original=slides[lane];var points=new V3[original.Points.Length];for(int i=0;i<points.Length;i++)points[i]=Rotate(original.Points[i]);Slides[lane]=new AquaticSlidePath(points,original.Radius,original.Enclosed,Family,lane);}
            Props=new QuietPropPlan[props.Length];for(int i=0;i<props.Length;i++)Props[i]=new QuietPropPlan(Rotate(props[i].Bottom),props[i].CatalogId,props[i].Source,props[i].Yaw+Orientation*90);
            LocalValve=new V3(-clear-.6f,1.1f,clear+1.2f);Valve=Rotate(LocalValve);
        }
        private static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
        public bool Valid(Room r,LayoutConfig c)
        {
            if(Recipe<0||Recipe>=CatalogCount||r.Environment!=LiminalEnvironment.AquaticPark||r.Width!=c.CellSize||r.Depth!=c.CellSize||r.Height!=Height||Pools.Length<2||Props.Length!=12||Slides.Length!=(Style==AquaticSlideStyle.TwinRacer?2:1))return false;
            foreach(var pool in Pools)if(pool.Level!=c.WaterLevel||pool.Bottom>=pool.Level||Math.Min(pool.Width,pool.Depth)<4||Math.Abs(pool.CenterX)+pool.Width*.5f>Half-.8f||Math.Abs(pool.CenterZ)+pool.Depth*.5f>Half-.8f)return false;
            foreach(var slide in Slides)
            {
                if(slide.Length<10||slide.Radius<1.05f||slide.Points.Length<32)return false;
                foreach(var v in slide.Points)
                    if(!Finite(v.X)||!Finite(v.Y)||!Finite(v.Z)||Math.Abs(v.X)+slide.Radius+.09f>Half-.35f||Math.Abs(v.Z)+slide.Radius+.09f>Half-.35f||v.Y<.24f||v.Y+2*slide.Radius+.09f>Height-.8f||(Math.Abs(v.Z)<Clear+.4f||Math.Abs(v.X)<Clear+.4f)&&v.Y<2.65f)return false;
                var end=slide.Points[slide.Points.Length-1];if(!Pools[0].Contains(end.X,end.Z))return false;
            }
            return true;
        }
    }
    public static class AquaticParkPlanner
    {
        public static int RecipeAt(int seed,int x,int z)
        {
            long xx=((long)x%80+80)%80,zz=((long)z%70+70)%70;
            uint h=LiminalRegions.Hash(seed,ContinuousTopology.FloorDivide(x,80),ContinuousTopology.FloorDivide(z,70),0x220A01u);
            int multiplier=1+2*(int)(h%2800);while(multiplier%5==0||multiplier%7==0)multiplier+=2;
            return (int)(((xx*70+zz)*multiplier+(h>>10))%5600);
        }
        public static void Describe(Room r,LayoutConfig c){DescribeRecipe(r,c,RecipeAt(c.Seed,r.X,r.Z));}
        public static void DescribeRecipe(Room r,LayoutConfig c,int recipe,int familyOverride=-1,int orientationOverride=-1)
        {
            if(recipe<0||recipe>=AquaticParkPlan.CatalogCount)throw new ArgumentOutOfRangeException(nameof(recipe));
            float half=c.CellSize*.5f,clear=AquaticParkArchitecture.MaxOpening(c)*.5f+.8f;
            int hall=recipe%7,shape=recipe/56%5,hc=recipe/1400;
            var family=AquaticSlideCatalog.Get(familyOverride>=0?familyOverride:AquaticSlideCatalog.At(recipe));var style=family.Route;
            float height=(15+hc+family.SizeProfile)*.36f,left=-(half+clear)*.5f,w=half-clear-2.1f;
            var pools=new List<WaterRegion>();float rz=clear+1.25f+(half-clear-2.25f)*.5f,depth=half-clear-2.25f;
            // Receiving pool stays off every ground-level exit route, with steps on all sides.
            pools.Add(new WaterRegion{CenterX=(clear+half-1.1f)*.5f,CenterZ=rz,Width=half-clear-1.1f,Depth=depth,Radius=0,Bottom=-.63f,Level=c.WaterLevel});
            for(int i=0;i<2;i++)
            {
                float span=depth*(.65f+hall*.05f),width=w*(shape==1?.86f:shape==3?.93f:1),radius=shape==0?0:shape==1?.35f:shape==2?.8f:shape==3?1.3f:Math.Min(width,span)*.5f;
                pools.Add(new WaterRegion{CenterX=left,CenterZ=(i==0?-1:1)*(clear+1.1f+depth*.5f),Width=width,Depth=span,Radius=radius,Bottom=-(2+hall%3)*.21f,Level=c.WaterLevel});
            }
            var slides=new List<AquaticSlidePath>();int lanes=style==AquaticSlideStyle.TwinRacer?2:1;
            for(int lane=0;lane<lanes;lane++)
            {
                float radius=family.Radius;
                bool tube=family.Enclosed;
                var points=new List<V3>();float minX=clear+1.25f,maxX=half-4.6f;
                float x=lanes==2?(lane==0?maxX:minX):maxX-family.EntryOffset,z=-half+2.2f;
                if(style==AquaticSlideStyle.FamilyFlume)x-=.25f;
                points.Add(new V3(x,height,z));
                float cruising=2.85f+hc*.08f;
                if(style==AquaticSlideStyle.SpiralTube)
                {
                    float ring=1.55f+family.SizeProfile*.1f,cz=-half+5.3f,cx=x-ring;
                    for(int i=1;i<=16;i++)points.Add(new V3(x,height-.15f*i/16,z+(cz-z)*i/16));
                    for(int i=1;i<=96;i++)
                    {float t=i/96f,a=t*2*Shapes.Pi;points.Add(new V3(cx+ring*Shapes.Cos(a),height-.15f-(2.4f+family.SizeProfile*.12f)*t,cz+ring*Shapes.Sin(a)));}
                    z=cz;cruising=height-2.55f-family.SizeProfile*.12f;
                }
                float endZ=pools[0].CenterZ,endX=lanes==2?x:pools[0].CenterX;
                for(int i=1;i<=96;i++)
                {
                    float t=i/96f,zz=z+(endZ-z)*t,blend=t*t*(3-2*t),xx=x+(endX-x)*blend;
                    float amplitude=Math.Max(0,Math.Min(xx-minX,maxX-xx))*.9f;
                    if(style==AquaticSlideStyle.EnclosedS)xx-=amplitude*Shapes.Sin(2*Shapes.Pi*t)*Shapes.Sin(Shapes.Pi*t);
                    else if(style==AquaticSlideStyle.SlalomTube)xx-=amplitude*Shapes.Sin(4*Shapes.Pi*t)*Shapes.Sin(Shapes.Pi*t);
                    else if(style==AquaticSlideStyle.SweepingFlume)xx-=amplitude*Shapes.Sin(Shapes.Pi*t);
                    else if(style==AquaticSlideStyle.OffsetCascade)xx-=amplitude*.8f*Shapes.Sin(Shapes.Pi*t)*Shapes.Sin(Shapes.Pi*t);
                    else if(style==AquaticSlideStyle.DoubleSweep)xx-=amplitude*.8f*Shapes.Sin(3*Shapes.Pi*t)*Shapes.Sin(Shapes.Pi*t);
                    else if(style==AquaticSlideStyle.GentleWave)xx-=amplitude*.4f*Shapes.Sin(Shapes.Pi*t);
                    float fall=Math.Max(0,Math.Min(1,(zz-clear-.55f)/(endZ-clear-.55f)));fall=fall*fall*(3-2*fall);
                    float early=Math.Max(0,Math.Min(1,(zz-z)/(-clear-.65f-z)));early=early*early*(3-2*early);
                    float upper=style==AquaticSlideStyle.SpiralTube?cruising:height*(1-early)+cruising*early;
                    float yy=upper*(1-fall)+.28f*fall;
                    if(style==AquaticSlideStyle.GentleWave&&zz<-clear)yy-=.16f*Shapes.Sin(t*6*Shapes.Pi)*Shapes.Sin(Shapes.Pi*t);
                    points.Add(new V3(xx,yy,zz));
                }
                slides.Add(new AquaticSlidePath(points.ToArray(),radius,tube,family,lane));
            }
            var props=new List<QuietPropPlan>();
            for(int i=0;i<4;i++)
            {
                float px=-half+.6f,pz=(i<2?-1:1)*(clear+1.5f)+(i%2)*1.3f*(i<2?-1:1);
                props.Add(new QuietPropPlan(new V3(px,.02f,pz),-1,"B_CafeChair",90));
                props.Add(new QuietPropPlan(new V3(-half+1.0f,.02f,(i<2?-1:1)*(half-1.05f)+(i%2)*.65f*(i<2?1:-1)),(31+i%2)*8+(recipe+i)%8));
                props.Add(new QuietPropPlan(new V3(half-.5f,.02f,half-2.2f-i*.6f),-1,"WaterBottle"));
            }
            int orientation=orientationOverride>=0?orientationOverride&3:(int)(LiminalRegions.Hash(c.Seed,r.X,r.Z,0x230F01u)%4);
            var plan=new AquaticParkPlan(recipe,half,clear,pools.ToArray(),slides.ToArray(),props.ToArray(),family.Id,orientation);
            r.Environment=LiminalEnvironment.AquaticPark;r.ZoneId=-1;r.Width=r.Depth=c.CellSize;r.Height=plan.Height;
            r.Plan=new ProceduralPlan(r.Environment,new PlannedModule[0],plan.Pools,clear,0,0,aquatic:plan);
            if(!plan.Valid(r,c))throw new ArgumentException("Invalid aquatic park plan: "+recipe);
        }
    }
}
