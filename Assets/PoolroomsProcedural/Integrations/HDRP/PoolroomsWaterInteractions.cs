#if POOLROOMS_HDRP_17
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsHDRP))]
    public sealed class PoolroomsWaterInteractions : MonoBehaviour
    {
        [SerializeField,TextArea] private string status;
        public string Status { get { return status; } }
        private PoolroomsHDRP adapter;
        private PoolroomsWorld world;
        private PoolroomsExplorer explorer;
        private CharacterController body;
        private Layout layout;
        private WaterDecal decal;
        private Material material;
        private BoxCollider bounds;
        private WaterSurface boundSurface;
        private BoxCollider previousBounds;
        private bool previousUnderwater,positioned,warnedShader;
        private bool previousRefraction;
        private float previousAbsorptionMultiplier;
        private Vector3 lastPosition;
        private float travelled,wakeCooldown,nextUpload;
        private int displayedCount=-1,displayedBudget;
        private bool displayedFoam;
        private readonly PoolRippleBuffer ripples=new PoolRippleBuffer();
        private readonly Vector4[] positions=new Vector4[PoolWaterModel.MaxRipples],pools=new Vector4[PoolWaterModel.MaxRipples],shapes=new Vector4[PoolWaterModel.MaxRipples];
        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            adapter=GetComponent<PoolroomsHDRP>();world=GetComponent<PoolroomsWorld>();BindExplorer();Clear();
        }
        private void BindExplorer()
        {
            var next=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;
            if(explorer==next)return;
            if(explorer!=null){explorer.WaterImpact-=Impact;explorer.Relocated-=Relocated;explorer.OriginShifted-=ShiftOrigin;}
            explorer=next;body=explorer!=null?explorer.GetComponent<CharacterController>():null;
            if(explorer!=null){explorer.WaterImpact+=Impact;explorer.Relocated+=Relocated;explorer.OriginShifted+=ShiftOrigin;}
            Clear();
        }
        private bool Available(bool requireDecals=true)
        {
            if(adapter==null||!adapter.isActiveAndEnabled||!adapter.nativeWater||world==null||!world.isActiveAndEnabled)return false;
            if(requireDecals&&!adapter.interactiveWater){status="Ondas locales desactivadas.";return false;}
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if(asset==null||!asset.currentPlatformRenderPipelineSettings.supportWater)return false;
            if(requireDecals&&!asset.currentPlatformRenderPipelineSettings.supportWaterDecals)
            {status="Activa Water Decals en el HDRP Asset desde Tools > Poolrooms > Agua.";return false;}
            if(requireDecals&&asset.currentPlatformRenderPipelineSettings.maximumWaterDecalCount<1)
            {status="El HDRP Asset necesita permitir al menos un Water Decal. Ejecuta el instalador de Agua.";return false;}
            return adapter.water!=null&&adapter.water.isActiveAndEnabled;
        }
        private void LateUpdate()
        {
            if(!Application.isPlaying||world==null)return;BindExplorer();
            if(!Available(false)||explorer==null||!explorer.IsReady||!explorer.isActiveAndEnabled)
            {
                Clear();if(adapter!=null&&adapter.water!=null)adapter.water.underWater=false;return;
            }
            if(explorer.IsPaused||Time.deltaTime<=0)return;
            UpdateUnderwater();
            if(!Available()){Clear();return;}
            if(layout!=world.CurrentLayout){Clear();layout=world.CurrentLayout;}
            var budget=PoolWaterModel.Budget(adapter.lowCost?PoolWaterDetail.Performance:adapter.waterDetail);
            ripples.Capacity=budget.Ripples;ripples.Advance(Time.deltaTime);
            Vector3 position=explorer.transform.position;
            if(!positioned){lastPosition=position;positioned=true;}
            Vector3 delta=position-lastPosition;lastPosition=position;delta.y=0;
            float distance=delta.magnitude;wakeCooldown=Mathf.Max(0,wakeCooldown-Time.deltaTime);
            if(distance>Mathf.Max(2,Time.deltaTime*8)){Clear();lastPosition=position;positioned=true;return;}
            WaterRegion pool;
            bool wet=world.TryGetWaterRegion(position,out pool)&&body!=null&&body.bounds.min.y<pool.Level-.015f;
            if(wet&&distance/Time.deltaTime>.12f)
            {
                travelled+=distance;
                if(travelled>.7f&&wakeCooldown<=0)
                {
                    travelled=0;wakeCooldown=.32f;
                    ripples.Add(new V3(position.x,pool.Level,position.z),pool,.13f*CameraPresentation.Unit(adapter.interactionIntensity),false);
                }
            }
            else if(!wet)travelled=0;
            if(Time.time>=nextUpload){nextUpload=Time.time+.05f;Upload(budget);}
        }
        private bool CreateDecal()
        {
            if(decal!=null)return true;
            Shader shader=Resources.Load<Shader>("PoolroomsWaterInteraction");
            if(shader==null||!shader.isSupported)
            {
                status="Falta el shader de interacción del agua o no está soportado.";
                if(!warnedShader){warnedShader=true;Debug.LogWarning(status,this);}return false;
            }
            material=CoreUtils.CreateEngineMaterial(shader);
            var go=new GameObject("Poolrooms local water waves");go.SetActive(false);go.transform.SetParent(transform,false);
            decal=go.AddComponent<WaterDecal>();decal.material=material;decal.scaleMode=DecalScaleMode.ScaleInvariant;
            decal.amplitude=PoolWaterModel.MaxHeight;decal.deepFoamDimmer=0;decal.surfaceFoamDimmer=.55f;
            decal.updateMode=CustomRenderTextureUpdateMode.OnDemand;go.SetActive(true);decal.enabled=false;
            return true;
        }
        private void Upload(WaterBudget budget)
        {
            int count=0;
            foreach(var e in ripples.Events)if(e.Alive)
            {
                positions[count]=new Vector4(e.Position.X,e.Position.Z,e.Age,e.Amplitude/PoolWaterModel.MaxHeight);
                pools[count]=new Vector4(e.Pool.CenterX,e.Pool.CenterZ,e.Pool.Width*.5f,e.Pool.Depth*.5f);
                shapes[count]=new Vector4(e.Pool.Radius,1/e.Lifetime,e.Strength,e.Impact?1:0);count++;
            }
            if(count==0){if(decal!=null)decal.enabled=false;displayedCount=-1;status="Agua HDRP activa; esperando movimiento o contacto.";return;}
            if(!CreateDecal())return;
            Vector3 focus=explorer.transform.position;
            decal.transform.position=new Vector3(Mathf.Round(focus.x*8)/8,adapter.water.transform.position.y,Mathf.Round(focus.z*8)/8);
            decal.transform.rotation=Quaternion.identity;
            decal.regionSize=Vector2.one*budget.RegionSize;
            var asset=(HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            int atlasSide=(int)asset.currentPlatformRenderPipelineSettings.waterDecalAtlasSize;
            decal.resolution=Vector2Int.one*Mathf.Min(budget.AtlasResolution,Mathf.Max(16,atlasSide/2));
            bool foam=budget.Foam&&adapter.impactFoam;
            material.SetFloat("_AffectsFoam",foam?1:0);
            material.SetInt("_RippleCount",count);material.SetVector("_DecalArea",new Vector4(decal.transform.position.x,decal.transform.position.z,budget.RegionSize,budget.RegionSize));
            material.SetVectorArray("_Ripples",positions);material.SetVectorArray("_Pools",pools);material.SetVectorArray("_Shapes",shapes);
            decal.enabled=true;decal.RequestUpdate();
            if(displayedCount!=count||displayedBudget!=budget.Ripples||displayedFoam!=foam)
            {
                displayedCount=count;displayedBudget=budget.Ripples;displayedFoam=foam;
                status="Ondas locales HDRP: "+count+" / "+budget.Ripples+". "+(foam?"Espuma de impacto activa.":"Sin espuma en este ajuste.");
            }
        }
        private void Impact(Vector3 position,float strength)
        {
            if(!Available()||explorer==null||!explorer.IsReady||explorer.IsPaused||Time.deltaTime<=0)return;
            var budget=PoolWaterModel.Budget(adapter.lowCost?PoolWaterDetail.Performance:adapter.waterDetail);
            ripples.Capacity=budget.Ripples;WaterRegion pool;
            if(world.TryGetWaterRegion(position,out pool))
            {
                ripples.Add(new V3(position.x,pool.Level,position.z),pool,strength*CameraPresentation.Unit(adapter.interactionIntensity),true);
                nextUpload=0;travelled=0;wakeCooldown=.32f;
            }
        }
        private void UpdateUnderwater()
        {
            WaterSurface surface=adapter.water;
            if(boundSurface!=surface)
            {
                RestoreBounds();boundSurface=surface;previousBounds=surface.volumeBounds;previousUnderwater=surface.underWater;
                previousRefraction=surface.underWaterRefraction;previousAbsorptionMultiplier=surface.absorptionDistanceMultiplier;
            }
            Transform eyes=explorer.eyes;WaterRegion pool;
            bool submerged=adapter.underwaterEffects&&eyes!=null&&world.TryGetWaterRegion(eyes.position,out pool)&&eyes.position.y<pool.Level-.035f;
            surface.underWater=submerged;
            if(!submerged)return;
            // Bounds are created only when needed. The exact rounded footprint query above
            // prevents the bounding rectangle from enabling fog in dry corners/other pools.
            if(bounds==null)
            {
                var go=new GameObject("Poolrooms underwater volume");go.transform.SetParent(transform,false);
                bounds=go.AddComponent<BoxCollider>();bounds.isTrigger=true;
            }
            world.TryGetWaterRegion(eyes.position,out pool);
            float depth=Mathf.Max(.05f,pool.Level-pool.Bottom);
            Vector3 centre=new Vector3(pool.CenterX,pool.Bottom+depth*.5f,pool.CenterZ),size=new Vector3(pool.Width,depth+.08f,pool.Depth);
            if(bounds.transform.position!=centre)bounds.transform.position=centre;
            if(bounds.size!=size)bounds.size=size;
            surface.volumeBounds=bounds;
            surface.absorptionDistanceMultiplier=1.25f;surface.underWaterRefraction=!adapter.lowCost;
        }
        private void RestoreBounds()
        {
            if(boundSurface!=null&&boundSurface.volumeBounds==bounds)
            {
                boundSurface.volumeBounds=previousBounds;boundSurface.underWater=previousUnderwater;
                boundSurface.underWaterRefraction=previousRefraction;boundSurface.absorptionDistanceMultiplier=previousAbsorptionMultiplier;
            }
            boundSurface=null;
        }
        private void Clear()
        {ripples.Clear();positioned=false;travelled=0;wakeCooldown=0;nextUpload=0;displayedCount=-1;if(decal!=null)decal.enabled=false;}
        private void ShiftOrigin(Vector3 shift)
        {
            lastPosition+=shift;ripples.ShiftOrigin(new V3(shift.x,0,shift.z));
            if(decal!=null)decal.transform.position+=shift;
            if(bounds!=null)bounds.transform.position+=shift;
            nextUpload=0;
            if(adapter!=null&&Available())Upload(PoolWaterModel.Budget(adapter.lowCost?PoolWaterDetail.Performance:adapter.waterDetail));
        }
        private void Relocated(){Clear();if(adapter!=null&&adapter.water!=null)adapter.water.underWater=false;}
        private void OnDisable()
        {
            if(explorer!=null){explorer.WaterImpact-=Impact;explorer.Relocated-=Relocated;explorer.OriginShifted-=ShiftOrigin;}explorer=null;Clear();RestoreBounds();
            if(decal!=null)CoreUtils.Destroy(decal.gameObject);if(bounds!=null)CoreUtils.Destroy(bounds.gameObject);CoreUtils.Destroy(material);
            decal=null;bounds=null;material=null;layout=null;
        }
    }
}
#endif
