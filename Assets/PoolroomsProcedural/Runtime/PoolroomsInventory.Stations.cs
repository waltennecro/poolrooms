using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;
namespace Poolrooms
{
    public sealed partial class PoolroomsInventory
    {
        private readonly Dictionary<string,SlidingTiles> sliding=new Dictionary<string,SlidingTiles>();
        private readonly Dictionary<string,MosaicTiles> mosaics=new Dictionary<string,MosaicTiles>();
        private readonly Dictionary<string,byte[]> sketches=new Dictionary<string,byte[]>();
        private readonly Dictionary<string,float> brewing=new Dictionary<string,float>();
        private static void TrimStationCache<T>(Dictionary<string,T> cache,int capacity)
        {if(cache.Count<capacity)return;string first=null;foreach(var key in cache.Keys){first=key;break;}if(first!=null)cache.Remove(first);}
        public void RestoreStation(PoolroomsStation station)
        {if(station.kind==StationKind.Sketch&&sketches.TryGetValue(station.key,out var pixels))station.Paint(pixels);station.RestoreLight(this);RestoreWorkshop(station);}
        private static uint StationSeed(string key){uint hash=2166136261;foreach(char c in key??"")hash=unchecked((hash^c)*16777619);return hash;}
        public void OpenStation(PoolroomsStation station)
        {
            if(station==null||!station.Available(explorer))return;
            if(WorkshopCatalog.IsWorkshop(station.kind)){OpenWorkshop(station);return;}
            BeginInterface(ImmersionStations.Name(station.kind));
            Text("Un rincón preparado para quien pase por aquí. Sin prisa, puntos obligatorios ni personajes.","en-note",content);
            switch(station.kind)
            {
                case StationKind.SlidingPuzzle:SlidingStation(station);break;
                case StationKind.Mosaic:MosaicStation(station);break;
                case StationKind.Sketch:SketchStation(station);break;
                case StationKind.Tea:TeaStation(station);break;
                case StationKind.Water:
                    Text("Hay agua fresca. Puedes llenar una botella para el camino.","en-reading",content);
                    Button("Llenar una botella",()=>Reward(station,"water",(int)MallItemShape.Bottle*8,"Botella llena y guardada.",StationSound.Pour),content);break;
                case StationKind.Books:
                    Text(RoomMemories.At(unchecked((int)StationSeed(station.key)),station.variant,7,CurrentBiome()).Note,"en-reading",content);
                    Journal.Read(station.key);Button("Tomar un libro para el camino",()=>Reward(station,"book",0,"Libro guardado. Quedan otros para la próxima visita.",StationSound.Page),content);
                    Button("Devolver un libro",()=>{if(!Valid(station))return;string found=null;for(int i=0;i<ExplorationInventory.Capacity;i++){var slot=Items.Slot(i);if(!slot.Empty&&ExplorationItems.Resolve(slot.Key)?.Use==ItemUse.Read){found=slot.Key;break;}}if(found==null){Toast("No tienes un libro o una postal para dejar.");return;}Items.Remove(found,System.Numerics.BigInteger.One);Journal.Record(RoomActivity.Reading,station.key+":return");SoundStation(station,StationSound.Page);Toast("Libro dejado para otro viajero.");RefreshAll();},content,"en-quiet");break;
                case StationKind.Postcards:
                    Text("Una postal dice: «Volvemos por la galería del jardín. Dejamos la luz encendida para que encuentres el camino». Puedes llevarte una o escribir aquí una nota.","en-reading",content);Journal.Read(station.key);
                    Button("Guardar una postal",()=>Reward(station,"postcard",8,"Postal guardada.",StationSound.Paper),content);NoteStation(station);break;
                case StationKind.Parcels:
                    Text("Paquetes sin nombre preparados para los viajeros. Elige algo útil; cada compartimento se recoge una sola vez durante esta sesión.","en-reading",content);
                    Button("Galletas",()=>Reward(station,"snack",(int)MallItemShape.FoodBox*8,"Galletas guardadas.",StationSound.Parcel),content);
                    Button("Pelota",()=>Reward(station,"ball",(int)MallItemShape.Ball*8,"Pelota guardada. Sácala del inventario para jugar.",StationSound.Parcel),content);
                    Button("Cuaderno",()=>Reward(station,"notebook",8,"Cuaderno guardado.",StationSound.Paper),content);break;
                case StationKind.MusicBox:
                    Text("Toca una secuencia breve de notas suaves. La música sale de esta caja y responde al tamaño de la sala.","en-reading",content);
                    var row=Element("en-tabs",content);for(int i=0;i<8;i++){int note=i;Button(new[]{"DO","RE","MI","FA","SOL","LA","SI","DO"}[i],()=>{if(!Valid(station))return;sound?.PlayInteractiveNote(note,station.SoundPosition);Animate(HumanoidGesture.PlayKeys,.7f);station.Pulse();Journal.Record(RoomActivity.Music,station.key);},row);}break;
            }
        }
        private bool Valid(PoolroomsStation station){if(station!=null&&station.Available(explorer))return true;CloseInterface();return false;}
        private void SoundStation(PoolroomsStation station,StationSound cue){sound?.PlayStation(cue,station.SoundPosition);station.Pulse();if(cue==StationSound.Pour)station.Pour();}
        private bool Reward(PoolroomsStation station,string compartment,int catalog,string message,StationSound cue)
        {
            if(!Valid(station))return false;string key=station.key+":"+compartment;
            if(WasCollected(key)){Toast("Este compartimento ya está recogido durante esta sesión.");return false;}
            if(!Items.Add("catalog:"+catalog,System.Numerics.BigInteger.One)){Toast("Hace falta un espacio para otro tipo de objeto.");return false;}
            MarkCollected(key);SoundStation(station,cue);Animate(cue==StationSound.Pour?HumanoidGesture.Water:HumanoidGesture.Reach,1.6f);Toast(message);RefreshAll();return true;
        }
        private void NoteStation(PoolroomsStation station)
        {
            var field=new TextField("Tu nota para la próxima visita"){multiline=true,maxLength=240};field.AddToClassList("en-station-note");content.Add(field);
            Button("Dejar esta nota",()=>{if(!Valid(station)||string.IsNullOrWhiteSpace(field.value))return;if(!notesLeft.ContainsKey(station.key))TrimStationCache(notesLeft,4096);notesLeft[station.key]=field.value.Trim();SoundStation(station,StationSound.Pencil);Journal.Record(RoomActivity.Notice,station.key+":written");Toast("Nota colocada durante esta sesión. Puedes volver a leerla aquí.");},content,"en-quiet");
            if(notesLeft.TryGetValue(station.key,out string note))Text(note,"en-reading",content);
        }
        private readonly Dictionary<string,string> notesLeft=new Dictionary<string,string>();
        private void TeaStation(PoolroomsStation station)
        {
            Text("Prepara una taza, espera un momento y guárdala para tomarla cuando quieras.","en-reading",content);var status=Text("","en-note",content);
            Button("Preparar la infusión",()=>{if(!Valid(station))return;if(brewing.TryGetValue(station.key,out float until)&&Time.time<until){Toast("La infusión sigue reposando.");return;}if(WasCollected(station.key+":tea")){Toast("Esta taza ya está preparada y recogida.");return;}if(!brewing.ContainsKey(station.key))TrimStationCache(brewing,256);brewing[station.key]=Time.time+4;SoundStation(station,StationSound.Pour);Animate(HumanoidGesture.Water,2);},content);
            Button("Guardar la taza preparada",()=>{if(!Valid(station))return;if(!brewing.TryGetValue(station.key,out float until)||Time.time<until){Toast("Primero prepara la infusión y deja que repose unos segundos.");return;}Reward(station,"tea",(int)MallItemShape.Mug*8,"Taza preparada y guardada.",StationSound.Cup);},content);
            status.schedule.Execute(()=>{if(!InterfaceOpen||station==null)return;status.text=brewing.TryGetValue(station.key,out float t)?Time.time<t?"Reposando · "+Mathf.CeilToInt(t-Time.time)+" s":"La taza está lista.":"Todo está preparado para empezar.";}).Every(200);
        }
        private void SlidingStation(PoolroomsStation station)
        {
            if(!sliding.TryGetValue(station.key,out var game)){TrimStationCache(sliding,128);game=new SlidingTiles(StationSeed(station.key));sliding.Add(station.key,game);}
            Text("Ordena las fichas del 1 al 15 deslizando una ficha junto al espacio vacío. Sin límite de tiempo.","en-note",content);var board=Element("en-station-board",content);var status=Text("","en-note",content);
            Action redraw=null;redraw=()=>{board.Clear();status.text=game.Complete?"Resuelto. El rincón queda listo para la próxima visita.":"Movimientos: "+game.Moves;for(int i=0;i<16;i++){int cell=i;var b=Button(game.Tiles[i]==0?"":game.Tiles[i].ToString(),()=>{if(!Valid(station)||!game.Slide(cell))return;SoundStation(station,StationSound.Tile);Animate(HumanoidGesture.Place,.5f);if(game.Complete)Journal.Record(RoomActivity.Game,station.key);redraw();},board,"en-station-tile");b.SetEnabled(game.Tiles[i]!=0);}};redraw();
        }
        private void MosaicStation(PoolroomsStation station)
        {
            if(!mosaics.TryGetValue(station.key,out var game)){TrimStationCache(mosaics,128);game=new MosaicTiles(StationSeed(station.key));mosaics.Add(station.key,game);}
            string[] arrows={"↑","→","↓","←"};Text("Gira las nueve piezas hasta que coincidan con el dibujo de referencia.","en-note",content);var target=Element("en-tabs",content);for(int i=0;i<9;i++)Text(arrows[game.Target[i]],"en-invitation",target);
            var board=Element("en-mosaic-board",content);var status=Text("","en-note",content);Action redraw=null;
            redraw=()=>{board.Clear();status.text=game.Complete?"Mosaico terminado. Gracias por dejar un poco de color.":"Pulsa una pieza para girarla.";for(int i=0;i<9;i++){int tile=i;Button(arrows[game.Turns[i]],()=>{if(!Valid(station))return;game.Turn(tile);SoundStation(station,StationSound.Ceramic);if(game.Complete)Journal.Record(RoomActivity.Game,station.key);redraw();},board,"en-station-tile");}};redraw();
        }
        private void SketchStation(PoolroomsStation station)
        {
            if(!sketches.TryGetValue(station.key,out var pixels)){TrimStationCache(sketches,128);pixels=new byte[64];sketches.Add(station.key,pixels);}
            Text("Deja un dibujo de ocho por ocho casillas. También aparecerá en la pizarra del mundo durante esta sesión.","en-note",content);int colour=1;var palette=Element("en-tabs",content);
            for(int i=0;i<6;i++){int pick=i;var b=Button("Color "+(i+1),()=>colour=pick,palette);b.style.backgroundColor=PoolroomsStation.Palette(i);}
            var board=Element("en-sketch-board",content);var cells=new Button[64];
            for(int i=0;i<64;i++){int at=i;cells[i]=Button("",()=>{if(!Valid(station))return;pixels[at]=(byte)colour;cells[at].style.backgroundColor=PoolroomsStation.Palette(colour);station.Paint(pixels);SoundStation(station,StationSound.Pencil);Journal.Record(RoomActivity.Notice,station.key+":drawing");},board,"en-sketch-cell");cells[i].style.backgroundColor=PoolroomsStation.Palette(pixels[i]);}
            Button("Borrar el dibujo",()=>{if(!Valid(station))return;Array.Clear(pixels,0,pixels.Length);station.Paint(pixels);for(int i=0;i<64;i++)cells[i].style.backgroundColor=PoolroomsStation.Palette(0);},content,"en-quiet");station.Paint(pixels);
        }
    }
}
