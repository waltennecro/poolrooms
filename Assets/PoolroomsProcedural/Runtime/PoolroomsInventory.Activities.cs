using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    public sealed partial class PoolroomsInventory
    {
        private GameObject markerRoot;
        private readonly Queue<GameObject> markers=new Queue<GameObject>();
        private bool photographing;
        private GameObject presentation;
        public void ShowContainer(PoolroomsInteractable container)
        {
            BeginInterface("El mueble de las visitas");
            Text("Hay pequeñas cosas preparadas para quien pase por aquí. Puedes tomar las galletas una sola vez durante esta sesión.","en-reading",content);
            Button("Tomar las galletas",()=>{if(container!=null)container.CollectContents(this);},content);
            Button("Cerrar el mueble",()=>{if(container!=null)container.CloseCabinet(this);CloseInterface();},content,"en-quiet");
        }
        private void OpenPhotos()
        {
            try{string folder=Path.Combine(Application.persistentDataPath,"Photos");Directory.CreateDirectory(folder);Application.OpenURL(new Uri(folder+Path.DirectorySeparatorChar).AbsoluteUri);}
            catch(Exception error){Toast("No se pudo abrir la carpeta: "+error.Message);}
        }
        private void TakePhoto()
        {if(!photographing)StartCoroutine(CapturePostcard());}
        private IEnumerator CapturePostcard()
        {
            photographing=true;if(root!=null)root.style.display=UnityEngine.UIElements.DisplayStyle.None;
            var biome=CurrentBiome();string landmark=explorer.world!=null&&explorer.world.Terrain!=null&&explorer.world.Terrain.IsInside?"Level94":explorer.world!=null&&explorer.world.InsideMall?"CompleteMall":null;Texture2D image=null;yield return null;
            if(presentation!=null)presentation.SetActive(false);yield return new WaitForEndOfFrame();
            if(explorer==null||explorer.IsPaused){photographing=false;if(root!=null)root.style.display=UnityEngine.UIElements.DisplayStyle.Flex;if(presentation!=null)presentation.SetActive(true);yield break;}
            try
            {
                image=ScreenCapture.CaptureScreenshotAsTexture();byte[] bytes=image.EncodeToPNG();
                string folder=Path.Combine(Application.persistentDataPath,"Photos");Directory.CreateDirectory(folder);
                string path=Path.Combine(folder,"Recuerdo_"+Guid.NewGuid().ToString("N")+".png");File.WriteAllBytes(path,bytes);
                if(landmark!=null)Journal.PhotographLandmark(landmark);else Journal.Photograph(biome);Toast("Fotografía guardada. Puedes abrir su carpeta desde el cuaderno.");
            }
            catch(Exception error){Toast("No se pudo guardar la fotografía: "+error.Message);}
            finally{if(image!=null)Destroy(image);if(presentation!=null)presentation.SetActive(true);photographing=false;if(root!=null)root.style.display=UnityEngine.UIElements.DisplayStyle.Flex;}
        }
        private bool PlaceMarker()
        {
            Vector3 direction=(explorer.Aim.forward+Vector3.down*.65f).normalized;
            int count=Physics.RaycastNonAlloc(explorer.Aim.position,direction,query,4,~0,QueryTriggerInteraction.Ignore);if(count==query.Length)return false;
            int at=-1;float nearest=float.MaxValue;
            for(int i=0;i<count;i++)if(query[i].collider!=null&&!query[i].collider.transform.IsChildOf(transform)&&query[i].distance<nearest){nearest=query[i].distance;at=i;}
            if(at<0||query[at].normal.y<.85f||query[at].collider.GetComponentInParent<PoolroomsPickup>()!=null)return false;
            if(markerRoot==null)
            {
                markerRoot=new GameObject("Session chalk marks");if(explorer.world!=null)markerRoot.transform.SetParent(explorer.world.transform,false);explorer.OriginShifted+=ShiftMarkers;
            }
            var node=new GameObject("Chalk arrow");node.transform.SetParent(markerRoot.transform,false);node.transform.position=query[at].point+query[at].normal*.004f;
            Vector3 forward=Vector3.ProjectOnPlane(explorer.Aim.forward,query[at].normal);if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
            node.transform.rotation=Quaternion.LookRotation(forward.normalized,query[at].normal);
            var owner=node.AddComponent<PoolroomsChunk>();var mesh=new MeshData{Theme=SurfaceTheme.EncounterFurniture};
            mesh.Quad(new V3(-.025f,0,-.16f),new V3(-.025f,0,.06f),new V3(.025f,0,.06f),new V3(.025f,0,-.16f),V3.Up,Surface.Slide,.3f);
            mesh.Triangle(new V3(-.11f,0,.03f),new V3(0,0,.2f),new V3(.11f,0,.03f),V3.Up,V3.Up,V3.Up,new V2(),new V2(.5f,1),new V2(1,0),Surface.Slide);
            MeshFactory.Create("Matte chalk",mesh,node.transform,explorer.world.profile,owner,false);markers.Enqueue(node);
            while(markers.Count>64){var old=markers.Dequeue();if(old!=null)Destroy(old);}return true;
        }
        private void ShiftMarkers(Vector3 offset){if(markerRoot!=null)markerRoot.transform.position+=offset;}
        private void DisposeMarkers(){if(explorer!=null)explorer.OriginShifted-=ShiftMarkers;if(markerRoot!=null)Destroy(markerRoot);markerRoot=null;markers.Clear();}
        private void ShowPresentation(ExplorationItem item)
        {
            if(presentation!=null)Destroy(presentation);if(explorer.world==null||explorer.world.profile==null)return;
            var prop=Spawn(item,explorer.world.profile,explorer.Aim.position);if(prop==null)return;
            prop.Attach(explorer.Aim);presentation=prop.gameObject;
        }
        private void UpdatePresentation()
        {
            if(presentation==null)return;
            if(gestureLeft<=0){Destroy(presentation);presentation=null;return;}
            float pulse=Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Clamp01(GestureProgress)),2);
            Vector3 target=Gesture==HumanoidGesture.Drink||Gesture==HumanoidGesture.Eat?new Vector3(.13f,-.09f,.3f):Gesture==HumanoidGesture.Photograph?new Vector3(0,-.05f,.3f):new Vector3(0,-.22f,.48f);
            presentation.transform.localPosition=Vector3.Lerp(new Vector3(.28f,-.28f,.7f),target,pulse);
            presentation.transform.localRotation=Quaternion.Slerp(Quaternion.Euler(5,-12,-8),Quaternion.Euler(Gesture==HumanoidGesture.Drink?-45:0,0,0),pulse);
        }
    }
}
