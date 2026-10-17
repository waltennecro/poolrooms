using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class TerrainArchitecture
    {
        // Same globally aligned sample coordinates and derivatives on both sides of each edge.
        // Outer skirts hide LOD T-junctions; physical support always uses the near resolution.
        public static MeshData Ground(TerrainRegionPlan plan,int cellX,int cellZ,int resolution,bool skirts=true)
        {int support;return Ground(plan,cellX,cellZ,resolution,skirts,out support);}
        public static MeshData Ground(TerrainRegionPlan plan,int cellX,int cellZ,int resolution,bool skirts,out int supportIndices)
        {
            if(plan==null)throw new ArgumentNullException(nameof(plan));
            resolution=resolution>=32?32:resolution>=16?16:8;
            var mesh=new MeshData();float step=TerrainRegionPlan.TileSize/resolution;
            float ox=cellX*TerrainRegionPlan.TileSize,oz=cellZ*TerrainRegionPlan.TileSize;
            var holes=new List<PlanRect>();
            for(int iz=cellZ-1;iz<=cellZ+1;iz++)for(int ix=cellX-1;ix<=cellX+1;ix++)
            {
                var home=plan.House(ix,iz);var b=home==null?null:home.Blueprint;
                if(b==null||b.Basements==0)continue;var hole=b.StairHole;
                var cut=new PlanRect(hole.MinX+home.X,hole.MinZ+home.Z,hole.MaxX+home.X,hole.MaxZ+home.Z);
                if(cut.Overlaps(new PlanRect(ox-32,oz-32,ox+32,oz+32)))holes.Add(cut);
            }
            var pieces=new List<PlanRect>(8);
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
            {
                float ax=ox-TerrainRegionPlan.HalfTile+x*step,az=oz-TerrainRegionPlan.HalfTile+z*step,bx=ax+step,bz=az+step;
                if(plan.Connected&&!Clip(plan,ref ax,ref az,ref bx,ref bz))continue;
                pieces.Clear();pieces.Add(new PlanRect(ax,az,bx,bz));
                foreach(var hole in holes)PlanRect.Subtract(pieces,hole);
                foreach(var rect in pieces)
                {
                ax=rect.MinX;az=rect.MinZ;bx=rect.MaxX;bz=rect.MaxZ;
                V3 a=P(plan,ax,az,ox,oz),b=P(plan,bx,az,ox,oz),c=P(plan,bx,bz,ox,oz),d=P(plan,ax,bz,ox,oz);
                Surface surface=Surface.Tile;
                float uv=surface==Surface.Tile?5:3;
                mesh.QuadUV(a,b,c,d,plan.Normal(ax,az),plan.Normal(bx,az),plan.Normal(bx,bz),plan.Normal(ax,bz),
                    new V2(ax/uv,az/uv),new V2(bx/uv,az/uv),new V2(bx/uv,bz/uv),new V2(ax/uv,bz/uv),surface);
                }
            }
            supportIndices=mesh.Indices[(int)Surface.Tile].Count;
            if(skirts&&!plan.Connected)for(int side=0;side<4;side++)for(int i=0;i<resolution;i++)
            {
                float t=-TerrainRegionPlan.HalfTile+i*step;
                V3 a=side<2?P(plan,ox+t,oz+(side==0?-TerrainRegionPlan.HalfTile:TerrainRegionPlan.HalfTile),ox,oz):P(plan,ox+(side==2?-TerrainRegionPlan.HalfTile:TerrainRegionPlan.HalfTile),oz+t,ox,oz);
                V3 b=side<2?P(plan,ox+t+step,oz+(side==0?-TerrainRegionPlan.HalfTile:TerrainRegionPlan.HalfTile),ox,oz):P(plan,ox+(side==2?-TerrainRegionPlan.HalfTile:TerrainRegionPlan.HalfTile),oz+t+step,ox,oz);
                mesh.Quad(a,b,b-V3.Up*7,a-V3.Up*7,side<2?new V3(0,0,side==0?-1:1):new V3(side==2?-1:1,0,0),Surface.Tile,5);
            }
            PaintGround(mesh,plan,ox,oz);return mesh;
        }
        public static MeshData Horizon(TerrainRegionPlan plan,int cx,int cz,int radius)
        {
            var mesh=new MeshData();float ox=cx*TerrainRegionPlan.TileSize,oz=cz*TerrainRegionPlan.TileSize;
            float hole=(radius+.5f)*TerrainRegionPlan.TileSize;
            // Insert the exact near-tile boundary into the coarse horizon grid. This avoids
            // a half-tile gap/overlap when using 64 m tiles with 64 m horizon samples.
            var grid=new List<float>();for(int i=0;i<=64;i++)grid.Add(-2048+i*64);
            if(!grid.Contains(-hole))grid.Add(-hole);if(!grid.Contains(hole))grid.Add(hole);grid.Sort();
            for(int z=0;z<grid.Count-1;z++)for(int x=0;x<grid.Count-1;x++)
            {
                float ax=ox+grid[x],az=oz+grid[z],bx=ox+grid[x+1],bz=oz+grid[z+1];
                if(plan.Connected&&!Clip(plan,ref ax,ref az,ref bx,ref bz))continue;
                if(ax>=ox-hole&&bx<=ox+hole&&az>=oz-hole&&bz<=oz+hole)continue;
                V3 a=new V3(ax,plan.Height(ax,az)-.12f,az),b=new V3(bx,plan.Height(bx,az)-.12f,az);
                V3 c=new V3(bx,plan.Height(bx,bz)-.12f,bz),d=new V3(ax,plan.Height(ax,bz)-.12f,bz);
                mesh.QuadUV(a,b,c,d,plan.Normal(ax,az),plan.Normal(bx,az),plan.Normal(bx,bz),plan.Normal(ax,bz),new V2(ax/5,az/5),new V2(bx/5,az/5),new V2(bx/5,bz/5),new V2(ax/5,bz/5),Surface.Tile);
            }
            PaintGround(mesh,plan,0,0);return mesh;
        }
        public static MeshData Roads(TerrainRegionPlan plan,int cx,int cz)
        {
            var mesh=new MeshData();float ox=cx*TerrainRegionPlan.TileSize,oz=cz*TerrainRegionPlan.TileSize;
            for(int direction=0;direction<2;direction++)for(int line=-1;line<=1;line++)for(int i=0;i<32;i++)
            {
                float a=(direction==0?oz:ox)-TerrainRegionPlan.HalfTile+i*(TerrainRegionPlan.TileSize/32),b=a+TerrainRegionPlan.TileSize/32;
                float approximate=(direction==0?ox:oz)+line*384;
                V2 p=direction==0?new V2(plan.RoadX(approximate,a),a):new V2(a,plan.RoadZ(a,approximate));
                V2 q=direction==0?new V2(plan.RoadX(approximate,b),b):new V2(b,plan.RoadZ(b,approximate));
                Ribbon(mesh,plan,p,q,2.8f,cx,cz,Surface.Accent,.075f);
            }
            foreach(var path in plan.Paths(cx,cz))Ribbon(mesh,plan,path.A,path.B,1.2f,cx,cz,Surface.Plaster,.055f);
            return mesh;
        }
        private static void Ribbon(MeshData m,TerrainRegionPlan p,V2 a,V2 b,float half,int cx,int cz,Surface surface,float lift)
        {
            float dx=b.X-a.X,dz=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dz*dz);if(length<.001f)return;
            // Subdivide long driveways before clipping, so the road follows the actual hills.
            int segments=Math.Max(1,(int)Math.Ceiling(length/4));
            for(int segment=0;segment<segments;segment++)
            {
                float start=segment/(float)segments,end=(segment+1f)/segments;
                V2 u=new V2(a.X+dx*start,a.Y+dz*start),v=new V2(a.X+dx*end,a.Y+dz*end);
                float sx=-dz/length*half,sz=dx/length*half;
                var polygon=new List<V2>{new V2(u.X+sx,u.Y+sz),new V2(v.X+sx,v.Y+sz),new V2(v.X-sx,v.Y-sz),new V2(u.X-sx,u.Y-sz)};
                for(int edge=0;edge<4;edge++)
                {
                    var clipped=new List<V2>();if(polygon.Count==0)break;
                    V2 previous=polygon[polygon.Count-1];float pv=ClipValue(previous,p,cx,cz,edge);
                    foreach(var current in polygon)
                    {
                        float cv=ClipValue(current,p,cx,cz,edge);
                        if((cv>=0)!=(pv>=0)){float t=pv/(pv-cv);clipped.Add(new V2(previous.X+(current.X-previous.X)*t,previous.Y+(current.Y-previous.Y)*t));}
                        if(cv>=0)clipped.Add(current);previous=current;pv=cv;
                    }
                    polygon=clipped;
                }
                for(int i=1;i+1<polygon.Count;i++)
                {
                    V2 x=polygon[0],y=polygon[i],z=polygon[i+1];
                    m.Triangle(new V3(x.X-cx*TerrainRegionPlan.TileSize,p.Height(x.X,x.Y)+lift,x.Y-cz*TerrainRegionPlan.TileSize),new V3(y.X-cx*TerrainRegionPlan.TileSize,p.Height(y.X,y.Y)+lift,y.Y-cz*TerrainRegionPlan.TileSize),new V3(z.X-cx*TerrainRegionPlan.TileSize,p.Height(z.X,z.Y)+lift,z.Y-cz*TerrainRegionPlan.TileSize),p.Normal(x.X,x.Y),p.Normal(y.X,y.Y),p.Normal(z.X,z.Y),new V2(x.X/3,x.Y/3),new V2(y.X/3,y.Y/3),new V2(z.X/3,z.Y/3),surface);
                }
            }
        }
        private static void PaintGround(MeshData mesh,TerrainRegionPlan plan,float ox,float oz)
        {foreach(var v in mesh.Positions)mesh.BlendWeights.Add(new V3(TerrainAppearance.SoilWeight(plan,v.X+ox,v.Z+oz),0,0));}
        private static float ClipValue(V2 p,TerrainRegionPlan plan,int cx,int cz,int edge)
        {
            float x0=cx*TerrainRegionPlan.TileSize-TerrainRegionPlan.HalfTile,x1=cx*TerrainRegionPlan.TileSize+TerrainRegionPlan.HalfTile,z0=cz*TerrainRegionPlan.TileSize-TerrainRegionPlan.HalfTile,z1=cz*TerrainRegionPlan.TileSize+TerrainRegionPlan.HalfTile;
            if(plan.Connected){x0=Math.Max(x0,-plan.HalfWidth);x1=Math.Min(x1,plan.HalfWidth);z0=Math.Max(z0,-plan.HalfDepth);z1=Math.Min(z1,plan.HalfDepth);}
            switch(edge){case 0:return p.X-x0;case 1:return x1-p.X;case 2:return p.Y-z0;default:return z1-p.Y;}
        }
        private static bool Clip(TerrainRegionPlan p,ref float ax,ref float az,ref float bx,ref float bz)
        {ax=Math.Max(ax,-p.HalfWidth);az=Math.Max(az,-p.HalfDepth);bx=Math.Min(bx,p.HalfWidth);bz=Math.Min(bz,p.HalfDepth);return bx>ax&&bz>az;}
        private static V3 P(TerrainRegionPlan p,float x,float z,float ox,float oz){return new V3(x-ox,p.Height(x,z),z-oz);}
    }
}
