using System;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    // Reuses the terrain/house streamer. The region occupies reserved cells at the common datum;
    // walking across its threshold never teleports, hides rooms, or rebuilds the saved profile.
    public sealed partial class PoolroomsTerrainSystem
    {
        private ConnectedTerrainSite connectedSite;
        private bool connectedSuspended,ownsConnectedDaylight;
        private readonly GameObject[] connectedCaps=new GameObject[4];
        private readonly PoolroomsChunk[] connectedBridges=new PoolroomsChunk[4];
        private bool ConnectedWorld { get { return world!=null&&world.unifiedExploration&&world.CurrentLayout!=null&&world.CurrentLayout.Config.Continuous; } }
        public bool ConnectedMode { get { return world!=null&&world.CurrentLayout!=null&&ConnectedTerrainTopology.Enabled(world.CurrentLayout.Config); } }
        public bool IsConnected { get { return RegionRoot!=null&&Plan!=null&&Plan.Connected; } }
        public ConnectedTerrainSite ConnectedSite { get { return connectedSite; } }
        public void ShiftConnectedOrigin(Vector3 delta){if(IsConnected)RegionRoot.position+=delta;}
        private Vector3 StreamPosition()
        {
            Vector3 p=RegionRoot.InverseTransformPoint(world.viewer.position);
            if(Plan.Connected){p.x=Mathf.Clamp(p.x,-Plan.HalfWidth+.01f,Plan.HalfWidth-.01f);p.z=Mathf.Clamp(p.z,-Plan.HalfDepth+.01f,Plan.HalfDepth-.01f);}
            return p;
        }
        public void TickConnected(Vector2Int focus,bool suspended)
        {
            if(!Application.isPlaying||!isActiveAndEnabled||!ConnectedMode||world.viewer==null)return;
            connectedSuspended=suspended;
            ConnectedTerrainSite site;
            if(!suspended&&ConnectedTerrainTopology.Find(world.CurrentLayout.Config,focus.x,focus.y,world.LoadRadius+2,out site))
            {
                if(!IsConnected||connectedSite.Key!=site.Key){if(RegionRoot!=null)Exit();BeginConnected(site);}
            }
            else if(IsConnected&&connectedSite.Distance(focus.x,focus.y)>world.UnloadRadius+3&&!suspended)ClearConnected();
            if(!IsConnected)return;
            RegionRoot.gameObject.SetActive(!suspended);
            Vector3 p=RegionRoot.InverseTransformPoint(world.viewer.position);
            SetConnectedDaylight(!suspended&&Plan.Contains(p.x,p.z));
            var house=Plan.HouseAt(p.x,p.y,p.z);
            InsideHouse=IsInside&&house!=null&&house.Contains(p.x,p.y,p.z);
            if(acousticRoom!=null){acousticRoom.X=focus.x;acousticRoom.Z=focus.y;}
            for(int d=0;d<4;d++)if(connectedCaps[d]!=null)
            {
                bool closed=!world.IsRoomLoaded(connectedSite.GateX(d),connectedSite.GateZ(d));
                if(connectedCaps[d].activeSelf!=closed)connectedCaps[d].SetActive(closed);
            }
        }
        private void BeginConnected(ConnectedTerrainSite site)
        {
            connectedSite=site;EnsureMaterials();Explorer=world.viewer.GetComponentInParent<PoolroomsExplorer>();
            Plan=TerrainRegionPlan.CreateConnected(world.CurrentLayout.Config.Seed,site,world.CurrentLayout.Config.ExpandedVariety);
            acousticRoom=new Room{X=site.X,Z=site.Z,Width=12,Depth=12,Height=3.2f,Environment=LiminalEnvironment.Hotel,
                Plan=new ProceduralPlan(LiminalEnvironment.Hotel,new PlannedModule[0],new WaterRegion[0],4,0,0)};
            var root=new GameObject("Nivel 94 conectado · "+(site.HalfWidth*2)+" x "+(site.HalfDepth*2)+" m · "+site.X+","+site.Z);
            RegionRoot=root.transform;RegionRoot.SetParent(transform,false);RegionRoot.position=world.CellWorldPosition(site.X,site.Z);
            var loose=new GameObject("Loose meadow objects");LooseRoot=loose.transform;LooseRoot.SetParent(RegionRoot,false);
            var sun=new GameObject("Nivel 94 daylight");sun.transform.SetParent(RegionRoot,false);sun.transform.localRotation=Quaternion.Euler(Plan.SunPitch,Plan.SunYaw,0);
            Sun=sun.AddComponent<Light>();Sun.type=LightType.Directional;Sun.color=Plan.Daylight==TerrainDaylight.WarmAfternoon?new Color(1,.89f,.73f):new Color(1,.97f,.91f);Sun.intensity=1.1f;Sun.shadows=LightShadows.Soft;Sun.enabled=false;
            var adapter=Type.GetType("Poolrooms.HDRP.PoolroomsTerrainAtmosphere, Poolrooms.HDRP");
            if(adapter!=null&&GraphicsSettings.currentRenderPipeline!=null&&GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline"))
            {var component=GetComponent(adapter);if(component==null)component=gameObject.AddComponent(adapter);atmosphere=component as IPoolroomsTerrainAtmosphere;}
            var config=world.CurrentLayout.Config;
            for(int d=0;d<4;d++)
            {
                Room gateway=world.CurrentLayout.At(site.GateX(d),site.GateZ(d));
                var data=ConnectedTerrainArchitecture.Bridge(site,gateway,config);
                var gallery=new GameObject("Meadow threshold "+d);gallery.transform.SetParent(RegionRoot,false);
                var owner=gallery.AddComponent<PoolroomsChunk>();connectedBridges[d]=owner;owner.waterRegions=new WaterRegion[0];owner.cellX=site.X;owner.cellZ=site.Z;gallery.AddComponent<PoolroomsTerrainChunk>();
                MeshFactory.Create("Threshold architecture",data.Structure,gallery.transform,world.profile,owner,world.BuildColliders,false);
                MeshFactory.Create("Threshold details",data.Decoration,gallery.transform,world.profile,owner,false,false);
                // Temporary closure on the landscape side until the matching indoor room exists.
                V3 p=ConnectedTerrainArchitecture.Point(site,gateway,config,1);var cap=new MeshData();
                float hx=d%2==0?config.DoorWidth*.5f:.15f,hz=d%2==0?.15f:config.DoorWidth*.5f;
                cap.Box(new V3(p.X-hx,0,p.Z-hz),new V3(p.X+hx,config.DoorHeight,p.Z+hz),Surface.Plaster,config.TextureWorldSize);
                cap.Theme=SurfaceTheme.WaitingHall;
                connectedCaps[d]=MeshFactory.Create("Awaiting indoor support",cap,gallery.transform,world.profile,owner,world.BuildColliders,false);
                world.RegisterExternalChunk(owner,data.Fixtures);
            }
            IsInside=false;InsideHouse=false;bootstrapped=false;horizonRadius=-1;epoch++;
            worker=StartCoroutine(Stream(epoch));world.ExternalGeometryChanged();
        }
        public bool GatewayReady(ConnectedTerrainSite site,int x,int z)
        {
            if(!IsConnected||connectedSuspended||connectedSite.Key!=site.Key||!site.Gateway(x,z))return false;
            int d=site.GateDirection(x,z);float px=Directions.DX[d]*(site.HalfWidth-.5f),pz=Directions.DZ[d]*(site.HalfDepth-.5f);
            Tile tile;return loaded.TryGetValue(TerrainRegionPlan.Key(TerrainRegionPlan.Cell(px),TerrainRegionPlan.Cell(pz)),out tile)&&tile.Detailed;
        }
        public bool VisitConnected()
        {
            if(!ConnectedMode||!Application.isPlaying||world.viewer==null)return false;
            Explorer=world.viewer.GetComponentInParent<PoolroomsExplorer>();if(Explorer==null)return false;
            if(world.Malls!=null&&world.Malls.IsInside)world.Malls.Exit();
            Vector2Int focus=world.ViewerCell;ConnectedTerrainSite best;
            // Search in space, not traversal history; never fabricate an overlapping district for TP.
            if(!ConnectedTerrainTopology.Nearest(world.CurrentLayout.Config,focus.x,focus.y,out best))return false;
            if(!IsConnected||connectedSite.Key!=best.Key){if(RegionRoot!=null)Exit();BeginConnected(best);}
            connectedSuspended=false;RegionRoot.gameObject.SetActive(true);Explorer.Teleport(Spawn);SetConnectedDaylight(true);world.ExternalGeometryChanged();return true;
        }
        public bool ReturnToConnectedGateway()
        {
            if(!IsConnected||Explorer==null)return false;
            Vector3 p=RegionRoot.InverseTransformPoint(world.viewer.position);int best=0;float distance=float.MaxValue;
            for(int d=0;d<4;d++)
            {float dx=Directions.DX[d]*(connectedSite.RadiusX+1)*world.CurrentLayout.Config.CellSize-p.x,dz=Directions.DZ[d]*(connectedSite.RadiusZ+1)*world.CurrentLayout.Config.CellSize-p.z;float n=dx*dx+dz*dz;if(n<distance){distance=n;best=d;}}
            Explorer.Teleport(world.CellWorldPosition(connectedSite.GateX(best),connectedSite.GateZ(best))+Vector3.up*.1f);SetConnectedDaylight(false);return true;
        }
        private void SetConnectedDaylight(bool inside)
        {
            IsInside=inside;
            // Directional sunlight needs occlusion in the shared world. At zero shadow quality,
            // the terrain sky provides ambient daylight instead of light passing through room roofs.
            int shadows=world.shadowLightBudgetOverride>=0?world.shadowLightBudgetOverride:world.profile.maxShadowLights;
            if(Sun!=null)Sun.enabled=inside&&!connectedSuspended&&shadows>0;
            if(inside==ownsConnectedDaylight)return;
            var master=Explorer!=null?Explorer.GetComponent<PoolroomsMasterView>():null;master?.ReleaseCameraDistance();
            if(inside)
            {
                cameraView=Explorer!=null&&Explorer.eyes!=null?Explorer.eyes.GetComponent<Camera>():null;
                if(cameraView!=null){oldFar=cameraView.farClipPlane;cameraView.farClipPlane=Mathf.Max(oldFar,1800);}
            }
            else
            {atmosphere?.Release();if(cameraView!=null)cameraView.farClipPlane=oldFar;cameraView=null;InsideHouse=false;}
            ownsConnectedDaylight=inside;
        }
        private void ClearConnected()
        {
            SetConnectedDaylight(false);bootstrapped=false;epoch++;
            if(worker!=null)StopCoroutine(worker);worker=null;DiscardStagedTile();
            if(RegionRoot!=null){RegionRoot.gameObject.SetActive(false);Destroy(RegionRoot.gameObject);}
            RegionRoot=null;LooseRoot=null;Sun=null;Plan=null;horizon=null;acousticRoom=null;loaded.Clear();horizonX=horizonZ=int.MinValue;horizonRadius=-1;
            for(int d=0;d<4;d++){connectedCaps[d]=null;connectedBridges[d]=null;}
            world.ExternalGeometryChanged();
        }
    }
}
