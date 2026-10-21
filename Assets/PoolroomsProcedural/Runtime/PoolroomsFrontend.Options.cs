using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Poolrooms
{
    public sealed partial class PoolroomsFrontend
    {
        private GamePreferences preferences;
        private bool displayPreview;
        private double displayDeadline;
        private DisplayConfiguration previousDisplay;

        private void LoadFrontendPreferences()
        {
            preferences=new GamePreferences();
            bool restored=false;
            try
            {
                if(PlayerPrefs.HasKey(PoolroomsGameSettings.PreferencesKey))
                {
                    var loaded=JsonUtility.FromJson<GamePreferences>(PlayerPrefs.GetString(PoolroomsGameSettings.PreferencesKey));
                    if(loaded!=null&&loaded.version==18){preferences=loaded;restored=true;}
                }
            }
            catch(ArgumentException){preferences=new GamePreferences();}
            if(!restored&&template!=null)
            {
                var source=template.GetComponentInChildren<PoolroomsExplorer>(true);
                if(source!=null)
                {
                    preferences.sensitivity=source.lookSensitivity;preferences.walkSpeed=source.walkSpeed;
                    preferences.sprintSpeed=source.sprintSpeed;preferences.allowSlide=source.allowSlide;
                    preferences.slideSpeed=source.slideSpeed;
                    var camera=source.GetComponentInChildren<Camera>(true);
                    if(camera!=null)preferences.fieldOfView=camera.fieldOfView;
                    if(source.world!=null)foreach(var component in source.world.GetComponents<MonoBehaviour>())
                        if(component is IPoolroomsGraphicsSettings graphics)graphics.CaptureSettings(preferences);
                }
            }
            preferences.Sanitize();
        }

        private void SaveFrontendPreferences()
        {
            preferences.Sanitize();
            if(actor!=null)
            {
                var settings=actor.GetComponent<PoolroomsGameSettings>();
                if(settings!=null){settings.ApplyFrontendPreferences(preferences);return;}
            }
            PlayerPrefs.SetString(PoolroomsGameSettings.PreferencesKey,JsonUtility.ToJson(preferences));
            PlayerPrefs.Save();
        }

        private void OptionToggle(string title,bool value,Action<bool> change,VisualElement parent)
        {
            var control=new Toggle(title){value=value};control.AddToClassList("fe-field");parent.Add(control);
            control.RegisterValueChangedCallback(e=>{change(e.newValue);SaveFrontendPreferences();ResizeFrontend();});
        }

        private void OptionSlider(string title,float min,float max,float value,Action<float> change,VisualElement parent,bool whole=false)
        {
            var row=Node("fe-option",parent);var label=Text(title+" · "+value.ToString(whole?"0":"0.00"),"fe-note",row);
            var slider=new Slider(min,max){value=value};slider.AddToClassList("fe-slider");row.Add(slider);
            slider.RegisterValueChangedCallback(e=>
            {
                float actual=whole?Mathf.Round(e.newValue):e.newValue;
                change(actual);SaveFrontendPreferences();label.text=title+" · "+actual.ToString(whole?"0":"0.00");
            });
        }

        private void OptionChoice(string title,List<string> choices,int index,Action<int> change,VisualElement parent)
        {
            var dropdown=new DropdownField(title,choices,Mathf.Clamp(index,0,choices.Count-1));dropdown.AddToClassList("fe-field");parent.Add(dropdown);
            dropdown.RegisterValueChangedCallback(_=>{change(dropdown.index);SaveFrontendPreferences();});
        }

        private void DrawFrontendOptions()
        {
            var tabs=Node("fe-tabs",pageBody);
            string[] names={"Pantalla","Gráficos","Sonido","Controles"};
            for(int i=0;i<names.Length;i++)
            {
                int at=i;var tab=MenuButton(names[i],()=>{optionsTab=at;DrawPage(FrontendPage.Options);},tabs);
                tab.EnableInClassList("fe-selected",at==optionsTab);
            }
            var scroll=Scroll(pageBody);
            switch(optionsTab)
            {
                case 0:DrawFrontendDisplay(scroll);break;
                case 1:
                    OptionChoice("Calidad",new List<string>{"Conservar perfil","Rendimiento","Equilibrada","Alta","Ultra"},preferences.graphicsQuality+1,i=>preferences.graphicsQuality=i-1,scroll);
                    OptionSlider("Distancia de salas · 0 conserva el perfil",0,12,preferences.rendering.roomRadius,v=>preferences.rendering.roomRadius=(int)v,scroll,true);
                    OptionSlider("Exposición",0,12,preferences.exposureEV,v=>preferences.exposureEV=v,scroll);
                    OptionToggle("Reflejos",preferences.reflections,v=>preferences.reflections=v,scroll);
                    OptionToggle("Iluminación indirecta",preferences.indirectLight,v=>preferences.indirectLight=v,scroll);
                    OptionToggle("Oclusión ambiental",preferences.ambientOcclusion,v=>preferences.ambientOcclusion=v,scroll);
                    OptionToggle("Niebla volumétrica",preferences.volumetricFog,v=>preferences.volumetricFog=v,scroll);
                    OptionChoice("Pasto",new List<string>(Enum.GetNames(typeof(TerrainGrassQuality))),(int)preferences.terrainGrass,i=>preferences.terrainGrass=(TerrainGrassQuality)i,scroll);
                    OptionChoice("Agua",new List<string>(Enum.GetNames(typeof(PoolWaterAppearance))),(int)preferences.waterAppearance,i=>preferences.waterAppearance=(PoolWaterAppearance)i,scroll);
                    Text("La calidad elegida se aplica al crear o retomar el recorrido. El ray tracing necesita HDRP, una API y una GPU compatibles. Las opciones avanzadas siguen disponibles con F7 durante la partida.","fe-note",scroll);
                    break;
                case 2:
                    OptionSlider("Volumen general",0,1,preferences.masterVolume,v=>preferences.masterVolume=v,scroll);
                    OptionSlider("Ambiente",0,1,preferences.environmentVolume,v=>preferences.environmentVolume=v,scroll);
                    OptionSlider("Pasos",0,1,preferences.footstepVolume,v=>preferences.footstepVolume=v,scroll);
                    OptionSlider("Agua",0,1,preferences.waterVolume,v=>preferences.waterVolume=v,scroll);
                    OptionSlider("Música personal",0,1,preferences.music.volume,v=>preferences.music.volume=v,scroll);
                    OptionToggle("Sonido espacial",preferences.spatialAmbience,v=>preferences.spatialAmbience=v,scroll);
                    OptionToggle("Reverberación",preferences.reverb,v=>preferences.reverb=v,scroll);
                    OptionToggle("Oclusión por paredes",preferences.soundOcclusion,v=>preferences.soundOcclusion=v,scroll);
                    break;
                default:
                    OptionSlider("Sensibilidad",.01f,.3f,preferences.sensitivity,v=>preferences.sensitivity=v,scroll);
                    OptionToggle("Invertir mirada vertical",preferences.invertY,v=>preferences.invertY=v,scroll);
                    OptionToggle("Alternar carrera",preferences.toggleSprint,v=>preferences.toggleSprint=v,scroll);
                    OptionToggle("Permitir deslizamiento",preferences.allowSlide,v=>preferences.allowSlide=v,scroll);
                    Text("WASD · movimiento\nEspacio · salto / doble salto\nShift · dash\nE · interactuar\nTab · inventario\n5 · barra rápida\nF5 · perspectiva\nF6 · foto\nF7 · renderizado\nEsc · pausa","fe-note",scroll);
                    Text("Los controles detallados, el vuelo con alas y las skins siguen disponibles en los ajustes completos de la partida.","fe-note",scroll);
                    break;
            }
        }

        private void DrawExperience()
        {
            var scroll=Scroll(pageBody);
            OptionChoice("Atmósfera",new List<string>{"Tranquila","Inquietante · sin sustos ni voces"},(int)preferences.soundscape.mode,i=>preferences.soundscape.mode=(SoundscapeMode)i,scroll);
            OptionSlider("Campo de visión",50,105,preferences.fieldOfView,v=>preferences.fieldOfView=v,scroll,true);
            OptionToggle("Reducir movimiento",preferences.reduceMotion,v=>preferences.reduceMotion=v,scroll);
            OptionToggle("Reducir destellos",preferences.reducedFlashes,v=>preferences.reducedFlashes=v,scroll);
            OptionToggle("Alto contraste",preferences.highContrast,v=>preferences.highContrast=v,scroll);
            OptionSlider("Escala de interfaz",.85f,2,preferences.uiScale,v=>{preferences.uiScale=v;ResizeFrontend();},scroll);
            Text("El modo tranquilo conserva los sonidos del agua, los pasos y la vida de las salas. Puedes cambiar de ambiente sin modificar la semilla ni perder tu posición.","fe-note",scroll);
        }

        private void DrawFrontendDisplay(VisualElement parent)
        {
            var limits=new List<string>{"Sin límite","30","45","60","90","120","144"};
            int[] frames={-1,30,45,60,90,120,144};
            OptionChoice("Límite de FPS",limits,Array.IndexOf(frames,preferences.frameLimit),i=>{preferences.frameLimit=frames[i];Application.targetFrameRate=frames[i];},parent);
            OptionChoice("Sincronización vertical",new List<string>{"Desactivada","Cada fotograma","Cada dos fotogramas"},preferences.vSync,i=>{preferences.vSync=i;QualitySettings.vSyncCount=i;},parent);
            var resolutions=new List<Vector2Int>();var labels=new List<string>();
            void Add(int width,int height){var size=new Vector2Int(width,height);if(width<640||height<360||resolutions.Contains(size))return;resolutions.Add(size);labels.Add(width+" × "+height);}
            Add(Screen.width,Screen.height);foreach(var supported in Screen.resolutions)Add(supported.width,supported.height);
            if(resolutions.Count==0)Add(1280,720);
            var resolution=new DropdownField("Resolución",labels,0);resolution.AddToClassList("fe-field");parent.Add(resolution);
            var mode=new DropdownField("Ventana",new List<string>{"Ventana","Sin bordes","Pantalla completa"},Screen.fullScreenMode==FullScreenMode.Windowed?0:Screen.fullScreenMode==FullScreenMode.ExclusiveFullScreen?2:1);
            mode.AddToClassList("fe-field");parent.Add(mode);
            MenuButton("Probar pantalla",()=>
            {
                previousDisplay=PoolroomsGameSettings.CurrentDisplay();displayPreview=true;displayDeadline=Time.realtimeSinceStartupAsDouble+15;
                var size=resolutions[resolution.index];var window=mode.index==0?FullScreenMode.Windowed:mode.index==2?FullScreenMode.ExclusiveFullScreen:FullScreenMode.FullScreenWindow;
                Screen.SetResolution(size.x,size.y,window);
                var box=CreateFrontendDialog("¿Conservar esta pantalla?","Se restaurará la anterior en 15 segundos si no confirmas.");
                MenuButton("Conservar",()=>{displayPreview=false;var actual=PoolroomsGameSettings.CurrentDisplay();preferences.screenWidth=actual.Width;preferences.screenHeight=actual.Height;preferences.windowMode=actual.Mode;SaveFrontendPreferences();CloseFrontendDialog();},box);
                MenuButton("Restaurar",RevertFrontendDisplay,box);
                box.schedule.Execute(()=>box.Q<Button>()?.Focus());
            },parent);
        }

        private void TickFrontendDisplay()
        {if(displayPreview&&Time.realtimeSinceStartupAsDouble>=displayDeadline)RevertFrontendDisplay();}

        private void RevertFrontendDisplay()
        {
            if(!displayPreview)return;displayPreview=false;
            var mode=previousDisplay.Mode==GameWindowMode.Window?FullScreenMode.Windowed:previousDisplay.Mode==GameWindowMode.Exclusive?FullScreenMode.ExclusiveFullScreen:FullScreenMode.FullScreenWindow;
            Screen.SetResolution(previousDisplay.Width,previousDisplay.Height,mode);CloseFrontendDialog();
        }
    }
}
