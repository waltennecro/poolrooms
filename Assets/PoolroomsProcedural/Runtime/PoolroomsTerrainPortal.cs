using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DefaultExecutionOrder(140)]
    public sealed class PoolroomsTerrainPortal : MonoBehaviour
    {
        private PoolroomsTerrainSystem terrain;
        private Room room;
        private bool exit,positioned;
        private V3 previous;
        public void Use(){if(terrain==null)return;if(exit)terrain.Exit();else terrain.Enter(room,transform.localPosition);}
        private void LateUpdate()
        {
            var actor=terrain!=null?terrain.Explorer:null;
            if(actor==null||actor.IsPaused||actor.InputCaptured||!actor.IsReady||Cursor.lockState!=CursorLockMode.Locked||Time.deltaTime<=0)
            {positioned=false;return;}
            Vector3 p=transform.InverseTransformPoint(actor.transform.position+Vector3.up);
            var current=new V3(p.x,p.y,p.z);
            if(positioned&&TerrainPortalCrossing.Crossed(previous,current,!exit))
            {positioned=false;Use();return;}
            previous=current;positioned=true;
        }
        public static PoolroomsTerrainPortal Create(PoolroomsTerrainSystem system,Room room,bool exit,Vector3 position,Transform parent,PoolroomsProfile profile,Material[] material)
        {
            var go=new GameObject(exit?"Nivel 94 · doorway back":"Nivel 94 · cross the black doorway / E");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var p=go.AddComponent<PoolroomsTerrainPortal>();p.terrain=system;p.room=room;p.exit=exit;
            var owner=go.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
            var geometry=new MeshData();geometry.Box(new V3(-1.3f,0,-.014f),new V3(1.3f,3.5f,.014f),Surface.Tile);
            MeshFactory.Create("Freestanding black plane",geometry,go.transform,profile,owner,false,false,material);
            var trigger=go.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up*1.75f;trigger.size=new Vector3(2.6f,3.5f,.35f);
            return p;
        }
    }
}
