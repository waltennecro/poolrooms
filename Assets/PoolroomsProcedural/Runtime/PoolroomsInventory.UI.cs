using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor=UnityEngine.Cursor;

namespace Poolrooms
{
    public sealed partial class PoolroomsInventory
    {
        private GameObject uiObject;
        private PanelSettings panel;
        private VisualElement root,modal,content,grid,sidebar,hotbarRoot;
        private Label toastLabel,promptLabel,titleLabel,detailLabel;
        private readonly Button[] cells=new Button[ExplorationInventory.SlotsPerPage];
        private readonly Button[] hotbarCells=new Button[ExplorationInventory.HotbarSize];
        private readonly Dictionary<string,Texture2D> iconCache=new Dictionary<string,Texture2D>();
        private int page,selected=-1,moveFrom=-1;
        private int barVersion=-1,barSelection=-1;
        private bool barDisplayed;
        private void EnsureUI()
        {
            if(root!=null)return;
            panel=ScriptableObject.CreateInstance<PanelSettings>();panel.name="Encuentro · inventario";panel.scaleMode=PanelScaleMode.ConstantPixelSize;
            panel.scale=Mathf.Clamp(Screen.height/900f,.65f,1.25f);panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("PoolroomsTheme");
            uiObject=new GameObject("Encuentro inventory interface");uiObject.transform.SetParent(transform,false);
            var document=uiObject.AddComponent<UIDocument>();document.panelSettings=panel;document.sortingOrder=280;
            root=document.rootVisualElement;root.pickingMode=PickingMode.Ignore;root.AddToClassList("en-root");
            var stylesheet=Resources.Load<StyleSheet>("EncuentroInterface");if(stylesheet!=null)root.styleSheets.Add(stylesheet);
            toastLabel=Text("","en-toast",root);toastLabel.pickingMode=PickingMode.Ignore;
            promptLabel=Text("","en-prompt",root);promptLabel.pickingMode=PickingMode.Ignore;
            hotbarRoot=Element("en-hotbar",root);for(int i=0;i<hotbarCells.Length;i++){int at=i;hotbarCells[i]=Button("",()=>{SelectedHotbar=at;RefreshHotbar();},hotbarRoot,"en-hotbar-slot");}
            modal=Element("en-backdrop",root);modal.style.display=DisplayStyle.None;
            var card=Element("en-card",modal);var header=Element("en-header",card);titleLabel=Text("Inventario","en-title",header);Button("Cerrar · Tab / Esc",CloseInterface,header,"en-quiet");
            content=Element("en-content",card);RefreshHotbar();
        }
        private static VisualElement Element(string name,VisualElement parent){var node=new VisualElement();node.AddToClassList(name);parent.Add(node);return node;}
        private static Label Text(string value,string css,VisualElement parent){var node=new Label(value){enableRichText=false};node.AddToClassList(css);parent.Add(node);return node;}
        private static Button Button(string title,Action action,VisualElement parent,string css="en-button")
        {var button=new Button(action){text=title};button.AddToClassList(css);parent.Add(button);return button;}
        private void BeginInterface(string title)
        {
            EnsureUI();if(!InterfaceOpen){previousLock=Cursor.lockState;previousCursor=Cursor.visible;}
            InterfaceOpen=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;explorer.Wings?.Suspend();
            modal.style.display=DisplayStyle.Flex;titleLabel.text=title;content.Clear();grid=sidebar=null;
            hotbarRoot.style.display=DisplayStyle.None;promptLabel.text="";
        }
        public void CloseInterface()
        {
            if(!InterfaceOpen)return;InterfaceOpen=false;if(modal!=null)modal.style.display=DisplayStyle.None;
            Cursor.lockState=previousLock;Cursor.visible=previousCursor;grid=sidebar=null;moveFrom=-1;RefreshHotbar();
        }
        public void OpenInventory()
        {
            BeginInterface("Tus cosas");var navigation=Element("en-tabs",content);
            for(int i=0;i<ExplorationInventory.Pages;i++){int at=i;var tab=Button("Página "+(i+1),()=>{page=at;selected=-1;OpenInventory();},navigation);tab.EnableInClassList("en-selected",i==page);}
            Button("Cuaderno",OpenJournal,navigation,"en-quiet");
            var layout=Element("en-inventory",content);grid=Element("en-grid",layout);sidebar=Element("en-sidebar",layout);
            for(int i=0;i<cells.Length;i++){int at=page*ExplorationInventory.SlotsPerPage+i;cells[i]=Button("",()=>Select(at),grid,"en-slot");}
            detailLabel=Text("Selecciona un objeto.","en-detail",sidebar);
            Button("Usar",()=>{var key=SelectedKey();if(key!=null)Use(key);},sidebar);
            Button("Sacar y sostener",()=>{var key=SelectedKey();if(key!=null)Equip(key);},sidebar);
            Button("Guardar lo que sostengo",()=>StoreHeld(),sidebar);
            Button("Mover a otro espacio",()=>{if(selected>=0&&!Items.Slot(selected).Empty){moveFrom=selected;Toast("Elige el espacio de destino, también en otra página.");}},sidebar,"en-quiet");
            Text("Asignar a la barra rápida","en-caption",sidebar);var bind=Element("en-tabs",sidebar);
            for(int i=0;i<ExplorationInventory.HotbarSize;i++){int at=i;Button(HotbarKey(i),()=>{if(Items.Bind(at,selected)){RefreshAll();Toast("Acceso rápido asignado.");}},bind,"en-bind");}
            Text("250 tipos de objeto · 50 espacios por página\nLas cantidades por pila no tienen un máximo fijo.\n5 muestra u oculta la barra; X usa su objeto.\nDatos de esta sesión: todavía no se guarda la partida.","en-note",sidebar);
            RefreshAll();
        }
        private string SelectedKey(){return selected>=0&&selected<ExplorationInventory.Capacity&&!Items.Slot(selected).Empty?Items.Slot(selected).Key:null;}
        private void Select(int index)
        {
            if(moveFrom>=0){Items.Move(moveFrom,index);moveFrom=-1;}selected=index;RefreshAll();
        }
        public void RefreshAll()
        {
            if(root==null)return;RefreshHotbar();
            if(grid!=null&&sidebar!=null)
            {
                for(int i=0;i<cells.Length;i++)
                {
                    int index=page*ExplorationInventory.SlotsPerPage+i;var slot=Items.Slot(index);var node=cells[i];node.Clear();node.text="";node.EnableInClassList("en-selected",index==selected);
                    if(slot.Empty){Text((index+1).ToString(),"en-slot-number",node);continue;}
                    var definition=ExplorationItems.Resolve(slot.Key);Texture2D icon=Icon(definition);
                    if(icon!=null){var image=new Image{image=icon};image.AddToClassList("en-icon");node.Add(image);}
                    Text(definition.Name,"en-slot-name",node);Text(ExplorationInventory.DisplayCount(slot.Count),"en-count",node);
                }
                var key=SelectedKey();var item=ExplorationItems.Resolve(key);
                detailLabel.text=item==null?"Selecciona un objeto.":item.Name+"\n\n"+item.Description+"\n\nCantidad: "+ExplorationInventory.DisplayCount(Items.Count(key))+"\nAcción: "+UseName(item.Use);
            }
        }
        private static string UseName(ItemUse use)
        {switch(use){case ItemUse.Read:return "Leer";case ItemUse.Drink:return "Beber";case ItemUse.Eat:return "Comer";case ItemUse.Photograph:return "Fotografiar";case ItemUse.Journal:return "Abrir cuaderno";case ItemUse.Water:return "Regar";case ItemUse.Mark:return "Dejar señal";case ItemUse.Play:return "Sacar para jugar";case ItemUse.Listen:return "Escuchar música";case ItemUse.Clean:return "Limpiar";default:return "Sostener y colocar";}}
        private Texture2D Icon(ExplorationItem item)
        {
            if(item==null)return null;string name=item.Use==ItemUse.Journal?"journal":item.Use==ItemUse.Photograph?"camera":item.Use==ItemUse.Water?"plant":item.Use==ItemUse.Mark?"compass":item.Use==ItemUse.Read?"book":item.Use==ItemUse.Eat?"food":item.Use==ItemUse.Drink?"cup":item.Use==ItemUse.Listen?"music":item.Use==ItemUse.Play?"game":item.Source!=null?"furniture":"bag";
            if(!iconCache.TryGetValue(name,out var icon)){icon=Resources.Load<Texture2D>("EncounterIcons/"+name);iconCache[name]=icon;}return icon;
        }
        private static string HotbarKey(int i){return (i<4?i+1:i+2).ToString();}
        private void RefreshHotbar()
        {
            if(hotbarRoot==null)return;hotbarRoot.style.display=HotbarVisible&&!InterfaceOpen?DisplayStyle.Flex:DisplayStyle.None;
            bool displayed=HotbarVisible&&!InterfaceOpen;if(barVersion==Items.Version&&barSelection==SelectedHotbar&&barDisplayed==displayed)return;barVersion=Items.Version;barSelection=SelectedHotbar;barDisplayed=displayed;
            for(int i=0;i<hotbarCells.Length;i++){string key=Items.Binding(i);var item=ExplorationItems.Resolve(key);hotbarCells[i].text=HotbarKey(i)+"\n"+(item!=null?item.Name:"Vacío")+"\n"+ExplorationInventory.DisplayCount(Items.Count(key));hotbarCells[i].EnableInClassList("en-selected",i==SelectedHotbar);}
        }
        private void RefreshToast(){EnsureUI();toastLabel.text=Status;}
        private void RefreshPrompt(){if(root==null)return;promptLabel.text=focused!=null?"E · "+focused.ActionLabel+(focused.GetComponent<PoolroomsPickup>()!=null?"    T · tomar / mover":""):focusedPickup!=null?"E · tomar    Z · guardar lo que sostengo":"";}
        public void ShowText(string title,string message)
        {BeginInterface(title);var scroll=new ScrollView();content.Add(scroll);Text(message,"en-reading",scroll);Button("Volver a explorar",CloseInterface,content);}
        public void OpenJournal()
        {
            BeginInterface("Cuaderno de viaje");var scroll=new ScrollView();content.Add(scroll);
            Text("Un mundo para quedarse un rato","en-subtitle",scroll);
            Text("Estos son propósitos opcionales. Puedes ignorarlos y seguir explorando a tu ritmo.","en-note",scroll);
            foreach(var invitation in Journal.Invitations())Text(invitation,"en-invitation",scroll);
            Text("Salas visitadas: "+Journal.Rooms+"\nLos recuerdos, las plantas cuidadas y el inventario pertenecen a esta sesión.","en-note",scroll);
            AddRoutes(scroll);
            Button("Mis objetos",OpenInventory,content);Button("Abrir carpeta de fotos",OpenPhotos,content,"en-quiet");
        }
        public void OpenGame(string key,uint seed,Vector3 position)
        {
            if(!games.TryGetValue(key,out var game)){if(games.Count>=128)games.Clear();game=new PairingGame(seed);games.Add(key,game);}
            game.HideMiss();BeginInterface("Una partida a parejas");Text("Encuentra ocho parejas. Sin cronómetro ni penalizaciones.","en-note",content);
            var board=Element("en-game-grid",content);var status=Text("","en-note",content);
            Action redraw=null;redraw=()=>
            {
                board.Clear();status.text=game.Complete?"Partida terminada. Gracias por dejar la mesa lista para la próxima visita.":"Parejas: "+game.Matched+" / 8 · Intentos: "+game.Attempts;
                for(int i=0;i<16;i++)
                {
                    int card=i;string[] symbols={"SOL","HOJA","TAZA","FLOR","AGUA","LUNA","LIBRO","CASA"};
                    var button=Button(game.Visible(i)?symbols[game.Cards[i]]:"·",()=>
                    {
                        if(!game.Turn(card))return;Animate(HumanoidGesture.Place,.65f);sound?.PlayInteractiveNote(game.Cards[card],position);
                        if(game.Complete){Journal.Record(RoomActivity.Game,key);Toast("Un pequeño recuerdo: partida completada.");}
                        redraw();if(game.Second>=0)board.schedule.Execute(()=>{game.HideMiss();redraw();}).StartingIn(1100);
                    },board,"en-game-card");button.SetEnabled((game.Mask&(1<<i))==0);
                }
            };redraw();
            Button("Otra partida",()=>{games.Remove(key);OpenGame(key,seed+1,position);},content,"en-quiet");
        }
        public void OpenPiano(string key,Vector3 position,PoolroomsPianoKeys keyboard=null)
        {
            BeginInterface("Teclado de sobremesa");Text("Prueba una melodía. Puedes usar las teclas A S D F G H J K.","en-note",content);var keys=Element("en-piano",content);
            for(int i=0;i<8;i++){int note=i;Button(new[]{"DO","RE","MI","FA","SOL","LA","SI","DO"}[i],()=>Play(note),keys,"en-piano-key");}
            keys.focusable=true;keys.Focus();keys.RegisterCallback<KeyDownEvent>(e=>{int note="asdfghjk".IndexOf(char.ToLowerInvariant(e.character));if(note>=0){Play(note);e.StopPropagation();}});
            void Play(int note){if(!InterfaceOpen)return;keyboard?.Press(note);sound?.PlayInteractiveNote(note,position);Animate(HumanoidGesture.PlayKeys,.7f);Journal.Record(RoomActivity.Music,key);}
        }
        private void DisposeUI()
        {if(uiObject!=null)Destroy(uiObject);if(panel!=null)Destroy(panel);root=modal=content=grid=sidebar=hotbarRoot=null;uiObject=null;panel=null;}
    }
}
