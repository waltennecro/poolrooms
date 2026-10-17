using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsTerrainGrass : MonoBehaviour
    {
        private PoolroomsWorld world;
        private TerrainRegionPlan plan,pendingPlan;
        private Transform region;
        private TerrainGrassQuality quality,pendingQuality;
        private readonly GeometryBuildJob job=new GeometryBuildJob();
        private readonly Dictionary<long,PoolroomsChunk> patches=new Dictionary<long,PoolroomsChunk>();
        private readonly List<long> remove=new List<long>();
        private int pendingX,pendingZ,pendingRing;
        private readonly Dictionary<long,int> triangleCounts=new Dictionary<long,int>();
        public int RenderedTriangles {get;private set;}
        private readonly Dictionary<long,int> rings=new Dictionary<long,int>();
        private Material[] materials;
        public int PatchCount { get { return patches.Count; } }
        private void Awake(){world=GetComponent<PoolroomsWorld>();}
        private void Update()
        {
            var terrain=world!=null?world.Terrain:null;
            var settings=world!=null&&world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsGameSettings>():null;
            var requested=settings!=null&&settings.Preferences!=null?settings.Preferences.terrainGrass:TerrainGrassQuality.Economy;
            Transform root=terrain!=null?terrain.RegionRoot:null;TerrainRegionPlan target=terrain!=null?terrain.Plan:null;
            if(root!=region||target!=plan||requested!=quality){Clear();region=root;plan=target;quality=requested;}
            if(region==null||plan==null||world.viewer==null||quality==TerrainGrassQuality.Off)
            {if(job.IsCompleted)job.DiscardCompleted();return;}
            Vector3 point=region.InverseTransformPoint(world.viewer.position);
            if(!plan.Contains(point.x,point.z)||point.y-plan.Height(point.x,point.z)>90){Clear();if(job.IsCompleted)job.DiscardCompleted();return;}
            int cx=TerrainGrass.Cell(point.x),cz=TerrainGrass.Cell(point.z),radius=TerrainGrass.Radius(quality);
            remove.Clear();foreach(var entry in patches)
            {int x=unchecked((int)(entry.Key>>32)),z=unchecked((int)entry.Key);if(Math.Max(Math.Abs(x-cx),Math.Abs(z-cz))>radius)remove.Add(entry.Key);}
            foreach(long key in remove){var chunk=patches[key];if(chunk!=null){chunk.gameObject.SetActive(false);Destroy(chunk.gameObject);}patches.Remove(key);rings.Remove(key);RenderedTriangles-=triangleCounts[key];triangleCounts.Remove(key);}
            if(job.HasWork)
            {
                if(!job.IsCompleted)return;RoomGeometry data;
                try{data=job.TakeCompleted();}catch(Exception e){Debug.LogException(e,this);enabled=false;return;}
                if(pendingPlan==plan&&pendingQuality==quality&&Math.Max(Math.Abs(pendingX-cx),Math.Abs(pendingZ-cz))<=radius&&
                    (quality!=TerrainGrassQuality.High||pendingRing==Math.Max(Math.Abs(pendingX-cx),Math.Abs(pendingZ-cz))))
                    Attach(data.Structure,pendingX,pendingZ,pendingRing);
                return; // At most one mesh upload per frame.
            }
            if(!world.ViewerReady)return;
            int nextX=0,nextZ=0,best=int.MaxValue;bool found=false;
            for(int z=cz-radius;z<=cz+radius;z++)for(int x=cx-radius;x<=cx+radius;x++)
            {
                long key=TerrainRegionPlan.Key(x,z);int ring=Math.Max(Math.Abs(x-cx),Math.Abs(z-cz));
                bool exists=patches.ContainsKey(key);if(exists&&(quality!=TerrainGrassQuality.High||rings[key]==ring))continue;
                int distance=(x-cx)*(x-cx)+(z-cz)*(z-cz)+(exists?(rings[key]<ring?-10000:200):100);if(distance>=best)continue;
                nextX=x;nextZ=z;best=distance;found=true;
            }
            if(!found)return;pendingX=nextX;pendingZ=nextZ;pendingPlan=plan;pendingQuality=quality;pendingRing=Math.Max(Math.Abs(nextX-cx),Math.Abs(nextZ-cz));
            int tx=nextX,tz=nextZ;var snapshot=plan;var detail=quality;int tr=pendingRing;
#if UNITY_WEBGL && !UNITY_EDITOR
            Attach(TerrainGrass.Build(snapshot,tx,tz,detail,tr),tx,tz,tr);
#else
            job.Start(()=>{var g=new RoomGeometry();g.Structure.Append(TerrainGrass.Build(snapshot,tx,tz,detail,tr));return g;});
#endif
        }
        private void Attach(MeshData data,int x,int z,int ring)
        {
            long key=TerrainRegionPlan.Key(x,z);PoolroomsChunk old;int oldTriangles;
            triangleCounts.TryGetValue(key,out oldTriangles);
            int budget=quality==TerrainGrassQuality.High?60480:2700;
            if(RenderedTriangles-oldTriangles+data.TriangleCount>budget)return;
            RenderedTriangles+=data.TriangleCount-oldTriangles;triangleCounts[key]=data.TriangleCount;
            if(patches.TryGetValue(key,out old)){if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}patches.Remove(key);}
            rings[key]=ring;
            if(data.TriangleCount==0){patches.Add(key,null);return;} // Cache clearings as well.
            EnsureMaterials();var go=new GameObject("Nivel 94 grass patch");go.transform.SetParent(region,false);go.transform.localPosition=new Vector3(x*TerrainGrass.PatchSize,0,z*TerrainGrass.PatchSize);
            var owner=go.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];patches.Add(key,owner);
            var mesh=MeshFactory.Create("Batched grass",data,go.transform,world.profile,owner,false,false,materials);
            var renderer=mesh.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=quality==TerrainGrassQuality.High;
            renderer.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.Off;
            // No collider, Light, reflection probe, pickup or per-blade GameObject.
        }
        private void EnsureMaterials()
        {
            if(materials!=null)return;
            Shader shader=Shader.Find("HDRP/Lit")??Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
            Material dark=Make(shader,new Color(.32f,.45f,.16f)),light=Make(shader,new Color(.43f,.54f,.24f));
            materials=new Material[6];for(int i=0;i<materials.Length;i++)materials[i]=dark;materials[(int)Surface.Plaster]=light;
        }
        private static Material Make(Shader shader,Color color)
        {
            var m=new Material(shader){name="Shared Nivel 94 grass",enableInstancing=true};
            foreach(string p in new[]{"_BaseColor","_Color"})if(m.HasProperty(p))m.SetColor(p,color);
            foreach(string p in new[]{"_Smoothness","_Glossiness"})if(m.HasProperty(p))m.SetFloat(p,.12f);
            foreach(string p in new[]{"_CullMode","_CullModeForward","_Cull"})if(m.HasProperty(p))m.SetFloat(p,0);
            if(m.HasProperty("_DoubleSidedEnable")){m.SetFloat("_DoubleSidedEnable",1);m.EnableKeyword("_DOUBLESIDED_ON");}
            if(m.HasProperty("_DoubleSidedConstants"))m.SetVector("_DoubleSidedConstants",new Vector4(-1,-1,-1,0));
            PoolroomsBiomeMaterialFactory.Validate(m);return m;
        }
        private void Clear()
        {
            foreach(var chunk in patches.Values)if(chunk!=null){chunk.gameObject.SetActive(false);Destroy(chunk.gameObject);}patches.Clear();rings.Clear();triangleCounts.Clear();RenderedTriangles=0;
        }
        private void OnDisable(){Clear();plan=null;region=null;if(job.IsCompleted)job.DiscardCompleted();}
        private void OnDestroy()
        {Clear();if(materials!=null){Destroy(materials[0]);Destroy(materials[(int)Surface.Plaster]);}materials=null;}
    }
}
