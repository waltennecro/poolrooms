using System;
using System.Collections;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsWorld : MonoBehaviour
    {
        public PoolroomsProfile profile;
        [Header("Mundo liminal conectado / v29")]
        [InspectorName("Exploración unificada"),Tooltip("Incluye todos los niveles implementados con accesos caminables. Desactívalo para perfiles de prueba o generación personalizada.")] public bool unifiedExploration=true;
        private PoolroomsProfile sourceProfile,runtimeProfile;
        private PoolroomsConnectedMalls connectedMalls;
        public PoolroomsConnectedMalls ConnectedMalls { get { return connectedMalls; } }
        public bool InsideMall { get { return connectedMalls!=null&&connectedMalls.ContainsViewer||malls!=null&&malls.IsInside; } }
        public MallPlan ActiveMallPlan { get { return connectedMalls!=null&&connectedMalls.ContainsViewer?connectedMalls.ActivePlan:malls!=null&&malls.IsInside?malls.Plan:null; } }
        public Transform ActiveMallRoot { get { return connectedMalls!=null&&connectedMalls.ContainsViewer?connectedMalls.ActiveRoot:malls!=null&&malls.IsInside?malls.InteriorRoot:null; } }
        public int LoadedMallSectorCount { get { return connectedMalls!=null?connectedMalls.LoadedSectorCount:malls!=null?malls.LoadedCount:0; } }
        public Transform viewer;
        public bool generateOnStart=true,streaming=true;
        // Scene quality overrides; -1 keeps the generation profile budget. Never edits the shared asset.
        [NonSerialized] public int activeLightBudgetOverride=-1,shadowLightBudgetOverride=-1;
        [SerializeField,HideInInspector] private Transform generatedRoot;
        private readonly Dictionary<long,PoolroomsChunk> loaded=new Dictionary<long,PoolroomsChunk>();
        private readonly List<PoolroomsLuminaire> lamps=new List<PoolroomsLuminaire>();
        // Reused buffers: the streaming loop runs every frame and must not allocate.
        private readonly List<long> removeScratch=new List<long>();
        private readonly List<PoolroomsLuminaire> visibleLamps=new List<PoolroomsLuminaire>();
        private readonly List<RankedLamp> ranked=new List<RankedLamp>();
        private struct RankedLamp{public float Distance;public bool Eligible;public PoolroomsLuminaire Lamp;}
        // Distance first; the instance id breaks ties so equidistant lamps never swap on and off between updates.
        private static readonly Comparison<RankedLamp> ByDistance=(a,b)=>
            a.Distance!=b.Distance?a.Distance.CompareTo(b.Distance):a.Lamp.GetInstanceID().CompareTo(b.Lamp.GetInstanceID());
        private Layout layout;
        private readonly ContinuousOrigin origin=new ContinuousOrigin();
        private int cacheFocusX=int.MinValue,cacheFocusZ=int.MinValue,cacheRadius=-1;
        private int idleSearchX,idleSearchZ,idleSearchRadius,idleSearchCount,idleSearchGeneration=-1;
        private bool idleSearchLocal;
        public int WorkSearchCount { get; private set; }
        [NonSerialized] public MasterViewOptions administratorView; // Legacy API.
        [NonSerialized] public MasterViewOptions renderingView;
        private MasterViewOptions VisibilityOptions { get { return renderingView??administratorView; } }
        public int LoadRadius { get { return MasterViewPolicy.Radius(VisibilityOptions!=null?VisibilityOptions.roomRadius:0,profile!=null?profile.loadRadius:1,1,12); } }
        public int UnloadRadius { get { return Math.Max(Mathf.Clamp(profile!=null?profile.unloadRadius:2,2,13),LoadRadius+1); } }
        public int TerrainRadius { get { return MasterViewPolicy.Radius(VisibilityOptions!=null?VisibilityOptions.terrainRadius:0,profile!=null?profile.terrainViewRadius:3,2,8); } }
        public int MallRadius { get { return MasterViewPolicy.Radius(VisibilityOptions!=null?VisibilityOptions.mallRadius:0,profile!=null?profile.mallDetailRadius:1,1,14); } }
        public int MallFloorRadius { get { return MasterViewPolicy.Radius(VisibilityOptions!=null?VisibilityOptions.mallFloorRadius:0,1,1,5); } }
        public int LoadedRoomCount { get { return loaded.Count; } }
        private bool LocalRoomStreaming { get { return streaming||layout!=null&&layout.Config.Continuous||VisibilityOptions!=null&&VisibilityOptions.roomRadius>0; } }
        public bool ExtendedRoomView { get { return VisibilityOptions!=null&&VisibilityOptions.roomRadius>2&&!OutsideRooms; } }
        public bool UseNearEffects(PoolroomsChunk chunk)
        {
            if(!ExtendedRoomView||chunk==null||chunk.connectedMall||chunk.GetComponent<PoolroomsTerrainChunk>()!=null)return true;
            Vector2Int focus=Focus();return Math.Max(Math.Abs((long)chunk.cellX-focus.x),Math.Abs((long)chunk.cellZ-focus.y))<=2;
        }
        public string StreamingFailure {get;private set;}="";
        public string StreamingStatus {get{return !string.IsNullOrEmpty(StreamingFailure)?"Error de generación: "+StreamingFailure:layout==null?"Generador sin inicializar":viewer==null?"Falta el jugador de seguimiento":ViewerReady?"Zona preparada":"Generando suelo y conexiones";}}
        private Coroutine worker;
        public bool FrontendSuspended { get; set; }
        private IEnumerator SupervisedStream()
        {
            StreamingFailure="";var stream=Stream();
            while(true)
            {
                if(FrontendSuspended){yield return null;continue;}
                bool next=false;Exception failure=null;
                try{next=stream.MoveNext();}catch(Exception error){failure=error;}
                if(failure!=null){StreamingFailure=failure.GetBaseException().Message;Debug.LogException(failure,this);worker=null;yield break;}
                if(!next){worker=null;yield break;}yield return stream.Current;
            }
        }
        [ContextMenu("Reanudar generación en la posición actual")]
        public void ResumeStreaming()
        {if(!Application.isPlaying||!isActiveAndEnabled)return;if(layout==null&&!Initialize())return;if(generatedRoot==null)ClearGeometry();if(worker!=null)StopCoroutine(worker);worker=StartCoroutine(SupervisedStream());}
        private void ResolveViewer()
        {
            var actor=viewer!=null?viewer.GetComponentInParent<PoolroomsExplorer>():null;
            if(actor!=null&&(actor.world==null||actor.world==this)){actor.world=this;viewer=actor.transform;return;}
            PoolroomsExplorer candidate=null;int matches=0;
            foreach(var explorer in UnityEngine.Object.FindObjectsByType<PoolroomsExplorer>(FindObjectsSortMode.None))
            if(explorer.world==this||explorer.world==null){candidate=explorer;matches++;}
            if(matches==1){candidate.world=this;viewer=candidate.transform;}
        }

        private readonly GeometryBuildJob buildJob=new GeometryBuildJob();
        private int generation;
        private readonly List<RoomCell> connectionAnchors=new List<RoomCell>();
        private int anchorHash,idleAnchorHash;
        private void RefreshStreamAnchors()
        {
            connectionAnchors.Clear();if(connectedMalls!=null)connectedMalls.AppendStreamAnchors(connectionAnchors);
            if(terrain!=null&&terrain.IsConnected)for(int d=0;d<4;d++)connectionAnchors.Add(new RoomCell(terrain.ConnectedSite.GateX(d),terrain.ConnectedSite.GateZ(d)));
            unchecked{anchorHash=17;foreach(var p in connectionAnchors)anchorHash=(anchorHash*31+p.X)*31+p.Z;}
        }
        private bool NearConnection(int x,int z)
        {foreach(var p in connectionAnchors)if(Math.Abs((long)x-p.X)<=1&&Math.Abs((long)z-p.Z)<=1)return true;return false;}

        private float nextLightUpdate;
        private PoolroomsTerrainSystem terrain;
        public PoolroomsTerrainSystem Terrain { get { return terrain; } }
        private bool OutsideRooms { get { return malls!=null&&malls.IsInside||terrain!=null&&terrain.IsInside&&!terrain.IsConnected; } }
        private PoolroomsMallSystem malls;
        public PoolroomsMallSystem Malls { get { return malls; } }
        public event Action GeometryChanged;
        public event Action LightsChanged;
        public event Action<PoolroomsChunk> ChunkCreated;
        public Layout CurrentLayout { get { return layout; } }
        public Transform GeneratedRoot { get { return generatedRoot; } }
        public IReadOnlyList<PoolroomsLuminaire> Luminaires { get { return lamps; } }
        public IReadOnlyList<PoolroomsLuminaire> VisibleLuminaires { get { return visibleLamps; } }
        public IEnumerable<PoolroomsChunk> Chunks
        {
            get
            {
                if(terrain!=null&&terrain.IsInside&&!terrain.IsConnected){foreach(var c in terrain.Chunks)yield return c;yield break;}
                if(malls!=null&&malls.IsInside){foreach(var c in malls.Chunks)yield return c;yield break;}
                if(terrain!=null&&terrain.IsConnected)foreach(var c in terrain.Chunks)yield return c;
                if(connectedMalls!=null)foreach(var c in connectedMalls.Chunks)yield return c;
                if(loaded.Count>0){foreach(var c in loaded.Values)yield return c;}
                else if(generatedRoot!=null)foreach(var c in generatedRoot.GetComponentsInChildren<PoolroomsChunk>())
                    if(c.GetComponent<PoolroomsPickup>()==null&&c.GetComponent<PoolroomsMallPortal>()==null&&c.GetComponent<PoolroomsTerrainPortal>()==null)yield return c;
            }
        }
        public bool SpawnReady { get { return loaded.ContainsKey(0); } }
        public bool ViewerReady
        {
            get { if(terrain!=null&&terrain.IsInside)return terrain.ViewerReady;if(malls!=null&&malls.IsInside)return malls.ViewerReady;if(layout==null)return false;Vector2Int f=Focus();ConnectedMallSite site;
                ConnectedTerrainSite meadow;
                if(ConnectedTerrainTopology.At(layout.Config,f.x,f.y,out meadow))return terrain!=null&&terrain.IsConnected&&terrain.ViewerReady;
                if(ConnectedMallTopology.At(layout.Config,f.x,f.y,out site))return connectedMalls!=null&&connectedMalls.ViewerReady;
                Room r=layout.At(f.x,f.y);return r!=null&&loaded.ContainsKey(ContinuousTopology.CellKey(r.X,r.Z)); }
        }

        // Disabling colliders is useful for an editor preview, but never for a live Explorer.
        public bool BuildColliders { get { return profile!=null&&(profile.colliders||Application.isPlaying&&viewer!=null&&viewer.GetComponentInParent<PoolroomsExplorer>()!=null); } }
        private void EnsureChunkColliders(PoolroomsChunk chunk)
        {
            if(!BuildColliders)return;
            Transform architecture=chunk.transform.Find("Architecture");
            if(architecture!=null)EnsureCollider(architecture.gameObject);
            if(chunk.caps!=null)foreach(var cap in chunk.caps)if(cap!=null)EnsureCollider(cap);
            if(chunk.links!=null)foreach(var link in chunk.links)if(link!=null)EnsureCollider(link);
        }
        private static void EnsureCollider(GameObject go)
        {
            var filter=go.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)return;
            var collider=go.GetComponent<MeshCollider>();if(collider==null)collider=go.AddComponent<MeshCollider>();
            collider.enabled=true;collider.isTrigger=false;collider.sharedMesh=filter.sharedMesh;
        }
        private void Start()
        {
            if(!Initialize())return;
            if(generateOnStart || generatedRoot==null || layout.Config.Continuous) ClearGeometry();
            else
            {
                foreach(var chunk in generatedRoot.GetComponentsInChildren<PoolroomsChunk>())
                {if(chunk.GetComponent<PoolroomsPickup>()!=null||chunk.GetComponent<PoolroomsMallPortal>()!=null||chunk.GetComponent<PoolroomsTerrainPortal>()!=null)continue;Room room=layout.Rooms[chunk.roomId];chunk.cellX=room.X;chunk.cellZ=room.Z;
                    loaded[ContinuousTopology.CellKey(room.X,room.Z)]=chunk;lamps.AddRange(chunk.GetComponentsInChildren<PoolroomsLuminaire>(true));EnsureChunkColliders(chunk);AttachRoomExtras(room,chunk.transform);ChunkCreated?.Invoke(chunk);}
                RefreshConnections();LimitLights();Physics.SyncTransforms();GeometryChanged?.Invoke();
            }
            worker=StartCoroutine(SupervisedStream());
        }
        private bool Initialize()
        {
            ResolveViewer();
            if(Application.isPlaying&&GetComponent<PoolroomsDirectionalLighting>()==null)gameObject.AddComponent<PoolroomsDirectionalLighting>();
            if(profile==null){Debug.LogError("Poolrooms: assign a Profile, or use Tools > Poolrooms > Create demo.",this);return false;}
            if((transform.lossyScale-Vector3.one).sqrMagnitude>.0001f||Quaternion.Angle(transform.rotation,Quaternion.identity)>.01f)
            {Debug.LogError("Poolrooms: world rotation must be 0 and scale 1. Translation is supported.",this);return false;}
            if(Application.isPlaying&&unifiedExploration)PrepareExplorationProfile();
            try{var config=profile.Snapshot();if(unifiedExploration)WalkableExploration.Configure(config);if(!unifiedExploration){config.ConnectedMalls=false;config.ConnectedTerrain=false;config.CommonPoolrooms=false;}layout=new Layout(config);}
            catch(Exception e){Debug.LogError("Poolrooms: "+e.Message,this);return false;}
            if(ConnectedMallTopology.Enabled(layout.Config))
            {connectedMalls=GetComponent<PoolroomsConnectedMalls>();if(connectedMalls==null)connectedMalls=gameObject.AddComponent<PoolroomsConnectedMalls>();connectedMalls.enabled=true;}
            else if(connectedMalls!=null)connectedMalls.Clear();
            if(profile.enableTerrainRegions||ConnectedTerrainTopology.Enabled(layout.Config)){terrain=GetComponent<PoolroomsTerrainSystem>();if(terrain==null)terrain=gameObject.AddComponent<PoolroomsTerrainSystem>();}
            if(profile.enableGiantMalls){malls=GetComponent<PoolroomsMallSystem>();if(malls==null)malls=gameObject.AddComponent<PoolroomsMallSystem>();}
            if(Application.isPlaying&&profile.independentBiomeMaterials)
            {
                Type adapter=Type.GetType("Poolrooms.HDRP.PoolroomsBiomeAtmosphere, Poolrooms.HDRP");
                if(adapter!=null&&GetComponent(adapter)==null)gameObject.AddComponent(adapter);
            }
            return true;
        }
        private void PrepareExplorationProfile()
        {
            if(profile!=runtimeProfile)
            {
                if(runtimeProfile!=null)Destroy(runtimeProfile);
                sourceProfile=profile;runtimeProfile=Instantiate(profile);runtimeProfile.name=profile.name+" / exploración conectada";
                runtimeProfile.hideFlags=HideFlags.DontSave;profile=runtimeProfile;
            }
            profile.zoneMode=ZoneMode.Random;profile.zoneFamilyFilter=-1;
            profile.continuousWorld=true;profile.liminalGeneration=LiminalGeneration.Expanded;
            profile.proceduralGrammar=ProceduralGrammar.SolarAtlas;profile.autonomousGeneration=true;
            profile.composition=SpatialComposition.Exploration;profile.independentBiomeMaterials=true;
            profile.enableTerrainRegions=true;profile.enableGiantMalls=true;profile.enhancedMallArchitecture=profile.refinedMallArchitecture=true;
            profile.mallDistricts=false; // Preserve the small supermarkets as well as full mall districts.
        }
        private void OnDisable(){if(terrain!=null)terrain.Exit();if(malls!=null)malls.Exit();if(connectedMalls!=null)connectedMalls.Clear();generation++;if(worker!=null)StopCoroutine(worker);worker=null;}
        private void OnEnable()
        {
            if(Application.isPlaying&&layout!=null&&worker==null)worker=StartCoroutine(SupervisedStream());
        }
        private void OnDestroy(){generation++;if(runtimeProfile!=null){profile=sourceProfile;Destroy(runtimeProfile);runtimeProfile=null;}}
        public void Rebuild()
        {
            if(!Initialize())return;
            if(worker!=null)StopCoroutine(worker);
            ClearGeometry();
            if(viewer!=null){var explorer=viewer.GetComponentInParent<PoolroomsExplorer>();if(explorer!=null)explorer.ResetToSpawn();}
            if(Application.isPlaying)worker=StartCoroutine(SupervisedStream());
        }
        private void ClearGeometry()
        {
            if(terrain!=null)terrain.Exit();
            if(malls!=null)malls.Exit();
            if(connectedMalls!=null)connectedMalls.Clear();
            generation++;loaded.Clear();lamps.Clear();visibleLamps.Clear();origin.Reset();cacheFocusX=cacheFocusZ=int.MinValue;
            if(generatedRoot!=null)
            {
                // Immediately disable colliders; Destroy is deferred until end of frame.
                generatedRoot.gameObject.SetActive(false);
                Dispose(generatedRoot.gameObject);
            }
            var root=new GameObject("Poolrooms Generated");root.transform.SetParent(transform,false);generatedRoot=root.transform;
            GeometryChanged?.Invoke();
        }
        public void BuildAllImmediate()
        {
            if(!Initialize())return;
            if(worker!=null)StopCoroutine(worker);worker=null;
            ClearGeometry();
            if(layout.Config.Continuous)
            {
                Vector2Int f=Focus();int radius=LoadRadius;
                for(int z=f.y-radius;z<=f.y+radius;z++)for(int x=f.x-radius;x<=f.x+radius;x++)
                {if(ConnectedTerrainTopology.Reserved(layout.Config,x,z))continue;Room r=layout.At(x,z);Attach(r,Architecture.Build(r,layout));}
            }
            else foreach(Room r in layout.Rooms) Attach(r,Architecture.Build(r,layout));
            RefreshConnections();LimitLights();Physics.SyncTransforms();GeometryChanged?.Invoke();
        }
        public void ClearPreview()
        {
            if(worker!=null)StopCoroutine(worker);worker=null;
            ClearGeometry();layout=null;
        }
        private IEnumerator Stream()
        {
            int epoch=generation;
            while(layout!=null&&epoch==generation)
            {
                if(OutsideRooms){yield return null;continue;}
#if !UNITY_WEBGL || UNITY_EDITOR
                // StopCoroutine does not stop Task.Run. Wait for an abandoned epoch's build and
                // discard it before selecting work for the current layout/focus.
                while(buildJob.HasWork&&!buildJob.IsCompleted)
                {if(epoch!=generation)yield break;yield return null;}
                if(epoch!=generation)yield break;
                if(buildJob.HasWork)buildJob.DiscardCompleted();
#endif
                Vector2Int focus=Focus();
                if(layout.Config.Continuous&&(focus.x!=cacheFocusX||focus.y!=cacheFocusZ||cacheRadius!=UnloadRadius))
                {
                    layout.TrimNeighbourhood(focus.x,focus.y,UnloadRadius+2);
                    cacheFocusX=focus.x;cacheFocusZ=focus.y;cacheRadius=UnloadRadius;
                }
                RefreshStreamAnchors();UnloadDistant(focus);
                Room next=FindNext(focus);
                if(next==null){yield return null;continue;}
                RoomGeometry data=null;
#if UNITY_WEBGL && !UNITY_EDITOR
                data=Architecture.Build(next,layout);
#else
                Layout targetLayout=layout;Room target=next;
                // No Unity API calls inside this task. Only one geometry task in flight.
                buildJob.Start(()=>Architecture.Build(target,targetLayout));
                while(!buildJob.IsCompleted){if(epoch!=generation)yield break;yield return null;}
                if(epoch!=generation)yield break;
                Exception failure=null;
                try{data=buildJob.TakeCompleted();}catch(Exception e){failure=e;}
                if(failure!=null)
                {
                    // Report once and stop streaming, but leave the worker restartable (Rebuild or re-enabling the component).
                    StreamingFailure=failure.GetBaseException().Message;Debug.LogException(failure,this);worker=null;yield break;
                }
#endif
                if(epoch!=generation)yield break;
                // The player may have moved/teleported while the CPU mesh was being built.
                if(OutsideRooms){yield return null;continue;}
                if(LocalRoomStreaming&&Distance(next,Focus())>LoadRadius&&!NearConnection(next.X,next.Z)){yield return null;continue;}
                Attach(next,data);RefreshConnectionsNear(next.X,next.Z);LimitLights();Physics.SyncTransforms();GeometryChanged?.Invoke();
                yield return null; // At most one room upload/collider cook per frame.
            }
        }
        private Vector2Int Focus()
        {
            Vector3 p=viewer!=null?viewer.position-transform.position:Vector3.zero;
            if(layout.Config.Continuous)return new Vector2Int(origin.CellX(p.x,layout.Config.CellSize),origin.CellZ(p.z,layout.Config.CellSize));
            return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(p.x/layout.Config.CellSize+.5f),0,layout.Config.Columns-1),
                Mathf.Clamp(Mathf.FloorToInt(p.z/layout.Config.CellSize+.5f),0,layout.Config.Rows-1));
        }
        private static long Distance(Room r,Vector2Int f){return Math.Max(Math.Abs((long)r.X-f.x),Math.Abs((long)r.Z-f.y));}
        public Vector2Int ViewerCell { get { return layout!=null?Focus():Vector2Int.zero; } }
        public Vector3 CellWorldPosition(int x,int z)
        {V3 p=origin.Offset(x,z,layout.Config.CellSize);return transform.position+new Vector3(p.X,0,p.Z);}
        public bool IsRoomLoaded(int x,int z){return loaded.ContainsKey(ContinuousTopology.CellKey(x,z));}
        public bool FindMallDisplay(Vector3 p,Vector3 direction,float maximum,out PoolroomsMallDisplay display,out int index,out float distance)
        {
            if(connectedMalls!=null&&connectedMalls.FindDisplayItem(p,direction,maximum,out display,out index,out distance))return true;
            if(malls!=null)return malls.FindDisplayItem(p,direction,maximum,out display,out index,out distance);
            display=null;index=-1;distance=maximum;return false;
        }
        public Vector3 RoomWorldPosition(Room room)
        {
            if(layout==null||room==null)return transform.position;
            V3 offset=origin.Offset(room.X,room.Z,layout.Config.CellSize);
            return transform.position+new Vector3(offset.X,0,offset.Z);
        }
        public Vector3 SpawnWorldPosition { get { return terrain!=null&&terrain.IsInside?terrain.Spawn:malls!=null&&malls.IsInside?malls.Spawn:connectedMalls!=null&&connectedMalls.ContainsViewer?connectedMalls.ActiveRoot.TransformPoint(PoolroomsMallSystem.ToUnity(connectedMalls.ActivePlan.Spawn)):layout!=null?RoomWorldPosition(layout.Rooms[0])+Vector3.up*.1f:transform.position+Vector3.up*.1f; } }
        public bool BelowSafeFloor(Vector3 position)
        {if(terrain!=null&&terrain.IsInside)return terrain.BelowFloor(position);return position.y<(malls!=null&&malls.IsInside?malls.InteriorRoot.position.y:transform.position.y)-8;}
        private void RecenterOrigin()
        {
            if(!layout.Config.Continuous||viewer==null)return;
            Vector2Int focus=Focus();if(!origin.NeedsShift(focus.x,focus.y))return;
            V3 delta=origin.Recenter(focus.x,focus.y,layout.Config.CellSize);var shift=new Vector3(delta.X,0,delta.Z);
            foreach(var chunk in loaded.Values)chunk.transform.localPosition+=shift;
            if(connectedMalls!=null)connectedMalls.ShiftOrigin(shift);
            if(terrain!=null)terrain.ShiftConnectedOrigin(shift);
            var explorer=viewer.GetComponentInParent<PoolroomsExplorer>();
            if(explorer!=null)explorer.ShiftOrigin(shift);
            else viewer.position+=shift;
            Physics.SyncTransforms();GeometryChanged?.Invoke();nextLightUpdate=0;
        }
        private Room FindNext(Vector2Int focus)
        {
            // While streaming only the square around the viewer can qualify, so skip the rest of a large map.
            bool local=LocalRoomStreaming;int radius=local?LoadRadius:int.MaxValue;
            // A settled window has no new work until the focus, radius, epoch or loaded set
            // changes. Avoid hundreds of topology/cache queries every idle frame at wide views.
            if(idleSearchGeneration==generation&&idleSearchX==focus.x&&idleSearchZ==focus.y&&
                idleSearchRadius==radius&&idleSearchLocal==local&&idleSearchCount==loaded.Count&&idleAnchorHash==anchorHash)return null;
            WorkSearchCount++;
            Room result=RoomStreamingSearch.Next(layout,loaded,focus.x,focus.y,local,local?radius:0,connectionAnchors);
            if(result==null)
            {idleAnchorHash=anchorHash;idleSearchGeneration=generation;idleSearchX=focus.x;idleSearchZ=focus.y;idleSearchRadius=radius;idleSearchLocal=local;idleSearchCount=loaded.Count;}
            return result;
        }
        private void UnloadDistant(Vector2Int focus)
        {
            if(!LocalRoomStreaming)return;
            List<long> remove=removeScratch;remove.Clear();int radius=UnloadRadius;
            foreach(var pair in loaded)if(Math.Max(Math.Abs((long)pair.Value.cellX-focus.x),Math.Abs((long)pair.Value.cellZ-focus.y))>radius&&!NearConnection(pair.Value.cellX,pair.Value.cellZ))remove.Add(pair.Key);
            if(remove.Count==0)return;
            foreach(long id in remove){PoolroomsChunk chunk=loaded[id];loaded.Remove(id);chunk.gameObject.SetActive(false);Dispose(chunk.gameObject);}
            remove.Clear();
            lamps.RemoveAll(l=>l==null);
            RefreshConnections();LimitLights();Physics.SyncTransforms();GeometryChanged?.Invoke();
        }
        private void Attach(Room r,RoomGeometry data)
        {
            string address=layout.Config.Continuous?r.X+", "+r.Z:r.Id.ToString();
            var go=new GameObject("Room "+address+" - "+r.DisplayName);go.transform.SetParent(generatedRoot,false);
            go.transform.position=RoomWorldPosition(r);
            try
            {
                var chunk=go.AddComponent<PoolroomsChunk>();chunk.roomId=r.Id;chunk.cellX=r.X;chunk.cellZ=r.Z;
                chunk.lifePlan=data.Life;chunk.waterRegions=data.Regions.ToArray();
                chunk.preparedTransitions=data.PreparedTransitions.ToArray();
                var sourceProps=new List<PoolroomsChunk.SourceProp>();foreach(var p in data.SourceProps)sourceProps.Add(new PoolroomsChunk.SourceProp(p));foreach(var p in data.SourceSeats)sourceProps.Add(new PoolroomsChunk.SourceProp(p));chunk.sourceProps=sourceProps.ToArray();
                MeshFactory.Create("Architecture",data.Structure,go.transform,profile,chunk,BuildColliders);
                if(BuildColliders&&data.CollisionOnly.TriangleCount>0)MeshFactory.CreateCollider("Play safety nets",data.CollisionOnly,go.transform,chunk);
                MeshFactory.Create("Batched decoration",data.Decoration,go.transform,profile,chunk,false);
                PoolroomsArchitecturalDetail.Attach(data.Dressing,go.transform,profile,chunk,this);
                MeshFactory.Create("Glazing",data.Glass,go.transform,profile,chunk,false);
                for(int i=0;i<data.VisualMeshes.Count;i++)
                {
                    var visual=data.VisualMeshes[i];Material[] overrides=null;
                    if(visual.Theme==SurfaceTheme.FlumeFlow)
                    {overrides=new Material[6];overrides[(int)Surface.Slide]=Application.isPlaying?PoolroomsFlumeFlow.For(this).Resolve(visual.FlowProfile):PoolroomsBiomeMaterialFactory.Resolve(SurfaceTheme.FlumeFlow,(int)Surface.Slide,profile,chunk);}
                    MeshFactory.Create(visual.Theme==SurfaceTheme.FlumeFlow?"Flowing flume water":"Biome finish "+i,visual,go.transform,profile,chunk,false,materialOverrides:overrides);
                }
                var water=MeshFactory.Create("Water footprint",data.Water,go.transform,profile,chunk,false,true);
                chunk.water=water!=null?water.GetComponent<MeshRenderer>():null;
                float t=layout.Config.WallThickness;
                for(int d=0;d<4;d++)if(r.HasExit(d))
                {
                    // Full solid cap seals a doorway until BOTH neighbouring chunks exist.
                    bool openHall=r.Plan?.Restful?.Large==true&&LargeRegions.OpenEdge(layout.Config,r.X,r.Z,d,r.Environment);
                    float half=openHall?layout.Config.CellSize*.5f:SolarConnections.DoorWidth(r,d,layout.Config)*.5f;
                    var airfield=r.Plan!=null&&r.Plan.City!=null?r.Plan.City.Airfield:null;
                    if(airfield!=null&&Math.Abs(airfield.Column+Directions.DX[d])<=1&&Math.Abs(airfield.Row+Directions.DZ[d])<=3)half=layout.Config.CellSize*.5f;
                    var cap=new MeshData{Theme=profile.mallDistricts&&r.Environment==LiminalEnvironment.Supermarket?SurfaceTheme.Mall:BiomeSurfaces.ForEnvironment(r.Environment),RetailPalette=r.Environment==LiminalEnvironment.Supermarket?1:0,PlayPalette=r.Environment==LiminalEnvironment.IndoorPlay?1:0,PaletteVariant=r.Plan!=null?r.Plan.Palette:0};
                    cap.Box(new V3(-half,r.Floor,-t*.5f),new V3(half,openHall?r.Height:SolarConnections.DoorHeight(r,d,layout.Config),t*.5f),r.Environment==LiminalEnvironment.Poolrooms?Surface.Tile:Surface.Plaster,layout.Config.TextureWorldSize);
                    cap.TranslateRotate(new V3(Directions.DX[d]*r.Width*.5f,0,Directions.DZ[d]*r.Depth*.5f),d*Shapes.Pi*.5f);
                    chunk.caps[d]=MeshFactory.Create("Streaming gate "+d,cap,go.transform,profile,chunk,BuildColliders);
                    // Broad halls share their floor; an intentionally empty connector has no renderer.
                    if(d<2&&data.Connectors[d]!=null&&data.Connectors[d].Structure.TriangleCount>0)
                    {
                        CorridorGeometry corridor=data.Connectors[d];
                        chunk.links[d]=MeshFactory.Create("Connector "+d+" - "+(corridor.SolarConnection==SolarConnectionKind.Existing?corridor.Style.ToString():corridor.SolarConnection.ToString()),corridor.Structure,go.transform,profile,chunk,BuildColliders);
                        if(corridor.Natural)
                        {
                            chunk.links[d].GetComponent<MeshRenderer>().enabled=false;
                            for(int part=0;part<corridor.Finishes.Count;part++)MeshFactory.Create("Transition material "+part,corridor.Finishes[part],chunk.links[d].transform,profile,chunk,false);
                            if(corridor.TransitionGlass.TriangleCount>0)
                            {
                                MeshFactory.Create("Garden roof glazing",corridor.TransitionGlass,chunk.links[d].transform,profile,chunk,false);
                                if(Application.isPlaying)PoolroomsRoomLife.AttachShutter(this,r,d,corridor,chunk.links[d].transform,chunk);
                            }
                        }
                        foreach(Fixture f in corridor.Fixtures)AddFixture(f,chunk.links[d].transform);
                        chunk.links[d].SetActive(false);
                    }
                }
                foreach(Fixture f in data.Fixtures)AddFixture(f,go.transform);
                AttachRoomExtras(r,go.transform);
                loaded.Add(ContinuousTopology.CellKey(r.X,r.Z),chunk);ChunkCreated?.Invoke(chunk);
            }
            catch{loaded.Remove(ContinuousTopology.CellKey(r.X,r.Z));go.SetActive(false);Dispose(go);throw;}
        }

        private void AttachRoomExtras(Room r,Transform parent)
        {
            if(r.Plan!=null&&r.Plan.City!=null)PoolroomsCityBuildings.Attach(r.Plan.City,parent,layout.Config);
            var sources=parent.GetComponent<PoolroomsChunk>();
            if(Application.isPlaying&&layout.Config.EncounterExploration&&sources!=null)PoolroomsRoomLife.Attach(this,r,parent,sources.lifePlan);
            if(Application.isPlaying&&sources!=null&&sources.sourceProps!=null)
            {
                var actor=viewer!=null?viewer.GetComponentInParent<PoolroomsExplorer>():null;var carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
                foreach(var p in sources.sourceProps)
                {string key="regional:"+layout.Config.Seed+":"+r.X+":"+r.Z+":"+p.slot;if(carry==null||!carry.WasTaken(key))PoolroomsPickup.CreateSourceSeat(key,p.Plan(),parent);}
            }
            if(Application.isPlaying&&r.Plan!=null&&(r.Plan.Restful!=null||r.Plan.Rain!=null||r.Plan.Quiet!=null||r.Plan.Garden!=null||r.Plan.Baths!=null||r.Plan.Aquatic!=null))
            {
                var actor=viewer!=null?viewer.GetComponentInParent<PoolroomsExplorer>():null;var carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
                var props=r.Plan.Restful!=null?r.Plan.Restful.Props:r.Plan.Rain!=null?r.Plan.Rain.Props:r.Plan.Aquatic!=null?r.Plan.Aquatic.Props:r.Plan.Baths!=null?r.Plan.Baths.Props:r.Plan.Garden!=null?r.Plan.Garden.Props:r.Plan.Quiet.Props;
                string prefix=r.Plan.Restful!=null?"restful:":r.Plan.Rain!=null?"rainhouses:":r.Plan.Aquatic!=null?"aquatic:":r.Plan.Baths!=null?"thermal:":r.Plan.Garden!=null?"orangerie:":"quiet:";
                for(int i=0;i<props.Length;i++)
                {
                    var prop=props[i];string key=prefix+layout.Config.Seed+":"+r.X+":"+r.Z+":"+i;
                    if(carry!=null&&carry.WasTaken(key))continue;Vector3 bottom=new Vector3(prop.Bottom.X,prop.Bottom.Y,prop.Bottom.Z);
                    if(prop.CatalogId>=0){var item=MallItemCatalog.Get(prop.CatalogId);PoolroomsPickup.CreateCatalog(key,prop.CatalogId,bottom+Vector3.up*(item.Size.Y*.5f),parent,profile);}
                    else {var created=PoolroomsPickup.CreateSource(key,prop.Source,bottom,parent,SourceFurniture.Longest(prop.Source));if(created!=null)created.transform.localRotation=Quaternion.Euler(0,prop.Yaw,0);}
                }
            }
            if(Application.isPlaying&&r.Plan!=null&&r.Plan.Rain!=null&&GetComponent<PoolroomsRainWeather>()==null)gameObject.AddComponent<PoolroomsRainWeather>();
            if(Application.isPlaying&&r.Plan!=null&&r.Plan.Aquatic!=null)
            {
                var actor=viewer!=null?viewer.GetComponentInParent<PoolroomsExplorer>():null;
                if(actor!=null&&actor.GetComponent<PoolroomsAquaticTraversal>()==null)actor.gameObject.AddComponent<PoolroomsAquaticTraversal>();
                PoolroomsAquaticControl.Attach(this,r,parent);
            }
            if(Application.isPlaying&&r.Plan!=null&&r.Plan.City!=null)
            {
                var actor=viewer!=null?viewer.GetComponentInParent<PoolroomsExplorer>():null;var carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
                for(int i=0;i<r.Plan.City.Props.Length;i++)
                {
                    var prop=r.Plan.City.Props[i];string key="city11:"+layout.Config.Seed+":"+r.X+":"+r.Z+":"+i;
                    if(carry==null||!carry.WasTaken(key))
                    {var created=PoolroomsPickup.CreateSource(key,SourceFurniture.Id(prop.Source),new Vector3(prop.Position.X,prop.Position.Y,prop.Position.Z),parent,SourceFurniture.Longest(prop.Source));if(created!=null)created.transform.localRotation=Quaternion.Euler(0,prop.Yaw,0);}
                }
                if(CityTerraces.TryBuilding(r.Plan.City.Buildings,out var terrace))
                {
                    var p=CityTerraces.AwningBottom(terrace);
                    var graphics=viewer!=null?viewer.GetComponentInParent<PoolroomsGameSettings>():null;
                    bool low=graphics!=null&&graphics.Preferences!=null&&graphics.Preferences.graphicsQuality==0;
                    var awning=City11Sources.Create("B_Awning",parent,new Vector3(p.X,p.Y,p.Z),CityTerraces.AwningLongest,out var size,low?1:0);
                    if(awning!=null){awning.transform.localPosition+=Vector3.up*(size.y*.5f);awning.transform.localRotation=Quaternion.Euler(0,terrace.Turn?180:0,0);}
                }
                var airfield=r.Plan.City.Airfield;
                if(airfield!=null&&airfield.AircraftSpawn)
                {
                    string key="city11-aircraft:"+layout.Config.Seed+":"+airfield.CenterX+":"+airfield.CenterZ;
                    if(carry==null||!carry.WasTaken(key))PoolroomsAircraft.Create(this,parent,new Vector3(0,0,0),key);
                }
                if(airfield!=null&&airfield.HelicopterSpawn)
                {
                    string key="city11-helicopter:"+layout.Config.Seed+":"+airfield.CenterX+":"+airfield.CenterZ;
                    if(carry==null||!carry.WasTaken(key))PoolroomsHelicopter.Create(this,parent,Vector3.zero,key);
                }
                string carKey="city11-car:"+layout.Config.Seed+":"+r.X+":"+r.Z;
                if(airfield==null&&LiminalRegions.Hash(layout.Config.Seed,r.X,r.Z,0x1100CAu)%9==0&&(carry==null||!carry.WasTaken(carKey)))
                    PoolroomsCityVehicle.Create(this,parent,new Vector3(1.75f,0,-r.Depth*.29f),carKey,LiminalRegions.Hash(layout.Config.Seed,r.X,r.Z,0x1311CAu)%3==0?"U_CarConcept":"ToyCar");
            }
            if(Application.isPlaying&&r.Plan!=null&&r.Plan.SoftPlay!=null)
            {
                var actor=viewer!=null?viewer.GetComponentInParent<PoolroomsExplorer>():null;
                var carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
                if(actor!=null&&actor.GetComponent<PoolroomsPlayTraversal>()==null)actor.gameObject.AddComponent<PoolroomsPlayTraversal>();
                for(int i=0;i<r.Plan.SoftPlay.Props.Length;i++)
                {
                    var prop=r.Plan.SoftPlay.Props[i];string key="softplay:"+layout.Config.Seed+":"+r.X+":"+r.Z+":"+i;
                    if(carry!=null&&carry.WasTaken(key))continue;
                    PoolroomsPickup.CreatePlayProp(key,prop.Kind,prop.Colour,new Vector3(prop.Position.X,prop.Position.Y,prop.Position.Z),parent,profile);
                }
            }
            if(terrain!=null&&!r.MallGateway)terrain.AddEntrance(r,parent);
            if(malls!=null)malls.AddEntrance(r,parent);
            if(Application.isPlaying&&!profile.mallDistricts&&r.Environment==LiminalEnvironment.Supermarket&&r.Plan!=null&&r.Plan.Modules.Length>0)
            {
                var explorer=viewer!=null?viewer.GetComponentInParent<PoolroomsExplorer>():null;
                var carry=explorer!=null?explorer.GetComponent<PoolroomsCarry>():null;
                string key="market:"+layout.Config.Seed+":"+r.X+":"+r.Z;
                foreach(var module in r.Plan.Modules)if(module.Kind==LiminalModule.Checkout)
                {if(parent.GetComponentInChildren<PoolroomsPickup>(true)==null&&(carry==null||!carry.WasTaken(key)))PoolroomsPickup.Create(key,new Vector3(module.X,module.Rise+.2f,module.Z),parent,profile);break;}
            }
        }
        public void RegisterExternalChunk(PoolroomsChunk chunk,IEnumerable<Fixture> fixtures)
        {foreach(var fixture in fixtures)AddFixture(fixture,chunk.transform);ChunkCreated?.Invoke(chunk);ExternalGeometryChanged();}
        // Lighting-only changes do not rebuild connections or synchronize all physics transforms.
        public void RequestLightingRefresh(){nextLightUpdate=0;LightsChanged?.Invoke();}
        public void ExternalGeometryChanged(){if(layout!=null)RefreshConnections();nextLightUpdate=0;Physics.SyncTransforms();GeometryChanged?.Invoke();}
        public PoolroomsLuminaire AddInteractiveLamp(Transform parent,Vector3 position)
        {
            int index=lamps.Count;AddFixture(new Fixture{Position=new V3(position.x,position.y,position.z),Power=450,Width=.18f,Depth=.18f,Temperature=3300,Range=4.5f},parent);
            nextLightUpdate=0;LightsChanged?.Invoke();return lamps.Count>index?lamps[index]:null;
        }
        private void AddFixture(Fixture f,Transform parent)
        {
            if(f.Power<=0)return; // Switched-off sources must not consume the light/shadow budget.
            var lamp=new GameObject(f.Skylight?"Skylight":"Ceiling light");lamp.transform.SetParent(parent,false);
            lamp.transform.localPosition=new Vector3(f.Position.X,f.Position.Y,f.Position.Z);lamp.transform.localRotation=f.Aim.Length>0.001f?Quaternion.LookRotation(new Vector3(f.Aim.X,f.Aim.Y,f.Aim.Z),Mathf.Abs(f.Aim.Y)>.95f?Vector3.forward:Vector3.up):Quaternion.Euler(90,0,0);
            var meta=lamp.AddComponent<PoolroomsLuminaire>();meta.lumens=f.Power;meta.width=f.Width;meta.depth=f.Depth;meta.temperature=f.Temperature;meta.solarBeam=f.SolarBeam;meta.solarFill=f.SolarFill;meta.poolroomSunlight=f.PoolroomSunlight;meta.mallSunlight=f.MallSunlight;meta.ambientSupport=f.AmbientSupport;
            meta.mallLocal=f.MallLocal;meta.floorOffset=f.FloorOffset;meta.fullRange=f.Range>0?f.Range:25;meta.shadowlessRange=f.ShadowlessRange;
            var light=lamp.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=f.ConeAngle>0?f.ConeAngle:110;light.innerSpotAngle=f.SolarBeam?light.spotAngle*.85f:75;
            light.useColorTemperature=false;light.range=f.Range>0?f.Range:25;light.color=f.CustomColor?new Color(f.LightColor.X,f.LightColor.Y,f.LightColor.Z):Mathf.CorrelatedColorTemperatureToRGB(f.Temperature);light.intensity=f.CustomColor?f.Power/1500f:4;
            light.shadows=LightShadows.None;light.enabled=false;meta.source=light;lamps.Add(meta);
        }
        private void RefreshConnections()
        {
            foreach(var pair in loaded)RefreshConnections(pair.Value);
        }
        // A newly attached room can change only its own four edges and the reciprocal
        // edges of its neighbours. Full refresh remains available for external regions.
        private void RefreshConnectionsNear(int x,int z)
        {
            for(int side=-1;side<4;side++)
            {
                long nx=(long)x+(side<0?0:Directions.DX[side]),nz=(long)z+(side<0?0:Directions.DZ[side]);
                if(nx<int.MinValue||nx>int.MaxValue||nz<int.MinValue||nz>int.MaxValue)continue;
                if(loaded.TryGetValue(ContinuousTopology.CellKey((int)nx,(int)nz),out var chunk))RefreshConnections(chunk);
            }
        }
        private void RefreshConnections(PoolroomsChunk chunk)
        {
            Room r=layout.At(chunk.cellX,chunk.cellZ);
            for(int d=0;d<4;d++)if(r.HasExit(d))
            {
                Room n=layout.Neighbour(r,d);bool joined=n!=null&&loaded.ContainsKey(ContinuousTopology.CellKey(n.X,n.Z));
                ConnectedMallSite site;ConnectedTerrainSite meadow;
                if(n!=null&&ConnectedTerrainTopology.At(layout.Config,n.X,n.Z,out meadow))
                    joined=terrain!=null&&terrain.GatewayReady(meadow,r.X,r.Z);
                if(n!=null&&ConnectedMallTopology.At(layout.Config,n.X,n.Z,out site))
                    joined=connectedMalls!=null&&connectedMalls.GatewayReady(site,r.X<site.X?-1:1);
                if(chunk.caps[d]!=null&&chunk.caps[d].activeSelf==joined)chunk.caps[d].SetActive(!joined);
                if(d<2&&chunk.links[d]!=null&&chunk.links[d].activeSelf!=joined)chunk.links[d].SetActive(joined);
            }
        }
        private void Update()
        {
            if(FrontendSuspended)return;
            if(layout==null)return;
            if(!OutsideRooms)RecenterOrigin();
            if(connectedMalls!=null)
            {connectedMalls.SetSuspended(OutsideRooms);if(!OutsideRooms)connectedMalls.Tick(Focus());}
            if(terrain!=null)terrain.TickConnected(Focus(),OutsideRooms);
            if(Time.unscaledTime<nextLightUpdate)return;
            nextLightUpdate=Time.unscaledTime+.2f;LimitLights();PoolroomsArchitecturalDetail.Tick(this);
        }
        public bool MallSunActive { get; private set; }
        public void SetMallSunActive(bool active)
        {if(MallSunActive==active)return;MallSunActive=active;nextLightUpdate=0;if(isActiveAndEnabled&&layout!=null&&profile!=null)LimitLights();}
        public bool PoolroomSunActive { get; private set; }
        public void SetPoolroomSunActive(bool active)
        {
            if(PoolroomSunActive==active)return;
            PoolroomSunActive=active;nextLightUpdate=0;
            if(isActiveAndEnabled&&layout!=null&&profile!=null)LimitLights();
        }
        private void LimitLights()
        {
            Vector3 p=viewer!=null?viewer.position:transform.position;
            lamps.RemoveAll(l=>l==null);
            int shadowBudget=shadowLightBudgetOverride>=0?shadowLightBudgetOverride:profile.maxShadowLights;
            if(terrain!=null&&(terrain.IsInside||terrain.IsConnected&&terrain.Sun!=null&&terrain.Sun.enabled)&&shadowBudget>0)shadowBudget--;
            int lightBudget=activeLightBudgetOverride>=0?activeLightBudgetOverride:profile.maxActiveLights;
            if(PoolroomSunActive||MallSunActive){shadowBudget=Math.Max(0,shadowBudget-1);lightBudget=Math.Max(0,lightBudget-1);}
            Transform mallRoot=InsideMall?ActiveMallRoot:null;
            bool mallDaylight=mallRoot!=null&&ActiveMallPlan!=null&&p.y>=mallRoot.position.y-.5f&&p.y<=mallRoot.position.y+ActiveMallPlan.Floors*MallPlan.FloorHeight+8;
            PoolroomsLuminaire preferredSun=null;float sunDistance=float.PositiveInfinity;
            ranked.Clear();
            foreach(var lamp in lamps)if(lamp.source!=null)
            {
                if(!lamp.userEnabled){lamp.source.enabled=false;continue;}
                Vector3 offset=lamp.transform.position-p;float distance=offset.sqrMagnitude;
                if(lamp.fullRange<=0)lamp.fullRange=lamp.source.range; // Older baked metadata.
                bool eligible=lamp.gameObject.activeInHierarchy&&!(PoolroomSunActive&&lamp.poolroomSunlight)&&!(MallSunActive&&lamp.mallSunlight)&&(!lamp.mallLocal||MallLighting.FloorEligible(-offset.y,lamp.floorOffset));
                if(lamp.solarBeam)distance=(offset.x*offset.x+offset.z*offset.z+offset.y*offset.y*.08f)*.35f;
                else if(lamp.solarFill)distance*=.6f;
                if(eligible&&shadowBudget>0&&lightBudget>0&&lamp.solarBeam&&mallDaylight&&lamp.transform.IsChildOf(mallRoot)&&
                    (distance<sunDistance||distance==sunDistance&&(preferredSun==null||lamp.GetInstanceID()<preferredSun.GetInstanceID())))
                {preferredSun=lamp;sunDistance=distance;}
                ranked.Add(new RankedLamp{Distance=eligible?distance:float.PositiveInfinity,Eligible=eligible,Lamp=lamp});
            }
            // Reserve one EXISTING shadow/light slot for the active mall's nearest skylight.
            // Local shop lamps used to take every shadow slot and turn the sunlight off.
            if(preferredSun!=null)for(int i=0;i<ranked.Count;i++)if(ranked[i].Lamp==preferredSun)
            {var entry=ranked[i];entry.Distance=-1;ranked[i]=entry;break;}
            ranked.Sort(ByDistance);
            int active=0,shadowed=0;bool changed=false;visibleLamps.Clear();
            foreach(var entry in ranked)
            {
                PoolroomsLuminaire lamp=entry.Lamp;
                bool enabled=entry.Eligible&&active<lightBudget;
                LightShadows shadows=enabled&&!lamp.solarFill&&shadowed<shadowBudget?LightShadows.Soft:LightShadows.None;
                // Direct aperture light without shadows crosses opaque walls or mall floor slabs.
                // Keep low-cost gallery fill and lamps when shadow quality is disabled.
                if(enabled&&lamp.solarBeam&&(lamp.poolroomSunlight||lamp.GetComponentInParent<PoolroomsChunk>()?.connectedMall==true||malls!=null&&malls.IsInside)&&shadows==LightShadows.None)enabled=false;
                // Only write on change: every write on an HDRP light can queue work for the renderer.
                if(lamp.source.enabled!=enabled){lamp.source.enabled=enabled;changed=true;}
                if(lamp.source.shadows!=shadows){lamp.source.shadows=shadows;changed=true;}
                float range=MallLighting.Range(lamp.fullRange,lamp.shadowlessRange,shadows!=LightShadows.None);
                if(Mathf.Abs(lamp.source.range-range)>.001f){lamp.source.range=range;changed=true;}
                if(enabled){active++;visibleLamps.Add(lamp);}if(shadows!=LightShadows.None)shadowed++;
            }
            ranked.Clear();
            if(changed)LightsChanged?.Invoke();
        }
        public bool TryGetWater(Vector3 worldPosition,out float level)
        {
            WaterRegion region;bool found=TryGetWaterRegion(worldPosition,out region);
            level=found?region.Level:0;return found;
        }
        // Returns the actual rounded pool in world coordinates, shared by swimming and optics.
        public bool TryGetWaterRegion(Vector3 worldPosition,out WaterRegion result)
        {
            result=default(WaterRegion);if(layout==null)return false;
            // Reserved outdoor cells have no pools. Avoid building/caching indoor recipes for
            // every water/optics substep while walking across the connected meadow.
            if(OutsideRooms||terrain!=null&&terrain.IsInside)return false;
            Vector3 p=worldPosition-transform.position;float cell=layout.Config.CellSize;
            Room r=layout.At(origin.CellX(p.x,cell),origin.CellZ(p.z,cell));
            PoolroomsChunk chunk;
            if(r==null||!loaded.TryGetValue(ContinuousTopology.CellKey(r.X,r.Z),out chunk))return false;
            V3 offset=origin.Offset(r.X,r.Z,cell);float x=p.x-offset.X,z=p.z-offset.Z;
            if(chunk.waterRegions==null)chunk.waterRegions=GetWaterRegions(r,layout.Config);
            foreach(WaterRegion region in chunk.waterRegions)if(region.Contains(x,z))
            {
                result=PoolWaterModel.Translate(region,new V3(transform.position.x+offset.X,transform.position.y,transform.position.z+offset.Z));
                return true;
            }
            return false;
        }
        public static WaterRegion[] GetWaterRegions(Room r,LayoutConfig cfg)
        {return r.Plan!=null?r.Plan.Pools:r.ZoneId>=0?ZoneArchitecture.DescribeWater(r,cfg):new[]{GetWaterRegion(r,cfg)};}
        // Compatibility helper: for multi-pool zones use GetWaterRegions or TryGetWater.
        public static WaterRegion GetWaterRegion(Room r,LayoutConfig cfg)
        {
            if(r.Plan!=null)return r.Plan.Pools.Length>0?r.Plan.Pools[0]:default(WaterRegion);
            if(r.ZoneId>=0){var regions=ZoneArchitecture.DescribeWater(r,cfg);return regions.Length>0?regions[0]:default(WaterRegion);}
            if(!r.IsDeep)return new WaterRegion{Width=r.Width,Depth=r.Depth,Radius=0,Bottom=r.Floor,Level=cfg.WaterLevel};
            return new WaterRegion{Width=r.Kind==RoomKind.Atrium?12:13,Depth=r.Kind==RoomKind.Waterpark?14:12,
                Radius=r.Kind==RoomKind.Atrium?5.8f:2.8f,Bottom=-1.2f,Level=cfg.WaterLevel};
        }
        public PoolroomsAquaticControl AquaticControlAt(Room room)
        {return room!=null&&loaded.TryGetValue(ContinuousTopology.CellKey(room.X,room.Z),out var chunk)&&chunk!=null?chunk.GetComponent<PoolroomsAquaticControl>():null;}
        public Room RoomAtViewer()
        {if(layout==null)return null;if(terrain!=null&&terrain.IsInside)return terrain.AcousticRoom;if(malls!=null&&malls.IsInside)return malls.AcousticRoom;if(connectedMalls!=null&&connectedMalls.AcousticRoom!=null)return connectedMalls.AcousticRoom;Vector2Int f=Focus();return layout.At(f.x,f.y);}
        // Explicit administrator travel does not depend on random portal probability or biome shares.
        public bool VisitAdministratorDistrict(bool exterior)
        {return VisitDistrict(exterior,true);}
        internal bool VisitJourneyDistrict(bool exterior)
        {return VisitDistrict(exterior,false);}
        private bool VisitDistrict(bool exterior,bool administratorOnly)
        {
            if(!Application.isPlaying||!isActiveAndEnabled||layout==null||viewer==null)return false;
            var actor=viewer.GetComponentInParent<PoolroomsExplorer>();
            if(actor==null||administratorOnly&&(actor.MasterMode==null||!actor.MasterMode.IsActive))return false;
            if(!exterior&&connectedMalls!=null&&ConnectedMallTopology.Enabled(layout.Config))return connectedMalls.VisitNearest();
            if(exterior&&terrain!=null&&terrain.ConnectedMode)return terrain.VisitNearest();
            if(exterior&&terrain!=null&&terrain.IsInside){actor.Teleport(terrain.Spawn);return true;}
            if(!exterior&&malls!=null&&malls.IsInside){actor.Teleport(malls.Spawn);return true;}
            if(terrain!=null&&terrain.IsInside)terrain.Exit();
            if(malls!=null&&malls.IsInside)malls.Exit();
            Room address=RoomAtViewer();if(address==null)return false;
            if(exterior)
            {
                if(terrain==null)terrain=GetComponent<PoolroomsTerrainSystem>();
                if(terrain==null)terrain=gameObject.AddComponent<PoolroomsTerrainSystem>();
                terrain.enabled=true;return terrain.Enter(address,new Vector3(0,address.Floor,0));
            }
            if(malls==null)malls=GetComponent<PoolroomsMallSystem>();
            if(malls==null)malls=gameObject.AddComponent<PoolroomsMallSystem>();
            malls.enabled=true;return malls.Enter(address);
        }
        private Vector2Int NavigationFocus()
        {
            Room anchor=RoomAtViewer();return anchor!=null?new Vector2Int(anchor.X,anchor.Z):Focus();
        }
        public bool VisitPoolClimate(bool sunlight)
        {
            if(layout==null||viewer==null||(!PoolroomArchitectureMix.Enabled(layout.Config)&&layout.Config.Grammar!=ProceduralGrammar.SolarAtlas))return false;
            if(!layout.Config.Continuous)
            {foreach(Room r in layout.Rooms)if(r.Environment==LiminalEnvironment.Poolrooms&&(r.Plan!=null&&r.Plan.Solar!=null&&SolarClimateRegions.HasSky(r.Plan.Solar.Climate))==sunlight){Visit(RoomWorldPosition(r)+Vector3.up*.1f);return true;}return false;}
            Vector2Int focus=NavigationFocus();
            for(int radius=0;radius<=96;radius++)for(int offset=-radius;offset<=radius;offset++)for(int side=0;side<4;side++)
            {
                long x=(long)focus.x+(side%2==0?offset:side==1?radius:-radius),z=(long)focus.y+(side%2==1?offset:side==0?radius:-radius);
                if(x<int.MinValue||x>int.MaxValue||z<int.MinValue||z>int.MaxValue)continue;
                if(ConnectedTerrainTopology.Reserved(layout.Config,(int)x,(int)z))continue;
                if(LiminalRegions.At(layout.Config,(int)x,(int)z)!=LiminalEnvironment.Poolrooms||SolarClimateRegions.HasSky(SolarClimateRegions.At(layout.Config,(int)x,(int)z).Kind)!=sunlight)continue;
                Room candidate=layout.At((int)x,(int)z);
                if(candidate.Environment!=LiminalEnvironment.Poolrooms||(candidate.Plan!=null&&candidate.Plan.Solar!=null&&SolarClimateRegions.HasSky(candidate.Plan.Solar.Climate))!=sunlight)continue;
                Visit(RoomWorldPosition(candidate)+Vector3.up*.1f);return true;
            }
            return false;
        }
        public bool VisitEnvironment(LiminalEnvironment environment)
        {
            if(layout==null||viewer==null)return false;
            if(profile.mallDistricts&&environment==LiminalEnvironment.Supermarket)
            {
                var actor=viewer.GetComponentInParent<PoolroomsExplorer>();
                if(actor!=null&&actor.MasterMode!=null&&actor.MasterMode.IsActive)return VisitAdministratorDistrict(false);
                if(malls!=null)return malls.VisitNearest();
            }
            if(!layout.Config.Continuous)
            {
                foreach(Room room in layout.Rooms)if(room.Environment==environment)
                {Visit(RoomWorldPosition(room)+Vector3.up*.1f);return true;}
                return false;
            }
            Vector2Int focus=NavigationFocus();int x,z;
            if(!LiminalRegions.Find(layout.Config,environment,focus.x,focus.y,out x,out z))return false;
            if(ConnectedTerrainTopology.Reserved(layout.Config,x,z)||layout.At(x,z).Environment!=environment)
            {
                bool found=false;
                for(int ring=1;ring<=96&&!found;ring++)for(int dz=-ring;dz<=ring&&!found;dz++)for(int dx=-ring;dx<=ring;dx++)
                {if(Math.Max(Math.Abs(dx),Math.Abs(dz))!=ring)continue;long ax=(long)focus.x+dx,az=(long)focus.y+dz;
                    if(ax<int.MinValue||ax>int.MaxValue||az<int.MinValue||az>int.MaxValue)continue;int nx=(int)ax,nz=(int)az;
                    if(!ConnectedTerrainTopology.Reserved(layout.Config,nx,nz)&&LiminalRegions.At(layout.Config,nx,nz)==environment&&
                        layout.At(nx,nz).Environment==environment){x=nx;z=nz;found=true;break;}}
                if(!found)return false;
            }
            Visit(RoomWorldPosition(layout.At(x,z))+Vector3.up*.1f);return true;
        }
        public bool IsPilotingAircraft {get{return viewer!=null&&viewer.GetComponentInParent<PoolroomsExplorer>() is PoolroomsExplorer actor&&actor.Vehicle is PoolroomsFlyingVehicle;}}
        public bool IsPositionStreamed(Vector3 point)
        {
            if(layout==null||!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z))return false;
            if(terrain!=null&&terrain.IsInside&&!terrain.IsConnected)return terrain.IsPositionReady(point);
            if(malls!=null&&malls.IsInside)return malls.IsPositionReady(point);
            if(terrain!=null&&terrain.IsConnected&&terrain.ContainsPosition(point))return terrain.IsPositionReady(point);
            if(connectedMalls!=null&&connectedMalls.TryPositionReady(point,out bool mallReady))return mallReady;
            Vector3 local=point-transform.position;int x=origin.CellX(local.x,layout.Config.CellSize),z=origin.CellZ(local.z,layout.Config.CellSize);
            return !ConnectedTerrainTopology.Reserved(layout.Config,x,z)&&IsRoomLoaded(x,z);
        }
        public bool VisitAirfield()
        {
            if(layout==null||viewer==null)return false;var focus=NavigationFocus();CityAirfieldCell cell;
            if(!CityAirfields.Find(layout.Config,focus.x,focus.y,out cell))return false;
            Visit(CellWorldPosition(cell.X,cell.Z)+new Vector3(9,.1f,0));return true;
        }
        public bool VisitHelicopter()
        {
            if(layout==null||viewer==null)return false;var focus=NavigationFocus();CityAirfieldCell cell;
            if(!CityAirfields.Find(layout.Config,focus.x,focus.y,out cell))return false;
            Visit(CellWorldPosition(cell.CenterX+1,cell.CenterZ)+new Vector3(7,.1f,0));return true;
        }
        public bool VisitZone(int zoneIndex)
        {
            if(layout==null||viewer==null)return false;
            if(layout.Config.Continuous)
            {
                Vector2Int focus=NavigationFocus();int x,z;
                if(!ContinuousTopology.FindZone(layout.Config,zoneIndex,focus.x,focus.y,out x,out z))return false;
                Visit(RoomWorldPosition(layout.At(x,z))+Vector3.up*.1f);return true;
            }
            foreach(Room r in layout.Rooms)if(r.ZoneId==zoneIndex)
            {
                Vector3 destination=RoomWorldPosition(r)+Vector3.up*.1f;
                Visit(destination);
                return true;
            }
            return false;
        }
        private void Visit(Vector3 destination)
        {
            if(terrain!=null&&terrain.IsInside&&!terrain.IsConnected){terrain.Exit(destination);return;}
            if(malls!=null&&malls.IsInside){malls.Exit(destination);return;}
            var explorer=viewer.GetComponentInParent<PoolroomsExplorer>();
            if(explorer!=null)explorer.Teleport(destination);
            else
            {
                var body=viewer.GetComponent<CharacterController>();bool active=body!=null&&body.enabled;
                if(active)body.enabled=false;viewer.position=destination;if(active)body.enabled=true;
            }
        }
        private static void Dispose(UnityEngine.Object item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
    }
}
