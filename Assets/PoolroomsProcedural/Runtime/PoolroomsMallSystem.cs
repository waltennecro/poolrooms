using System;
using System.Collections;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsMallSystem : MonoBehaviour
    {
        private PoolroomsWorld world;
        private PoolroomsExplorer explorer;
        private Transform interior;
        private Room entrance;
        public Room AcousticRoom { get; private set; }
        private MallPlan plan;
        private readonly Dictionary<int,PoolroomsChunk> loaded=new Dictionary<int,PoolroomsChunk>();
        private readonly List<int> remove=new List<int>();
        private readonly Dictionary<int,PoolroomsMallDisplay> displays=new Dictionary<int,PoolroomsMallDisplay>();
        private readonly GeometryBuildJob job=new GeometryBuildJob();
        private Coroutine worker;
        private int epoch;
        private bool shellReady;
        private PoolroomsChunk shell;
        public bool IsInside { get; private set; }
        public int LoadedCount { get { return loaded.Count; } }
        public Transform InteriorRoot { get { return interior; } }
        public MallPlan Plan { get { return plan; } }
        public IEnumerable<PoolroomsChunk> Chunks
        {get{if(shell!=null)yield return shell;foreach(var c in loaded.Values)yield return c;}}
        public Vector3 Spawn { get { return interior!=null?interior.TransformPoint(ToUnity(plan.Spawn)):world.transform.position+Vector3.up*.1f; } }
        public bool ViewerReady
        {
            get
            {
                if(!IsInside||!shellReady||interior==null||world.viewer==null)return false;
                Vector3 p=interior.InverseTransformPoint(world.viewer.position);
                int floor=plan.FloorAt(p.y),sector=plan.SectorAt(p.x);
                // Match the connected mall's safety neighbourhood before entering a bay or stairs.
                for(int f=Math.Max(0,floor-1);f<=Math.Min(plan.Floors-1,floor+1);f++)
                for(int s=Math.Max(0,sector-1);s<=Math.Min(plan.SectorCount-1,sector+1);s++)
                    if(!loaded.ContainsKey(Key(f,s)))return false;
                return true;
            }
        }
        private void Awake(){world=GetComponent<PoolroomsWorld>();}
        private void OnEnable(){if(IsInside&&worker==null)worker=StartCoroutine(Stream(epoch));}
        private void OnDisable(){if(Application.isPlaying)Exit();}
        public static Vector3 ToUnity(V3 p){return new Vector3(p.X,p.Y,p.Z);}
        private static int Key(int floor,int sector){return floor*32+sector;}
        public bool HasEntrance(Room room)
        {
            if(world==null)world=GetComponent<PoolroomsWorld>();
            return (!world.unifiedExploration||world.CurrentLayout==null||!world.CurrentLayout.Config.Continuous)&&(world.CurrentLayout==null||!ConnectedMallTopology.Enabled(world.CurrentLayout.Config))&&world.profile!=null&&world.profile.enableGiantMalls&&room!=null&&room.Environment==LiminalEnvironment.Supermarket&&
                (LiminalRegions.Hash(world.CurrentLayout!=null?world.CurrentLayout.Config.Seed:world.profile.seed,room.X,room.Z,0xCA112u)>>8)*(1f/16777216f)<Mathf.Clamp01(world.profile.mallEntranceChance);
        }
        public void AddEntrance(Room room,Transform parent)
        {
            if(!Application.isPlaying||!HasEntrance(room)||parent.GetComponentInChildren<PoolroomsMallPortal>(true)!=null)return;
            PoolroomsMallPortal.Create(this,room,false,new Vector3(0,0,-room.Depth*.5f+2.5f),parent,world.profile);
        }
        public bool VisitNearest()
        {
            if(world==null)world=GetComponent<PoolroomsWorld>();
            if(world.ConnectedMalls!=null&&world.CurrentLayout!=null&&ConnectedMallTopology.Enabled(world.CurrentLayout.Config))return world.ConnectedMalls.VisitNearest();
            if(world!=null&&world.unifiedExploration&&world.CurrentLayout!=null&&world.CurrentLayout.Config.Continuous)return false;
            if(world==null)world=GetComponent<PoolroomsWorld>();
            if(world.profile==null||!world.profile.enableGiantMalls||world.CurrentLayout==null||world.viewer==null)return false;
            if(IsInside)return true;
            var config=world.CurrentLayout.Config;
            if(world.profile.mallEntranceChance<=0||config.Procedural!=LiminalGeneration.Supermarkets&&config.Procedural!=LiminalGeneration.MixedWithRetail&&config.Procedural!=LiminalGeneration.Expanded||
                (config.Procedural==LiminalGeneration.MixedWithRetail||config.Procedural==LiminalGeneration.Expanded)&&(config.SupermarketShare<=0||config.Procedural==LiminalGeneration.Expanded&&config.AdditionalEnvironmentShare>=1))return false;
            Room focus=world.RoomAtViewer();int cx=focus!=null?focus.X:0,cz=focus!=null?focus.Z:0;
            Room found=null;
            if(!world.CurrentLayout.Config.Continuous)
            {foreach(var room in world.CurrentLayout.Rooms)if(HasEntrance(room)){found=room;break;}}
            else
            {
                for(int r=0;r<=128&&found==null;r++)for(int offset=-r;offset<=r&&found==null;offset++)for(int side=0;side<4;side++)
                {
                    long x=(long)cx+(side%2==0?offset:side==1?r:-r),z=(long)cz+(side%2==1?offset:side==0?r:-r);
                    if(x<int.MinValue||x>int.MaxValue||z<int.MinValue||z>int.MaxValue)continue;
                    if(LiminalRegions.At(config,(int)x,(int)z)!=LiminalEnvironment.Supermarket)continue;
                    if((LiminalRegions.Hash(config.Seed,(int)x,(int)z,0xCA112u)>>8)*(1f/16777216f)>=Mathf.Clamp01(world.profile.mallEntranceChance))continue;
                    var room=world.CurrentLayout.At((int)x,(int)z);if(HasEntrance(room)){found=room;break;}
                }
                world.CurrentLayout.TrimNeighbourhood(cx,cz,world.UnloadRadius+2);
            }
            return found!=null&&Enter(found);
        }
        public bool Enter(Room room)
        {
            if(world!=null&&world.ConnectedMalls!=null&&world.CurrentLayout!=null&&ConnectedMallTopology.Enabled(world.CurrentLayout.Config))return world.ConnectedMalls.VisitNearest();
            if(world!=null&&world.unifiedExploration&&world.CurrentLayout!=null&&world.CurrentLayout.Config.Continuous)return false;
            if(!Application.isPlaying||!isActiveAndEnabled||world==null||world.viewer==null||room==null)return false;
            explorer=world.viewer.GetComponentInParent<PoolroomsExplorer>();if(explorer==null)return false;
            if(IsInside)Exit();
            if(world.Terrain!=null&&world.Terrain.IsInside)world.Terrain.Exit();
            entrance=room;plan=MallPlan.Create(world.CurrentLayout.Config.Seed,room.X,room.Z,world.profile.enhancedMallArchitecture,world.profile.refinedMallArchitecture,world.profile.autonomousGeneration);
            AcousticRoom=new Room{X=room.X,Z=room.Z,Environment=LiminalEnvironment.Supermarket,Width=plan.Width,Depth=plan.Depth,Height=MallPlan.FloorHeight,DetailSeed=room.DetailSeed,
                Plan=new ProceduralPlan(LiminalEnvironment.Supermarket,new PlannedModule[0],new WaterRegion[0],3,0,3)};
            if(!plan.Valid()){Debug.LogError("Poolrooms: invalid mall plan.",this);return false;}
            var root=new GameObject("Centro comercial 2010 · "+plan.Floors+" pisos · "+room.X+", "+room.Z);
            interior=root.transform;interior.SetParent(world.transform,false);interior.localPosition=new Vector3(0,-400,0);
            IsInside=true;shellReady=false;epoch++;
            if(world.GeneratedRoot!=null)world.GeneratedRoot.gameObject.SetActive(false);
            var carry=explorer.GetComponent<PoolroomsCarry>();if(carry!=null)carry.ShowExteriorObjects(false);
            explorer.Teleport(Spawn);world.ExternalGeometryChanged();
            PoolroomsMallPortal.Create(this,null,true,new Vector3(-plan.ShopHalf-17,0,-25),interior,world.profile);
            worker=StartCoroutine(Stream(epoch));return true;
        }
        public void Exit(Vector3? destination=null)
        {
            if(!IsInside)return;
            Vector3 target=destination??(entrance!=null?world.RoomWorldPosition(entrance)+new Vector3(0,.1f,-entrance.Depth*.5f+5):world.transform.position+Vector3.up*.1f);
            IsInside=false;
            if(world.GeneratedRoot!=null)world.GeneratedRoot.gameObject.SetActive(true);
            var carry=explorer!=null?explorer.GetComponent<PoolroomsCarry>():null;if(carry!=null)carry.ShowExteriorObjects(true);
            epoch++;if(worker!=null)StopCoroutine(worker);worker=null;
            if(interior!=null){interior.gameObject.SetActive(false);Destroy(interior.gameObject);}
            interior=null;shell=null;loaded.Clear();displays.Clear();shellReady=false;
            if(explorer!=null)explorer.Teleport(target);world.ExternalGeometryChanged();
        }
        private IEnumerator Stream(int version)
        {
            MallPlan target=plan;
            // A cancelled coroutine does not cancel its background Task. Observe/discard it first.
            while(job.HasWork&&!job.IsCompleted)yield return null;
            if(job.HasWork)job.DiscardCompleted();if(version!=epoch)yield break;
            if(!shellReady)
            {
                RoomGeometry data=null;
#if UNITY_WEBGL && !UNITY_EDITOR
                data=MallArchitecture.Shell(target);
#else
                job.Start(()=>MallArchitecture.Shell(target));while(!job.IsCompleted){if(version!=epoch)yield break;yield return null;}
                if(version!=epoch)yield break;
                try{data=job.TakeCompleted();}catch(Exception error){Debug.LogException(error,this);Exit();yield break;}
#endif
                shell=Attach("Mall structural shell",data,0);shellReady=true;yield return null;
            }
            while(IsInside&&version==epoch)
            {
                Vector3 p=interior.InverseTransformPoint(world.viewer.position);int floor=target.FloorAt(p.y),sector=target.SectorAt(p.x);
                int radius=world.MallRadius,floorRadius=world.MallFloorRadius;remove.Clear();
                foreach(var pair in loaded)if(Math.Abs(pair.Key/32-floor)>floorRadius||Math.Abs(pair.Key%32-sector)>radius+1)remove.Add(pair.Key);
                bool removed=false;
                foreach(int key in remove){var old=loaded[key];loaded.Remove(key);displays.Remove(key);old.gameObject.SetActive(false);Destroy(old.gameObject);removed=true;}
                if(removed)world.ExternalGeometryChanged();
                int nextFloor=-1,nextSector=-1,best=int.MaxValue;
                for(int f=Math.Max(0,floor-floorRadius);f<=Math.Min(target.Floors-1,floor+floorRadius);f++)for(int s=Math.Max(0,sector-radius);s<=Math.Min(target.SectorCount-1,sector+radius);s++)
                {
                    int score=(f-floor)*(f-floor)*4+(s-sector)*(s-sector);
                    if(!loaded.ContainsKey(Key(f,s))&&score<best){best=score;nextFloor=f;nextSector=s;}
                }
                if(nextFloor<0){yield return null;continue;}
                MallSectorGeometry description=null;int buildFloor=nextFloor,buildSector=nextSector,quality=world.profile.curveSegments;
#if UNITY_WEBGL && !UNITY_EDITOR
                description=MallArchitecture.Sector(target,buildFloor,buildSector,quality);
#else
                job.Start(()=>{description=MallArchitecture.Sector(target,buildFloor,buildSector,quality);return description.Geometry;});
                while(!job.IsCompleted){if(version!=epoch)yield break;yield return null;}
                if(version!=epoch)yield break;
                try{job.TakeCompleted();}catch(Exception error){Debug.LogException(error,this);Exit();yield break;}
#endif
                // Teleports/fast noclip may have changed focus while this pure mesh was building.
                p=interior.InverseTransformPoint(world.viewer.position);
                if(Math.Abs(target.FloorAt(p.y)-buildFloor)>world.MallFloorRadius||Math.Abs(target.SectorAt(p.x)-buildSector)>world.MallRadius){yield return null;continue;}
                float baseY=buildFloor*MallPlan.FloorHeight;
                description.Geometry.Structure.TranslateRotate(new V3(0,-baseY,0),0);description.Geometry.Decoration.TranslateRotate(new V3(0,-baseY,0),0);description.Geometry.Glass.TranslateRotate(new V3(0,-baseY,0),0);
                for(int i=0;i<description.Geometry.Fixtures.Count;i++){var light=description.Geometry.Fixtures[i];light.Position.Y-=baseY;description.Geometry.Fixtures[i]=light;}
                var chunk=Attach("Mall floor "+(buildFloor+1)+" / sector "+buildSector,description.Geometry,baseY);
                loaded.Add(Key(buildFloor,buildSector),chunk);
                var carry=explorer.GetComponent<PoolroomsCarry>();
                PoolroomsPickup.CreateMallSeats(target,description.Geometry,baseY,chunk.transform,carry);
                if(target.Enhanced)
                {
                    var display=chunk.gameObject.AddComponent<PoolroomsMallDisplay>();
                    display.Initialize(target,description,baseY,world.profile,chunk,carry);
                    displays.Add(Key(buildFloor,buildSector),display);
                }
                else foreach(var item in description.Pickups)
                {
                    string key="mall:"+target.Seed+":"+target.AddressX+":"+target.AddressZ+":"+item.Id;
                    if(carry==null||!carry.WasTaken(key))PoolroomsPickup.Create(key,ToUnity(item.Position)-Vector3.up*baseY,chunk.transform,world.profile);
                }
                yield return null;
            }
            worker=null;
        }
        public bool FindDisplayItem(Vector3 position,Vector3 direction,float maximum,out PoolroomsMallDisplay display,out int index,out float distance)
        {
            display=null;index=-1;distance=maximum;
            if(!IsInside)return false;
            foreach(var d in displays.Values)
            {
                int candidate;float t;
                if(d!=null&&d.Find(position,direction,distance,out candidate,out t))
                {display=d;index=candidate;distance=t;}
            }
            return display!=null;
        }
        private PoolroomsChunk Attach(string name,RoomGeometry data,float y)
        {
            var go=new GameObject(name);go.transform.SetParent(interior,false);go.transform.localPosition=Vector3.up*y;
            var chunk=go.AddComponent<PoolroomsChunk>();chunk.waterRegions=new WaterRegion[0];
            MeshFactory.Create("Architecture",data.Structure,go.transform,world.profile,chunk,world.BuildColliders);
            MeshFactory.Create("Batched decoration",data.Decoration,go.transform,world.profile,chunk,false);
            PoolroomsArchitecturalDetail.Attach(data.Dressing,go.transform,world.profile,chunk,world,-y);
            MeshFactory.Create("Windows and skylight glazing",data.Glass,go.transform,world.profile,chunk,world.BuildColliders);
            world.RegisterExternalChunk(chunk,data.Fixtures);return chunk;
        }
    }
}
