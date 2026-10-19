using System;
using System.Collections;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    public interface IPoolroomsTerrainAtmosphere { void Release(); }
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed partial class PoolroomsTerrainSystem : MonoBehaviour
    {
        private PoolroomsWorld world;
        private PoolroomsExplorer explorer;
        public PoolroomsExplorer Explorer { get { if(explorer==null&&world!=null&&world.viewer!=null)explorer=world.viewer.GetComponentInParent<PoolroomsExplorer>();return explorer; } private set { explorer=value; } }
        public TerrainRegionPlan Plan { get; private set; }
        public bool IsInside { get; private set; }
        public Transform RegionRoot { get; private set; }
        public Transform LooseRoot { get; private set; }
        public Light Sun { get; private set; }
        public bool InsideHouse { get; private set; }
        public TerrainVisualQuality VisualQuality
        {get{var settings=Explorer!=null?Explorer.GetComponent<PoolroomsGameSettings>():null;return settings!=null?settings.Preferences.terrainVisuals:TerrainVisualQuality.Economy;}}
        public float InteriorShelter
        {
            get
            {
                if(!IsInside||Plan==null||RegionRoot==null||world.viewer==null)return 0;
                Vector3 p=RegionRoot.InverseTransformPoint(world.viewer.position);int cx=TerrainRegionPlan.Cell(p.x),cz=TerrainRegionPlan.Cell(p.z);float result=0;
                for(int z=cz-1;z<=cz+1;z++)for(int x=cx-1;x<=cx+1;x++)result=Mathf.Max(result,TerrainAppearance.Shelter(Plan.House(x,z),new V3(p.x,p.y,p.z)));
                return result;
            }
        }
        private Room entrance,acousticRoom;
        private Vector3 returnPosition;
        private readonly GeometryBuildJob job=new GeometryBuildJob();
        private Coroutine worker;
        private GameObject stagingTile;
        public bool HasStagedTile { get { return stagingTile!=null; } }
        private int epoch,horizonX=int.MinValue,horizonZ=int.MinValue,horizonRadius=-1;
        private PoolroomsChunk horizon,materialOwner;
        private TerrainMaterialSet materials;
        private TerrainVisualQuality appliedVisualQuality=(TerrainVisualQuality)(-1);
        private IPoolroomsTerrainAtmosphere atmosphere;
        private bool bootstrapped;
        private struct Tile { public PoolroomsChunk Chunk;public int X,Z,Resolution;public bool Detailed; }
        private readonly Dictionary<long,Tile> loaded=new Dictionary<long,Tile>();
        private readonly List<long> remove=new List<long>();
        private readonly Dictionary<Light,bool> previousSuns=new Dictionary<Light,bool>();
        private Camera cameraView;private float oldFar;
        public int LoadedCount { get { return loaded.Count; } }
        public IEnumerable<PoolroomsChunk> Chunks { get { foreach(var t in loaded.Values)yield return t.Chunk;if(IsConnected){foreach(var b in connectedBridges)if(b!=null)yield return b;if(horizon!=null)yield return horizon;} } }
        public Room AcousticRoom { get { return acousticRoom; } }
        public Vector3 Spawn { get { return RegionRoot!=null?RegionRoot.TransformPoint(new Vector3(0,Plan.Height(0,4)+.12f,4)):world.transform.position+Vector3.up*.1f; } }
        private void Awake(){world=GetComponent<PoolroomsWorld>();if(GetComponent<PoolroomsTerrainGrass>()==null)gameObject.AddComponent<PoolroomsTerrainGrass>();}
        private void OnDisable(){if(RegionRoot!=null)Exit();}
        public bool ViewerReady
        {
            get{return IsInside&&bootstrapped&&world.viewer!=null&&IsPositionReady(world.viewer.position);}
        }
        public bool ContainsPosition(Vector3 position)
        {if(RegionRoot==null||Plan==null||!RegionRoot.gameObject.activeInHierarchy)return false;Vector3 p=RegionRoot.InverseTransformPoint(position);return Plan.Contains(p.x,p.z);}
        public bool IsPositionReady(Vector3 position)
        {
            if(RegionRoot==null||Plan==null||!RegionRoot.gameObject.activeInHierarchy||!IsInside&&!IsConnected)return false;
            Vector3 p=RegionRoot.InverseTransformPoint(position);if(Plan.Connected&&!Plan.Contains(p.x,p.z))return false;
            int cx=TerrainRegionPlan.Cell(p.x),cz=TerrainRegionPlan.Cell(p.z);
            Tile t;if(!loaded.TryGetValue(TerrainRegionPlan.Key(cx,cz),out t)||!t.Detailed)return false;
            int sx=p.x-cx*TerrainRegionPlan.TileSize>TerrainRegionPlan.HalfTile-12?1:p.x-cx*TerrainRegionPlan.TileSize<-TerrainRegionPlan.HalfTile+12?-1:0,sz=p.z-cz*TerrainRegionPlan.TileSize>TerrainRegionPlan.HalfTile-12?1:p.z-cz*TerrainRegionPlan.TileSize<-TerrainRegionPlan.HalfTile+12?-1:0;
            for(int z=Math.Min(0,sz);z<=Math.Max(0,sz);z++)for(int x=Math.Min(0,sx);x<=Math.Max(0,sx);x++)
                if(Plan.IntersectsTile(cx+x,cz+z)&&(!loaded.TryGetValue(TerrainRegionPlan.Key(cx+x,cz+z),out t)||!t.Detailed))return false;
            return true;
        }
        private void EnsureMaterials()
        {
            if(materials!=null)return;
            var root=new GameObject("Nivel 94 shared materials");root.transform.SetParent(transform,false);materialOwner=root.AddComponent<PoolroomsChunk>();materialOwner.waterRegions=new WaterRegion[0];materials=new TerrainMaterialSet(materialOwner);
        }
        public bool HasEntrance(Room room)
        {
            if(world==null)world=GetComponent<PoolroomsWorld>();
            return !ConnectedWorld&&!ConnectedMode&&world.profile!=null&&world.profile.enableTerrainRegions&&room!=null&&
                (StableRandom.Hash(world.profile.seed,room.X,room.Z,0x940E0u)>>8)*(1f/16777216f)<Mathf.Clamp01(world.profile.terrainEntranceChance);
        }
        public void AddEntrance(Room room,Transform parent)
        {
            if(!Application.isPlaying||!HasEntrance(room)||parent.GetComponentInChildren<PoolroomsTerrainPortal>(true)!=null)return;
            EnsureMaterials();Physics.SyncTransforms();var chunk=parent.GetComponent<PoolroomsChunk>();
            foreach(Vector2 point in new[]{new Vector2(room.Width*.28f,-room.Depth*.28f),new Vector2(-room.Width*.28f,-room.Depth*.28f),new Vector2(room.Width*.28f,room.Depth*.28f),Vector2.zero})
            {
                bool wet=false;if(chunk!=null&&chunk.waterRegions!=null)foreach(var region in chunk.waterRegions)if(region.Contains(point.x,point.y)){wet=true;break;}
                if(wet)continue;RaycastHit hit;
                Vector3 origin=parent.TransformPoint(new Vector3(point.x,room.Floor+3,point.y));
                if(!Physics.Raycast(origin,Vector3.down,out hit,6,~0,QueryTriggerInteraction.Ignore)||!hit.collider.transform.IsChildOf(parent))continue;
                Vector3 foot=parent.InverseTransformPoint(hit.point);
                if(Physics.CheckBox(parent.TransformPoint(foot+Vector3.up*1.8f),new Vector3(1.45f,1.65f,.65f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))continue;
                bool safe=true;
                foreach(float dz in new[]{-3f,1.8f})
                {
                    var landing=foot+Vector3.forward*dz;
                    if(chunk!=null&&chunk.waterRegions!=null)foreach(var region in chunk.waterRegions)if(region.Contains(landing.x,landing.z)){safe=false;break;}
                    RaycastHit support;
                    if(!Physics.Raycast(parent.TransformPoint(landing+Vector3.up*.65f),Vector3.down,out support,1,~0,QueryTriggerInteraction.Ignore)||!support.collider.transform.IsChildOf(parent)||Mathf.Abs(parent.InverseTransformPoint(support.point).y-foot.y)>.2f)safe=false;
                    if(Physics.CheckBox(parent.TransformPoint(landing+Vector3.up*.95f),new Vector3(.37f,.83f,.37f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))safe=false;
                }
                if(!safe)continue;
                PoolroomsTerrainPortal.Create(this,room,false,foot,parent,world.profile,materials.Portal);return;
            }
        }
        public bool VisitNearest()
        {
            if(world==null||world.profile==null||!world.profile.enableTerrainRegions||world.CurrentLayout==null||world.viewer==null)return false;
            if(ConnectedWorld)return ConnectedMode&&VisitConnected();
            if(IsInside)return true;
            if(world.Malls!=null&&world.Malls.IsInside)world.Malls.Exit();
            var r=world.RoomAtViewer();return r!=null&&Enter(r,new Vector3(0,r.Floor,0));
        }
        public bool Enter(Room room,Vector3 localEntrance)
        {
            if(ConnectedWorld)return ConnectedMode&&VisitConnected();
            if(!Application.isPlaying||!isActiveAndEnabled||IsInside||world==null||world.CurrentLayout==null||world.viewer==null||room==null)return false;
            Explorer=world.viewer.GetComponentInParent<PoolroomsExplorer>();if(Explorer==null)return false;
            if(world.Malls!=null&&world.Malls.IsInside)world.Malls.Exit();
            EnsureMaterials();entrance=room;returnPosition=world.RoomWorldPosition(room)+localEntrance-Vector3.forward*3+Vector3.up*.12f;
            Plan=TerrainRegionPlan.Create(world.CurrentLayout.Config.Seed,room.X,room.Z,true,world.CurrentLayout.Config.ExpandedVariety,world.CurrentLayout.Config.ExpandedBiomeAreas);
            acousticRoom=new Room{X=room.X,Z=room.Z,Width=12,Depth=12,Height=3.2f,Environment=LiminalEnvironment.Hotel,DetailSeed=room.DetailSeed,
                Plan=new ProceduralPlan(LiminalEnvironment.Hotel,new PlannedModule[0],new WaterRegion[0],4,0,0)};
            var root=new GameObject("Nivel 94 · "+Plan.Width+" x "+Plan.Depth+" m · "+room.X+","+room.Z);RegionRoot=root.transform;RegionRoot.SetParent(transform,false);RegionRoot.localPosition=Vector3.up*1200;
            var loose=new GameObject("Loose meadow objects");LooseRoot=loose.transform;LooseRoot.SetParent(RegionRoot,false);
            IsInside=true;bootstrapped=false;epoch++;
            if(world.GeneratedRoot!=null)world.GeneratedRoot.gameObject.SetActive(false);
            var carry=Explorer.GetComponent<PoolroomsCarry>();if(carry!=null)carry.ShowExteriorObjects(false);
            foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional&&l.GetComponentInParent<PoolroomsMasterView>()==null){previousSuns[l]=l.enabled;l.enabled=false;}
            var sun=new GameObject("Nivel 94 daylight");sun.transform.SetParent(RegionRoot,false);sun.transform.localRotation=Quaternion.Euler(Plan.SunPitch,Plan.SunYaw,0);
            Sun=sun.AddComponent<Light>();Sun.type=LightType.Directional;Sun.color=Plan.Daylight==TerrainDaylight.WarmAfternoon?new Color(1,.89f,.73f):new Color(1,.97f,.91f);Sun.intensity=1.1f;Sun.shadows=LightShadows.Soft;
            var masterView=Explorer.GetComponent<PoolroomsMasterView>();if(masterView!=null)masterView.ReleaseCameraDistance();
            cameraView=Explorer.eyes!=null?Explorer.eyes.GetComponent<Camera>():null;if(cameraView!=null){oldFar=cameraView.farClipPlane;cameraView.farClipPlane=Mathf.Max(oldFar,1800);}
            var bridge=Type.GetType("Poolrooms.HDRP.PoolroomsTerrainAtmosphere, Poolrooms.HDRP");if(bridge!=null&&GraphicsSettings.currentRenderPipeline!=null&&GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline")){var component=GetComponent(bridge);if(component==null)component=gameObject.AddComponent(bridge);atmosphere=component as IPoolroomsTerrainAtmosphere;}
            PoolroomsTerrainPortal.Create(this,null,true,new Vector3(0,Plan.Height(0,0),0),RegionRoot,world.profile,materials.Portal);
            Explorer.Teleport(Spawn);world.ExternalGeometryChanged();worker=StartCoroutine(Stream(epoch));return true;
        }
        public void Exit(Vector3? destination=null)
        {
            if(IsConnected){ClearConnected();if(destination.HasValue&&Explorer!=null)Explorer.Teleport(destination.Value);world.ExternalGeometryChanged();return;}
            if(!IsInside)return;Vector3 target=destination??returnPosition;IsInside=false;atmosphere?.Release();InsideHouse=false;bootstrapped=false;epoch++;
            if(worker!=null)StopCoroutine(worker);worker=null;DiscardStagedTile();
            if(RegionRoot!=null){RegionRoot.gameObject.SetActive(false);Destroy(RegionRoot.gameObject);}RegionRoot=null;LooseRoot=null;Sun=null;horizon=null;loaded.Clear();horizonX=horizonZ=int.MinValue;
            var masterView=Explorer!=null?Explorer.GetComponent<PoolroomsMasterView>():null;if(masterView!=null)masterView.ReleaseCameraDistance();
            if(cameraView!=null)cameraView.farClipPlane=oldFar;cameraView=null;
            foreach(var pair in previousSuns)if(pair.Key!=null)pair.Key.enabled=pair.Value;previousSuns.Clear();
            if(world.GeneratedRoot!=null)world.GeneratedRoot.gameObject.SetActive(true);
            var carry=Explorer!=null?Explorer.GetComponent<PoolroomsCarry>():null;if(carry!=null)carry.ShowExteriorObjects(true);
            if(Explorer!=null)Explorer.Teleport(target);world.ExternalGeometryChanged();
        }
        public bool BelowFloor(Vector3 p)
        {if(RegionRoot==null)return false;Vector3 local=RegionRoot.InverseTransformPoint(p);return local.y<Plan.Height(local.x,local.z)-10;}
        private void Update()
        {
            if(materials!=null&&RegionRoot!=null&&appliedVisualQuality!=VisualQuality)
            {
                appliedVisualQuality=VisualQuality;
                foreach(var renderer in RegionRoot.GetComponentsInChildren<MeshRenderer>(true))
                    if(renderer.gameObject.name=="Meadow ground"||renderer.gameObject.name=="Horizon")renderer.sharedMaterial=materials.ForGround(appliedVisualQuality)[0];
            }
            if(!IsInside||RegionRoot==null||world.viewer==null)return;
            Vector3 p=RegionRoot.InverseTransformPoint(world.viewer.position);var house=Plan.HouseAt(p.x,p.y,p.z);
            InsideHouse=house!=null&&house.Contains(p.x,p.y,p.z);
        }
        private int CompleteRadius(int cx,int cz,int requested)
        {
            int complete=-1;
            for(int ring=0;ring<=requested;ring++)
            {
                for(int z=-ring;z<=ring;z++)for(int x=-ring;x<=ring;x++)
                {
                    if(Math.Max(Math.Abs(x),Math.Abs(z))!=ring)continue;
                    if(!Plan.IntersectsTile(cx+x,cz+z))continue;
                    Tile tile;if(!loaded.TryGetValue(TerrainRegionPlan.Key(cx+x,cz+z),out tile)||ring<=1&&!tile.Detailed)return complete;
                }
                complete=ring;
            }
            return complete;
        }
        private IEnumerator Stream(int version)
        {
            while(job.HasWork&&!job.IsCompleted)yield return null;if(job.HasWork)job.DiscardCompleted();if(version!=epoch)yield break;
            TerrainRegionPlan plan=Plan;
            while((IsInside||IsConnected)&&version==epoch)
            {
                if(IsConnected&&connectedSuspended){yield return null;continue;}
                Vector3 p=StreamPosition();int cx=TerrainRegionPlan.Cell(p.x),cz=TerrainRegionPlan.Cell(p.z),radius=world.TerrainRadius;
                remove.Clear();foreach(var pair in loaded)if(Math.Max(Math.Abs(pair.Value.X-cx),Math.Abs(pair.Value.Z-cz))>radius+1)remove.Add(pair.Key);
                foreach(long key in remove){var old=loaded[key];loaded.Remove(key);old.Chunk.gameObject.SetActive(false);Destroy(old.Chunk.gameObject);}if(remove.Count>0)world.ExternalGeometryChanged();
                int nx=0,nz=0,resolution=0,best=int.MaxValue;bool detail=false,found=false;
                for(int z=cz-radius;z<=cz+radius;z++)for(int x=cx-radius;x<=cx+radius;x++)
                {
                    if(!plan.IntersectsTile(x,z))continue;
                    int distance=Math.Max(Math.Abs(x-cx),Math.Abs(z-cz));Tile tile;
                    bool exists=loaded.TryGetValue(TerrainRegionPlan.Key(x,z),out tile);
                    float tileDistance=Mathf.Max(Mathf.Abs(x*TerrainRegionPlan.TileSize-p.x),Mathf.Abs(z*TerrainRegionPlan.TileSize-p.z));
                    bool near=TerrainStreamingPolicy.Detailed(distance,tileDistance,exists&&tile.Detailed);
                    int res=TerrainStreamingPolicy.Resolution(distance,tileDistance,exists?tile.Resolution:0,exists&&tile.Detailed);
                    if(exists&&tile.Resolution==res&&tile.Detailed==near)continue;
                    int score=(x-cx)*(x-cx)+(z-cz)*(z-cz);if(score>=best)continue;
                    nx=x;nz=z;resolution=res;detail=near;best=score;found=true;
                }
                int completeRadius=CompleteRadius(cx,cz,radius);
                bool horizonNeeded=completeRadius>=1&&(horizonX!=cx||horizonZ!=cz||horizonRadius!=completeRadius);
                if(!found||horizonNeeded&&!detail)
                {
                    if(horizonNeeded)
                    {
                        RoomGeometry far=null;int fx=cx,fz=cz,fr=completeRadius;
#if UNITY_WEBGL && !UNITY_EDITOR
                        far=new RoomGeometry();far.Structure.Append(TerrainArchitecture.Horizon(plan,fx,fz,fr));
#else
                        job.Start(()=>{var g=new RoomGeometry();g.Structure.Append(TerrainArchitecture.Horizon(plan,fx,fz,fr));return g;});while(!job.IsCompleted){if(version!=epoch)yield break;yield return null;}
                        if(version!=epoch)yield break;try{far=job.TakeCompleted();}catch(Exception e){Debug.LogException(e,this);Exit();yield break;}
#endif
                        p=StreamPosition();
                        if(TerrainRegionPlan.Cell(p.x)!=fx||TerrainRegionPlan.Cell(p.z)!=fz||CompleteRadius(fx,fz,world.TerrainRadius)!=fr){yield return null;continue;}
                        if(horizon!=null){horizon.gameObject.SetActive(false);Destroy(horizon.gameObject);}
                        var go=new GameObject("Distant rolling horizon");go.transform.SetParent(RegionRoot,false);horizon=go.AddComponent<PoolroomsChunk>();horizon.waterRegions=new WaterRegion[0];if(plan.Connected)go.AddComponent<PoolroomsTerrainChunk>();
                        var mesh=MeshFactory.Create("Horizon",far.Structure,go.transform,world.profile,horizon,false,false,materials.ForGround(VisualQuality));mesh.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                        horizonX=fx;horizonZ=fz;horizonRadius=fr;bootstrapped=true;
                    }
                    yield return null;continue;
                }
                int tx=nx,tz=nz,tr=resolution;int groundSupportIndices=0;bool td=detail;TerrainHouseGeometry houseData=null;RoomGeometry data=null;var house=plan.House(tx,tz);
#if UNITY_WEBGL && !UNITY_EDITOR
                data=new RoomGeometry();data.Structure.Append(TerrainArchitecture.Ground(plan,tx,tz,tr,true,out groundSupportIndices));data.Decoration.Append(TerrainArchitecture.Roads(plan,tx,tz));if(house!=null)houseData=TerrainHouseArchitecture.Build(house,td,plan);
#else
                job.Start(()=>{var g=new RoomGeometry();g.Structure.Append(TerrainArchitecture.Ground(plan,tx,tz,tr,true,out groundSupportIndices));g.Decoration.Append(TerrainArchitecture.Roads(plan,tx,tz));if(house!=null)houseData=TerrainHouseArchitecture.Build(house,td,plan);return g;});
                while(!job.IsCompleted){if(version!=epoch)yield break;yield return null;}if(version!=epoch)yield break;
                try{data=job.TakeCompleted();}catch(Exception e){Debug.LogException(e,this);Exit();yield break;}
#endif
                yield return UploadTile(version,tx,tz,tr,td,groundSupportIndices,plan,house,houseData,data);
                p=StreamPosition();cx=TerrainRegionPlan.Cell(p.x);cz=TerrainRegionPlan.Cell(p.z);
                // Release local movement only after the complete collision neighbourhood is committed.
                if(!bootstrapped)bootstrapped=CompleteRadius(cx,cz,1)>=1;
                yield return null;
            }
            worker=null;
        }
        private void DiscardStagedTile()
        {
            if(stagingTile==null)return;
            stagingTile.SetActive(false);Destroy(stagingTile);stagingTile=null;
        }
        private bool TileBuildCurrent(int version,int x,int z,int resolution,bool detailed)
        {
            if(version!=epoch||RegionRoot==null||(!IsInside&&!IsConnected)||IsConnected&&connectedSuspended)return false;
            Vector3 p=StreamPosition();int cx=TerrainRegionPlan.Cell(p.x),cz=TerrainRegionPlan.Cell(p.z);Tile existing;
            bool found=loaded.TryGetValue(TerrainRegionPlan.Key(x,z),out existing);
            return TerrainStreamingPolicy.CanPublish(Math.Max(Math.Abs(x-cx),Math.Abs(z-cz)),
                Mathf.Max(Mathf.Abs(x*TerrainRegionPlan.TileSize-p.x),Mathf.Abs(z*TerrainRegionPlan.TileSize-p.z)),
                world.TerrainRadius,resolution,detailed,found?existing.Resolution:0,found&&existing.Detailed);
        }
        private IEnumerator UploadTile(int version,int tx,int tz,int tr,bool td,int groundSupportIndices,
            TerrainRegionPlan plan,TerrainHousePlan house,TerrainHouseGeometry houseData,RoomGeometry data)
        {
            if(!TileBuildCurrent(version,tx,tz,tr,td))yield break;
            stagingTile=new GameObject("Meadow "+tx+","+tz+(td?" · near":" · distant"));var tileGo=stagingTile;tileGo.SetActive(false);tileGo.transform.SetParent(RegionRoot,false);tileGo.transform.localPosition=new Vector3(tx*TerrainRegionPlan.TileSize,0,tz*TerrainRegionPlan.TileSize);
            try
            {
                var owner=tileGo.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];owner.cellX=plan.AddressX;owner.cellZ=plan.AddressZ;tileGo.AddComponent<PoolroomsTerrainChunk>();
                var stagedPickups=new List<PoolroomsPickup>();
                MeshFactory.Create("Meadow ground",data.Structure,tileGo.transform,world.profile,owner,false,false,materials.ForGround(VisualQuality));

                if(td)yield return null;
                if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                if(td)
                {
                    // The visual skirts hide LOD cracks below the surface. They must not become
                    // vertical collision faces at a walking seam.
                    MeshFactory.CreateCollider("Meadow support without skirts",data.Structure,tileGo.transform,owner,groundSupportIndices);
                }

                if(td)yield return null;
                if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                MeshFactory.Create("Winding lanes and driveways",data.Decoration,tileGo.transform,world.profile,owner,false,false,materials.Ground);

                if(td)yield return null;
                if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                if(houseData!=null)
                {
                    var b=house.Blueprint;
                    var home=new GameObject("House · "+(b==null?"classic":b.Name+" #"+b.Id)+" · "+house.Floors+" floors"+(b!=null&&b.Basements>0?" + cellar":"")+(b!=null&&b.Attic?" + attic":""));home.transform.SetParent(tileGo.transform,false);home.transform.localPosition=new Vector3(house.X-tx*TerrainRegionPlan.TileSize,house.Base,house.Z-tz*TerrainRegionPlan.TileSize);
                    if(td)
                    {
                        if(world.BuildColliders)MeshFactory.CreateCollider("House collision support",houseData.Geometry.Structure,home.transform,owner);

                        if(td)yield return null;
                        if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                        MeshFactory.Create("House shell",houseData.Envelope.Structure,home.transform,world.profile,owner,false,false,materials.Houses[house.Variant]);

                        if(td)yield return null;
                        if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                        MeshFactory.Create("House roof",houseData.Roof,home.transform,world.profile,owner,false,false,materials.Roof);

                        if(td)yield return null;
                        if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                        MeshFactory.Create("House inside wall finish",houseData.WallFinish,home.transform,world.profile,owner,false,false,materials.Interior);

                        if(td)yield return null;
                        if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                        MeshFactory.Create("House interior",houseData.Interior.Structure,home.transform,world.profile,owner,false,false,materials.Interior);

                        if(td)yield return null;
                        if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                        MeshFactory.Create("House interior details",houseData.Interior.Decoration,home.transform,world.profile,owner,false,false,materials.Interior);

                        if(td)yield return null;
                        if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                    }
                    else
                    {
                        MeshFactory.Create("House shell",houseData.Geometry.Structure,home.transform,world.profile,owner,false,false,materials.Houses[house.Variant]);
                        if(td)yield return null;
                        if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                    }
                    // Distant houses retain their old three-renderer budget; no furniture or trim growth.
                    MeshFactory.Create("House details",houseData.Envelope.Decoration,home.transform,world.profile,owner,false,false,materials.Houses[house.Variant]);

                    if(td)yield return null;
                    if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                    MeshFactory.Create("House windows",houseData.Geometry.Glass,home.transform,world.profile,owner,td,false,materials.Glass);

                    if(td)yield return null;
                    if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                    if(td)
                    {
                        PoolroomsArchitecturalDetail.Attach(houseData.Geometry.Dressing,home.transform,world.profile,owner,world);
                        // Fixtures use house-local coordinates. Keep registration in the same owner space.
                        V3 offset=new V3(house.X-tx*TerrainRegionPlan.TileSize,house.Base,house.Z-tz*TerrainRegionPlan.TileSize);
                        foreach(var f in houseData.Geometry.Fixtures){var fixture=f;fixture.Position+=offset;data.Fixtures.Add(fixture);}
                        var carry=Explorer!=null?Explorer.GetComponent<PoolroomsCarry>():null;
                        foreach(var seat in houseData.Geometry.SourceSeats)
                        {
                            string key=plan.ItemKey(house,seat.Slot);
                            if(carry==null||!carry.WasTaken(key))stagedPickups.Add(PoolroomsPickup.CreateSourceSeat(key,seat,home.transform));
                            yield return null;if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                        }
                        int itemBatch=0;foreach(var item in houseData.Items)
                        {
                            string key=plan.ItemKey(house,item.Slot);
                            if(carry==null||!carry.WasTaken(key))stagedPickups.Add(PoolroomsPickup.CreateCatalog(key,item.CatalogId,PoolroomsMallSystem.ToUnity(item.Position),home.transform,world.profile));
                            if(++itemBatch%4==0){yield return null;if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}}
                        }
                    }
                }
                if(!TileBuildCurrent(version,tx,tz,tr,td)){DiscardStagedTile();yield break;}
                // The player may have picked up an old copy while this inactive replacement
                // was being assembled. Recheck ownership immediately before publication.
                var currentCarry=Explorer!=null?Explorer.GetComponent<PoolroomsCarry>():null;
                if(currentCarry!=null)foreach(var item in stagedPickups)if(item!=null&&currentCarry.WasTaken(item.SpawnKey))
                {item.gameObject.SetActive(false);Destroy(item.gameObject);}
                long k=TerrainRegionPlan.Key(tx,tz);Tile prior;
                if(loaded.TryGetValue(k,out prior)){prior.Chunk.gameObject.SetActive(false);Destroy(prior.Chunk.gameObject);}
                var stagedGround=tileGo.transform.Find("Meadow ground");
                if(stagedGround!=null)stagedGround.GetComponent<MeshRenderer>().sharedMaterial=materials.ForGround(VisualQuality)[0];
                tileGo.SetActive(true);stagingTile=null;
                loaded[k]=new Tile{Chunk=owner,X=tx,Z=tz,Resolution=tr,Detailed=td};world.RegisterExternalChunk(owner,data.Fixtures);
            }
            finally
            {
                if(stagingTile==tileGo)DiscardStagedTile();
            }
        }
    }
}
