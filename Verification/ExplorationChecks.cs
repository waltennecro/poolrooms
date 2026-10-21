using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void WalkableNetwork()
    {
        var signatures=new HashSet<string>();int assigned=0;
        var defaults=new LayoutConfig{Zones=ZoneMode.Random,City11Share=0,QuietLevelShare=0,SoftPlayShare=0,RainHouseShare=0,AquaticParkShare=0,CommonSolarShare=0,ConnectedMallShare=0,ConnectedTerrainShare=0};
        WalkableExploration.Configure(defaults);defaults.Validate();Check(defaults.DiscoveryCoverage&&defaults.CommonSolarShare==.5f&&ConnectedMallTopology.Enabled(defaults)&&ConnectedTerrainTopology.Enabled(defaults),"Old profile leaves levels unavailable");
        Check(Enum.GetValues(typeof(LiminalEnvironment)).Length==RestfulLevelPlanner.EnvironmentCount,"Discovery policy must include newly appended environments");
        foreach(int bx in new[]{-3,0,2})foreach(int bz in new[]{-2,0,3})
        {
            var counts=new int[RestfulLevelPlanner.EnvironmentCount];var signature=new List<int>();
            for(int x=0;x<8;x++)for(int z=0;z<8;z++)
            {bool covered=WalkableExploration.Region(defaults,bx*8+x,bz*8+z,out var e);signature.Add(covered?(int)e:-1);if(covered){counts[(int)e]++;assigned++;}}
            Check(counts.All(n=>n>=1)&&counts.Sum()==64,"Expanded block misses a destination before allocating duplicates");signatures.Add(string.Join(",",signature));
        }
        Check(signatures.Count==9,"Biome order repeats between region blocks");
        var custom=new LayoutConfig{Procedural=LiminalGeneration.Expanded};Check(!WalkableExploration.Region(custom,4,5,out _),"Custom generation unexpectedly migrated");
        var extreme=new LayoutConfig{Zones=ZoneMode.Random,SupermarketShare=1,AdditionalEnvironmentShare=1};WalkableExploration.Configure(extreme);
        foreach(LiminalEnvironment e in Enum.GetValues(typeof(LiminalEnvironment)))Check(LiminalRegions.Find(extreme,e,0,0,out _,out _),"Discovery and optional search disagree for "+e);
        var galleryConfig=new LayoutConfig{CellSize=60,Continuous=true,Procedural=LiminalGeneration.TransitGallery};var gallery=new Room{Exits=15,Environment=LiminalEnvironment.TransitGallery};QuietLevelPlanner.Describe(gallery,galleryConfig);var galleryGeometry=QuietLevelArchitecture.Build(gallery,galleryConfig);
        foreach(int sign in new[]{-1,1})for(float x=28;x<=30;x+=.2f)Check(SphereClear(galleryGeometry.Structure,new V3(sign*x,.9f,0),.32f),"Civic column blocks an east/west entrance");
        int routes=0,boundaryPairs=0,bridgeCases=0;int[] seeds={0,31,481516,87,1234};float[] sizes={36,36,36,28,60};
        for(int trial=0;trial<seeds.Length;trial++)
        {
            var c=new LayoutConfig{Seed=seeds[trial],CellSize=sizes[trial],Zones=ZoneMode.Random,Grammar=ProceduralGrammar.SolarAtlas,AutonomousGeneration=true,IndependentBiomeMaterials=true,EnableAircraft=true,EnableImportedBuildings=true,CurveSegments=16};WalkableExploration.Configure(c);
            var topology=new ContinuousTopology(c);var edges=new Dictionary<long,int>();var parents=new Dictionary<long,long>();var queue=new Queue<long>();var targets=new long?[RestfulLevelPlanner.EnvironmentCount+4];var distances=new Dictionary<long,int>();
            long origin=ContinuousTopology.CellKey(0,0);queue.Enqueue(origin);parents[origin]=origin;distances[origin]=0;
            Func<int,int,int> exits=(x,z)=>{long key=ContinuousTopology.CellKey(x,z);if(!edges.TryGetValue(key,out int bits)){bits=ConnectedTerrainTopology.Exits(c,x,z,ConnectedMallTopology.Exits(c,x,z,topology.Exits(x,z)));edges[key]=bits;}return bits;};
            const int radius=112;
            while(queue.Count>0&&targets.Any(v=>v==null))
            {
                long key=queue.Dequeue();int x=(int)(key>>32),z=unchecked((int)key);ConnectedMallSite mall;ConnectedTerrainSite meadow;
                bool mg=ConnectedMallTopology.Gateway(c,x,z,out mall),tg=ConnectedTerrainTopology.Gateway(c,x,z,out meadow);
                if(mg&&targets[RestfulLevelPlanner.EnvironmentCount]==null)targets[RestfulLevelPlanner.EnvironmentCount]=key;if(tg&&targets[RestfulLevelPlanner.EnvironmentCount+1]==null)targets[RestfulLevelPlanner.EnvironmentCount+1]=key;
                if(!mg&&!tg)
                {
                    var e=LiminalRegions.At(c,x,z);if(targets[(int)e]==null)targets[(int)e]=key;
                    if(e==LiminalEnvironment.Poolrooms){int solar=PoolroomArchitectureMix.UsesSolar(c,x,z)?RestfulLevelPlanner.EnvironmentCount+2:RestfulLevelPlanner.EnvironmentCount+3;if(targets[solar]==null)targets[solar]=key;}
                }
                int bits=exits(x,z);
                for(int side=0;side<4;side++)if((bits&(1<<side))!=0)
                {
                    int nx=x+Directions.DX[side],nz=z+Directions.DZ[side];if(Math.Abs(nx)>radius||Math.Abs(nz)>radius||ConnectedTerrainTopology.Reserved(c,nx,nz))continue;
                    Check((exits(nx,nz)&(1<<((side+2)%4)))!=0,"Walking graph has a one-way doorway");
                    long next=ContinuousTopology.CellKey(nx,nz);if(parents.ContainsKey(next))continue;parents[next]=key;distances[next]=distances[key]+1;queue.Enqueue(next);
                }
            }
            Check(targets.All(v=>v!=null),"Unreachable walking destination for seed "+c.Seed+": "+string.Join(",",Enumerable.Range(0,targets.Length).Where(i=>targets[i]==null)));
            int maxRoute=targets.Max(v=>distances[v.Value]);Console.WriteLine($"Walking graph seed {c.Seed}, cell {c.CellSize}: {RestfulLevelPlanner.EnvironmentCount} environments + mall + Level94 and both Poolrooms climates reached; {parents.Count} explored cells; longest witness {maxRoute} room edges. Not a distance promise or PhysX test.");
            var layout=new Layout(c);var tested=new HashSet<string>();
            foreach(long? target in targets)
            {
                long at=target.Value;routes++;
                while(at!=origin)
                {
                    long prev=parents[at];int ax=(int)(prev>>32),az=unchecked((int)prev),bx=(int)(at>>32),bz=unchecked((int)at);
                    var a=layout.At(ax,az);var b=layout.At(bx,bz);
                    Check(a.HasExit(DirectionsFrom(bx-ax,bz-az))&&b.HasExit(DirectionsFrom(ax-bx,az-bz)),"Witness path disagrees with actual layout");
                    if(a.Environment!=b.Environment)
                    {
                        string pair=Math.Min((int)a.Environment,(int)b.Environment)+":"+Math.Max((int)a.Environment,(int)b.Environment);
                        if(tested.Add(pair)){CheckWalkingThreshold(a,b,c);boundaryPairs++;}
                    }
                    at=prev;
                }
            }
            var mr=layout.At((int)(targets[RestfulLevelPlanner.EnvironmentCount].Value>>32),unchecked((int)targets[RestfulLevelPlanner.EnvironmentCount].Value));ConnectedMallTopology.Gateway(c,mr.X,mr.Z,out var site);
            Check(mr.MallGateway,"Mall witness ends at a tool-only entrance");var mb=ConnectedMallArchitecture.Bridge(site,mr,c);
            for(int step=0;step<=24;step++)CheckBridgePoint(mb.Structure,ConnectedMallArchitecture.Point(site,mr,c,step/24f));bridgeCases++;
            var tr=layout.At((int)(targets[RestfulLevelPlanner.EnvironmentCount+1].Value>>32),unchecked((int)targets[RestfulLevelPlanner.EnvironmentCount+1].Value));ConnectedTerrainTopology.Gateway(c,tr.X,tr.Z,out var terrain);
            Check(tr.TerrainGateway,"Level94 witness ends at a tool-only entrance");var tb=ConnectedTerrainArchitecture.Bridge(terrain,tr,c);
            for(int step=0;step<=24;step++)CheckBridgePoint(tb.Structure,ConnectedTerrainArchitecture.Point(terrain,tr,c,step/24f));bridgeCases++;
        }
        // Regression: RainHouses shares its full cell. A generic curved corridor with
        // zero length used to divide by zero and leave a missing/invalid connector.
        foreach(int side in new[]{0,1})
        {
            var c=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.RainHouses};var a=new Room{Exits=15};var b=new Room{Exits=15};RainHousePlanner.DescribeRecipe(a,c,0);RainHousePlanner.DescribeRecipe(b,c,2499);
            var join=Corridors.Build(a,b,side,c);Validate(join.Structure);Check(join.Structure.TriangleCount>0,"Direct rain threshold missing");
            var point=new V3(Directions.DX[side]*c.CellSize*.5f,1,Directions.DZ[side]*c.CellSize*.5f);Check(Math.Abs(Support(join.Structure,point)+.015f)<.002f,"Direct rain threshold lacks supporting floor");
        }
        Console.WriteLine($"Walkable discovery: {assigned} mandatory candidate regions across 9 independently shuffled blocks; {routes} graph witness routes, {boundaryPairs} physical biome boundary pairs and {bridgeCases} mall/meadow bridge cases. No administrator state or teleport used; native Unity pending.");
    }
    static int DirectionsFrom(int x,int z){return x==1?1:x==-1?3:z==1?0:2;}
    static void CheckBridgePoint(MeshData mesh,V3 point)
    {Check(Math.Abs(Support(mesh,point+V3.Up*2))<.002f&&SphereClear(mesh,point+V3.Up,.3f),"Connected district bridge blocks walking");}
    static void CheckWalkingThreshold(Room a,Room b,LayoutConfig c)
    {
        int side=DirectionsFrom(b.X-a.X,b.Z-a.Z);if(side>=2){var swap=a;a=b;b=swap;side-=2;}
        Check(Math.Abs(SolarConnections.DoorWidth(a,side,c)-SolarConnections.DoorWidth(b,(side+2)%4,c))<.001f,"Biome boundary width mismatch");
        Check(Math.Abs(SolarConnections.DoorHeight(a,side,c)-SolarConnections.DoorHeight(b,(side+2)%4,c))<.001f,"Biome boundary height mismatch");
        var ag=Architecture.Build(a,c);var bg=Architecture.Build(b,c);var link=Corridors.Build(a,b,side,c);Validate(link.Structure);
        var combined=new MeshData();combined.Append(ag.Structure);combined.Append(ag.CollisionOnly);combined.Append(link.Structure);
        var other=new MeshData();other.Append(bg.Structure);other.Append(bg.CollisionOnly);other.TranslateRotate(new V3(Directions.DX[side]*c.CellSize,0,Directions.DZ[side]*c.CellSize),0);combined.Append(other);
        float start=(side==0?a.Depth:a.Width)*.5f-.7f,end=c.CellSize-(side==0?b.Depth:b.Width)*.5f+.7f;var path=Corridors.Describe(a,b,side,c);
        // Test the real doorway approaches and curved connector, not a straight line
        // across the centre of authored pools/columns, which the player can walk around.
        for(float distance=start;distance<=end;distance+=.2f)
        {
            var point=new V3(Directions.DX[side]*distance,2,Directions.DZ[side]*distance);
            if(!link.Natural&&path.Length>.01f&&distance>path.Start&&distance<path.End)
            {path.Sample((distance-path.Start)/path.Length,out var center,out _,out _);point=side==0?new V3(center.X,2,center.Z):new V3(center.Z,2,-center.X);}
            float floor=Support(combined,point);
            Check(floor>=-.32f&&floor<=.35f,"Walking route has an unsupported or raised floor: "+a.Environment+" / "+b.Environment+" at "+distance);
            bool low=SphereClear(combined,new V3(point.X,floor+.4f,point.Z),.29f),mid=SphereClear(combined,new V3(point.X,floor+.9f,point.Z),.32f),head=SphereClear(combined,new V3(point.X,floor+1.55f,point.Z),.2f);
            if(!low||!mid||!head)
            {
                var test=new V3(point.X,floor+(!low?.4f:!mid?.9f:1.55f),point.Z);float rad=!low?.29f:!mid?.32f:.2f;
                foreach(var part in new[]{ag.Structure,ag.CollisionOnly,link.Structure,other})foreach(var indices in part.Indices)for(int i=0;i<indices.Count;i+=3)
                {var va=part.Positions[indices[i]];var vb=part.Positions[indices[i+1]];var vc=part.Positions[indices[i+2]];if(PointTriangleDistanceSquared(test,va,vb,vc)<rad*rad){Console.WriteLine($"Blocked triangle at ({va.X},{va.Y},{va.Z}) ({vb.X},{vb.Y},{vb.Z}) ({vc.X},{vc.Y},{vc.Z}), low={low} mid={mid} head={head}, side {side}");goto Printed;}}
            }
            Printed:
            Check(low&&mid&&head,"Walking route obstructed at biome boundary: "+a.Environment+" / "+b.Environment+" at "+distance+" floor "+floor+" coords "+a.X+","+a.Z+" -> "+b.X+","+b.Z);
        }
    }
}
