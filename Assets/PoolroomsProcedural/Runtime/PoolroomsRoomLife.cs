using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    public static class PoolroomsRoomLife
    {
        public static void AttachNotice(PoolroomsWorld world,Transform parent,PoolroomsChunk owner,DressingGeometry dressing,string key,int seed,int x,int z,float offsetY=0,RoomGeometry geometry=null,LiminalEnvironment environment=LiminalEnvironment.RainHouses)
        {
            if(dressing==null)return;
            foreach(var anchor in dressing.Anchors)
            {
                if(anchor.Center.Y+offsetY<.85f||anchor.Center.Y+offsetY>1.9f||V3.Dot(anchor.Normal,new V3(-anchor.Center.X,0,-anchor.Center.Z))<=.1f)continue;
                var node=MeshFactory.Create("A note for the next visitor",EncounterFurniture.Notice(),parent,world.profile,owner,false);
                node.transform.localPosition=new Vector3(anchor.Center.X,anchor.Center.Y+offsetY,anchor.Center.Z)+new Vector3(anchor.Normal.X,anchor.Normal.Y,anchor.Normal.Z)*.12f;
                node.transform.localRotation=Quaternion.LookRotation(-new Vector3(anchor.Normal.X,anchor.Normal.Y,anchor.Normal.Z),Vector3.up);
                var shape=node.AddComponent<BoxCollider>();shape.size=new Vector3(.72f,.54f,.05f);
                var story=RoomMemories.At(seed,x,z,LiminalEnvironment.RainHouses);var interaction=node.AddComponent<PoolroomsInteractable>();interaction.world=world;interaction.key=key;interaction.activity=RoomActivity.Notice;interaction.title=story.Title;interaction.note="Dejamos este lugar preparado para la próxima visita. Hay tiempo para descansar, leer o escuchar una canción. Gracias por cuidar las cosas que encuentres.";
                if(geometry!=null&&world.CurrentLayout!=null)
                {
                    var room=new Room{X=x,Z=z,Floor=-offsetY,Environment=environment};var plan=new RoomLifePlan{Notice=anchor};
                    var stations=ImmersionStations.Plan(room,geometry,plan,world.CurrentLayout.Config);
                    PoolroomsStation.Attach(world,parent,owner,stations,key,offsetY);
                }
                return;
            }
        }
        public static void AttachShutter(PoolroomsWorld world,Room room,int direction,CorridorGeometry corridor,Transform parent,PoolroomsChunk owner)
        {
            var pivot=new GameObject("Skylight shutter hinge");pivot.transform.SetParent(parent,false);pivot.transform.localPosition=new Vector3(corridor.ShutterPosition.X,corridor.ShutterPosition.Y,corridor.ShutterPosition.Z);pivot.transform.localRotation=Quaternion.Euler(0,corridor.ShutterYaw,0);
            var mesh=new MeshData{Theme=SurfaceTheme.EncounterFurniture};EncounterFurniture.Chamfer(mesh,0,.7f,2.2f,1.4f,0,.045f,.009f,Surface.Accent);
            var shade=MeshFactory.Create("Hinged skylight shade",mesh,pivot.transform,world.profile,owner,false);shade.GetComponent<MeshRenderer>().rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            var handle=new GameObject("Skylight control");handle.transform.SetParent(parent,false);Vector3 horizontal=direction==0?new Vector3(1.95f,0,corridor.ShutterPosition.Z):new Vector3(corridor.ShutterPosition.X,0,-1.95f);handle.transform.localPosition=horizontal+Vector3.up*1.3f;
            var knob=EncounterFurniture.Door();var control=MeshFactory.Create("Control handle",knob,handle.transform,world.profile,owner,false);control.transform.localScale=Vector3.one*.16f;
            var shape=handle.AddComponent<BoxCollider>();shape.size=new Vector3(.18f,.3f,.16f);
            var use=handle.AddComponent<PoolroomsInteractable>();use.world=world;use.activity=RoomActivity.Window;use.key="skylight:"+world.CurrentLayout.Config.Seed+":"+room.X+":"+room.Z+":"+direction;use.movingPart=pivot.transform;
        }
        public static void Attach(PoolroomsWorld world,Room room,Transform parent,RoomLifePlan plan)
        {
            if(plan==null||parent.Find("Warm room details")!=null)return;
            var owner=parent.GetComponentInParent<PoolroomsChunk>();if(owner==null)return;
            var actor=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;
            var inventory=PoolroomsInventory.For(actor);var carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
            var root=new GameObject("Warm room details");root.transform.SetParent(parent,false);
            string prefix="life:"+world.CurrentLayout.Config.Seed+":"+room.X+":"+room.Z+":";
            if(plan.Notice.HasValue)
            {
                var anchor=plan.Notice.Value;var go=MeshFactory.Create("Traveler notice",EncounterFurniture.Notice(),root.transform,world.profile,owner,false);
                go.transform.localPosition=new Vector3(anchor.Center.X,anchor.Center.Y,anchor.Center.Z)+new Vector3(anchor.Normal.X,anchor.Normal.Y,anchor.Normal.Z)*.12f;
                go.transform.localRotation=Quaternion.LookRotation(-new Vector3(anchor.Normal.X,anchor.Normal.Y,anchor.Normal.Z),Vector3.up);
                var shape=go.AddComponent<BoxCollider>();shape.size=new Vector3(.72f,.54f,.05f);
                var interaction=go.AddComponent<PoolroomsInteractable>();interaction.world=world;interaction.activity=RoomActivity.Notice;interaction.key=prefix+"notice";interaction.title=plan.Vignette.Title;interaction.note=plan.Vignette.Note;
            }
            PoolroomsStation.Attach(world,root.transform,owner,plan.Stations,prefix);
            foreach(var p in plan.Furniture)
            {
                string key=prefix+p.Slot;if(carry!=null&&carry.WasTaken(key))continue;
                var point=new Vector3(p.Bottom.X,p.Bottom.Y,p.Bottom.Z);
                if(p.Kind==LifeFurnishing.Catalog)
                {
                    var definition=MallItemCatalog.Get(p.Source);var pickup=PoolroomsPickup.CreateCatalog(key,p.Source,point+Vector3.up*(definition.Size.Y*.5f),root.transform,world.profile);if(pickup!=null)pickup.transform.localRotation=Quaternion.Euler(0,p.Yaw,0);continue;
                }
                if(p.Kind==LifeFurnishing.Source)
                {
                    var pickup=PoolroomsPickup.CreateSource(key,SourceFurniture.Id(p.Source),point,root.transform,SourceFurniture.Longest(p.Source));
                    if(pickup!=null)pickup.transform.localRotation=Quaternion.Euler(0,p.Yaw,0);continue;
                }
                var go=MeshFactory.Create(p.Kind.ToString(),EncounterFurniture.Body(p.Kind),root.transform,world.profile,owner,true);go.transform.localPosition=point;go.transform.localRotation=Quaternion.Euler(0,p.Yaw,0);
                var use=go.AddComponent<PoolroomsInteractable>();use.world=world;use.key=key;use.title=plan.Vignette.Title;use.note=plan.Vignette.Note;
                use.activity=p.Kind==LifeFurnishing.Piano?RoomActivity.Music:p.Kind==LifeFurnishing.GameTable?RoomActivity.Game:RoomActivity.Drawer;
                if(p.Kind==LifeFurnishing.Cabinet)
                {
                    var pivot=new GameObject("Hinged door");pivot.transform.SetParent(go.transform,false);pivot.transform.localPosition=new Vector3(-.39f,.15f,-.274f);
                    var doorVisual=MeshFactory.Create("Inset wood door",EncounterFurniture.Door(),pivot.transform,world.profile,owner,false);doorVisual.GetComponent<MeshRenderer>().rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;use.movingPart=pivot.transform;
                    var door=pivot.AddComponent<BoxCollider>();door.center=new Vector3(.39f,.33f,0);door.size=new Vector3(.78f,.66f,.04f);
                    // The visual door swings; collision follows its kinematic body, not a stale static transform.
                    var body=pivot.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;
                    use.contents=PoolroomsPickup.CreateCatalog("display:"+key,25*8,new Vector3(0,.54f,0),go.transform,world.profile)?.gameObject;
                    if(use.contents!=null){var pickup=use.contents.GetComponent<PoolroomsPickup>();if(pickup!=null)Object.Destroy(pickup);foreach(var shape in use.contents.GetComponents<Collider>())shape.enabled=false;if(inventory!=null&&inventory.WasCollected(key))use.contents.SetActive(false);}
                }
                else if(p.Kind==LifeFurnishing.Piano)
                {
                    use.movingPart=go.transform;var keyboard=go.AddComponent<PoolroomsPianoKeys>();
                    for(int i=0;i<8;i++)
                    {
                        var keyVisual=MeshFactory.Create("Moving key "+i,EncounterFurniture.PianoKey(i),go.transform,world.profile,owner,false);
                        keyVisual.transform.localPosition=new Vector3(-.35f+i*.1f,.905f,-.15f);keyboard.keys[i]=keyVisual.transform;
                        keyVisual.GetComponent<MeshRenderer>().rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
                    }
                    keyboard.enabled=false;
                } // Identifies the musical keyboard; it does not animate the furniture body.
            }
        }
    }
}
