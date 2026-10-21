using System;
using System.Collections;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Poolrooms
{
    // The frontend has its own scene, camera and input owner. A journey is an
    // inactive template until the player explicitly chooses to start it.
    [DefaultExecutionOrder(-31000)]
    public sealed partial class PoolroomsFrontend : MonoBehaviour
    {
        public GameObject journeyTemplate;
        public PoolroomsFrontendEnvironment sceneEnvironment;
        public static PoolroomsFrontend Instance { get; private set; }
        public readonly FrontendFlow Flow = new FrontendFlow();
        private GameObject template,session;
        private bool ownsTemplate;
        private Scene menuScene,sessionScene;
        private Camera menuCamera;
        private PoolroomsExplorer actor;
        private PoolroomsWorld world;
        private readonly List<PoolroomsProfile> profiles = new List<PoolroomsProfile>();
        private readonly List<Camera> cameras = new List<Camera>();
        private readonly List<bool> cameraStates = new List<bool>();
        private readonly List<AudioListener> listeners = new List<AudioListener>();
        private readonly List<bool> listenerStates = new List<bool>();
        private readonly List<UnityEngine.UIElements.UIDocument> hiddenDocuments = new List<UnityEngine.UIElements.UIDocument>();
        private readonly List<UnityEngine.UIElements.DisplayStyle> documentStates = new List<UnityEngine.UIElements.DisplayStyle>();
        private Coroutine loading;
        private float sessionScale=1,originalScale;
        private bool sessionAudio,originalAudio,quitting;
        private CursorLockMode originalCursor;
        private bool originalCursorVisible;
        private int chosenSeed=481516;
        private JourneyStart chosenStart=JourneyStart.Origin;
        private LiminalEnvironment chosenEnvironment=LiminalEnvironment.Poolrooms;
        private bool connectedJourney=true;
        private string destinationName="Un mundo conectado";
        public bool HasGeneratedSession { get { return session!=null && Flow.HasSession; } }
        public PoolroomsWorld SessionWorld { get { return world; } }
        public bool Owns(PoolroomsExplorer explorer) { return actor!=null&&actor==explorer; }
        public static PoolroomsFrontend CreateForScene(Scene scene)
        {CaptureLoadedDemo(scene,LoadSceneMode.Additive);return Instance;}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance=null;
            SceneManager.sceneLoaded-=CaptureLoadedDemo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBoot()
        {
            SceneManager.sceneLoaded-=CaptureLoadedDemo;
            SceneManager.sceneLoaded+=CaptureLoadedDemo;
        }

        private static void CaptureLoadedDemo(Scene scene,LoadSceneMode mode)
        {
            if(Instance!=null||!Application.isPlaying)return;
            var roots=scene.GetRootGameObjects();
            PoolroomsExplorer explorer=null;
            foreach(var root in roots)
            foreach(var candidate in root.GetComponentsInChildren<PoolroomsExplorer>(true))
                if(candidate.showStartMenu&&candidate.world!=null){explorer=candidate;break;}
            if(explorer==null)return;
            // sceneLoaded runs before Start: neither the generator nor player
            // Start has run. Capture every sibling together so Unity remaps
            // cross-object world/player/lighting references when cloning.
            var dormant=new GameObject("Dormant journey source");
            foreach(var root in roots)root.transform.SetParent(dormant.transform,true);
            var environment=PoolroomsFrontendEnvironment.Capture(dormant.transform);
            dormant.SetActive(false);
            var frontendScene=SceneManager.CreateScene("Poolrooms · Main menu");
            var node=new GameObject("Poolrooms Frontend");
            SceneManager.MoveGameObjectToScene(node,frontendScene);
            var frontend=node.AddComponent<PoolroomsFrontend>();
            frontend.menuScene=frontendScene;
            frontend.sceneEnvironment=environment;
            frontend.template=Instantiate(dormant);
            frontend.ownsTemplate=true;
            frontend.template.name="Journey template · never generated";
            frontend.template.SetActive(false);
            SceneManager.MoveGameObjectToScene(frontend.template,frontendScene);
            frontend.chosenSeed=explorer.world.profile!=null?explorer.world.profile.seed:481516;
            Destroy(dormant);
            SceneManager.SetActiveScene(frontendScene);
            SceneManager.UnloadSceneAsync(scene);
        }

        private void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;
            originalScale=Time.timeScale;originalAudio=AudioListener.pause;
            originalCursor=Cursor.lockState;originalCursorVisible=Cursor.visible;
        }

        private void Start()
        {
            if(Instance!=this)return;
            if(!menuScene.IsValid())menuScene=gameObject.scene;
            if(template==null&&journeyTemplate!=null)
            {
                // Frontend prefabs are saved inactive. Instantiation cannot
                // invoke any gameplay Start while browsing the menu.
                if(journeyTemplate.activeSelf)
                    throw new InvalidOperationException("The journey template must be saved inactive.");
                template=journeyTemplate;
                var source=template.GetComponentInChildren<PoolroomsWorld>(true);
                if(source!=null&&source.profile!=null)chosenSeed=source.profile.seed;
            }
            LoadFrontendPreferences();
            CreateMenuCamera();
            CreateFrontendUI();
            EnterMenuState();
            DrawPage(FrontendPage.Home);
        }

        private void CreateMenuCamera()
        {
            var node=new GameObject("Frontend camera · no world rendering");
            node.transform.SetParent(transform,false);
            menuCamera=node.AddComponent<Camera>();menuCamera.cullingMask=0;
            menuCamera.clearFlags=CameraClearFlags.SolidColor;
            menuCamera.backgroundColor=new Color(.025f,.04f,.045f);
            menuCamera.depth=100;menuCamera.nearClipPlane=.1f;menuCamera.farClipPlane=2;
            var data=Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
            if(data!=null&&UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null&&
                UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline"))node.AddComponent(data);
            node.AddComponent<AudioListener>();
        }

        private void EnterMenuState()
        {
            Time.timeScale=0;AudioListener.pause=true;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(menuCamera!=null)menuCamera.gameObject.SetActive(true);
            if(frontendRoot!=null)frontendRoot.style.display=UnityEngine.UIElements.DisplayStyle.Flex;
        }

        public void ReturnToMenu()
        {
            if(!Flow.Suspend()||actor==null||world==null)return;
            actor.Inventory?.CloseInterface();actor.Views?.ExitPhoto();actor.MasterMode?.CloseInterface();
            var pause=actor.GetComponent<PoolroomsPauseMenu>();pause?.ReleasePause();
            actor.GetComponent<PoolroomsGameSettings>()?.Flush();
            sessionScale=Time.timeScale;sessionAudio=AudioListener.pause;
            actor.SetPaused(true);world.FrontendSuspended=true;
            HideSessionPresentation();
            LoadFrontendPreferences();
            SceneManager.SetActiveScene(menuScene);
            EnterMenuState();DrawPage(FrontendPage.Home);
        }

        public void ContinueJourney()
        {
            if(actor==null||world==null||!Flow.Resume())return;
            SceneManager.SetActiveScene(sessionScene);
            world.FrontendSuspended=false;
            RestoreSessionPresentation();
            LeaveMenuState();
        }

        private void LeaveMenuState()
        {
            if(frontendRoot!=null)frontendRoot.style.display=UnityEngine.UIElements.DisplayStyle.None;
            menuCamera.gameObject.SetActive(false);
            Time.timeScale=sessionScale;AudioListener.pause=sessionAudio;
            actor.SetPaused(false);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        }

        private void BeginSelectedJourney()
        {
            if(template==null){ShowMessage("Falta el recorrido","Crea la demo o configura una escena de inicio con una plantilla de recorrido.");return;}
            if(Flow.HasSession)
            {
                ShowConfirmation("Comenzar otro recorrido","La sesión abierta y su inventario se sustituirán. Los ajustes y las skins se conservan.",StartSelectedJourney);
                return;
            }
            StartSelectedJourney();
        }

        private void StartSelectedJourney()
        {
            int ticket=Flow.Begin();if(ticket<0)return;
            SaveFrontendPreferences();
            if(loading!=null)StopCoroutine(loading);
            ShowLoading("Preparando un recorrido nuevo…");
            loading=StartCoroutine(LoadJourney(ticket));
        }

        private IEnumerator LoadJourney(int ticket)
        {
            // Give the loading section a painted frame before instantiating.
            yield return null;
            DisposeSession();
            yield return null;
            if(!Flow.IsCurrent(ticket))yield break;
            Exception failure=null;
            try
            {
                sessionScene=SceneManager.CreateScene("Poolrooms · Exploration "+ticket);
                session=Instantiate(template);session.name="Active journey";
                SceneManager.MoveGameObjectToScene(session,sessionScene);
                actor=session.GetComponentInChildren<PoolroomsExplorer>(true);
                world=actor!=null?actor.world:null;
                if(actor==null||world==null||world.profile==null)throw new InvalidOperationException("La plantilla necesita un jugador y un perfil de generación.");
                var profile=Instantiate(world.profile);profiles.Add(profile);world.profile=profile;
                profile.seed=chosenSeed;world.unifiedExploration=connectedJourney;
                actor.showStartMenu=false;actor.SetPaused(true);
                SceneManager.SetActiveScene(sessionScene);
                sceneEnvironment?.Apply(session.transform);
                session.SetActive(true);
                HideSessionPresentation();
            }
            catch(Exception error){failure=error;}
            if(failure!=null){FailLoading(ticket,failure.Message);yield break;}
            yield return null; // Run player/world Start under the loading screen.
            HideSessionPresentation();
            double deadline=Time.realtimeSinceStartupAsDouble+120;
            while(Flow.IsCurrent(ticket)&&world!=null&&!world.ViewerReady)
            {
                if(!string.IsNullOrEmpty(world.StreamingFailure)||Time.realtimeSinceStartupAsDouble>deadline)
                {FailLoading(ticket,string.IsNullOrEmpty(world.StreamingFailure)?"La generación tardó demasiado. Puedes volver a intentarlo.":world.StreamingFailure);yield break;}
                if(loadingText!=null)loadingText.text=world.StreamingStatus;
                yield return null;
            }
            if(!Flow.IsCurrent(ticket)||world==null)yield break;
            bool found=true;
            switch(chosenStart)
            {
                case JourneyStart.Environment:found=world.VisitEnvironment(chosenEnvironment);break;
                case JourneyStart.SunlitPoolrooms:found=world.VisitPoolClimate(true);break;
                case JourneyStart.Mall:found=world.VisitJourneyDistrict(false);break;
                case JourneyStart.Meadows:found=world.VisitJourneyDistrict(true);break;
            }
            if(!found){FailLoading(ticket,"Ese destino no está disponible con este perfil. Prueba Mundo conectado o el inicio del recorrido.");yield break;}
            yield return null;
            while(Flow.IsCurrent(ticket)&&world!=null&&!world.ViewerReady)
            {
                if(!string.IsNullOrEmpty(world.StreamingFailure)||Time.realtimeSinceStartupAsDouble>deadline)
                {FailLoading(ticket,"No se pudo terminar de preparar el destino: "+world.StreamingStatus);yield break;}
                if(loadingText!=null)loadingText.text="Preparando el suelo y las conexiones de "+destinationName+"…";
                yield return null;
            }
            if(!Flow.Complete(ticket))yield break;
            sessionScale=originalScale>0?originalScale:1;sessionAudio=originalAudio;
            RestoreSessionPresentation();LeaveMenuState();loading=null;
        }

        private void FailLoading(int ticket,string error)
        {
            if(!Flow.Fail(ticket,error))return;
            DisposeSession();EnterMenuState();DrawPage(FrontendPage.Home);
            ShowMessage("El recorrido no está listo",error);loading=null;
        }

        private void CancelLoading()
        {
            int ticket=Flow.Ticket;
            if(!Flow.Fail(ticket,"Carga cancelada."))return;
            if(loading!=null)StopCoroutine(loading);loading=null;
            DisposeSession();EnterMenuState();DrawPage(FrontendPage.Home);
        }

        private void HideSessionPresentation()
        {
            if(session==null)return;
            foreach(var camera in session.GetComponentsInChildren<Camera>(true))
                if(!cameras.Contains(camera)){cameras.Add(camera);cameraStates.Add(camera.enabled);camera.enabled=false;}
            foreach(var listener in session.GetComponentsInChildren<AudioListener>(true))
                if(!listeners.Contains(listener)){listeners.Add(listener);listenerStates.Add(listener.enabled);listener.enabled=false;}
            HideNewSessionDocuments();
        }

        private void HideNewSessionDocuments()
        {
            if(session==null)return;
            foreach(var document in session.GetComponentsInChildren<UnityEngine.UIElements.UIDocument>(true))
            {
                if(hiddenDocuments.Contains(document)||document.rootVisualElement==null)continue;
                hiddenDocuments.Add(document);documentStates.Add(document.rootVisualElement.style.display.value);
                document.rootVisualElement.style.display=UnityEngine.UIElements.DisplayStyle.None;
            }
        }

        private void RestoreSessionPresentation()
        {
            for(int i=0;i<cameras.Count;i++)if(cameras[i]!=null)cameras[i].enabled=cameraStates[i];
            for(int i=0;i<listeners.Count;i++)if(listeners[i]!=null)listeners[i].enabled=listenerStates[i];
            for(int i=0;i<hiddenDocuments.Count;i++)if(hiddenDocuments[i]!=null&&hiddenDocuments[i].rootVisualElement!=null)
                hiddenDocuments[i].rootVisualElement.style.display=documentStates[i];
            cameras.Clear();cameraStates.Clear();listeners.Clear();listenerStates.Clear();hiddenDocuments.Clear();documentStates.Clear();
        }

        private void DisposeSession()
        {
            if(menuScene.IsValid()&&menuScene.isLoaded)SceneManager.SetActiveScene(menuScene);
            RestoreSessionPresentation();
            if(session!=null){session.SetActive(false);Destroy(session);session=null;}
            actor=null;world=null;
            if(sessionScene.IsValid()&&sessionScene.isLoaded)SceneManager.UnloadSceneAsync(sessionScene);
            foreach(var profile in profiles)if(profile!=null)Destroy(profile);profiles.Clear();
        }

        private void OnApplicationQuit(){quitting=true;}
        private void OnApplicationFocus(bool focused){if(!focused)RevertFrontendDisplay();}
        private void OnDestroy()
        {
            if(Instance!=this)return;
            Instance=null;
            if(!quitting)DisposeSession();
            if(ownsTemplate&&template!=null)Destroy(template);
            DestroyFrontendUI();
            Time.timeScale=originalScale;AudioListener.pause=originalAudio;
            Cursor.lockState=originalCursor;Cursor.visible=originalCursorVisible;
        }
    }
}
