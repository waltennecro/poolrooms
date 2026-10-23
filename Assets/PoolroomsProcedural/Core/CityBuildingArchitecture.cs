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
            var detail=new MeshData{Theme=SurfaceTheme.CityArchitecture,PaletteVariant=b.Design,EmissionTint=new V3(1,.9f,.78f),EmissionStrength=.4f};
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
                {
                    float lightZ=Math.Max(-d*.31f,design.Envelope(b,level).MinZ+design.PorchDepth+.4f);
                    Box(detail,w*.23f,lightZ,1.04f,.18f,y+2.86f,y+2.91f,Surface.Metal);
                    Box(detail,w*.23f,lightZ,1,.14f,y+2.845f,y+2.86f,Surface.Glow);
                    o.Fixtures.Add(new Fixture{Position=b.Point(w*.23f,y+2.8f,lightZ),Power=950,Range=6,Width=1,Depth=.14f,CustomColor=true,LightColor=new V3(1,.9f,.78f)});
                }
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
            var court=p.Court(b,level);float left=-b.Width*.5f+4.5f,right=p.Envelope(b,level).MaxX-.15f;
            // Partitions are real apertures behind the protected front gallery.
            float z=court.MaxZ+.55f;
            if(z>p.Envelope(b,level).MaxZ-.6f)return;
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
                float px=p.Envelope(b,level).MaxX-.25f;
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
            var envelope=p.Envelope(b,level);float front=envelope.MinZ+p.PorchDepth;
            for(int side=0;side<4;side++)
            {
                float span=side%2==0?envelope.Width:envelope.MaxZ-front;
                var wall=new MeshData();var pane=new MeshData();
                if(side==2)Shapes.Portal(wall,span,3.2f,0,2.25f,2.7f,.22f,16,1.2f);
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
                        foreach(int sign in new[]{-1,1})Box(wall,x+sign*(gap*.5f+.027f),-.018f,.054f,.28f,sill,2.55f,Surface.Accent);
                        Box(wall,x,-.08f,gap+.12f,.38f,sill-.035f,sill+.025f,Surface.Accent);
                    }
                    foreach(var r in free)Box(wall,r.X,0,r.Width,.22f,sill,2.52f,Surface.Plaster);
                }
                if((p.Family%4)==1||p.Strange)
                    for(int slat=0;slat<2+p.Variant%3;slat++)
                        Box(wall,0,-.17f,span-.35f,.2f,2.62f+slat*.11f,2.665f+slat*.11f,Surface.Metal);
                V3 at=side==0?new V3(envelope.X,y,envelope.MaxZ):side==2?new V3(envelope.X,y,front):
                    new V3(side==1?envelope.MaxX:envelope.MinX,y,(front+envelope.MaxZ)*.5f);
                wall.TranslateRotate(at,side*Shapes.Pi*.5f);pane.TranslateRotate(at,side*Shapes.Pi*.5f);solid.Append(wall);glass.Append(pane);
            }
            // Recessed entrances and outward-open balconies articulate the actual
            // shell. Upper envelopes retreat independently, not just painted bands.
            if(level==0||p.Family%4==0||p.Strange&&level%2==0)
            {
                int bays=3;float pitch=envelope.Width/bays;
                for(int i=0;i<bays;i++)
                {
                    var arch=new MeshData();Shapes.Portal(arch,pitch,3.2f,0,pitch-.3f,2.78f,.15f,16,1.2f);
                    arch.TranslateRotate(new V3(envelope.MinX+(i+.5f)*pitch,y,envelope.MinZ+.10f),0);solid.Append(arch);
                }
            }
            if(level>0)
            {
                float z=envelope.MinZ+.08f;
                Box(solid,envelope.X,z,envelope.Width,.08f,y,y+.18f,Surface.Plaster);
                Box(solid,envelope.X,z,envelope.Width,.07f,y+1.03f,y+1.1f,Surface.Metal);
                Box(glass,envelope.X,z,envelope.Width-.12f,.018f,y+.18f,y+1.03f,Surface.Tile);
                foreach(int sign in new[]{-1,1})
                {
                    float x=sign<0?envelope.MinX+.08f:envelope.MaxX-.08f;
                    Box(solid,x,envelope.MinZ+p.PorchDepth*.5f,.07f,p.PorchDepth,y+1.03f,y+1.1f,Surface.Metal);
                    Box(glass,x,envelope.MinZ+p.PorchDepth*.5f,.018f,p.PorchDepth,y+.18f,y+1.03f,Surface.Tile);
                    Box(solid,x,z,.07f,.07f,y,y+1.1f,Surface.Metal);
                }
            }
            EncounterFurniture.Chamfer(detail,envelope.X,envelope.MinZ+.12f,envelope.Width,.24f,y+2.94f,y+3.08f,.025f,Surface.Accent);
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
            var envelope=p.Envelope(b,level);float x=envelope.MaxX-.8f,z=envelope.MaxZ-.8f;
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
                foreach(var at in new[]{new V3(x,y+.004f,z),new V3(envelope.MaxX-.72f,y+.004f,.15f),new V3(envelope.MaxX-.72f,y+.004f,1.4f)})
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
            float bx=-b.Width*.5f+1.2f,bz=envelope.MaxZ-.27f;
            // Human-scale book/cabinet wall beyond the far stair landing.
            if(b.Depth>9)
            {
                foreach(int sign in new[]{-1,1})Box(solid,bx+sign*.73f,bz,.04f,.32f,y,y+1.75f,Surface.Accent);
                Box(solid,bx,bz+.14f,1.5f,.04f,y,y+1.75f,Surface.Accent);
                for(int shelf=0;shelf<5;shelf++)
                {
                    float sy=y+.05f+shelf*.34f;Box(solid,bx,bz,1.5f,.32f,sy,sy+.035f,Surface.Accent);
                    if(shelf==4)continue;
                    for(int book=0;book<8;book++)Box(detail,bx-.59f+book*.17f,bz-.01f,.08f,.24f,sy+.035f,sy+.22f+(book%3)*.025f,(book%3==0?Surface.Plaster:Surface.Accent));
                }
                for(int row=0;row<4;row++)Box(detail,bx,bz-.17f,1.34f,.025f,y+.25f+row*.36f,y+.28f+row*.36f,Surface.Metal);
            }
        }
        static void Roof(MeshData solid,MeshData detail,MeshData glass,CityBuildingPlan b,CityBuildingDesign p)
        {
            float y=.12f+b.Floors*3.2f;var hole=p.Court(b,b.Floors-1);
            var envelope=p.Envelope(b,b.Floors-1);var parts=new List<PlanRect>{envelope};
            PlanRect.Subtract(parts,hole);
            foreach(var r in parts)Rect(solid,r,y,y+.18f,Surface.Plaster);
            // Real roof aperture, rather than a glass patch pasted onto an opaque slab.
            if(p.Family%2==0)
            {
                // A real barrel-vaulted glazed roof, with curved geometry and ends.
                float vaultRise=.55f+(p.Family%5)*.12f+p.Variant*.035f;
                for(int i=0;i<12;i++)
                {
                    float a=i*Shapes.Pi/12,beta=(i+1)*Shapes.Pi/12,mid=(a+beta)*.5f;
                    var a0=new V3(hole.X-hole.Width*.5f*Shapes.Cos(a),y+.23f+vaultRise*Shapes.Sin(a),hole.MinZ);
                    var b0=new V3(hole.X-hole.Width*.5f*Shapes.Cos(beta),y+.23f+vaultRise*Shapes.Sin(beta),hole.MinZ);
                    var roofNormal=new V3(-Shapes.Cos(mid),Shapes.Sin(mid),0);var along=new V3(0,0,hole.Depth);var inner=new V3(0,-.018f,0);
                    glass.Quad(a0,b0,b0+along,a0+along,roofNormal,Surface.Tile,1.2f);
                    glass.Quad(a0+inner,a0+along+inner,b0+along+inner,b0+inner,roofNormal*-1,Surface.Tile,1.2f);
                    for(int rib=0;rib<3;rib++)ZoneShapes.Beam(detail,a0+along*(rib*.5f),b0+along*(rib*.5f),.035f,Surface.Metal,1.2f,4);
                    foreach(int end in new[]{0,1})
                    {
                        float z=end==0?hole.MinZ:hole.MaxZ;var aa=new V3(a0.X,a0.Y,z);var bb=new V3(b0.X,b0.Y,z);
                        var n=new V3(0,0,end==0?-1:1);
                        glass.Triangle(new V3(hole.X,y+.23f,z),aa,bb,n,n,n,new V2(.5f,0),new V2((float)i/12,1),new V2((float)(i+1)/12,1),Surface.Tile);
                        glass.Triangle(new V3(hole.X,y+.212f,z),bb+inner,aa+inner,n*-1,n*-1,n*-1,new V2(.5f,0),new V2((float)(i+1)/12,1),new V2((float)i/12,1),Surface.Tile);
                    }
                }
            }
            else Rect(glass,hole,y+.21f,y+.225f,Surface.Tile);
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
                float inset=.25f+i*.2f,top=y+.18f+(i+1)*.18f;
                foreach(int s in new[]{-1,1})
                {
                    Box(detail,s<0?envelope.MinX+inset:envelope.MaxX-inset,envelope.Z,.32f,envelope.Depth-2*inset,top-.18f,top,Surface.Accent);
                    Box(detail,envelope.X,s<0?envelope.MinZ+inset:envelope.MaxZ-inset,envelope.Width-2*inset,.32f,top-.18f,top,Surface.Accent);
                }
            }
        }
    }
}
