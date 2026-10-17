using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public readonly struct TerrainHouseItem
    {
        public readonly int Slot,CatalogId;
        public readonly V3 Position;
        public TerrainHouseItem(int slot,int id,V3 position){Slot=slot;CatalogId=id;Position=position;}
    }
    public sealed class TerrainHouseGeometry
    {
        public readonly RoomGeometry Geometry=new RoomGeometry();
        public readonly RoomGeometry Envelope=new RoomGeometry(),Interior=new RoomGeometry();
        public readonly MeshData Roof=new MeshData(),WallFinish=new MeshData();
        public readonly List<TerrainHouseItem> Items=new List<TerrainHouseItem>();
    }
    public static class TerrainHouseArchitecture
    {
        public static TerrainHouseGeometry Build(TerrainHousePlan house,bool detailed,TerrainRegionPlan region=null)
        {
            if(house==null)throw new ArgumentNullException(nameof(house));
            if(house.Blueprint!=null)return TerrainVariedHouseArchitecture.Build(house,detailed,region);
            var output=new TerrainHouseGeometry();var g=output.Geometry;float w=house.Width,d=house.Depth,h=TerrainHousePlan.FloorHeight;
            g.Glass.Theme=SurfaceTheme.Glass;
            Box(g.Structure,0,0,w,d,-.85f,0,Surface.Tile);
            for(int floor=0;floor<house.Floors;floor++)
            {
                float y=floor*h;
                if(floor>0)
                {
                    // Floor leaves an entire stair well open, plus a full-width rear landing.
                    Box(g.Structure,-1.2f,0,w-2.4f,d,y-.18f,y,Surface.Tile);
                    Box(g.Structure,w*.5f-1.2f,d*.5f-1,2.4f,2,y-.18f,y,Surface.Tile);
                }
                Wall(g,0,-d*.5f,w,y,y+h,floor==0,true);
                Wall(g,0,d*.5f,w,y,y+h,false,false);
                Side(g,-w*.5f,d,y,y+h);Side(g,w*.5f,d,y,y+h);
                if(detailed){Interior(output,house,floor);TerrainHouseDetails.Floor(output,house,floor);}
                else if(floor==0)Box(g.Structure,0,0,w-.4f,d-.4f,h-.18f,h,Surface.Tile); // Removed for 2-floor stair detail when promoted.
            }
            float eave=house.Floors*h,peak=eave+2.1f;
            // Closed gable roof with physical underside; no invisible box blocks the upstairs.
            output.Roof.Quad(new V3(-w*.5f-.35f,eave,-d*.5f-.4f),new V3(0,peak,-d*.5f-.4f),new V3(0,peak,d*.5f+.4f),new V3(-w*.5f-.35f,eave,d*.5f+.4f),new V3(-.35f,1,0).Unit,Surface.Accent,1.2f);
            output.Roof.Quad(new V3(0,peak,-d*.5f-.4f),new V3(w*.5f+.35f,eave,-d*.5f-.4f),new V3(w*.5f+.35f,eave,d*.5f+.4f),new V3(0,peak,d*.5f+.4f),new V3(.35f,1,0).Unit,Surface.Accent,1.2f);
            // Shingle rows follow the eave, while their overlap runs up the slope.
            for(int i=0;i<output.Roof.UV.Count;i++){var uv=output.Roof.UV[i];output.Roof.UV[i]=new V2(uv.Y,uv.X);}
            // Lit roofs are single-sided: give them a physical, visible underside as well.
            output.Roof.Quad(new V3(-w*.5f-.35f,eave-.12f,-d*.5f-.4f),new V3(0,peak-.12f,-d*.5f-.4f),new V3(0,peak-.12f,d*.5f+.4f),new V3(-w*.5f-.35f,eave-.12f,d*.5f+.4f),new V3(.35f,-1,0).Unit,Surface.Tile,1.2f);
            output.Roof.Quad(new V3(0,peak-.12f,-d*.5f-.4f),new V3(w*.5f+.35f,eave-.12f,-d*.5f-.4f),new V3(w*.5f+.35f,eave-.12f,d*.5f+.4f),new V3(0,peak-.12f,d*.5f+.4f),new V3(-.35f,-1,0).Unit,Surface.Tile,1.2f);
            foreach(int side in new[]{-1,1})
            {
                float z=side*d*.5f;g.Structure.Triangle(new V3(-w*.5f,eave,z),new V3(w*.5f,eave,z),new V3(0,peak,z),new V3(0,0,side),new V3(0,0,side),new V3(0,0,side),new V2(0,0),new V2(w,0),new V2(w*.5f,2.1f),Surface.Plaster);
                float inside=z-side*.1f;V3 inward=new V3(0,0,-side);
                g.Structure.Triangle(new V3(-w*.5f,eave-.12f,inside),new V3(w*.5f,eave-.12f,inside),new V3(0,peak-.12f,inside),inward,inward,inward,new V2(0,0),new V2(w,0),new V2(w*.5f,2.1f),Surface.Plaster);
                Box(g.Decoration,0,z,w+.22f,.12f,eave-.12f,eave+.06f,Surface.Metal);
            }
            // Low porch and two broad steps, below the CharacterController step height.
            Box(g.Structure,0,-d*.5f-.65f,3.2f,1.3f,-.2f,0,Surface.Tile);
            Box(g.Structure,0,-d*.5f-1.55f,3.2f,.5f,-.35f,-.18f,Surface.Tile);
            if(detailed)
            {
                for(int i=0;i<8;i++)
                {
                    float x=-w*.5f-3+i*(w+6)/7f;
                    if(Math.Abs(x)<1.7f)continue;
                    Box(g.Structure,x,-d*.5f-4,.07f,.09f,-.2f,1.05f,Surface.Metal);
                }
                foreach(int side in new[]{-1,1})
                {Box(g.Structure,side*(w*.25f+1),-d*.5f-4,w*.5f-1,.045f,.3f,.4f,Surface.Metal);Box(g.Structure,side*(w*.25f+1),-d*.5f-4,w*.5f-1,.045f,.77f,.87f,Surface.Metal);}
                Box(g.Decoration,-w*.5f+1,-d*.5f-.2f,.4f,.14f,1.15f,1.55f,Surface.Metal); // Mailbox.
                Box(g.Structure,w*.5f-1,d*.5f-1,.9f,.9f,eave,eave+1.4f,Surface.Plaster);
            }
            if(region!=null&&StableRandom.Hash(house.Seed,0,0,0x94F0u)%23==0)
            {
                float x=w*.5f+16,z=18,baseY=region.Height(house.X+x,house.Z+z)-house.Base;
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box(g.Structure,x+sx*1.65f,z+sz*1.65f,.22f,.22f,baseY,baseY+12,Surface.Metal);
                Shapes.Column(g.Structure,x,z,2.25f,baseY+11.8f,baseY+15,12,Surface.Metal,1.2f);
                Box(g.Decoration,x,z,4.5f,4.5f,baseY+11.4f,baseY+11.55f,Surface.Metal);
            }
            if(detailed)TerrainYardArchitecture.Append(g,region,house);
            if(detailed)TerrainHouseFinish.Append(g.Decoration,house);
            if(detailed)TerrainHouseFinish.SplitWalls(g.Structure,output.Envelope.Structure,output.WallFinish,house);
            else output.Envelope.Structure.Append(g.Structure);
            output.Envelope.Decoration.Append(g.Decoration);
            // Aggregate collision geometry is still authoritative, with all furniture and the roof.
            g.Structure.Append(output.Roof);g.Structure.Append(output.Interior.Structure);
            g.Decoration.Append(output.Interior.Decoration);g.Fixtures.AddRange(output.Interior.Fixtures);
            return output;
        }
        private static void Wall(RoomGeometry g,float x,float z,float width,float floor,float top,bool door,bool front)
        {
            // Three open bays: central doorway on ground front, glazed windows on other bays.
            float[] centres={-width*.29f,0,width*.29f};float win=1.5f,last=-width*.5f;
            foreach(float c in centres)
            {
                float left=c-win*.5f,right=c+win*.5f;
                Box(g.Structure,(last+left)*.5f,z,left-last,.24f,floor,top,Surface.Plaster);
                bool opening=door&&Math.Abs(c)<.01f;float sill=opening?0:.9f,lintel=opening?2.5f:2.22f;
                if(sill>0)Box(g.Structure,c,z,win,.24f,floor,floor+sill,Surface.Plaster);
                Box(g.Structure,c,z,win,.24f,floor+lintel,top,Surface.Plaster);
                if(!opening)Window(g,c,z,win,floor+sill,floor+lintel,false);
                else foreach(int side in new[]{-1,1})Box(g.Decoration,c+side*(win*.5f+.035f),z,.07f,.29f,floor,floor+lintel+.04f,Surface.Metal);
                last=right;
            }
            Box(g.Structure,(last+width*.5f)*.5f,z,width*.5f-last,.24f,floor,top,Surface.Plaster);
        }
        private static void Side(RoomGeometry g,float x,float depth,float floor,float top)
        {
            float last=-depth*.5f;
            foreach(float c in new[]{-depth*.28f,depth*.28f})
            {
                float left=c-.8f,right=c+.8f;
                Box(g.Structure,x,(last+left)*.5f,.24f,left-last,floor,top,Surface.Plaster);
                Box(g.Structure,x,c,.24f,1.6f,floor,floor+.9f,Surface.Plaster);
                Box(g.Structure,x,c,.24f,1.6f,floor+2.22f,top,Surface.Plaster);
                Window(g,x,c,1.6f,floor+.9f,floor+2.22f,true);last=right;
            }
            Box(g.Structure,x,(last+depth*.5f)*.5f,.24f,depth*.5f-last,floor,top,Surface.Plaster);
        }
        private static void Window(RoomGeometry g,float x,float z,float width,float low,float high,bool side)
        {
            Box(g.Glass,x,z,side?.018f:width-.08f,side?width-.08f:.018f,low+.03f,high-.03f,Surface.Glow);
            foreach(int k in new[]{-1,0,1})Box(g.Decoration,side?x:x+k*width*.5f,side?z+k*width*.5f:z,side?.28f:.065f,side?.065f:.28f,low,high,Surface.Metal);
            foreach(float y in new[]{low,high,(low+high)*.5f})Box(g.Decoration,x,z,side?.28f:width+.1f,side?width+.1f:.28f,y-.025f,y+.025f,Surface.Metal);
        }
        private static void Interior(TerrainHouseGeometry output,TerrainHousePlan house,int floor)
        {
            var g=output.Interior;float w=house.Width,d=house.Depth,y=floor*TerrainHousePlan.FloorHeight,top=y+TerrainHousePlan.FloorHeight-.18f;
            // Central hall and cross hall remain clear; every room has a 1.5 m passage.
            Box(g.Structure,-w*.25f-.45f,0,w*.5f-.9f,.14f,y,top,Surface.Plaster);
            float rightEnd=house.Floors>1?w*.5f-2.45f:w*.5f;
            Box(g.Structure,(rightEnd+.9f)*.5f,0,rightEnd-.9f,.14f,y,top,Surface.Plaster);
            if(floor==0)
            {
                float left=-w*.27f,right=w*.19f;
                // Living room: sofa, coffee table and low media console.
                Sofa(g,left,-d*.5f+1.15f,y,house.Variant);
                Table(g,left,-d*.24f,1.25f,.72f,y,.5f);
                Box(g.Structure,-w*.5f+.5f,-d*.24f,.7f,2,y,y+.55f,Surface.Accent);
                Box(g.Decoration,-w*.5f+.5f,-d*.24f,.08f,1.15f,y+.55f,y+1.25f,Surface.Metal);
                Item(output,house,floor,0,MallItemShape.Book,new V3(left-.22f,y+.5f,-d*.24f));
                Item(output,house,floor,1,MallItemShape.Mug,new V3(left+.28f,y+.5f,-d*.24f));
                // Kitchen at rear-left: cabinets, countertop, sink, hob and a fridge.
                Box(g.Structure,-w*.5f+.7f,d*.27f,1.05f,3.1f,y,y+.88f,Surface.Metal);
                Box(g.Structure,-w*.5f+.7f,d*.27f,1.12f,3.15f,y+.88f,y+.94f,Surface.Accent);
                Box(g.Decoration,-w*.5f+.7f,d*.27f,.7f,.65f,y+.94f,y+.955f,Surface.Metal);
                for(int k=0;k<3;k++)Box(g.Decoration,-w*.5f+1.24f,d*.27f-.95f+k*.9f,.035f,.14f,y+.53f,y+.57f,Surface.Metal);
                Box(g.Structure,-w*.5f+.75f,d*.5f-1.1f,1.1f,1.1f,y,y+2,Surface.Metal);
                Table(g,left,d*.23f,1.4f,.85f,y,.76f);
                foreach(int side in new[]{-1,1})Chair(g,left+side*1.05f,d*.23f,y);
                Item(output,house,floor,2,MallItemShape.Plate,new V3(left,y+.76f,d*.23f));
                Item(output,house,floor,3,MallItemShape.Bottle,new V3(left+.42f,y+.76f,d*.23f));
                // Bathroom fixtures fit between the clear central hall and the stair strip.
                float bathX=w*.18f,bathZ=-d*.5f+1.25f;
                Box(g.Structure,bathX,bathZ,.9f,.6f,y,y+.84f,Surface.Metal);
                Box(g.Structure,bathX,bathZ,.95f,.64f,y+.84f,y+.9f,Surface.Accent);
                Box(g.Decoration,bathX,bathZ+.18f,.35f,.04f,y+.9f,y+1.15f,Surface.Metal);
                Shapes.Column(g.Structure,bathX,-d*.22f,.26f,y,y+.42f,12,Surface.Metal,1.2f);
                Box(g.Structure,bathX,-d*.22f+.23f,.52f,.18f,y+.42f,y+.8f,Surface.Metal);
                Item(output,house,floor,9,MallItemShape.Soap,new V3(bathX-.17f,y+.9f,bathZ));
                Item(output,house,floor,10,MallItemShape.Towel,new V3(bathX+.2f,y+.9f,bathZ));
                if(house.Floors==1)Bedroom(output,house,floor,w*.3f,d*.25f,y);
                else
                {
                    // Broad straight flight; stair opening and rear landing match the floor mesh.
                    float start=-d*.5f+1.2f,end=d*.5f-2,run=end-start;const int steps=18;
                    float stairX=w*.5f-1.3f;
                    for(int i=0;i<steps;i++)Box(g.Structure,stairX,start+(i+.5f)*run/steps,2,run/steps,y,y+(i+1)*TerrainHousePlan.FloorHeight/steps,Surface.Accent);
                    for(int i=0;i<=steps;i+=3)Box(g.Structure,stairX-1.07f,start+i*run/steps,.06f,.06f,y+i*TerrainHousePlan.FloorHeight/steps,y+i*TerrainHousePlan.FloorHeight/steps+1,Surface.Metal);
                    // Handrail follows the slope and does not obstruct the traversable flight.
                    var rail=new MeshData();Box(rail,0,0,.065f,run+.2f,0,.065f,Surface.Metal);
                    float rise=TerrainHousePlan.FloorHeight,angle=(float)Math.Atan2(rise,run),c=(float)Math.Cos(angle),s=(float)Math.Sin(angle);
                    for(int j=0;j<rail.Positions.Count;j++){var p=rail.Positions[j];rail.Positions[j]=new V3(p.X,p.Y*c+p.Z*s,p.Z*c-p.Y*s);p=rail.Normals[j];rail.Normals[j]=new V3(p.X,p.Y*c+p.Z*s,p.Z*c-p.Y*s);}
                    rail.TranslateRotate(new V3(stairX-1.07f,y+rise*.5f+1,(start+end)*.5f),0);g.Structure.Append(rail);
                    // Ground floor study between the hall and stairs, without blocking the stair route.
                    Table(g,right,d*.3f,1.05f,.65f,y,.74f);Chair(g,right,d*.3f-.8f,y);
                    Item(output,house,floor,4,MallItemShape.Radio,new V3(right,y+.74f,d*.3f));
                }
            }
            else
            {
                Bedroom(output,house,floor,-w*.28f,-d*.26f,y);
                Table(g,-w*.28f,d*.26f,1.4f,.7f,y,.74f);Chair(g,-w*.28f,d*.26f-.8f,y);
                Item(output,house,floor,5,MallItemShape.Notebook,new V3(-w*.28f,y+.74f,d*.26f));
                Item(output,house,floor,6,MallItemShape.Phone,new V3(-w*.28f+.4f,y+.74f,d*.26f));
                // Upper railing stays left of the stair well, with rear landing left open.
                Box(g.Structure,w*.5f-2.43f,-1,.06f,d-4,y+.75f,y+.82f,Surface.Metal);
                for(int i=0;i<4;i++)Box(g.Structure,w*.5f-2.43f,-d*.5f+1+i*(d-4)/3,.06f,.06f,y,y+.82f,Surface.Metal);
            }
            foreach(float z in new[]{-d*.22f,d*.23f})Shapes.Column(g.Decoration,-w*.23f,z,.2f,y+2.9f,y+2.96f,12,Surface.Metal,1.2f);
            g.Fixtures.Add(new Fixture{Position=new V3(-w*.23f,y+2.94f,-d*.22f),Width=.3f,Depth=.3f,Power=800,Temperature=4100,Range=9});
            g.Fixtures.Add(new Fixture{Position=new V3(-w*.23f,y+2.94f,d*.23f),Width=.3f,Depth=.3f,Power=900,Temperature=4300,Range=9});
        }
        private static void Bedroom(TerrainHouseGeometry output,TerrainHousePlan house,int floor,float x,float z,float y)
        {
            var g=output.Interior;
            Box(g.Structure,x,z,1.9f,2.7f,y,y+.3f,Surface.Accent);Box(g.Structure,x,z,1.8f,2.6f,y+.3f,y+.58f,Surface.Slide);
            Box(g.Structure,x,z+1.3f,1.9f,.12f,y,y+1.05f,Surface.Accent);
            Box(g.Decoration,x,z+.9f,1.15f,.45f,y+.58f,y+.7f,Surface.Metal);
            Table(g,x-1.5f,z+.85f,.7f,.7f,y,.56f);
            Item(output,house,floor,7,MallItemShape.Book,new V3(x-1.5f,y+.56f,z+.85f));
            Item(output,house,floor,8,MallItemShape.Watch,new V3(x-1.5f+.2f,y+.56f,z+.64f));
            Box(g.Structure,Math.Min(house.Width*.5f-.7f,x+1.65f),z+.4f,.8f,1.7f,y,y+1.9f,Surface.Accent);
        }
        private static void Item(TerrainHouseGeometry output,TerrainHousePlan h,int floor,int slot,MallItemShape shape,V3 point)
        {
            int id=(int)shape*8+(int)(StableRandom.Hash(h.Seed,floor,slot,0x940C0u)%8);var def=MallItemCatalog.Get(id);
            point.Y+=def.Size.Y*.5f+.012f;output.Items.Add(new TerrainHouseItem(floor*16+slot,id,point));
        }
        private static void Sofa(RoomGeometry g,float x,float z,float y,int variant)
        {
            Box(g.Structure,x,z,2.25f,.92f,y+.12f,y+.48f,Surface.Slide);
            Box(g.Structure,x,z-.4f,2.25f,.16f,y+.22f,y+1.02f,Surface.Slide);
            foreach(int side in new[]{-1,1})Box(g.Structure,x+side*1.06f,z,.14f,.92f,y+.15f,y+.75f,Surface.Slide);
            for(int i=0;i<3;i++)Box(g.Decoration,x-.7f+i*.7f,z,.67f,.76f,y+.48f,y+.55f,Surface.Slide);
        }
        private static void Table(RoomGeometry g,float x,float z,float w,float d,float y,float height)
        {
            Box(g.Structure,x,z,w,d,y+height-.06f,y+height,Surface.Accent);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box(g.Structure,x+sx*(w*.5f-.09f),z+sz*(d*.5f-.09f),.07f,.07f,y,y+height-.06f,Surface.Accent);
        }
        private static void Chair(RoomGeometry g,float x,float z,float y)
        {
            Box(g.Structure,x,z,.5f,.5f,y+.42f,y+.48f,Surface.Slide);Box(g.Structure,x,z+.22f,.5f,.06f,y+.42f,y+.95f,Surface.Accent);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box(g.Structure,x+sx*.19f,z+sz*.19f,.055f,.055f,y,y+.42f,Surface.Accent);
        }
        private static void Box(MeshData m,float x,float z,float w,float d,float y0,float y1,Surface s)
        {if(w>0&&d>0&&y1>y0)m.Box(new V3(x-w*.5f,y0,z-d*.5f),new V3(x+w*.5f,y1,z+d*.5f),s,1.2f);}
    }
}
