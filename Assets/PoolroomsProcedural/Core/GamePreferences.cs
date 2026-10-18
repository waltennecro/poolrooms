using System;
using System.Globalization;

namespace Poolrooms.Core
{
    public enum SettingsCategory { Display,Graphics,Camera,Audio,Controls,Comfort,Skins }
    public enum GameWindowMode { Window,Borderless,Exclusive,Maximized }
    public enum SettingsGraphicsFeature { Reflections,IndirectLight,AmbientOcclusion,Volumetrics,Water,WaterInteractions }
    public static class SettingsSearch
    {
        public static bool Matches(string text,string query)
        {return CultureInfo.InvariantCulture.CompareInfo.IndexOf(text??"",(query??"").Trim(),CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace)>=0;}
    }
    [Flags] public enum SettingsChange { None=0,Timing=1,Graphics=2,Camera=4,Audio=8,Controls=16,Comfort=32,All=63 }

    [Serializable]
    public sealed class GamePreferences
    {
        public int version=18;
        public CameraPreferences camera=new CameraPreferences();
        public CameraPerspective perspective=CameraPerspective.FirstPerson;
        // -1 preserves the scene's preset until the player chooses a quality.
        public int graphicsQuality=-1,lightBudget=-1,shadowBudget=-1,waterDetail=-1;
        public float exposureEV=5.5f;
        public int dressingVersion=1,dressingDensity=2;
        public int renderingVersion=1;
        public MasterViewOptions rendering=new MasterViewOptions();
        public TerrainGrassQuality terrainGrass=TerrainGrassQuality.Economy;
        public TerrainVisualQuality terrainVisuals=TerrainVisualQuality.Economy;
        public bool reflections=true,indirectLight=true,ambientOcclusion=true,volumetricFog=true;
        public PoolWaterAppearance waterAppearance=PoolWaterAppearance.ClearCeramic;
        public bool waterRipples=true,waterFoam=true,underwater=true;
        public int frameLimit=-1,vSync=0,screenWidth=0,screenHeight=0;
        public GameWindowMode windowMode=GameWindowMode.Borderless;
        public float masterVolume=.85f,environmentVolume=.8f,footstepVolume=.65f,waterVolume=.75f;
        public bool drips=true,reverb=true,muteUnfocused=true;
        public float sensitivity=.09f,walkSpeed=3.2f,sprintSpeed=5.5f,fieldOfView=70,uiScale=1;
        public int slideVersion=1;
        public bool allowSlide=true;
        public float slideSpeed=ExplorerSlide.DefaultSpeed;
        public bool invertY,toggleSprint,reduceMotion,reducedFlashes,highContrast,opaqueMenu,pauseOnFocusLoss=true;
        public void Sanitize()
        {
            if(dressingVersion<1)dressingDensity=2;dressingVersion=1;dressingDensity=Math.Max(0,Math.Min(2,dressingDensity));
            if(renderingVersion<1){rendering=new MasterViewOptions();terrainGrass=TerrainGrassQuality.Economy;}renderingVersion=1;
            if(rendering==null)rendering=new MasterViewOptions();rendering.Sanitize();
            if(!Enum.IsDefined(typeof(TerrainGrassQuality),terrainGrass))terrainGrass=TerrainGrassQuality.Economy;
            if(!Enum.IsDefined(typeof(TerrainVisualQuality),terrainVisuals))terrainVisuals=TerrainVisualQuality.Economy;
            version=18;if(camera==null)camera=new CameraPreferences();camera.Sanitize();
            if(!Enum.IsDefined(typeof(CameraPerspective),perspective))perspective=CameraPerspective.FirstPerson;
            if(slideVersion<1){allowSlide=true;slideSpeed=ExplorerSlide.DefaultSpeed;}slideVersion=1;
            if(graphicsQuality<-1||graphicsQuality>3)graphicsQuality=-1;
            if(waterDetail<-1||waterDetail>2)waterDetail=-1;
            lightBudget=lightBudget<0?-1:Math.Max(4,Math.Min(32,lightBudget));
            shadowBudget=shadowBudget<0?-1:Math.Max(0,Math.Min(6,shadowBudget));
            if(lightBudget>=0&&shadowBudget>lightBudget)shadowBudget=lightBudget;
            exposureEV=Clamp(exposureEV,0,12,5.5f);
            if(!Enum.IsDefined(typeof(PoolWaterAppearance),waterAppearance))waterAppearance=PoolWaterAppearance.ClearCeramic;
            if(!ValidFrameLimit(frameLimit))frameLimit=-1;vSync=Math.Max(0,Math.Min(2,vSync));
            if(screenWidth<640||screenHeight<360||screenWidth>16384||screenHeight>16384){screenWidth=screenHeight=0;}
            if(!Enum.IsDefined(typeof(GameWindowMode),windowMode))windowMode=GameWindowMode.Borderless;
            masterVolume=CameraPresentation.Unit(masterVolume,.85f);environmentVolume=CameraPresentation.Unit(environmentVolume,.8f);
            footstepVolume=CameraPresentation.Unit(footstepVolume,.65f);waterVolume=CameraPresentation.Unit(waterVolume,.75f);
            sensitivity=Clamp(sensitivity,.01f,.3f,.09f);walkSpeed=Clamp(walkSpeed,1.5f,5,3.2f);
            sprintSpeed=Clamp(sprintSpeed,walkSpeed,8,Math.Max(walkSpeed,5.5f));
            slideSpeed=Clamp(slideSpeed,ExplorerSlide.MinSpeed,ExplorerSlide.MaxSpeed,ExplorerSlide.DefaultSpeed);
            fieldOfView=Clamp(fieldOfView,50,105,70);uiScale=Clamp(uiScale,.85f,2,1);
        }
        public static bool ValidFrameLimit(int v){return v==-1||v==30||v==45||v==60||v==90||v==120||v==144;}
        public float EffectiveFlare {get{return camera.flareIntensity*(reducedFlashes?.35f:1);}}
        public bool EffectiveCameraMotion {get{return camera.cameraMotion&&!reduceMotion;}}
        public static float Clamp(float v,float min,float max,float fallback)
        {return float.IsNaN(v)||float.IsInfinity(v)?fallback:Math.Max(min,Math.Min(max,v));}
        public void Reset(SettingsCategory category)
        {
            var defaults=new GamePreferences();
            switch(category)
            {
                case SettingsCategory.Display:frameLimit=defaults.frameLimit;vSync=defaults.vSync;break;
                case SettingsCategory.Graphics:
                    graphicsQuality=-1;lightBudget=shadowBudget=waterDetail=-1;exposureEV=defaults.exposureEV;
                    dressingDensity=defaults.dressingDensity;rendering=new MasterViewOptions();terrainGrass=defaults.terrainGrass;terrainVisuals=defaults.terrainVisuals;
                    reflections=indirectLight=ambientOcclusion=volumetricFog=waterRipples=waterFoam=underwater=true;
                    waterAppearance=defaults.waterAppearance;break;
                case SettingsCategory.Camera:camera=new CameraPreferences();perspective=CameraPerspective.FirstPerson;fieldOfView=defaults.fieldOfView;break;
                case SettingsCategory.Audio:
                    masterVolume=defaults.masterVolume;environmentVolume=defaults.environmentVolume;footstepVolume=defaults.footstepVolume;
                    waterVolume=defaults.waterVolume;drips=reverb=muteUnfocused=true;break;
                case SettingsCategory.Controls:sensitivity=defaults.sensitivity;walkSpeed=defaults.walkSpeed;sprintSpeed=defaults.sprintSpeed;allowSlide=defaults.allowSlide;slideSpeed=defaults.slideSpeed;invertY=toggleSprint=false;break;
                case SettingsCategory.Comfort:uiScale=1;reduceMotion=reducedFlashes=highContrast=opaqueMenu=false;pauseOnFocusLoss=true;break;
            }
            Sanitize();
        }
        public static SettingsChange ChangesFor(SettingsCategory category)
        {
            switch(category){case SettingsCategory.Display:return SettingsChange.Timing;case SettingsCategory.Graphics:return SettingsChange.Graphics;
                case SettingsCategory.Camera:return SettingsChange.Camera;case SettingsCategory.Audio:return SettingsChange.Audio;
                case SettingsCategory.Controls:return SettingsChange.Controls;case SettingsCategory.Skins:return SettingsChange.None;default:return SettingsChange.Comfort|SettingsChange.Camera;}
        }
    }

    public readonly struct DisplayConfiguration : IEquatable<DisplayConfiguration>
    {
        public readonly int Width,Height;
        public readonly GameWindowMode Mode;
        public readonly uint RefreshNumerator,RefreshDenominator;
        public DisplayConfiguration(int width,int height,GameWindowMode mode,uint refreshNumerator=0,uint refreshDenominator=0)
        {Width=width;Height=height;Mode=mode;RefreshNumerator=refreshNumerator;RefreshDenominator=refreshDenominator;}
        public bool Valid {get{return Width>=640&&Height>=360&&Width<=16384&&Height<=16384&&Enum.IsDefined(typeof(GameWindowMode),Mode);}}
        public bool Equals(DisplayConfiguration other){return Width==other.Width&&Height==other.Height&&Mode==other.Mode;}
        public override bool Equals(object obj){return obj is DisplayConfiguration other&&Equals(other);}
        public override int GetHashCode(){unchecked{return Width*397^Height*17^(int)Mode;}}
    }
    // A display change is persisted only when confirmed. Expiry, close, disable and focus loss
    // all restore the exact configuration that preceded the preview, using unscaled time.
    public sealed class DisplayPreview
    {
        public bool Pending {get;private set;}
        public float Remaining {get;private set;}
        public DisplayConfiguration Original {get;private set;}
        public DisplayConfiguration Candidate {get;private set;}
        public bool Begin(DisplayConfiguration original,DisplayConfiguration candidate)
        {
            if(Pending||!original.Valid||!candidate.Valid||original.Equals(candidate))return false;
            Original=original;Candidate=candidate;Remaining=15;Pending=true;return true;
        }
        public bool Tick(float unscaledDelta)
        {
            if(!Pending||float.IsNaN(unscaledDelta)||float.IsInfinity(unscaledDelta)||unscaledDelta<=0)return false;
            Remaining=Math.Max(0,Remaining-unscaledDelta);return Remaining==0;
        }
        public bool Confirm(){if(!Pending||Remaining<=0)return false;Pending=false;Remaining=0;return true;}
        public bool Cancel(){if(!Pending)return false;Pending=false;Remaining=0;return true;}
    }
    public sealed class SprintPreferenceTracker
    {
        private bool previous,latched,releaseRequired;
        public void Reset(){previous=false;latched=false;releaseRequired=true;}
        public bool Sample(bool held,bool toggle,bool blocked)
        {
            if(blocked){Reset();return false;}
            if(!toggle){previous=held;latched=false;releaseRequired=false;return held;}
            if(!held)releaseRequired=false;
            if(releaseRequired){previous=held;return false;}
            if(held&&!previous)latched=!latched;previous=held;return latched;
        }
    }
}
