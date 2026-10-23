using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Bounded per-building worker geometry; no per-window objects or physics worlds.
    public static class CityBuildingArchitecture
    {
        static void Box(MeshData m,float x,float z,float w,float d,float lo,float hi,Surface s)
        {if(w>.0001f&&d>.0001f&&hi>lo)ZoneShapes.Box(m,x,z,w,d,lo,hi,s,1.2f);}
        static void Rect(MeshData m,PlanRect r,float lo,float hi,Surface s)
        {Box(m,r.X,r.Z,r.Width,r.Depth,lo,hi,s);}
        public static void Build(RoomGeometry o,CityBuildingPlan b,LayoutConfig c)
        {
            var design=new CityBuildingDesign(b.Design);float w=b.Width,d=b.Depth;
            var solid=new MeshData{Theme=SurfaceTheme.CityArchitecture,PaletteVariant=b.Design};
            var detail=new MeshData{Theme=SurfaceTheme.CityArchitecture,PaletteVariant=b.Design};
            var glass=new MeshData{Theme=SurfaceTheme.Glass};
            float sx=-w*.5f+2.2f;
            for(int level=0;level<b.Floors;level++)
            {
                float y=.12f+level*3.2f;
                foreach(var r in design.Floor(b,level))Rect(solid,r,level==0?0:y-.15f,y,Surface.Tile);
                if(level>0)
                {
                    foreach(var hole in design.Openings(b,level))Rail(solid,glass,hole,y);
                    // A full front gallery remains accessible from the switchback.
                    Portico(solid,detail,b,design,level,y,c);
                }
                else GroundFurniture(solid,detail,b);
                if(level<b.Floors-1)City11Architecture.Stair(solid,detail,sx,y);
                Shell(solid,detail,glass,b,design,level,y);
                UpperFurniture(o,solid,detail,b,design,level,y);
                if(level==0||level==b.Floors-1)
                    o.Fixtures.Add(new Fixture{Position=b.Point(w*.23f,y+2.8f,-d*.31f),Power=950,Range=6,Width=1,Depth=.14f,CustomColor=true,LightColor=new V3(1,.9f,.78f)});
            }
            Roof(solid,detail,glass,b,design);
            float angle=b.Turn?Shapes.Pi:0;var origin=new V3(b.X,0,b.Z);
            solid.TranslateRotate(origin,angle);detail.TranslateRotate(origin,angle);glass.TranslateRotate(origin,angle);
            // Structure owns collision; visual copy supplies per-building PBR bank.
            // Its material theme is kept on a separate draw batch, not lost when
            // joining the city-wide collider/legacy material buffer.
            o.Structure.Append(solid);o.VisualMeshes.Add(solid);o.VisualMeshes.Add(detail);
            o.Glass.Append(glass);o.CollisionOnly.Append(glass);
        }
        static void GroundFurniture(MeshData solid,MeshData detail,CityBuildingPlan b)
        {
            float x=b.Width*.23f;
            // Preserve original physical anchors for cups, radio and city seats.
            EncounterFurniture.Chamfer(solid,x,0,1.25f,.75f,.84f,.88f,.009f,Surface.Accent);
            foreach(int s in new[]{-1,1})Box(solid,x+s*.48f,0,.07f,.55f,.12f,.84f,Surface.Metal);
            Box(solid,x,b.Depth*.28f,1.4f,.55f,.12f,.92f,Surface.Accent);
            for(int n=0;n<3;n++)Box(detail,x,b.Depth*.28f-.283f,1.15f,.025f,.26f+n*.2f,.28f+n*.2f,Surface.Metal);
        }
        static void Rail(MeshData solid,MeshData glass,PlanRect r,float y)
        {
            // Four low guards, glass infill and spaced posts make each real floor cut safe.
            foreach(int sign in new[]{-1,1})
            {
                float x=sign<0?r.MinX-.035f:r.MaxX+.035f;
                float z=sign<0?r.MinZ-.035f:r.MaxZ+.035f;
                Box(solid,x,r.Z,.07f,r.Depth+.14f,y,y+.18f,Surface.Plaster);
                Box(solid,r.X,z,r.Width,.07f,y,y+.18f,Surface.Plaster);
                Box(solid,x,r.Z,.065f,r.Depth+.14f,y+1.02f,y+1.1f,Surface.Metal);
                Box(solid,r.X,z,r.Width,.065f,y+1.02f,y+1.1f,Surface.Metal);
                Box(glass,x,r.Z,.018f,r.Depth+.04f,y+.18f,y+1.02f,Surface.Tile);
                Box(glass,r.X,z,r.Width,.018f,y+.18f,y+1.02f,Surface.Tile);
                int nx=Math.Max(1,(int)Math.Ceiling(r.Width/1.1f)),nz=Math.Max(1,(int)Math.Ceiling(r.Depth/1.1f));
                for(int i=0;i<=nz;i++)Box(solid,x,r.MinZ+i*r.Depth/nz,.025f,.025f,y+.18f,y+1.02f,Surface.Metal);
                for(int i=0;i<=nx;i++)Box(solid,r.MinX+i*r.Width/nx,z,.025f,.025f,y+.18f,y+1.02f,Surface.Metal);
            }
        }
        static void Portico(MeshData solid,MeshData detail,CityBuildingPlan b,CityBuildingDesign p,int level,float y,LayoutConfig c)
        {
            var court=p.Court(b,level);float left=-b.Width*.5f+4.5f,right=b.Width*.5f-.15f;
            // Partitions are real apertures behind the protected front gallery.
            float z=court.MaxZ+.55f;
            if(z>b.Depth*.5f-.6f)return;
            float span=right-left,door=Math.Min(2,span-.35f),height=2.7f;
            var wall=new MeshData();
            if(p.Family%4==0||p.Family%4==3)
            {
                Shapes.Portal(wall,span,height,0,door,2.45f,.14f,Math.Max(12,c.CurveSegments),1.2f);
                wall.TranslateRotate(new V3((left+right)*.5f,y,z),0);solid.Append(wall);
            }
            else
            {
                // Long roof ribs / end piers turn a generic box into a gallery.
                float px=b.Width*.5f-.25f;
                Shapes.Column(solid,px,z,.095f,y,y+2.9f,12,Surface.Plaster,1.2f);
                Box(detail,(left+right)*.5f,z,span,.18f,y+2.65f,y+2.9f,Surface.Accent);
            }
            if(p.Strange)
            {
                int ribs=2+p.Family%4;
                for(int i=0;i<ribs;i++)
                {
                    float rz=-b.Depth*.38f+i*b.Depth*.7f/(ribs-1);
                    // High arches are above standing height; no arbitrary invisible
                    // obstruction crosses the landing, entry or front gallery.
                    float crown=y+2.48f+(i%2)*.13f;
                    EncounterFurniture.Chamfer(detail,0,rz,b.Width-.3f,.14f,crown,crown+.22f,.035f,Surface.Plaster);
                }
            }
        }
        static void Shell(MeshData solid,MeshData detail,MeshData glass,CityBuildingPlan b,CityBuildingDesign p,int level,float y)
        {
            for(int side=0;side<4;side++)
            {
                float span=side%2==0?b.Width:b.Depth;var wall=new MeshData();var pane=new MeshData();
                if(level==0&&side==2)Shapes.Portal(wall,span,3.2f,0,2.25f,2.7f,.22f,16,1.2f);
                else
                {
                    int bays=2+(p.Family%3)+(p.Variant%2);float pitch=(span-.3f)/bays;
                    float gap=Math.Min(pitch-.28f,1.35f+p.Variant*.045f),sill=p.Strange?.52f:.8f;
                    Box(wall,0,0,span,.22f,0,sill,Surface.Plaster);
                    Box(wall,0,0,span,.22f,2.52f,3.2f,Surface.Plaster);
                    var free=new List<PlanRect>{new PlanRect(-span*.5f,0,span*.5f,1)};
                    for(int i=0;i<bays;i++)
                    {
                        float x=-span*.5f+.15f+(i+.5f)*pitch;
                        PlanRect.Subtract(free,new PlanRect(x-gap*.5f,-1,x+gap*.5f,2));
                        Box(pane,x,0,gap,.018f,sill+.025f,2.495f,Surface.Tile);
                        // Recessed timber/metal frames, real reveal depth and sill.
                        foreach(int sign in new[]{-1,1})Box(wall,x+sign*(gap*.5f+.027f),-.018f,.054f,.28f,sill,2.55f,Surface.Accent);
                        Box(wall,x,-.08f,gap+.12f,.38f,sill-.035f,sill+.025f,Surface.Accent);
                    }
                    foreach(var r in free)Box(wall,r.X,0,r.Width,.22f,sill,2.52f,Surface.Plaster);
                }
                // Inward-contained brise soleil: no intrusion into the driving road
                // or neighbouring cell, even with the smallest supported footprint.
                if((p.Family%4)==1||p.Strange)
                    for(int slat=0;slat<2+p.Variant%3;slat++)
                        Box(wall,0,-.17f,span-.35f,.2f,2.62f+slat*.11f,2.665f+slat*.11f,Surface.Metal);
                var at=new V3(Directions.DX[side]*b.Width*.5f,y,Directions.DZ[side]*b.Depth*.5f);
                wall.TranslateRotate(at,side*Shapes.Pi*.5f);pane.TranslateRotate(at,side*Shapes.Pi*.5f);solid.Append(wall);glass.Append(pane);
            }
            // Repeated deep coffers/ribs vary the actual section, not just colour.
            int count=2+p.Family%5;
            for(int i=0;i<count;i++)
            {
                float z=-b.Depth*.36f+i*b.Depth*.72f/(count-1);
                Box(detail,0,z,b.Width-.3f,.1f,y+2.96f,y+3.045f,Surface.Accent);
            }
        }
        static void UpperFurniture(RoomGeometry o,MeshData solid,MeshData detail,CityBuildingPlan b,CityBuildingDesign p,int level,float y)
        {
            if(level==0)return;
            // Side/rear rooms are furnished outside the court and stair. Imported
            // furnishings are appended only on real supported walkable deck slabs.
            float x=b.Width*.5f-.8f,z=b.Depth*.5f-.8f;
            if(p.Family%3==0)
            {
                EncounterFurniture.Chamfer(solid,x,z,.55f,.65f,y+.44f,y+.52f,.02f,Surface.Accent);
                Box(solid,x,z,.32f,.4f,y,y+.44f,Surface.Metal);
                Box(solid,x+.21f,z,.075f,.65f,y+.52f,y+1.08f,Surface.Accent);
            }
            else
            {
                // At most one imported source per floor: bounded source/LOD cost.
                int source=p.Family%3==1?35:30;
                var half=SourceFurniture.HalfExtents(source,0,.015f);
                foreach(var at in new[]{new V3(x,y+.004f,z),new V3(b.Width*.5f-.72f,y+.004f,.15f),new V3(b.Width*.5f-.72f,y+.004f,1.4f)})
                {
                    bool supported=true;
                    foreach(int ix in new[]{-1,1})foreach(int iz in new[]{-1,1})
                    {
                        bool found=false;foreach(var deck in p.Floor(b,level))if(deck.Contains(at.X+ix*half.X,at.Z+iz*half.Z)){found=true;break;}
                        if(!found)supported=false;
                    }
                    if(!supported||!SourceFurniture.Clear(solid,new SourceFurniturePlacement(0,source,at,0),.015f))continue;
                    o.SourceSeats.Add(new SourceFurniturePlacement(980000+((b.X<0?0:1)+(b.Z<0?0:2))*8+(level-1),source,b.Point(at.X,at.Y,at.Z),b.Turn?180:0));break;
                }
            }
            float bx=-b.Width*.5f+1.2f,bz=b.Depth*.5f-.27f;
            // Human-scale book/cabinet wall beyond the far stair landing.
            if(b.Depth>9)
            {
                Box(solid,bx,bz,1.5f,.32f,y,y+1.75f,Surface.Accent);
                for(int row=0;row<4;row++)Box(detail,bx,bz-.17f,1.34f,.025f,y+.25f+row*.36f,y+.28f+row*.36f,Surface.Metal);
            }
        }
        static void Roof(MeshData solid,MeshData detail,MeshData glass,CityBuildingPlan b,CityBuildingDesign p)
        {
            float y=.12f+b.Floors*3.2f;var hole=p.Court(b,b.Floors-1);
            var parts=new List<PlanRect>{new PlanRect(-b.Width*.5f,-b.Depth*.5f,b.Width*.5f,b.Depth*.5f)};
            PlanRect.Subtract(parts,hole);
            foreach(var r in parts)Rect(solid,r,y,y+.18f,Surface.Plaster);
            // Real roof aperture, rather than a glass patch pasted onto an opaque slab.
            Rect(glass,hole,y+.21f,y+.225f,Surface.Tile);
            int ribs=3+p.Variant;float rise=.45f+(p.Family%8)*.14f+p.Variant*.045f;
            for(int i=0;i<=ribs;i++)
            {
                float z=hole.MinZ+hole.Depth*i/ribs;
                if(p.Strange)
                {
                    float cy=y+.3f+rise*(i%2==0?.4f:1);
                    Box(detail,hole.X,z,hole.Width+.2f,.12f,cy,cy+.16f,Surface.Metal);
                    foreach(int s in new[]{-1,1})Box(solid,s<0?hole.MinX-.07f:hole.MaxX+.07f,z,.1f,.12f,y+.18f,cy+.16f,Surface.Metal);
                }
                else Box(detail,hole.X,z,hole.Width+.15f,.075f,y+.23f,y+.34f,Surface.Metal);
            }
            // Skyline identity is section geometry: terraced rings, lantern banks,
            // pleated screens or deep offset hoods, entirely within the plot.
            int layers=1+p.Family%4;
            for(int i=0;i<layers;i++)
            {
                float inset=.25f+i*.2f,top=y+.3f+(i+1)*(.14f+p.Variant*.018f);
                foreach(int s in new[]{-1,1})
                {
                    Box(detail,s*(b.Width*.5f-inset),0,.09f,b.Depth-2*inset,top-.1f,top,Surface.Accent);
                    Box(detail,0,s*(b.Depth*.5f-inset),b.Width-2*inset,.09f,top-.1f,top,Surface.Accent);
                }
            }
        }
    }
}
