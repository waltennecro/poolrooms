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
        public string Status {get;private set;}="Apariencia neutral";
        public string Attribution {get;private set;}="Silueta neutral original.";
        public bool Loading {get;private set;}
        public string Folder {get{return Path.Combine(Application.persistentDataPath,"Avatars");}}
        private PoolroomsExplorer explorer;
        private GameObject modelRoot;
        private Renderer[] renderers=Array.Empty<Renderer>();
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private bool visible,hidden,visibilityValid;
        private float targetYaw;
        public float Yaw {get{return targetYaw;}}
        private Animator animator;
        private bool hasSpeed,hasGrounded,hasSliding;
        private Transform leftLeg,rightLeg,leftArm,rightArm;
        private float phase;
        private int generation;
        private void Awake(){explorer=GetComponent<PoolroomsExplorer>();targetYaw=PlayerPrefs.GetFloat("Poolrooms.Avatar.Yaw",0);CreateNeutral();}
        private void Start()
        {
            string saved=PlayerPrefs.GetString("Poolrooms.Avatar.Selection","");
            if(saved.StartsWith("obj:",StringComparison.Ordinal))LoadObj(saved.Substring(4));
            else if(saved.StartsWith("asset:",StringComparison.Ordinal))foreach(var skin in Definitions())if(skin.name==saved.Substring(6)){Apply(skin);break;}
        }
        public PoolroomsAvatarDefinition[] Definitions()
        {var all=Resources.LoadAll<PoolroomsAvatarDefinition>("PoolroomsAvatars");Array.Sort(all,(a,b)=>string.CompareOrdinal(a.name,b.name));return all;}
        public string[] ObjFiles()
        {
            try{Directory.CreateDirectory(Folder);var files=Directory.GetFiles(Folder,"*.obj",SearchOption.TopDirectoryOnly);Array.Sort(files,StringComparer.OrdinalIgnoreCase);if(files.Length>64)Array.Resize(ref files,64);return files;}
            catch(Exception e){Status=e.Message;return Array.Empty<string>();}
        }
        public void SetVisible(bool value,bool completelyHidden)
        {
            if(visibilityValid&&visible==value&&hidden==completelyHidden)return;
            visible=value;hidden=completelyHidden;
            foreach(var r in renderers)if(r!=null){r.forceRenderingOff=hidden;r.shadowCastingMode=visible?ShadowCastingMode.On:ShadowCastingMode.ShadowsOnly;}
            visibilityValid=true;
        }
        public void ResetAppearance(){generation++;Loading=false;CreateNeutral();PlayerPrefs.DeleteKey("Poolrooms.Avatar.Selection");PlayerPrefs.Save();}
        public bool Apply(PoolroomsAvatarDefinition skin)
        {
            if(skin==null||skin.model==null){Status="Selecciona un modelo 3D válido.";return false;}
            GameObject candidate=null;
            try
            {
                candidate=new GameObject("Player avatar / "+skin.name);candidate.SetActive(false);candidate.transform.SetParent(transform,false);
                var instance=Instantiate(skin.model,candidate.transform);Sanitize(instance);
                if(!Normalize(candidate,skin.height,skin.forwardCorrection))throw new FormatException("El modelo no tiene geometría o altura válida.");
                generation++;Loading=false;Replace(candidate);candidate=null;
                animator=modelRoot.GetComponentInChildren<Animator>();
                if(animator!=null)
                {
                    animator.applyRootMotion=false;if(skin.controller!=null)animator.runtimeAnimatorController=skin.controller;
                    hasSpeed=hasGrounded=hasSliding=false;
                    foreach(var p in animator.parameters){hasSpeed|=p.name=="Speed"&&p.type==AnimatorControllerParameterType.Float;hasGrounded|=p.name=="Grounded"&&p.type==AnimatorControllerParameterType.Bool;hasSliding|=p.name=="Sliding"&&p.type==AnimatorControllerParameterType.Bool;}
                }
                Status=skin.name;Attribution=skin.attribution??"";PlayerPrefs.SetString("Poolrooms.Avatar.Selection","asset:"+skin.name);PlayerPrefs.Save();return true;
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
            foreach(var animation in instance.GetComponentsInChildren<Animator>(true)){animation.applyRootMotion=false;animation.updateMode=AnimatorUpdateMode.Normal;}
        }
        static bool Normalize(GameObject candidate,float height,float yaw)
        {
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
            wrapper.transform.localScale=Vector3.one*scale;wrapper.transform.localRotation=Quaternion.Euler(0,yaw,0);return true;
        }
        public void LoadObj(string file)
        {
            if(Loading)return;
            try
            {
                string path=Path.GetFullPath(file);string directory=Path.GetFullPath(Folder);
                if(!string.Equals(Path.GetDirectoryName(path),directory,StringComparison.OrdinalIgnoreCase)||!string.Equals(Path.GetExtension(path),".obj",StringComparison.OrdinalIgnoreCase))throw new FormatException("Copia el OBJ a la carpeta Avatars del juego.");
                if(!File.Exists(path)||new FileInfo(path).Length>AvatarObj.MaxBytes)throw new FormatException("OBJ ausente o mayor de 16 MB.");
                StartCoroutine(ImportObj(path,++generation));
            }
            catch(Exception e){Status=e.Message;}
        }
        private IEnumerator ImportObj(string path,int request)
        {
            Loading=true;Status="Leyendo OBJ…";
            var task=Task.Run(()=>AvatarObj.Parse(File.ReadAllText(path)));
            while(!task.IsCompleted)yield return null;
            if(request!=generation)yield break;
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
                candidate=new GameObject("Player OBJ / "+Path.GetFileNameWithoutExtension(path));candidate.transform.SetParent(transform,false);
                candidate.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=candidate.AddComponent<MeshRenderer>();
                renderer.sharedMaterials=AvatarObjMaterials.Create(data,path,resources);
                // Convert OBJ handedness by reflecting Z and reversing winding together.
                if(!Normalize(candidate,1.75f,0))throw new FormatException("El OBJ no tiene altura válida.");
                Replace(candidate);candidate=null;owned.AddRange(resources);resources.Clear();
                Status=Path.GetFileName(path)+" · OBJ estático";Attribution="Modelo local proporcionado por el jugador.";PlayerPrefs.SetString("Poolrooms.Avatar.Selection","obj:"+path);PlayerPrefs.Save();
            }
            catch(Exception e){if(candidate!=null)Destroy(candidate);Status="No se pudo importar: "+e.Message;}
            finally{foreach(var resource in resources)if(resource!=null)Destroy(resource);Loading=false;}
        }
        public void SetYaw(float degrees){targetYaw=Mathf.Repeat(GamePreferences.Clamp(degrees,0,360,0),360);PlayerPrefs.SetFloat("Poolrooms.Avatar.Yaw",targetYaw);if(modelRoot!=null)modelRoot.transform.localRotation=Quaternion.Euler(0,targetYaw,0);}
        private void Replace(GameObject candidate)
        {
            if(modelRoot!=null){modelRoot.SetActive(false);Destroy(modelRoot);}foreach(var resource in owned)if(resource!=null)Destroy(resource);owned.Clear();
            modelRoot=candidate;modelRoot.transform.localRotation=Quaternion.Euler(0,targetYaw,0);modelRoot.SetActive(true);animator=null;leftLeg=rightLeg=leftArm=rightArm=null;
            renderers=modelRoot.GetComponentsInChildren<Renderer>(true);
            visibilityValid=false;
            foreach(var r in renderers){r.motionVectorGenerationMode=MotionVectorGenerationMode.Object;r.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;}
            SetVisible(visible,hidden);
        }
        private void CreateNeutral()
        {
            var root=new GameObject("Neutral player silhouette");root.transform.SetParent(transform,false);var material=AvatarObjMaterials.Material("Neutral player",new Color(.38f,.43f,.46f));
            Replace(root);owned.Add(material);
            Part("Torso",root.transform,new Vector3(0,1.05f,0),new Vector3(.42f,.34f,.24f),material);
            Part("Head",root.transform,new Vector3(0,1.55f,0),new Vector3(.24f,.13f,.24f),material);
            leftLeg=Limb("Left leg",root.transform,new Vector3(-.12f,.76f,0),new Vector3(.18f,.38f,.18f),material);
            rightLeg=Limb("Right leg",root.transform,new Vector3(.12f,.76f,0),new Vector3(.18f,.38f,.18f),material);
            leftArm=Limb("Left arm",root.transform,new Vector3(-.28f,1.34f,0),new Vector3(.14f,.3f,.14f),material);
            rightArm=Limb("Right arm",root.transform,new Vector3(.28f,1.34f,0),new Vector3(.14f,.3f,.14f),material);
            renderers=modelRoot.GetComponentsInChildren<Renderer>();
            visibilityValid=false;
            foreach(var r in renderers){r.motionVectorGenerationMode=MotionVectorGenerationMode.Object;r.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;}
            SetVisible(visible,hidden);Status="Apariencia neutral";Attribution="Silueta neutral original.";
        }
        static Transform Limb(string name,Transform root,Vector3 position,Vector3 size,Material material)
        {var pivot=new GameObject(name).transform;pivot.SetParent(root,false);pivot.localPosition=position;Part(name+" mesh",pivot,Vector3.down*size.y,size,material);return pivot;}
        static void Part(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {var part=GameObject.CreatePrimitive(PrimitiveType.Capsule);part.name=name;part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=size;var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);part.GetComponent<Renderer>().sharedMaterial=material;}
        private void LateUpdate()
        {
            if(explorer==null||explorer.IsPaused||Time.deltaTime<=0)return;float speed=explorer.ViewVelocity.magnitude;
            if(modelRoot!=null)modelRoot.transform.localScale=new Vector3(1,explorer.IsAquaticRiding?.45f:explorer.IsSliding?.55f:1,1);
            if(animator!=null&&animator.runtimeAnimatorController!=null){if(hasSpeed)animator.SetFloat("Speed",speed);if(hasGrounded)animator.SetBool("Grounded",explorer.ViewGrounded);if(hasSliding)animator.SetBool("Sliding",explorer.IsSliding);}
            if(leftLeg==null)return;
            phase+=Time.deltaTime*speed*3;float angle=Mathf.Sin(phase)*Mathf.Min(speed*7,28);
            leftLeg.localRotation=Quaternion.Euler(angle,0,0);rightLeg.localRotation=Quaternion.Euler(-angle,0,0);leftArm.localRotation=Quaternion.Euler(-angle*.6f,0,0);rightArm.localRotation=Quaternion.Euler(angle*.6f,0,0);
        }
        private void OnDisable(){generation++;Loading=false;}
        private void OnDestroy(){foreach(var resource in owned)if(resource!=null)Destroy(resource);if(modelRoot!=null)Destroy(modelRoot);}
    }
}
