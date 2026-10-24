using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public struct Fixture
    {
        public V3 Position;
        public float Width, Depth, Power, Temperature;
        public bool Skylight,CustomColor;
        // Localized aperture lights are the fallback when the physical sun cannot cast shadows.
        public bool SolarBeam,SolarFill;
        public bool AmbientSupport; // Diffuse readability aid; never a floating specular lamp.
        public bool MallSunlight; // Mall aperture fallback, replaced by the shared regional sun.
        public bool PoolroomSunlight; // Local aperture fallback, replaced only by the Poolrooms directional sun.
        public V3 Aim;
        public float ConeAngle;
        public V3 LightColor;
        public float Range;
        // Local mall lamps must not illuminate the next storey when shadows are unavailable.
        public bool MallLocal;
        public float FloorOffset,ShadowlessRange;
        public float StoreyHeight; // Zero keeps historical mall eligibility.
    }
    [Serializable]
    public struct WaterRegion
    {
        public float Width, Depth, Radius, Bottom, Level;
        public float CenterX,CenterZ;
        public bool Contains(float x,float z)
        {
            if(Width<=0||Depth<=0)return false;
            float ax=Math.Abs(x-CenterX),az=Math.Abs(z-CenterZ),hx=Width*.5f,hz=Depth*.5f;
            if(ax>hx||az>hz)return false;
            float dx=Math.Max(0,ax-(hx-Radius)),dz=Math.Max(0,az-(hz-Radius));
            return dx*dx+dz*dz<=Radius*Radius+1e-5f;
        }
    }
    public sealed class RoomGeometry
    {
        public readonly MeshData Structure=new MeshData(),Water=new MeshData(),Decoration=new MeshData(),Glass=new MeshData(),CollisionOnly=new MeshData();
        public DressingGeometry Dressing;
        public Forest39Canopy Forest39Canopy;
        public Level10CropPatch[] Level10Crops;
        public RoomLifePlan Life;
        public readonly List<Fixture> Fixtures=new List<Fixture>();
        // Optional batched visuals never enter the support/collision meshes.
        public readonly List<MeshData> VisualMeshes=new List<MeshData>();
        public readonly List<PreparedTransition> PreparedTransitions=new List<PreparedTransition>();
        public readonly List<SourceFurniturePlacement> SourceSeats=new List<SourceFurniturePlacement>();
        public readonly List<SourceFurniturePlacement> SourceProps=new List<SourceFurniturePlacement>();
        public readonly CorridorGeometry[] Connectors=new CorridorGeometry[2];
        public WaterRegion Region;
        public readonly List<WaterRegion> Regions=new List<WaterRegion>();
    }
    public static class Architecture
    {
        public static RoomGeometry Build(Room r,Layout layout)
        {
            RoomGeometry output=Build(r,layout.Config);
            if(layout.Config.EnableQuietLevels&&!QuietLevelPlanner.IsLevel(r.Environment))
            {foreach(var destination in new[]{LiminalEnvironment.Archive906,LiminalEnvironment.Courtyard188,LiminalEnvironment.Coast48})output.PreparedTransitions.Add(new PreparedTransition(destination,new V3(0,r.Floor,0)));}
            for(int d=0;d<2;d++)if(r.HasExit(d))
            {
                Room neighbour=layout.Neighbour(r,d);
                if(!ConnectedTerrainTopology.Reserved(layout.Config,neighbour.X,neighbour.Z))
                    output.Connectors[d]=Corridors.Build(r,neighbour,d,layout.Config);
            }
            return output;
        }
        public static RoomGeometry Build(Room r,LayoutConfig cfg)
        {
            if(r.Plan?.Level10!=null)return Level10Architecture.Build(r,cfg);
            if(r.Plan?.Forest39!=null)return Forest39Architecture.Build(r,cfg);
            if(r.Plan?.Quiet?.Sky63!=null)return Sky63Architecture.Build(r,cfg);
            if(r.Plan?.Quiet?.Conservatory!=null)return ConservatoryDistrictArchitecture.Build(r,cfg);
            if(r.Plan?.Quiet?.Archive!=null)return ArchiveDistrictArchitecture.Build(r,cfg);
            if(r.Plan?.Quiet?.Courtyard!=null)return CourtyardDistrictArchitecture.Build(r,cfg);
            RoomGeometry output=BuildBase(r,cfg);
            RoomPrograms.Append(output,r,cfg);
            ExistingLevelEnrichment.Append(output,r,cfg);
            output.Dressing=ArchitecturalDressing.Build(output.Structure,LiminalRegions.Hash(cfg.Seed,r.X,r.Z,0xDEC006u));
            if(r.Plan?.Restful!=null){output.Dressing.First.Theme=output.Dressing.Second.Theme=RestfulLevelPlanner.Theme(r.Environment);}
            if(r.Plan!=null&&r.Plan.Garden!=null)
            {output.Dressing.First.Theme=output.Dressing.Second.Theme=SurfaceTheme.Orangerie;}
            if(r.Plan!=null&&r.Plan.Rain!=null){output.Dressing.First.Theme=output.Dressing.Second.Theme=SurfaceTheme.RainLodge;}
            if(r.Plan!=null&&r.Plan.Aquatic!=null){output.Dressing.First.Theme=output.Dressing.Second.Theme=SurfaceTheme.AquaticPark;}
            if(r.Plan!=null&&r.Plan.Baths!=null){output.Dressing.First.Theme=output.Dressing.Second.Theme=SurfaceTheme.ThermalStone;}
            RegionalSourcePlanner.Append(output,r,cfg);
            if(cfg.EncounterExploration)
            {
                output.Life=RoomLifePlanner.Build(r,output,cfg);
                if(HorizonLevelPlanner.IsLevel(r.Environment))
                {output.Life.Stations=HorizonLevelPlanner.Stations(r,cfg);output.Life.Vignette=HorizonLevelPlanner.Memory(r.Environment);}
                output.Life.Scenery=SceneryPlanner.Build(r,output,cfg);
            }
            return output;
        }
        private static RoomGeometry BuildBase(Room r,LayoutConfig cfg)
        {
            if(r.TerrainGateway)return ConnectedTerrainArchitecture.Gateway(r,cfg);
            if(r.MallGateway)
            {
                var gateway=MallArchitectureV20.Gateway(r,cfg);
                gateway.Structure.Box(new V3(-r.Width*.5f,r.Height,-r.Depth*.5f),new V3(r.Width*.5f,r.Height+.2f,r.Depth*.5f),Surface.Plaster,cfg.TextureWorldSize);
                return gateway;
            }
            if(r.Plan!=null){var generated=ProceduralArchitecture.Build(r,cfg);AutonomousArchitecture.Append(generated,r,cfg);ZoneLighting.Apply(generated,r,cfg);return generated;}
            if(r.ZoneId>=0){var zone=ZoneArchitecture.Build(r,cfg);ZoneLighting.Apply(zone,r,cfg);return zone;}
            var output=new RoomGeometry();var m=output.Structure;
            float w=r.Width,d=r.Depth,h=r.Height,uv=cfg.TextureWorldSize,t=cfg.WallThickness;
            int n=Math.Max(3,cfg.CurveSegments/4);
            if(r.IsDeep) Basin(output,r,cfg,n);
            else
            {
                m.Box(new V3(-w*.5f,r.Floor-.25f,-d*.5f),new V3(w*.5f,r.Floor,d*.5f),Surface.Tile,uv);
                output.Region=new WaterRegion{Width=w,Depth=d,Radius=0,Bottom=r.Floor,Level=cfg.WaterLevel};
                Shapes.Fill(output.Water,Shapes.RoundedRect(w,d,0,n),cfg.WaterLevel,Surface.Tile,uv);
            }
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();bool ns=side%2==0;float width=ns?w:d;
                if(r.HasExit(side)) Shapes.Portal(wall,width,h,r.Floor,cfg.DoorWidth,cfg.DoorHeight,t,cfg.CurveSegments/2,uv);
                else wall.Box(new V3(-width*.5f,r.Floor,-t*.5f),new V3(width*.5f,h,t*.5f),Surface.Tile,uv);
                wall.TranslateRotate(new V3(Directions.DX[side]*w*.5f,0,Directions.DZ[side]*d*.5f),side*Shapes.Pi*.5f);m.Append(wall);
            }
            Roof(output,r,cfg);
            switch(r.Kind)
            {
                case RoomKind.Colonnade:
                    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    {
                        float radius=.85f+(r.DetailSeed%4)*.12f;
                        Shapes.Column(m,x*w*.24f,z*d*.24f,radius,r.Floor,h,cfg.CurveSegments,Surface.Tile,uv);
                        Shapes.Column(m,x*w*.24f,z*d*.24f,radius+.09f,r.Floor,r.Floor+.18f,cfg.CurveSegments,Surface.Accent,uv);
                    }
                    break;
                case RoomKind.Arcade:
                    for(int k=-1;k<=1;k+=2)
                    {
                        var arch=new MeshData();Shapes.Portal(arch,w-1,h,r.Floor,7.5f,5.8f,.7f,cfg.CurveSegments,uv);
                        arch.TranslateRotate(new V3(0,0,k*d*.23f),0);m.Append(arch);
                    }
                    break;
                case RoomKind.Rotunda:
                    for(int quadrant=0;quadrant<4;quadrant++)
                    {
                        var path=new V2[n+1];
                        for(int j=0;j<=n;j++){float a=(quadrant*90+22+j*46f/n)*Shapes.Pi/180;path[j]=new V2(Shapes.Cos(a)*6.6f,Shapes.Sin(a)*6.6f);}
                        Shapes.Partition(m,path,.65f,r.Floor,h,Surface.Tile,uv);
                    }
                    break;
                case RoomKind.Meander:
                    for(int side=-1;side<=1;side+=2)
                    {
                        int count=cfg.CurveSegments;var path=new V2[count+1];
                        for(int j=0;j<=count;j++){float z=(j/(float)count-.5f)*d*.64f;path[j]=new V2(side*3.1f+Shapes.Sin(z*.42f)*1.8f,z);}
                        Shapes.Partition(m,path,.45f,r.Floor,h,Surface.Tile,uv);
                    }
                    break;
                case RoomKind.Lagoon:
                    Benches(m,r,uv);break;
                case RoomKind.Atrium:
                    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                        Shapes.Column(m,x*7,z*7,1.1f,0,h,cfg.CurveSegments,Surface.Tile,uv);
                    // A bridge across the stepped basin, wide enough for the explorer.
                    m.Box(new V3(-w*.5f,-.2f,-1.2f),new V3(w*.5f,.12f,1.2f),Surface.Accent,uv);
                    for(int x=-1;x<=1;x+=2) Shapes.Column(m,x*3,0,.45f,-1.25f,-.2f,cfg.CurveSegments/2,Surface.Tile,uv);
                    break;
                case RoomKind.Waterpark:
                    Slides(m,r,cfg);break;
            }
            output.Regions.Add(output.Region);
            ZoneLighting.Apply(output,r,cfg);
            return output;
        }
        private static void Basin(RoomGeometry o,Room r,LayoutConfig cfg,int n)
        {
            float w=r.Kind==RoomKind.Atrium?12:13,d=r.Kind==RoomKind.Waterpark?14:12;
            float radius=r.Kind==RoomKind.Atrium?5.8f:2.8f,uv=cfg.TextureWorldSize;
            var contour=Shapes.RoundedRect(w,d,radius,n);var outer=Shapes.RoundedRect(r.Width,r.Depth,0,n);
            Shapes.Ring(o.Structure,outer,contour,0,Surface.Tile,uv);
            Shapes.Skirt(o.Structure,outer,-1.5f,0,Surface.Tile,uv);
            Shapes.Fill(o.Structure,outer,-1.5f,Surface.Tile,uv,false);
            float previousY=0;
            for(int step=0;step<4;step++)
            {
                float nextY=-(step+1)*.30f;
                Shapes.Skirt(o.Structure,contour,nextY,previousY,Surface.Accent,uv,true);
                var next=Shapes.RoundedRect(w-(step+1)*.9f,d-(step+1)*.9f,Math.Max(.4f,radius-(step+1)*.45f),n);
                Shapes.Ring(o.Structure,contour,next,nextY,Surface.Accent,uv);
                contour=next;previousY=nextY;
            }
            Shapes.Fill(o.Structure,contour,previousY,Surface.Accent,uv);
            Shapes.Fill(o.Water,Shapes.RoundedRect(w,d,radius,n),cfg.WaterLevel,Surface.Tile,uv);
            o.Region=new WaterRegion{Width=w,Depth=d,Radius=radius,Bottom=previousY,Level=cfg.WaterLevel};
        }
        private static void Roof(RoomGeometry o,Room r,LayoutConfig c)
        {
            float w=r.Width*.5f,d=r.Depth*.5f,h=r.Height;
            // Offset rectangular aperture; it never intersects boundary walls.
            bool sky=r.Kind!=RoomKind.Arcade && r.Kind!=RoomKind.Waterpark;
            float x0=-6,x1=-2,z0=2,z1=7,th=.25f;
            if(sky)
            {
                o.Structure.Box(new V3(-w,h,-d),new V3(x0,h+th,d),Surface.Plaster,c.TextureWorldSize);
                o.Structure.Box(new V3(x1,h,-d),new V3(w,h+th,d),Surface.Plaster,c.TextureWorldSize);
                o.Structure.Box(new V3(x0,h,-d),new V3(x1,h+th,z0),Surface.Plaster,c.TextureWorldSize);
                o.Structure.Box(new V3(x0,h,z1),new V3(x1,h+th,d),Surface.Plaster,c.TextureWorldSize);
                o.Structure.Box(new V3(x0,h+.27f,z0),new V3(x1,h+.3f,z1),Surface.Glow);
                o.Fixtures.Add(new Fixture{Position=new V3(-4,h-.12f,4.5f),Width=4,Depth=5,Power=9500,Temperature=5600,Skylight=true});
            }
            else o.Structure.Box(new V3(-w,h,-d),new V3(w,h+th,d),Surface.Plaster,c.TextureWorldSize);
            for(int k=-1;k<=1;k+=2)
            {
                float x=sky?4:k*5,z=sky?k*5:0;
                o.Structure.Box(new V3(x-1.1f,h-.12f,z-.18f),new V3(x+1.1f,h-.08f,z+.18f),Surface.Glow);
                o.Fixtures.Add(new Fixture{Position=new V3(x,h-.2f,z),Width=2.2f,Depth=.36f,Power=sky?2200:6500,Temperature=r.Kind==RoomKind.Arcade?7000:4700});
            }
        }
        private static void Benches(MeshData m,Room r,float uv)
        {
            for(int side=-1;side<=1;side+=2)
            {
                var p=Shapes.RoundedRect(1.4f,6,.4f,6);var bench=new MeshData();
                Shapes.Skirt(bench,p,0,.48f,Surface.Tile,uv,false,true);Shapes.Fill(bench,p,.48f,Surface.Tile,uv);
                bench.TranslateRotate(new V3(side*(r.Width*.5f-1.1f),0,4),0);m.Append(bench);
            }
        }
        private static void Slides(MeshData m,Room r,LayoutConfig c)
        {
            for(int side=-1;side<=1;side+=2)
            {
                V3 a=new V3(side*5,4,6),b=new V3(side*8,3.8f,3),cc=new V3(side*3,.5f,-1),d=new V3(side*4,.1f,-4.5f);
                Shapes.Slide(m,a,b,cc,d,.85f,c.CurveSegments*2);
                for(int k=1;k<=3;k++)
                {
                    V3 p=Shapes.Bezier(a,b,cc,d,k/4f);
                    Shapes.Column(m,p.X,p.Z,.13f,-1.2f,p.Y-.1f,12,Surface.Metal,c.TextureWorldSize);
                }
                float platformMin=side<0?-9.4f:3.8f,platformMax=side<0?-3.8f:9.4f;
                m.Box(new V3(platformMin,3.75f,6),new V3(platformMax,4,8.2f),Surface.Accent,c.TextureWorldSize);
                Shapes.Column(m,side*8.8f,7.4f,.16f,0,3.75f,12,Surface.Metal,c.TextureWorldSize);
                // Side flights meet the platform edge, never pass underneath its low slab.
                for(int step=0;step<20;step++)
                {
                    float z=-1+step*.35f,x=side*8.8f;
                    m.Box(new V3(x-.6f,0,z),new V3(x+.6f,(step+1)*.2f,z+.35f),Surface.Tile,c.TextureWorldSize);
                }
            }
        }
        // Each room owns its links towards north/east; south/west links belong to its neighbours.
        public static MeshData Corridor(Room from,Room to,int direction,LayoutConfig c)
        {
            return Corridors.Build(from,to,direction,c).Structure;
        }
    }
}
