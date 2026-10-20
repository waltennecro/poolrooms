using System;
using System.Collections;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Shared spatial streaming: several mall districts can coexist with ordinary room chunks.
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsConnectedMalls : MonoBehaviour
    {
        private sealed class District
        {
            public ConnectedMallSite Site;
            public MallPlan Plan;
            public Room Acoustic;
            public Transform Root;
            public PoolroomsChunk Shell;
            public Coroutine Worker;
            public readonly GeometryBuildJob Job=new GeometryBuildJob();
            public readonly Dictionary<int,PoolroomsChunk> Sectors=new Dictionary<int,PoolroomsChunk>();
            public readonly Dictionary<int,PoolroomsMallDisplay> Displays=new Dictionary<int,PoolroomsMallDisplay>();
            public readonly List<PoolroomsChunk> Bridges=new List<PoolroomsChunk>();
            public readonly List<int> Remove=new List<int>();
            public readonly GameObject[] EdgeCaps=new GameObject[2];
        }
        private PoolroomsWorld world;
        private readonly Dictionary<long,District> districts=new Dictionary<long,District>();
        private readonly List<long> retired=new List<long>();
        private int lastUploadFrame=-1;
        private bool running;
        private int focusX=int.MinValue,focusZ=int.MinValue,viewRadius=-1;
        public int DistrictCount { get { return districts.Count; } }
        public int LoadedSectorCount { get { int n=0;foreach(var d in districts.Values)n+=d.Sectors.Count;return n; } }
        public IEnumerable<PoolroomsChunk> Chunks
        {
            get {foreach(var d in districts.Values){if(d.Shell!=null)yield return d.Shell;foreach(var b in d.Bridges)yield return b;foreach(var c in d.Sectors.Values)yield return c;}}
        }
        private void Awake(){world=GetComponent<PoolroomsWorld>();}
        private void OnDisable(){Clear();}
        public void Clear()
        {
            running=false;focusX=focusZ=int.MinValue;viewRadius=-1;
            foreach(var d in districts.Values)Release(d);
            districts.Clear();
        }
        private void Release(District d)
        {
            if(d.Worker!=null)StopCoroutine(d.Worker);d.Worker=null;
            if(d.Root!=null){d.Root.gameObject.SetActive(false);if(Application.isPlaying)Destroy(d.Root.gameObject);else DestroyImmediate(d.Root.gameObject);}
            d.Root=null;
            // Each district owns its abandoned job; a later district never consumes its result.
        }
        public void SetSuspended(bool suspended)
        {foreach(var d in districts.Values)if(d.Root!=null)if(d.Root.gameObject.activeSelf==suspended)d.Root.gameObject.SetActive(!suspended);}
        public void ShiftOrigin(Vector3 shift)
        {foreach(var d in districts.Values)if(d.Root!=null)d.Root.position+=shift;}
        public void Tick(Vector2Int focus)
        {
            if(world==null)world=GetComponent<PoolroomsWorld>();
            if(!Application.isPlaying||!isActiveAndEnabled||world.CurrentLayout==null||!ConnectedMallTopology.Enabled(world.CurrentLayout.Config))return;
            running=true;
            if(focus.x!=focusX||focus.y!=focusZ||viewRadius!=world.LoadRadius)
            {
                focusX=focus.x;focusZ=focus.y;viewRadius=world.LoadRadius;
                var config=world.CurrentLayout.Config;
                int radius=world.LoadRadius+2,bx=ConnectedMallTopology.Block(focus.x),bz=ConnectedMallTopology.Block(focus.y);
                retired.Clear();
                foreach(var pair in districts)if(pair.Value.Site.Distance(focus.x,focus.y)>radius+2)retired.Add(pair.Key);
                foreach(long key in retired){Release(districts[key]);districts.Remove(key);}
                for(int iz=bz-1;iz<=bz+1;iz++)for(int ix=bx-1;ix<=bx+1;ix++)
                {
                    ConnectedMallSite site;if(!ConnectedMallTopology.Site(config,ix,iz,out site)||site.Distance(focus.x,focus.y)>radius||districts.ContainsKey(site.Key))continue;
                    var d=new District{Site=site,Plan=MallPlan.Create(config.Seed,site.X,site.Z,true,true,config.AutonomousGeneration,config.ExpandedVariety,config.ExpandedBiomeAreas)};
                    var root=new GameObject("Connected mall · "+d.Plan.Floors+" floors · "+site.X+","+site.Z);d.Root=root.transform;d.Root.SetParent(world.transform,false);
                    d.Root.position=world.CellWorldPosition(site.X,site.Z);
                    d.Acoustic=new Room{X=site.X,Z=site.Z,Environment=LiminalEnvironment.Supermarket,Width=d.Plan.Width,Depth=d.Plan.Depth,Height=MallPlan.FloorHeight,
                        DetailSeed=StableRandom.Hash(config.Seed,site.X,site.Z),Plan=new ProceduralPlan(LiminalEnvironment.Supermarket,new PlannedModule[0],new WaterRegion[0],3,0,3)};
                    districts.Add(site.Key,d);d.Worker=StartCoroutine(Stream(d));
                }
            }
            if(retired.Count>0){world.ExternalGeometryChanged();retired.Clear();}
            foreach(var d in districts.Values)for(int i=0;i<2;i++)if(d.EdgeCaps[i]!=null)
                {bool closed=!world.IsRoomLoaded(d.Site.X+(i==0?-1:1)*(d.Site.RadiusX+1),d.Site.Z);if(d.EdgeCaps[i].activeSelf!=closed)d.EdgeCaps[i].SetActive(closed);}
        }
        private District AtPosition(Vector3 position,bool reservation)
        {
            foreach(var d in districts.Values)
            {
                if(d.Root==null||!d.Root.gameObject.activeInHierarchy)continue;
                Vector3 p=d.Root.InverseTransformPoint(position);
                float hx=reservation?(d.Site.RadiusX+.5f)*world.CurrentLayout.Config.CellSize:d.Plan.Width*.5f+.3f;
                float hz=reservation?(d.Site.RadiusZ+.5f)*world.CurrentLayout.Config.CellSize:d.Plan.Depth*.5f+.3f;
                if(Mathf.Abs(p.x)<=hx&&Mathf.Abs(p.z)<=hz)return d;
            }
            return null;
        }
        public bool ContainsViewer { get { return world!=null&&world.viewer!=null&&AtPosition(world.viewer.position,false)!=null; } }
        public Room AcousticRoom { get { var d=world!=null&&world.viewer!=null?AtPosition(world.viewer.position,true):null;return d!=null?d.Acoustic:null; } }
        public MallPlan ActivePlan { get { var d=world!=null&&world.viewer!=null?AtPosition(world.viewer.position,false):null;return d!=null?d.Plan:null; } }
        public Transform ActiveRoot { get { var d=world!=null&&world.viewer!=null?AtPosition(world.viewer.position,false):null;return d!=null?d.Root:null; } }
        public bool ViewerReady
        {
            get{return world!=null&&world.viewer!=null&&TryPositionReady(world.viewer.position,out bool ready)&&ready;}
        }
        public bool TryPositionReady(Vector3 position,out bool ready)
        {
            ready=false;var d=world!=null?AtPosition(position,true):null;if(d==null)return false;
            if(d.Shell==null||d.Bridges.Count!=2)return true;
            Vector3 p=d.Root.InverseTransformPoint(position);
            if(!ConnectedMallArchitecture.ContainsSupportFootprint(d.Site,world.CurrentLayout.Config,p.x,p.z))return true;
            int floor=d.Plan.FloorAt(p.y),sector=d.Plan.SectorAt(p.x);
            // Check the requested bay/floor, which may differ from the viewer's loaded bay.
            for(int f=Math.Max(0,floor-1);f<=Math.Min(d.Plan.Floors-1,floor+1);f++)
            for(int s=Math.Max(0,sector-1);s<=Math.Min(d.Plan.SectorCount-1,sector+1);s++)
                if(!d.Sectors.ContainsKey(f*32+s))return true;
            ready=true;return true;
        }
        public bool GatewayReady(ConnectedMallSite site,int side)
        {
            District d;if(!districts.TryGetValue(site.Key,out d)||d.Shell==null||d.Bridges.Count!=2)return false;
            int end=side<0?0:d.Plan.Bays+1;
            return d.Sectors.ContainsKey(end)&&d.Sectors.ContainsKey(end+(side<0?1:-1));
        }
        public void AppendStreamAnchors(List<RoomCell> anchors)
        {
            foreach(var d in districts.Values)if(d.Root!=null&&d.Root.gameObject.activeInHierarchy)
            {anchors.Add(new RoomCell(d.Site.X-d.Site.RadiusX-1,d.Site.Z));anchors.Add(new RoomCell(d.Site.X+d.Site.RadiusX+1,d.Site.Z));}
        }
        public bool VisitNearest()
        {
            if(world==null||world.viewer==null||world.CurrentLayout==null||!ConnectedMallTopology.Enabled(world.CurrentLayout.Config))return false;
            Vector2Int focus=world.Terrain!=null&&world.Terrain.IsInside?new Vector2Int(world.Terrain.Plan.AddressX,world.Terrain.Plan.AddressZ):world.ViewerCell;ConnectedMallSite best=default(ConnectedMallSite);long distance=long.MaxValue;bool found=false;
            int bx=ConnectedMallTopology.Block(focus.x),bz=ConnectedMallTopology.Block(focus.y);
            for(int ring=0;ring<=8&&!found;ring++)for(int iz=bz-ring;iz<=bz+ring;iz++)for(int ix=bx-ring;ix<=bx+ring;ix++)
            {
                ConnectedMallSite site;if(!ConnectedMallTopology.Site(world.CurrentLayout.Config,ix,iz,out site))continue;
                long dx=(long)site.X-focus.x,dz=(long)site.Z-focus.y,score=dx*dx+dz*dz;
                if(score>=distance)continue;best=site;distance=score;found=true;
            }
            if(!found)return false;
            // TP is a shortcut to an actual world address, never another hidden scene.
            var actor=world.viewer.GetComponentInParent<PoolroomsExplorer>();if(actor==null)return false;
            if(world.Terrain!=null&&world.Terrain.IsInside)world.Terrain.Exit();
            actor.Teleport(world.CellWorldPosition(best.X,best.Z)+new Vector3(0,.1f,-18));Tick(new Vector2Int(best.X,best.Z));return true;
        }
        public bool FindDisplayItem(Vector3 position,Vector3 direction,float maximum,out PoolroomsMallDisplay display,out int index,out float distance)
        {
            display=null;index=-1;distance=maximum;
            foreach(var d in districts.Values)if(d.Root!=null&&d.Root.gameObject.activeInHierarchy)foreach(var candidate in d.Displays.Values)
            {
                int slot;float t;if(candidate!=null&&candidate.Find(position,direction,distance,out slot,out t)){display=candidate;index=slot;distance=t;}
            }
            return display!=null;
        }
        private bool UploadAvailable(){if(lastUploadFrame==Time.frameCount)return false;lastUploadFrame=Time.frameCount;return true;}
        private IEnumerator Stream(District d)
        {
            LayoutConfig config=world.CurrentLayout.Config;
            RoomGeometry data=null;
#if UNITY_WEBGL && !UNITY_EDITOR
            data=MallArchitecture.Shell(d.Plan,config.DoorWidth,config.DoorHeight);
#else
            d.Job.Start(()=>MallArchitecture.Shell(d.Plan,config.DoorWidth,config.DoorHeight));
            while(!d.Job.IsCompleted){if(d.Root==null)yield break;yield return null;}
            try{data=d.Job.TakeCompleted();}catch(Exception e){Debug.LogException(e,this);yield break;}
#endif
            while(!UploadAvailable())yield return null;
            d.Shell=Attach(d,"Mall structural shell",data,0);yield return null;
            for(int side=-1;side<=1;side+=2)
            {
                Room gateway=world.CurrentLayout.At(d.Site.X+side*(d.Site.RadiusX+1),d.Site.Z);
                var bridge=ConnectedMallArchitecture.Bridge(d.Site,gateway,config);
                while(!UploadAvailable())yield return null;
                var owner=Attach(d,"Connected mall approach "+side,bridge,0);d.Bridges.Add(owner);
                V3 point=ConnectedMallArchitecture.Point(d.Site,gateway,config,0);
                var cap=new MeshData{Theme=SurfaceTheme.Mall};cap.Box(new V3(point.X-.15f,0,-config.DoorWidth*.5f),new V3(point.X+.15f,config.DoorHeight,config.DoorWidth*.5f),Surface.Plaster,config.TextureWorldSize);
                d.EdgeCaps[side<0?0:1]=MeshFactory.Create("Exterior support gate",cap,owner.transform,world.profile,owner,world.BuildColliders);
                world.ExternalGeometryChanged();yield return null;
            }
            while(running&&d.Root!=null&&world.viewer!=null)
            {
                if(!d.Root.gameObject.activeInHierarchy){yield return null;continue;}
                Vector3 p=d.Root.InverseTransformPoint(world.viewer.position);
                int floor=d.Plan.FloorAt(p.y),sector=d.Plan.SectorAt(p.x),radius=Math.Max(1,world.MallRadius),floors=Math.Max(1,world.MallFloorRadius);
                d.Remove.Clear();foreach(var pair in d.Sectors)if(Math.Abs(pair.Key/32-floor)>floors+1||Math.Abs(pair.Key%32-sector)>radius+1)d.Remove.Add(pair.Key);
                foreach(int key in d.Remove){var old=d.Sectors[key];d.Sectors.Remove(key);d.Displays.Remove(key);old.gameObject.SetActive(false);Destroy(old.gameObject);}
                if(d.Remove.Count>0)world.ExternalGeometryChanged();
                int nf=-1,ns=-1,best=int.MaxValue;
                for(int f=Math.Max(0,floor-floors);f<=Math.Min(d.Plan.Floors-1,floor+floors);f++)for(int s=Math.Max(0,sector-radius);s<=Math.Min(d.Plan.SectorCount-1,sector+radius);s++)
                {int score=(f-floor)*(f-floor)*4+(s-sector)*(s-sector);if(score<best&&!d.Sectors.ContainsKey(f*32+s)){best=score;nf=f;ns=s;}}
                if(nf<0){yield return null;continue;}
                int nextFloor=nf,nextSector=ns,quality=world.profile.curveSegments;MallSectorGeometry description=null;
#if UNITY_WEBGL && !UNITY_EDITOR
                description=MallArchitecture.Sector(d.Plan,nextFloor,nextSector,quality);
#else
                d.Job.Start(()=>{description=MallArchitecture.Sector(d.Plan,nextFloor,nextSector,quality);return description.Geometry;});
                while(!d.Job.IsCompleted){if(d.Root==null)yield break;yield return null;}
                try{d.Job.TakeCompleted();}catch(Exception e){Debug.LogException(e,this);yield break;}
#endif
                p=d.Root.InverseTransformPoint(world.viewer.position);
                if(Math.Abs(d.Plan.FloorAt(p.y)-nextFloor)>world.MallFloorRadius||Math.Abs(d.Plan.SectorAt(p.x)-nextSector)>world.MallRadius){yield return null;continue;}
                float y=nextFloor*MallPlan.FloorHeight;
                description.Geometry.Structure.TranslateRotate(new V3(0,-y,0),0);description.Geometry.Decoration.TranslateRotate(new V3(0,-y,0),0);description.Geometry.Glass.TranslateRotate(new V3(0,-y,0),0);
                for(int i=0;i<description.Geometry.Fixtures.Count;i++){var f=description.Geometry.Fixtures[i];f.Position.Y-=y;description.Geometry.Fixtures[i]=f;}
                while(!UploadAvailable())yield return null;
                // Other districts may have consumed the upload slot while the viewer moved.
                // Validate again after waiting, not only when the CPU task completed.
                if(d.Root==null||!d.Root.gameObject.activeInHierarchy){yield return null;continue;}
                p=d.Root.InverseTransformPoint(world.viewer.position);
                if(Math.Abs(d.Plan.FloorAt(p.y)-nextFloor)>world.MallFloorRadius||Math.Abs(d.Plan.SectorAt(p.x)-nextSector)>world.MallRadius){yield return null;continue;}
                var chunk=Attach(d,"Mall floor "+(nextFloor+1)+" / sector "+nextSector,description.Geometry,y);
                d.Sectors.Add(nextFloor*32+nextSector,chunk);
                var actor=world.viewer.GetComponentInParent<PoolroomsExplorer>();var carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
                PoolroomsPickup.CreateMallSeats(d.Plan,description.Geometry,y,chunk.transform,carry);
                var display=chunk.gameObject.AddComponent<PoolroomsMallDisplay>();display.Initialize(d.Plan,description,y,world.profile,chunk,carry);d.Displays.Add(nextFloor*32+nextSector,display);
                world.ExternalGeometryChanged();yield return null;
            }
        }
        private PoolroomsChunk Attach(District d,string name,RoomGeometry data,float y)
        {
            var go=new GameObject(name);go.transform.SetParent(d.Root,false);go.transform.localPosition=Vector3.up*y;
            var owner=go.AddComponent<PoolroomsChunk>();owner.cellX=d.Site.X;owner.cellZ=d.Site.Z;owner.connectedMall=true;owner.waterRegions=new WaterRegion[0];
            MeshFactory.Create("Architecture",data.Structure,go.transform,world.profile,owner,world.BuildColliders);
            MeshFactory.Create("Batched decoration",data.Decoration,go.transform,world.profile,owner,false);
            PoolroomsArchitecturalDetail.Attach(data.Dressing,go.transform,world.profile,owner,world,-y);
            if(world.CurrentLayout.Config.EncounterExploration)PoolroomsRoomLife.AttachNotice(world,go.transform,owner,data.Dressing,"mall-note:"+d.Plan.Seed+":"+d.Site.X+":"+d.Site.Z+":"+name,d.Plan.Seed,d.Site.X,d.Site.Z,-y);
            MeshFactory.Create("Windows and skylight glazing",data.Glass,go.transform,world.profile,owner,world.BuildColliders);
            world.RegisterExternalChunk(owner,data.Fixtures);return owner;
        }
    }
}
