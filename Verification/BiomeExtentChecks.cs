using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    sealed class RegionSample
    {
        public int Count;public bool Edge;public LiminalEnvironment Environment;
    }
    static Dictionary<long,RegionSample> RegionAreas(LayoutConfig c,int radius)
    {
        var regions=new Dictionary<long,RegionSample>();
        for(int z=-radius;z<radius;z++)for(int x=-radius;x<radius;x++)
        {
            BiomeExtent.NearestRegion(c,x,z,out int rx,out int rz);long key=ContinuousTopology.CellKey(rx,rz);
            if(!regions.TryGetValue(key,out var sample))
            {sample=new RegionSample{Environment=LiminalRegions.At(c,x,z)};regions.Add(key,sample);}
            sample.Count++;sample.Edge|=x==-radius||z==-radius||x==radius-1||z==radius-1;
        }
        return regions;
    }
    static Dictionary<long,(int Count,bool Edge,PoolClimate Climate)> ClimateAreas(LayoutConfig c,int radius)
    {
        var result=new Dictionary<long,(int Count,bool Edge,PoolClimate Climate)>();
        for(int z=-radius;z<radius;z++)for(int x=-radius;x<radius;x++)
        {
            var sky=SolarClimateRegions.At(c,x,z);long key=ContinuousTopology.CellKey(sky.RegionX,sky.RegionZ);
            result.TryGetValue(key,out var sample);sample.Count++;sample.Climate=sky.Kind;
            sample.Edge|=x==-radius||z==-radius||x==radius-1||z==radius-1;result[key]=sample;
        }
        return result;
    }
    static void BiomeExtents()
    {
        var totalsBefore=new long[18];var totalsAfter=new long[18];int regionPairs=0,meshSamples=0;
        foreach(int seed in new[]{0,31,481516})
        {
            var before=new LayoutConfig{Seed=seed};var after=new LayoutConfig{Seed=seed};WalkableExploration.Configure(before);WalkableExploration.Configure(after);
            before.ExpandedBiomeAreas=false;Check(after.ExpandedBiomeAreas&&after.ExpandedVariety,"Unified exploration omitted one of the two requested expansions");
            var old=RegionAreas(before,192);var next=RegionAreas(after,236);
            foreach(var pair in old)
            {
                if(pair.Value.Edge||!next.TryGetValue(pair.Key,out var other)||other.Edge)continue;
                Check(other.Environment==pair.Value.Environment,"Scaling changed a logical region's biome label");
                int env=(int)other.Environment;totalsBefore[env]+=pair.Value.Count;totalsAfter[env]+=other.Count;regionPairs++;
            }
            foreach(int x in new[]{int.MinValue,-1,0,int.MaxValue})foreach(int z in new[]{int.MinValue,-1,0,int.MaxValue})
            {
                var e=LiminalRegions.At(after,x,z);Check(Enum.IsDefined(typeof(LiminalEnvironment),e),"Extreme extent address is not a biome");
                Check(e==LiminalRegions.At(after,x,z),"Extended region depends on query order");
                var climate=SolarClimateRegions.At(after,x,z);Check(float.IsFinite(climate.SunDirection.X)&&float.IsFinite(climate.SunDirection.Y)&&float.IsFinite(climate.SunDirection.Z),"Expanded solar climate overflow");
            }
        }
        for(int env=0;env<18;env++)
        {
            double ratio=totalsAfter[env]/(double)totalsBefore[env];
            Check(totalsBefore[env]>500&&ratio>1.46&&ratio<1.54,"Region area failed +50% sample for "+(LiminalEnvironment)env+": "+ratio);
            Console.WriteLine($"Region field {(LiminalEnvironment)env}: {totalsBefore[env]} -> {totalsAfter[env]} room cells in matched complete regions ({ratio:F4}x). Reservations not deducted.");
        }
        foreach(bool common in new[]{false,true})
        {
            var old=ClimateAreas(new LayoutConfig{Seed=31,CommonPoolrooms=common},96);
            var next=ClimateAreas(new LayoutConfig{Seed=31,CommonPoolrooms=common,ExpandedBiomeAreas=true},118);
            var first=new long[5];var second=new long[5];
            foreach(var pair in old)
            {
                if(pair.Value.Edge||!next.TryGetValue(pair.Key,out var other)||other.Edge)continue;
                Check(other.Climate==pair.Value.Climate,"Area growth changed a solar climate's logical identity");
                int climate=(int)other.Climate;first[climate]+=pair.Value.Count;second[climate]+=other.Count;
            }
            for(int climate=0;climate<5;climate++)
            {double ratio=second[climate]/(double)first[climate];Check(first[climate]>100&&ratio>1.44&&ratio<1.56,"Solar/closed regional area did not grow consistently: "+(PoolClimate)climate);}
            Console.WriteLine($"Solar field {(common?"common":"atlas")}: all five climates retain identity and approximately +50% area.");
        }
        int shopsBefore=0,shopsAfter=0;var envelopes=new HashSet<string>();
        foreach(int size in new[]{28,36,60})for(int seed=0;seed<128;seed++)
        {
            var before=MallPlan.Create(seed,-12,19,true,true,true,true);var next=MallPlan.Create(seed,-12,19,true,true,true,true,true);
            var c=new LayoutConfig{Seed=seed,CellSize=size,ExpandedVariety=true,ExpandedBiomeAreas=true};var site=new ConnectedMallSite(c,-12,19);
            Check(next.Valid()&&next.Floors==before.Floors&&next.Floors>=2&&next.Floors<=6,"Expansion changed actual storey count or invalidated mall circulation");
            Check(next.Shops.Length>=Math.Ceiling(before.Shops.Length*1.5),"Mall gained area without enough accessible shops");
            double ratio=next.Width*next.Depth/(before.Width*(double)before.Depth);Check(Math.Abs(ratio-1.5)<.00001,"Mall gross floor area is not +50 percent");
            Check(site.Width==next.Width&&site.Depth==next.Depth&&site.Floors==next.Floors&&site.Bays==next.Bays,"Mall reservation disagrees with actual building dimensions");
            Check(site.RadiusX*c.CellSize>=next.Width*.5f&&site.RadiusZ*c.CellSize>=next.Depth*.5f,"Enlarged mall escaped its reserved cells");
            var keys=new HashSet<int>();for(int floor=0;floor<next.Floors;floor++)for(int sector=0;sector<next.SectorCount;sector++)Check(keys.Add(floor*32+sector),"Additional commercial sectors alias another floor");
            shopsBefore+=before.Shops.Length;shopsAfter+=next.Shops.Length;envelopes.Add(next.Floors+":"+next.Bays+":"+next.Depth);
            if(seed%32!=0)continue;
            var shell=MallArchitecture.Shell(next);Validate(shell.Structure);Validate(shell.Decoration);Validate(shell.Glass);meshSamples++;
            foreach(int floor in new[]{0,next.Floors-1})foreach(int sector in new[]{0,1,next.Bays,next.Bays+1})
            {
                var data=MallArchitecture.Sector(next,floor,sector,12);Validate(data.Geometry.Structure);Validate(data.Geometry.Decoration);Validate(data.Geometry.CollisionOnly);meshSamples++;
            }
        }
        int oldHomes=0,newHomes=0,outerHomes=0,terrainSites=0;
        foreach(int size in new[]{28,36,60})foreach(int seed in new[]{0,31,94,481516})
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,ConnectedTerrain=true,ConnectedTerrainShare=1,ConnectedMalls=true,ConnectedMallShare=1,ExpandedVariety=true};
            Check(ConnectedTerrainTopology.Site(c,0,0,out var original),"Missing original terrain site");c.ExpandedBiomeAreas=true;
            Check(ConnectedTerrainTopology.Site(c,0,0,out var site),"Missing enlarged terrain site");
            double ratio=(2L*site.RadiusX+1)*(2L*site.RadiusZ+1)/(double)((2L*original.RadiusX+1)*(2L*original.RadiusZ+1));Check(ratio>1.46&&ratio<1.54,"Integer terrain reservation failed area enlargement");
            var old=TerrainRegionPlan.CreateConnected(seed,original,true);var next=TerrainRegionPlan.CreateConnected(seed,site,true,true);
            Check(next.Width==site.HalfWidth*2&&next.Depth==site.HalfDepth*2&&next.HalfWidth==site.HalfWidth&&next.HalfDepth==site.HalfDepth,"Terrain population envelope did not grow with the actual district");
            Check(next.HouseCount>old.HouseCount,"Extended Level94 has no additional houses");
            int outside=0;foreach(var h in next.Houses)if(Math.Abs(h.X)>old.Width*.5f||Math.Abs(h.Z)>old.Depth*.5f)outside++;
            Check(outside>0,"Extended terrain is an empty strip beyond the old populated envelope");
            oldHomes+=old.HouseCount;newHomes+=next.HouseCount;outerHomes+=outside;
            var sites=new List<ConnectedTerrainSite>();
            for(int bx=-2;bx<=2;bx++)for(int bz=-3;bz<=2;bz++)if(ConnectedTerrainTopology.Site(c,bx,bz,out var s))sites.Add(s);
            for(int i=0;i<sites.Count;i++)for(int j=0;j<i;j++)Check(Math.Abs((long)sites[i].X-sites[j].X)>sites[i].RadiusX+sites[j].RadiusX+2||Math.Abs((long)sites[i].Z-sites[j].Z)>sites[i].RadiusZ+sites[j].RadiusZ+2,"Extended Level94 districts overlap");
            for(int bx=-3;bx<=3;bx++)for(int bz=-3;bz<=3;bz++)if(ConnectedMallTopology.Site(c,bx,bz,out var mall))Check(!ConnectedTerrainTopology.Intersects(c,mall.X,mall.Z,mall.RadiusX+1,mall.RadiusZ+1),"Extended commercial site overlaps Level94");
            var layout=new Layout(c);for(int side=0;side<4;side++)
            {
                var room=layout.At(site.GateX(side),site.GateZ(side));var bridge=ConnectedTerrainArchitecture.Bridge(site,room,c);Validate(bridge.Structure);
                for(int sample=0;sample<=24;sample++)CheckBridgePoint(bridge.Structure,ConnectedTerrainArchitecture.Point(site,room,c,sample/24f));meshSamples++;
            }
            terrainSites++;
        }
        IntegrationSourceContracts();
        Console.WriteLine($"PASS: biome extents; {regionPairs} matched complete region pairs across all 18 environments; {shopsBefore} -> {shopsAfter} accessible shops in paired mall plans; {oldHomes} -> {newHomes} generated Level94 houses, {outerHomes} in newly populated outer areas, {terrainSites} terrain cases; {meshSamples} mesh samples; {assertions} core assertions. Native Unity/PhysX/FPS unrun.");
    }
}
