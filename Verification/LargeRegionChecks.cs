using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static LayoutConfig LargeConfig(int seed=481516,int size=36)
    {
        var c=new LayoutConfig{Seed=seed,CellSize=size,Zones=ZoneMode.Random,Grammar=ProceduralGrammar.SolarAtlas,AutonomousGeneration=true,IndependentBiomeMaterials=true,LargeProceduralRegions=true};
        WalkableExploration.Configure(c);c.Validate();return c;
    }
    static void CheckLargeRoute(LayoutConfig c,EncounterRoute route,RouteDestination destination,LiminalEnvironment selected=0)
    {
        Check(route!=null,"Large region walking route missing: "+destination+" / "+selected+" / seed "+c.Seed);
        Check(route.Visited<=LargeRegionRoutes.MaximumVisited,"Large route exceeded bounded search");
        int x=route.StartX,z=route.StartZ;var topology=new ContinuousTopology(c);
        foreach(int side in route.Directions)
        {
            int exits=ConnectedTerrainTopology.Exits(c,x,z,ConnectedMallTopology.Exits(c,x,z,topology.Exits(x,z)));
            Check((exits&(1<<side))!=0,"Large route crosses a sealed edge");
            x+=Directions.DX[side];z+=Directions.DZ[side];Check(!ConnectedTerrainTopology.Reserved(c,x,z),"Large route enters a reserved footprint");
            int back=ConnectedTerrainTopology.Exits(c,x,z,ConnectedMallTopology.Exits(c,x,z,topology.Exits(x,z)));
            Check((back&(1<<Directions.Opposite(side)))!=0,"Large route is not reciprocal");
        }
        Check(x==route.EndX&&z==route.EndZ,"Large route ends in the wrong room");
        if(destination==RouteDestination.SpecificBiome)Check(LiminalRegions.At(c,x,z)==selected,"Large route found a different biome");
        if(destination==RouteDestination.Mall)Check(ConnectedMallTopology.Gateway(c,x,z,out _),"Large mall route has no physical gateway");
        if(destination==RouteDestination.Meadow)Check(ConnectedTerrainTopology.Gateway(c,x,z,out _),"Large meadow route has no physical gateway");
    }
    static void LargeRegionWorld()
    {
        int found=0,rooms=0,paths=0,openPairs=0;long steps=0;var sectionKinds=new HashSet<RegionSection>();
        foreach(int seed in new[]{0,31,481516})
        {
            var c=LargeConfig(seed);var old=LargeConfig(seed);old.LargeProceduralRegions=false;
            Check(Math.Abs(BiomeExtent.RegionSpacing(c)/BiomeExtent.RegionSpacing(old)-8)<1e-8,"Large regions did not enlarge the physical field eightfold");
            for(int x=-20;x<=20;x++)for(int z=-20;z<=20;z++)
            {
                BiomeExtent.NearestRegion(old,x,z,out int ax,out int az);BiomeExtent.NearestRegion(c,x*8,z*8,out int bx,out int bz);
                Check(ax==bx&&az==bz,"Scaling moved the logical region identity");
            }
            foreach(LiminalEnvironment e in Enum.GetValues(typeof(LiminalEnvironment)))
            {
                Check(LiminalRegions.Find(c,e,0,0,out int x,out int z),"Missing large biome "+e);found++;
                var layout=new Layout(c);var room=layout.At(x,z);Check(room.Environment==e&&!room.TerrainGateway&&!room.MallGateway,"Large lookup points to a reserved gateway");
                var g=Architecture.Build(room,layout);Validate(g.Structure);Validate(g.CollisionOnly);Validate(g.Decoration);Validate(g.Glass);Validate(g.Water);
                Check(g.Structure.Positions.Count<300000,"Large region eagerly merged an entire district mesh");rooms++;
                Check(layout.CachedRoomCount<=5,"A large room build cached the whole region");
                sectionKinds.Add(LargeRegions.Section(c,x,z,e));
                if(seed==481516)
                {
                    var route=EncounterRoutes.Find(c,0,0,RouteDestination.SpecificBiome,e);CheckLargeRoute(c,route,RouteDestination.SpecificBiome,e);steps+=route.Directions.Length;paths++;
                }
            }
            foreach(var destination in new[]{RouteDestination.DifferentBiome,RouteDestination.Mall,RouteDestination.Meadow})
            {var route=EncounterRoutes.Find(c,0,0,destination);CheckLargeRoute(c,route,destination);steps+=route.Directions.Length;paths++;}
            Check(ConnectedTerrainTopology.Site(c,0,0,out var meadow)&&!meadow.Contains(0,0),"Large meadow swallowed spawn");
            Check(ConnectedMallTopology.Site(c,0,0,out var mall)&&!mall.Contains(0,0),"Large mall swallowed spawn");
            var plan=TerrainRegionPlan.CreateConnected(c.Seed,meadow,true,true);Check(plan.Width>=1000&&plan.Depth>=1000&&plan.HouseCount>20,"Large meadow is small or empty");
            Check(meadow.HalfWidth==plan.HalfWidth&&meadow.HalfDepth==plan.HalfDepth,"Terrain reservation and generated plan disagree");
            var malls=new List<ConnectedMallSite>();
            for(int bx=-2;bx<=2;bx++)for(int bz=-2;bz<=2;bz++)if(ConnectedMallTopology.Site(c,bx,bz,out var site))
            {
                var p=MallPlan.Create(c.Seed,site.X,site.Z,true,true,true,true,true,true);
                Check(p.Valid()&&p.Bays>=24&&p.SectorCount<32&&p.Width==site.Width&&p.Depth==site.Depth,"Large mall dimensions or sector masks disagree");
                foreach(var prior in malls)Check(Math.Abs((long)prior.X-site.X)>prior.RadiusX+site.RadiusX+2||Math.Abs((long)prior.Z-site.Z)>prior.RadiusZ+site.RadiusZ+2,"Large mall reservations overlap");
                malls.Add(site);Check(!ConnectedTerrainTopology.Intersects(c,site.X,site.Z,site.RadiusX+1,site.RadiusZ+1),"Large mall overlaps meadow");
            }
        }
        // Dedicated modes cover all twenty broad gallery programmes at extreme scales.
        for(int index=0;index<20;index++)foreach(int size in new[]{28,36,60})
        {
            var e=(LiminalEnvironment)(18+index);var c=LargeConfig(481516,size);c.ConnectedMalls=c.ConnectedTerrain=false;
            c.Procedural=index<10?(LiminalGeneration)((int)LiminalGeneration.SunsetLibrary+index):(LiminalGeneration)((int)LiminalGeneration.SolarObservatory+index-10);
            int hx=0,hz=0;bool ready=false;
            for(int sx=-8;sx<=8&&!ready;sx++)for(int sz=-8;sz<=8&&!ready;sz++)if(LargeRegions.Section(c,sx*4,sz*4,e)==RegionSection.Forum){hx=sx*4+1;hz=sz*4+1;ready=true;}
            Check(ready,"Missing broad interior sector");var layout=new Layout(c);var a=layout.At(hx,hz);var b=layout.At(hx+1,hz);
            Check(LargeRegions.OpenEdge(c,hx,hz,1,e)&&LargeRegions.OpenEdge(c,hx+1,hz,3,e)&&a.HasExit(1)&&b.HasExit(3),"Broad hall has an asymmetric opening");
            var ga=Architecture.Build(a,c);var gb=Architecture.Build(b,c);var combined=new MeshData();combined.Append(ga.Structure);var shifted=new MeshData();shifted.Append(gb.Structure);shifted.TranslateRotate(new V3(size,0,0),0);combined.Append(shifted);
            Check(a.Height==b.Height&&a.Height==10.4f,"Continuous hall roofs have mismatched height");
            Check(SolarConnections.DoorWidth(a,1,c)==size-1.2f,"Broad portal width not used by connection queries");
            var seam=Corridors.Build(a,b,1,c);Check(seam.Structure.TriangleCount==0,"Open hall adds a coplanar floor strip");
            for(int lane=-1;lane<=1;lane++)for(int t=-4;t<=4;t++)
            {
                var at=new V3(size*.5f+t*.12f,1.8f,lane*2.5f);
                Check(Math.Abs(Support(combined,at))<.002f&&SphereClear(combined,new V3(at.X,.9f,at.Z),.34f),"Broad hall threshold blocks the player or lacks floor");
            }
            Validate(ga.Structure);Validate(ga.Decoration);Validate(gb.Structure);openPairs++;
        }
        var streamConfig=LargeConfig();var streamLayout=new Layout(streamConfig);var loaded=new Dictionary<long,bool>();
        int before=streamLayout.CachedRoomCount;
        var next=RoomStreamingSearch.Next(streamLayout,loaded,40,40,true,12,null);
        Check(next!=null&&streamLayout.CachedRoomCount-before<=1,"Large regions eagerly describe the full render window");
        streamLayout.TrimNeighbourhood(40,40,2);Check(streamLayout.CachedRoomCount<=1&&streamLayout.CachedTileCount<=16,"Large region cache cannot be trimmed");
        Check(sectionKinds.Count==5,"Missing interior district roles");
        Console.WriteLine($"PASS: large regions; {found} real biome destinations, {rooms} full room meshes, {paths} reciprocal walking routes / {steps} room steps, {openPairs} broad hall boundary pairs across all twenty programmes, five sector roles, kilometre-scale populated meadows, nonoverlapping 24..28-bay malls, bounded local room streaming. Core only; native Unity/appearance/FPS unrun.");
    }
}
