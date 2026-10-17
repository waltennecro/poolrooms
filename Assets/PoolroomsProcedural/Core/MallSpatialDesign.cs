using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum MallAtriumShape { LinkedCourts, BroadPlaza, OffsetGalleries, TerracedCourts }
    public enum MallRoofShape { FlatGrid, BarrelVault, Lantern }
    public readonly struct MallEdge
    {
        public readonly V3 A,B;
        public MallEdge(V3 a,V3 b){A=a;B=b;}
    }
    // One description drives slabs, railings, bridges, roof apertures and diagnostic plans.
    // Stores and escalator landings keep their safe envelopes; no prescribed biome sequence.
    public sealed class MallSpatialDesign
    {
        public const float BridgeWidth=4.8f;
        public readonly MallAtriumShape Shape;
        public readonly MallRoofShape Roof;
        public readonly int PlazaSector;
        private readonly MallPlan plan;
        private readonly MallRect[][] voids;
        private readonly MallRect[] roofOpenings;
        private readonly float[][] bridges;
        private readonly float[] generatedHalf,generatedCenter;
        public MallSpatialDesign(MallPlan p)
        {
            plan=p;var r=new StableRandom(LiminalRegions.Hash(p.Seed,p.AddressX,p.AddressZ,0xCA210u));
            Shape=(MallAtriumShape)r.Range(4);Roof=(MallRoofShape)r.Range(3);
            PlazaSector=2+r.Range(p.Bays-3);
            voids=new MallRect[p.Floors][];bridges=new float[p.Floors][];
            var roof=new List<MallRect>();
            if(p.Autonomous)
            {
                generatedHalf=new float[p.Bays];generatedCenter=new float[p.Bays];
                var shapeRng=new StableRandom(LiminalRegions.Hash(p.Seed,p.AddressX,p.AddressZ,0xCA220u));
                for(int b=0;b<p.Bays;b++){generatedHalf[b]=shapeRng.Range(6.8f,11.4f);generatedCenter[b]=shapeRng.Range(-1.3f,1.3f);}
            }
            for(int f=0;f<p.Floors;f++)
            {
                var crossings=new List<float>();
                if(f>0)
                {
                    // Alternating bridge positions give different views on different floors.
                    int first=2+((f+(int)(p.Seed&3))%3);
                    if(p.Autonomous)
                    {
                        var bridgeRng=new StableRandom(LiminalRegions.Hash(p.Seed,p.AddressX,p.AddressZ,0xCA230u+(uint)f));
                        int bay=1+bridgeRng.Range(3);
                        while(bay<p.Bays-1){crossings.Add(-p.ShopHalf+(bay+.5f)*MallPlan.BayWidth);bay+=3+bridgeRng.Range(3);}
                    }
                    else for(int bay=first;bay<p.Bays-1;bay+=4)crossings.Add(-p.ShopHalf+(bay+.5f)*MallPlan.BayWidth);
                }
                bridges[f]=crossings.ToArray();var holes=new List<MallRect>();
                for(int bay=0;bay<p.Bays;bay++)
                {
                    float half=HalfDepth(bay),center=Center(bay);
                    // Galleries can step back, never eat the 3-metre storefront circulation route.
                    if(Shape==MallAtriumShape.TerracedCourts)half=Math.Max(6.5f,half-(f%3)*.7f);
                    var rect=new MallRect(-p.ShopHalf+bay*16,center-half,-p.ShopHalf+(bay+1)*16,center+half);
                    var pieces=new List<MallRect>{rect};
                    foreach(float x in crossings)
                    {
                        var next=new List<MallRect>();
                        foreach(var piece in pieces)next.AddRange(piece.Subtract(new MallRect(x-BridgeWidth*.5f,-13,x+BridgeWidth*.5f,13)));
                        pieces=next;
                    }
                    holes.AddRange(pieces);
                    if(f==0)
                    {
                        float x0=rect.X0+(bay==0?3:.06f),x1=rect.X1-(bay==p.Bays-1?3:.06f);
                        roof.Add(new MallRect(x0,rect.Z0+.6f,x1,rect.Z1-.6f));
                    }
                }
                voids[f]=holes.ToArray();
            }
            roofOpenings=roof.ToArray();
        }
        private float HalfDepth(int bay)
        {
            if(generatedHalf!=null)return generatedHalf[bay];
            switch(Shape)
            {
                case MallAtriumShape.LinkedCourts:return bay%4==0?7.5f:bay%4==2?11.8f:10;
                case MallAtriumShape.BroadPlaza:return Math.Abs(bay-(PlazaSector-1))<=1?12:8.3f;
                case MallAtriumShape.OffsetGalleries:return 9.4f;
                default:return bay%3==0?8.5f:11.8f;
            }
        }
        private float Center(int bay){if(generatedCenter!=null)return generatedCenter[bay];return Shape==MallAtriumShape.OffsetGalleries?(bay/2%2==0?-1.6f:1.6f):0;}
        public IReadOnlyList<MallRect> Voids(int floor){return voids[floor];}
        public IReadOnlyList<MallRect> RoofOpenings { get { return roofOpenings; } }
        public IReadOnlyList<float> Bridges(int floor){return bridges[floor];}
        public float RoofRise(float t)
        {return Roof==MallRoofShape.BarrelVault?.15f+2.2f*(1-t*t):Roof==MallRoofShape.Lantern?.15f+1.8f*(1-Math.Abs(t)):.15f;}
        public IEnumerable<MallEdge> Edges(int floor)
        {
            var holes=voids[floor];
            foreach(var h in holes)
            {
                yield return new MallEdge(new V3(h.X0,0,h.Z0),new V3(h.X1,0,h.Z0));
                yield return new MallEdge(new V3(h.X1,0,h.Z1),new V3(h.X0,0,h.Z1));
                foreach(int side in new[]{-1,1})
                {
                    float x=side<0?h.X0:h.X1;
                    var intervals=new List<V2>{new V2(h.Z0,h.Z1)};
                    foreach(var other in holes)
                    {
                        if(Math.Abs((side<0?other.X1:other.X0)-x)>.001f)continue;
                        var next=new List<V2>();
                        foreach(var interval in intervals)
                        {
                            float a=Math.Max(interval.X,other.Z0),b=Math.Min(interval.Y,other.Z1);
                            if(a>=b)next.Add(interval);
                            else{if(a>interval.X+.001f)next.Add(new V2(interval.X,a));if(b<interval.Y-.001f)next.Add(new V2(b,interval.Y));}
                        }
                        intervals=next;
                    }
                    foreach(var interval in intervals)yield return new MallEdge(new V3(x,0,interval.X),new V3(x,0,interval.Y));
                }
            }
        }
        public static void Mirror(MeshData m)
        {
            for(int i=0;i<m.Positions.Count;i++){V3 v=m.Positions[i];v.X=-v.X;m.Positions[i]=v;v=m.Normals[i];v.X=-v.X;m.Normals[i]=v;}
            // Reflection reverses winding, including Unity MeshCollider's front-facing surfaces.
            foreach(var indices in m.Indices)for(int i=0;i<indices.Count;i+=3){int a=indices[i];indices[i]=indices[i+1];indices[i+1]=a;}
        }
    }
}
