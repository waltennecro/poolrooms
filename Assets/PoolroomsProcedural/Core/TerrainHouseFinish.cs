using System;

namespace Poolrooms.Core
{
    // Visual trim only: no new physical obstacle at a doorway, window or stair.
    public static class TerrainHouseFinish
    {
        public static void SplitWalls(MeshData source,MeshData exterior,MeshData interior,TerrainHousePlan h)
        {
            for(int surface=0;surface<source.Indices.Length;surface++)
            {
                var indices=source.Indices[surface];
                for(int i=0;i<indices.Count;i+=3)
                {
                    int ia=indices[i],ib=indices[i+1],ic=indices[i+2];
                    V3 a=source.Positions[ia],b=source.Positions[ib],c=source.Positions[ic],normal=source.Normals[ia],centre=(a+b+c)*(1/3f);
                    bool wall=surface==(int)Surface.Plaster&&Math.Abs(normal.Y)<.01f&&
                        Math.Abs(centre.X)<=h.Width*.5f+.25f&&Math.Abs(centre.Z)<=h.Depth*.5f+.25f&&
                        (Math.Abs(centre.X)>h.Width*.5f-.3f||Math.Abs(centre.Z)>h.Depth*.5f-.3f)&&
                        V3.Dot(normal,new V3(-centre.X,0,-centre.Z))>0;
                    var target=wall?interior:exterior;
                    target.Triangle(a,b,c,source.Normals[ia],source.Normals[ib],source.Normals[ic],source.UV[ia],source.UV[ib],source.UV[ic],(Surface)surface);
                }
            }
        }
        public static void Append(MeshData m,TerrainHousePlan h)
        {
            float w=h.Width,d=h.Depth,eave=h.Floors*TerrainHousePlan.FloorHeight;
            var r=new StableRandom(StableRandom.Hash(h.Seed,0,0,0x9434Fu));
            Action<float,float,float,float,float,float,Surface> box=(x,z,bw,bd,lo,hi,s)=>ZoneShapes.Box(m,x,z,bw,bd,lo,hi,s,1.2f);
            foreach(int side in new[]{-1,1})
            {
                // Eave fascia, narrow gutter and downpipe on the rear corners.
                box(side*(w*.5f+.24f),0,.13f,d+.85f,eave-.18f,eave+.04f,Surface.Metal);
                box(side*(w*.5f+.34f),0,.095f,d+.8f,eave-.05f,eave+.04f,Surface.Metal);
                box(side*(w*.5f+.16f),d*.5f+.18f,.085f,.085f,.1f,eave,Surface.Metal);
                foreach(int front in new[]{-1,1})
                {
                    box(side*(w*.5f-.05f),front*(d*.5f+.135f),.14f,.075f,0,eave,Surface.Metal);
                    ZoneShapes.Beam(m,new V3(side*(w*.5f+.38f),eave,front*(d*.5f+.43f)),new V3(0,eave+2.13f,front*(d*.5f+.43f)),.07f,Surface.Metal,1.2f,4);
                }
            }
            // Ridge cap and gable vent, both small enough to remain one batched detail mesh.
            box(0,0,.17f,d+.95f,eave+2.08f,eave+2.19f,Surface.Accent);
            foreach(int side in new[]{-1,1})
            {
                box(0,side*(d*.5f+.13f),.58f,.055f,eave+.65f,eave+1.15f,Surface.Metal);
                for(int slat=0;slat<4;slat++)box(0,side*(d*.5f+.17f),.48f,.025f,eave+.71f+slat*.11f,eave+.75f+slat*.11f,Surface.Plaster);
            }
            for(int floor=0;floor<h.Floors;floor++)
            {
                float y=floor*TerrainHousePlan.FloorHeight;
                foreach(int side in new[]{-1,1})foreach(int front in new[]{-1,1})
                {
                    float x=side*w*.29f,z=front*(d*.5f+.18f);
                    box(x,z,1.69f,.22f,y+.82f,y+.9f,Surface.Metal);
                    box(x,z,1.68f,.1f,y+2.22f,y+2.34f,Surface.Metal);
                    if(r.Range(0f,1f)<.55f)foreach(int shutter in new[]{-1,1})
                    {
                        float sx=x+shutter*.98f;
                        box(sx,z,.32f,.05f,y+.92f,y+2.2f,Surface.Plaster);
                        for(int slat=0;slat<5;slat++)box(sx,z+front*.03f,.3f,.022f,y+1.01f+slat*.23f,y+1.07f+slat*.23f,Surface.Metal);
                    }
                }
            }
            box(0,-d*.5f-.16f,1.7f,.1f,2.51f,2.64f,Surface.Metal);
            // Door-side bell and an unlit covered porch fixture (no additional realtime Light).
            box(1.02f,-d*.5f-.16f,.09f,.04f,1.28f,1.41f,Surface.Metal);
            box(-1.2f,-d*.5f-.22f,.16f,.18f,2.01f,2.21f,Surface.Metal);
        }
    }
}
