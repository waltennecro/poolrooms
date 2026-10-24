using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    public static class PoolroomsLevel10
    {
        public static void Attach(PoolroomsWorld world,Room room,Transform parent)
        {
            var plan=room.Plan?.Level10;if(plan==null||world==null)return;
            var owner=parent.GetComponent<PoolroomsChunk>();if(owner==null)return;
            var actor=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;
            var inventory=actor!=null?PoolroomsInventory.For(actor):null;
            foreach(var site in Level10Activities.Plan(plan))
            {
                string key=Level10Activities.Key(world.CurrentLayout.Config.Seed,room.X,room.Z,site.Slot);
                bool pickup=site.Kind==Level10Action.WheatBundle||site.Kind==Level10Action.SeedPacket;
                if(pickup&&inventory!=null&&inventory.WasCollected(key))continue;
                var go=new GameObject(Level10Activities.Name(site.Kind));go.transform.SetParent(parent,false);
                V3 bottom=site.Bottom;bottom.Y=Level10Terrain.MeshHeight(plan,bottom.X,bottom.Z);go.transform.localPosition=Vec(bottom);
                if(site.Kind!=Level10Action.Rest)MeshFactory.Create("Farm activity",Level10Equipment.Body(site.Kind),go.transform,world.profile,owner,!pickup);
                var movingData=Level10Equipment.Moving(site.Kind);GameObject moving=null;
                if(movingData.TriangleCount>0)
                {
                    moving=MeshFactory.Create("Moving mechanism",movingData,go.transform,world.profile,owner,false);
                    moving.transform.localPosition=Vec(Level10Equipment.Pivot(site.Kind));
                    moving.GetComponent<Renderer>().rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
                }
                var size=Level10Activities.Size(site.Kind);
                if(pickup||site.Kind==Level10Action.Rest)
                {var shape=go.AddComponent<BoxCollider>();shape.size=Vec(size);shape.center=Vector3.up*size.Y*.5f;}
                if(site.Kind==Level10Action.SeedCabinet&&moving!=null)
                {var door=moving.AddComponent<BoxCollider>();door.size=new Vector3(.77f,.93f,.055f);door.center=new Vector3(.39f,.135f,0);}
                GameObject flow=null;
                if(site.Kind==Level10Action.HandPump){flow=MeshFactory.Create("Water from hand pump",Level10Equipment.WaterFlow(),go.transform,world.profile,owner,false);if(flow!=null){var wet=flow.GetComponent<Renderer>();wet.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;wet.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;flow.SetActive(false);}}
                var action=go.AddComponent<PoolroomsLevel10Activity>();
                action.Configure(site.Kind,key,plan.Seed+(uint)site.Slot,moving,flow,Level10Activities.Story(plan.Place));
                var interaction=go.AddComponent<PoolroomsInteractable>();interaction.world=world;interaction.key=key;interaction.title=Level10Activities.Name(site.Kind);interaction.activity=RoomActivity.Reading;
                if(inventory!=null)action.Restore(inventory);
            }
            Supplies(world,room,parent,inventory);
        }
        static void Supplies(PoolroomsWorld world,Room room,Transform parent,PoolroomsInventory inventory)
        {
            var p=room.Plan.Level10;float x=p.Rest.X-.95f,z=p.Rest.Z+.65f;
            int[] foods={ProvisionCatalog.AlmondWater,0};
            V3[] sites={new V3(x-.26f,.847f,z+.26f),new V3(x+.26f,.847f,z-.26f)};
            var actor=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;var carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
            for(int i=0;i<sites.Length;i++)
            {
                string key=Level10Activities.Key(world.CurrentLayout.Config.Seed,room.X,room.Z,100+i);
                if(inventory!=null&&inventory.WasCollected(key)||carry!=null&&carry.WasTaken(key))continue;
                PoolroomsPickup.CreateFood(key,foods[i],Vec(sites[i]),parent,world.profile);
            }
        }
        public static string Routes(PoolroomsExplorer actor)
        {
            var world=actor!=null?actor.world:null;var room=world!=null?world.RoomAtViewer():null;
            if(room?.Plan?.Level10==null)return "";
            string text="La marca de referencia de la veleta es "+(room.Plan.Level10.Seed%4+1)+".\nCaminos próximos: ";string[] directions={"norte","este","sur","oeste"};
            for(int side=0;side<4;side++)
            {
                if(!room.HasExit(side))continue;var next=world.CurrentLayout.Neighbour(room,side);
                text+="\n"+directions[side]+": "+(next.Plan?.Level10!=null?Level10Activities.Title(next.Plan.Level10.Place):next.DisplayName);
            }
            return text+"\n\nLas rodadas continúan; puedes regresar por el mismo camino. C cambia el ritmo de travesía y J activa avance automático.";
        }
        public static PoolroomsPickup CreateItem(int id,string key,Vector3 position,PoolroomsProfile profile)
        {return PoolroomsPickup.CreateLevel10Item(id,key,position,profile);}
        static Vector3 Vec(V3 v){return new Vector3(v.X,v.Y,v.Z);}
    }
}
