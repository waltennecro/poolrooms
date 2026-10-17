using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum SolarDetailKind { Bench,Lounger,Locker,Shower,Rail,Ladder,LifeRing,Drain,ServiceCupboard,Stool,TileTable,SlattedScreen,StairTerrace,AccessibleRamp,Arch,Niche,RoundColumn,SquarePier,Vault,DoublePortal }
    public readonly struct SolarDetail
    {
        public readonly SolarDetailKind Kind;
        public readonly PlanRect Bounds;
        public readonly float Rise;
        public readonly int Turns;
        public readonly uint Seed;
        public SolarDetail(SolarDetailKind kind,PlanRect bounds,float rise,int turns,uint seed){Kind=kind;Bounds=bounds;Rise=rise;Turns=turns;Seed=seed;}
    }
    public readonly struct SolarAperture
    {
        // Side -1 = horizontal roof opening. For a wall, Rect's Z coordinates denote height.
        public readonly int Side;
        public readonly PlanRect Rect;
        public readonly bool Louvers;
        public SolarAperture(int side,PlanRect rect,bool louvers){Side=side;Rect=rect;Louvers=louvers;}
    }
    public sealed class SolarRoomPlan
    {
        public readonly int Recipe;
        public readonly float OutlineRadius;
        public readonly bool Chamfered;
        public readonly PoolClimate Climate;
        public readonly PoolClimateDescription ClimateDescription;
        public readonly SolarDetail[] Details;
        public readonly SolarAperture[] Apertures;
        public readonly SolarConnectionKind[] Connections;
        public SolarRoomPlan(int recipe,PoolClimateDescription climate,SolarDetail[] details,SolarAperture[] apertures,SolarConnectionKind[] connections,float outlineRadius=0,bool chamfered=false)
        {OutlineRadius=outlineRadius;Chamfered=chamfered;Recipe=recipe;Climate=climate.Kind;ClimateDescription=climate;Details=details;Apertures=apertures;Connections=connections;}
        public bool Valid(ProceduralPlan p,Room r,LayoutConfig c)
        {
            if(Recipe<0||Recipe>=SolarCatalog.Count||r.Environment!=LiminalEnvironment.Poolrooms||p.Circulation==null||!p.Circulation.Valid(r,c)||p.Pools.Length==0||p.Pools.Length>6||Details.Length>14)return false;
            if(r.Width>c.CellSize||r.Depth>c.CellSize||r.Width<16||r.Depth<16||r.Height<c.DoorHeight+1||r.Height>15)return false;
            var occupied=new List<PlanRect>();
            foreach(WaterRegion pool in p.Pools)
            {
                PlanRect rect=new PlanRect(pool.CenterX-pool.Width*.5f,pool.CenterZ-pool.Depth*.5f,pool.CenterX+pool.Width*.5f,pool.CenterZ+pool.Depth*.5f);
                if(pool.Width<2.4f||pool.Depth<2.4f||pool.Bottom>=c.WaterLevel||pool.Level!=c.WaterLevel||pool.Radius<0||pool.Radius>Math.Min(pool.Width,pool.Depth)*.5f||!Fits(rect,r,p))return false;
                int steps=(int)Math.Round(-pool.Bottom/.21f);if(steps<1||Math.Min(pool.Width,pool.Depth)-steps*.6f<1.2f)return false;
                foreach(PlanRect old in occupied)if(old.Overlaps(rect,.1f))return false;occupied.Add(rect);
            }
            foreach(SolarDetail detail in Details)
            {
                if(!Fits(detail.Bounds,r,p)||detail.Bounds.Width<.4f||detail.Bounds.Depth<.4f||detail.Rise>r.Height-2.2f)return false;
                foreach(PlanRect old in occupied)if(old.Overlaps(detail.Bounds,.1f))return false;occupied.Add(detail.Bounds);
            }
            foreach(SolarAperture aperture in Apertures)
            {
                if(!SolarClimateRegions.HasSky(Climate)||aperture.Rect.Width<=0||aperture.Rect.Depth<=0)return false;
                if(aperture.Side<0)
                {if(Math.Abs(aperture.Rect.X)+aperture.Rect.Width*.5f>r.Width*.5f-.25f||Math.Abs(aperture.Rect.Z)+aperture.Rect.Depth*.5f>r.Depth*.5f-.25f)return false;
                 foreach(PlanRect cut in p.Circulation.Cuts)if(cut.Overlaps(aperture.Rect,.3f))return false;}
                else
                {float extent=(aperture.Side%2==0?r.Width:r.Depth)*.5f;if(Math.Abs(aperture.Rect.X)+aperture.Rect.Width*.5f>extent-.3f||aperture.Rect.MinZ<1.2f||aperture.Rect.MaxZ>r.Height-.35f||r.HasExit(aperture.Side))return false;}
            }
            return SolarClimateRegions.HasSky(Climate)?Apertures.Length>0:Apertures.Length==0;
        }
        private static bool Fits(PlanRect rect,Room r,ProceduralPlan p)
        {return rect.MinX>-r.Width*.5f+.35f&&rect.MaxX<r.Width*.5f-.35f&&rect.MinZ>-r.Depth*.5f+.35f&&rect.MaxZ<r.Depth*.5f-.35f&&p.Circulation.Fits(rect,.15f)&&SolarOutline.Contains(r,p.Solar,rect.MinX,rect.MinZ)&&SolarOutline.Contains(r,p.Solar,rect.MaxX,rect.MinZ)&&SolarOutline.Contains(r,p.Solar,rect.MinX,rect.MaxZ)&&SolarOutline.Contains(r,p.Solar,rect.MaxX,rect.MaxZ);}
    }
    public static class SolarPlanner
    {
        public static void Describe(Room r,LayoutConfig c,int forcedRecipe=-1)
        {
            int recipe=forcedRecipe<0?SolarCatalog.At(c,r.X,r.Z):forcedRecipe;SolarRecipe design=SolarCatalog.Get(recipe);
            var random=new StableRandom(LiminalRegions.Hash(c.Seed,r.X,r.Z,0x1920u));
            r.ZoneId=-1;r.DetailSeed=random.Next();
            float max=c.CellSize-3.5f,min=Math.Min(19,c.CellSize-8);
            r.Width=random.Range(min,max);r.Depth=random.Range(min,max);
            if(design.SpatialVariant==0)r.Width=Math.Max(r.Width,max*.94f);
            if(design.SpatialVariant==1)r.Depth=Math.Max(r.Depth,max*.94f);
            var edges=new SolarConnectionKind[4];
            for(int d=0;d<4;d++)
            {
                edges[d]=SolarConnections.At(c,r.X,r.Z,d);
                if(!r.HasExit(d)||!SolarConnections.IsDirect(edges[d]))continue;
                if(d%2==0)r.Depth=c.CellSize;else r.Width=c.CellSize;
            }
            r.Height=design.SpatialVariant==3?random.Range(10,14.5f):random.Range(Math.Max(6.3f,c.DoorHeight+1),9.8f);
            PoolClimateDescription climate=SolarClimateRegions.At(c,r.X,r.Z);
            for(int attempt=0;attempt<10;attempt++)
            {
                if(attempt==5)
                {
                    // Rounded corners and four broad mouths can consume the small sampled
                    // footprint. Expand within this cell and retry the same recipe before
                    // sacrificing pools or narrowing a connection shared with its neighbour.
                    r.Width=Math.Max(r.Width,max);r.Depth=Math.Max(r.Depth,max);
                }
                var rng=new StableRandom(LiminalRegions.Hash(c.Seed,r.X,r.Z,0x1930u+(uint)attempt));
                PlanCirculation circulation=Circulation(r,c,design,ref rng,attempt);
                var plan=Make(r,c,design,climate,circulation,edges,ref rng);
                if(plan.Valid(r,c)){r.Plan=plan;return;}
            }
            throw new InvalidOperationException("Solar atlas could not preserve circulation at "+r.X+","+r.Z+" (seed "+c.Seed+", recipe "+recipe+")");
        }
        private static PlanCirculation Circulation(Room r,LayoutConfig c,SolarRecipe design,ref StableRandom rng,int attempt)
        {
            var p=PlanCirculation.Create(r,c,ref rng,attempt>=3?3:attempt,design.SpatialVariant==1?2:design.SpatialVariant==2?3:1);
            // Make broad shared room openings safe over their complete mouth, not just the old 4 m portal.
            var reserved=new List<PlanRect>(p.Reserved);
            for(int d=0;d<4;d++)if(r.HasExit(d))
            {
                float half=SolarConnections.Width(c,r.X,r.Z,d)*.5f+.65f,hx=r.Width*.5f,hz=r.Depth*.5f;
                reserved.Add(d%2==0?new PlanRect(-half,d==0?hz-3:-hz,half,d==0?hz:-hz+3):new PlanRect(d==1?hx-3:-hx,-half,d==1?hx:-hx+3,half));
            }
            var cuts=new List<PlanRect>();
            foreach(PlanRect cut in p.Cuts){bool fits=true;foreach(PlanRect path in reserved)if(path.Overlaps(cut,.2f)){fits=false;break;}if(fits)cuts.Add(cut);}
            // Space variants alter the usable footprint: gallery versus clipped court versus tall central hall.
            if(design.SpatialVariant!=1)cuts.Clear();
            return new PlanCirculation(p.Layout,p.Routes,reserved.ToArray(),cuts.ToArray());
        }
        private static ProceduralPlan Make(Room r,LayoutConfig c,SolarRecipe design,PoolClimateDescription climate,PlanCirculation circulation,SolarConnectionKind[] edges,ref StableRandom rng)
        {
            var bounds=new PlanRect(-r.Width*.5f+.8f,-r.Depth*.5f+.8f,r.Width*.5f-.8f,r.Depth*.5f-.8f);
            var blocked=new List<PlanRect>();foreach(PlanRect q in circulation.Reserved)blocked.Add(q.Expanded(.3f));foreach(PlanRect q in circulation.Cuts)blocked.Add(q.Expanded(.5f));
            float outlineRadius=SolarOutline.Radius(r,design,c);
            if(outlineRadius>0)for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
            {float hx=r.Width*.5f,hz=r.Depth*.5f;blocked.Add(new PlanRect(sx<0?-hx:hx-outlineRadius,sz<0?-hz:hz-outlineRadius,sx<0?-hx+outlineRadius:hx,sz<0?-hz+outlineRadius:hz));}
            var bays=VariedPlanner.Bays(bounds,blocked);bays.Sort((a,b)=>(b.Width*b.Depth).CompareTo(a.Width*a.Depth));
            var pools=new List<WaterRegion>();var details=new List<SolarDetail>();
            int desired=c.AutonomousGeneration?1+rng.Range(6):1+design.PoolFamily%4;
            for(int i=0;i<bays.Count;i++)
            {
                PlanRect bay=bays[i];float w=bay.Width-.9f,d=bay.Depth-.9f;if(w<2.8f||d<2.8f)continue;
                if(pools.Count<desired)
                {
                    int form=c.AutonomousGeneration?rng.Range(6):design.PoolFamily%6;
                    if(form==2||form==3){if(w>d)d=Math.Max(2.8f,d*(form==2?.5f:.7f));else w=Math.Max(2.8f,w*(form==2?.5f:.7f));}
                    if(form==4){w*=.85f;d*=.85f;}
                    if(design.PoolFamily>=12){w=Math.Max(2.8f,w*.92f);d=Math.Max(2.8f,d*.96f);}
                    w=Math.Min(w,design.PoolFamily>=12?19:24);d=Math.Min(d,design.PoolFamily>=12?22:24);
                    if(c.AutonomousGeneration){w=Math.Max(2.8f,w*rng.Range(.65f,1));d=Math.Max(2.8f,d*rng.Range(.65f,1));}
                    int cap=Math.Min(7,(int)((Math.Min(w,d)-1.3f)/.6f));
                    if(cap<1)continue;int steps=1+(design.PoolFamily/4+rng.Range(cap))%cap;
                    float radius=form==5?Math.Min(w,d)*.48f:form==0?Math.Min(w,d)*.2f:0;
                    pools.Add(new WaterRegion{CenterX=bay.X,CenterZ=bay.Z,Width=w,Depth=d,Radius=radius,Bottom=-steps*.21f,Level=c.WaterLevel});
                    // Furnish a real dry strip adjacent to this basin, when it does not obstruct the protected route.
                    float strip=bay.Depth-d;
                    if(strip>2.1f)
                    {
                        var q=new PlanRect(bay.X-Math.Min(w*.5f,2.6f),bay.MaxZ-strip*.5f+.15f,bay.X+Math.Min(w*.5f,2.6f),bay.MaxZ-.35f);
                        AddDetail(details,q,c.AutonomousGeneration?rng.Range(8):design.Structure,ref rng,true);
                    }
                }
                else
                {
                    var q=new PlanRect(bay.MinX+.5f,bay.MinZ+.5f,bay.MaxX-.5f,bay.MaxZ-.5f);
                    // Architectural elements and furniture share bounded bays but never overlap each other.
                    if(q.Width>=2.1f&&q.Depth>=2.1f)AddDetail(details,q,c.AutonomousGeneration?rng.Range(8):design.Structure,ref rng,false);
                }
            }
            // A bounded fallback uses a straighter circulation; it never silently creates a dry empty room.
            var apertures=Apertures(r,climate,circulation,design,ref rng,c.AutonomousGeneration);
            var solar=new SolarRoomPlan(design.Id,climate,details.ToArray(),apertures.ToArray(),edges,outlineRadius,design.SpatialVariant==3);
            return new ProceduralPlan(LiminalEnvironment.Poolrooms,new PlannedModule[0],pools.ToArray(),c.DoorWidth*.5f+.8f,design.Structure,0,false,circulation,null,null,solar);
        }
        private static void AddDetail(List<SolarDetail> result,PlanRect q,int structure,ref StableRandom rng,bool furniture)
        {
            if(result.Count>=14||q.Width<.7f||q.Depth<.7f)return;
            SolarDetailKind kind;
            if(furniture||rng.Value()<.4f)kind=(SolarDetailKind)rng.Range(14);
            else kind=structure==0?SolarDetailKind.RoundColumn:structure==1?SolarDetailKind.Arch:structure==2?SolarDetailKind.SlattedScreen:structure==3?SolarDetailKind.SquarePier:structure==4?SolarDetailKind.Vault:structure==5?SolarDetailKind.DoublePortal:structure==6?SolarDetailKind.Niche:SolarDetailKind.StairTerrace;
            float w=Math.Min(q.Width,7),d=Math.Min(q.Depth,7);q=new PlanRect(q.X-w*.5f,q.Z-d*.5f,q.X+w*.5f,q.Z+d*.5f);
            float rise=(kind==SolarDetailKind.StairTerrace||kind==SolarDetailKind.AccessibleRamp)?Math.Min(1.2f,(d-1.3f)*.35f):.8f;
            if(rise<.3f&&(kind==SolarDetailKind.StairTerrace||kind==SolarDetailKind.AccessibleRamp))kind=SolarDetailKind.Bench;
            result.Add(new SolarDetail(kind,q,rise,rng.Range(4),rng.Next()));
        }
        private static List<SolarAperture> Apertures(Room r,PoolClimateDescription climate,PlanCirculation circulation,SolarRecipe recipe,ref StableRandom rng,bool autonomous=false)
        {
            var result=new List<SolarAperture>();if(!SolarClimateRegions.HasSky(climate.Kind))return result;
            bool roof=autonomous?rng.Value()<.7f:recipe.PoolFamily%3!=1;
            V3 sun=climate.SunDirection;int side=Math.Abs(sun.X)>Math.Abs(sun.Z)?(sun.X>0?3:1):(sun.Z>0?2:0);
            if(!roof)
            {
                for(int n=0;n<1;n++)
                {
                    int d=side;if(r.HasExit(d))continue;
                    // Non-cut wall segments are tested against the outline before carving.
                    float extent=(d%2==0?r.Width:r.Depth)*.5f,width=Math.Min(10,extent*.9f);
                    float sill=Math.Max(1.6f,r.Height*.38f),top=r.Height-.65f;
                    var q=new PlanRect(-width*.5f,sill,width*.5f,top);bool clear=true;
                    foreach(PlanRect cut in circulation.Cuts)
                    {
                        bool touches=d==0?cut.MaxZ>=r.Depth*.5f-.1f:d==1?cut.MaxX>=r.Width*.5f-.1f:d==2?cut.MinZ<=-r.Depth*.5f+.1f:cut.MinX<=-r.Width*.5f+.1f;
                        if(!touches)continue;float min=d==0?cut.MinX:d==1?-cut.MaxZ:d==2?-cut.MaxX:cut.MinZ;float max=d==0?cut.MaxX:d==1?-cut.MinZ:d==2?-cut.MinX:cut.MaxZ;
                        if(q.MaxX>min-.4f&&q.MinX<max+.4f){clear=false;break;}
                    }
                    if(!clear)continue;result.Add(new SolarAperture(d,q,recipe.Structure%2==0));break;
                }
            }
            if(roof||result.Count==0)
            {
                float w=Math.Min(r.Width*.42f,10),d=Math.Min(r.Depth*.38f,9);
                if(autonomous){w*=rng.Range(.55f,1);d*=rng.Range(.55f,1);}
                var q=new PlanRect(-w*.5f,-d*.5f,w*.5f,d*.5f);
                // Protected central skylight never intersects perimeter outline cuts.
                result.Add(new SolarAperture(-1,q,recipe.Structure%3==0));
                if(recipe.SpatialVariant==3||autonomous&&rng.Value()<.65f)
                {
                    // A smaller offset lantern creates a different overhead composition.
                    float x=r.Width*.28f,z=-r.Depth*.22f;
                    var second=new PlanRect(x-1.2f,z-1.2f,x+1.2f,z+1.2f);bool valid=!q.Overlaps(second,.35f);
                    foreach(PlanRect cut in circulation.Cuts)valid&=!cut.Overlaps(second,.35f);
                    if(valid)result.Add(new SolarAperture(-1,second,false));
                }
            }
            return result;
        }
    }
}
