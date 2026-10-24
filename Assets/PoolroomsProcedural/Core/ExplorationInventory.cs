using System;
using System.Collections.Generic;
using System.Numerics;

namespace Poolrooms.Core
{
    public enum ItemUse { None,Read,Drink,Eat,Photograph,Journal,Water,Mark,Play,Listen,Clean }
    public sealed class ExplorationItem
    {
        public readonly string Key,Name,Description,Source;
        public readonly int CatalogId,PlayKind,PlayColour;
        public readonly ItemUse Use;
        public readonly bool Consumable;
        public ExplorationItem(string key,string name,string description,ItemUse use=ItemUse.None,bool consumable=false,int catalog=-1,string source=null,int playKind=-1,int playColour=0)
        {Key=key;Name=name;Description=description;Use=use;Consumable=consumable;CatalogId=catalog;Source=source;PlayKind=playKind;PlayColour=playColour;}
    }
    public static class ExplorationItems
    {
        public const int ToolCount=10;
        private static readonly ExplorationItem[] tools={
            new ExplorationItem("tool:0","Cuaderno de viaje","Descubrimientos, recuerdos y pequeños propósitos opcionales.",ItemUse.Journal),
            new ExplorationItem("tool:1","Cámara de bolsillo","Encuadra un lugar y añade una fotografía a tus recuerdos de esta sesión.",ItemUse.Photograph,catalog:11*8),
            new ExplorationItem("tool:2","Regadera","Cuida las plantas mirando una maceta cercana.",ItemUse.Water,source:"WaterBottle"),
            new ExplorationItem("tool:3","Tiza de colores","Deja una señal discreta en el suelo para orientarte durante esta sesión.",ItemUse.Mark,catalog:2*8),
            new ExplorationItem("tool:4","Paño de lino","Limpia y ordena pequeños rincones que encuentres.",ItemUse.Clean,catalog:30*8),
            new ExplorationItem("tool:5","Álbum de postales","Pequeñas invitaciones a descubrir sitios tranquilos.",ItemUse.Read,catalog:1*8),
            new ExplorationItem("tool:6","Pelota de viaje","Sácala del inventario para lanzarla y jugar.",ItemUse.Play,playKind:0),
            new ExplorationItem("tool:7","Agua potable","Un pequeño descanso, sin medidor de hambre ni obligación de consumir.",ItemUse.Drink,true,21*8),
            new ExplorationItem("tool:8","Galletas de bienvenida","Alguien dejó algo para compartir.",ItemUse.Eat,true,25*8),
            new ExplorationItem("tool:9","Libro de sobremesa","Relatos breves y cálidos sobre quienes usan estos lugares.",ItemUse.Read,catalog:0)
        };
        private static readonly Dictionary<string,ExplorationItem> cache=new Dictionary<string,ExplorationItem>(StringComparer.Ordinal);
        public static ExplorationItem Tool(int id){return id>=0&&id<tools.Length?tools[id]:null;}
        public static ExplorationItem Resolve(string key)
        {
            if(string.IsNullOrEmpty(key)||key.Length>128)return null;
            if(cache.TryGetValue(key,out var prior))return prior;
            var parts=key.Split(':');ExplorationItem item=null;
            if(parts.Length==2&&parts[0]=="level10"&&int.TryParse(parts[1],out int fieldItem))return Level10Activities.Item(fieldItem);
            if(parts.Length==2&&parts[0]=="forest39"&&int.TryParse(parts[1],out int forestItem))return Forest39Activities.Item(forestItem);
            if(parts.Length==2&&parts[0]=="food"&&int.TryParse(parts[1],out int food)&&food>=0&&food<ProvisionCatalog.Count)
                item=ProvisionCatalog.Item(food);
            if(parts.Length==2&&parts[0]=="tool"&&int.TryParse(parts[1],out int tool))return Tool(tool);
            if(parts.Length==2&&parts[0]=="catalog"&&int.TryParse(parts[1],out int id)&&id>=0&&id<MallItemCatalog.Count)
            {
                var d=MallItemCatalog.Get(id);ItemUse use=d.Shape==MallItemShape.Book||d.Shape==MallItemShape.Notebook?ItemUse.Read:d.Shape==MallItemShape.Bottle||d.Shape==MallItemShape.Mug?ItemUse.Drink:d.Shape==MallItemShape.Snack||d.Shape==MallItemShape.FoodBox?ItemUse.Eat:d.Shape==MallItemShape.Camera?ItemUse.Photograph:d.Shape==MallItemShape.Radio||d.Shape==MallItemShape.Headphones?ItemUse.Listen:d.Shape==MallItemShape.Soap||d.Shape==MallItemShape.Towel?ItemUse.Clean:d.Shape==MallItemShape.Ball||d.Shape==MallItemShape.Blocks||d.Shape==MallItemShape.Gamepad?ItemUse.Play:ItemUse.None;
                item=new ExplorationItem("catalog:"+id,d.Name,"Objeto de la sala. Puedes guardarlo, examinarlo o volver a colocarlo.",use,use==ItemUse.Drink||use==ItemUse.Eat,catalog:id);
            }
            if(parts.Length==2&&parts[0]=="source")
            {
                int known=-1;for(int i=0;i<SourceFurniture.Count;i++)if(SourceFurniture.Id(i)==parts[1])known=i;
                if(known>=0||parts[1]=="Lantern")
                {
                    string[] names={"Botella de agua","Equipo de música","Silla de madera","Sofá de terciopelo","Sillón tapizado","Sofá de cuero","Cámara de sobremesa","Mesa de café","Silla de café","Planta en maceta","Lámpara Art Nouveau","Taza de porcelana","Jarrón con flores","Lámpara de sobremesa","Refrigerador comercial","Puf de seda","Zapatilla de exposición","Lámpara de vidriera","Lámpara de escritorio","Mandarina","Aguacate","Plato de aceitunas","Silla de cafetería interior","Mesa de comedor","Caja registradora","Taburete de barra","Servilletero","Cartel de menú","Perchero de pie","Perchero de madera","Cesta de mimbre","Plato de porcelana","Cuenco de porcelana","Pila de servilletas","Radiador antiguo","Maceta de cerámica"};
                    item=new ExplorationItem(key,known>=0?names[known]:"Farol","Modelo con licencia; procedencia y atribución en Créditos.",known==0||known==11?ItemUse.Drink:known==1?ItemUse.Listen:known==6?ItemUse.Photograph:known>=19&&known<=21?ItemUse.Eat:ItemUse.None,known==19||known==20,source:parts[1]);
                }
            }
            if(parts.Length==3&&parts[0]=="play"&&int.TryParse(parts[1],out int kind)&&int.TryParse(parts[2],out int colour)&&kind>=0&&kind<3&&colour>=0&&colour<16)
                item=new ExplorationItem("play:"+kind+":"+colour,kind==0?"Pelota blanda":kind==1?"Bloque acolchado":"Aro de juego","Puedes sacarlo, colocarlo y jugar con él.",ItemUse.Play,playKind:kind,playColour:colour);
            if(item!=null)cache.Add(key,item);return item;
        }
    }
    public sealed class InventorySlot
    {
        public string Key {get;internal set;}
        public BigInteger Count {get;internal set;}
        public bool Empty {get{return Key==null||Count<=0;}}
    }
    // Session-only data; no files or PlayerPrefs. Stack counts are arbitrary precision.
    public sealed class ExplorationInventory
    {
        public const int Pages=5,SlotsPerPage=50,Capacity=Pages*SlotsPerPage,HotbarSize=8;
        private readonly InventorySlot[] slots=new InventorySlot[Capacity];
        private readonly string[] bindings=new string[HotbarSize];
        public int Version {get;private set;}
        public ExplorationInventory(){for(int i=0;i<slots.Length;i++)slots[i]=new InventorySlot();}
        public InventorySlot Slot(int index){if(index<0||index>=Capacity)throw new ArgumentOutOfRangeException(nameof(index));return slots[index];}
        public int Find(string key){for(int i=0;i<Capacity;i++)if(!slots[i].Empty&&slots[i].Key==key)return i;return -1;}
        public BigInteger Count(string key){int at=Find(key);return at<0?BigInteger.Zero:slots[at].Count;}
        public bool Add(string key,BigInteger amount)
        {
            var definition=ExplorationItems.Resolve(key);if(amount<=0||definition==null||definition.Key!=key)return false;
            int at=Find(key);if(at<0)for(int i=0;i<Capacity;i++)if(slots[i].Empty){at=i;break;}
            if(at<0)return false;slots[at].Key=key;slots[at].Count+=amount;Version++;return true;
        }
        public bool Remove(string key,BigInteger amount)
        {
            if(amount<=0)return false;int at=Find(key);if(at<0||slots[at].Count<amount)return false;
            slots[at].Count-=amount;if(slots[at].Count==0)slots[at].Key=null;Version++;return true;
        }
        public bool Move(int from,int to)
        {
            if(from<0||from>=Capacity||to<0||to>=Capacity||from==to||slots[from].Empty)return false;
            var a=slots[from];var b=slots[to];string key=b.Key;BigInteger count=b.Count;b.Key=a.Key;b.Count=a.Count;a.Key=key;a.Count=count;Version++;return true;
        }
        public bool Bind(int hotbar,int slot)
        {if(hotbar<0||hotbar>=HotbarSize||slot<0||slot>=Capacity||slots[slot].Empty)return false;bindings[hotbar]=slots[slot].Key;Version++;return true;}
        public string Binding(int index){return index>=0&&index<HotbarSize?bindings[index]:null;}
        public static string DisplayCount(BigInteger count)
        {string text=count.ToString();return text.Length<=9?text:text.Substring(0,3)+"… ("+text.Length+" dígitos)";}
    }
}
