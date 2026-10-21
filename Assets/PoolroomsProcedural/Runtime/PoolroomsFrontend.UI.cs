using System;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Poolrooms
{
    public sealed partial class PoolroomsFrontend
    {
        private PanelSettings frontendPanel;
        private VisualElement frontendRoot,pageBody,dialogLayer,card;
        private Image frontImage,backImage;
        private Texture2D currentArt,leavingArt;
        private float fadeElapsed;
        private Label pageTitle,pageCaption,footer,loadingText;
        private AudioSource interfaceAudio;
        private AudioClip clickClip;
        private FrontendPage displayedPage;
        private int optionsTab;
        private readonly string[] pageNames={"Inicio","Nuevo recorrido","Destinos","Ajustes","Tu experiencia","Créditos"};

        private void CreateFrontendUI()
        {
            frontendPanel=ScriptableObject.CreateInstance<PanelSettings>();
            frontendPanel.name="Espacios · independent frontend";
            frontendPanel.scaleMode=PanelScaleMode.ConstantPixelSize;
            frontendPanel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("PoolroomsTheme");
            var node=new GameObject("Frontend interface");node.transform.SetParent(transform,false);
            var document=node.AddComponent<UIDocument>();document.panelSettings=frontendPanel;document.sortingOrder=1000;
            frontendRoot=document.rootVisualElement;frontendRoot.AddToClassList("fe-root");
            var style=Resources.Load<StyleSheet>("PoolroomsFrontend");
            if(style==null)throw new InvalidOperationException("Falta Runtime/Resources/PoolroomsFrontend.uss.");
            frontendRoot.styleSheets.Add(style);
            backImage=new Image{scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};backImage.AddToClassList("fe-background");frontendRoot.Add(backImage);
            frontImage=new Image{scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};frontImage.AddToClassList("fe-background");frontendRoot.Add(frontImage);
            Node("fe-shade",frontendRoot).pickingMode=PickingMode.Ignore;
            card=Node("fe-card",frontendRoot);
            Text("EXPLORACIÓN  /  UN MUNDO PARA QUEDARSE", "fe-kicker",card);
            pageTitle=Text("ESPACIOS\nSIN FIN","fe-title",card);
            pageCaption=Text("Hay otro lugar al otro lado.","fe-caption",card);
            pageBody=Node("fe-body",card);
            footer=Text("Elige un recorrido. Lo demás puede esperar.","fe-footer",frontendRoot);
            frontendRoot.RegisterCallback<KeyDownEvent>(OnFrontendKeyDown);
            frontendRoot.RegisterCallback<GeometryChangedEvent>(_=>ResizeFrontend());
            interfaceAudio=menuCamera.gameObject.AddComponent<AudioSource>();
            interfaceAudio.playOnAwake=false;interfaceAudio.ignoreListenerPause=true;interfaceAudio.spatialBlend=0;
            clickClip=Resources.Load<AudioClip>("EncounterAudio/Note0");
            ResizeFrontend();
        }

        private static VisualElement Node(string css,VisualElement parent)
        {var node=new VisualElement();node.AddToClassList(css);parent.Add(node);return node;}

        private static Label Text(string value,string css,VisualElement parent)
        {var label=new Label(value){enableRichText=false};label.AddToClassList(css);parent.Add(label);return label;}

        private Button MenuButton(string label,Action action,VisualElement parent,string description="",bool enabled=true)
        {
            var button=new Button(()=>{PlayMenuSound();action();}){text=label,tooltip=description};
            button.AddToClassList("fe-button");button.SetEnabled(enabled);parent.Add(button);
            button.RegisterCallback<MouseEnterEvent>(_=>{if(footer!=null&&!string.IsNullOrEmpty(description))footer.text=description;});
            button.RegisterCallback<FocusInEvent>(_=>{if(footer!=null&&!string.IsNullOrEmpty(description))footer.text=description;});
            return button;
        }

        private void PlayMenuSound()
        {
            if(interfaceAudio!=null&&clickClip!=null)
                interfaceAudio.PlayOneShot(clickClip,preferences.masterVolume*.08f);
        }

        private void DrawPage(FrontendPage page)
        {
            if(!Flow.Navigate(page))return;
            displayedPage=page;CloseFrontendDialog();pageBody.Clear();loadingText=null;
            SetBackground(page);
            card.EnableInClassList("fe-wide",page!=FrontendPage.Home);
            pageTitle.text=page==FrontendPage.Home?"ESPACIOS\nSIN FIN":pageNames[(int)page];
            pageTitle.EnableInClassList("fe-title-small",page!=FrontendPage.Home);
            pageCaption.text=Caption(page);
            switch(page)
            {
                case FrontendPage.Home:DrawHome();break;
                case FrontendPage.Journey:DrawJourney();break;
                case FrontendPage.Atlas:DrawAtlas();break;
                case FrontendPage.Options:DrawFrontendOptions();break;
                case FrontendPage.Appearance:DrawExperience();break;
                case FrontendPage.Credits:DrawFrontendCredits();break;
            }
            if(page!=FrontendPage.Home)MenuButton("← Volver al inicio",()=>DrawPage(FrontendPage.Home),pageBody);
            pageBody.schedule.Execute(()=>pageBody.Query<Button>().ToList().Find(button=>button.enabledInHierarchy)?.Focus());
            ResizeFrontend();
        }

        private static string Caption(FrontendPage page)
        {
            switch(page)
            {
                case FrontendPage.Journey:return "Elige cómo empieza. El mundo continuará conectado.";
                case FrontendPage.Atlas:return "Cada destino es el comienzo de un recorrido más grande.";
                case FrontendPage.Options:return "Configura tu experiencia antes de entrar.";
                case FrontendPage.Appearance:return "Tu manera de mirar, escuchar y recorrer.";
                case FrontendPage.Credits:return "Imágenes, materiales, objetos y personas detrás del proyecto.";
                default:return "Hay otro lugar al otro lado.";
            }
        }

        private void DrawHome()
        {
            var home=Scroll(pageBody);
            MenuButton("CONTINUAR",ContinueJourney,home,"Retoma la sesión abierta en tu posición actual.",Flow.CanResume);
            MenuButton("NUEVO RECORRIDO",()=>DrawPage(FrontendPage.Journey),home,"Selecciona semilla, tipo de recorrido y punto de partida.");
            MenuButton("ELEGIR DESTINO",()=>DrawPage(FrontendPage.Atlas),home,"Explora los 58 biomas y los dos grandes destinos conectados.");
            MenuButton("AJUSTES",()=>DrawPage(FrontendPage.Options),home,"Pantalla, gráficos, sonido y controles.");
            MenuButton("TU EXPERIENCIA",()=>DrawPage(FrontendPage.Appearance),home,"Calma, atmósfera, cámara y comodidad.");
            MenuButton("CRÉDITOS",()=>DrawPage(FrontendPage.Credits),home,"Consulta las fuentes y licencias incluidas.");
            MenuButton("SALIR",ConfirmQuit,home,"Cerrar el juego.");
            if(!string.IsNullOrEmpty(Flow.Error))Text(Flow.Error,"fe-error",home);
            Text(Flow.CanResume?"Continuar retoma esta sesión. La partida todavía no se guarda al cerrar.":"Elige dónde comienza tu recorrido.","fe-note",home);
        }

        private void DrawJourney()
        {
            var scroll=Scroll(pageBody);
            Text("PUNTO DE PARTIDA","fe-kicker",scroll);
            Text(destinationName,"fe-selection",scroll);
            MenuButton("Cambiar destino",()=>DrawPage(FrontendPage.Atlas),scroll);
            var seed=new TextField("Semilla · número o palabra"){value=chosenSeed.ToString(),maxLength=64};seed.AddToClassList("fe-field");scroll.Add(seed);
            seed.RegisterValueChangedCallback(e=>{if(FrontendFlow.TrySeed(e.newValue,chosenSeed,out int value))chosenSeed=value;});
            MenuButton("Otra semilla",()=>{chosenSeed=unchecked((int)DateTime.UtcNow.Ticks);seed.SetValueWithoutNotify(chosenSeed.ToString());},scroll);
            var mode=new DropdownField("Recorrido",new System.Collections.Generic.List<string>{"Mundo conectado","Recorrido personalizado"},connectedJourney?0:1);mode.AddToClassList("fe-field");scroll.Add(mode);
            mode.RegisterValueChangedCallback(_=>connectedJourney=mode.index==0);
            Text("Mundo conectado incluye las transiciones caminables entre regiones. La semilla identifica su distribución; el punto de partida no elimina los demás niveles.","fe-note",scroll);
            MenuButton("COMENZAR EXPLORACIÓN",BeginSelectedJourney,scroll,"Solo ahora se crea la partida y se preparan sus primeras salas.");
        }

        private void SelectStart(JourneyStart start,LiminalEnvironment environment,string name)
        {chosenStart=start;chosenEnvironment=environment;destinationName=name;DrawPage(FrontendPage.Journey);}

        private void DrawAtlas()
        {
            var search=new TextField("Buscar un lugar"){maxLength=80};search.AddToClassList("fe-field");pageBody.Add(search);
            var list=Scroll(pageBody);
            Action<string> rebuild=query=>
            {
                list.Clear();
                if(SettingsSearch.Matches("Inicio mundo conectado",query))MenuButton("Un mundo conectado · inicio",()=>SelectStart(JourneyStart.Origin,LiminalEnvironment.Poolrooms,"Un mundo conectado"),list);
                if(SettingsSearch.Matches("Poolrooms solares",query))MenuButton("Poolrooms solares",()=>SelectStart(JourneyStart.SunlitPoolrooms,LiminalEnvironment.Poolrooms,"Poolrooms solares"),list);
                if(SettingsSearch.Matches("Centro comercial completo",query))MenuButton("Centro comercial completo",()=>SelectStart(JourneyStart.Mall,LiminalEnvironment.Supermarket,"Centro comercial completo"),list);
                if(SettingsSearch.Matches("Nivel 94 colinas casas",query))MenuButton("Nivel 94 · colinas y casas",()=>SelectStart(JourneyStart.Meadows,LiminalEnvironment.RainHouses,"Nivel 94"),list);
                foreach(LiminalEnvironment value in Enum.GetValues(typeof(LiminalEnvironment)))
                {
                    var environment=value;string name=InteriorPlanner.Name(environment);
                    if(SettingsSearch.Matches(name,query))MenuButton(name,()=>SelectStart(JourneyStart.Environment,environment,name),list);
                }
                if(list.childCount==0)Text("No hay destinos con ese nombre.","fe-note",list);
            };
            search.RegisterValueChangedCallback(e=>rebuild(e.newValue));rebuild("");
        }

        private ScrollView Scroll(VisualElement parent)
        {
            var scroll=new ScrollView(ScrollViewMode.Vertical);scroll.AddToClassList("fe-scroll");parent.Add(scroll);return scroll;
        }

        private void DrawFrontendCredits()
        {
            var scroll=Scroll(pageBody);
            Text("UN MUNDO PARA QUEDARSE","fe-kicker",scroll);
            Text("Arquitectura procedural, encuentros, talleres, exploración e ilustraciones originales creadas para este proyecto.","fe-note",scroll);
            Text("Los seis fondos son ilustraciones de menú generadas con image_gen. No representan capturas de Unity ni garantizan que una escena se vea exactamente igual.","fe-note",scroll);
            var credits=Resources.Load<TextAsset>("PoolroomsAssetCredits");
            Text(credits!=null?credits.text:"Las licencias y la procedencia acompañan a cada banco de recursos dentro de PoolroomsProcedural/Resources.","fe-credits",scroll);
        }

        private void ShowLoading(string message)
        {
            CloseFrontendDialog();pageBody.Clear();card.AddToClassList("fe-wide");
            pageTitle.text="Preparando el recorrido";pageTitle.AddToClassList("fe-title-small");
            pageCaption.text=destinationName;SetBackground(FrontendPage.Journey);
            loadingText=Text(message,"fe-selection",pageBody);
            Text("Se preparan suelo, objetos y conexiones antes de darte el control.","fe-note",pageBody);
            MenuButton("Cancelar y volver",CancelLoading,pageBody);
        }

        private void ShowMessage(string title,string message)
        {
            var box=CreateFrontendDialog(title,message);MenuButton("Entendido",CloseFrontendDialog,box);
            box.schedule.Execute(()=>box.Q<Button>()?.Focus());
        }

        private VisualElement CreateFrontendDialog(string title,string message)
        {
            CloseFrontendDialog();dialogLayer=Node("fe-modal",frontendRoot);
            dialogLayer.focusable=true;dialogLayer.Focus();
            var box=Node("fe-dialog",dialogLayer);Text(title,"fe-dialog-title",box);Text(message,"fe-note",box);return box;
        }

        private void ShowConfirmation(string title,string message,Action accepted)
        {
            var box=CreateFrontendDialog(title,message);
            MenuButton("Confirmar",()=>{CloseFrontendDialog();accepted();},box);
            MenuButton("Cancelar",CloseFrontendDialog,box);
            box.schedule.Execute(()=>box.Q<Button>()?.Focus());
        }

        private void CloseFrontendDialog(){dialogLayer?.RemoveFromHierarchy();dialogLayer=null;}

        private void ConfirmQuit()
        {
            ShowConfirmation("Salir del juego","Se conservarán los ajustes. La sesión del mundo y su inventario no se guardan todavía.",()=>
            {
                SaveFrontendPreferences();quitting=true;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            });
        }

        private void OnFrontendKeyDown(KeyDownEvent e)
        {
            if(dialogLayer!=null&&e.keyCode==KeyCode.Tab)
            {
                var buttons=dialogLayer.Query<Button>().ToList();
                if(buttons.Count>0)
                {
                    int focused=buttons.FindIndex(button=>button==frontendRoot.panel?.focusController.focusedElement);
                    int next=(focused+(e.shiftKey?-1:1)+buttons.Count)%buttons.Count;
                    buttons[next].Focus();
                }
                e.StopPropagation();frontendRoot.panel?.focusController.IgnoreEvent(e);return;
            }
            if(e.keyCode!=KeyCode.Escape)return;
            if(displayPreview){RevertFrontendDisplay();e.StopPropagation();return;}
            if(dialogLayer!=null)CloseFrontendDialog();
            else if(Flow.Phase==FrontendPhase.Loading)CancelLoading();
            else if(displayedPage!=FrontendPage.Home)DrawPage(FrontendPage.Home);
            else ConfirmQuit();
            e.StopPropagation();
        }

        private void SetBackground(FrontendPage page)
        {
            string name=page.ToString();if(currentArt!=null&&currentArt.name==name)return;
            ReleaseLeavingArt();leavingArt=currentArt;backImage.image=leavingArt;
            currentArt=Resources.Load<Texture2D>("FrontendArt/"+name);frontImage.image=currentArt;
            fadeElapsed=0;frontImage.style.opacity=leavingArt==null?1:0;
        }

        private void ReleaseLeavingArt()
        {
            if(backImage!=null)backImage.image=null;
            if(leavingArt!=null&&leavingArt!=currentArt)Resources.UnloadAsset(leavingArt);
            leavingArt=null;
        }

        private void Update()
        {
            if(frontendRoot==null||!Flow.Visible)return;
            fadeElapsed+=Time.unscaledDeltaTime;
            float alpha=FrontendFlow.Fade(fadeElapsed,preferences.reduceMotion);
            frontImage.style.opacity=alpha;
            if(alpha>=1&&leavingArt!=null)ReleaseLeavingArt();
            TickFrontendDisplay();
        }

        private void ResizeFrontend()
        {
            if(frontendPanel==null)return;
            frontendPanel.scale=Mathf.Clamp(Screen.height/900f,.65f,2)*preferences.uiScale;
            bool compact=Screen.width/Mathf.Max(.5f,frontendPanel.scale)<1000;
            frontendRoot.EnableInClassList("fe-compact",compact);
            frontendRoot.EnableInClassList("fe-high-contrast",preferences.highContrast);
        }

        private void DestroyFrontendUI()
        {
            RevertFrontendDisplay();ReleaseLeavingArt();
            if(frontImage!=null)frontImage.image=null;
            if(currentArt!=null)Resources.UnloadAsset(currentArt);currentArt=null;
            if(frontendPanel!=null)Destroy(frontendPanel);
        }
    }
}
