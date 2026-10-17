using System;

namespace Poolrooms.Core
{
    // Appended settings are opt-in: old seeds retain their original layouts with None.
    public enum SpatialComposition { None, Exploration }
    public enum SpatialRole { Free, Arrival, Compression, Reveal, Rest, FramedView }

    public static class SpaceComposition
    {
        private static readonly SpatialRole[] architecturalRoles={SpatialRole.Compression,SpatialRole.Reveal,SpatialRole.Rest,SpatialRole.FramedView};
        public static void Apply(Layout layout)
        {
            if(layout.Config.Composition==SpatialComposition.None)return;
            // Classify what was randomly generated. Never move recipes into a prescribed role.
            foreach(Room r in layout.Rooms)Describe(r);
            foreach(Room r in layout.Rooms)Invite(r,layout);
        }

        public static void Describe(Room room)
        {
            room.RouteDepth=-1;room.ArrivalDirection=-1;room.InvitationDirection=-1;
            room.Role=SpatialRole.Free;
            if(room.Plan!=null)
            {
                room.Role=room.Environment==LiminalEnvironment.Supermarket?SpatialRole.FramedView:room.Environment==LiminalEnvironment.IndoorPlay?SpatialRole.FramedView:
                    room.Plan.Pools.Length>=3?SpatialRole.Reveal:SpatialRole.Rest;
                return;
            }
            if(room.ZoneId<0)
            {
                room.Role=room.Kind==RoomKind.Meander?SpatialRole.Compression:
                    room.Kind==RoomKind.Arcade||room.Kind==RoomKind.Colonnade?SpatialRole.FramedView:
                    room.IsDeep?SpatialRole.Reveal:SpatialRole.Free;
                return;
            }
            float best=0;
            foreach(SpatialRole role in architecturalRoles)
            {
                float score=Suitability(room.ZoneId,role);
                if(score>best){best=score;room.Role=role;}
            }
        }

        public static void Invite(Room room,Layout layout)
        {
            LayoutConfig c=layout.Config;
            room.InvitationDirection=-1;
            // Local glimpses only: no parent tree, main route, destination or increasing depth.
            // Some rooms remain neutral; returning and wandering are equally valid.
            if(StableRandom.Hash(c.Seed,room.X,room.Z,0xC041u)%4==0)return;
            float current=ZoneLighting.Get(Lighting(room,c)).Power,best=float.NegativeInfinity;
            for(int d=0;d<4;d++)
            {
                if(!room.HasExit(d))continue;
                Room next=layout.DescribeAt(room.X+Directions.DX[d],room.Z+Directions.DZ[d]);if(next==null)continue;
                float contrast=ZoneLighting.Get(Lighting(next,c)).Power-current;
                float variation=(StableRandom.Hash(c.Seed,room.X,room.Z,0xC080u+(uint)d)>>8)*(1f/16777216f);
                float score=variation+Math.Max(0,contrast)*.3f;
                if(score>best){best=score;room.InvitationDirection=d;}
            }
        }

        public static float Suitability(int zone,SpatialRole role)
        {
            ZoneRecipe z=ZoneCatalog.Get(zone);ZoneFamily f=z.Family;
            switch(role)
            {
                case SpatialRole.Arrival:return f==ZoneFamily.Enfilades&&z.Pools==PoolPlan.Shallow?20:0;
                case SpatialRole.Reveal:
                    float pool=z.Pools==PoolPlan.TwinLong||z.Pools==PoolPlan.TwinCross||z.Pools==PoolPlan.TwinOvals?6:
                        z.Pools==PoolPlan.Dry?-8:z.Pools==PoolPlan.Shallow?-2:2;
                    return pool+(f==ZoneFamily.Natatoriums||f==ZoneFamily.LightWells||f==ZoneFamily.LanternHalls?7:
                        f==ZoneFamily.Reservoirs||f==ZoneFamily.Terraces?4:0);
                case SpatialRole.Rest:return f==ZoneFamily.QuietLounges?12:f==ZoneFamily.Niches?10:
                    f==ZoneFamily.ThermalCells||f==ZoneFamily.ChangingRooms?7:0;
                case SpatialRole.Compression:return f==ZoneFamily.Meanders||f==ZoneFamily.FilterScreens?12:
                    f==ZoneFamily.SplitGalleries||f==ZoneFamily.ServiceGalleries||f==ZoneFamily.ConcentricPassages?10:0;
                case SpatialRole.FramedView:return f==ZoneFamily.Enfilades||f==ZoneFamily.Cloisters||f==ZoneFamily.Aqueducts?12:
                    f==ZoneFamily.ApsidalBaths||f==ZoneFamily.Hypostyles||f==ZoneFamily.Bridges?8:0;
                default:return 0;
            }
        }

        // Direction-independent: canonical north/east ownership must not reverse the experience.
        // +1 opens toward the owner's far end; -1 opens toward its near end.
        public static int RevealEnd(Room from,Room to,LayoutConfig c)
        {
            if(c.Composition==SpatialComposition.None||c.Corridors!=CorridorStyle.Mixed)return 0;
            if(to.Role==SpatialRole.Reveal&&from.Role==SpatialRole.Compression)return 1;
            if(from.Role==SpatialRole.Reveal&&to.Role==SpatialRole.Compression)return -1;
            return 0;
        }

        public static bool IsPreferred(CorridorStyle style,Room from,Room to,LayoutConfig c)
        {
            if(RevealEnd(from,to,c)!=0)return style==CorridorStyle.Narrow||style==CorridorStyle.Straight;
            if(from.Role==SpatialRole.FramedView||to.Role==SpatialRole.FramedView)
                return style==CorridorStyle.Arcade||style==CorridorStyle.PortalSequence||style==CorridorStyle.Colonnade;
            if(from.Role==SpatialRole.Rest||to.Role==SpatialRole.Rest)return style==CorridorStyle.WideGallery;
            if(from.Role==SpatialRole.Compression||to.Role==SpatialRole.Compression)
                return style==CorridorStyle.Narrow||style==CorridorStyle.Curved||style==CorridorStyle.OffsetScreens;
            return true;
        }

        public static ZoneLightStyle Lighting(Room room,LayoutConfig c)
        {
            if(room.Environment==LiminalEnvironment.Supermarket&&room.Plan!=null)return RetailLighting.Style(room,c);
            if(room.Plan!=null&&room.Plan.Interior!=null)return InteriorPlanner.Lighting(room,c);
            if(c.Composition==SpatialComposition.None)
                return room.ZoneId>=0?ZoneLighting.DefaultFor(room.ZoneId):(ZoneLightStyle)(((int)room.Kind*3+room.Id)%ZoneLighting.Count);
            // Independent per coordinate; pools can be bright, blue, warm or dark.
            uint seed=room.Plan!=null?LiminalRegions.Hash(c.Seed,room.X,room.Z,0x1647u):StableRandom.Hash(c.Seed,room.X,room.Z,0x1647u);
            return (ZoneLightStyle)(seed%ZoneLighting.Count);
        }

        public static float Smooth(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    }
}
