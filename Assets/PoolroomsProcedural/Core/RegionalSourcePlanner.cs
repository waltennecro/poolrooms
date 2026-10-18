using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Find real, clear tabletop supports in generated interiors. A new recipe is
    // not needed for every room; reject placements when the architecture has none.
    public static class RegionalSourcePlanner
    {
        public const float ContactGap=.006f;
        readonly struct Candidate
        {
            public readonly V3 Point;public readonly int Slot;public readonly uint Rank;
            public Candidate(V3 p,int slot,uint rank){Point=p;Slot=slot;Rank=rank;}
        }
        public static void Append(RoomGeometry o,Room room,LayoutConfig c)
        {
            if(!c.AutonomousGeneration||room.Plan==null||room.Plan.City!=null||room.Plan.Rain!=null||room.Plan.Aquatic!=null||room.Plan.Baths!=null||room.Plan.Garden!=null||room.Plan.Quiet!=null||room.MallGateway||room.TerrainGateway)return;
            int budget=c.CurveSegments<=12?1:2;
            if(room.Plan.Interior!=null)foreach(var bay in room.Plan.Interior.Bays)
            {
                if(bay.Variant!=2||bay.Seed%3!=0)continue;
                var camera=new SourceFurniturePlacement(800000+(int)(bay.Seed%100000),6,bay.World(bay.LocalWidth*.5f-.8f,ContactGap,-bay.LocalDepth*.5f+1.25f),bay.QuarterTurns*90);
                if(TryPlace(o,camera)&&o.SourceProps.Count>=budget)return;
            }
            var candidates=new List<Candidate>();int serial=0;float clear=room.Plan.ClearHalfWidth+.25f;
            foreach(var indices in o.Structure.Indices)for(int i=0;i<indices.Count;i+=3,serial++)
            {
                int ia=indices[i];var a=o.Structure.Positions[ia];var b=o.Structure.Positions[indices[i+1]];var d=o.Structure.Positions[indices[i+2]];
                if(o.Structure.Normals[ia].Y<.95f||a.Y<.65f||a.Y>1.2f||Math.Abs(a.Y-b.Y)>.001f||Math.Abs(a.Y-d.Y)>.001f||V3.Cross(b-a,d-a).Length<.025f)continue;
                V3 p=(a+b+d)*(1f/3);
                if(Math.Abs(p.X)<clear||Math.Abs(p.Z)<clear||Math.Abs(p.X)>room.Width*.5f-.5f||Math.Abs(p.Z)>room.Depth*.5f-.5f)continue;
                uint rank=LiminalRegions.Hash(c.Seed,room.X,room.Z,0x1411A0u+(uint)serial);
                // Keep at most 24 best candidates rather than a list proportional
                // to every shelf/triangle in a large procedural interior.
                var candidate=new Candidate(p,9000+serial,rank);
                if(candidates.Count<24)candidates.Add(candidate);
                else{int worst=0;for(int n=1;n<candidates.Count;n++)if(candidates[n].Rank>candidates[worst].Rank)worst=n;if(rank<candidates[worst].Rank)candidates[worst]=candidate;}
            }
            candidates.Sort((a,b)=>a.Rank==b.Rank?a.Slot.CompareTo(b.Slot):a.Rank.CompareTo(b.Rank));
            foreach(var candidate in candidates)
            {
                int source=room.Environment==LiminalEnvironment.IndoorPlay||room.Environment==LiminalEnvironment.SoftPlay?0: candidate.Rank%3==0?1:0;
                var prop=new SourceFurniturePlacement(candidate.Slot,source,candidate.Point+V3.Up*ContactGap,(candidate.Rank%4)*90);
                if(TryPlace(o,prop)&&o.SourceProps.Count>=budget)break;
            }
        }
        static bool TryPlace(RoomGeometry o,SourceFurniturePlacement prop)
        {
            foreach(var placed in o.SourceProps)if((prop.Bottom-placed.Bottom).Length<1.4f||prop.Slot==placed.Slot)return false;
            if(!SourceFurniture.Clear(o.Structure,prop)||!SourceFurniture.Clear(o.Decoration,prop)||!SourceFurniture.Clear(o.Glass,prop)||!SourceFurniture.Clear(o.CollisionOnly,prop))return false;
            var half=SourceFurniture.HalfExtents(prop.Source,prop.Yaw,.025f);
            foreach(int dx in new[]{-1,0,1})foreach(int dz in new[]{-1,0,1})if(!Supported(o.Structure,new V3(prop.Bottom.X+dx*half.X,prop.Bottom.Y-ContactGap,prop.Bottom.Z+dz*half.Z)))return false;
            o.SourceProps.Add(prop);return true;
        }
        public static bool Supported(MeshData mesh,V3 p)
        {
            foreach(var indices in mesh.Indices)for(int i=0;i<indices.Count;i+=3)
            {
                int ia=indices[i];if(mesh.Normals[ia].Y<.9f)continue;var a=mesh.Positions[ia];var b=mesh.Positions[indices[i+1]];var c=mesh.Positions[indices[i+2]];
                if(Math.Abs(a.Y-p.Y)>.002f||Math.Abs(b.Y-p.Y)>.002f||Math.Abs(c.Y-p.Y)>.002f)continue;
                float det=(b.Z-c.Z)*(a.X-c.X)+(c.X-b.X)*(a.Z-c.Z);if(Math.Abs(det)<1e-8f)continue;
                float u=((b.Z-c.Z)*(p.X-c.X)+(c.X-b.X)*(p.Z-c.Z))/det,v=((c.Z-a.Z)*(p.X-c.X)+(a.X-c.X)*(p.Z-c.Z))/det;
                if(u>=-.00001f&&v>=-.00001f&&u+v<=1.00001f)return true;
            }
            return false;
        }
    }
}
