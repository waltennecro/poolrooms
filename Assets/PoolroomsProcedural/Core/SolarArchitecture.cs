using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class SolarArchitecture
    {
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            if(r.Plan==null||r.Plan.Solar==null||!r.Plan.Valid(r,c))throw new ArgumentException("Invalid solar room.");
            var o=new RoomGeometry();o.Regions.AddRange(r.Plan.Pools);o.Region=r.Plan.Pools[0];
            var footprint=new PlanRect(-r.Width*.5f,-r.Depth*.5f,r.Width*.5f,r.Depth*.5f);
            var floor=new List<PlanRect>{footprint};foreach(PlanRect cut in r.Plan.Circulation.Cuts)PlanRect.Subtract(floor,cut);
            foreach(WaterRegion p in r.Plan.Pools)PlanRect.Subtract(floor,new PlanRect(p.CenterX-p.Width*.5f,p.CenterZ-p.Depth*.5f,p.CenterX+p.Width*.5f,p.CenterZ+p.Depth*.5f));
            V2[] contour=SolarOutline.Contour(r,r.Plan.Solar,c.CurveSegments);
            foreach(PlanRect q in floor)SolarOutline.Slab(o.Structure,q,contour,-1.65f,0,Surface.Tile,c.TextureWorldSize);
            foreach(WaterRegion p in r.Plan.Pools)ProceduralArchitecture.Basin(o,p,c);
            Walls(o,r,c);Roof(o,r,c,footprint);Lights(o,r,c);
            foreach(SolarDetail detail in r.Plan.Solar.Details)Detail(o,r,detail,c);
            PoolStructures(o,r,c);
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);ProceduralArchitecture.Prune(o.Water);
            return o;
        }
        private static void PoolStructures(RoomGeometry o,Room r,LayoutConfig c)
        {
            int style=SolarCatalog.Get(r.Plan.Solar.Recipe).Structure,n=Math.Max(8,Math.Min(24,c.CurveSegments));float uv=c.TextureWorldSize;
            foreach(WaterRegion pool in r.Plan.Pools)
            {
                var m=new MeshData();float w=pool.Width,d=pool.Depth,bottom=pool.Bottom,h=r.Height;
                if(style==0)for(int sign=-1;sign<=1;sign+=2)Shapes.Column(m,sign*w*.23f,0,Math.Min(.38f,w*.08f),bottom,h,n,Surface.Tile,uv);
                if(style==1||style==5)
                {
                    int frames=style==1?2:3;
                    for(int i=0;i<frames;i++)
                    {
                        float span=w*.82f,door=span*.72f,archHeight=Math.Min(h-.3f,Math.Max(door*.5f+2.2f,4.8f));
                        ZoneShapes.Portal(m,0,(i-(frames-1)*.5f)*d*.62f/frames,0,span,h,bottom,door,archHeight,.22f,style==1?1:2,n,uv);
                    }
                }
                if(style==2)
                {
                    // Perforated piers stand in the basin, so the circulation remains completely open.
                    int count=3;for(int i=0;i<count;i++)
                    {float x=(i-1)*w*.25f;ZoneShapes.Portal(m,x,0,0,w*.22f,h,bottom,w*.14f,Math.Min(h-.5f,4.5f),.22f,3,n,uv);}
                }
                if(style==3)for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)ZoneShapes.Box(m,sx*w*.27f,sz*d*.27f,Math.Min(.7f,w*.12f),Math.Min(.7f,d*.12f),bottom,h,Surface.Tile,uv);
                if(style==4)
                {
                    float radius=Math.Min(w*.38f,4),spring=Math.Max(2.3f,h-radius-.45f);
                    ZoneShapes.Vault(m,0,0,radius,d*.78f,spring,0,n,uv);
                    for(int sign=-1;sign<=1;sign+=2)Shapes.Column(m,sign*(radius+.05f),0,.15f,bottom,spring,n,Surface.Tile,uv);
                }
                if(style==6)
                {for(int sign=-1;sign<=1;sign+=2)ZoneShapes.Box(m,sign*w*.4f,0,w*.06f,d*.85f,-.025f,.035f,Surface.Accent,uv);}
                if(style==7)
                {
                    int steps=3;float strip=Math.Min(1.1f,w*.22f),run=d*.65f;
                    for(int i=0;i<steps;i++)ZoneShapes.Box(m,-w*.34f,-run*.5f+(i+.5f)*run/steps,strip,run/steps,bottom,.2f*(i+1),Surface.Tile,uv);
                }
                m.TranslateRotate(new V3(pool.CenterX,0,pool.CenterZ),0);o.Structure.Append(m);
            }
        }
        private static void Walls(RoomGeometry o,Room r,LayoutConfig c)
        {
            float hx=r.Width*.5f,hz=r.Depth*.5f;
            for(int side=0;side<4;side++)
            {
                float extent=side%2==0?hx:hz,lo=-extent+r.Plan.Solar.OutlineRadius,hi=extent-r.Plan.Solar.OutlineRadius;
                foreach(PlanRect cut in r.Plan.Circulation.Cuts)
                {
                    bool touches=side==0?cut.MaxZ==hz:side==1?cut.MaxX==hx:side==2?cut.MinZ==-hz:cut.MinX==-hx;if(!touches)continue;
                    float a=side==0?cut.MinX:side==1?-cut.MaxZ:side==2?-cut.MaxX:cut.MinZ;
                    float b=side==0?cut.MaxX:side==1?-cut.MinZ:side==2?-cut.MinX:cut.MaxZ;
                    if(a<=-extent+.001f)lo=Math.Max(lo,b);else hi=Math.Min(hi,a);
                }
                var wall=new MeshData();
                if(r.HasExit(side))
                {
                    float width=SolarConnections.DoorWidth(r,side,c),height=SolarConnections.DoorHeight(r,side,c),half=width*.5f+.3f;
                    int shape=r.Plan.Solar.Connections[side]==SolarConnectionKind.OpenHall?3:1;
                    // Wide arches retain >2 m straight headroom; cap radius when an opening is unusually broad.
                    if(width*.25f>height-2.2f)shape=3;
                    int frames=r.Plan.Solar.Connections[side]==SolarConnectionKind.DoubleArch?2:1;
                    for(int frame=0;frame<frames;frame++)
                        ZoneShapes.Portal(wall,0,(frame-(frames-1)*.5f)*c.WallThickness*.7f,0,half*2,r.Height,0,width,height,c.WallThickness/frames,shape,Math.Max(8,c.CurveSegments/2),c.TextureWorldSize);
                    WallBand(wall,lo,-half,0,r.Height,c);WallBand(wall,half,hi,0,r.Height,c);
                }
                else
                {
                    var pieces=new List<PlanRect>{new PlanRect(lo,0,hi,r.Height)};
                    foreach(SolarAperture aperture in r.Plan.Solar.Apertures)if(aperture.Side==side)PlanRect.Subtract(pieces,aperture.Rect);
                    foreach(PlanRect q in pieces)WallBand(wall,q.MinX,q.MaxX,q.MinZ,q.MaxZ,c);
                    foreach(SolarAperture aperture in r.Plan.Solar.Apertures)if(aperture.Side==side)WindowFrame(wall,aperture,c);
                }
                wall.TranslateRotate(new V3(Directions.DX[side]*hx,0,Directions.DZ[side]*hz),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
            SolarOutline.CornerWalls(o.Structure,r,r.Plan.Solar,c);
            foreach(PlanRect cut in r.Plan.Circulation.Cuts)
            {
                float x=cut.MinX<0?cut.MaxX:cut.MinX,z=cut.MinZ<0?cut.MaxZ:cut.MinZ;
                ZoneShapes.Box(o.Structure,x,cut.Z,c.WallThickness,cut.Depth,0,r.Height,Surface.Tile,c.TextureWorldSize);
                ZoneShapes.Box(o.Structure,cut.X,z,cut.Width,c.WallThickness,0,r.Height,Surface.Tile,c.TextureWorldSize);
            }
        }
        private static void WallBand(MeshData m,float x0,float x1,float bottom,float top,LayoutConfig c)
        {if(x1-x0>.001f&&top-bottom>.001f)ZoneShapes.Box(m,(x0+x1)*.5f,0,x1-x0,c.WallThickness,bottom,top,Surface.Tile,c.TextureWorldSize);}
        private static void WindowFrame(MeshData m,SolarAperture a,LayoutConfig c)
        {
            PlanRect q=a.Rect;float uv=c.TextureWorldSize;
            ZoneShapes.Box(m,q.X,0,q.Width+.2f,.55f,q.MinZ-.1f,q.MinZ+.08f,Surface.Plaster,uv);
            ZoneShapes.Box(m,q.X,0,q.Width+.2f,.4f,q.MaxZ-.08f,q.MaxZ+.1f,Surface.Plaster,uv);
            int bars=a.Louvers?Math.Max(2,(int)(q.Width/1.4f)):1;
            for(int i=0;i<=bars;i++)ZoneShapes.Box(m,q.MinX+q.Width*i/bars,0,.1f,.38f,q.MinZ,q.MaxZ,Surface.Plaster,uv);
        }
        private static void Roof(RoomGeometry o,Room r,LayoutConfig c,PlanRect bounds)
        {
            var free=new List<PlanRect>{bounds};foreach(PlanRect cut in r.Plan.Circulation.Cuts)PlanRect.Subtract(free,cut);
            foreach(SolarAperture aperture in r.Plan.Solar.Apertures)if(aperture.Side<0)PlanRect.Subtract(free,aperture.Rect);
            V2[] contour=SolarOutline.Contour(r,r.Plan.Solar,c.CurveSegments);
            foreach(PlanRect q in free)SolarOutline.Slab(o.Structure,q,contour,r.Height,r.Height+.3f,Surface.Plaster,c.TextureWorldSize);
            foreach(SolarAperture a in r.Plan.Solar.Apertures)if(a.Side<0)
            {
                PlanRect q=a.Rect;float h=r.Height,uv=c.TextureWorldSize;
                for(int sign=-1;sign<=1;sign+=2)
                {
                    ZoneShapes.Box(o.Structure,q.X+sign*q.Width*.5f,q.Z,.16f,q.Depth+.16f,h-.12f,h+.5f,Surface.Plaster,uv);
                    ZoneShapes.Box(o.Structure,q.X,q.Z+sign*q.Depth*.5f,q.Width,.16f,h-.12f,h+.5f,Surface.Plaster,uv);
                }
                if(a.Louvers)
                {
                    int n=Math.Max(2,(int)(q.Width/1.6f));
                    for(int i=1;i<n;i++)ZoneShapes.Box(o.Structure,q.MinX+q.Width*i/n,q.Z,.15f,q.Depth,h+.08f,h+.4f,Surface.Plaster,uv);
                }
            }
            int structure=SolarCatalog.Get(r.Plan.Solar.Recipe).Structure;
            // Authored ceiling treatments are distinct geometry, clipped to the actual roof pieces.
            if(structure==6||structure==5)
            {
                float pitch=structure==6?3.1f:5.2f;
                for(float x=-r.Width*.5f+pitch;x<r.Width*.5f;x+=pitch)
                {
                    var strip=new List<PlanRect>{new PlanRect(x-.12f,-r.Depth*.5f,x+.12f,r.Depth*.5f)};
                    foreach(PlanRect cut in r.Plan.Circulation.Cuts)PlanRect.Subtract(strip,cut);
                    foreach(SolarAperture a in r.Plan.Solar.Apertures)if(a.Side<0)PlanRect.Subtract(strip,a.Rect.Expanded(.1f));
                    foreach(PlanRect q in strip)SolarOutline.Slab(o.Structure,q,contour,r.Height-.35f,r.Height,Surface.Plaster,c.TextureWorldSize);
                }
            }
        }
        private static void Lights(RoomGeometry o,Room r,LayoutConfig c)
        {
            SolarRoomPlan p=r.Plan.Solar;bool sky=SolarClimateRegions.HasSky(p.Climate);LightingLook look=PoolroomLighting.Look(r,c);
            o.Structure.EmissionTint=sky?p.ClimateDescription.SunColor:look.Color;o.Structure.EmissionStrength=sky?.45f:look.Emission;
            foreach(SolarAperture aperture in p.Apertures)
            {
                PlanRect q=aperture.Rect;V3 source,aim;
                if(aperture.Side<0){source=new V3(q.X,r.Height+.42f,q.Z);aim=p.ClimateDescription.SunDirection;}
                else
                {
                    int side=aperture.Side;float hx=r.Width*.5f,hz=r.Depth*.5f;
                    V3 local=new V3(q.X,q.Z,.25f);float angle=side*Shapes.Pi*.5f;
                    source=new V3(Shapes.Cos(angle)*local.X+Shapes.Sin(angle)*local.Z,local.Y,-Shapes.Sin(angle)*local.X+Shapes.Cos(angle)*local.Z)+new V3(Directions.DX[side]*hx,0,Directions.DZ[side]*hz);
                    aim=p.ClimateDescription.SunDirection;
                }
                bool diffuse=p.Climate==PoolClimate.DiffuseDaylight;
                o.Fixtures.Add(new Fixture{Position=source,Aim=aim,Width=q.Width,Depth=aperture.Side<0?q.Depth:1,Power=(diffuse?48000:135000)*p.ClimateDescription.Brightness,Temperature=5500,Skylight=true,CustomColor=true,LightColor=p.ClimateDescription.SunColor,Range=48,SolarBeam=true,PoolroomSunlight=true,ConeAngle=diffuse?95:72});
                // Explicit broad bounce preserves readable ceramic surfaces on hardware without SSGI/RT.
                // It is bounded to the room and uses the existing active-light budget.

            }
            if(sky)o.Fixtures.Add(new Fixture{Position=new V3(0,Math.Min(r.Height-.4f,4.2f),0),Width=4,Depth=4,Power=14000,Temperature=6000,CustomColor=true,LightColor=new V3(.84f,.91f,1),Range=Math.Min(r.Width,r.Depth)*.35f,SolarFill=true});
            // At most four lamps in the atlas; darkness is a region choice, not a global underexposure.
            int count=sky?2:4;
            for(int i=0;i<count;i++)
            {
                float x=(i%2==0?-1:1)*r.Width*.26f,z=(i/2==0?-1:1)*r.Depth*.24f;
                if(!r.Plan.Circulation.Contains(r,x,z,.5f))continue;
                float power=sky?900:p.Climate==PoolClimate.Enclosed?6500:(c.Lighting==LightingDistribution.Classic?2200:3600)*look.Power;if(p.Climate==PoolClimate.Dim&&(c.Lighting==LightingDistribution.Classic?i>1:i%2==1))power=0;
                ZoneShapes.Box(o.Structure,x,z,1.5f,.42f,r.Height-.16f,r.Height-.1f,power>0?Surface.Glow:Surface.Plaster,c.TextureWorldSize);
                if(power>0)o.Fixtures.Add(new Fixture{Position=new V3(x,r.Height-.22f,z),Width=1.5f,Depth=.42f,Power=power,Temperature=4900,LightColor=look.Color,CustomColor=true,Range=Math.Max(look.Range,r.Height+2)});
            }
            if(p.Climate==PoolClimate.Dim&&c.Lighting!=LightingDistribution.Classic)PoolroomLighting.AddSupport(o.Fixtures,r,look);
        }
        private static void Detail(RoomGeometry o,Room r,SolarDetail p,LayoutConfig c)
        {
            var m=new MeshData();var decor=new MeshData();PlanRect q=p.Bounds;
            // Quarter turns preserve the world footprint: exchange the local dimensions before rotation.
            float w=(p.Turns&1)==0?q.Width:q.Depth,d=(p.Turns&1)==0?q.Depth:q.Width,uv=c.TextureWorldSize;
            var rng=new StableRandom(p.Seed);int n=Math.Max(8,Math.Min(24,c.CurveSegments));float h=r.Height;
            switch(p.Kind)
            {
                case SolarDetailKind.Bench:
                    Box(m,0,0,w*.85f,Math.Min(.65f,d*.65f),.1f,.46f,Surface.Tile,uv);
                    Box(m,0,Math.Min(.22f,d*.2f),w*.85f,.12f,.46f,.9f,Surface.Accent,uv);break;
                case SolarDetailKind.Lounger:
                    Box(m,0,-d*.12f,w*.65f,d*.7f,.14f,.42f,Surface.Plaster,uv);
                    ZoneShapes.Ramp(m,0,d*.31f,w*.65f,d*.2f,.4f,0,uv);break;
                case SolarDetailKind.Locker:
                    int doors=Math.Max(1,Math.Min(5,(int)(w/.65f)));
                    for(int i=0;i<doors;i++)
                    {float x=(i-(doors-1)*.5f)*w/doors;Box(m,x,0,w/doors-.05f,Math.Min(.65f,d*.8f),0,1.9f,Surface.Plaster,uv);Box(decor,x+w/doors*.25f,-Math.Min(.65f,d*.8f)*.5f-.008f,.025f,.025f,.9f,1.08f,Surface.Metal,uv);}break;
                case SolarDetailKind.Shower:
                    Box(m,0,d*.3f,w*.75f,.22f,0,2.5f,Surface.Tile,uv);
                    for(int i=0;i<2;i++){float x=(i-.5f)*w*.45f;ZoneShapes.Beam(m,new V3(x,.7f,d*.15f),new V3(x,2.2f,d*.15f),.04f,Surface.Metal,uv,6);Box(decor,x,d*.05f,.22f,.28f,2.15f,2.2f,Surface.Metal,uv);}break;
                case SolarDetailKind.Rail:
                    for(int sign=-1;sign<=1;sign+=2){float x=sign*w*.36f;ZoneShapes.Beam(m,new V3(x,0,-d*.35f),new V3(x,1,-d*.35f),.04f,Surface.Metal,uv,6);ZoneShapes.Beam(m,new V3(x,1,-d*.35f),new V3(x,1,d*.35f),.04f,Surface.Metal,uv,6);}break;
                case SolarDetailKind.Ladder:
                    for(int sign=-1;sign<=1;sign+=2)ZoneShapes.Beam(m,new V3(sign*.3f,0,0),new V3(sign*.3f,1.8f,0),.035f,Surface.Metal,uv,6);
                    for(int i=0;i<6;i++)ZoneShapes.Beam(m,new V3(-.3f,.2f+i*.27f,0),new V3(.3f,.2f+i*.27f,0),.03f,Surface.Metal,uv,6);break;
                case SolarDetailKind.LifeRing:
                    float radius=Math.Min(.38f,Math.Min(w,d)*.32f);int sides=16;
                    for(int i=0;i<sides;i++){float a=i*Shapes.Pi*2/sides,b=(i+1)*Shapes.Pi*2/sides;ZoneShapes.Beam(decor,new V3(radius*Shapes.Cos(a),.15f,radius*Shapes.Sin(a)),new V3(radius*Shapes.Cos(b),.15f,radius*Shapes.Sin(b)),.075f,Surface.Accent,uv,6);}break;
                case SolarDetailKind.Drain:
                    Box(decor,0,0,w*.8f,d*.3f,.006f,.02f,Surface.Metal,uv);
                    for(int i=0;i<8;i++)Box(decor,(i-3.5f)*w*.08f,0,.015f,d*.28f,.02f,.023f,Surface.Plaster,uv);break;
                case SolarDetailKind.ServiceCupboard:
                    Box(m,0,0,w*.6f,d*.35f,0,1.35f,Surface.Plaster,uv);Box(decor,0,-d*.18f,.04f,.035f,.65f,.82f,Surface.Metal,uv);break;
                case SolarDetailKind.Stool:
                    Shapes.Column(m,0,0,Math.Min(w,d)*.22f,.05f,.45f,12,Surface.Accent,uv);break;
                case SolarDetailKind.TileTable:
                    Shapes.Column(m,0,0,Math.Min(w,d)*.12f,0,.7f,12,Surface.Tile,uv);Box(m,0,0,w*.7f,d*.6f,.7f,.78f,Surface.Tile,uv);break;
                case SolarDetailKind.SlattedScreen:
                    int bars=Math.Max(2,Math.Min(8,(int)(w/.5f)));
                    for(int i=0;i<bars;i++)Box(m,(i-(bars-1)*.5f)*w/bars,0,.12f,Math.Min(d*.4f,.25f),0,h-.5f,Surface.Plaster,uv);break;
                case SolarDetailKind.StairTerrace:case SolarDetailKind.AccessibleRamp:
                    float rise=p.Rise,flight=Math.Max(.7f,d*.65f);Box(m,0,d*.35f,w*.8f,d*.25f,rise-.16f,rise,Surface.Tile,uv);
                    if(p.Kind==SolarDetailKind.AccessibleRamp)ZoneShapes.Ramp(m,0,-d*.1f,Math.Min(1.8f,w*.75f),flight,rise,0,uv);
                    else{int steps=Math.Max(2,(int)Math.Ceiling(rise/.2f));for(int i=0;i<steps;i++)Box(m,0,-d*.425f+(i+.5f)*flight/steps,w*.8f,flight/steps,0,rise*(i+1)/steps,Surface.Tile,uv);}break;
                case SolarDetailKind.Arch:case SolarDetailKind.DoublePortal:
                    int arches=p.Kind==SolarDetailKind.DoublePortal?2:1;
                    for(int i=0;i<arches;i++)ZoneShapes.Portal(m,0,(i-(arches-1)*.5f)*d*.48f,0,w*.9f,h-.3f,0,Math.Max(1.3f,w*.72f),Math.Min(h-.5f,Math.Max(3.3f,w*.36f+2.2f)),.25f,1,n,uv);break;
                case SolarDetailKind.Niche:
                    Box(m,0,d*.3f,w*.85f,d*.2f,0,1.7f,Surface.Tile,uv);Box(m,0,d*.1f,w*.85f,d*.2f,0,.4f,Surface.Tile,uv);break;
                case SolarDetailKind.RoundColumn:
                    int columns=1+(int)(p.Seed%3);
                    for(int i=0;i<columns;i++)Shapes.Column(m,(i-(columns-1)*.5f)*w*.65f/columns,0,Math.Min(.65f,Math.Min(w,d)*.2f),0,h,n,Surface.Tile,uv);break;
                case SolarDetailKind.SquarePier:
                    Box(m,0,0,w*.34f,d*.34f,0,h,Surface.Tile,uv);break;
                case SolarDetailKind.Vault:
                    float vaultRadius=Math.Min(w*.4f,2.5f);ZoneShapes.Vault(m,0,0,vaultRadius,d*.8f,Math.Min(h-vaultRadius-.4f,3.8f),0,n,uv);
                    for(int sign=-1;sign<=1;sign+=2)Box(m,sign*(vaultRadius+.08f),0,.16f,d*.8f,0,Math.Min(h-vaultRadius-.4f,3.8f),Surface.Tile,uv);break;
            }
            m.TranslateRotate(new V3(q.X,0,q.Z),p.Turns*Shapes.Pi*.5f);decor.TranslateRotate(new V3(q.X,0,q.Z),p.Turns*Shapes.Pi*.5f);
            o.Structure.Append(m);o.Decoration.Append(decor);
        }
        private static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface mat,float uv)
        {ZoneShapes.Box(m,x,z,w,d,bottom,top,mat,uv);}
    }
}
