using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    [DefaultExecutionOrder(50),DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer),typeof(CharacterController))]
    public sealed class PoolroomsWings : MonoBehaviour
    {
        private const string PreferenceKey="Poolrooms.Player.Wings";
        public WingPreferences Preferences {get;private set;}=new WingPreferences();
        private readonly WingFlight flight=new WingFlight();
        public bool Flying {get{return isActiveAndEnabled&&flight.Active;}}
        public string Status {get{return !Preferences.equipped?"Alas desequipadas":WingCatalog.Name(Preferences.design)+" · "+(Flying?"Volando":"En reposo")+" · "+WingFlight.Speed(Preferences.speed)+" m/s";}}
        public const string Attribution="Iconos: Lorc / Game-icons.net · CC BY 3.0. 144 variantes 3D originales; los iconos descargados no son modelos 3D.";
        private PoolroomsExplorer explorer;
        private CharacterController body;
        private PoolroomsGameSettings settings;
        private PoolroomsAvatar avatar;
        private GameObject visualRoot;
        private readonly Transform[] wings=new Transform[2];
        private readonly List<MeshRenderer> renderers=new List<MeshRenderer>();
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private bool visible,hidden,dirty,subscribed;
        private float saveAt,phase,unfold;
        public static readonly string[] SpeedNames={"Lento · 3 m/s","Medio · 8 m/s","Rápido · 18 m/s","Muy rápido · 36 m/s"};
        private void Awake()
        {
            explorer=GetComponent<PoolroomsExplorer>();body=GetComponent<CharacterController>();
            try{Preferences=JsonUtility.FromJson<WingPreferences>(PlayerPrefs.GetString(PreferenceKey,""))??new WingPreferences();}catch{Preferences=new WingPreferences();}
            Preferences.Sanitize();
        }
        private void OnEnable(){HookSettings();Rebuild();}
        private void HookSettings()
        {
            if(settings==null)settings=GetComponent<PoolroomsGameSettings>();
            if(settings!=null&&!subscribed){settings.Changed+=ApplyRendering;subscribed=true;}
        }
        public bool ToggleFlight()
        {
            bool allowed=Preferences.equipped&&explorer!=null&&explorer.isActiveAndEnabled&&!explorer.IsPaused&&!explorer.InputCaptured&&
                !explorer.IsAquaticRiding&&explorer.Vehicle==null&&(explorer.MasterMode==null||!explorer.MasterMode.Flying)&&
                body!=null&&body.enabled&&(explorer.world==null||explorer.world.ViewerReady);
            return isActiveAndEnabled&&flight.Toggle(allowed);
        }
        public void Stop(){flight.Stop();}
        public void Suspend(){flight.Suspend();}
        public void CycleSpeed(){Preferences.speed=(WingSpeed)(((int)Preferences.speed+1)%4);SavePreferences(false);}
        public void SavePreferences(bool rebuild=true)
        {
            Preferences.Sanitize();dirty=true;saveAt=Time.unscaledTime+.6f;
            if(!Preferences.equipped)Stop();if(rebuild)Rebuild();else{ApplyAttachment();ApplyRendering();}
        }
        public void ResetSettings(){Stop();Preferences=new WingPreferences();SavePreferences();}
        private void Flush()
        {if(!dirty)return;PlayerPrefs.SetString(PreferenceKey,JsonUtility.ToJson(Preferences));PlayerPrefs.Save();dirty=false;}
        // Called only by the explorer after pause/chat/photo/streaming input gates.
        public void TickFlight(Vector3 direction,float dt)
        {
            if(!Flying||body==null||!body.enabled)return;
            if(explorer.IsPaused||explorer.InputCaptured||explorer.IsAquaticRiding||explorer.Vehicle!=null){Suspend();return;}
            V3 delta=flight.Step(new V3(direction.x,direction.y,direction.z),Preferences.speed,dt);
            int count=WingFlight.Steps(delta);Vector3 step=new Vector3(delta.X,delta.Y,delta.Z)/count;
            bool collision=false;
            for(int i=0;i<count;i++)
            {
                // Region readiness is sampled after every bounded capsule movement, including at 10 FPS.
                if(explorer.world!=null&&!explorer.world.ViewerReady){Suspend();break;}
                if(body.Move(step)!=CollisionFlags.None)collision=true;
            }
            if(collision)Suspend();
        }
        public void SetVisible(bool value,bool completelyHidden)
        {visible=value;hidden=completelyHidden;ApplyVisibility();}
        private void ApplyVisibility()
        {
            foreach(var renderer in renderers)if(renderer!=null)
            {
                renderer.forceRenderingOff=!Preferences.equipped||hidden||!visible&&!Preferences.castShadows;
                renderer.shadowCastingMode=Preferences.castShadows?(visible?ShadowCastingMode.On:ShadowCastingMode.ShadowsOnly):ShadowCastingMode.Off;
            }
        }
        private void Update()
        {if(!subscribed)HookSettings();if(dirty&&Time.unscaledTime>=saveAt)Flush();}
        private void LateUpdate()
        {
            if(visualRoot==null)return;
            ApplyAttachment();
            bool moving=explorer!=null&&!explorer.IsPaused&&!explorer.InputCaptured&&(explorer.world==null||explorer.world.ViewerReady)&&Cursor.lockState==CursorLockMode.Locked;
            float dt=moving?ExplorerLocomotion.FrameTime(Time.deltaTime):0;
            if(dt>0)
            {
                phase=Mathf.Repeat(phase+dt*(Flying?5.8f:1.4f),Mathf.PI*2);
                unfold=Mathf.MoveTowards(unfold,Flying||!Preferences.foldOnGround?1:0,dt*3);
            }
            for(int side=0;side<2;side++)if(wings[side]!=null)
            {
                float sign=side==0?1:-1;
                float fold=Mathf.Lerp(73,4,unfold),flap=Mathf.Sin(phase)*Mathf.Lerp(2,17,unfold);
                wings[side].localRotation=Quaternion.Euler(0,sign*(fold+flap*.35f),sign*(Mathf.Lerp(-48,10,unfold)+flap));
            }
        }
        private void Rebuild()
        {
            ClearVisual();if(!Preferences.equipped||!isActiveAndEnabled)return;
            visualRoot=new GameObject("Player wings / "+WingCatalog.Name(Preferences.design));visualRoot.transform.SetParent(transform,false);
            ApplyAttachment();
            unfold=Flying||!Preferences.foldOnGround?1:0;
            var material=CreateMaterials();
            for(int side=0;side<2;side++)
            {
                bool left=side==1;var mesh=Upload(WingCatalog.Build(Preferences.design,left));owned.Add(mesh);
                var go=new GameObject(left?"Left wing":"Right wing");go.transform.SetParent(visualRoot.transform,false);
                go.transform.localPosition=new Vector3(left?-.14f:.14f,0,0);wings[side]=go.transform;
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=material;renderers.Add(renderer);
            }
            // A solid back mount keeps both wings attached when the skin is replaced.
            var mountData=new MeshData();mountData.Box(new V3(-.16f,-.12f,-.035f),new V3(.16f,.12f,.035f),Surface.Metal,.2f);
            var mountMesh=Upload(mountData);owned.Add(mountMesh);var mount=new GameObject("Wing harness");mount.transform.SetParent(visualRoot.transform,false);
            mount.AddComponent<MeshFilter>().sharedMesh=mountMesh;var mountRenderer=mount.AddComponent<MeshRenderer>();mountRenderer.sharedMaterials=material;renderers.Add(mountRenderer);
            ApplyRendering();
        }
        private void ApplyAttachment()
        {
            if(visualRoot==null)return;
            float height=body!=null?body.height:1.8f;
            if(avatar==null)avatar=GetComponent<PoolroomsAvatar>();
            if(avatar!=null&&avatar.TryBackFrame(out var position,out var up))
            {
                // Use chest/neck positions rather than imported bone axes, which differ between rigs.
                Quaternion rotation=Quaternion.FromToRotation(transform.up,up)*transform.rotation;
                visualRoot.transform.SetPositionAndRotation(position+up*((Preferences.attachmentHeight-.74f)*height)-(rotation*Vector3.forward)*Preferences.backOffset,rotation);
            }
            else
            {
                visualRoot.transform.localPosition=new Vector3(0,height*Preferences.attachmentHeight,-Preferences.backOffset);
                visualRoot.transform.localRotation=Quaternion.identity;
            }
            visualRoot.transform.localScale=Vector3.one*Preferences.size;
        }
        private Material[] CreateMaterials()
        {
            Shader shader=Shader.Find("HDRP/Lit")??Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
            if(shader==null)throw new InvalidOperationException("Alas: falta un shader Lit compatible.");
            var result=new Material[6];int family=Preferences.design/WingCatalog.Variants;
            for(int slot=0;slot<result.Length;slot++)
            {
                if(slot!=0&&slot!=1&&slot!=3&&slot!=4)continue;
                var material=new Material(shader){name="Wing finish / "+slot,enableInstancing=true};owned.Add(material);result[slot]=material;
                V3 rgb=WingCatalog.Color(Preferences.design,slot==3);Color tint=new Color(rgb.X,rgb.Y,rgb.Z);
                if(slot==1&&family==5)tint=new Color(.92f,.64f,.29f);
                foreach(string p in new[]{"_BaseColor","_Color"})if(material.HasProperty(p))material.SetColor(p,tint);
                foreach(string p in new[]{"_Smoothness","_Glossiness"})if(material.HasProperty(p))material.SetFloat(p,family==7||family==10||slot==3?.72f:.36f);
                if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",family==7||family==11||slot==3?.72f:0);
                if(slot==4)
                {
                    foreach(string p in new[]{"_EmissiveColor","_EmissionColor"})if(material.HasProperty(p))material.SetColor(p,tint*1.6f);
                    material.EnableKeyword("_EMISSION");
                }
                PoolroomsBiomeMaterialFactory.Validate(material);
            }
            return result;
        }
        private static Mesh Upload(MeshData data)
        {
            var mesh=new Mesh{name="Volumetric wings",indexFormat=data.Positions.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            var positions=new List<Vector3>(data.Positions.Count);var normals=new List<Vector3>(data.Normals.Count);var uv=new List<Vector2>(data.UV.Count);
            foreach(var v in data.Positions)positions.Add(new Vector3(v.X,v.Y,v.Z));foreach(var n in data.Normals)normals.Add(new Vector3(n.X,n.Y,n.Z));foreach(var t in data.UV)uv.Add(new Vector2(t.X,t.Y));
            mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=6;
            for(int i=0;i<6;i++)mesh.SetTriangles(data.Indices[i],i,false);
            mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
        }
        private void ApplyRendering()
        {
            bool rt=settings!=null&&settings.RayTracingAvailable&&(int)settings.Preferences.graphicsQuality>=2;
            foreach(var renderer in renderers)if(renderer!=null)
            {
                renderer.receiveShadows=true;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
                renderer.motionVectorGenerationMode=MotionVectorGenerationMode.Object;
                renderer.rayTracingMode=rt?UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform:UnityEngine.Experimental.Rendering.RayTracingMode.Off;
            }
            ApplyVisibility();
        }
        private void ClearVisual()
        {
            if(visualRoot!=null){visualRoot.SetActive(false);Destroy(visualRoot);}visualRoot=null;renderers.Clear();
            for(int i=0;i<wings.Length;i++)wings[i]=null;
            foreach(var item in owned)if(item!=null)Destroy(item);owned.Clear();
        }
        private void OnDisable(){Stop();Flush();if(settings!=null&&subscribed)settings.Changed-=ApplyRendering;subscribed=false;ClearVisual();}
        private void OnDestroy(){ClearVisual();}
    }
}
