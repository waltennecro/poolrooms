using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum LifeFurnishing { Source,Cabinet,Piano,GameTable,Catalog }
    public readonly struct LifePlacement
    {
        public readonly LifeFurnishing Kind;
        public readonly int Source,Slot;
        public readonly V3 Bottom;
        public readonly float Yaw;
        public LifePlacement(LifeFurnishing kind,int source,int slot,V3 bottom,float yaw=0){Kind=kind;Source=source;Slot=slot;Bottom=bottom;Yaw=yaw;}
    }
    public sealed class RoomLifePlan
    {
        public RoomVignette Vignette;
        public DressingAnchor? Notice;
        public readonly List<LifePlacement> Furniture=new List<LifePlacement>();
    }
    public static class RoomLifePlanner
    {
        public const int MaxFurniture=9;
        public static RoomLifePlan Build(Room room,RoomGeometry geometry,LayoutConfig c)
        {
            var plan=new RoomLifePlan{Vignette=RoomMemories.At(c.Seed,room.X,room.Z,room.Environment)};
            if(geometry.Dressing!=null)foreach(var anchor in geometry.Dressing.Anchors)
                if(anchor.Center.Y>=room.Floor+.85f&&anchor.Center.Y<=room.Floor+1.9f&&V3.Dot(anchor.Normal,new V3(-anchor.Center.X,0,-anchor.Center.Z))>.1f)plan.Notice=anchor;
            if(room.MallGateway||room.TerrainGateway)return plan;
            uint h=LiminalRegions.Hash(c.Seed,room.X,room.Z,0xCAFE03u);
            for(int trial=0;trial<4;trial++)
            {
                int corner=((int)(h%4)+trial)%4;float x=(corner%2==0?-1:1)*room.Width*.29f,z=(corner<2?-1:1)*room.Depth*.29f;
                float floor=room.Floor;
                bool wet=false;foreach(var water in geometry.Regions)if(water.Contains(x,z)&&water.Level>floor+.015f)wet=true;
                if(wet)continue;
                int turn=(int)((h>>4)%4);float angle=turn*Shapes.Pi*.5f;
                V3 Point(float ox,float oz,float y){return new V3(x+(float)Math.Cos(angle)*ox+(float)Math.Sin(angle)*oz,y,z-(float)Math.Sin(angle)*ox+(float)Math.Cos(angle)*oz);}
                bool lounge=plan.Vignette.Activity==RoomActivity.Reading||plan.Vignette.Activity==RoomActivity.Rest;
                var table=new SourceFurniturePlacement(0,7,Point(0,0,floor+.006f),turn*90);
                var chair=new SourceFurniturePlacement(1,lounge?3:8,Point(lounge?-1.5f:-1.05f,0,floor+.006f),90+turn*90);
                var plant=new SourceFurniturePlacement(2,9,Point(1.15f,.9f,floor+.006f),turn*90);
                var secondChair=new SourceFurniturePlacement(6,8,Point(1.05f,0,floor+.006f),270+turn*90);
                if(!Fits(table)||!Fits(chair)||!Fits(plant)||!lounge&&!Fits(secondChair))continue;
                plan.Furniture.Add(new LifePlacement(LifeFurnishing.Source,7,0,table.Bottom,table.Yaw));
                plan.Furniture.Add(new LifePlacement(LifeFurnishing.Source,chair.Source,1,chair.Bottom,chair.Yaw));
                plan.Furniture.Add(new LifePlacement(LifeFurnishing.Source,9,2,plant.Bottom,plant.Yaw));
                float top=table.Bottom.Y+SourceFurniture.Dimensions(7).Y+.006f;
                plan.Furniture.Add(new LifePlacement(LifeFurnishing.Source,11,3,Point(-.25f,-.14f,top)));
                plan.Furniture.Add(new LifePlacement(LifeFurnishing.Source,11,5,Point(.25f,-.14f,top)));
                if(!lounge)plan.Furniture.Add(new LifePlacement(LifeFurnishing.Source,8,6,secondChair.Bottom,secondChair.Yaw));
                if(plan.Vignette.Activity==RoomActivity.Plant)
                    plan.Furniture.Add(new LifePlacement(LifeFurnishing.Source,12,7,Point(0,.12f,top),turn*90));
                else
                {
                    plan.Furniture.Add(new LifePlacement(LifeFurnishing.Catalog,lounge?0:8,7,Point(0,.18f,top),turn*90));
                    plan.Furniture.Add(new LifePlacement(LifeFurnishing.Source,0,8,Point(.23f,.2f,top),turn*90));
                }
                // A detailed functional furnishing replaces this cluster's side table in some rooms.
                LifeFurnishing special=plan.Vignette.Activity==RoomActivity.Music?LifeFurnishing.Piano:plan.Vignette.Activity==RoomActivity.Game?LifeFurnishing.GameTable:LifeFurnishing.Cabinet;
                var space=new SourceFurniturePlacement(4,7,Point(0,-1.3f,floor+.006f),turn*90);
                var bespoke=EncounterFurniture.Body(special);bespoke.TranslateRotate(space.Bottom,space.Yaw*Shapes.Pi/180);
                if(Fits(space)&&BespokeClear(bespoke,geometry))plan.Furniture.Add(new LifePlacement(special,-1,4,space.Bottom,space.Yaw));
                return plan;
            }
            plan.Vignette=new RoomVignette(plan.Vignette.Title,"Dejamos una invitación para la próxima visita. Puedes descansar aquí; hay lugares preparados un poco más adelante.",RoomActivity.Notice,plan.Vignette.Edition);
            return plan;
            bool Fits(SourceFurniturePlacement p)
            {
                var half=SourceFurniture.HalfExtents(p.Source,p.Yaw,.08f);
                float clear=Math.Max(c.DoorWidth*.5f+.45f,room.Plan?.ClearHalfWidth??0);
                if(Math.Abs(p.Bottom.X)-half.X<clear||Math.Abs(p.Bottom.Z)-half.Z<clear)return false;
                if(Math.Abs(p.Bottom.X)+half.X>room.Width*.5f-.3f||Math.Abs(p.Bottom.Z)+half.Z>room.Depth*.5f-.3f)return false;
                foreach(var water in geometry.Regions)foreach(int dx in new[]{-1,1})foreach(int dz in new[]{-1,1})if(water.Contains(p.Bottom.X+dx*half.X,p.Bottom.Z+dz*half.Z)&&water.Level>p.Bottom.Y+.01f)return false;
                if(!SourceFurniture.Clear(geometry.Structure,p,.05f)||!SourceFurniture.Clear(geometry.Decoration,p,.05f)||!SourceFurniture.Clear(geometry.Glass,p,.05f)||!SourceFurniture.Clear(geometry.CollisionOnly,p,.05f))return false;
                foreach(var prior in geometry.SourceProps)if(Overlap(p,prior))return false;
                foreach(var prior in geometry.SourceSeats)if(Overlap(p,prior))return false;
                var extra=room.Plan?.Restful?.Props??room.Plan?.Rain?.Props??room.Plan?.Aquatic?.Props??room.Plan?.Baths?.Props??room.Plan?.Garden?.Props??room.Plan?.Quiet?.Props;
                if(extra!=null)foreach(var prop in extra)
                {
                    if(prop.CatalogId>=0)
                    {
                        var size=MallItemCatalog.Get(prop.CatalogId).Size;
                        if(Math.Abs(p.Bottom.X-prop.Bottom.X)<half.X+Math.Max(size.X,size.Z)*.5f&&Math.Abs(p.Bottom.Z-prop.Bottom.Z)<half.Z+Math.Max(size.X,size.Z)*.5f&&prop.Bottom.Y<p.Bottom.Y+SourceFurniture.Dimensions(p.Source).Y&&prop.Bottom.Y+size.Y>p.Bottom.Y)return false;
                    }
                    else if(prop.Source!=null)
                    {for(int index=0;index<14;index++)if(SourceFurniture.Id(index)==prop.Source&&Overlap(p,new SourceFurniturePlacement(0,index,prop.Bottom,prop.Yaw)))return false;}
                }
                foreach(int dx in new[]{-1,0,1})foreach(int dz in new[]{-1,0,1})if(!RegionalSourcePlanner.Supported(geometry.Structure,new V3(p.Bottom.X+dx*half.X,p.Bottom.Y-.006f,p.Bottom.Z+dz*half.Z)))return false;
                return true;
            }
        }
        private static bool BespokeClear(MeshData furniture,RoomGeometry geometry)
        {
            float x0=float.MaxValue,y0=float.MaxValue,z0=float.MaxValue,x1=float.MinValue,y1=float.MinValue,z1=float.MinValue;
            foreach(var v in furniture.Positions){x0=Math.Min(x0,v.X);y0=Math.Min(y0,v.Y);z0=Math.Min(z0,v.Z);x1=Math.Max(x1,v.X);y1=Math.Max(y1,v.Y);z1=Math.Max(z1,v.Z);}
            foreach(var mesh in new[]{geometry.Structure,geometry.Decoration,geometry.Glass,geometry.CollisionOnly})
                foreach(var indices in mesh.Indices)for(int i=0;i<indices.Count;i+=3)
                {
                    var a=mesh.Positions[indices[i]];var b=mesh.Positions[indices[i+1]];var c=mesh.Positions[indices[i+2]];
                    if(Math.Max(a.X,Math.Max(b.X,c.X))<x0-.04f||Math.Min(a.X,Math.Min(b.X,c.X))>x1+.04f||
                       Math.Max(a.Y,Math.Max(b.Y,c.Y))<y0||Math.Min(a.Y,Math.Min(b.Y,c.Y))>y1+.04f||
                       Math.Max(a.Z,Math.Max(b.Z,c.Z))<z0-.04f||Math.Min(a.Z,Math.Min(b.Z,c.Z))>z1+.04f)continue;
                    return false;
                }
            return true;
        }
        private static bool Overlap(SourceFurniturePlacement a,SourceFurniturePlacement b)
        {var x=SourceFurniture.HalfExtents(a.Source,a.Yaw,.1f);var y=SourceFurniture.HalfExtents(b.Source,b.Yaw,.1f);return Math.Abs(a.Bottom.X-b.Bottom.X)<x.X+y.X&&Math.Abs(a.Bottom.Z-b.Bottom.Z)<x.Z+y.Z&&Math.Abs(a.Bottom.Y-b.Bottom.Y)<x.Y+y.Y;}
    }
    // Original joinery, chamfered edges, inset doors and separate moving parts.
    public static class EncounterFurniture
    {
        public static MeshData Body(LifeFurnishing kind)
        {
            var m=new MeshData{Theme=SurfaceTheme.EncounterFurniture};
            Box(m,0,.08f,0,.86f,.08f,.52f,Surface.Accent);Box(m,0,.12f,.23f,.86f,.73f,.05f,Surface.Accent);
            foreach(int sign in new[]{-1,1})Box(m,sign*.41f,.12f,0,.055f,.73f,.52f,Surface.Accent);
            Box(m,0,.44f,0,.8f,.035f,.48f,Surface.Accent);Chamfer(m,0,0,.92f,.6f,.85f,.9f,.012f,Surface.Accent);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(m,x*.33f,0,z*.18f,.06f,.12f,.06f,Surface.Metal);
            if(kind==LifeFurnishing.Piano)
            {
                Box(m,0,.91f,.1f,.82f,.08f,.31f,Surface.Accent);
                Box(m,0,.9f,-.15f,.82f,.012f,.24f,Surface.Metal);
                // Music stand and brass hinge, at a human-readable height.
                Box(m,0,1.0f,.19f,.58f,.2f,.025f,Surface.Accent);
            }
            if(kind==LifeFurnishing.GameTable)
            {
                for(int x=0;x<4;x++)for(int z=0;z<4;z++)Chamfer(m,-.27f+x*.18f,-.18f+z*.12f,.16f,.1f,.9f,.915f,.003f,(x+z)%2==0?Surface.Plaster:Surface.Slide);
            }
            return m;
        }
        public static MeshData PianoKey(int i)
        {
            if(i<0||i>=8)throw new ArgumentOutOfRangeException(nameof(i));
            var m=new MeshData{Theme=SurfaceTheme.EncounterFurniture};
            Chamfer(m,0,0,.092f,.23f,0,.035f,.003f,Surface.Plaster);
            if(i!=2&&i!=6)Box(m,.048f,.032f,.07f,.04f,.026f,.12f,Surface.Metal);return m;
        }
        public static MeshData Door()
        {
            var m=new MeshData{Theme=SurfaceTheme.EncounterFurniture};Chamfer(m,.39f,0,.78f,.035f,0,.66f,.008f,Surface.Accent);
            Box(m,.67f,.28f,-.033f,.035f,.11f,.05f,Surface.Metal);
            foreach(int side in new[]{-1,1})Box(m,.02f,.31f+side*.2f,.02f,.025f,.08f,.035f,Surface.Metal);return m;
        }
        public static MeshData Notice()
        {
            var m=new MeshData{Theme=SurfaceTheme.EncounterFurniture,EmissionStrength=0};
            Chamfer(m,0,0,.72f,.035f,-.27f,.27f,.008f,Surface.Accent);
            Box(m,0,-.22f,-.022f,.61f,.44f,.008f,Surface.Slide);
            MallGlyphs.Write(m,"BIENVENIDA",new V3(-.28f,.12f,-.03f),.009f);
            for(int i=0;i<5;i++)Box(m,-.04f,-.02f-i*.038f,-.031f,.38f-i*.02f,.008f,.003f,Surface.Metal);
            return m;
        }
        private static void Box(MeshData m,float x,float bottom,float z,float w,float height,float depth,Surface surface)
        {m.Box(new V3(x-w*.5f,bottom,z-depth*.5f),new V3(x+w*.5f,bottom+height,z+depth*.5f),surface,.7f);}
        public static void Chamfer(MeshData m,float x,float z,float width,float depth,float bottom,float top,float bevel,Surface surface)
        {
            float w=width*.5f,d=depth*.5f;bevel=Math.Min(bevel,Math.Min(w,Math.Min(d,(top-bottom)*.4f)));
            var ring=Shapes.RoundedRect(width,depth,bevel,1);
            for(int i=0;i<ring.Length;i++)
            {
                var a=ring[i];var b=ring[(i+1)%ring.Length];var normal=new V3(b.Y-a.Y,0,a.X-b.X).Unit;
                var loA=new V3(x+a.X,bottom,z+a.Y);var loB=new V3(x+b.X,bottom,z+b.Y);
                var midA=new V3(x+a.X,top-bevel,z+a.Y);var midB=new V3(x+b.X,top-bevel,z+b.Y);
                var hiA=new V3(x+a.X*(w-bevel)/w,top,z+a.Y*(d-bevel)/d);var hiB=new V3(x+b.X*(w-bevel)/w,top,z+b.Y*(d-bevel)/d);
                m.Quad(loA,loB,midB,midA,normal,surface,.7f);m.Quad(midA,midB,hiB,hiA,(normal+V3.Up).Unit,surface,.7f);
                m.Triangle(new V3(x,top,z),hiA,hiB,V3.Up,V3.Up,V3.Up,new V2(),new V2(1,0),new V2(0,1),surface);
                m.Triangle(new V3(x,bottom,z),loB,loA,V3.Up*-1,V3.Up*-1,V3.Up*-1,new V2(),new V2(0,1),new V2(1,0),surface);
            }
        }
    }
}
