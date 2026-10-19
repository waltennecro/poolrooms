using System;

namespace Poolrooms.Core
{
    public static class InteriorArchitecture
    {
        public static RoomGeometry Build(Room room,LayoutConfig c)
        {
            if(room.Plan==null||room.Plan.Interior==null||!room.Plan.Valid(room,c))throw new ArgumentException("Invalid interior plan.");
            var output=new RoomGeometry();output.Structure.RetailPalette=1;output.Decoration.RetailPalette=1;
            output.Structure.PaletteVariant=room.Plan.Palette;output.Decoration.PaletteVariant=room.Plan.Palette;
            ProceduralArchitecture.DryEnvelope(output,room,c);
            WallFinish(output.Structure);
            Ceiling(output,room,c);FloorFinish(output,room,c);
            foreach(InteriorBay bay in room.Plan.Interior.Bays)
            {
                var solid=new MeshData();var detail=new MeshData();float uv=c.TextureWorldSize;
                if(room.Environment==LiminalEnvironment.Hotel||room.Environment==LiminalEnvironment.Office&&(bay.Variant==1||bay.Variant==3)||
                    c.AutonomousGeneration&&room.Plan.Interior.Galleries.Length>0&&bay.Variant==1)
                    Partition(solid,detail,bay,room.Height,uv,room.Environment==LiminalEnvironment.Hotel?"HAB "+(100+(bay.Seed%800)):
                        room.Environment==LiminalEnvironment.Office?"OFICINA":room.Environment==LiminalEnvironment.WaitingHall?"SALA":"COMEDOR");
                if(c.AutonomousGeneration)AutonomousArchitecture.Interior(solid,detail,bay,room.Environment,c);
                else switch(room.Environment)
                {
                    case LiminalEnvironment.Office:Office(solid,detail,bay,c);break;
                    case LiminalEnvironment.Hotel:Hotel(solid,detail,bay,c);break;
                    case LiminalEnvironment.WaitingHall:Waiting(solid,detail,bay,c);break;
                    case LiminalEnvironment.DiningHall:Dining(solid,detail,bay,c);break;
                }
                V3 offset=new V3(bay.X,0,bay.Z);float angle=bay.QuarterTurns*Shapes.Pi*.5f;
                solid.TranslateRotate(offset,angle);detail.TranslateRotate(offset,angle);output.Structure.Append(solid);output.Decoration.Append(detail);
                if(room.Environment==LiminalEnvironment.Hotel)
                {
                    float x=-bay.LocalWidth*.5f+1.1f,z=bay.LocalDepth*.5f-1.5f;
                    output.Fixtures.Add(new Fixture{Position=bay.World(x,1.6f,z),Width=.38f,Depth=.38f,Power=120,Temperature=2900});
                }
            }
            return output;
        }
        private static void Box(MeshData mesh,float x,float z,float width,float depth,float bottom,float top,Surface material,float uv)
        {ZoneShapes.Box(mesh,x,z,width,depth,bottom,top,material,uv);}
        private static void WallFinish(MeshData mesh)
        {
            // The shared envelope uses ceramic everywhere. Keep its geometry and collision,
            // but give dry interiors plaster walls rather than carpet-colored pool tiles.
            var tile=mesh.Indices[(int)Surface.Tile];int kept=0;
            for(int i=0;i<tile.Count;i+=3)
            {
                bool wall=false;for(int k=0;k<3;k++)if(mesh.Positions[tile[i+k]].Y>.01f)wall=true;
                if(wall)for(int k=0;k<3;k++)mesh.Indices[(int)Surface.Plaster].Add(tile[i+k]);
                else {tile[kept++]=tile[i];tile[kept++]=tile[i+1];tile[kept++]=tile[i+2];}
            }
            if(kept<tile.Count)tile.RemoveRange(kept,tile.Count-kept);
        }
        private static void Partition(MeshData solid,MeshData detail,InteriorBay bay,float height,float uv,string label)
        {
            float w=bay.LocalWidth,d=bay.LocalDepth,t=.16f;
            // Single open doorway faces a protected gallery. Side and back walls enclose the room.
            ZoneShapes.Portal(solid,0,-d*.5f,0,w,height,0,2.4f,3.05f,t,3,12,uv);
            WallFinish(solid);
            for(int side=-1;side<=1;side+=2)Box(solid,side*(w*.5f-t*.5f),0,t,d,0,height,Surface.Plaster,uv);
            Box(solid,0,d*.5f-t*.5f,w,t,0,height,Surface.Plaster,uv);
            for(int side=-1;side<=1;side+=2)
            {
                Box(detail,side*1.25f,-d*.5f-.025f,.07f,.03f,0,3.12f,Surface.Accent,uv);
                Box(detail,side*(w*.5f-.18f),0,.025f,d-.2f,.03f,.15f,Surface.Accent,uv);
            }
            Box(detail,0,-d*.5f-.025f,2.6f,.03f,3.05f,3.13f,Surface.Accent,uv);
            Lettering(detail,label,new V3(-label.Length*.045f,3.3f,-d*.5f-.091f),.015f);
        }
        private static void Ceiling(RoomGeometry o,Room room,LayoutConfig c)
        {
            float uv=c.TextureWorldSize,h=room.Height;
            bool hotel=room.Environment==LiminalEnvironment.Hotel;int nx=hotel?2:3,nz=3;
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
            {
                float px=(x+.5f)*room.Width/nx-room.Width*.5f,pz=(z+.5f)*room.Depth/nz-room.Depth*.5f;
                // Hotel ceiling lights stay in galleries; lamps inside each bedroom are separate.
                if(hotel){px=0;pz=(z-1)*room.Depth*.32f;if(x>0)continue;}
                float w=hotel?.45f:1.1f,d=hotel?.45f:.48f;
                Box(o.Structure,px,pz,w+.12f,d+.12f,h-.14f,h-.06f,Surface.Metal,uv);
                Box(o.Decoration,px,pz,w,d,h-.17f,h-.145f,Surface.Glow,uv);
                o.Fixtures.Add(new Fixture{Position=new V3(px,h-.21f,pz),Width=w,Depth=d,Power=hotel?600:950,Temperature=hotel?3100:4400});
            }
            // Acoustic ceiling tiles and static grilles, batched into the decoration mesh.
            if(!hotel)
            {
                for(float x=-room.Width*.5f+1.2f;x<room.Width*.5f;x+=2.4f)Box(o.Decoration,x,0,.035f,room.Depth,h-.027f,h-.007f,Surface.Metal,uv);
                for(float z=-room.Depth*.5f+1.2f;z<room.Depth*.5f;z+=2.4f)Box(o.Decoration,0,z,room.Width,.035f,h-.027f,h-.007f,Surface.Metal,uv);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++)Box(o.Decoration,side*room.Width*.31f+(i-2.5f)*.09f,0,.035f,.6f,h-.05f,h-.03f,Surface.Metal,uv);
            }
        }
        private static void FloorFinish(RoomGeometry o,Room room,LayoutConfig c)
        {
            float uv=c.TextureWorldSize;
            if(room.Environment==LiminalEnvironment.Hotel)
            {
                // Dark wainscot stays on perimeter walls, leaving every connector open.
                for(int side=0;side<4;side++)
                {
                    float width=side%2==0?room.Width:room.Depth,half=c.DoorWidth*.5f+.15f;
                    var trim=new MeshData();float segment=(width*.5f-half);
                    for(int sign=-1;sign<=1;sign+=2)Box(trim,sign*(half+segment*.5f),-.03f,segment,.04f,.05f,1.05f,Surface.Slide,uv);
                    trim.TranslateRotate(new V3(Directions.DX[side]*room.Width*.5f,0,Directions.DZ[side]*room.Depth*.5f),side*Shapes.Pi*.5f);o.Decoration.Append(trim);
                }
                // Finite geometric carpet motif, no bespoke shader or extra floor colliders.
                for(float x=-room.Width*.5f+1;x<room.Width*.5f-1;x+=1.5f)for(float z=-room.Depth*.5f+1;z<room.Depth*.5f-1;z+=1.5f)
                {
                    float size=.18f;var diamond=new MeshData();
                    diamond.Quad(new V3(x-size,.004f,z),new V3(x,.004f,z+size),new V3(x+size,.004f,z),new V3(x,.004f,z-size),V3.Up,Surface.Accent,uv);o.Decoration.Append(diamond);
                }
            }
            else if(room.Environment==LiminalEnvironment.DiningHall)
            {
                // Wide tile joints support the old diner appearance without a renderer per tile.
                for(float x=-room.Width*.5f+.8f;x<room.Width*.5f;x+=1.6f)Box(o.Decoration,x,0,.014f,room.Depth,.002f,.004f,Surface.Accent,uv);
                for(float z=-room.Depth*.5f+.8f;z<room.Depth*.5f;z+=1.6f)Box(o.Decoration,0,z,room.Width,.014f,.002f,.004f,Surface.Accent,uv);
            }
        }
        private static void Office(MeshData solid,MeshData detail,InteriorBay b,LayoutConfig c)
        {
            float w=b.LocalWidth,d=b.LocalDepth,uv=c.TextureWorldSize;
            int rows=Math.Max(1,Math.Min(c.CurveSegments<=12?2:3,(int)((d-1.8f)/2.6f)));
            if(b.Variant==2)
            {
                // Conference space at the back, with an unobstructed doorway landing.
                float tw=Math.Min(w-2.8f,4),td=Math.Min(d-3,2.5f),z=d*.5f-td*.5f-.8f;
                Table(solid,0,z,tw,td,uv);
                for(int side=-1;side<=1;side+=2)Chair(solid,side*(tw*.5f+.42f),z,side<0?3:1,uv);
                Box(solid,w*.5f-.34f,0,.38f,Math.Min(2.6f,d-2),0,1.4f,Surface.Accent,uv);
                Board(solid,detail,0,d*.5f-.24f,Math.Min(3,w-1),uv);
                return;
            }
            for(int side=-1;side<=1;side+=2)for(int row=0;row<rows;row++)
            {
                float x=side*(w*.5f-1.1f),z=-d*.5f+1.8f+row*2.6f;
                Table(solid,x,z,1.4f,.85f,uv);Chair(solid,x,z-.82f,2,uv);
                Box(detail,x,z+.1f,.45f,.09f,.84f,1.2f,Surface.Slide,uv);
                Box(detail,x,z,.35f,.2f,.79f,.81f,Surface.Plaster,uv);
                Box(detail,x+.43f,z-.08f,.18f,.24f,.78f,.83f,Surface.Plaster,uv);
                if(b.Variant==0)Box(solid,side*(w*.5f-.24f),z,.12f,2.2f,0,1.35f,Surface.Slide,uv);
            }
            Box(solid,0,d*.5f-.36f,Math.Min(2.8f,w-2),.45f,0,1.25f,Surface.Accent,uv);
            for(int i=-1;i<=1;i++)Box(detail,i*.65f,d*.5f-.59f,.48f,.025f,.9f,1.15f,Surface.Plaster,uv);
        }
        private static void Hotel(MeshData solid,MeshData detail,InteriorBay b,LayoutConfig c)
        {
            float w=b.LocalWidth,d=b.LocalDepth,uv=c.TextureWorldSize,z=d*.5f-1.65f;
            Box(solid,0,z,1.9f,2.3f,0,.38f,Surface.Accent,uv);
            Box(solid,0,z,1.88f,2.25f,.38f,.56f,Surface.Plaster,uv);
            Box(detail,0,z-.28f,1.87f,1.5f,.56f,.59f,Surface.Slide,uv);
            for(int side=-1;side<=1;side+=2)Box(detail,side*.48f,z+.65f,.68f,.4f,.56f,.7f,Surface.Plaster,uv);
            Box(solid,0,z+1.15f,2.25f,.15f,0,1.15f,Surface.Accent,uv);
            float tx=-w*.5f+1.1f;Table(solid,tx,z,.65f,.65f,uv);
            Shapes.Column(detail,tx,z,.11f,.8f,1.14f,8,Surface.Accent,uv);
            Shapes.Column(detail,tx,z,.24f,1.14f,1.48f,8,Surface.Glow,uv);
            Box(solid,w*.5f-.48f,d*.5f-1.1f,.6f,1.25f,0,2.05f,Surface.Accent,uv);
            for(int i=-1;i<=1;i++)Box(detail,w*.5f-.8f,d*.5f-1.1f+i*.32f,.035f,.02f,1.05f,1.22f,Surface.Metal,uv);
            float x=-w*.5f+.55f,front=-d*.5f+1.25f;
            if(b.Variant==0){Box(solid,x,front,.65f,1,0,.85f,Surface.Plaster,uv);Box(detail,x,front,.68f,1.03f,.85f,.88f,Surface.Metal,uv);}
            else {Chair(solid,x+.3f,front,3,uv);Table(solid,w*.5f-.65f,front,.65f,.7f,uv);}
            // Blank reflective panel and abstract wall frame; no portraits, faces or figures.
            Box(detail,w*.5f-.09f,0,.015f,1.05f,1.1f,1.9f,Surface.Metal,uv);
            Box(detail,0,d*.5f-.09f,1.2f,.025f,1.7f,2.5f,Surface.Accent,uv);
            Box(detail,0,d*.5f-.108f,1.05f,.012f,1.78f,2.42f,Surface.Slide,uv);
        }
        private static void Waiting(MeshData solid,MeshData detail,InteriorBay b,LayoutConfig c)
        {
            float w=b.LocalWidth,d=b.LocalDepth,uv=c.TextureWorldSize;
            int rows=Math.Max(1,Math.Min(c.CurveSegments<=12?3:4,(int)((d-2)/1.5f)));
            int columns=Math.Max(1,Math.Min(c.CurveSegments<=12?2:3,(int)((w*.5f-1.35f)/.82f)));
            for(int side=-1;side<=1;side+=2)for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                float x=side*(1.55f+col*.82f),z=-d*.5f+1.6f+row*1.5f;
                Chair(solid,x,z,b.Variant==1?0:2,uv);
                if(col<columns-1)Box(detail,side*(1.96f+col*.82f),z,.33f,.06f,.21f,.25f,Surface.Metal,uv);
            }
            float back=d*.5f-.45f;
            if(b.Variant==2)Box(solid,0,back,Math.Min(3,w-1),.6f,0,1.05f,Surface.Accent,uv);
            else Board(solid,detail,0,back,Math.Min(2.4f,w-1),uv);
            Lettering(detail,b.Variant==0?"ESPERA":b.Variant==1?"SALA":"INFORMACION",new V3(-.7f,2.1f,back-.085f),.025f);
        }
        private static void Dining(MeshData solid,MeshData detail,InteriorBay b,LayoutConfig c)
        {
            float w=b.LocalWidth,d=b.LocalDepth,uv=c.TextureWorldSize;
            int rows=Math.Max(1,Math.Min(c.CurveSegments<=12?2:3,(int)((d-2)/2.7f)));
            for(int side=-1;side<=1;side+=2)for(int row=0;row<rows;row++)
            {
                float x=side*(w*.5f-1.1f),z=-d*.5f+1.9f+row*2.7f;
                Table(solid,x,z,1.25f,.8f,uv);
                if(b.Variant==0)
                {
                    for(int end=-1;end<=1;end+=2)
                    {
                        Box(solid,x,z+end*.7f,1.35f,.55f,0,.45f,Surface.Accent,uv);
                        Box(solid,x,z+end*.92f,1.35f,.12f,.4f,1.12f,Surface.Slide,uv);
                        Box(detail,x,z+end*.64f,1.3f,.43f,.45f,.5f,Surface.Slide,uv);
                    }
                }
                else {Chair(solid,x,z-.8f,2,uv);Chair(solid,x,z+.8f,0,uv);}
                Box(detail,x,z,.2f,.2f,.79f,.95f,Surface.Plaster,uv);
            }
            float bz=d*.5f-.38f;
            Box(solid,0,bz,Math.Min(3.2f,w-1.2f),.6f,0,.95f,Surface.Accent,uv);
            Box(detail,0,bz,Math.Min(3.25f,w-1.1f),.62f,.95f,1,Surface.Metal,uv);
            if(b.Variant==2)
            {
                for(int i=-1;i<=1;i++)Box(detail,i*.65f,bz,.42f,.35f,1,1.08f,Surface.Metal,uv);
                Box(detail,0,bz,Math.Min(3,w-1.4f),.04f,1.42f,1.48f,Surface.Metal,uv);
                for(int side=-1;side<=1;side+=2)Box(detail,side*1.2f,bz,.04f,.04f,1,1.45f,Surface.Metal,uv);
            }
            Lettering(detail,b.Variant==2?"BUFFET":"CAFETERIA",new V3(-.7f,1.85f,bz+.22f),.023f);
        }
        private static void Table(MeshData solid,float x,float z,float w,float d,float uv)
        {
            Box(solid,x,z,w,d,.73f,.79f,Surface.Accent,uv);
            for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)Box(solid,x+sx*(w*.5f-.08f),z+sz*(d*.5f-.08f),.065f,.065f,0,.74f,Surface.Metal,uv);
        }
        private static void Chair(MeshData solid,float x,float z,int turns,float uv)
        {
            var chair=new MeshData();Box(chair,0,0,.58f,.55f,.43f,.5f,Surface.Slide,uv);
            Box(chair,0,.22f,.58f,.08f,.5f,.99f,Surface.Slide,uv);
            for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)Box(chair,sx*.24f,sz*.2f,.04f,.04f,0,.44f,Surface.Metal,uv);
            chair.TranslateRotate(new V3(x,0,z),turns*Shapes.Pi*.5f);solid.Append(chair);
        }
        private static void Board(MeshData solid,MeshData detail,float x,float z,float w,float uv)
        {
            Box(solid,x,z,w,.08f,1.2f,2.25f,Surface.Accent,uv);
            Box(detail,x,z-.048f,w-.12f,.01f,1.26f,2.19f,Surface.Plaster,uv);
            for(int row=0;row<3;row++)Box(detail,x-w*.2f,z-.055f,w*.35f,.004f,1.48f+row*.18f,1.5f+row*.18f,Surface.Metal,uv);
        }
        private static void Lettering(MeshData mesh,string text,V3 origin,float pixel)
        {
            var part=new MeshData();MallGlyphs.Write(part,text,origin,pixel);
            // Static physical signs do not emit light or consume an extra light source.
            part.Indices[(int)Surface.Slide].AddRange(part.Indices[(int)Surface.Glow]);part.Indices[(int)Surface.Glow].Clear();mesh.Append(part);
        }
    }
}
