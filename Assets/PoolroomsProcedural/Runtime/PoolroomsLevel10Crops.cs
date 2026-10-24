using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    public static class PoolroomsLevel10Crops
    {
        public static void Attach(Level10CropPatch[] patches,Level10Plan plan,Transform parent,PoolroomsWorld world,PoolroomsProfile profile,PoolroomsChunk owner)
        {
            if(patches==null||plan==null)return;
            for(int index=0;index<patches.Length;index++)
            {
                if(patches[index].Meshes[0].TriangleCount==0)continue;
                var root=new GameObject("Level10 wheat patch "+index);root.transform.SetParent(parent,false);
                var levels=new LOD[3];MeshFilter detailed=null;Renderer detailedRenderer=null;
                for(int lod=0;lod<3;lod++)
                {
                    var go=MeshFactory.Create("Wheat geometry LOD "+lod,patches[index].Meshes[lod],root.transform,profile,owner,false);
                    if(go==null)continue;var renderer=go.GetComponent<MeshRenderer>();
                    renderer.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicGeometry;
                    renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
                    levels[lod]=new LOD(lod==0?.34f:lod==1?.10f:.002f,new Renderer[]{renderer});
                    if(lod==0){detailed=go.GetComponent<MeshFilter>();detailedRenderer=renderer;}
                }
                var group=root.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.SetLODs(levels);group.RecalculateBounds();
                var wind=root.AddComponent<PoolroomsLevel10CropWind>();wind.Configure(world,plan,detailed,detailedRenderer);
            }
        }
    }
    // Nearby visible patches only; two updates globally per frame and at most
    // eight updates/second/patch. No collider rebuild, new allocation or material
    // copy during steady motion. Far LODs stay static while retaining the field.
    public sealed class PoolroomsLevel10CropWind : MonoBehaviour
    {
        static int frame=-1,updates;
        PoolroomsWorld world;PoolroomsExplorer actor;Level10Plan plan;Mesh mesh;Renderer target;
        Vector3[] original,normals,vertices,bentNormals;
        float[] weights,derivatives,phaseSin,phaseCos;
        float next;
        public void Configure(PoolroomsWorld owner,Level10Plan descriptor,MeshFilter detailed,Renderer renderer)
        {world=owner;plan=descriptor;mesh=detailed!=null?detailed.sharedMesh:null;target=renderer;actor=world!=null&&world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;}
        void Prepare()
        {
            original=mesh.vertices;normals=mesh.normals;vertices=new Vector3[original.Length];bentNormals=new Vector3[original.Length];
            weights=new float[original.Length];derivatives=new float[original.Length];phaseSin=new float[original.Length];phaseCos=new float[original.Length];
            for(int i=0;i<original.Length;i++)
            {
                Vector3 v=original[i];float root=Level10Terrain.MeshHeight(plan,v.x,v.z),h=Mathf.Clamp01((v.y-root)/1.6f);
                weights[i]=h*h;derivatives[i]=2*h/1.6f;
                float phase=v.x*.16f+v.z*.09f+plan.Seed%67;
                phaseSin[i]=Mathf.Sin(phase);phaseCos[i]=Mathf.Cos(phase);
            }
            mesh.MarkDynamic();var bounds=mesh.bounds;bounds.Expand(.24f);mesh.bounds=bounds;
        }
        void LateUpdate()
        {
            if(mesh==null||target==null||world==null||world.viewer==null||plan==null||!target.isVisible||!target.enabled||
                actor!=null&&(actor.IsPaused||!actor.IsReady)||Time.unscaledTime<next)return;
            if((target.bounds.ClosestPoint(world.viewer.position)-world.viewer.position).sqrMagnitude>24*24)return;
            if(frame!=Time.frameCount){frame=Time.frameCount;updates=0;}if(updates>=2)return;
            updates++;next=Time.unscaledTime+.125f;if(original==null)Prepare();
            float sine=Mathf.Sin(Time.time*.7f),cosine=Mathf.Cos(Time.time*.7f);
            for(int i=0;i<vertices.Length;i++)
            {
                float wind=(sine*phaseCos[i]+cosine*phaseSin[i])*.075f;
                Vector3 v=original[i];v.x+=wind*weights[i];vertices[i]=v;
                Vector3 n=normals[i];n.y-=wind*derivatives[i]*n.x;bentNormals[i]=n.normalized;
            }
            mesh.vertices=vertices;mesh.normals=bentNormals;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetQuota(){frame=-1;updates=0;}
    }
}
