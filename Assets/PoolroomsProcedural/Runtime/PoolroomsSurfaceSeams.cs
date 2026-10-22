using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
namespace Poolrooms
{
    // One manager per world, storing only opaque primitives touching the perimeter. No
    // Update loop, global registry, original full-room cache or physics rebuild.
    internal sealed class PoolroomsSurfaceSeams
    {
        sealed class Patch
        {
            public int X,Z,Neighbours;
            public MeshData[] Source;
            public GameObject[] Objects;
        }
        readonly Dictionary<long,Patch> patches=new Dictionary<long,Patch>();
        float cellSize;
        public void Clear(){patches.Clear();}
        public void Register(int x,int z,float size,MeshData[] boundary,Transform parent,PoolroomsProfile profile,PoolroomsChunk owner)
        {
            cellSize=size;var patch=new Patch{X=x,Z=z,Source=boundary,Objects=new GameObject[boundary.Length]};
            for(int i=0;i<boundary.Length;i++)if(boundary[i].TriangleCount>0)
                patch.Objects[i]=MeshFactory.Create("Shared boundary "+i,boundary[i],parent,profile,owner,false,renderData:boundary[i],stableMaterialSlots:true);
            patches.Add(ContinuousTopology.CellKey(x,z),patch);RefreshNear(x,z);
        }
        public void Remove(int x,int z)
        {if(patches.Remove(ContinuousTopology.CellKey(x,z)))RefreshNear(x,z);}
        void RefreshNear(int x,int z)
        {
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                if(TryGet(x,z,dx,dz,out var p))Refresh(p);
        }
        bool TryGet(int x,int z,int dx,int dz,out Patch p)
        {
            long nx=(long)x+dx,nz=(long)z+dz;p=null;
            return nx>=int.MinValue&&nx<=int.MaxValue&&nz>=int.MinValue&&nz<=int.MaxValue&&patches.TryGetValue(ContinuousTopology.CellKey((int)nx,(int)nz),out p);
        }
        void Refresh(Patch p)
        {
            int mask=0;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                if(TryGet(p.X,p.Z,dx,dz,out var neighbour)&&RenderBoundary.HigherPriority(p.X,p.Z,neighbour.X,neighbour.Z))mask|=1<<((dz+1)*3+dx+1);
            if(mask==p.Neighbours)return;
            var covers=new List<MeshData>();
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                if(TryGet(p.X,p.Z,dx,dz,out var n)&&RenderBoundary.HigherPriority(p.X,p.Z,n.X,n.Z))
                    foreach(var source in n.Source)if(source.TriangleCount>0)covers.Add(RenderBoundary.Translate(source,new V3(dx*cellSize,0,dz*cellSize)));
            var clean=covers.Count==0?p.Source:RenderBoundary.Resolve(p.Source,covers);
            for(int i=0;i<clean.Length;i++)if(p.Objects[i]!=null)MeshFactory.UpdateSurface(p.Objects[i],clean[i]);
            p.Neighbours=mask;
        }
    }
}
