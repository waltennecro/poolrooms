using System;
using System.Collections;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsStation : MonoBehaviour
    {
        public StationKind kind;
        public string key;
        public PoolroomsWorld world;
        public int variant;
        private float nextUse;
        private PoolroomsLuminaire lamp;
        private Coroutine animation,flowAnimation;
        private GameObject flow;
        private Transform accent;
        private Texture2D drawing;
        private Material drawingMaterial;
        private int registryIndex=-1;
        private static readonly List<PoolroomsStation> registry=new List<PoolroomsStation>();
        public Vector3 SoundPosition {get{return transform.TransformPoint(new Vector3(0,0,.2f));}}
        public string Label {get{return kind==StationKind.Chimes||kind==StationKind.RestBell?"hacer sonar suavemente":kind==StationKind.Light?"encender / apagar luz":ImmersionStations.Name(kind);}}
        private void OnEnable(){if(registryIndex<0&&registry.Count<8192){registryIndex=registry.Count;registry.Add(this);}}
        private void OnDisable(){if(registryIndex>=0){int end=registry.Count-1;var last=registry[end];registry[registryIndex]=last;last.registryIndex=registryIndex;registry.RemoveAt(end);registryIndex=-1;}if(animation!=null)StopCoroutine(animation);animation=null;if(flowAnimation!=null)StopCoroutine(flowAnimation);flowAnimation=null;if(flow!=null)flow.SetActive(false);if(accent!=null)accent.localRotation=Quaternion.identity;}
        public static PoolroomsStation Nearby(PoolroomsWorld world,Vector3 ears,ref int cursor)
        {
            PoolroomsStation best=null;float distance=18*18;
            for(int i=0,n=Mathf.Min(64,registry.Count);i<n;i++)
            {
                cursor=(cursor+1)%registry.Count;var station=registry[cursor];
                if(station==null||station.world!=world||!station.gameObject.activeInHierarchy)continue;
                float d=(station.transform.position-ears).sqrMagnitude;if(d<distance&&d>2.5f*2.5f){best=station;distance=d;}
            }
            return best;
        }
        public bool Use(PoolroomsExplorer actor)
        {
            if(actor==null||actor.IsPaused||actor.InputCaptured||!actor.IsReady||Time.time<nextUse)return false;nextUse=Time.time+.3f;
            var inventory=PoolroomsInventory.For(actor);var sound=actor.GetComponent<PoolroomsSoundscape>();
            sound?.PlayStation(ImmersionStations.Sound(kind),SoundPosition);inventory.Animate(kind==StationKind.Books||kind==StationKind.Postcards?HumanoidGesture.Read:HumanoidGesture.Adjust,1.4f);
            if(kind==StationKind.Chimes||kind==StationKind.RestBell){Pulse();inventory.Journal.Record(RoomActivity.Music,key);inventory.Toast("Una nota suave se pierde en la sala.");return true;}
            if(kind==StationKind.Light)
            {
                if(lamp==null)lamp=world?.AddInteractiveLamp(transform,new Vector3(0,.02f,.16f));
                if(lamp==null)return false;bool enabled=!inventory.State("station-lamp:"+key);inventory.SetState("station-lamp:"+key,enabled);lamp.userEnabled=enabled;world.RequestLightingRefresh();inventory.Toast(enabled?"La luz queda preparada para la próxima visita.":"Luz apagada.");return true;
            }
            inventory.OpenStation(this);return true;
        }
        public void RestoreLight(PoolroomsInventory inventory)
        {
            if(kind!=StationKind.Light||!inventory.State("station-lamp:"+key))return;
            if(lamp==null)lamp=world?.AddInteractiveLamp(transform,new Vector3(0,.02f,.16f));
            if(lamp!=null){lamp.userEnabled=true;world.RequestLightingRefresh();}
        }
        public bool Available(PoolroomsExplorer actor){return this!=null&&isActiveAndEnabled&&gameObject.activeInHierarchy&&actor!=null&&Vector3.Distance(actor.Aim.position,transform.position)<4.5f;}
        public void Pour()
        {
            if(kind!=StationKind.Tea&&kind!=StationKind.Water)return;
            if(flow==null)
            {
                var owner=GetComponentInParent<PoolroomsChunk>();if(owner==null)return;
                var mesh=new MeshData();ZoneShapes.Beam(mesh,new V3(.01f,-.077f,.14f),new V3(.01f,.023f,.14f),.0035f,Surface.Tile,.1f,8);
                flow=MeshFactory.Create("Serving water",mesh,transform,world.profile,owner,false,true);
                var renderer=flow.GetComponent<MeshRenderer>();var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.9f,.99f,.99f,.35f));block.SetColor("_Color",new Color(.9f,.99f,.99f,.35f));renderer.SetPropertyBlock(block);
            }
            if(flowAnimation!=null)StopCoroutine(flowAnimation);flowAnimation=StartCoroutine(Flow());
        }
        private IEnumerator Flow()
        {
            flow.SetActive(true);float t=0;while(t<1.3f){t+=Time.deltaTime;flow.transform.localScale=new Vector3(1+Mathf.Sin(t*25)*.06f,1,1);yield return null;}flow.SetActive(false);flowAnimation=null;
        }
        public void Pulse()
        {
            if(accent==null)accent=transform.Find("Moving accent");if(accent==null)return;
            if(animation!=null)StopCoroutine(animation);animation=StartCoroutine(Swing());
        }
        private IEnumerator Swing()
        {
            float t=0;while(t<2.5f){t+=Time.deltaTime;accent.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*9)*7*Mathf.Exp(-t*2));yield return null;}accent.localRotation=Quaternion.identity;animation=null;
        }
        public void Paint(byte[] pixels)
        {
            if(drawing==null)
            {
                drawing=new Texture2D(8,8,TextureFormat.RGBA32,false){name="Session drawing",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                var owner=GetComponentInParent<PoolroomsChunk>();if(owner==null)return;
                var data=new MeshData{Theme=SurfaceTheme.EncounterFurniture};data.QuadUV(new V3(-.21f,-.135f,.082f),new V3(.21f,-.135f,.082f),new V3(.21f,.135f,.082f),new V3(-.21f,.135f,.082f),new V3(0,0,1),new V3(0,0,1),new V3(0,0,1),new V3(0,0,1),new V2(1,0),new V2(0,0),new V2(0,1),new V2(1,1),Surface.Plaster);
                var node=MeshFactory.Create("Your drawing",data,transform,world.profile,owner,false);var renderer=node.GetComponent<MeshRenderer>();if(renderer.sharedMaterial==null){Destroy(node);return;}drawingMaterial=new Material(renderer.sharedMaterial);renderer.sharedMaterial=drawingMaterial;
                foreach(string name in new[]{"_BaseColorMap","_BaseMap","_MainTex"})if(drawingMaterial.HasProperty(name))drawingMaterial.SetTexture(name,drawing);
                foreach(string name in new[]{"_BaseColor","_Color"})if(drawingMaterial.HasProperty(name))drawingMaterial.SetColor(name,Color.white);
            }
            var colours=new Color32[64];for(int i=0;i<64;i++)colours[(7-i/8)*8+i%8]=Palette(pixels[i]);drawing.SetPixels32(colours);drawing.Apply(false,false);
        }
        private static readonly Color[] colours={new Color(.94f,.9f,.78f),new Color(.17f,.31f,.35f),new Color(.36f,.58f,.43f),new Color(.76f,.4f,.3f),new Color(.85f,.66f,.27f),new Color(.51f,.44f,.65f)};
        public static Color Palette(int index){return colours[Mathf.Clamp(index,0,5)];}
        private void OnDestroy(){if(drawing!=null)Destroy(drawing);if(drawingMaterial!=null)Destroy(drawingMaterial);}
        public static void Attach(PoolroomsWorld world,Transform parent,PoolroomsChunk owner,List<StationPlacement> placements,string prefix,float offsetY=0)
        {
            foreach(var p in placements)
            {
                var a=p.Anchor;var node=MeshFactory.Create(ImmersionStations.Name(p.Kind),ImmersionStations.Model(p.Kind,p.Variant),parent,world.profile,owner,true);
                node.transform.localPosition=new Vector3(a.Center.X,a.Center.Y+offsetY,a.Center.Z)+new Vector3(a.Normal.X,a.Normal.Y,a.Normal.Z)*.115f;
                node.transform.localRotation=Quaternion.LookRotation(new Vector3(a.Normal.X,a.Normal.Y,a.Normal.Z),Vector3.up);
                var station=node.AddComponent<PoolroomsStation>();station.world=world;station.kind=p.Kind;station.variant=p.Variant;station.key=prefix+":station:"+p.Slot;
                if(p.Kind==StationKind.Tea||p.Kind==StationKind.Water)
                {
                    var settings=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsGameSettings>():null;bool low=settings!=null&&settings.Preferences.graphicsQuality==0;
                    var cup=City11Sources.Create("R_Teacup",node.transform,Vector3.zero,.11f,out var size,low?1:0);
                    if(cup!=null)cup.transform.localPosition=new Vector3(.01f,-.13f+size.y*.5f,.1f);
                }
                var use=node.AddComponent<PoolroomsInteractable>();use.key=station.key;use.world=world;
                var actor=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;actor?.GetComponent<PoolroomsInventory>()?.RestoreStation(station);
                var accent=new GameObject("Moving accent");accent.transform.SetParent(node.transform,false);
                var data=new MeshData{Theme=SurfaceTheme.EncounterFurniture};ZoneShapes.Beam(data,new V3(-.035f,.13f,.11f),new V3(.035f,.13f,.11f),.008f,Surface.Metal,.3f,8);
                var handle=MeshFactory.Create("Handle",data,accent.transform,world.profile,owner,false);handle.GetComponent<MeshRenderer>().rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            }
        }
    }
}
