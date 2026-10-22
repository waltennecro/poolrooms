using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    public static class PoolroomsMallMaterials
    {
        [System.Serializable] private sealed class Palette{public City11Sources.MaterialDefinition[] materials=new City11Sources.MaterialDefinition[0];}
        private static Palette palette;
        public static Material Resolve(SurfaceTheme theme,int slot,int variant,PoolroomsChunk owner)
        {
            if(slot==(int)Surface.Glow)return null;
            if(palette==null)
            {
                var text=Resources.Load<TextAsset>("MallFinishes/palette");
                if(text==null)return null;palette=JsonUtility.FromJson<Palette>(text.text);
            }
            int index=MallRealism.Finish(theme,(Surface)slot,variant);
            if(index<0||palette.materials==null||index>=palette.materials.Length)return null;
            // Shared read-only source materials and mip-streamed maps; each sector
            // does not allocate another 4K texture or another source material.
            return City11Sources.MaterialFor(palette.materials[index]);
        }
        public static MeshData[] PrepareSurfaces(RoomGeometry data,float baseY)
        {
            // Retail meshes are authored in floor coordinates, then parented at y.
            // Resolve overlap only after all finish meshes share room-local space.
            foreach(var visual in data.VisualMeshes)visual.TranslateRotate(new V3(0,-baseY,0),0);
            return MeshFactory.RoomSurfaces(data);
        }
        public static void Attach(RoomGeometry data,Transform parent,PoolroomsProfile profile,PoolroomsChunk owner,bool collision,float baseY,MeshData[] renderSurfaces=null)
        {
            if(renderSurfaces==null)renderSurfaces=PrepareSurfaces(data,baseY);
            for(int i=0;i<data.VisualMeshes.Count;i++)
            {
                var visual=data.VisualMeshes[i];
                MeshFactory.Create("Retail finish / "+visual.Theme,visual,parent,profile,owner,false,renderData:renderSurfaces[i+2]);
            }
            if(collision&&data.CollisionOnly.TriangleCount>0)
            {
                data.CollisionOnly.TranslateRotate(new V3(0,-baseY,0),0);
                MeshFactory.CreateCollider("Retail furniture collision",data.CollisionOnly,parent,owner);
            }
        }
    }
}
