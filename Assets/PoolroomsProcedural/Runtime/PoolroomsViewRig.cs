using System;
using System.Collections;
using System.IO;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(Camera))]
    public sealed class PoolroomsViewRig : MonoBehaviour
    {
        public PoolroomsExplorer explorer;
        public LayerMask collisionMask=~0;
        public CameraPerspective Perspective {get;private set;}
        public bool PhotoActive {get;private set;}
        public bool ExternalPose {get{return PhotoActive||Perspective!=CameraPerspective.FirstPerson;}}
        public Transform Aim {get{SyncAim();return aim!=null?aim:transform;}}
        private Transform aim;
        private Camera view;
        private PoolroomsCameraEffects effects;
        private PoolroomsAvatar avatar;
        private readonly RaycastHit[] hits=new RaycastHit[32];
        private readonly Collider[] overlaps=new Collider[32];
        private float distance;
        private bool snap=true;
        private float oldTime,photoFov=65,photoYaw,photoPitch,photoRoll,photoSpeed=3;
        private bool oldAudio,oldCursorVisible,overlayVisible=true,showAvatar=true,capturing;
        private CursorLockMode oldLock;
        private Vector3 photoPosition,photoCentre;
        private string photoStatus="";
        public string PhotoDirectory {get{return Path.Combine(Application.persistentDataPath,"Photos");}}
        private void OnEnable(){if(explorer!=null)Bind(explorer);}
        public void Bind(PoolroomsExplorer owner)
        {
            if(explorer!=null){explorer.Relocated-=Relocated;explorer.OriginShifted-=Shift;}
            explorer=owner;view=GetComponent<Camera>();effects=GetComponent<PoolroomsCameraEffects>();
            if(aim==null){var go=new GameObject("Player interaction aim");aim=go.transform;aim.SetParent(transform.parent,false);}
            if(explorer!=null){explorer.Relocated+=Relocated;explorer.OriginShifted+=Shift;avatar=explorer.GetComponent<PoolroomsAvatar>();}
            SyncAim();
        }
        public void SyncAim()
        {
            if(aim==null||explorer==null)return;
            aim.localPosition=(effects!=null&&effects.HasRestPose?effects.RestLocalPosition:new Vector3(0,1.65f,0))+explorer.StanceCameraOffset;
            aim.localRotation=Quaternion.Euler(explorer.ViewPitch,0,0);
        }
        public void SetPerspective(CameraPerspective value)
        {
            if(!Enum.IsDefined(typeof(CameraPerspective),value))value=CameraPerspective.FirstPerson;
            Perspective=value;snap=true;
        }
        public bool HandleInput(Vector2 movement,Vector2 look,bool cycle,bool photo,bool escape,bool capture,bool hide,bool lookHeld,float vertical,float roll,float scroll,bool fast)
        {
            if(explorer==null)return false;
            if(PhotoActive)
            {
                if(photo||escape){ExitPhoto();return true;}
                if(hide)overlayVisible=!overlayVisible;
                if(capture)Capture();
                Cursor.lockState=lookHeld?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!lookHeld;
                float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
                if(lookHeld){photoYaw+=look.x*explorer.lookSensitivity;photoPitch=Mathf.Clamp(photoPitch-look.y*explorer.lookSensitivity,-89,89);}
                photoRoll=Mathf.Clamp(photoRoll+roll*dt*30,-45,45);photoFov=Mathf.Clamp(photoFov-scroll*3,20,110);
                var rotation=Quaternion.Euler(photoPitch,photoYaw,0);
                Vector3 delta=rotation*new Vector3(movement.x,0,movement.y)+Vector3.up*vertical;
                Vector3 wanted=photoPosition+Vector3.ClampMagnitude(delta,1)*(photoSpeed*(fast?3:1)*dt);
                wanted=photoCentre+Vector3.ClampMagnitude(wanted-photoCentre,CameraPerspectives.PhotoRadius);
                photoPosition=SafePosition(photoPosition,wanted);
                return true;
            }
            if(explorer.IsPaused||explorer.InputCaptured||!explorer.IsReady)return false;
            if(photo){EnterPhoto();return PhotoActive;}
            if(cycle)
            {
                var settings=explorer.GetComponent<PoolroomsGameSettings>();
                if(settings!=null){settings.Preferences.perspective=CameraPerspectives.Next(Perspective);settings.Change(SettingsChange.Camera);}
                else SetPerspective(CameraPerspectives.Next(Perspective));
            }
            return false;
        }
        public bool EnterPhoto()
        {
            if(PhotoActive||explorer==null||!explorer.IsReady||explorer.IsPaused||explorer.InputCaptured)return false;
            oldTime=Time.timeScale;oldAudio=AudioListener.pause;oldLock=Cursor.lockState;oldCursorVisible=Cursor.visible;
            photoPosition=transform.position;photoCentre=explorer.transform.position+Vector3.up;
            photoYaw=transform.eulerAngles.y;photoPitch=Mathf.DeltaAngle(0,transform.eulerAngles.x);photoRoll=0;photoFov=view.fieldOfView;
            showAvatar=Perspective!=CameraPerspective.FirstPerson;
            PhotoActive=true;explorer.SetPaused(true);Time.timeScale=0;AudioListener.pause=true;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;overlayVisible=true;photoStatus="";return true;
        }
        public void ExitPhoto()
        {
            if(!PhotoActive)return;
            PhotoActive=false;Time.timeScale=oldTime;AudioListener.pause=oldAudio;
            Cursor.lockState=oldLock;Cursor.visible=oldCursorVisible;
            if(explorer!=null)explorer.SetPaused(false);
            effects?.RefreshPreferences();snap=true;
            if(avatar!=null)avatar.SetVisible(Perspective!=CameraPerspective.FirstPerson,false);
            if(explorer!=null&&explorer.Wings!=null)explorer.Wings.SetVisible(Perspective!=CameraPerspective.FirstPerson,false);
        }
        public void ComposeView()
        {
            if(explorer==null||view==null)return;SyncAim();
            bool visible=Perspective!=CameraPerspective.FirstPerson;
            if(PhotoActive)
            {transform.SetPositionAndRotation(photoPosition,Quaternion.Euler(photoPitch,photoYaw,photoRoll));view.fieldOfView=photoFov;visible=showAvatar;}
            else if(ExternalPose)
            {
                var pose=CameraPerspectives.Get(Perspective);Vector3 focus=aim.position;
                var frame=pose.FollowPitch?aim.rotation:explorer.transform.rotation;
                Vector3 offset=frame*new Vector3(pose.Offset.X,pose.Offset.Y,pose.Offset.Z);
                Vector3 safe=SafePosition(focus,focus+offset);float limit=(safe-focus).magnitude;
                distance=snap?limit:CameraPerspectives.Recovery(distance,limit,Time.deltaTime);snap=false;
                transform.position=focus+offset.normalized*distance;
                if(distance>.001f)transform.rotation=Quaternion.LookRotation(focus-transform.position,explorer.transform.up);
                visible=distance>.55f;
            }
            if(avatar==null)avatar=explorer.GetComponent<PoolroomsAvatar>();
            if(avatar!=null)avatar.SetVisible(visible,PhotoActive&&!showAvatar);
            if(explorer.Wings!=null)explorer.Wings.SetVisible(visible,PhotoActive&&!showAvatar);
            // The existing torch keeps its intensity/effects, but emits from the player's head in external views.
            var light=explorer.Flashlight!=null?explorer.Flashlight.Source:null;
            if(light!=null){var parent=ExternalPose?aim:transform;if(light.transform.parent!=parent)light.transform.SetParent(parent,false);}
        }
        public Vector3 SafePosition(Vector3 origin,Vector3 wanted)
        {
            float tangent=Mathf.Tan((PhotoActive?photoFov:view!=null?view.fieldOfView:70)*Mathf.Deg2Rad*.5f);
            float aspect=view!=null?view.aspect:1.78f,near=view!=null?view.nearClipPlane:.06f;
            float radius=Mathf.Max(CameraPerspectives.CollisionRadius,near*Mathf.Sqrt(1+tangent*tangent*(1+aspect*aspect)));
            int overlapCount=Physics.OverlapSphereNonAlloc(origin,radius,overlaps,collisionMask,QueryTriggerInteraction.Ignore);
            if(overlapCount==overlaps.Length)return origin;
            for(int i=0;i<overlapCount;i++)if(!Own(overlaps[i]))return origin;
            Vector3 delta=wanted-origin;float length=delta.magnitude;if(length<.0001f)return origin;
            int count=Physics.SphereCastNonAlloc(origin,radius,delta/length,hits,length,collisionMask,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return origin;
            float safe=length;for(int i=0;i<count;i++)if(!Own(hits[i].collider))safe=Mathf.Min(safe,CameraPerspectives.SafeDistance(length,hits[i].distance));
            return origin+delta*(safe/length);
        }
        private bool Own(Collider c){return c==null||explorer!=null&&c.transform.IsChildOf(explorer.transform);}
        private void Shift(Vector3 shift){photoPosition+=shift;photoCentre+=shift;snap=true;}
        private void Relocated(){ExitPhoto();snap=true;}
        public void Capture(){if(PhotoActive&&!capturing)StartCoroutine(SavePhoto());}
        private IEnumerator SavePhoto()
        {
            capturing=true;Texture2D image=null;yield return new WaitForEndOfFrame();
            if(!PhotoActive){capturing=false;yield break;}
            try
            {
                image=ScreenCapture.CaptureScreenshotAsTexture();byte[] png=image.EncodeToPNG();Directory.CreateDirectory(PhotoDirectory);
                string path=Path.Combine(PhotoDirectory,"Liminal_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")+"_"+Guid.NewGuid().ToString("N").Substring(0,6)+".png");
                File.WriteAllBytes(path,png);photoStatus="Guardada: "+path;
            }
            catch(Exception error){photoStatus="No se pudo guardar: "+error.Message;Debug.LogWarning(photoStatus,this);}
            finally{if(image!=null)Destroy(image);capturing=false;}
        }
        private void OnGUI()
        {
            if(!PhotoActive||!overlayVisible||capturing)return;
            GUILayout.BeginArea(new Rect(18,18,360,410),GUI.skin.box);
            GUILayout.Label("MODO FOTO");GUILayout.Label("WASD · mover | E/Q · subir/bajar\nBotón derecho · mirar | Shift · rapidez\nZ/C · inclinar | Rueda · encuadre\nF8 · ocultar panel | F9 · foto | F6/Esc · volver");
            GUILayout.Label("Campo de visión · "+Mathf.RoundToInt(photoFov)+"°");photoFov=GUILayout.HorizontalSlider(photoFov,20,110);
            GUILayout.Label("Inclinación · "+Mathf.RoundToInt(photoRoll)+"°");photoRoll=GUILayout.HorizontalSlider(photoRoll,-45,45);
            GUILayout.Label("Velocidad");photoSpeed=GUILayout.HorizontalSlider(photoSpeed,.3f,8);
            showAvatar=GUILayout.Toggle(showAvatar,"Mostrar personaje");
            if(GUILayout.Button("Guardar PNG sin interfaz · F9"))Capture();
            if(GUILayout.Button("Abrir carpeta de fotos"))
            {try{Directory.CreateDirectory(PhotoDirectory);Application.OpenURL(new Uri(PhotoDirectory).AbsoluteUri);}catch(Exception e){photoStatus=e.Message;}}
            if(GUILayout.Button("Volver a explorar"))ExitPhoto();
            GUILayout.Label(photoStatus);GUILayout.EndArea();
        }
        private void OnDisable()
        {
            ExitPhoto();if(explorer!=null){explorer.Relocated-=Relocated;explorer.OriginShifted-=Shift;}
            if(avatar!=null)avatar.SetVisible(false,false);
            if(explorer!=null&&explorer.Wings!=null)explorer.Wings.SetVisible(false,false);
            if(aim!=null){while(aim.childCount>0)aim.GetChild(0).SetParent(transform,true);Destroy(aim.gameObject);aim=null;}
        }
    }
}
