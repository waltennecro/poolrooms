using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    // Models are standard Unity OBJ imports; original glTF files, licenses and adaptation
    // reports are preserved in the repository. No runtime glTF parser/plugin is required.
    public static class City11Sources
    {
        [Serializable] public sealed class Definition
        {public string id,resource;public float[] minimum,maximum;public MaterialDefinition[] materials;public LODDefinition[] lods;public WheelDefinition[] wheels;}
        [Serializable] public sealed class WheelDefinition{public string part;public float[] center;public float radius;public bool front;}
        [Serializable] public sealed class LODDefinition{public string resource;public int triangles;}
        [Serializable] public sealed class MaterialDefinition
        {public string name,albedo,normal,emissive,mask,workflow,specular;public float[] colour,emission;public float normalScale=1,metallic,roughness=.5f,clearcoat;public bool transparent,doubleSided,alphaClip;public float alphaCutoff=.5f;public string[] extensions;}
        private static readonly Dictionary<string,Definition> definitions=new Dictionary<string,Definition>();
        private static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        public static Definition Get(string id)
        {
            if(definitions.TryGetValue(id,out var value))return value;
            var data=Resources.Load<TextAsset>((id.StartsWith("G_",StringComparison.Ordinal)?"GardenSources/":id.StartsWith("B_Building",StringComparison.Ordinal)?"Buildings/":id.StartsWith("B_",StringComparison.Ordinal)?"Bistro/":id.StartsWith("U_",StringComparison.Ordinal)?"Urban/":id.StartsWith("A_",StringComparison.Ordinal)?"Aircraft/":id.StartsWith("H_",StringComparison.Ordinal)?"Helicopters/":id.StartsWith("I_",StringComparison.Ordinal)?"Interiors/":"City11/")+id);
            if(data==null)return null;value=JsonUtility.FromJson<Definition>(data.text);definitions.Add(id,value);return value;
        }
        public static GameObject Create(string id,Transform parent,Vector3 position,float targetLongest,out Vector3 size,int startLOD=0,bool preservePivot=false)
        {
            size=Vector3.zero;var definition=Get(id);if(definition==null)return null;
            string resource=startLOD>0&&definition.lods!=null&&definition.lods.Length>0?definition.lods[0].resource:definition.resource;
            var source=Resources.Load<GameObject>(resource);if(source==null)return null;
            var root=new GameObject(id+" / licensed source");root.transform.SetParent(parent,false);root.transform.localPosition=position;
            var mesh=UnityEngine.Object.Instantiate(source,root.transform);Bounds bounds=new Bounds();bool found=false;
            foreach(var filter in mesh.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;var local=filter.sharedMesh.bounds;Matrix4x4 matrix=Relative(filter.transform,root.transform);
                for(int corner=0;corner<8;corner++)
                {
                    Vector3 p=matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
                }
            }
            if(!found){UnityEngine.Object.Destroy(root);return null;}
            float scale=targetLongest/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            if(!float.IsFinite(scale)||scale<=0){UnityEngine.Object.Destroy(root);return null;}size=bounds.size*scale;
            mesh.transform.localPosition=(mesh.transform.localPosition-(preservePivot?Vector3.zero:bounds.center))*scale;mesh.transform.localScale*=scale;
            if(definition.lods!=null&&definition.lods.Length>0)
            {
                var tiers=new List<LOD>{new LOD(.12f,mesh.GetComponentsInChildren<Renderer>())};
                for(int i=startLOD;i<definition.lods.Length;i++)
                {
                    var lowSource=Resources.Load<GameObject>(definition.lods[i].resource);if(lowSource==null)continue;
                    var low=UnityEngine.Object.Instantiate(lowSource,root.transform);low.transform.localPosition=mesh.transform.localPosition;low.transform.localRotation=mesh.transform.localRotation;low.transform.localScale=mesh.transform.localScale;
                    tiers.Add(new LOD(i==0?.045f:.008f,low.GetComponentsInChildren<Renderer>()));
                }
                var group=root.AddComponent<LODGroup>();group.SetLODs(tiers.ToArray());group.RecalculateBounds();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;
            }
            if(id.StartsWith("I_",StringComparison.Ordinal)&&root.GetComponent<LODGroup>()==null)
            {
                // These already have 4k/10k triangles. Cull small distant furniture
                // instead of retaining a renderer for every loaded distant room.
                var group=root.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.018f,mesh.GetComponentsInChildren<Renderer>())});group.RecalculateBounds();
            }
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                var imported=renderer.sharedMaterials;var assigned=new Material[imported.Length];
                for(int i=0;i<assigned.Length;i++)
                {
                    string materialName=imported[i]!=null?imported[i].name:renderer.gameObject.name;int index=-1;
                    for(int n=0;n<definition.materials.Length;n++)if(materialName.Contains(definition.materials[n].name)||renderer.gameObject.name.Contains(definition.materials[n].name)){index=n;break;}
                    if(index<0&&definition.materials.Length==1)index=0;
                    if(index<0){UnityEngine.Object.Destroy(root);Debug.LogError("City11 source material mapping failed for "+id+" / "+materialName);return null;}
                    assigned[i]=MaterialFor(definition.materials[index]);
                }
                renderer.sharedMaterials=assigned;renderer.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
                renderer.motionVectorGenerationMode=MotionVectorGenerationMode.Object;
            }
            return root;
        }
        public static Vector3 ModelPoint(Transform node,Transform root,Vector3 point){return Relative(node,root).MultiplyPoint3x4(point);}
        public static bool TryLocalBounds(Transform root,out Bounds bounds)
        {
            bounds=new Bounds();bool found=false;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;var local=filter.sharedMesh.bounds;Matrix4x4 matrix=Relative(filter.transform,root);
                for(int corner=0;corner<8;corner++)
                {Vector3 p=matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);}
            }
            return found;
        }
        static Vector3 Vec(float[] p){return new Vector3(p[0],p[1],p[2]);}
        static Matrix4x4 Relative(Transform node,Transform root)
        {return node==root?Matrix4x4.identity:Relative(node.parent,root)*Matrix4x4.TRS(node.localPosition,node.localRotation,node.localScale);}
        static Material MaterialFor(MaterialDefinition d)
        {
            if(materials.TryGetValue(d.name,out var cached)&&cached!=null)return cached;
            bool specularWorkflow=d.workflow=="specular";
            var shader=Shader.Find("HDRP/Lit")??Shader.Find("Universal Render Pipeline/Lit")??Shader.Find(specularWorkflow?"Standard (Specular setup)":"Standard");
            var m=new Material(shader){name="Liminal licensed source / "+d.name,enableInstancing=true};
            Color colour=d.colour!=null&&d.colour.Length==4?new Color(d.colour[0],d.colour[1],d.colour[2],d.colour[3]).gamma:Color.white;
            // Specular anti-aliasing filters unresolved normals without changing texture,
            // mesh LOD, reflection sample counts or the streaming distance.
            if(m.HasProperty("_EnableGeometricSpecularAA"))
            {
                Float(m,"_EnableGeometricSpecularAA",1);Float(m,"_SpecularAAScreenSpaceVariance",.1f);Float(m,"_SpecularAAThreshold",.2f);
                m.EnableKeyword("_ENABLE_GEOMETRIC_SPECULAR_AA");
            }
            Set(m,"_BaseColor",colour);Set(m,"_Color",colour);
            foreach(string property in new[]{"_BaseColorMap","_BaseMap","_MainTex"})Texture(m,property,d.albedo);
            bool masked=!string.IsNullOrEmpty(d.mask);Texture(m,"_MaskMap",d.mask);if(masked)m.EnableKeyword("_MASKMAP");
            Float(m,"_Metallic",d.metallic);Float(m,"_Smoothness",1-d.roughness);Float(m,"_Glossiness",1-d.roughness);Float(m,"_SmoothnessRemapMin",0);Float(m,"_SmoothnessRemapMax",masked?1:1-d.roughness);Float(m,"_MetallicRemapMin",0);Float(m,"_MetallicRemapMax",masked?1:d.metallic);
            if(specularWorkflow)
            {
                // Bistro authored dielectric F0 is preserved, rather than forcing
                // every fabric/wood surface to the standard metallic F0 value.
                if(shader.name=="HDRP/Lit")
                {
                    Float(m,"_MaterialID",4);Set(m,"_SpecularColor",Color.white);Texture(m,"_SpecularColorMap",d.specular);
                    m.EnableKeyword("_MATERIAL_FEATURE_SPECULAR_COLOR");m.EnableKeyword("_SPECULARCOLORMAP");
                }
                else
                {
                    Float(m,"_WorkflowMode",0);Set(m,"_SpecColor",Color.white);Texture(m,"_SpecGlossMap",d.specular);
                    Float(m,"_Smoothness",1);Float(m,"_GlossMapScale",1);
                    m.EnableKeyword("_SPECULAR_SETUP");m.EnableKeyword("_SPECGLOSSMAP");m.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
            }
            if(d.clearcoat>0&&m.HasProperty("_CoatMask")){Float(m,"_CoatMask",Mathf.Clamp01(d.clearcoat));m.EnableKeyword("_MATERIAL_FEATURE_CLEAR_COAT");}
            if(!string.IsNullOrEmpty(d.normal))
            {Texture(m,"_NormalMap",d.normal);Texture(m,"_BumpMap",d.normal);Float(m,"_NormalScale",d.normalScale);Float(m,"_BumpScale",d.normalScale);m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_NORMALMAP_TANGENT_SPACE");}
            if(d.emission!=null&&d.emission.Length==3)
            {Set(m,"_EmissiveColor",new Color(d.emission[0],d.emission[1],d.emission[2]));Texture(m,"_EmissiveColorMap",d.emissive);Float(m,"_UseEmissiveIntensity",0);if(!string.IsNullOrEmpty(d.emissive))m.EnableKeyword("_EMISSIVE_COLOR_MAP");}
            if(d.alphaClip)
            {
                Float(m,"_AlphaCutoffEnable",1);Float(m,"_AlphaClip",1);Float(m,"_Cutoff",d.alphaCutoff);Float(m,"_AlphaCutoff",d.alphaCutoff);Float(m,"_AlphaCutoffShadow",d.alphaCutoff);
                m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=(int)RenderQueue.AlphaTest;
                if(d.doubleSided){Float(m,"_DoubleSidedNormalMode",1);Float(m,"_Cull",0);m.EnableKeyword("_DOUBLESIDED_ON");}
            }
            bool transmission=d.extensions!=null&&Array.IndexOf(d.extensions,"KHR_materials_transmission")>=0;
            if(transmission||d.transparent)
            {colour.a=transmission?.16f:colour.a;Set(m,"_BaseColor",colour);Set(m,"_Color",colour);Float(m,"_SurfaceType",1);Float(m,"_BlendMode",0);Float(m,"_ZWrite",0);Float(m,"_TransparentZWrite",0);m.renderQueue=3000;m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");}
            if(d.doubleSided){Float(m,"_DoubleSidedEnable",1);Float(m,"_CullMode",0);Float(m,"_CullModeForward",0);}
            PoolroomsBiomeMaterialFactory.Validate(m);materials.Add(d.name,m);return m;
        }
        static void Set(Material m,string name,Color value){if(m.HasProperty(name))m.SetColor(name,value);}
        static void Float(Material m,string name,float value){if(m.HasProperty(name))m.SetFloat(name,value);}
        static void Texture(Material m,string name,string resource){if(m.HasProperty(name)&&!string.IsNullOrEmpty(resource))m.SetTexture(name,Resources.Load<Texture2D>(resource));}
    }
}
