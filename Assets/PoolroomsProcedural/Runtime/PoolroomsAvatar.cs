using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsAvatar : MonoBehaviour
    {
        public string Status {get;private set;}="Sin skin · importa tu humanoide";
        public string Attribution {get;private set;}="";
        public bool Loading {get;private set;}
        public SkinPreferences Preferences {get;private set;}=new SkinPreferences();
        public string Folder {get{return Path.Combine(Application.persistentDataPath,"Avatars");}}
        public static Func<string,PoolroomsAvatarDefinition> ProjectImporter;
        private PoolroomsExplorer explorer;
        private PoolroomsGameSettings settings;
        private PoolroomsHumanoidSkin humanoid;
        private CharacterController body;
        private GameObject modelRoot;
        private Renderer[] renderers=Array.Empty<Renderer>();
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private readonly Dictionary<Material,Material> materialCopies=new Dictionary<Material,Material>();
        private IDisposable imported;
        private bool visible,hidden,visibilityValid;
        private float targetYaw;
        private int generation;
        private bool subscribed,dirty,renderingCached,lastRt,lastShadows,lastOverride;
        private float saveAt,lastMetal,lastSmooth;
        public float Yaw {get{return targetYaw;}}
        public string AnimationState {get{return humanoid!=null?humanoid.Motion.State:"Sin animación";}}
        private void Awake()
        {
            explorer=GetComponent<PoolroomsExplorer>();body=GetComponent<CharacterController>();settings=GetComponent<PoolroomsGameSettings>();
            targetYaw=PlayerPrefs.GetFloat("Poolrooms.Avatar.Yaw",0);
            try{Preferences=JsonUtility.FromJson<SkinPreferences>(PlayerPrefs.GetString("Poolrooms.Avatar.Animation",""))??new SkinPreferences();}catch{Preferences=new SkinPreferences();}
            Preferences.Sanitize();
        }
        private void HookSettings()
        {if(settings==null)settings=GetComponent<PoolroomsGameSettings>();if(settings!=null&&!subscribed){settings.Changed+=ApplyRendering;subscribed=true;}}
        private void OnEnable(){HookSettings();}
        private void Start(){Rebuild();}
        public void SavePreferences(){Preferences.Sanitize();dirty=true;saveAt=Time.unscaledTime+.6f;ApplyRendering();}
        private void Flush(){if(!dirty)return;PlayerPrefs.SetString("Poolrooms.Avatar.Animation",JsonUtility.ToJson(Preferences));PlayerPrefs.Save();dirty=false;}
        private void Update(){if(settings==null)HookSettings();if(dirty&&Time.unscaledTime>=saveAt)Flush();}
        public void ResetSettings(){Preferences=new SkinPreferences();targetYaw=0;PlayerPrefs.SetFloat("Poolrooms.Avatar.Yaw",0);SavePreferences();Rebuild();}
        public void Rebuild()
        {
            if(Loading)return;SavePreferences();string saved=PlayerPrefs.GetString("Poolrooms.Avatar.Selection","");
            if(saved.StartsWith("obj:",StringComparison.Ordinal))LoadFile(saved.Substring(4));
            else if(saved.StartsWith("file:",StringComparison.Ordinal))LoadFile(saved.Substring(5));
            else if(saved.StartsWith("asset:",StringComparison.Ordinal))foreach(var skin in Definitions())if(skin.name==saved.Substring(6)){Apply(skin);break;}
        }
        public PoolroomsAvatarDefinition[] Definitions()
        {var all=Resources.LoadAll<PoolroomsAvatarDefinition>("PoolroomsAvatars");Array.Sort(all,(a,b)=>string.CompareOrdinal(a.name,b.name));return all;}
        public string[] ObjFiles()
        {
            try
            {
                Directory.CreateDirectory(Folder);var files=new List<string>();foreach(var file in Directory.GetFiles(Folder))
                {string ext=Path.GetExtension(file).ToLowerInvariant();if(ext==".obj"||ext==".glb"||ext==".gltf")files.Add(file);}
                files.Sort(StringComparer.OrdinalIgnoreCase);if(files.Count>64)files.RemoveRange(64,files.Count-64);return files.ToArray();
            }
            catch(Exception e){Status=e.Message;return Array.Empty<string>();}
        }
        public void SetVisible(bool value,bool completelyHidden)
        {
            if(visibilityValid&&visible==value&&hidden==completelyHidden)return;visible=value;hidden=completelyHidden;
            foreach(var r in renderers)if(r!=null)
            {r.forceRenderingOff=hidden||!visible&&!Preferences.castShadows;r.shadowCastingMode=Preferences.castShadows?(visible?ShadowCastingMode.On:ShadowCastingMode.ShadowsOnly):ShadowCastingMode.Off;}
            visibilityValid=true;
        }
        public void ResetAppearance()
        {generation++;Loading=false;Clear();Status="Sin skin · importa tu humanoide";Attribution="";PlayerPrefs.DeleteKey("Poolrooms.Avatar.Selection");PlayerPrefs.Save();}
        public bool Apply(PoolroomsAvatarDefinition skin)
        {
            if(skin==null||skin.model==null){Status="Selecciona un modelo 3D válido.";return false;}
            GameObject candidate=null;
            try
            {
                candidate=new GameObject("Player avatar / "+skin.name);candidate.SetActive(false);candidate.transform.SetParent(transform,false);
                var instance=Instantiate(skin.model,candidate.transform);Sanitize(instance);
                if(!Normalize(candidate,skin.height,skin.forwardCorrection+targetYaw,Preferences.zUp))throw new FormatException("El modelo no tiene geometría o altura válida.");
                if(skin.sharedHumanoid)candidate.AddComponent<PoolroomsHumanoidSkin>().Build(Preferences,skin.height);
                generation++;Loading=false;Replace(candidate);candidate=null;
                Status=skin.name+" · "+(humanoid!=null?humanoid.Report:"Modelo estático");Attribution=skin.attribution??"";
                PlayerPrefs.SetString("Poolrooms.Avatar.Selection","asset:"+skin.name);PlayerPrefs.Save();return true;
            }
            catch(Exception e){if(candidate!=null)Destroy(candidate);Status=e.Message;return false;}
        }
        static void Sanitize(GameObject instance)
        {
            foreach(var collider in instance.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
            foreach(var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            foreach(var light in instance.GetComponentsInChildren<Light>(true))light.enabled=false;
            foreach(var camera in instance.GetComponentsInChildren<Camera>(true))camera.enabled=false;
            foreach(var audio in instance.GetComponentsInChildren<AudioSource>(true))audio.enabled=false;
            foreach(var listener in instance.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;
            foreach(var particles in instance.GetComponentsInChildren<ParticleSystem>(true))particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach(var animation in instance.GetComponentsInChildren<Animator>(true)){animation.applyRootMotion=false;animation.enabled=false;}
            foreach(var animation in instance.GetComponentsInChildren<Animation>(true))animation.enabled=false;
        }
        static bool Normalize(GameObject candidate,float height,float yaw,bool zUp=false)
        {
            var orientation=new GameObject("Source orientation").transform;orientation.SetParent(candidate.transform,false);
            var original=new List<Transform>();foreach(Transform child in candidate.transform)if(child!=orientation)original.Add(child);
            foreach(var child in original)child.SetParent(orientation,false);
            orientation.localRotation=Quaternion.Euler(zUp?-90:0,yaw,0);
            var renderers=candidate.GetComponentsInChildren<Renderer>(true);bool found=false;var bounds=new Bounds();
            foreach(var r in renderers)if(r is MeshRenderer||r is SkinnedMeshRenderer)
            {
                var b=r.localBounds;
                for(int corner=0;corner<8;corner++)
                {
                    Vector3 point=candidate.transform.InverseTransformPoint(r.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                }
            }
            if(!found||!float.IsFinite(bounds.size.y)||bounds.size.y<.0001f)return false;
            Vector3 pivot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            Vector3 local=pivot;
            var wrapper=new GameObject("Normalized model");wrapper.transform.SetParent(candidate.transform,false);
            var children=new List<Transform>();foreach(Transform t in candidate.transform)if(t!=wrapper.transform)children.Add(t);
            foreach(var child in children)child.SetParent(wrapper.transform,true);
            float scale=GamePreferences.Clamp(height,.5f,2.5f,1.75f)/bounds.size.y;
            foreach(Transform child in wrapper.transform)child.localPosition-=local;
            wrapper.transform.localScale=Vector3.one*scale;return true;
        }
        public void LoadObj(string file){LoadFile(file);}
        public void LoadFile(string file)
        {
            if(Loading)return;
            try
            {
                string path=Path.GetFullPath(file.Trim().Trim('"'));if(!File.Exists(path))throw new FileNotFoundException("No existe el modelo seleccionado.");
                string ext=Path.GetExtension(path).ToLowerInvariant();
                if(ext==".obj"){if(new FileInfo(path).Length>AvatarObj.MaxBytes)throw new FormatException("OBJ mayor de 16 MB.");StartCoroutine(ImportObj(path,++generation));}
                else if(ext==".glb"||ext==".gltf")
                {
                    if(!AvatarGltf.Available)throw new FormatException("Instala Unity glTFast para importar GLB/glTF. OBJ funciona sin extensiones.");
                    if(new FileInfo(path).Length>64*1024*1024)throw new FormatException("GLB/glTF mayor de 64 MB.");StartCoroutine(ImportGltf(path,++generation));
                }
                else if(ProjectImporter!=null){var definition=ProjectImporter(path);if(definition!=null)Apply(definition);}
                else throw new FormatException("FBX, DAE, 3DS, DXF y BLEND se preparan desde Unity. En el juego importa OBJ o GLB/glTF con glTFast instalado.");
            }
            catch(Exception e){Status=e.GetBaseException().Message;}
        }
        private IEnumerator ImportObj(string path,int request)
        {
            Loading=true;Status="Leyendo OBJ…";var task=Task.Run(()=>AvatarObj.Parse(File.ReadAllText(path)));
            while(!task.IsCompleted)yield return null;if(request!=generation)yield break;
            GameObject candidate=null;var resources=new List<UnityEngine.Object>();
            try
            {
                if(task.IsFaulted)throw task.Exception.GetBaseException();var data=task.Result;
                var mesh=new Mesh{name="Player OBJ",indexFormat=data.Positions.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};resources.Add(mesh);
                var positions=new List<Vector3>(data.Positions.Count);var normals=new List<Vector3>(data.Positions.Count);var uv=new List<Vector2>(data.Positions.Count);
                foreach(var v in data.Positions)positions.Add(new Vector3(v.X,v.Y,-v.Z));foreach(var n in data.Normals)normals.Add(new Vector3(n.X,n.Y,-n.Z));foreach(var t in data.UV)uv.Add(new Vector2(t.X,t.Y));
                mesh.SetVertices(positions);mesh.SetUVs(0,uv);mesh.subMeshCount=data.Submeshes.Count;
                for(int i=0;i<data.Submeshes.Count;i++)
                {var indices=data.Submeshes[i];for(int t=0;t<indices.Count;t+=3){int b=indices[t+1];indices[t+1]=indices[t+2];indices[t+2]=b;}mesh.SetTriangles(indices,i);}
                if(data.MissingNormals)mesh.RecalculateNormals();else mesh.SetNormals(normals);mesh.RecalculateBounds();mesh.RecalculateTangents();
                candidate=new GameObject("Player OBJ / "+Path.GetFileNameWithoutExtension(path));candidate.SetActive(false);candidate.transform.SetParent(transform,false);
                var geometry=new GameObject("OBJ mesh");geometry.transform.SetParent(candidate.transform,false);
                geometry.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=geometry.AddComponent<MeshRenderer>();
                renderer.sharedMaterials=AvatarObjMaterials.Create(data,path,resources);
                // Convert OBJ handedness by reflecting Z and reversing winding together.
                if(!Normalize(candidate,1.75f,targetYaw,Preferences.zUp))throw new FormatException("El OBJ no tiene altura válida.");
                candidate.AddComponent<PoolroomsHumanoidSkin>().Build(Preferences,1.75f);
                Replace(candidate);candidate=null;owned.AddRange(resources);resources.Clear();
                Status=Path.GetFileName(path)+" · "+humanoid.Report;Attribution="Modelo local proporcionado por el jugador.";
                PlayerPrefs.SetString("Poolrooms.Avatar.Selection","file:"+path);PlayerPrefs.Save();
            }
            catch(Exception e){if(candidate!=null)Destroy(candidate);Status="No se pudo adaptar: "+e.GetBaseException().Message;}
            finally{foreach(var resource in resources)if(resource!=null)Destroy(resource);Loading=false;}
        }
        private IEnumerator ImportGltf(string path,int request)
        {
            Loading=true;Status="Leyendo glTF…";var candidate=new GameObject("Player glTF");candidate.SetActive(false);candidate.transform.SetParent(transform,false);
            AvatarGltf loader=null;Task<bool> task=null;Exception failure=null;
            try{loader=new AvatarGltf();task=loader.Load(path,candidate.transform);}catch(Exception e){failure=e;}
            if(task!=null)while(!task.IsCompleted)yield return null;
            try
            {
                if(request!=generation)throw new OperationCanceledException();
                if(failure!=null)throw failure;if(task.IsFaulted)throw task.Exception.GetBaseException();if(!task.Result)throw new FormatException("glTFast no pudo leer el modelo. Revisa el archivo y sus texturas.");
                Sanitize(candidate);if(!Normalize(candidate,1.75f,targetYaw,Preferences.zUp))throw new FormatException("Altura no válida.");
                candidate.AddComponent<PoolroomsHumanoidSkin>().Build(Preferences,1.75f);Replace(candidate);candidate=null;imported=loader;loader=null;
                Status=Path.GetFileName(path)+" · "+humanoid.Report;Attribution="Modelo local proporcionado por el jugador.";PlayerPrefs.SetString("Poolrooms.Avatar.Selection","file:"+path);PlayerPrefs.Save();
            }
            catch(OperationCanceledException){}catch(Exception e){Status=e.GetBaseException().Message;}
            finally{if(candidate!=null)Destroy(candidate);loader?.Dispose();if(request==generation)Loading=false;}
        }
        public void SetYaw(float degrees)
        {targetYaw=Mathf.Repeat(GamePreferences.Clamp(degrees,0,360,0),360);PlayerPrefs.SetFloat("Poolrooms.Avatar.Yaw",targetYaw);Status="Orientación guardada. Pulsa Recalcular adaptación para aplicarla al esqueleto.";}
        private void Clear()
        {
            if(modelRoot!=null){modelRoot.SetActive(false);Destroy(modelRoot);}modelRoot=null;humanoid=null;renderers=Array.Empty<Renderer>();
            foreach(var resource in owned)if(resource!=null)Destroy(resource);owned.Clear();materialCopies.Clear();renderingCached=false;imported?.Dispose();imported=null;
        }
        private void Replace(GameObject candidate)
        {
            Clear();modelRoot=candidate;modelRoot.SetActive(true);humanoid=modelRoot.GetComponent<PoolroomsHumanoidSkin>();renderers=modelRoot.GetComponentsInChildren<Renderer>(true);
            // Clone instance materials so quality/appearance controls never modify imported project assets.
            var clones=new Dictionary<Material,Material>();
            foreach(var renderer in renderers)
            {
                var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]!=null)
                {
                    var source=materials[i];if(!clones.TryGetValue(source,out var clone)){clone=new Material(source);owned.Add(clone);clones.Add(source,clone);materialCopies.Add(clone,source);}materials[i]=clone;
                }
                renderer.sharedMaterials=materials;
            }
            ApplyRendering();
        }
        private void ApplyRendering()
        {
            HookSettings();bool rt=settings!=null&&settings.RayTracingAvailable&&(int)settings.Preferences.graphicsQuality>=2;
            if(renderingCached&&lastRt==rt&&lastShadows==Preferences.castShadows&&lastOverride==Preferences.materialOverride&&lastMetal==Preferences.metallic&&lastSmooth==Preferences.smoothness)return;
            renderingCached=true;lastRt=rt;lastShadows=Preferences.castShadows;lastOverride=Preferences.materialOverride;lastMetal=Preferences.metallic;lastSmooth=Preferences.smoothness;
            foreach(var r in renderers)if(r!=null)
            {
                r.motionVectorGenerationMode=MotionVectorGenerationMode.Object;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;
                r.rayTracingMode=rt?(r is SkinnedMeshRenderer?UnityEngine.Experimental.Rendering.RayTracingMode.DynamicGeometry:UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform):UnityEngine.Experimental.Rendering.RayTracingMode.Off;
            }
            foreach(var pair in materialCopies)
            {
                var material=pair.Key;var source=pair.Value;if(material==null||source==null)continue;
                foreach(string property in scalarProperties)if(material.HasProperty(property))
                {
                    float value=source.HasProperty(property)?source.GetFloat(property):0;
                    if(Preferences.materialOverride)value=property=="_Metallic"?Preferences.metallic:Preferences.smoothness;
                    material.SetFloat(property,value);
                }
                PoolroomsBiomeMaterialFactory.Validate(material);
            }
            visibilityValid=false;SetVisible(visible,hidden);
        }
        static readonly string[] scalarProperties={"_Metallic","_Smoothness","_Glossiness","_SmoothnessRemapMin","_SmoothnessRemapMax"};
        public bool PlayEmote(HumanoidEmote emote)
        {return humanoid!=null&&!Loading&&explorer!=null&&!explorer.IsPaused&&!explorer.InputCaptured&&explorer.IsReady&&!explorer.IsSliding&&!explorer.IsDashing&&!explorer.IsFlying&&!explorer.IsAquaticRiding&&explorer.Vehicle==null&&humanoid.Motion.Play(emote,explorer.ViewGrounded||body!=null&&body.enabled&&body.isGrounded);}
        private void LateUpdate(){if(humanoid!=null)humanoid.Tick(explorer,Preferences);}
        private void OnDisable(){Flush();generation++;Loading=false;if(settings!=null&&subscribed)settings.Changed-=ApplyRendering;subscribed=false;}
        private void OnDestroy(){Flush();Clear();}
    }
}
