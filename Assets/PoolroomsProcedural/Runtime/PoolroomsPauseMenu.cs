using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsPauseMenu : MonoBehaviour
    {
        public PoolroomsCameraEffects cameraEffects;
        [SerializeField] private string preferencesKey="Poolrooms.Camera.v1";
        private PoolroomsExplorer explorer;
        private PoolroomsGameSettings gameSettings;
        private bool paused,settings,atTitle,journeyStarted;
        private Button headerAction;
        private float previousTimeScale;
        private bool previousAudioPause,previousCursorVisible;
        private CursorLockMode previousCursorLock;
        private GameObject menuObject;
        private PanelSettings panelSettings;
        private VisualElement overlay,shell,body,navigation,home,settingsBody,modal;
        private ScrollView content;
        private TextField search;
        private Label title,subtitle,saveLabel,previewLabel,searchCaption;
        private Toggle locomotionToggle;
        private Toggle cameraMotionToggle,indirectToggle,fogToggle;
        private Slider sprintSpeedSlider;
        private Label sprintSpeedLabel;
        private SettingsCategory category=SettingsCategory.Display;
        private readonly List<VisualElement> groups=new List<VisualElement>();
        private readonly List<SettingRow> rows=new List<SettingRow>();
        private readonly List<Button> tabs=new List<Button>();
        private readonly string[] categories={"Pantalla","Gráficos","Cámara","Sonido","Controles","Comodidad","Skins"};
        private readonly string[] captions={"A tu pantalla y a tu ritmo.","Luz, reflejos y detalle.","Encuentra tu manera de mirar.","Escucha el espacio.","Una exploración cómoda.","Pequeños ajustes para sentirte bien.","Tu personaje y sus movimientos."};
        private struct SettingRow {public VisualElement Element;public int Group;public string Search;}
        private VisualElement currentGroup;
        private int groupIndex,lastPreviewSecond=-1;
        private DisplayConfiguration displayDraft;
        private List<DisplayConfiguration> resolutions;
        private List<GameWindowMode> windowModes;
        private string query="";
        public bool IsPaused {get{return paused;}}
        public bool IsSettingsOpen {get{return paused&&settings;}}
        public PoolroomsGameSettings Settings {get{return gameSettings;}}
        private GamePreferences Prefs {get{return gameSettings.Preferences;}}

        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            explorer=GetComponent<PoolroomsExplorer>();gameSettings=GetComponent<PoolroomsGameSettings>();
            if(gameSettings==null)gameSettings=gameObject.AddComponent<PoolroomsGameSettings>();
            gameSettings.Initialize(preferencesKey);gameSettings.Changed+=RefreshAppearance;gameSettings.DisplayChanged+=DisplayChanged;
            if(cameraEffects==null)cameraEffects=GetComponentInChildren<PoolroomsCameraEffects>();ApplyPreferences();
        }
        public void ApplyPreferences(){if(gameSettings!=null){gameSettings.Initialize(preferencesKey);gameSettings.BindCamera(cameraEffects);}}
        public void TogglePause()
        {
            if(gameSettings!=null&&gameSettings.Display.Pending){gameSettings.RevertDisplay();return;}
            if(modal!=null){CloseModal();return;}
            if(!paused)Pause();else if(settings)ShowHome();else if(!atTitle)Resume(true);
        }
        public void OpenStartMenu()
        {
            if(PoolroomsFrontend.Instance!=null&&PoolroomsFrontend.Instance.Owns(explorer))
            {PoolroomsFrontend.Instance.ReturnToMenu();return;}
            atTitle=true;
            if(!paused)Pause();else ShowHome();
        }
        public void BeginExploration()
        {
            atTitle=false;journeyStarted=true;Resume(true);
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        }
        private void ConfirmExit()
        {
            var dialog=CreateModal("Cerrar la exploración", "Los ajustes se conservan. La posición y los objetos movidos no se guardan entre sesiones.");
            ActionButton("Salir",()=>{gameSettings?.Flush();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            },"pr-primary",dialog);
            ActionButton("Seguir aquí",CloseModal,"pr-secondary",dialog);
        }
        public void Pause()
        {
            if(PoolroomsFrontend.Instance!=null&&PoolroomsFrontend.Instance.Flow.Visible&&PoolroomsFrontend.Instance.Owns(explorer))return;
            explorer?.Inventory?.CloseInterface();explorer?.Views?.ExitPhoto();
            if(paused||!isActiveAndEnabled||explorer==null||!explorer.isActiveAndEnabled)return;
            var master=explorer.MasterMode;if(master!=null)master.CloseInterface();
            previousTimeScale=Time.timeScale;previousAudioPause=AudioListener.pause;previousCursorLock=Cursor.lockState;previousCursorVisible=Cursor.visible;
            paused=true;settings=false;explorer.SetPaused(true);Time.timeScale=0;AudioListener.pause=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            try{EnsureUI();ShowHome();overlay.style.display=DisplayStyle.Flex;RefreshAppearance();}
            catch(Exception error){Resume(true);DestroyUI();Debug.LogException(error,this);}
        }
        public void OpenSettings(SettingsCategory target=SettingsCategory.Display)
        {
            if(!Enum.IsDefined(typeof(SettingsCategory),target))target=SettingsCategory.Display;
            if(!paused)Pause();if(!paused)return;
            category=target;settings=true;BuildSettings();title.text="Ajustes";subtitle.text=captions[(int)category];
            home.style.display=DisplayStyle.None;settingsBody.style.display=DisplayStyle.Flex;RefreshAppearance();Filter();tabs[(int)category].Focus();
        }
        public void OpenRenderingSettings()
        {
            gameSettings?.RevertDisplay();CloseModal();query="";OpenSettings(SettingsCategory.Graphics);
            if(content==null)return;var anchor=content.Q<VisualElement>("rendering-controls");
            if(anchor!=null)content.schedule.Execute(()=>{if(IsSettingsOpen&&anchor.parent!=null)content.ScrollTo(anchor);});
        }
        private void Resume(bool restoreCursor)
        {
            if(!paused)return;gameSettings?.RevertDisplay();CloseModal();paused=false;settings=false;
            Time.timeScale=previousTimeScale;AudioListener.pause=previousAudioPause;if(overlay!=null)overlay.style.display=DisplayStyle.None;
            if(explorer!=null)explorer.SetPaused(false);
            if(restoreCursor){Cursor.lockState=previousCursorLock;Cursor.visible=previousCursorVisible;}else{Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            gameSettings?.Flush();
        }
        public void ReleasePause(){Resume(false);}
        private void OnApplicationFocus(bool focused){if(!focused&&Application.isPlaying&&(gameSettings==null||Prefs.pauseOnFocusLoss))Pause();}
        private void Update()
        {
            if(!paused||gameSettings==null||!gameSettings.Display.Pending||previewLabel==null)return;
            int seconds=Mathf.CeilToInt(gameSettings.Display.Remaining);if(seconds==lastPreviewSecond)return;
            lastPreviewSecond=seconds;previewLabel.text="Regreso automático en "+seconds+" s";
        }
        private void OnDisable()
        {
            Resume(false);
            if(gameSettings!=null){gameSettings.Changed-=RefreshAppearance;gameSettings.DisplayChanged-=DisplayChanged;gameSettings.Flush();}
            DestroyUI();
        }
        private static VisualElement Element(string css,VisualElement parent)
        {var v=new VisualElement();v.AddToClassList(css);parent?.Add(v);return v;}
        private static Label Label(string text,string css,VisualElement parent)
        {var v=new Label(text){enableRichText=false};v.AddToClassList(css);parent?.Add(v);return v;}
        private static Button ActionButton(string text,Action action,string css,VisualElement parent)
        {var v=new Button(action){text=text};v.AddToClassList(css);parent?.Add(v);return v;}
        private void EnsureUI()
        {
            if(menuObject!=null)return;
            panelSettings=ScriptableObject.CreateInstance<PanelSettings>();panelSettings.name="Poolrooms minimal settings";
            panelSettings.scaleMode=PanelScaleMode.ConstantPixelSize;panelSettings.referenceResolution=new Vector2Int(1280,800);
            panelSettings.themeStyleSheet=Resources.Load<ThemeStyleSheet>("PoolroomsTheme");
            menuObject=new GameObject("Poolrooms pause interface");menuObject.transform.SetParent(transform,false);
            var document=menuObject.AddComponent<UIDocument>();document.panelSettings=panelSettings;document.sortingOrder=300;
            overlay=document.rootVisualElement;overlay.AddToClassList("pr-root");
            var styles=Resources.Load<StyleSheet>("PoolroomsSettings");
            if(styles==null)throw new InvalidOperationException("Copia Runtime/Resources/PoolroomsSettings.uss con la actualización.");
            overlay.styleSheets.Add(styles);shell=Element("pr-shell",overlay);
            var header=Element("pr-header",shell);var brand=Element("pr-heading",header);
            Label("ESPACIOS  /  EXPLORACIÓN", "pr-eyebrow",brand);title=Label("Pausa","pr-title",brand);subtitle=Label("Tu tiempo, tu ritmo.","pr-muted",brand);
            headerAction=ActionButton("Continuar  ·  Esc",()=>{if(atTitle)ShowHome();else Resume(true);},"pr-quiet-button",header);
            body=Element("pr-body",shell);home=new ScrollView(ScrollViewMode.Vertical);home.AddToClassList("pr-home");body.Add(home);settingsBody=Element("pr-settings",body);
            overlay.RegisterCallback<GeometryChangedEvent>(_=>Responsive());
            settingsBody.style.display=DisplayStyle.None;overlay.style.display=DisplayStyle.None;
        }
        private void ShowHome()
        {
            if(home==null)return;settings=false;query="";CloseModal();home.Clear();settingsBody.style.display=DisplayStyle.None;home.style.display=DisplayStyle.Flex;
            title.text=atTitle?"Espacios sin fin":"Pausa";subtitle.text=atTitle?"Un mundo para perder la noción del tiempo.":"Tu tiempo, tu ritmo.";
            if(headerAction!=null){headerAction.text=atTitle?"Inicio":"Continuar  ·  Esc";}
            shell.EnableInClassList("pr-title-screen",atTitle);
            var welcome=Element("pr-welcome",home);Label("UN MOMENTO DE CALMA","pr-eyebrow",welcome);
            Label(atTitle?"Hay otro lugar\nal otro lado.":"El recorrido\npuede esperar.","pr-home-title",welcome);
            var room=explorer.world!=null?explorer.world.RoomAtViewer():null;
            Label(explorer.world!=null&&explorer.world.InsideMall?"Centro comercial":room!=null?InteriorPlanner.Name(room.Environment):"Explora a tu ritmo.","pr-muted",welcome);
            var actions=Element("pr-home-actions",home);
            ActionButton(atTitle?(journeyStarted?"Retomar recorrido":"Comenzar exploración"):"Continuar explorando",()=>{if(atTitle)BeginExploration();else Resume(true);},"pr-primary",actions);
            ActionButton("Ajustes",()=>OpenSettings(category),"pr-secondary",actions);
            if(!atTitle)ActionButton("Modo foto · F6",()=>{Resume(true);explorer.Views?.EnterPhoto();},"pr-secondary",actions);
            ActionButton("Ver controles",()=>OpenSettings(SettingsCategory.Controls),"pr-secondary",actions);
            ActionButton("Créditos y licencias",ShowAssetCredits,"pr-quiet-button",actions);
            if(!atTitle&&explorer.MasterMode!=null&&explorer.MasterMode.IsActive)
                ActionButton("Modo administrador",()=>{Resume(true);explorer.MasterMode.ToggleMenu();},"pr-quiet-button",actions);
            if(!atTitle)ActionButton("Volver al inicio",OpenStartMenu,"pr-quiet-button",actions);
            if(atTitle){
                ActionButton("Salir del juego",ConfirmExit,"pr-quiet-button",actions);
                if(explorer.world!=null&&explorer.world.profile!=null)Label("Semilla del recorrido · "+explorer.world.profile.seed,"pr-footnote",home);
                Label("Retomar conserva esta sesión abierta; todavía no hay guardado de partida.","pr-footnote",home);
            }
            Label("Sin daño. Sin prisa. Sin objetivos obligatorios.","pr-footnote",home);
            home.schedule.Execute(()=>home.Q<Button>()?.Focus());Responsive();
        }
        private void BuildSettings()
        {
            settingsBody.Clear();rows.Clear();groups.Clear();tabs.Clear();resolutions=null;windowModes=null;displayDraft=PoolroomsGameSettings.CurrentDisplay();
            navigation=Element("pr-navigation",settingsBody);
            for(int i=0;i<categories.Length;i++)
            {
                int index=i;var tab=ActionButton(categories[i],()=>SelectCategory((SettingsCategory)index),"pr-tab",navigation);tabs.Add(tab);
            }
            var main=Element("pr-main",settingsBody);search=new TextField(){name="settings-search",value=query,isDelayed=false};search.AddToClassList("pr-search");search.tooltip="Buscar opciones en todas las categorías";
            main.Add(search);searchCaption=Label("Buscar opciones…","pr-search-caption",main);searchCaption.pickingMode=PickingMode.Ignore;
            search.RegisterValueChangedCallback(e=>{query=e.newValue??"";searchCaption.style.display=string.IsNullOrEmpty(query)?DisplayStyle.Flex:DisplayStyle.None;Filter();});
            search.RegisterCallback<FocusInEvent>(_=>searchCaption.style.display=DisplayStyle.None);
            search.RegisterCallback<FocusOutEvent>(_=>searchCaption.style.display=string.IsNullOrEmpty(query)?DisplayStyle.Flex:DisplayStyle.None);
            content=new ScrollView(ScrollViewMode.Vertical);content.AddToClassList("pr-content");main.Add(content);
            for(int i=0;i<categories.Length;i++)
            {
                groupIndex=i;currentGroup=Element("pr-group",content);currentGroup.name="settings-group-"+i;groups.Add(currentGroup);Label(categories[i],"pr-section-title",currentGroup);
                switch((SettingsCategory)i){case SettingsCategory.Display:DrawDisplay();break;case SettingsCategory.Graphics:DrawGraphics();break;
                    case SettingsCategory.Camera:DrawCamera();break;case SettingsCategory.Audio:DrawAudio();break;case SettingsCategory.Controls:DrawControls();break;case SettingsCategory.Skins:DrawSkins();break;default:DrawComfort();break;}
            }
            var footer=Element("pr-footer",main);saveLabel=Label("Cambios guardados automáticamente","pr-footnote",footer);
            ActionButton("Restablecer categoría",ConfirmReset,"pr-quiet-button",footer);ActionButton("Volver",ShowHome,"pr-quiet-button",footer);
            Filter();Responsive();
        }
        private void SelectCategory(SettingsCategory target)
        {
            category=target;query="";search.SetValueWithoutNotify("");if(searchCaption!=null)searchCaption.style.display=DisplayStyle.Flex;subtitle.text=captions[(int)category];Filter();content.scrollOffset=Vector2.zero;
        }
        private void Filter()
        {
            if(content==null)return;string term=query.Trim();var visible=new bool[groups.Count];
            foreach(var row in rows)
            {
                bool match=term.Length==0?row.Group==(int)category:SettingsSearch.Matches(row.Search,term);
                row.Element.style.display=match?DisplayStyle.Flex:DisplayStyle.None;if(match)visible[row.Group]=true;
            }
            for(int i=0;i<groups.Count;i++)groups[i].style.display=visible[i]?DisplayStyle.Flex:DisplayStyle.None;
            for(int i=0;i<tabs.Count;i++)tabs[i].EnableInClassList("pr-selected",i==(int)category);
            content.EnableInClassList("pr-empty",Array.IndexOf(visible,true)<0);
            var empty=content.Q<Label>("empty-results");
            if(empty==null){empty=Label("No hay opciones con ese nombre.","pr-muted",content);empty.name="empty-results";}
            empty.style.display=Array.IndexOf(visible,true)<0?DisplayStyle.Flex:DisplayStyle.None;
        }
        private VisualElement Row(string name,string description,out VisualElement control,bool enabled=true)
        {
            var row=Element("pr-setting",currentGroup);var copy=Element("pr-setting-copy",row);Label(name,"pr-setting-title",copy);
            if(!string.IsNullOrEmpty(description))Label(description,"pr-setting-description",copy);
            control=Element("pr-control",row);control.SetEnabled(enabled);
            rows.Add(new SettingRow{Element=row,Group=groupIndex,Search=name+" "+description+" "+categories[groupIndex]});return row;
        }
        private void Note(string text)
        {var note=Label(text,"pr-note",currentGroup);rows.Add(new SettingRow{Element=note,Group=groupIndex,Search=text+" "+categories[groupIndex]});}
        private void Changed(SettingsChange flags){gameSettings.Change(flags);if(saveLabel!=null)saveLabel.text="Cambios aplicados · guardado automático";}
        private Toggle Toggle(string name,string description,bool value,Action<bool> setter,SettingsChange flags,bool enabled=true)
        {
            Row(name,description,out var control,enabled);var toggle=new Toggle(){value=value};toggle.AddToClassList("pr-toggle");toggle.tooltip=name;control.Add(toggle);
            toggle.RegisterValueChangedCallback(e=>{setter(e.newValue);Changed(flags);});return toggle;
        }
        private Slider Slider(string name,string description,float min,float max,float value,Action<float> setter,SettingsChange flags,string unit="",bool percent=false,bool enabled=true,Func<float> readValue=null)
        {
            Row(name,description,out var control,enabled);var slider=new Slider(min,max){value=value};slider.AddToClassList("pr-slider");slider.tooltip=name;
            var number=Label(Format(value,unit,percent),"pr-value",control);control.Insert(0,slider);
            slider.RegisterValueChangedCallback(e=>{setter(e.newValue);Changed(flags);float actual=readValue!=null?readValue():e.newValue;slider.SetValueWithoutNotify(actual);number.text=Format(actual,unit,percent);});
            return slider;
        }
        private static string Format(float value,string unit,bool percent)
        {return percent?Mathf.RoundToInt(value*100)+" %":value.ToString(unit=="°"?"0":"0.00")+unit;}
        private void Choice(string name,string description,List<string> choices,int index,Action<int> setter,bool enabled=true)
        {
            Row(name,description,out var control,enabled);var dropdown=new DropdownField(choices,Mathf.Clamp(index,0,choices.Count-1));dropdown.AddToClassList("pr-dropdown");dropdown.tooltip=name;control.Add(dropdown);
            dropdown.RegisterValueChangedCallback(e=>{int selected=choices.IndexOf(e.newValue);if(selected>=0)setter(selected);});
        }
        private void DrawDisplay()
        {
            windowModes=new List<GameWindowMode>{GameWindowMode.Window,GameWindowMode.Borderless};var names=new List<string>{"Ventana","Pantalla completa"};
            if(gameSettings.ExclusiveAvailable){windowModes.Add(GameWindowMode.Exclusive);names.Add("Completa exclusiva");}
            if(gameSettings.MaximizedAvailable){windowModes.Add(GameWindowMode.Maximized);names.Add("Ventana maximizada");}
            Choice("Modo de ventana","La pantalla completa sin bordes facilita cambiar de aplicación.",names,windowModes.IndexOf(displayDraft.Mode),i=>displayDraft=new DisplayConfiguration(displayDraft.Width,displayDraft.Height,windowModes[i]),gameSettings.DisplayAvailable);
            resolutions=new List<DisplayConfiguration>();var resolutionNames=new List<string>();var seen=new HashSet<string>();
            foreach(var resolution in Screen.resolutions)
            {
                var value=new DisplayConfiguration(resolution.width,resolution.height,displayDraft.Mode);string label=resolution.width+" × "+resolution.height;
                if(value.Valid&&seen.Add(label)){resolutions.Add(value);resolutionNames.Add(label);}
            }
            string current=displayDraft.Width+" × "+displayDraft.Height;
            if(seen.Add(current)){resolutions.Add(displayDraft);resolutionNames.Add(current);}
            Choice("Resolución","Prueba el cambio antes de conservarlo.",resolutionNames,resolutionNames.IndexOf(current),i=>displayDraft=new DisplayConfiguration(resolutions[i].Width,resolutions[i].Height,displayDraft.Mode),gameSettings.DisplayAvailable);
            Row("Aplicar pantalla","Tienes 15 segundos para confirmar; después vuelve a la anterior.",out var apply,gameSettings.DisplayAvailable);
            ActionButton("Probar cambio",()=>{if(!gameSettings.PreviewDisplay(displayDraft)&&saveLabel!=null)saveLabel.text="La pantalla ya usa esa configuración o el cambio no está disponible.";},"pr-secondary",apply);
            int[] rates={-1,30,45,60,90,120,144};
            Choice("Límite de FPS","Con VSync activo, la pantalla controla la frecuencia efectiva.",new List<string>{"Sin límite","30 FPS","45 FPS","60 FPS","90 FPS","120 FPS","144 FPS"},Array.IndexOf(rates,Prefs.frameLimit),i=>{Prefs.frameLimit=rates[i];Changed(SettingsChange.Timing);});
            Choice("Sincronización vertical","Reduce las líneas de corte de imagen.",new List<string>{"Desactivada","Cada refresco","Cada 2 refrescos"},Prefs.vSync,i=>{Prefs.vSync=i;Changed(SettingsChange.Timing);},gameSettings.DisplayAvailable);
        }
        private void DrawGraphics()
        {
            DrawRendering();
            bool available=gameSettings.GraphicsAvailable;
            Row("Calidad gráfica","Rendimiento prioriza fluidez; Alto añade detalle de luz y agua.",out var cards,available);cards.AddToClassList("pr-quality-cards");
            string[] names={"Rendimiento","Alto","Cinemático RT","Ultra"};var qualityButtons=new List<Button>();
            for(int i=0;i<3;i++)
            {
                int quality=i;var button=ActionButton(names[i],()=>{Prefs.graphicsQuality=quality;Changed(SettingsChange.Graphics);foreach(var b in qualityButtons)b.EnableInClassList("pr-selected",qualityButtons.IndexOf(b)==quality);},"pr-choice",cards);
                button.EnableInClassList("pr-selected",Prefs.graphicsQuality==i);if(i==2)button.SetEnabled(gameSettings.RayTracingAvailable);qualityButtons.Add(button);
            }
            if(!available)Note("Los ajustes de iluminación y agua estarán disponibles al preparar los gráficos de la escena.");
            else if(!gameSettings.RayTracingAvailable)Note("Cinemático RT no está disponible. Ultra usa SSR/SSGI compatibles y tiene un coste alto; para este equipo comienza con Rendimiento.");
            Slider("Exposición","Un valor menor deja entrar más luz.",0,12,Prefs.exposureEV,v=>Prefs.exposureEV=v,SettingsChange.Graphics," EV",enabled:available);
            Toggle("Reflejos","Superficies reflectantes y capturas del entorno.",Prefs.reflections,v=>Prefs.reflections=v,SettingsChange.Graphics,gameSettings.Supports(SettingsGraphicsFeature.Reflections));
            indirectToggle=Toggle("Iluminación indirecta","Luz que rebota entre superficies. Alto o Cinemático.",Prefs.indirectLight,v=>Prefs.indirectLight=v,SettingsChange.Graphics,gameSettings.Supports(SettingsGraphicsFeature.IndirectLight));
            Toggle("Oclusión ambiental","Sombras suaves en contactos y esquinas.",Prefs.ambientOcclusion,v=>Prefs.ambientOcclusion=v,SettingsChange.Graphics,gameSettings.Supports(SettingsGraphicsFeature.AmbientOcclusion));
            fogToggle=Toggle("Niebla volumétrica","Profundidad de la luz. Alto o Cinemático.",Prefs.volumetricFog,v=>Prefs.volumetricFog=v,SettingsChange.Graphics,gameSettings.Supports(SettingsGraphicsFeature.Volumetrics));
            int[] lights={-1,8,12,18,24,32},shadows={-1,0,1,2,4,6};
            Choice("Luces simultáneas","Menos luces pueden mejorar la fluidez.",new List<string>{"Según calidad","8","12","18","24","32"},Array.IndexOf(lights,Prefs.lightBudget),i=>{Prefs.lightBudget=lights[i];Changed(SettingsChange.Graphics);},available);
            Choice("Luces con sombras","Uno de los ajustes con mayor coste gráfico.",new List<string>{"Según calidad","Sin sombras","1","2","4","6"},Array.IndexOf(shadows,Prefs.shadowBudget),i=>{Prefs.shadowBudget=shadows[i];Changed(SettingsChange.Graphics);},available);
            bool water=gameSettings.Supports(SettingsGraphicsFeature.Water),interactions=gameSettings.Supports(SettingsGraphicsFeature.WaterInteractions);
            Choice("Detalle del agua","Rendimiento limita los efectos más costosos.",new List<string>{"Según calidad","Ligero","Alto","Cinemático"},Prefs.waterDetail+1,i=>{Prefs.waterDetail=i-1;Changed(SettingsChange.Graphics);},water);
            Choice("Apariencia del agua","Conserva piscinas y profundidad; cambia su acabado.",new List<string>{"Cerámica clara","Turquesa suave","Porcelana azul","Personalizada de la escena","Cristalina"},(int)Prefs.waterAppearance,i=>{Prefs.waterAppearance=(PoolWaterAppearance)i;Changed(SettingsChange.Graphics);},water);
            Toggle("Ondas al entrar al agua","Respuesta local a pasos y salpicaduras.",Prefs.waterRipples,v=>Prefs.waterRipples=v,SettingsChange.Graphics,interactions);
            Toggle("Espuma de impacto","Disponible cuando la calidad y el agua lo permiten.",Prefs.waterFoam,v=>Prefs.waterFoam=v,SettingsChange.Graphics,interactions);
            Toggle("Efecto bajo el agua","Apariencia de la cámara al sumergirte.",Prefs.underwater,v=>Prefs.underwater=v,SettingsChange.Graphics,water);
        }
        private void DrawRendering()
        {
            var anchor=Row("Renderizado","F7 abre directamente este apartado. Disponible al explorar, sin modo maestro.",out var info);
            anchor.name="rendering-controls";
            var world=explorer!=null?explorer.world:null;
            var status=Label("","pr-footnote",info);
            Action update=()=>{status.text=world==null?"Sin mundo activo.":world.Terrain!=null&&world.Terrain.IsInside?
                "Nivel 94 · "+world.Terrain.LoadedCount+" fragmentos":world.InsideMall?"Centro comercial · "+world.LoadedMallSectorCount+" sectores":"Mundo · "+world.LoadedRoomCount+" salas";};
            update();status.schedule.Execute(update).Every(400);
            int[] rooms={0,1,2,3,4,6,8,12},terrain={0,2,3,4,5,6,7,8};
            Choice("Distancia de salas","Radio de carga. Más salas usan más RAM y tiempo de generación.",new List<string>{"Según escena","1 · 9 salas","2 · 25 salas","3 · 49 salas","4 · 81 salas","6 · 169 salas","8 · 289 salas","12 · 625 salas"},Array.IndexOf(rooms,Prefs.rendering.roomRadius),i=>{Prefs.rendering.roomRadius=rooms[i];Changed(SettingsChange.Graphics);});
            Choice("Distancia del Nivel 94","Radio de fragmentos de 64 m. El horizonte usa una malla más sencilla.",new List<string>{"Según escena","2","3","4","5","6","7","8"},Array.IndexOf(terrain,Prefs.rendering.terrainRadius),i=>{Prefs.rendering.terrainRadius=terrain[i];Changed(SettingsChange.Graphics);});
            Slider("Distancia de tiendas","0 conserva la escena; hasta 14 sectores vecinos.",0,14,Prefs.rendering.mallRadius,v=>Prefs.rendering.mallRadius=Mathf.RoundToInt(v),SettingsChange.Graphics,readValue:()=>Prefs.rendering.mallRadius);
            Slider("Pisos vecinos visibles","0 conserva la escena; 5 permite todos los pisos.",0,5,Prefs.rendering.mallFloorRadius,v=>Prefs.rendering.mallFloorRadius=Mathf.RoundToInt(v),SettingsChange.Graphics,readValue:()=>Prefs.rendering.mallFloorRadius);
            Choice("Detalle arquitectónico","Herrajes, rejillas y registros sobre paredes reales. Realista: hasta 12 por fragmento; Doble: hasta 24. Se ocultan a distancia y no cambian las colisiones.",new List<string>{"Original","Realista","Doble"},Prefs.dressingDensity,i=>{Prefs.dressingDensity=i;Changed(SettingsChange.Graphics);});
            Choice("Pasto del Nivel 94","Económico: cobertura densa con hojas simples próximas. Alto: hojas curvas, dos tonos y detalle según distancia. Desactivado conserva la textura del terreno.",new List<string>{"Desactivado","Económico","Alto"},(int)Prefs.terrainGrass,i=>{Prefs.terrainGrass=(TerrainGrassQuality)i;Changed(SettingsChange.Graphics);});
            Choice("Acabado del Nivel 94","Económico: terreno PBR y cielo de bajo coste. Alto: mezcla gradual de tierra y césped, cielo más definido y sombras de mayor resolución. El pasto se ajusta por separado.",new List<string>{"Económico","Alto"},(int)Prefs.terrainVisuals,i=>{Prefs.terrainVisuals=(TerrainVisualQuality)i;Changed(SettingsChange.Graphics);});
            Toggle("Visibilidad en zonas oscuras","Relleno suave sin sombras para ver el entorno. También funciona sin modo maestro.",Prefs.rendering.inspectionLight,v=>Prefs.rendering.inspectionLight=v,SettingsChange.Graphics);
            Slider("Brillo del relleno","Intensidad del apoyo de visibilidad; conserva la exposición y la linterna.",0,1,Prefs.rendering.lightStrength,v=>Prefs.rendering.lightStrength=v,SettingsChange.Graphics,percent:true);
        }
        private void DrawCamera()
        {
            var perspectives=new List<string>();foreach(CameraPerspective p in Enum.GetValues(typeof(CameraPerspective)))perspectives.Add(CameraPerspectives.Get(p).Name);
            Choice("Perspectiva · F5","Cambia la posición de cámara; las interacciones conservan el alcance del personaje.",perspectives,(int)Prefs.perspective,i=>{Prefs.perspective=(CameraPerspective)i;Changed(SettingsChange.Camera);});
            Row("Modo foto · F6","Cámara libre local, encuadre, inclinación y PNG sin interfaz.",out var photoControl);
            ActionButton("Entrar al modo foto",()=>{Resume(true);explorer.Views?.EnterPhoto();},"pr-secondary",photoControl).SetEnabled(!atTitle&&explorer.IsReady&&explorer.Views!=null);
            var camera=Prefs.camera;
            Row("Visualización","Cada estilo conserva la iluminación propia de las habitaciones.",out var modes);modes.AddToClassList("pr-camera-cards");
            var buttons=new List<Button>();
            for(int i=0;i<Enum.GetValues(typeof(CameraViewMode)).Length;i++)
            {
                int index=i;var look=CameraPresentation.Look((CameraViewMode)i);
                var button=ActionButton(look.Name,()=>{Prefs.camera.viewMode=(CameraViewMode)index;Changed(SettingsChange.Camera);foreach(var b in buttons)b.EnableInClassList("pr-selected",buttons.IndexOf(b)==index);},"pr-camera-card",modes);
                button.tooltip=look.Description;button.AddToClassList("pr-camera-"+i);button.EnableInClassList("pr-selected",(int)camera.viewMode==i);buttons.Add(button);
            }
            Slider("Campo de visión","Cuánto espacio entra en la imagen.",50,105,Prefs.fieldOfView,v=>Prefs.fieldOfView=v,SettingsChange.Camera,"°");
            Slider("Intensidad del estilo","Mezcla el aspecto elegido con la cámara normal.",0,1,camera.styleIntensity,v=>Prefs.camera.styleIntensity=v,SettingsChange.Camera,percent:true);
            Slider("Destellos de luz","Siguen el color y la intensidad de las luces reales.",0,1,camera.flareIntensity,v=>Prefs.camera.flareIntensity=v,SettingsChange.Camera,percent:true);
            Slider("Tamaño de destellos","Escala óptica de halos; no cambia la potencia de las lámparas.",.5f,1.5f,camera.flareSize,v=>Prefs.camera.flareSize=v,SettingsChange.Camera,percent:true);
            Slider("Reflejos de lente","Anillos y reflejos suaves alrededor de luces visibles.",0,1,camera.ghostIntensity,v=>Prefs.camera.ghostIntensity=v,SettingsChange.Camera,percent:true);
            locomotionToggle=Toggle("Balanceo al correr","Movimiento suave por desplazamiento real e inercia de la linterna.",camera.locomotionMotion,v=>Prefs.camera.locomotionMotion=v,SettingsChange.Camera);
            Toggle("Respuesta al aterrizar","Flexión suave y breve de la cámara al tocar el suelo. Reducir movimiento la desactiva.",camera.landingMotion,v=>Prefs.camera.landingMotion=v,SettingsChange.Camera);
            Slider("Intensidad del balanceo","Controla cámara y linterna; cero deja la vista estable.",0,1,camera.motionIntensity,v=>Prefs.camera.motionIntensity=v,SettingsChange.Camera,percent:true);
            Slider("Salpicaduras y gotas","Cantidad del efecto al tocar el agua.",0,1,camera.splashIntensity,v=>Prefs.camera.splashIntensity=v,SettingsChange.Camera,percent:true);
            Toggle("Gotas en la cámara","Efectos de contacto con el agua.",camera.waterEffects,v=>Prefs.camera.waterEffects=v,SettingsChange.Camera);
            cameraMotionToggle=Toggle("Movimiento al salpicar","Una reacción pequeña de cámara, sin sacudidas fuertes.",camera.cameraMotion,v=>Prefs.camera.cameraMotion=v,SettingsChange.Camera);
            Slider("Resplandor","Suavidad alrededor de las luces.",0,2,camera.bloomScale,v=>Prefs.camera.bloomScale=v,SettingsChange.Camera,percent:true);
            Slider("Grano de película","Textura del estilo elegido; Normal conserva imagen limpia.",0,1,camera.grainScale,v=>Prefs.camera.grainScale=v,SettingsChange.Camera,percent:true);
            Slider("Separación de color","Aberración cromática del estilo elegido.",0,1,camera.chromaticScale,v=>Prefs.camera.chromaticScale=v,SettingsChange.Camera,percent:true);
            Slider("Viñeta","Oscurecimiento suave en los bordes.",0,1,camera.vignetteScale,v=>Prefs.camera.vignetteScale=v,SettingsChange.Camera,percent:true);
        }
        private void DrawSkins()
        {
            DrawWings();
            var avatar=explorer.GetComponent<PoolroomsAvatar>();if(avatar==null)return;var skin=avatar.Preferences;
            Row("Importar modelo","OBJ directo. GLB/glTF requieren Unity glTFast. En el Editor también FBX, DAE, 3DS, DXF y BLEND.",out var input);
            input.AddToClassList("pr-skin-import");
            var path=new TextField{value="",tooltip="Ruta local completa al archivo 3D"};path.AddToClassList("pr-skin-path");input.Add(path);
            var fileActions=Element("pr-skin-actions",input);
            ActionButton("Examinar…",()=>{string file=AvatarFilePicker.Open();if(!string.IsNullOrEmpty(file))path.value=file;},"pr-secondary",fileActions);
            ActionButton("Importar",()=>avatar.LoadFile(path.value),"pr-primary",fileActions);
            Toggle("OBJ sencillo · sin huesos","Importa el modelo como una skin estática, sin intentar adaptarlo. Conserva materiales, sombras y movimiento del jugador. Actívalo antes de importar o pulsa Recalcular para aplicarlo.",skin.objStatic,v=>{skin.objStatic=v;avatar.SavePreferences();},SettingsChange.None);
            var definitions=avatar.Definitions();var files=avatar.ObjFiles();var names=new List<string>{"Sin skin"};
            foreach(var definition in definitions)names.Add(definition.name);foreach(var file in files)names.Add(System.IO.Path.GetFileName(file));
            string saved=PlayerPrefs.GetString("Poolrooms.Avatar.Selection","");int index=0;
            for(int i=0;i<definitions.Length;i++)if(saved=="asset:"+definitions[i].name)index=i+1;
            for(int i=0;i<files.Length;i++)if(saved=="obj:"+files[i]||saved=="file:"+files[i])index=1+definitions.Length+i;
            Choice("Skin seleccionada","Solo cambia la apariencia. Conserva colisiones y habilidades del jugador.",names,index,i=>
            {if(i==0)avatar.ResetAppearance();else if(i<=definitions.Length)avatar.Apply(definitions[i-1]);else avatar.LoadFile(files[i-definitions.Length-1]);});
            Row("Biblioteca local","Los modelos preparados en Unity permanecen fuera de la carpeta de código reemplazable.",out var library);
            ActionButton("Abrir carpeta",()=>{System.IO.Directory.CreateDirectory(avatar.Folder);Application.OpenURL(new Uri(avatar.Folder).AbsoluteUri);},"pr-secondary",library);
            ActionButton("Actualizar lista",()=>OpenSettings(SettingsCategory.Skins),"pr-secondary",library);
            Row("Estado de la skin","La adaptación automática necesita un humanoide de pie y centrado.",out var state);
            state.AddToClassList("pr-skin-status");
            var status=Label(avatar.Status,"pr-muted",state);status.schedule.Execute(()=>status.text=avatar.Status+" · "+avatar.AnimationState).Every(500);
            var credit=Label(avatar.Attribution,"pr-footnote",state);credit.schedule.Execute(()=>credit.text=avatar.Attribution).Every(500);
            Toggle("Animación común","Descanso, caminar, correr, salto, caída, dash, deslizamiento, natación y emotes.",skin.animate,v=>{skin.animate=v;avatar.SavePreferences();},SettingsChange.None);
            Slider("Intensidad corporal","Amplitud del movimiento. No cambia los efectos de cámara.",0,1.25f,skin.intensity,v=>{skin.intensity=v;avatar.SavePreferences();},SettingsChange.None,percent:true);
            Slider("Ritmo de animación","Ajuste de zancada y emotes.",.5f,1.5f,skin.animationSpeed,v=>{skin.animationSpeed=v;avatar.SavePreferences();},SettingsChange.None,percent:true);
            Choice("Emote · B","Se interrumpe al moverte, saltar, hacer dash o conducir.",new List<string>{"Ninguno","Saludar","Estirarse","Celebrar","Bailar","Reverencia","Sentarse"},(int)skin.emote,i=>{skin.emote=(HumanoidEmote)i;avatar.SavePreferences();});
            Row("Previsualizar emote","Cambia a tercera persona con F5 para verlo.",out var emote);
            ActionButton("Probar emote",()=>{Resume(true);avatar.PlayEmote(skin.emote);},"pr-secondary",emote).SetEnabled(!atTitle);
            Note("Ajuste para modelos sin huesos: cambia los controles siguientes y pulsa Recalcular. Los Humanoid válidos conservan sus pesos originales. Ningún algoritmo automático garantiza una buena deformación en cualquier malla; revisa hombros y rodillas.");
            Choice("Pose original","Cómo estaba el modelo antes de animarlo.",new List<string>{"Automática","Brazos en T","Brazos en A","Brazos hacia abajo"},(int)skin.restPose,i=>{skin.restPose=(HumanoidRestPose)i;avatar.SavePreferences();});
            Toggle("Modelo con eje Z vertical","Para exportaciones con otro eje vertical. Requiere recalcular.",skin.zUp,v=>{skin.zUp=v;avatar.SavePreferences();},SettingsChange.None);
            Slider("Orientación original","Corrige el frente antes de calcular pesos. Requiere recalcular.",0,360,avatar.Yaw,v=>avatar.SetYaw(v),SettingsChange.None,"°");
            Slider("Anchura de hombros","Solo rig automático. Requiere recalcular.",.65f,1.4f,skin.shoulderScale,v=>{skin.shoulderScale=v;avatar.SavePreferences();},SettingsChange.None,percent:true);
            Slider("Altura de caderas","Fracción de la altura total. Solo rig automático; requiere recalcular.",.43f,.60f,skin.hipHeight,v=>{skin.hipHeight=v;avatar.SavePreferences();},SettingsChange.None,percent:true);
            Toggle("Piezas rígidas por malla","Para figuras segmentadas: cada malla sigue un solo hueso. No usar para un cuerpo de una sola pieza.",skin.rigidPieces,v=>{skin.rigidPieces=v;avatar.SavePreferences();},SettingsChange.None);
            Toggle("Ver pose original","Pausa la animación visual para revisar el ajuste.",skin.previewBindPose,v=>{skin.previewBindPose=v;avatar.SavePreferences();},SettingsChange.None);
            Row("Aplicar ajuste anatómico","Vuelve a importar el original. Si falla, conserva la skin anterior.",out var rebuild);
            ActionButton("Recalcular adaptación",()=>avatar.Rebuild(),"pr-primary",rebuild);
            Toggle("Proyectar sombras","La skin recibe la iluminación y reflejos de la calidad seleccionada.",skin.castShadows,v=>{skin.castShadows=v;avatar.SavePreferences();},SettingsChange.None);
            Toggle("Ajustar acabado del material","Conserva texturas; sobrescribe metal y suavidad en copias del material. Desactiva para restaurar el original.",skin.materialOverride,v=>{skin.materialOverride=v;avatar.SavePreferences();},SettingsChange.None);
            Slider("Suavidad de la skin","Solo con ajuste de acabado activado.",0,1,skin.smoothness,v=>{skin.smoothness=v;avatar.SavePreferences();},SettingsChange.None,percent:true);
            Slider("Metal de la skin","Cero para tela y piel; aumenta solo en superficies metálicas.",0,1,skin.metallic,v=>{skin.metallic=v;avatar.SavePreferences();},SettingsChange.None,percent:true);
            Note("Ray tracing: geometría animada dinámica en Cinemático RT/Ultra con hardware compatible; en otras calidades usa iluminación y reflejos disponibles. No agrega luces ni sondas por personaje. GLB/glTF: instala com.unity.cloud.gltfast 6.7.1 desde Package Manager. BLEND requiere Blender en el equipo del Editor.");
        }
        private void DrawWings()
        {
            var wings=explorer.Wings!=null?explorer.Wings:explorer.GetComponent<PoolroomsWings>();if(wings==null)return;
            var preferences=wings.Preferences;
            Row("ALAS · HORIZONTE","Vuelo para cualquier jugador, sin desbloquear el modo maestro. 12 familias y 144 variantes de geometría.",out var preview);
            var image=new Image{image=Resources.Load<Texture2D>("WingSources/Icons/"+WingCatalog.Icons[preferences.design/WingCatalog.Variants]),scaleMode=ScaleMode.ScaleToFit};
            image.style.width=72;image.style.height=72;preview.Add(image);
            Toggle("Equipar alas","G activa o detiene el vuelo. Las alas se conservan al cambiar de skin.",preferences.equipped,v=>{preferences.equipped=v;wings.SavePreferences();},SettingsChange.None);
            Choice("Familia de alas","Ángel, serafín, cuervo, demonio, dragón, mariposa, libélula, robot, neón, luz, cristal o mecánico.",new List<string>(WingCatalog.FamilyNames),preferences.design/WingCatalog.Variants,i=>
            {preferences.design=i*WingCatalog.Variants+preferences.design%WingCatalog.Variants;wings.SavePreferences();image.image=Resources.Load<Texture2D>("WingSources/Icons/"+WingCatalog.Icons[i]);});
            var variants=new List<string>();for(int i=0;i<WingCatalog.Variants;i++)variants.Add(WingCatalog.Name(i).Split('·')[1].Trim());
            Choice("Diseño de la familia","Cambia la silueta, longitud, disposición de plumas, lóbulos o paneles; no solo el color.",variants,preferences.design%WingCatalog.Variants,i=>{preferences.design=preferences.design/WingCatalog.Variants*WingCatalog.Variants+i;wings.SavePreferences();});
            Choice("Velocidad de vuelo · H","Cuatro velocidades. H recorre la lista mientras exploras; no cambia la velocidad al caminar.",new List<string>(PoolroomsWings.SpeedNames),(int)preferences.speed,i=>{preferences.speed=(WingSpeed)i;wings.SavePreferences(false);});
            Toggle("Plegar al caminar","Las alas se despliegan suavemente al volar y se recogen al aterrizar.",preferences.foldOnGround,v=>{preferences.foldOnGround=v;wings.SavePreferences(false);},SettingsChange.None);
            Toggle("Sombras de las alas","Usan las luces y reflejos del nivel. En primera persona solo se proyecta su sombra.",preferences.castShadows,v=>{preferences.castShadows=v;wings.SavePreferences(false);},SettingsChange.None);
            Slider("Tamaño de las alas","Ajuste cosmético; las colisiones siguen usando el cuerpo del jugador.",.6f,1.5f,preferences.size,v=>{preferences.size=v;wings.SavePreferences(false);},SettingsChange.None,percent:true);
            Slider("Altura del anclaje","Ajusta el arnés a la espalda de tu modelo humanoide.",.55f,.85f,preferences.attachmentHeight,v=>{preferences.attachmentHeight=v;wings.SavePreferences(false);},SettingsChange.None,percent:true);
            Slider("Separación de la espalda","Compensa el volumen de la skin o su ropa.",.08f,.4f,preferences.backOffset,v=>{preferences.backOffset=v;wings.SavePreferences(false);},SettingsChange.None," m");
            Row("Estado de las alas","F5 permite verlas desde las cámaras externas; el modo foto también respeta Mostrar personaje.",out var state);
            var status=Label(wings.Status,"pr-muted",state);status.schedule.Execute(()=>status.text=wings.Status).Every(500);
            Note("Vuelo: G alternar · H velocidad · WASD dirección según la mirada · Espacio subir · Ctrl bajar. Sin pulsar dirección te quedas suspendido. Las paredes y los sectores sin cargar siguen bloqueando el paso. Al quitar las alas vuelve la gravedad.");
            Note(PoolroomsWings.Attribution+" Créditos y fuentes completos en WingSources/Licenses.");
        }
        private void DrawAudio()
        {
            Slider("Volumen general","Todos los sonidos de la exploración.",0,1,Prefs.masterVolume,v=>Prefs.masterVolume=v,SettingsChange.Audio,percent:true);
            Slider("Ambiente","Ventilación y circulación de agua.",0,1,Prefs.environmentVolume,v=>Prefs.environmentVolume=v,SettingsChange.Audio,percent:true);
            Slider("Pasos","Contactos al caminar por distintas superficies.",0,1,Prefs.footstepVolume,v=>Prefs.footstepVolume=v,SettingsChange.Audio,percent:true);
            Slider("Agua","Pasos mojados y contactos con las piscinas.",0,1,Prefs.waterVolume,v=>Prefs.waterVolume=v,SettingsChange.Audio,percent:true);
            Toggle("Goteos ocasionales","Sonidos discretos cerca del agua.",Prefs.drips,v=>Prefs.drips=v,SettingsChange.Audio);
            Toggle("Reverberación por sala","El sonido responde al tamaño del espacio.",Prefs.reverb,v=>Prefs.reverb=v,SettingsChange.Audio);
            Toggle("Ambiente 3D","Circulación de piscinas y ventilación situadas en el espacio.",Prefs.spatialAmbience,v=>Prefs.spatialAmbience=v,SettingsChange.Audio);
            Toggle("Oclusión del sonido","Las paredes atenúan el ambiente con transiciones suaves.",Prefs.soundOcclusion,v=>Prefs.soundOcclusion=v,SettingsChange.Audio);
            Toggle("Silenciar en segundo plano","Cuando cambias a otra aplicación.",Prefs.muteUnfocused,v=>Prefs.muteUnfocused=v,SettingsChange.Audio);
            Choice("Atmósfera","Tranquilo es el estilo inicial. Inquietante añade pasos lejanos y resonancias: sin voces, susurros ni sobresaltos.",new List<string>{"Tranquila","Inquietante"},(int)Prefs.soundscape.mode,v=>{Prefs.soundscape.mode=(SoundscapeMode)v;Changed(SettingsChange.Audio);});
            Toggle("Detalles sonoros de las salas","Agua, aire, jardín y pequeñas resonancias desde puntos reales del mundo.",Prefs.soundscape.roomDetails,v=>Prefs.soundscape.roomDetails=v,SettingsChange.Audio);
            Slider("Detalle ambiental","Capas suaves de aire, jardín y resonancia del espacio.",0,1,Prefs.soundscape.detail,v=>Prefs.soundscape.detail=v,SettingsChange.Audio,percent:true);
            Slider("Intensidad inquietante","Controla el fondo y los sucesos lejanos del modo inquietante.",0,1,Prefs.soundscape.tension,v=>Prefs.soundscape.tension=v,SettingsChange.Audio,percent:true);
            Slider("Rango dinámico","Bajo suaviza la diferencia entre contactos y ambiente.",0,1,Prefs.soundscape.dynamicRange,v=>Prefs.soundscape.dynamicRange=v,SettingsChange.Audio,percent:true);
            Toggle("Pasos lejanos","Solo en el modo inquietante, separados por pausas largas.",Prefs.soundscape.distantSteps,v=>Prefs.soundscape.distantSteps=v,SettingsChange.Audio);
            Toggle("Sonidos de movimiento","Dash, ropa al saltar, aterrizajes y aire al volar.",Prefs.soundscape.movementSounds,v=>Prefs.soundscape.movementSounds=v,SettingsChange.Audio);
            Toggle("Objetos con sonido","Contactos suaves al tomar y soltar objetos; intensidad según su caída.",Prefs.soundscape.objectSounds,v=>Prefs.soundscape.objectSounds=v,SettingsChange.Audio);
            Toggle("Dar espacio a la música","Atenúa suavemente el ambiente mientras suenan tus canciones.",Prefs.soundscape.duckWithMusic,v=>Prefs.soundscape.duckWithMusic=v,SettingsChange.Audio);
            DrawLocalMusic();
        }
        private void DrawLocalMusic()
        {
            var player=GetComponent<PoolroomsMusicPlayer>();if(player==null)return;
            Note("Tu música · MP3, OGG, WAV y AIFF. Coloca las canciones en Music junto al ejecutable y pulsa Actualizar. Puedes usar subcarpetas; los archivos permanecen en tu PC.");
            Row("Carpeta de canciones","",out var folderControl);
            var folder=Label(player.Folder,"pr-muted",folderControl);folder.style.whiteSpace=WhiteSpace.Normal;
            folder.schedule.Execute(()=>folder.text=player.Folder).Every(1000);
            Row("Biblioteca local","La música continúa en el menú; pausa y detén desde estos controles.",out var library);
            ActionButton("Abrir carpeta",player.OpenFolder,"pr-secondary",library);
            var refresh=ActionButton("Actualizar lista",player.Refresh,"pr-secondary",library);
            Row("Canción","Selecciona una pista para reproducirla.",out var selection);
            var names=new List<string>{"Sin canciones"};var tracks=new DropdownField(names,0);selection.Add(tracks);tracks.AddToClassList("pr-dropdown");
            tracks.RegisterValueChangedCallback(e=>{int index=names.IndexOf(e.newValue);if(index>=0&&index<player.Tracks.Count)player.Play(index);});
            int libraryRevision=-1;
            tracks.schedule.Execute(()=>
            {
                refresh.SetEnabled(!player.Scanning);
                if(libraryRevision!=player.LibraryRevision)
                {
                    libraryRevision=player.LibraryRevision;int count=player.Tracks.Count;names.Clear();
                    for(int i=0;i<count;i++)names.Add((i+1)+" · "+System.IO.Path.GetFileName(player.Tracks[i]));
                    if(names.Count==0)names.Add("Sin canciones");tracks.choices=names;tracks.SetEnabled(count>0);
                }
                int selected=player.Selected;if(selected>=0&&selected<names.Count)tracks.SetValueWithoutNotify(names[selected]);
            }).Every(300);
            Row("Reproducción","",out var playback);
            ActionButton("Anterior",player.Previous,"pr-secondary",playback);
            var play=ActionButton("Reproducir / Pausar",player.PlayPause,"pr-primary",playback);
            ActionButton("Siguiente",player.Next,"pr-secondary",playback);ActionButton("Detener",player.Stop,"pr-secondary",playback);
            play.schedule.Execute(()=>play.text=player.Paused?"Reanudar":player.Playing?"Pausar":"Reproducir").Every(300);
            Row("Ahora suena","",out var now);var status=Label("","pr-muted",now);status.style.whiteSpace=WhiteSpace.Normal;
            status.schedule.Execute(()=>status.text=player.Title+" · "+MusicTime(player.Position)+" / "+MusicTime(player.Duration)+"\n"+player.Status).Every(300);
            Row("Posición","Busca un momento de la canción.",out var position);
            var seek=new Slider(0,1);position.Add(seek);seek.AddToClassList("pr-slider");bool seeking=false;
            seek.RegisterCallback<PointerDownEvent>(e=>seeking=true,TrickleDown.TrickleDown);
            seek.RegisterCallback<PointerUpEvent>(e=>seeking=false,TrickleDown.TrickleDown);
            seek.RegisterCallback<PointerCaptureOutEvent>(e=>seeking=false);
            seek.RegisterValueChangedCallback(e=>player.Seek(e.newValue));
            seek.schedule.Execute(()=>{seek.SetEnabled(player.Duration>0&&!player.Loading);if(!seeking)seek.SetValueWithoutNotify(player.Duration>0?player.Position/player.Duration:0);}).Every(500);
            Slider("Volumen de música","Se combina con Volumen general.",0,1,Prefs.music.volume,v=>Prefs.music.volume=v,SettingsChange.Audio,percent:true);
            Slider("Fundido entre canciones","Transición suave; se acorta para pistas muy breves.",0,5,Prefs.music.crossfade,v=>Prefs.music.crossfade=v,SettingsChange.Audio," s");
            Toggle("Reproducción automática","Al abrir el juego, empieza por la última pista seleccionada.",Prefs.music.autoPlay,v=>Prefs.music.autoPlay=v,SettingsChange.Audio);
            Toggle("Orden aleatorio","Recorre la biblioteca sin repetir canciones durante una vuelta.",Prefs.music.shuffle,v=>{Prefs.music.shuffle=v;player.ResetOrder();},SettingsChange.Audio);
            Choice("Repetir","Al terminar una pista o la lista.",new List<string>{"Desactivado","Toda la lista","Una canción"},(int)Prefs.music.repeat,v=>{Prefs.music.repeat=(MusicRepeat)v;Changed(SettingsChange.Audio);});
        }
        private static string MusicTime(float time)
        {int seconds=Mathf.Max(0,Mathf.FloorToInt(time));return (seconds/60)+":"+(seconds%60).ToString("00");}
        private void DrawControls()
        {
            Note("Inventario: Tab abre/cierra cinco páginas de 50 espacios. 5 muestra/oculta la barra rápida; 1–4 y 6–9 seleccionan su acceso y X lo usa. Z guarda el objeto sostenido; T toma o mueve muebles y E usa su función. Las cantidades por pila no tienen límite fijo. Esta sesión todavía no se guarda.");
            Note("Alas: G activa o detiene el vuelo normal · H cambia entre lento, medio, rápido y muy rápido · Espacio sube · Ctrl baja. Diseños y ajustes en Skins → Alas.");
            Note("Cámara: F5 recorre 8 perspectivas. F6 abre/cierra el modo foto; F8 oculta su panel y F9 guarda un PNG sin interfaz. En foto: WASD mover, E/Q subir/bajar, botón derecho mirar, Shift acelerar, Z/C inclinar y rueda ajustar encuadre.");
            Note("Objetos: E toma o interactúa; Q suelta. Mantén R y mueve el ratón para examinar el objeto sostenido sin girar la cámara.");
            Slider("Sensibilidad del ratón","Velocidad de la mirada.",.01f,.3f,Prefs.sensitivity,v=>Prefs.sensitivity=v,SettingsChange.Controls);
            Toggle("Invertir mirada vertical","Invierte el movimiento arriba y abajo.",Prefs.invertY,v=>Prefs.invertY=v,SettingsChange.Controls);
            Toggle("Correr con una pulsación","La tecla | alterna caminar y correr, sin mantenerla.",Prefs.toggleSprint,v=>Prefs.toggleSprint=v,SettingsChange.Controls);
            Slider("Velocidad al caminar","Paseo lento o avance más ágil.",1.5f,5,Prefs.walkSpeed,v=>{Prefs.walkSpeed=v;Prefs.sprintSpeed=Mathf.Max(Prefs.sprintSpeed,v);},SettingsChange.Controls," m/s");
            sprintSpeedSlider=Slider("Velocidad al correr","Se mantiene igual o mayor que caminar.",1.5f,8,Prefs.sprintSpeed,v=>Prefs.sprintSpeed=v,SettingsChange.Controls," m/s",readValue:()=>Prefs.sprintSpeed);
            sprintSpeedLabel=sprintSpeedSlider.parent.Q<Label>();
            Toggle("Deslizamiento rápido","Mantén Ctrl izquierdo. Puedes saltar conservando impulso; no consume energía.",Prefs.allowSlide,v=>Prefs.allowSlide=v,SettingsChange.Controls);
            Slider("Velocidad de deslizamiento","Más rápido que correr. Las paredes y los sectores sin cargar siguen bloqueando el paso.",ExplorerSlide.MinSpeed,ExplorerSlide.MaxSpeed,Prefs.slideSpeed,v=>Prefs.slideSpeed=v,SettingsChange.Controls," m/s");
            Note("WASD  ·  Caminar     Ratón  ·  Mirar     | / tecla junto a Z  ·  Correr\nShift  ·  Dash     Ctrl izquierdo  ·  Deslizarse\nEspacio  ·  Saltar / Doble salto     Esc  ·  Pausa     Y  ·  Chat\nF  ·  Linterna     V  ·  Intensidad     E  ·  Agarrar / Entrar\nQ  ·  Soltar objeto     M  ·  Administrador, si está desbloqueado");
            Note("Parque acuático: E junto a la boca del tobogán, desde la plataforma y con las manos libres. E junto a la válvula activa o detiene la corriente suave. La pausa detiene el recorrido.");
            Note("Coche: E entrar / salir · WASD conducir · Ctrl frenar. Detente y deja libre un lateral para salir.");
            Note("Avión: E pilotar · W/S ajustar potencia · A/D girar · Espacio subir · Ctrl bajar y frenar. Aterriza y detente para salir con E.");
            Note("Helicóptero: E pilotar · W/S avanzar / retroceder · A/D girar · Espacio subir · Ctrl bajar y frenar. Sin pulsar altura mantiene vuelo estacionario cuando el rotor alcanza régimen.");
            var torch=explorer.Flashlight;
            if(torch==null)return;
            Toggle("Linterna encendida","También puedes usar F durante la exploración.",torch.IsOn,v=>torch.SetOn(v),SettingsChange.None);
            var levels=new List<string>();for(int i=0;i<6;i++)levels.Add(FlashlightModel.Look((FlashlightLevel)i).Name);
            Choice("Intensidad de linterna","V recorre las intensidades; Rayo solar es exageradamente brillante.",levels,(int)torch.Level,i=>torch.SetLevel((FlashlightLevel)i));
            Toggle("Sombras de linterna","Añade sombras a la luz portátil.",torch.CastShadows,v=>torch.SetShadows(v),SettingsChange.None);
        }
        private void DrawComfort()
        {
            Slider("Escala del menú","Aumenta el tamaño de texto y controles.",.85f,2,Prefs.uiScale,v=>Prefs.uiScale=v,SettingsChange.Comfort,percent:true);
            Toggle("Más contraste en el menú","Texto y límites más visibles.",Prefs.highContrast,v=>Prefs.highContrast=v,SettingsChange.Comfort);
            Toggle("Fondo opaco","Separa los ajustes del escenario.",Prefs.opaqueMenu,v=>Prefs.opaqueMenu=v,SettingsChange.Comfort);
            Toggle("Reducir movimiento","Desactiva balanceo, inercia de linterna, reacción al agua y temblor VHS.",Prefs.reduceMotion,v=>{Prefs.reduceMotion=v;},SettingsChange.Comfort|SettingsChange.Camera);
            Toggle("Destellos reducidos","Suaviza el brillo sin borrar las luces del mundo.",Prefs.reducedFlashes,v=>Prefs.reducedFlashes=v,SettingsChange.Comfort|SettingsChange.Camera);
            Toggle("Pausa al cambiar de aplicación","El recorrido espera cuando pierdes el foco.",Prefs.pauseOnFocusLoss,v=>Prefs.pauseOnFocusLoss=v,SettingsChange.Comfort);
        }
        private void RefreshAppearance()
        {
            if(overlay==null||gameSettings==null)return;overlay.EnableInClassList("pr-high-contrast",Prefs.highContrast);
            overlay.EnableInClassList("pr-opaque",Prefs.opaqueMenu);overlay.EnableInClassList("pr-reduce-motion",Prefs.reduceMotion);
            if(cameraMotionToggle!=null)cameraMotionToggle.SetEnabled(!Prefs.reduceMotion);
            if(locomotionToggle!=null)locomotionToggle.SetEnabled(!Prefs.reduceMotion);
            if(indirectToggle!=null)indirectToggle.SetEnabled(Prefs.graphicsQuality!=0);
            if(fogToggle!=null)fogToggle.SetEnabled(Prefs.graphicsQuality!=0);
            if(sprintSpeedSlider!=null){sprintSpeedSlider.lowValue=Prefs.walkSpeed;sprintSpeedSlider.SetValueWithoutNotify(Prefs.sprintSpeed);if(sprintSpeedLabel!=null)sprintSpeedLabel.text=Format(Prefs.sprintSpeed," m/s",false);}
            panelSettings.scale=Prefs.uiScale;Responsive();
        }
        private void Responsive()
        {
            if(shell==null||overlay.resolvedStyle.width<=0)return;
            float width=overlay.resolvedStyle.width,height=overlay.resolvedStyle.height;
            shell.style.width=Mathf.Max(280,Mathf.Min(1100,width-32));shell.style.height=Mathf.Max(280,Mathf.Min(730,height-32));
            shell.EnableInClassList("pr-compact",width<880);shell.EnableInClassList("pr-short",height<560);
        }
        private void ShowAssetCredits()
        {
            var dialog=CreateModal("Créditos y licencias","Gracias a los autores que permiten compartir estos recursos.");
            var scroll=new ScrollView(ScrollViewMode.Vertical);scroll.style.height=Mathf.Min(350,Screen.height*.42f);dialog.Add(scroll);
            var credits=Resources.Load<TextAsset>("PoolroomsAssetCredits");
            var text=Label(credits!=null?credits.text:"Las atribuciones acompañan al paquete de recursos.","pr-footnote",scroll);
            text.style.whiteSpace=WhiteSpace.Normal;
            ActionButton("Volver",CloseModal,"pr-secondary",dialog);
        }
        private void ConfirmReset()
        {
            if(gameSettings.Display.Pending)return;
            var dialog=CreateModal("Restablecer "+categories[(int)category],"Solo se restablecen las opciones de esta categoría. La resolución actual se conserva.");
            ActionButton("Restablecer",()=>{gameSettings.Reset(category);CloseModal();BuildSettings();},"pr-primary",dialog);
            ActionButton("Cancelar",CloseModal,"pr-secondary",dialog);
        }
        private VisualElement CreateModal(string heading,string message)
        {
            CloseModal();modal=Element("pr-modal",overlay);var dialog=Element("pr-dialog",modal);Label(heading,"pr-dialog-title",dialog);Label(message,"pr-muted",dialog);return dialog;
        }
        private void DisplayChanged()
        {
            if(!paused)return;
            if(gameSettings.Display.Pending)
            {
                var dialog=CreateModal("¿Conservar esta pantalla?","Comprueba que puedes leer y usar el menú.");previewLabel=Label("Regreso automático en 15 s","pr-countdown",dialog);lastPreviewSecond=-1;
                ActionButton("Conservar",()=>gameSettings.KeepDisplay(),"pr-primary",dialog);ActionButton("Volver a la anterior",()=>gameSettings.RevertDisplay(),"pr-secondary",dialog);
                dialog.Q<Button>()?.Focus();
            }
            else {CloseModal();if(settings)BuildSettings();}
        }
        private void CloseModal(){if(modal!=null)modal.RemoveFromHierarchy();modal=null;previewLabel=null;}
        private void DestroyUI()
        {
            if(menuObject!=null){menuObject.SetActive(false);if(Application.isPlaying)Destroy(menuObject);else DestroyImmediate(menuObject);}
            if(panelSettings!=null){if(Application.isPlaying)Destroy(panelSettings);else DestroyImmediate(panelSettings);}
            menuObject=null;panelSettings=null;overlay=null;shell=null;home=null;settingsBody=null;content=null;modal=null;previewLabel=null;
            groups.Clear();rows.Clear();tabs.Clear();
        }
    }
}
