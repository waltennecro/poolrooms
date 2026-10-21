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
        public static void Attach(RoomGeometry data,Transform parent,PoolroomsProfile profile,PoolroomsChunk owner,bool collision,float baseY)
        {
            foreach(var visual in data.VisualMeshes)
            {
                visual.TranslateRotate(new V3(0,-baseY,0),0);
                MeshFactory.Create("Retail finish / "+visual.Theme,visual,parent,profile,owner,false);
            }
            if(collision&&data.CollisionOnly.TriangleCount>0)
            {
                data.CollisionOnly.TranslateRotate(new V3(0,-baseY,0),0);
                MeshFactory.CreateCollider("Retail furniture collision",data.CollisionOnly,parent,owner);
            }
        }
    }
}
