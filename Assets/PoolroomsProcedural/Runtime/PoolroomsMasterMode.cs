using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsMasterMode : MonoBehaviour
    {
        private readonly MasterSession session=new MasterSession();
        private PoolroomsExplorer explorer;
        private CharacterController body;
        private bool previousBodyEnabled=true,controllerOwned,wasFlying,wasNoclip;
        private enum Page { Closed, Chat, Master }
        private Page page;
        private readonly Queue<string> history=new Queue<string>();
        private GameObject menuObject;
        private PanelSettings panelSettings;
        private VisualElement overlay,panel;
        private ScrollView messages;
        private TextField input;
        private CursorLockMode previousLock;
        private bool previousVisible;
        private int consumedEscapeFrame=-1,openedFrame=-1,closedFrame=-1;
        private string status="";
        public bool IsActive { get { return isActiveAndEnabled&&session.Unlocked; } }
        public bool Flying { get { return isActiveAndEnabled&&session.Flying; } }
        public bool Noclip { get { return isActiveAndEnabled&&session.Noclip; } }
        public bool ClosedThisFrame { get { return closedFrame==Time.frameCount; } }
        public bool IsInterfaceOpen { get { return page!=Page.Closed; } }
        public float FlightSpeed { get { return session.Options.speed; } }
        private PoolroomsMasterView masterView;
        public MasterViewOptions ViewOptions { get { var settings=GetComponent<PoolroomsGameSettings>();return settings!=null&&settings.Preferences!=null?settings.Preferences.rendering:session.Options.view; } }
        private void Awake()
        {
            explorer=GetComponent<PoolroomsExplorer>();body=GetComponent<CharacterController>();
            masterView=GetComponent<PoolroomsMasterView>();if(masterView==null)masterView=gameObject.AddComponent<PoolroomsMasterView>();
        }
        public bool HandleEscape()
        {
            if(consumedEscapeFrame==Time.frameCount)return true;
            if(!IsInterfaceOpen)return false;consumedEscapeFrame=Time.frameCount;CloseInterface();return true;
        }
        public void OpenChat(){if(!CanOpen())return;Open(Page.Chat);}
        public void ToggleMenu(){if(page==Page.Chat)return;if(!IsActive||!CanOpen())return;if(page==Page.Master)CloseInterface();else Open(Page.Master);}
        private bool CanOpen(){return isActiveAndEnabled&&explorer!=null&&explorer.isActiveAndEnabled&&!explorer.IsPaused;}
        public void CloseInterface(bool restoreCursor=true)
        {
            if(!IsInterfaceOpen)return;page=Page.Closed;closedFrame=Time.frameCount;
            if(overlay!=null)overlay.style.display=DisplayStyle.None;
            explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);
            if(restoreCursor){Cursor.lockState=previousLock;Cursor.visible=previousVisible;}
        }
        private void Open(Page target)
        {
            if(!CanOpen()||target==Page.Master&&!IsActive)return;
            bool first=!IsInterfaceOpen;
            if(first){previousLock=Cursor.lockState;previousVisible=Cursor.visible;}
            page=target;openedFrame=Time.frameCount;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);
            try{EnsureUI();Draw();overlay.style.display=DisplayStyle.Flex;}
            catch(Exception error){CloseInterface();DestroyUI();Debug.LogException(error,this);}
        }
        public MasterCommandResult Submit(string text)
        {
            MasterCommandResult result=session.Submit(text);
            if(!string.IsNullOrWhiteSpace(text))AddHistory((result.IsCommand?"> ":"Tú: ")+Safe(text));
            if(result.IsCommand)AddHistory(result.Message);
            status=result.Message;
            if(result.Success&&(result.Action==MasterCommandAction.Unlock||result.Action==MasterCommandAction.Changed||result.Action==MasterCommandAction.Lock))
                if(!Synchronize())result=new MasterCommandResult(true,false,MasterCommandAction.None,status);
            if(result.Success&&result.Action==MasterCommandAction.Lock&&page==Page.Master)CloseInterface();
            if(result.Success&&(result.Action==MasterCommandAction.Unlock||result.Action==MasterCommandAction.Menu))Open(Page.Master);
            else if(page==Page.Chat)DrawMessages();
            return result;
        }
        private static string Safe(string text){if(text==null)return "";text=text.Replace('\n',' ').Replace('\r',' ');return text.Length>256?text.Substring(0,256):text;}
        private void AddHistory(string text){history.Enqueue(Safe(text));while(history.Count>64)history.Dequeue();}
        public void SetFlight(bool value){session.SetFlight(value);Synchronize();RefreshMenu();}
        public void SetNoclip(bool value){session.SetNoclip(value);Synchronize();RefreshMenu();}
        private void RefreshMenu(){if(page==Page.Master)Draw();}
        public void DisableMasterMode()
        {
            if(!IsActive)return;session.Lock();if(!Synchronize()){RefreshMenu();return;}CloseInterface();status="Modo administrador desactivado.";
        }
        private bool Synchronize()
        {
            if(body==null)body=GetComponent<CharacterController>();
            bool fly=session.Flying,noclip=session.Noclip,success=true;
            if(!noclip&&controllerOwned&&previousBodyEnabled)
            {
                Vector3 localPosition;
                if(PoolroomsCollisionRecovery.TryResolve(body,out localPosition))
                {
                    if((localPosition-transform.position).sqrMagnitude>.000001f)explorer.Teleport(localPosition);
                }
                else
                {
                    // Keep control in a thick wall; never silently send the player to the origin.
                    session.Submit("/unlock mastermode");session.SetNoclip(true);fly=noclip=true;success=false;
                    status="No hay espacio libre inmediato. Muévete fuera de la pared y vuelve a activar colisiones.";
                }
            }
            if(noclip&&!controllerOwned){previousBodyEnabled=body.enabled;controllerOwned=true;}
            if(noclip)body.enabled=false;
            else if(controllerOwned){body.enabled=previousBodyEnabled;controllerOwned=false;}
            if(wasFlying!=fly||wasNoclip!=noclip)explorer.ResetMotionForMasterMode();
            wasFlying=fly;wasNoclip=noclip;
            if(masterView!=null)masterView.ApplySettings();
            return success;
        }
        public void SetRoomLoadRadius(int radius){ViewOptions.roomRadius=radius;ApplyView();}
        public void SetTerrainViewRadius(int radius){ViewOptions.terrainRadius=radius;ApplyView();}
        public void SetMallVisibility(int sectors,int floors){ViewOptions.mallRadius=sectors;ViewOptions.mallFloorRadius=floors;ApplyView();}
        public void SetInspectionLight(bool value){ViewOptions.inspectionLight=value;ApplyView();}
        public void SetInspectionStrength(float value){ViewOptions.lightStrength=value;ApplyView();}
        private void ApplyView(){ViewOptions.Sanitize();GetComponent<PoolroomsGameSettings>()?.Change(SettingsChange.Graphics);if(masterView!=null)masterView.ApplySettings();}
        public bool VisitMall(){return Travel(false);}
        public bool VisitLevel94(){return Travel(true);}
        private bool Travel(bool exterior)
        {
            if(!IsActive)return false;
            bool found=explorer.world!=null&&explorer.world.VisitAdministratorDistrict(exterior);
            status=found?(exterior?"Nivel 94: preparando terreno y casas.":"Centro comercial: preparando estructura y tiendas."):"No se pudo viajar. Revisa que World y Explorer estén activos y el perfil sea válido.";
            if(found)CloseInterface();else RefreshMenu();return found;
        }
        public Vector3 Velocity(Transform view,Vector2 axis,float vertical,bool boost)
        {
            Transform basis=view!=null?view:transform;Vector3 right=basis.right,forward=basis.forward;
            V3 v=session.FlightVelocity(new V3(right.x,right.y,right.z),new V3(forward.x,forward.y,forward.z),axis.x,axis.y,vertical,boost);
            return new Vector3(v.X,v.Y,v.Z);
        }
        private void EnsureUI()
        {
            if(menuObject!=null)return;
            panelSettings=ScriptableObject.CreateInstance<PanelSettings>();panelSettings.name="Poolrooms chat and master panel";
            panelSettings.scaleMode=PanelScaleMode.ScaleWithScreenSize;panelSettings.referenceResolution=new Vector2Int(1280,800);
            panelSettings.themeStyleSheet=Resources.Load<ThemeStyleSheet>("PoolroomsTheme");
            menuObject=new GameObject("Poolrooms local chat and master menu");menuObject.transform.SetParent(transform,false);
            var document=menuObject.AddComponent<UIDocument>();document.panelSettings=panelSettings;document.sortingOrder=290;
            overlay=document.rootVisualElement;overlay.style.position=Position.Absolute;overlay.style.left=0;overlay.style.right=0;overlay.style.top=0;overlay.style.bottom=0;
            overlay.style.justifyContent=Justify.Center;overlay.style.alignItems=Align.Center;
            panel=new ScrollView(ScrollViewMode.Vertical);panel.style.width=650;panel.style.maxWidth=new Length(94,LengthUnit.Percent);panel.style.maxHeight=new Length(92,LengthUnit.Percent);
            panel.style.paddingLeft=26;panel.style.paddingRight=26;panel.style.paddingTop=24;panel.style.paddingBottom=24;overlay.Add(panel);
            overlay.RegisterCallback<KeyDownEvent>(evt=>{if(evt.keyCode==KeyCode.Escape){evt.StopImmediatePropagation();HandleEscape();}},TrickleDown.TrickleDown);
        }
        private Label Text(string text,int size=14)
        {
            var label=new Label(text){enableRichText=false};label.style.fontSize=size;label.style.whiteSpace=WhiteSpace.Normal;
            label.style.color=new Color(.88f,.88f,.82f);label.style.marginBottom=10;panel.Add(label);return label;
        }
        private void Button(string title,Action action)
        {
            var button=new Button(action){text=title};button.style.height=40;button.style.marginBottom=7;button.style.fontSize=15;
            button.style.color=new Color(.95f,.91f,.82f);button.style.backgroundColor=new Color(.22f,.14f,.13f);panel.Add(button);
        }
        private void Draw()
        {
            var scroll=panel as ScrollView;Vector2 offset=scroll!=null?scroll.scrollOffset:Vector2.zero;
            panel.Clear();input=null;messages=null;
            bool master=page==Page.Master;
            overlay.style.backgroundColor=new Color(.01f,.015f,.014f,master?.73f:.32f);
            panel.style.backgroundColor=master?new Color(.07f,.055f,.05f,.98f):new Color(.035f,.065f,.055f,.96f);
            panel.style.borderTopWidth=3;panel.style.borderTopColor=master?new Color(.8f,.28f,.17f):new Color(.35f,.65f,.5f);
            Text(master?"MODO ADMINISTRADOR":"CHAT LOCAL",25).style.unityFontStyleAndWeight=FontStyle.Bold;
            if(master)DrawMaster();else DrawChat();
            if(master&&scroll!=null)scroll.schedule.Execute(()=>{if(page==Page.Master)scroll.scrollOffset=offset;});
        }
        private void DrawChat()
        {
            Text("Mensajes y comandos de esta partida · /help · Esc para cerrar",12);
            messages=new ScrollView(ScrollViewMode.Vertical);messages.style.height=210;messages.style.marginBottom=12;panel.Add(messages);DrawMessages();
            input=new TextField(){maxLength=256};input.style.height=36;input.style.fontSize=16;panel.Add(input);
            input.RegisterCallback<KeyDownEvent>(evt=>
            {
                if(evt.keyCode==KeyCode.Y&&openedFrame==Time.frameCount){evt.StopImmediatePropagation();return;}
                if(evt.keyCode!=KeyCode.Return&&evt.keyCode!=KeyCode.KeypadEnter)return;
                evt.StopImmediatePropagation();string value=input.value;input.SetValueWithoutNotify("");Submit(value);
            },TrickleDown.TrickleDown);
            Button("Enviar",()=>{if(input==null)return;string value=input.value;input.SetValueWithoutNotify("");Submit(value);});
            Button("Cerrar",()=>CloseInterface());
            panel.schedule.Execute(()=>{if(page==Page.Chat&&input!=null)input.Focus();}).StartingIn(1);
        }
        private void DrawMessages()
        {
            if(messages==null)return;messages.Clear();Label last=null;
            foreach(string item in history)
            {last=new Label(item){enableRichText=false};last.style.whiteSpace=WhiteSpace.Normal;last.style.color=new Color(.82f,.88f,.8f);last.style.marginBottom=7;messages.Add(last);}
            if(last!=null){Label end=last;messages.schedule.Execute(()=>{if(page==Page.Chat&&messages!=null&&end.parent==messages.contentContainer)messages.ScrollTo(end);});}
        }
        private void DrawMaster()
        {
            if(!IsActive){CloseInterface();return;}
            Text(string.IsNullOrEmpty(status)?"Acceso activo solo en esta sesión.":status,13);
            var fly=new Toggle("Volar"){value=Flying};panel.Add(fly);fly.style.color=Color.white;fly.RegisterValueChangedCallback(e=>SetFlight(e.newValue));
            var clip=new Toggle("Atravesar paredes (activa vuelo)"){value=Noclip};panel.Add(clip);clip.style.color=Color.white;clip.RegisterValueChangedCallback(e=>SetNoclip(e.newValue));
            var speedLabel=Text("Velocidad: "+session.Options.speed.ToString("0")+" m/s");
            var speed=new Slider(5,250){value=session.Options.speed};panel.Add(speed);
            speed.RegisterValueChangedCallback(e=>{session.Options.speed=e.newValue;session.Options.Sanitize();speedLabel.text="Velocidad: "+session.Options.speed.ToString("0")+" m/s";});
            var boostLabel=Text("Impulso Shift: ×"+session.Options.boost.ToString("0.0"));
            var boost=new Slider(1,8){value=session.Options.boost};panel.Add(boost);
            boost.RegisterValueChangedCallback(e=>{session.Options.boost=e.newValue;session.Options.Sanitize();boostLabel.text="Impulso Shift: ×"+session.Options.boost.ToString("0.0");});
            Text("WASD · Volar según la mirada    Espacio · Subir\nCtrl izquierdo · Bajar    Shift · Impulso    M / Esc · Cerrar",13);
            Text("Cambiar vuelo o colisiones conserva tu ubicación. Dentro de una pared se corrige solo la intersección cercana; si no hay espacio, noclip sigue activo.",12);
            Button("Ajustes de renderizado · F7",()=>GetComponent<PoolroomsPauseMenu>()?.OpenRenderingSettings());
            Button("Volver al punto de aparición",()=>{explorer.ResetToSpawn();CloseInterface();});
            Button("Viajar al centro comercial 2010",()=>VisitMall());
            Button("Viajar a colinas Nivel 94",()=>VisitLevel94());
            Button("Visitar aeródromo de City11",()=>{bool found=explorer.world!=null&&explorer.world.VisitAirfield();status=found?"Aeródromo encontrado.":"No hay un aeródromo disponible con el perfil actual.";if(found)CloseInterface();else Draw();});
            Button("Visitar helipuerto de City11",()=>{bool found=explorer.world!=null&&explorer.world.VisitHelicopter();status=found?"Helipuerto encontrado.":"No hay un helipuerto disponible con el perfil actual.";if(found)CloseInterface();else Draw();});
            foreach(LiminalEnvironment environment in (LiminalEnvironment[])System.Enum.GetValues(typeof(LiminalEnvironment)))
            {
                LiminalEnvironment target=environment;string name=InteriorPlanner.Name(target);
                Button("Visitar "+name,()=>{bool found=explorer.world!=null&&explorer.world.VisitEnvironment(target);status=found?"Ubicación encontrada.":"Ese ambiente no está disponible cerca con el perfil actual.";if(found)CloseInterface();else Draw();});
            }
            Button("Restablecer trucos",()=>{session.ResetOptions();Synchronize();Draw();});
            Button("Continuar",()=>CloseInterface());Button("Desactivar modo administrador",DisableMasterMode);
        }
        private void OnDisable()
        {
            session.Lock();
            if(body!=null&&controllerOwned)
            {
                Vector3 localPosition;
                if(previousBodyEnabled&&PoolroomsCollisionRecovery.TryResolve(body,out localPosition))transform.position=localPosition;
                body.enabled=previousBodyEnabled;
            }
            controllerOwned=false;wasFlying=false;wasNoclip=false;
            CloseInterface(false);DestroyUI();
            if(explorer!=null)explorer.ResetMotionForMasterMode();
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        private void DestroyUI()
        {
            if(menuObject!=null){menuObject.SetActive(false);if(Application.isPlaying)Destroy(menuObject);else DestroyImmediate(menuObject);}
            if(panelSettings!=null){if(Application.isPlaying)Destroy(panelSettings);else DestroyImmediate(panelSettings);}
            menuObject=null;panelSettings=null;overlay=null;panel=null;messages=null;input=null;
        }
    }
}
