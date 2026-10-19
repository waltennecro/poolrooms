using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

namespace Poolrooms
{
    public static class MeshFactory
    {
        private static readonly ProfilerMarker UploadMarker=new ProfilerMarker("LiminalWorld.MeshUpload");
        private static readonly ProfilerMarker CollisionMarker=new ProfilerMarker("LiminalWorld.ColliderCook");
        // Unity mesh setters copy their input synchronously. These main-thread upload buffers
        // can be reused after each call instead of allocating megabytes for every streamed mesh.
        private static readonly List<Vector3> UploadPositions=new List<Vector3>();
        private static readonly List<Vector3> UploadNormals=new List<Vector3>();
        private static readonly List<Vector2> UploadUV=new List<Vector2>();
        private static readonly List<Color> UploadColors=new List<Color>();
        private static readonly List<int> PhysicsTriangles=new List<int>();
        private static readonly List<Material> UploadMaterials=new List<Material>(6);
        private static readonly string[] ColorProperties={"_BaseColor","_Color"};
        // Physics-only meshes need positions and triangles, not renderer/tangent/UV buffers.
        public static MeshCollider CreateCollider(string name,MeshData data,Transform parent,PoolroomsChunk owner,int supportIndexCount=-1)
        {
            using var timing=CollisionMarker.Auto();
            if(data.TriangleCount==0)return null;
            var triangles=PhysicsTriangles;triangles.Clear();if(triangles.Capacity<data.TriangleCount*3)triangles.Capacity=data.TriangleCount*3;
            foreach(var sub in data.Indices)triangles.AddRange(sub);
            if(supportIndexCount>=0)
            {
                if(supportIndexCount>triangles.Count||supportIndexCount%3!=0)throw new System.ArgumentOutOfRangeException(nameof(supportIndexCount));
                if(supportIndexCount<triangles.Count)triangles.RemoveRange(supportIndexCount,triangles.Count-supportIndexCount);
            }
            int positionCount=data.Positions.Count;
            if(supportIndexCount>=0)
            {
                // Skirt vertices occur after the support prefix. They must not inflate the
                // physics bounds or copy unreferenced visual buffers into the support mesh.
                positionCount=0;foreach(int index in triangles)positionCount=System.Math.Max(positionCount,index+1);
            }
            var mesh=new Mesh{name=name};if(positionCount>65535)mesh.indexFormat=IndexFormat.UInt32;
            var positions=UploadPositions;positions.Clear();if(positions.Capacity<positionCount)positions.Capacity=positionCount;
            for(int i=0;i<positionCount;i++){var p=data.Positions[i];positions.Add(new Vector3(p.X,p.Y,p.Z));}
            mesh.SetVertices(positions);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var go=new GameObject(name);go.transform.SetParent(parent,false);var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
            owner.ownedMeshes.Add(mesh);return collider;
        }
        private static Material PaletteMaterial(MeshData data,int surface,PoolroomsProfile profile,PoolroomsChunk owner)
        {
            SurfaceTheme theme=BiomeSurfaces.Resolve(data);
            if(data.AquaticColor>=0&&surface==(int)Surface.Slide)return PoolroomsBiomeMaterialFactory.SlideColor(data.AquaticColor,owner);
            if(theme==SurfaceTheme.Poolrooms||theme==SurfaceTheme.Automatic)return profile.MaterialFor(surface);
            if(theme==SurfaceTheme.Mall&&data.Weathering>.5f&&(surface==(int)Surface.Tile||surface==(int)Surface.Plaster))return PoolroomsBiomeMaterialFactory.Weathered(surface,owner);
            Material material=PoolroomsBiomeMaterialFactory.Resolve(theme,surface,profile,owner);
            if(data.PaletteVariant==0||theme==SurfaceTheme.Mall||theme==SurfaceTheme.Glass||theme==SurfaceTheme.Merchandise||surface==(int)Surface.Glow)return material;
            int key=8192+(int)theme*128+data.PaletteVariant*6+surface;Material cached;
            if(owner.paletteMaterials.TryGetValue(key,out cached))return cached;
            var tinted=new Material(material){name=material.name+" / mood "+data.PaletteVariant,enableInstancing=true};
            V3 rgb=theme==SurfaceTheme.IndoorPlay?LiminalPalette.Color((Surface)surface,data.PaletteVariant):RetailPalette.Color((Surface)surface,data.PaletteVariant);
            Color color=new Color(rgb.X,rgb.Y,rgb.Z);
            if(theme!=SurfaceTheme.IndoorPlay||surface==(int)Surface.Plaster||surface==(int)Surface.Tile)color=Color.Lerp(Color.white,color,.25f);
            foreach(string property in ColorProperties)if(tinted.HasProperty(property))tinted.SetColor(property,color);
            owner.paletteMaterials.Add(key,tinted);owner.ownedMaterials.Add(tinted);return tinted;
        }
        public static GameObject Create(string name,MeshData data,Transform parent,PoolroomsProfile profile,
            PoolroomsChunk owner,bool collider,bool water=false,Material[] materialOverrides=null)
        {
            using var timing=UploadMarker.Auto();
            if(data.TriangleCount==0)return null;
            var mesh=new Mesh{name=name};
            if(data.Positions.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
            var positions=UploadPositions;var normals=UploadNormals;var uv=UploadUV;
            positions.Clear();normals.Clear();uv.Clear();
            if(positions.Capacity<data.Positions.Count)positions.Capacity=data.Positions.Count;
            if(normals.Capacity<data.Normals.Count)normals.Capacity=data.Normals.Count;
            if(uv.Capacity<data.UV.Count)uv.Capacity=data.UV.Count;
            foreach(V3 p in data.Positions)positions.Add(new Vector3(p.X,p.Y,p.Z));
            foreach(V3 n in data.Normals)normals.Add(new Vector3(n.X,n.Y,n.Z));
            foreach(V2 t in data.UV)uv.Add(new Vector2(t.X,t.Y));
            mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);
            if(data.BlendWeights.Count==data.Positions.Count)
            {var colors=UploadColors;colors.Clear();if(colors.Capacity<data.BlendWeights.Count)colors.Capacity=data.BlendWeights.Count;foreach(V3 w in data.BlendWeights)colors.Add(new Color(w.X,w.Y,w.Z,1));mesh.SetColors(colors);}
            var materials=UploadMaterials;materials.Clear();int count=0;
            foreach(var tris in data.Indices)if(tris.Count>0)count++;
            mesh.subMeshCount=count;int index=0;
            for(int i=0;i<data.Indices.Length;i++)if(data.Indices[i].Count>0)
            {mesh.SetTriangles(data.Indices[i],index++,false);Material material=materialOverrides!=null&&i<materialOverrides.Length?materialOverrides[i]:water?PoolroomsClearWaterMaterial.Resolve(profile,owner):PaletteMaterial(data,i,profile,owner);
                if(!water&&i==(int)Surface.Glow&&data.EmissionStrength>=0&&material!=null)
                {
                    material=new Material(material){name="Zone light panels"};
                    Color tint=new Color(data.EmissionTint.X,data.EmissionTint.Y,data.EmissionTint.Z);
                    if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",tint);
                    if(material.HasProperty("_Color"))material.SetColor("_Color",tint);
                    if(material.HasProperty("_EmissiveColor"))material.SetColor("_EmissiveColor",tint*(12*data.EmissionStrength));
                    if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",tint*(4*data.EmissionStrength));
                    owner.ownedMaterials.Add(material);
                }
                materials.Add(material);}
            mesh.RecalculateBounds();mesh.RecalculateTangents();
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials.ToArray();
            renderer.shadowCastingMode=water||data.Theme==SurfaceTheme.Glass||data.Theme==SurfaceTheme.SlideGlass||data.Theme==SurfaceTheme.FlumeFlow?ShadowCastingMode.Off:ShadowCastingMode.On;
            renderer.receiveShadows=true;renderer.motionVectorGenerationMode=MotionVectorGenerationMode.Camera;
            if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;
            owner.ownedMeshes.Add(mesh);return go;
        }
    }
}
