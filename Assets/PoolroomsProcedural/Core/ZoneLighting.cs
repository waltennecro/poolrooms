using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum LightingDistribution { ZoneDefaults, Seeded, Uniform, Classic, Composed }
    public enum ZoneLightStyle { SoftDaylight, CleanWhite, WarmAmber, BlueDusk, CyanDepth,
        SparseFluorescent, HalfPower, EmergencyGuides, SunsetRose, MoonShaft }
    public readonly struct LightingLook
    {
        public readonly string Name;
        public readonly V3 Color;
        public readonly float Power,Emission,Range;
        public LightingLook(string name,V3 color,float power,float emission,float range)
        {Name=name;Color=color;Power=power;Emission=emission;Range=range;}
    }
    public static class ZoneLighting
    {
        public const int Count=10;
        public static LightingLook Get(ZoneLightStyle style)
        {
            switch(style)
            {
                case ZoneLightStyle.SoftDaylight:return new LightingLook("Luz cenital suave",new V3(.93f,.97f,1),1.35f,1,18);
                case ZoneLightStyle.CleanWhite:return new LightingLook("Blanco bien iluminado",new V3(1,.97f,.9f),1.65f,1.15f,17);
                case ZoneLightStyle.WarmAmber:return new LightingLook("Ámbar cálido",new V3(1,.63f,.29f),.68f,.65f,13);
                case ZoneLightStyle.BlueDusk:return new LightingLook("Azul tenue",new V3(.13f,.32f,1),.22f,.3f,10);
                case ZoneLightStyle.CyanDepth:return new LightingLook("Cian profundo",new V3(.12f,.8f,.84f),.35f,.42f,11);
                case ZoneLightStyle.SparseFluorescent:return new LightingLook("Fluorescentes aislados",new V3(.75f,1,.75f),.38f,.5f,10);
                case ZoneLightStyle.HalfPower:return new LightingLook("Iluminación parcial",new V3(.84f,.89f,1),.4f,.5f,11);
                case ZoneLightStyle.EmergencyGuides:return new LightingLook("Oscuridad con una luz guía",new V3(.52f,.68f,1),.07f,.12f,12);
                case ZoneLightStyle.SunsetRose:return new LightingLook("Atardecer rosado",new V3(1,.55f,.44f),.78f,.72f,15);
                case ZoneLightStyle.MoonShaft:return new LightingLook("Pozo de luz fría",new V3(.4f,.6f,1),.48f,.55f,15);
                default:throw new ArgumentOutOfRangeException(nameof(style));
            }
        }
        public static ZoneLightStyle DefaultFor(int zone)
        {
            // Balanced, authored alternation across old and new families; IDs remain stable.
            int family=zone<120?zone/6:20+(zone-120)/5;
            int variant=zone<120?zone%6:(zone-120)%5;
            return (ZoneLightStyle)((family*3+variant*7)%Count);
        }
        public static ZoneLightStyle Resolve(Room room,LayoutConfig c)
        {
            if(room.Plan!=null&&room.Plan.Garden!=null)return room.Plan.Garden.Hall==GardenHall.ReadingGarden?ZoneLightStyle.WarmAmber:ZoneLightStyle.SoftDaylight;
            if(room.Plan!=null&&room.Plan.Quiet!=null)return room.Environment==LiminalEnvironment.Archive906?ZoneLightStyle.WarmAmber:room.Environment==LiminalEnvironment.Coast48?ZoneLightStyle.SunsetRose:ZoneLightStyle.CleanWhite;
            if(room.Plan!=null&&room.Plan.Solar!=null)return SolarClimateRegions.LightStyle(room.Plan.Solar.Climate);
            if(room.Plan!=null&&room.Plan.SoftPlay!=null)return room.Plan.SoftPlay.Lighting==1?ZoneLightStyle.WarmAmber:ZoneLightStyle.CleanWhite;
            if(c.IndependentBiomeMaterials&&room.Environment==LiminalEnvironment.IndoorPlay)
                return LiminalRegions.Hash(c.Seed,room.X/Math.Max(3,c.EnvironmentRegionSize),room.Z/Math.Max(3,c.EnvironmentRegionSize),0x2010u)%3==0?ZoneLightStyle.WarmAmber:ZoneLightStyle.SoftDaylight;
            if(c.MallDistricts&&room.Environment==LiminalEnvironment.Supermarket)return ZoneLightStyle.SoftDaylight;
            if(c.Lighting==LightingDistribution.Uniform)return c.UniformLighting;
            if(room.Environment==LiminalEnvironment.Supermarket&&room.Plan!=null&&c.Lighting!=LightingDistribution.Classic)return RetailLighting.Style(room,c);
            if(room.Plan!=null&&room.Plan.Interior!=null&&c.Lighting!=LightingDistribution.Classic)return InteriorPlanner.Lighting(room,c);
            if(c.Lighting==LightingDistribution.Composed)return SpaceComposition.Lighting(room,c);
            if(c.Lighting==LightingDistribution.Seeded)
                return (ZoneLightStyle)((c.Continuous?StableRandom.Hash(c.Seed,room.X,room.Z,0x11A7u):StableRandom.Hash(c.Seed,room.Id,room.ZoneId,0x11A7u))%Count);
            if(room.Plan!=null)return (ZoneLightStyle)(LiminalRegions.Hash(c.Seed,room.X,room.Z,0x1647u)%Count);
            return room.ZoneId>=0?DefaultFor(room.ZoneId):(ZoneLightStyle)(((int)room.Kind*3+room.Id)%Count);
        }
        public static void Apply(RoomGeometry geometry,Room room,LayoutConfig c)
        {
            if(room.Plan!=null&&room.Plan.Garden!=null)return;
            if(room.Plan!=null&&room.Plan.Quiet!=null)return;
            if(room.Plan!=null&&room.Plan.City!=null)return;
            if(room.Plan!=null&&room.Plan.SoftPlay!=null)return;
            if(room.Plan!=null&&room.Plan.Solar!=null)return; // SolarAtlas authors lamp and sunlight powers separately.
            if(c.Lighting==LightingDistribution.Classic)return;
            ZoneLightStyle style=Resolve(room,c);LightingLook look=PoolroomLighting.Look(room,c);
            var fixtures=geometry.Fixtures;int ordinary=0;bool any=false;
            for(int i=0;i<fixtures.Count;i++)
            {
                Fixture f=fixtures[i];int k=f.Skylight?i:ordinary++;
                bool on=Enabled(style,f.Skylight,k);
                f.Power*=on?look.Power:0;f.LightColor=look.Color;f.CustomColor=true;f.Range=look.Range;
                fixtures[i]=f;any|=f.Power>0;
            }
            // Some roof arrangements have no skylights; keep one real source for every atmosphere.
            if(!any&&fixtures.Count>0)
            {
                int index=fixtures.FindIndex(f=>!f.Skylight);if(index<0)index=0;
                Fixture f=fixtures[index];f.Power=1200*look.Power;fixtures[index]=f;
            }
            if(c.Lighting==LightingDistribution.Composed&&room.InvitationDirection>=0)
            {
                // Gently emphasize one existing powered source nearest the next open doorway.
                int d=room.InvitationDirection,nearest=-1;float best=float.MaxValue;
                V3 target=new V3(Directions.DX[d]*room.Width*.5f,0,Directions.DZ[d]*room.Depth*.5f);
                for(int i=0;i<fixtures.Count;i++)
                {
                    Fixture f=fixtures[i];if(f.Power<=0)continue;
                    float dx=f.Position.X-target.X,dz=f.Position.Z-target.Z,distance=dx*dx+dz*dz;
                    if(distance<best){best=distance;nearest=i;}
                }
                if(nearest>=0){Fixture f=fixtures[nearest];f.Power*=1.2f;fixtures[nearest]=f;}
            }
            geometry.Structure.EmissionTint=look.Color;geometry.Structure.EmissionStrength=look.Emission;
            MatchPanels(geometry.Structure,fixtures);
            if(room.Environment==LiminalEnvironment.Poolrooms&&PoolroomLighting.IsDim(style))
                PoolroomLighting.AddSupport(fixtures,room,look);
        }
        private static bool Enabled(ZoneLightStyle style,bool skylight,int i)
        {
            switch(style)
            {
                case ZoneLightStyle.BlueDusk:case ZoneLightStyle.CyanDepth:return !skylight;
                case ZoneLightStyle.SparseFluorescent:return !skylight&&i%2==0;
                case ZoneLightStyle.HalfPower:return !skylight&&i%2==1;
                case ZoneLightStyle.EmergencyGuides:return !skylight&&i==0;
                case ZoneLightStyle.MoonShaft:return skylight&&i%2==0;
                default:return true;
            }
        }
        public static void Corridor(CorridorGeometry geometry,Room from,Room to,LayoutConfig c)
        {
            if(c.Lighting==LightingDistribution.Classic)return;
            LightingLook a=PoolroomLighting.Look(from,c),b=PoolroomLighting.Look(to,c);
            var lamps=geometry.Fixtures;
            for(int i=0;i<lamps.Count;i++)
            {
                float t=(i+.5f)/lamps.Count;
                if(c.Lighting==LightingDistribution.Composed)t=SpaceComposition.Smooth(t);
                Fixture f=lamps[i];
                f.Power*=a.Power+(b.Power-a.Power)*t;f.LightColor=a.Color*(1-t)+b.Color*t;
                f.CustomColor=true;f.Range=a.Range+(b.Range-a.Range)*t;lamps[i]=f;
            }
            geometry.Structure.EmissionTint=(a.Color+b.Color)*.5f;
            geometry.Structure.EmissionStrength=(a.Emission+b.Emission)*.5f;
        }
        private static void MatchPanels(MeshData mesh,List<Fixture> fixtures)
        {
            var glow=mesh.Indices[(int)Surface.Glow];var off=mesh.Indices[(int)Surface.Plaster];
            var kept=new List<int>(glow.Count);
            for(int i=0;i<glow.Count;i+=3)
            {
                V3 p=(mesh.Positions[glow[i]]+mesh.Positions[glow[i+1]]+mesh.Positions[glow[i+2]])*(1f/3);
                int nearest=-1;float best=float.MaxValue;
                for(int j=0;j<fixtures.Count;j++)
                {
                    V3 d=p-fixtures[j].Position;float distance=V3.Dot(d,d);
                    if(distance<best){best=distance;nearest=j;}
                }
                var target=nearest>=0&&fixtures[nearest].Power>0?kept:off;
                target.Add(glow[i]);target.Add(glow[i+1]);target.Add(glow[i+2]);
            }
            // Keep all panel geometry: unpowered panels become non-emissive, never holes.
            glow.Clear();glow.AddRange(kept);
        }
    }
}
