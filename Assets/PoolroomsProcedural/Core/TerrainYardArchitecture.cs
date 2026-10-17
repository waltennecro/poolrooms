using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum YardKind { Picnic, Bench, ClothesLine, ToolShelter, CycleRack }
    public readonly struct TerrainYardObject
    {
        public readonly YardKind Kind;
        public readonly PlanRect Bounds;
        public readonly float Base;
        public readonly uint Seed;
        public TerrainYardObject(YardKind kind,PlanRect bounds,float floor,uint seed){Kind=kind;Bounds=bounds;Base=floor;Seed=seed;}
    }
    public static class TerrainYardArchitecture
    {
        public const int PerHouseLimit=2;
        public static TerrainYardObject[] Plan(TerrainRegionPlan region,TerrainHousePlan house)
        {
            if(region==null)return Array.Empty<TerrainYardObject>();
            var random=new StableRandom(unchecked((uint)house.Seed)^0x25094u);var result=new List<TerrainYardObject>();
            for(int attempt=0;attempt<8&&result.Count<PerHouseLimit;attempt++)
            {
                var kind=(YardKind)random.Range(5);float w=kind==YardKind.Picnic?3:kind==YardKind.ClothesLine?3.1f:2.4f,d=kind==YardKind.Picnic?2.4f:1.65f;
                float x,z;
                if(attempt%2==0){x=random.Range(-house.Width*.22f,house.Width*.22f);z=house.Depth*.5f+4.2f;}
                else{x=(random.Range(2)==0?-1:1)*(house.Width*.5f+3.6f);z=random.Range(-1,2);}
                var bounds=new PlanRect(x-w*.5f,z-d*.5f,x+w*.5f,z+d*.5f);
                if(house.Blueprint!=null&&(house.X+bounds.MinX-.3f<house.CellX*64-32||house.X+bounds.MaxX+.3f>house.CellX*64+32||
                    house.Z+bounds.MinZ-.3f<house.CellZ*64-32||house.Z+bounds.MaxZ+.3f>house.CellZ*64+32))continue;
                bool valid=true;foreach(var old in result)if(bounds.Overlaps(old.Bounds,1)){valid=false;break;}
                if(!valid)continue;
                float min=float.MaxValue,max=float.MinValue;
                // Sample the complete support and its margin; lanes and entry clearing remain clear.
                for(int iz=0;iz<3;iz++)for(int ix=0;ix<3;ix++)
                {
                    float wx=house.X+bounds.MinX-.3f+(bounds.Width+.6f)*ix*.5f,wz=house.Z+bounds.MinZ-.3f+(bounds.Depth+.6f)*iz*.5f;
                    if(region.PathDistance(wx,wz)<4||Math.Abs(wx)<12&&Math.Abs(wz)<12)valid=false;
                    float y=region.Height(wx,wz);min=Math.Min(min,y);max=Math.Max(max,y);
                }
                if(max-min>.24f)valid=false;
                if(valid)result.Add(new TerrainYardObject(kind,bounds,max-house.Base+.018f,random.Next()));
            }
            return result.ToArray();
        }
        public static void Append(RoomGeometry output,TerrainRegionPlan region,TerrainHousePlan house)
        {foreach(var site in Plan(region,house))Build(output.Structure,output.Decoration,site);}
        public static void Build(MeshData structure,MeshData decoration,TerrainYardObject site)
        {
            var solid=new MeshData();var detail=new MeshData();float w=site.Bounds.Width,d=site.Bounds.Depth;
            Action<MeshData,float,float,float,float,float,float,Surface> b=(m,x,z,bw,bd,lo,hi,s)=>ZoneShapes.Box(m,x,z,bw,bd,lo,hi,s,1.2f);
            b(solid,0,0,w,d,-.3f,0,Surface.Accent);
            switch(site.Kind)
            {
                case YardKind.Picnic:
                    b(solid,0,0,w*.77f,.8f,.73f,.8f,Surface.Accent);
                    foreach(int side in new[]{-1,1})
                    {b(solid,0,side*.86f,w*.86f,.4f,.39f,.46f,Surface.Accent);b(solid,side*.84f,0,.12f,1.9f,0,.73f,Surface.Metal);}
                    for(int i=0;i<4;i++)b(detail,0,(i-1.5f)*.19f,w*.74f,.016f,.801f,.806f,Surface.Metal);break;
                case YardKind.Bench:
                    AutonomousArchitecture.Build(solid,detail,new GeneratedAssembly(AssemblyKind.Bench,new PlanRect(-w*.46f,-d*.28f,w*.46f,d*.28f),0,site.Seed),12);break;
                case YardKind.ClothesLine:
                    foreach(int side in new[]{-1,1}){b(solid,side*w*.44f,0,.08f,.08f,0,1.9f,Surface.Metal);b(solid,side*w*.44f,0,.09f,d*.78f,1.83f,1.89f,Surface.Metal);}
                    foreach(int side in new[]{-1,1})b(detail,0,side*d*.3f,w*.88f,.012f,1.87f,1.885f,Surface.Metal);
                    for(int i=0;i<3;i++)b(detail,(i-1)*w*.25f,-d*.3f,w*.19f,.025f,1.15f,1.87f,i%2==0?Surface.Plaster:Surface.Slide);break;
                case YardKind.ToolShelter:
                    b(solid,0,d*.44f,w,.12f,0,2.15f,Surface.Plaster);
                    foreach(int side in new[]{-1,1})b(solid,side*w*.46f,0,.12f,d,0,2.15f,Surface.Plaster);
                    b(solid,0,0,w,d,2.15f,2.25f,Surface.Accent);
                    b(solid,0,d*.25f,w*.76f,d*.34f,.82f,.89f,Surface.Accent);
                    for(int i=0;i<3;i++)b(detail,(i-1)*w*.25f,d*.37f,.16f,.16f,.89f,1.13f,Surface.Metal);break;
                case YardKind.CycleRack:
                    b(solid,0,0,w*.92f,.1f,.03f,.12f,Surface.Metal);
                    for(int i=0;i<4;i++)
                    {float x=(i-1.5f)*w*.23f;foreach(int side in new[]{-1,1})b(solid,x,side*.34f,.04f,.04f,.05f,.7f,Surface.Metal);b(solid,x,0,.04f,.72f,.65f,.7f,Surface.Metal);}break;
            }
            V3 origin=new V3(site.Bounds.X,site.Base,site.Bounds.Z);solid.TranslateRotate(origin,0);detail.TranslateRotate(origin,0);structure.Append(solid);decoration.Append(detail);
        }
    }
}
