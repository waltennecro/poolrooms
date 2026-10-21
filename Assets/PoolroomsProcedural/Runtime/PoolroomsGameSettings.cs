using System;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Optional rendering integration keeps the runtime/UI assembly independent of HDRP.
    public interface IPoolroomsGraphicsSettings
    {
        bool SettingsReady {get;}
        bool RayTracingAvailable {get;}
        string SettingsStatus {get;}
        bool Supports(SettingsGraphicsFeature feature);
        void CaptureSettings(GamePreferences values);
        void ApplySettings(GamePreferences values);
    }

    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsGameSettings : MonoBehaviour
    {
        public const string PreferencesKey="Poolrooms.Settings.v18";
        public GamePreferences Preferences {get;private set;}=new GamePreferences();
        public DisplayPreview Display {get;}=new DisplayPreview();
        public float EffectiveMasterVolume {get{return !focused&&Preferences.muteUnfocused?0:Preferences.masterVolume;}}
        public event Action Changed;
        public event Action DisplayChanged;
        private PoolroomsExplorer explorer;
        private PoolroomsCameraEffects effects;
        private Camera cameraView;
        private readonly CameraPreferences effectiveCamera=new CameraPreferences();
        private GamePreferences sceneDefaults;
        private IPoolroomsGraphicsSettings graphics;
        private bool initialized,dirty,focused=true,graphicsDirty,timingOwned;
        private float saveAt,graphicsAt;
        private int originalFrameLimit,originalVSync;
        public bool GraphicsAvailable {get{FindGraphics();return graphics!=null&&graphics.SettingsReady;}}
        public bool RayTracingAvailable {get{return GraphicsAvailable&&graphics.RayTracingAvailable;}}
        public bool Supports(SettingsGraphicsFeature feature){return GraphicsAvailable&&graphics.Supports(feature);}
        public string GraphicsStatus {get{return GraphicsAvailable?graphics.SettingsStatus:"Los ajustes HDRP necesitan la configuración gráfica de esta escena.";}}
        public bool DisplayAvailable {get{return Application.platform==RuntimePlatform.WindowsPlayer||Application.platform==RuntimePlatform.WindowsEditor||
            Application.platform==RuntimePlatform.LinuxPlayer||Application.platform==RuntimePlatform.LinuxEditor||Application.platform==RuntimePlatform.OSXPlayer||Application.platform==RuntimePlatform.OSXEditor;}}
        public bool ExclusiveAvailable {get{return Application.platform==RuntimePlatform.WindowsPlayer||Application.platform==RuntimePlatform.WindowsEditor;}}
        public bool MaximizedAvailable {get{return Application.platform==RuntimePlatform.OSXPlayer||Application.platform==RuntimePlatform.OSXEditor;}}
        private void Awake(){explorer=GetComponent<PoolroomsExplorer>();}
        public void Initialize(string legacyCameraKey="Poolrooms.Camera.v1")
        {
            if(initialized)return;
            if(explorer==null)explorer=GetComponent<PoolroomsExplorer>();
            cameraView=explorer.eyes!=null?explorer.eyes.GetComponent<Camera>():GetComponentInChildren<Camera>();
            var initial=new GamePreferences();initial.sensitivity=explorer.lookSensitivity;initial.walkSpeed=explorer.walkSpeed;initial.sprintSpeed=explorer.sprintSpeed;
            initial.allowSlide=explorer.allowSlide;initial.slideSpeed=explorer.slideSpeed;
            if(cameraView!=null)initial.fieldOfView=cameraView.fieldOfView;
            var audio=GetComponent<PoolroomsAmbientAudio>();
            if(audio!=null){initial.masterVolume=audio.masterVolume;initial.environmentVolume=audio.environmentVolume;initial.footstepVolume=audio.footstepVolume;initial.waterVolume=audio.waterVolume;initial.drips=audio.occasionalDrips;initial.reverb=audio.roomReverb;initial.spatialAmbience=audio.spatialAmbience;initial.soundOcclusion=audio.soundOcclusion;}
            initial.frameLimit=GamePreferences.ValidFrameLimit(Application.targetFrameRate)?Application.targetFrameRate:-1;initial.vSync=QualitySettings.vSyncCount;
            initial.screenWidth=Screen.width;initial.screenHeight=Screen.height;initial.windowMode=CurrentDisplay().Mode;
            FindGraphics();if(graphics!=null)graphics.CaptureSettings(initial);
            initial.Sanitize();sceneDefaults=Copy(initial);Preferences=Copy(initial);
            bool saved=PlayerPrefs.HasKey(PreferencesKey);
            try
            {
                if(saved)
                {
                    string json=PlayerPrefs.GetString(PreferencesKey);var loaded=JsonUtility.FromJson<GamePreferences>(json);
                    if(loaded!=null&&loaded.version==18)
                    {
                        // Existing v18 saves predate this opt-out. Preserve an explicit false, enable missing fields.
                        if(loaded.soundscape!=null&&!json.Contains("\"roomDetails\""))loaded.soundscape.roomDetails=true;
                        Preferences=loaded;
                    }
                }
                else if(PlayerPrefs.HasKey(legacyCameraKey))
                {var legacy=JsonUtility.FromJson<CameraPreferences>(PlayerPrefs.GetString(legacyCameraKey));if(legacy!=null)Preferences.camera=legacy;}
            }
            catch(ArgumentException){Preferences=Copy(initial);}
            Preferences.Sanitize();initialized=true;
            originalFrameLimit=Application.targetFrameRate;originalVSync=QualitySettings.vSyncCount;
            BindCamera(cameraView!=null?cameraView.GetComponent<PoolroomsCameraEffects>():null);
            Apply(SettingsChange.Camera|SettingsChange.Audio|SettingsChange.Controls|SettingsChange.Comfort|(saved?SettingsChange.Timing|SettingsChange.Graphics:SettingsChange.None));
            // Saved display dimensions are offered in the menu, never forced at startup.
        }
        private void Start(){Initialize();Apply(SettingsChange.Camera|SettingsChange.Audio|SettingsChange.Controls);}
        private static GamePreferences Copy(GamePreferences values){return JsonUtility.FromJson<GamePreferences>(JsonUtility.ToJson(values));}
        private void FindGraphics()
        {
            if(graphics is Component component&&component!=null)return;
            graphics=null;
            if(explorer==null||explorer.world==null)return;
            foreach(var behaviour in explorer.world.GetComponents<MonoBehaviour>())
                if(behaviour is IPoolroomsGraphicsSettings bridge){graphics=bridge;break;}
        }
        public void BindCamera(PoolroomsCameraEffects value)
        {if(value!=null)effects=value;if(effects!=null)cameraView=effects.GetComponent<Camera>();if(initialized)ApplyCamera();}
        public void Change(SettingsChange changes)
        {
            Initialize();Preferences.Sanitize();Apply(changes);dirty=true;saveAt=Time.unscaledTime+.6f;Changed?.Invoke();
        }
        public void ApplyFrontendPreferences(GamePreferences values)
        {
            if(values==null)throw new ArgumentNullException(nameof(values));
            Initialize();Preferences=Copy(values);Preferences.Sanitize();
            Apply(SettingsChange.All);dirty=true;Changed?.Invoke();Flush();
        }
        public void Reset(SettingsCategory category)
        {
            Initialize();if(category==SettingsCategory.Skins){GetComponent<PoolroomsAvatar>()?.ResetSettings();GetComponent<PoolroomsWings>()?.ResetSettings();return;}Preferences.Reset(category);
            if(category==SettingsCategory.Controls){var torch=GetComponent<PoolroomsFlashlight>();if(torch!=null)torch.ResetPreferences();}
            if(category==SettingsCategory.Graphics&&sceneDefaults!=null)
            {
                Preferences.graphicsQuality=sceneDefaults.graphicsQuality;Preferences.exposureEV=sceneDefaults.exposureEV;
                Preferences.reflections=sceneDefaults.reflections;Preferences.indirectLight=sceneDefaults.indirectLight;
                Preferences.ambientOcclusion=sceneDefaults.ambientOcclusion;Preferences.volumetricFog=sceneDefaults.volumetricFog;
                Preferences.waterAppearance=sceneDefaults.waterAppearance;Preferences.waterRipples=sceneDefaults.waterRipples;Preferences.waterFoam=sceneDefaults.waterFoam;Preferences.underwater=sceneDefaults.underwater;
            }
            Change(GamePreferences.ChangesFor(category));
        }
        private void Apply(SettingsChange changes)
        {
            if((changes&SettingsChange.Timing)!=0){QualitySettings.vSyncCount=Preferences.vSync;Application.targetFrameRate=Preferences.frameLimit;timingOwned=true;}
            if((changes&SettingsChange.Camera)!=0)ApplyCamera();
            if((changes&SettingsChange.Controls)!=0&&explorer!=null)
            {explorer.lookSensitivity=Preferences.sensitivity;explorer.walkSpeed=Preferences.walkSpeed;explorer.sprintSpeed=Preferences.sprintSpeed;explorer.invertLookY=Preferences.invertY;explorer.toggleSprint=Preferences.toggleSprint;explorer.allowSlide=Preferences.allowSlide;explorer.slideSpeed=Preferences.slideSpeed;explorer.ResetSprintPreference();}
            if((changes&SettingsChange.Audio)!=0)ApplyAudio();
            if((changes&SettingsChange.Graphics)!=0){graphicsDirty=true;graphicsAt=Time.unscaledTime+.18f;}
        }
        private void ApplyCamera()
        {
            if(explorer!=null&&explorer.Views!=null)explorer.Views.SetPerspective(Preferences.perspective);
            var p=Preferences.camera;
            effectiveCamera.viewMode=p.viewMode;effectiveCamera.styleIntensity=p.styleIntensity;effectiveCamera.flareIntensity=Preferences.EffectiveFlare;
            effectiveCamera.splashIntensity=p.splashIntensity;effectiveCamera.waterEffects=p.waterEffects;effectiveCamera.cameraMotion=Preferences.EffectiveCameraMotion;
            effectiveCamera.bloomScale=p.bloomScale;effectiveCamera.grainScale=p.grainScale;effectiveCamera.chromaticScale=p.chromaticScale;effectiveCamera.vignetteScale=p.vignetteScale;
            effectiveCamera.reduceMotion=Preferences.reduceMotion;
            effectiveCamera.locomotionMotion=p.locomotionMotion&&!Preferences.reduceMotion;effectiveCamera.motionIntensity=p.motionIntensity;
            effectiveCamera.flareSize=p.flareSize;effectiveCamera.ghostIntensity=p.ghostIntensity;
            if(effects!=null){effects.preferences=effectiveCamera;effects.SetFieldOfView(Preferences.fieldOfView);effects.RefreshPreferences();}
            else if(cameraView!=null)cameraView.fieldOfView=Preferences.fieldOfView;
        }
        private void ApplyAudio()
        {
            var audio=GetComponent<PoolroomsAmbientAudio>();if(audio==null)return;
            audio.masterVolume=EffectiveMasterVolume;audio.environmentVolume=Preferences.environmentVolume;
            audio.footstepVolume=Preferences.footstepVolume;audio.waterVolume=Preferences.waterVolume;audio.occasionalDrips=Preferences.drips;audio.roomReverb=Preferences.reverb;audio.spatialAmbience=Preferences.spatialAmbience;audio.soundOcclusion=Preferences.soundOcclusion;
        }
        private void Update()
        {
            if(!initialized)return;
            if(Display.Tick(Time.unscaledDeltaTime))RevertDisplay();
            if(graphicsDirty&&Time.unscaledTime>=graphicsAt)ApplyGraphics();
            if(dirty&&Time.unscaledTime>=saveAt)Flush();
        }
        private void ApplyGraphics()
        {
            FindGraphics();graphicsDirty=false;GetComponent<PoolroomsMasterView>()?.ApplySettings();
            if(graphics!=null&&graphics.SettingsReady)graphics.ApplySettings(Preferences);
        }
        public bool PreviewDisplay(DisplayConfiguration candidate)
        {
            Initialize();if(!DisplayAvailable||candidate.Mode==GameWindowMode.Exclusive&&!ExclusiveAvailable||candidate.Mode==GameWindowMode.Maximized&&!MaximizedAvailable)return false;
            if(!Display.Begin(CurrentDisplay(),candidate))return false;
            try{SetDisplay(candidate);DisplayChanged?.Invoke();return true;}
            catch(Exception error){RevertDisplay();Debug.LogException(error,this);return false;}
        }
        public void KeepDisplay()
        {
            if(!Display.Confirm())return;
            var actual=CurrentDisplay();Preferences.screenWidth=actual.Width;Preferences.screenHeight=actual.Height;Preferences.windowMode=actual.Mode;
            dirty=true;Flush();DisplayChanged?.Invoke();
        }
        public void RevertDisplay()
        {
            var original=Display.Original;if(!Display.Cancel())return;
            try{SetDisplay(original);}catch(Exception error){Debug.LogException(error,this);}finally{DisplayChanged?.Invoke();}
        }
        public static DisplayConfiguration CurrentDisplay()
        {
            GameWindowMode mode=Screen.fullScreenMode==FullScreenMode.Windowed?GameWindowMode.Window:
                Screen.fullScreenMode==FullScreenMode.ExclusiveFullScreen?GameWindowMode.Exclusive:
                Screen.fullScreenMode==FullScreenMode.MaximizedWindow?GameWindowMode.Maximized:GameWindowMode.Borderless;
            var refresh=Screen.currentResolution.refreshRateRatio;
            return new DisplayConfiguration(Screen.width,Screen.height,mode,refresh.numerator,refresh.denominator);
        }
        private static void SetDisplay(DisplayConfiguration value)
        {
            var mode=value.Mode==GameWindowMode.Window?FullScreenMode.Windowed:value.Mode==GameWindowMode.Exclusive?FullScreenMode.ExclusiveFullScreen:
                value.Mode==GameWindowMode.Maximized?FullScreenMode.MaximizedWindow:FullScreenMode.FullScreenWindow;
            if(value.RefreshNumerator>0&&value.RefreshDenominator>0)
                Screen.SetResolution(value.Width,value.Height,mode,new RefreshRate{numerator=value.RefreshNumerator,denominator=value.RefreshDenominator});
            else Screen.SetResolution(value.Width,value.Height,mode);
        }
        public void Flush()
        {
            if(!initialized)return;if(graphicsDirty)ApplyGraphics();if(!dirty)return;
            Preferences.Sanitize();PlayerPrefs.SetString(PreferencesKey,JsonUtility.ToJson(Preferences));PlayerPrefs.Save();dirty=false;
        }
        private void OnApplicationFocus(bool value)
        {focused=value;if(!initialized)return;if(!value)RevertDisplay();ApplyAudio();}
        private void OnApplicationQuit(){RevertDisplay();Flush();}
        private void OnDisable()
        {
            RevertDisplay();Flush();if(timingOwned){Application.targetFrameRate=originalFrameLimit;QualitySettings.vSyncCount=originalVSync;timingOwned=false;}
        }
    }
}
