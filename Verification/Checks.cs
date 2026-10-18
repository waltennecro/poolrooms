using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Poolrooms.Core;

static class Checks
{
    static int assertions;
    static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
    static string Hash(params MeshData[] meshes)
    {
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        foreach(var m in meshes){writer.Write(m.Positions.Count);foreach(var p in m.Positions){writer.Write(p.X);writer.Write(p.Y);writer.Write(p.Z);}foreach(var list in m.Indices){writer.Write(list.Count);foreach(int i in list)writer.Write(i);}}
        writer.Flush();return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    static int Main(string[] args)
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        string baseline=Path.Combine(root,"Verification/Baselines/v39_geometry.json");
        var actual=new SortedDictionary<string,string>();
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,4,6})for(int seed=0;seed<12;seed++)
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,DoorWidth=door,CurveSegments=12,Procedural=LiminalGeneration.SoftPlay};
            var r=new Room{X=17,Z=-23,Exits=15};SoftPlayPlanner.Describe(r,c);var g=Architecture.Build(r,c);
            actual[$"softplay:{size}:{door}:{seed}"]=Hash(g.Structure,g.CollisionOnly);
            Check(r.Plan.SoftPlay.Valid(r,c),"Invalid protected play circulation");
            Validate(g.Structure);Validate(g.Decoration);Validate(g.CollisionOnly);
            Check(g.VisualMeshes.Count==2,"Finishes must remain two batches per hall");
            foreach(var visual in g.VisualMeshes)Validate(visual);
            Check(g.VisualMeshes[1].Theme==SurfaceTheme.SoftPlayNet,"Net must not use blue plastic");
            Check(g.VisualMeshes[0].TriangleCount==2,"Floor detail should not multiply geometry");
            foreach(var f in g.Fixtures)
            {
                Check(f.CustomColor,"SoftPlay uses explicit regional source colours");
                Check((f.LightColor-g.Decoration.EmissionTint).Length<.00001f,"Lamp and emissive colour mismatch");
            }
        }
        foreach(var env in new[]{LiminalGeneration.Poolrooms,LiminalGeneration.IndoorPlay,LiminalGeneration.Supermarkets,LiminalGeneration.Offices,LiminalGeneration.Hotels})for(int seed=0;seed<16;seed++)
        {
            var layout=new Layout(new LayoutConfig{Seed=seed,Procedural=env,Continuous=true});
            var r=layout.At(seed-9,seed*2-15);var g=Architecture.Build(r,layout);
            actual[$"legacy:{env}:{seed}"]=Hash(g.Structure,g.Decoration,g.Water,g.CollisionOnly);
        }
        if(args.Contains("--capture-baseline")){Directory.CreateDirectory(Path.GetDirectoryName(baseline));File.WriteAllText(baseline,JsonSerializer.Serialize(actual,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Captured {actual.Count} original geometry fingerprints");return 0;}
        var expected=JsonSerializer.Deserialize<SortedDictionary<string,string>>(File.ReadAllText(baseline));
        foreach(var item in expected)Check(actual.TryGetValue(item.Key,out var hash)&&hash==item.Value,"Geometry changed: "+item.Key);
        MovementAndQuality();
        CompactTerrain();
        CityGeneration();
        CityStreetConnections();
        ImportedCityBuildings();
        AirfieldsAndFlight();
        RotorFlight();
        SourceFurniturePlacement();
        ExpandedFurniturePlacement();
        RegionalSourcePlacement();
        UrbanAndQuietRefinement();
        BistroTerraces();
        QuietGeneration();
        CivicGeneration();
        CameraLandingResponse();
        DressingGeneration();
        string[][] symbols={Array.Empty<string>(),new[]{"UNITY_EDITOR","UNITY_INCLUDE_TESTS","ENABLE_LEGACY_INPUT_MANAGER"},new[]{"POOLROOMS_HDRP_17","UNITY_EDITOR","UNITY_INCLUDE_TESTS"},new[]{"POOLROOMS_INPUT_SYSTEM","ENABLE_INPUT_SYSTEM","UNITY_INCLUDE_TESTS"}};
        var scripts=Directory.GetFiles(Path.Combine(root,"Assets"),"*.cs",SearchOption.AllDirectories);
        foreach(var config in symbols)foreach(var path in scripts)
        {
            var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(path),new CSharpParseOptions(LanguageVersion.CSharp9,preprocessorSymbols:config));
            foreach(var d in tree.GetDiagnostics())Check(d.Severity!=DiagnosticSeverity.Error,path+": "+d);
        }
        Console.WriteLine($"PASS: {assertions} checks; 108 protected SoftPlay geometries + 80 other-biome fingerprints; C# 9 syntax {scripts.Length} scripts x {symbols.Length} configurations.");
        Console.WriteLine("Core compiled; Unity/HDRP API compilation, shaders, PhysX, visual quality and FPS NOT executed.");return 0;
    }
    static void Validate(MeshData m)
    {
        Check(m.Positions.Count==m.Normals.Count&&m.Positions.Count==m.UV.Count,"Mismatched mesh buffers");
        foreach(var p in m.Positions)Check(float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z),"Nonfinite geometry");
        foreach(var list in m.Indices){Check(list.Count%3==0,"Truncated triangle");foreach(int i in list)Check(i>=0&&i<m.Positions.Count,"Invalid vertex index");}
    }
    static void MovementAndQuality()
    {
        foreach(int fps in new[]{10,15,20,30,60,120,144})
        {
            var dash=new ExplorerDash();float travel=0;float dt=1f/fps;
            for(int frame=0;frame<fps;frame++)
            {var velocity=dash.Step(dt,frame==0,true,new V3(),new V3(1,0,0));travel+=velocity.X*dt;Check(velocity.Y==0&&velocity.Z==0,"Dash altered vertical axis");}
            Check(Math.Abs(travel-ExplorerDash.Speed*ExplorerDash.Duration)<.0001f,"Dash distance depends on FPS");
            Check(dash.Charges==ExplorerDash.Capacity,"Dash did not recharge");
        }
        var blocked=new ExplorerDash();blocked.Step(.02f,true,false,new V3(),V3.Up);Check(blocked.Charges==3&&!blocked.Active,"Blocked input consumed dash");
        for(int i=0;i<3;i++){blocked.Step(.02f,true,true,new V3(),new V3(0,0,1));blocked.Cancel();}
        Check(blocked.Charges==0,"Burst charge limit failed");
        Check(blocked.Step(.02f,true,true,new V3(),new V3(0,0,1)).Length==0,"Fourth dash bypassed charge limit");
        blocked.Step(float.NaN,true,true,new V3(),new V3(0,0,1));Check(!blocked.Active,"Invalid time started dash");
        foreach(int quality in new[]{0,1,2,3})foreach(bool rt in new[]{false,true})
        {
            var tuning=new GraphicsSampling(quality,rt);
            Check(tuning.ReflectionSamples<=8&&tuning.IndirectSamples<=8,"Unbounded ray budget");
            Check(rt||tuning.ReflectionBounces==1,"Raster fallback requested reflection bounces");
            var prefs=new GamePreferences{graphicsQuality=quality};prefs.Sanitize();Check(prefs.graphicsQuality==quality,"Quality selection did not survive preferences");
        }
        Check(new GraphicsSampling(3,true).ReflectionBounces==2&&new GraphicsSampling(3,true).IndirectBounces==4,"Ultra samples not connected");
        foreach(PickupSurface surface in Enum.GetValues(typeof(PickupSurface)))
        {var p=new PickupPhysics(surface);Check(p.Friction>0&&p.Friction<1&&p.Restitution>=0&&p.Restitution<1&&p.AirDamping<.2f,"Invalid contact budget");}
        Check(new PickupPhysics(PickupSurface.Foam).Restitution<new PickupPhysics(PickupSurface.SoftBall).Restitution,"Foam bounces like a ball");
        Check(PickupPhysics.ReleaseVelocity(new V3(1000,50,-1000)).Length<=2.801f,"Dropping object turns dash into a projectile");
        Check(PickupPhysics.ReleaseVelocity(new V3(float.NaN,0,0)).Length==0,"Invalid release velocity");
    }
    static void CompactTerrain()
    {
        for(int seed=0;seed<8;seed++)
        {
            var plan=TerrainRegionPlan.Create(seed,11,-17);
            Check(plan.Width>=1536&&plan.Width<=2560&&plan.Depth>=1536&&plan.Depth<=2560,"Terrain size outside compact budget");
            foreach(var house in plan.Houses)
            {
                Check(plan.House(house.CellX,house.CellZ)==house,"Compact lot lost its streaming index");
                Check(TerrainRegionPlan.Cell(house.X)==house.CellX&&TerrainRegionPlan.Cell(house.Z)==house.CellZ,"House moved out of owning tile");
                Check(float.IsFinite(plan.Height(house.X,house.Z)),"Invalid house support");
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dz==0)continue;var other=plan.House(house.CellX+dx,house.CellZ+dz);if(other==null)continue;
                    Check(Math.Abs(house.X-other.X)>=(house.Width+other.Width)*.5f+5.9f||Math.Abs(house.Z-other.Z)>=(house.Depth+other.Depth)*.5f+5.9f,"Compact houses overlap their circulation margin");
                }
            }
            Check(TerrainGrass.Build(plan,3,3,TerrainGrassQuality.Off).TriangleCount==0,"Grass off still renders blades");
            var economy=TerrainGrass.Build(plan,3,3,TerrainGrassQuality.Economy);
            var high=TerrainGrass.Build(plan,3,3,TerrainGrassQuality.High);
            Validate(economy);Validate(high);
            Check(economy.TriangleCount<=588&&high.TriangleCount<=11760,"Grass exceeded explicit patch budget");
            Check(TerrainGrass.PatchBudget(TerrainGrassQuality.Economy)==9&&TerrainGrass.PatchBudget(TerrainGrassQuality.High)==25,"Grass streaming expanded unexpectedly");
        }
    }
    static void CityGeneration()
    {
        int maximum=0,buildings=0,props=0;
        foreach(int size in new[]{28,36,60})for(int seed=0;seed<8;seed++)
        {
            var config=new LayoutConfig{Seed=seed,CellSize=size,Procedural=LiminalGeneration.City11,Continuous=true,AutonomousGeneration=true,CurveSegments=12};
            var layout=new Layout(config);var room=layout.At(seed-6,seed+12);var data=Architecture.Build(room,layout);
            Check(room.Plan.City.Valid(room,config)&&room.Plan.Valid(room,config),"Invalid integrated city block");
            Validate(data.Structure);Validate(data.Decoration);Validate(data.Glass);Validate(data.CollisionOnly);foreach(var visual in data.VisualMeshes)Validate(visual);
            buildings+=room.Plan.City.Buildings.Length;props+=room.Plan.City.Props.Length;maximum=Math.Max(maximum,data.Structure.TriangleCount+data.Decoration.TriangleCount+data.Glass.TriangleCount);
            foreach(var prop in room.Plan.City.Props)
            {
                float support=Support(data.Structure,prop.Position);
                Check(Math.Abs(prop.Position.Y-support-.02f)<.001f,"City item has no actual counter support");
            }
            foreach(var b in room.Plan.City.Buildings)for(int level=0;level<b.Floors-1;level++)for(int half=0;half<2;half++)for(int step=0;step<9;step++)
            {
                float x=-b.Width*.5f+2.2f+(half==0?-.95f:.95f),z=half==0?-1.35f+(step+.5f)*.3f:1.35f-(step+.5f)*.3f;
                float y=.12f+level*3.2f+half*1.6f+(step+1)*1.6f/9;
                Check(Math.Abs(Support(data.Structure,b.Point(x,y+.03f,z))-y)<.001f,"City stair lost physical support");
            }
            foreach(var connector in data.Connectors)if(connector!=null)Validate(connector.Structure);
            Check(Hash(data.Structure,data.Decoration)==Hash(Architecture.Build(room,config).Structure,Architecture.Build(room,config).Decoration),"City geometry not deterministic");
        }
        var mixed=new LayoutConfig{Procedural=LiminalGeneration.Expanded,Continuous=true,EnableCity11=true,City11Share=1,AdditionalEnvironmentShare=1};
        int xFound,zFound;Check(LiminalRegions.Find(mixed,LiminalEnvironment.City11,0,0,out xFound,out zFound),"City master TP cannot find a generated region");
        mixed.EnableCity11=false;Check(!LiminalRegions.Find(mixed,LiminalEnvironment.City11,0,0,out xFound,out zFound),"Disabled city TP should not generate a district");
        Console.WriteLine($"City: {buildings} generated buildings / {props} source-prop instances in 24 blocks; max {maximum} structural/detail/glass triangles. These are instances, not downloaded model counts.");
        var city=new Layout(new LayoutConfig{Seed=481516,Procedural=LiminalGeneration.City11,Continuous=true});int spaces=0,carSites=0,cityItems=0;
        for(int z=0;z<18;z++)for(int x=0;x<18;x++)
        {
            var r=city.At(x,z);Check(r.Plan.City!=null,"Missing city plan in full-city mode");foreach(var b in r.Plan.City.Buildings)spaces+=b.Floors;
            cityItems+=r.Plan.City.Props.Length;
            if(LiminalRegions.Hash(city.Config.Seed,x,z,0x1100CAu)%9==0)carSites++;
        }
        Console.WriteLine($"City plan snapshot (324 blocks, not simultaneous loaded objects): 1296 generated building instances, {spaces} furnished floor spaces, {cityItems} source-prop/furniture instances, {spaces*3} procedural furniture assemblies, {carSites} car spawn sites. Snapshot with aircraft disabled; source families are audited separately.");
    }
    static void CityStreetConnections()
    {
        int joins=0,borders=0;
        foreach(int seed in new[]{0,31})foreach(int size in new[]{28,36,60})
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,Procedural=LiminalGeneration.Expanded,EnableCity11=true,EnableAircraft=true,City11Share=.5f,ConnectedTerrain=true,ConnectedMalls=true};
            var layout=new Layout(c);int cx,cz;Check(LiminalRegions.Find(c,LiminalEnvironment.City11,0,0,out cx,out cz),"No real city for street checks");
            for(int z=cz-4;z<=cz+4;z++)for(int x=cx-4;x<=cx+4;x++)
            {
                var room=layout.At(x,z);if(room.Plan==null||room.Plan.City==null)continue;
                for(int d=0;d<4;d++)if(room.HasExit(d))
                {
                    var n=layout.At(x+Directions.DX[d],z+Directions.DZ[d]);
                    float width=CityStreets.OpeningWidth(room,d,c);
                    Check(SolarConnections.DoorWidth(room,d,c)==width,"Streaming cap width differs from city aperture");
                    if(n.Plan!=null&&n.Plan.City!=null)
                    {
                        Check(n.HasExit((d+2)%4),"City road exit is not reciprocal");
                        Check(width==CityStreets.OpeningWidth(n,(d+2)%4,c),"Street widths differ across a seam");
                        Check(width>=6,"Neighbouring city blocks retained narrow gates");joins++;
                        var g=City11Architecture.Build(room,c);
                        for(float lateral=-width*.5f+.3f;lateral<width*.5f;lateral+=.6f)
                        {
                            var p=d%2==0?new V3(lateral,2.6f,Directions.DZ[d]*size*.5f):new V3(Directions.DX[d]*size*.5f,2.6f,lateral);
                            Check(Support(g.Structure,p)<.03f,"City edge fence blocks the widened road");
                        }
                    }
                    else {Check(width==c.DoorWidth,"City widened a non-city biome entrance");borders++;}
                }
            }
            layout.TrimNeighbourhood(cx,cz,2);
        }
        Check(joins>100&&borders>0,"Street checks missed city joins or biome borders");
        Console.WriteLine($"City streets: {joins} reciprocal joins and {borders} existing biome border entrances checked; graph exits preserved.");
    }
    static void AirfieldsAndFlight()
    {
        int cells=0;
        foreach(int size in new[]{28,36,60})foreach(int seed in new[]{0,18,481516})
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,Procedural=LiminalGeneration.City11,EnableAircraft=true};
            CityAirfieldCell found;Check(CityAirfields.Find(c,-127,91,out found),"Airfield navigation found no site");
            Check(found.AircraftSpawn,"Airfield destination must be its aircraft cell");
            var layout=new Layout(c);int terminals=0,spawns=0,helicopters=0;
            for(int row=-3;row<=3;row++)for(int column=-1;column<=1;column++)
            {
                int x=found.CenterX+column,z=found.CenterZ+row;CityAirfieldCell tile;
                Check(CityAirfields.At(c,x,z,out tile)&&tile.CenterX==found.CenterX&&tile.CenterZ==found.CenterZ,"Airfield reservation is fragmented");
                var room=layout.At(x,z);var g=Architecture.Build(room,layout);cells++;
                Check(room.Plan.City.Airfield!=null&&room.Plan.City.Valid(room,c)&&room.Plan.Valid(room,c),"Airfield plan violates the real room contract");
                Check(room.Plan.City.Buildings.Length==0&&room.Plan.City.Props.Length==0,"Buildings overlap the runway");
                Validate(g.Structure);Validate(g.Decoration);foreach(var v in g.VisualMeshes)Validate(v);
                Check(Hash(g.Structure,g.Decoration)==Hash(Architecture.Build(room,c).Structure,Architecture.Build(room,c).Decoration),"Airfield geometry depends on generation order");
                for(int side=0;side<4;side++)
                {
                    int dx=Directions.DX[side],dz=Directions.DZ[side];
                    if(Math.Abs(column+dx)>1||Math.Abs(row+dz)>3)continue;
                    Check(room.HasExit(side)&&layout.At(x+dx,z+dz).HasExit((side+2)%4),"Internal runway graph connection is not reciprocal");
                    // Wheels and walkers need actual mesh support up to each seam.
                    foreach(float offset in new[]{-.01f,0,.01f})
                    {
                        var point=new V3(dx*(size*.5f+offset),.04f,dz*(size*.5f+offset));
                        var support=offset<=0?Support(g.Structure,point):Support(Architecture.Build(layout.At(x+dx,z+dz),c).Structure,new V3(point.X-dx*size,.04f,point.Z-dz*size));
                        Check(Math.Abs(support)<.001f,"Airfield seam has no ground support");
                    }
                }
                if(tile.Terminal){terminals++;Check(g.Fixtures.Count==1&&g.Fixtures[0].CustomColor,"Terminal lamp does not match its source");}
                if(tile.AircraftSpawn)spawns++;
                if(tile.HelicopterSpawn){helicopters++;Check(g.VisualMeshes[0].TriangleCount>=132,"Helipad landing marks missing");foreach(var n in g.VisualMeshes[0].Normals)Check(float.IsFinite(n.Y),"Invalid helipad marking normal");}
                Check(Math.Abs(Support(g.Structure,new V3(0,.04f,0)))<.001f,"Runway floor missing");
                if(column==0)Check(Support(g.Structure,new V3(0,2.8f,0))<.02f,"Overhead geometry blocks runway clearance");
            }
            Check(terminals==1&&spawns==1&&helicopters==1,"Airfield duplicates terminal or aircraft spawns");
            CityAirfieldCell outside;Check(!CityAirfields.At(c,found.CenterX+2,found.CenterZ,out outside),"Airfield extends outside its district");
            c.EnableAircraft=false;Check(!CityAirfields.Find(c,0,0,out outside),"Disabled aircraft still replace city buildings");
        }
        var mixed=new LayoutConfig{Seed=31,Continuous=true,Procedural=LiminalGeneration.Expanded,EnableCity11=true,EnableAircraft=true,City11Share=.3f,ConnectedTerrain=true,ConnectedMalls=true};
        CityAirfieldCell site;Check(CityAirfields.Find(mixed,0,0,out site),"Mixed world never generates an accessible airfield");
        for(int dz=-3;dz<=3;dz++)for(int dx=-1;dx<=1;dx++)
        {
            int x=site.CenterX+dx,z=site.CenterZ+dz;ConnectedTerrainSite meadow;ConnectedMallSite mall;
            Check(LiminalRegions.At(mixed,x,z)==LiminalEnvironment.City11&&!ConnectedTerrainTopology.Reserved(mixed,x,z)&&!ConnectedTerrainTopology.Gateway(mixed,x,z,out meadow)&&!ConnectedMallTopology.Gateway(mixed,x,z,out mall),"Runway overwrites a different biome or entrance");
        }
        var reserved=new LayoutConfig{Seed=31,Continuous=true,Procedural=LiminalGeneration.City11,EnableAircraft=true,ConnectedTerrain=true};
        int rx=8+(int)(LiminalRegions.Hash(reserved.Seed,0,-4,0xA18001u)%3),rz=-4*CityAirfields.DistrictSize+8+(int)(LiminalRegions.Hash(reserved.Seed,0,-4,0xA18002u)%3);
        Check(ConnectedTerrainTopology.Reserved(reserved,rx,rz),"Negative airfield sample must actually be inside a terrain reserve");
        Check(!CityAirfields.At(reserved,rx,rz,out site),"Runway illegally replaces connected Level94 terrain");
        foreach(int boundary in new[]{int.MinValue,int.MaxValue})CityAirfields.At(mixed,boundary,boundary,out site);
        var flight=new AircraftDynamics();
        Check(flight.Step(.02f,0,0,0,1,1,1,false).Authority==0,"Stationary aircraft levitates");
        flight.Step(float.NaN,0,0,0,1,0,0,false);Check(float.IsFinite(flight.Throttle),"Invalid time poisoned throttle");
        foreach(float axis in new[]{-100f,-1,0,1,100,float.NaN,float.PositiveInfinity})
        foreach(float speed in new[]{-42f,0,9,18,42,100})
        {
            var f=flight.Step(.02f,speed,0,0,axis,axis,axis,false);
            Check(float.IsFinite(f.Forward)&&float.IsFinite(f.Vertical)&&float.IsFinite(f.Pitch)&&float.IsFinite(f.Roll)&&float.IsFinite(f.YawRate),"Flight input creates nonfinite forces");
            Check(Math.Abs(f.Pitch)<=9&&Math.Abs(f.Roll)<=12&&Math.Abs(f.YawRate)<=18&&f.Forward>=-10&&f.Forward<=5.8f,"Assisted flight exceeds force/turn budget");
        }
        float referenceSpeed=0;
        foreach(int hz in new[]{20,30,50,60,120})
        {
            flight.Reset();float dt=1f/hz,speed=0,travel=0,height=0,vertical=0,takeoffDistance=-1;
            for(int frame=0;frame<hz*8;frame++)
            {
                var f=flight.Step(dt,speed,vertical,height,1,0,0,false);speed+=f.Forward*dt;travel+=speed*dt;if(speed>=AircraftDynamics.TakeoffSpeed&&takeoffDistance<0)takeoffDistance=travel;
                vertical+=(f.Vertical-9.81f)*dt;height+=vertical*dt;if(height<0){height=0;vertical=0;}
            }
            Check(speed>AircraftDynamics.TakeoffSpeed&&takeoffDistance>0&&takeoffDistance<145,"Takeoff speed is not reached before the runway end at minimum cell size");
            if(hz==20)referenceSpeed=speed;else Check(Math.Abs(speed-referenceSpeed)<.15f,"Aircraft acceleration varies excessively with fixed timestep");
            var cruise=flight.Step(dt,AircraftDynamics.MaximumSpeed,0,80,0,0,0,false);
            var brake=flight.Step(dt,AircraftDynamics.MaximumSpeed,0,80,0,0,0,true);
            Check(cruise.Forward<0&&Math.Abs(cruise.Vertical-9.81f)<.001f,"Cruise has no speed limit or level assistance");
            Check(brake.Forward<cruise.Forward,"Air brake fails to reduce forward force");
            var ceiling=flight.Step(dt,30,0,275,0,0,1,false);Check(ceiling.Vertical<9.81f,"Ceiling assistance does not descend");
        }
        Console.WriteLine($"Aircraft: {cells} generated airfield cells; reciprocal runway seams, reservations and deterministic geometry; bounded assisted flight at five fixed timesteps. Pure core assertions, not PhysX flight tests.");
    }
    static void RotorFlight()
    {
        foreach(int hz in new[]{20,30,50,60,120})
        {
            var rotor=new HelicopterDynamics();float dt=1f/hz;
            for(int frame=0;frame<hz*8;frame++)rotor.Step(dt,0,0,0,0,0,0,false);
            Check(rotor.Rotor>.999f&&rotor.Rotor<=1,"Rotor fails to spool up within its budget");
            float height=30,vertical=0,speed=0;
            for(int frame=0;frame<hz*10;frame++)
            {
                var f=rotor.Step(dt,speed,vertical,height,0,0,0,false);vertical+=(f.Vertical-9.81f)*dt;height+=vertical*dt;
            }
            Check(Math.Abs(height-30)<.02f&&Math.Abs(vertical)<.005f,"Hover assistance depends on the fixed timestep or drifts");
            for(int frame=0;frame<hz*8;frame++)
            {
                var f=rotor.Step(dt,speed,vertical,height,1,1,1,false);vertical+=(f.Vertical-9.81f)*dt;height+=vertical*dt;speed+=f.Forward*dt;
                Check(Math.Abs(f.YawRate)<=35&&Math.Abs(f.Pitch)<=11&&Math.Abs(f.Roll)<=5&&Math.Abs(f.Forward)<=5,"Rotorcraft exceeds bounded acceleration/turn angles");
            }
            Check(height>60&&height<75&&speed>17&&speed<=HelicopterDynamics.MaximumSpeed+.01f,"Rotorcraft cannot climb and travel with independent controls");
            for(int frame=0;frame<hz*8;frame++)
            {
                var f=rotor.Step(dt,speed,vertical,height,1,0,-1,true);vertical+=(f.Vertical-9.81f)*dt;height+=vertical*dt;speed+=f.Forward*dt;
            }
            Check(Math.Abs(speed)<.01f&&vertical< -2.9f,"Landing controls fail to brake and descend");
            height=260;vertical=0;
            for(int frame=0;frame<hz*15;frame++){var f=rotor.Step(dt,0,vertical,height,0,0,1,false);vertical+=(f.Vertical-9.81f)*dt;height+=vertical*dt;}
            Check(height<255,"Rotorcraft ceiling allows indefinite ascent");
            rotor.Reset();Check(rotor.Rotor==0,"Rotor state survives reset");
            var rest=rotor.Step(.02f,0,0,0,0,0,0,false);Check(rest.Vertical<.1f,"Parked rotorcraft instantly levitates");
            foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            {
                float before=rotor.Rotor;var stopped=rotor.Step(invalid,0,0,0,1,1,1,false);Check(rotor.Rotor==before&&stopped.Vertical==0,"Invalid dt changes rotor state");
                var sanitised=rotor.Step(.02f,0,0,0,invalid,invalid,invalid,false);Check(float.IsFinite(sanitised.Forward)&&float.IsFinite(sanitised.Vertical)&&float.IsFinite(sanitised.YawRate),"Invalid input poisons flight forces");
            }
        }
        var city=new Layout(new LayoutConfig{Seed=481516,Continuous=true,Procedural=LiminalGeneration.City11,EnableAircraft=true});
        int cars=0,planes=0,helis=0,buildings=0,floors=0,items=0;
        for(int z=0;z<18;z++)for(int x=0;x<18;x++)
        {
            var plan=city.At(x,z).Plan.City;buildings+=plan.Buildings.Length;items+=plan.Props.Length;foreach(var b in plan.Buildings)floors+=b.Floors;
            if(plan.Airfield!=null){if(plan.Airfield.AircraftSpawn)planes++;if(plan.Airfield.HelicopterSpawn)helis++;}
            else if(LiminalRegions.Hash(city.Config.Seed,x,z,0x1100CAu)%9==0)cars++;
        }
        Check(cars+planes+helis>=35&&planes>0&&helis>0,"City sample fails the requested vehicle-instance mix");
        Console.WriteLine($"Active-airfield city snapshot: {buildings} procedural buildings, {floors} floor spaces, {items} licensed item instances, {cars} car sites + {planes} plane + {helis} helicopter. Repeated instances of three vehicle classes, not distinct downloaded models or simultaneously loaded objects.");
        Console.WriteLine("Helicopter: assisted hover, independent climb/travel/yaw, braking, ceiling and invalid-input handling at five fixed timesteps. Mathematical checks, not native PhysX flight.");
    }
    static void SourceFurniturePlacement()
    {
        int placements=0;var seen=new HashSet<int>();
        foreach(int size in new[]{28,36,60})for(int seed=0;seed<8;seed++)
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,Procedural=LiminalGeneration.City11};var room=new Layout(c).At(seed-9,seed+4);var g=Architecture.Build(room,c);
            for(int i=0;i<4;i++)
            {
                var b=room.Plan.City.Buildings[i];var prop=room.Plan.City.Props[8+i];var dims=SourceFurniture.Dimensions(prop.Source);seen.Add(prop.Source);
                Check(SourceFurniture.Id(prop.Source)!=null&&SourceFurniture.Longest(prop.Source)>=1,"Unknown city furniture scale");
                var delta=prop.Position-new V3(b.X,0,b.Z);float x=b.Turn?-delta.X:delta.X,z=b.Turn?-delta.Z:delta.Z;
                Check(x-dims.X*.5f>=1.1f&&x+dims.X*.5f<b.Width*.5f-.4f,"Furniture blocks building entry or wall margin");
                float stairRight=-b.Width*.5f+4.3f;
                Check(x-dims.X*.5f>=stairRight+.66f,"Furniture obstructs stair landing circulation");
                Check(z-dims.Z*.5f>.575f&&z+dims.Z*.5f<b.Depth*.28f-.375f,"Furniture intersects counter or rear cabinet");
                if(prop.Source==3||prop.Source==5)Check(b.Width>=10&&b.Depth>=10&&z-dims.Z*.5f>.375f+.7f,"Sofa squeezed into a narrow shop bay");
                foreach(float dx in new[]{-.49f,0,.49f})foreach(float dz in new[]{-.49f,0,.49f})
                {
                    var foot=prop.Position+new V3(dx*dims.X,0,dz*dims.Z);Check(Math.Abs(Support(g.Structure,foot)-.12f)<.001f,"Furniture footprint spans missing floor");
                }
                placements++;
            }
            foreach(var environment in new[]{LiminalEnvironment.Archive906,LiminalEnvironment.Courtyard188})
            {
                var q=new Room{X=seed-9,Z=seed+4,Exits=15,Environment=environment};QuietLevelPlanner.Describe(q,c);var mesh=Architecture.Build(q,c).Structure;
                foreach(var prop in q.Plan.Quiet.Props)
                {
                    if(prop.CatalogId>=0)continue;int source=SourceFurniture.Index(prop.Source);var dims=SourceFurniture.Dimensions(source);float angle=prop.Yaw*MathF.PI/180,co=MathF.Cos(angle),si=MathF.Sin(angle);
                    if(source==3||source==5)Check(environment==LiminalEnvironment.Courtyard188&&size>=36,"Sofa replaces a tight archive seat");
                    foreach(float dx in new[]{-.49f,0,.49f})foreach(float dz in new[]{-.49f,0,.49f})
                    {
                        float px=dx*dims.X,pz=dz*dims.Z;var foot=prop.Bottom+new V3(px*co+pz*si,0,-px*si+pz*co);Check(Math.Abs(Support(mesh,foot))<.001f,"Quiet furniture footprint crosses a stair void");
                    }
                    placements++;
                }
            }
        }
        Check(seen.SetEquals(new[]{2,3,4,5}),"City generation never selects one of its actual furniture treatments");
        var city=new Layout(new LayoutConfig{Seed=481516,CellSize=36,Continuous=true,Procedural=LiminalGeneration.City11,EnableAircraft=true});var counts=new int[9];
        for(int z=0;z<18;z++)for(int x=0;x<18;x++)foreach(var prop in city.At(x,z).Plan.City.Props)counts[prop.Source]++;
        Console.WriteLine($"Furniture: {placements} supported source placements checked; active-airfield sample has {counts[2]} original chairs, {counts[3]} velvet sofas, {counts[5]} wood/leather sofas, {counts[4]} damask chair variants and {counts[6]} antique camera instances. Chair variants are not new geometry families.");
    }
    static void ExpandedFurniturePlacement()
    {
        int homes=0,homeSeats=0,mallSectors=0,mallSeats=0;
        var sourceTypes=new HashSet<int>();
        foreach(int seed in new[]{0,94,481516})
        {
            var region=TerrainRegionPlan.Create(seed,3,-4);int sampled=0;
            foreach(var house in region.Houses)
            {
                if(sampled++>=12)break;var data=TerrainHouseArchitecture.Build(house,true,region);homes++;
                Check(data.Geometry.SourceSeats.Count<=2,"House exceeds bounded source furniture budget");
                Check(TerrainHouseArchitecture.Build(house,false,region).Geometry.SourceSeats.Count==0,"Distant house loads source furniture");
                var slots=new HashSet<int>();
                foreach(var seat in data.Geometry.SourceSeats)
                {
                    Check(slots.Add(seat.Slot)&&seat.Slot>=8000,"Furniture ownership collides with existing house items");
                    Check(seat.Bottom.Y==.02f,"Furniture entered a basement or protected upper flight");
                    Check(SourceFurniture.Clear(data.Geometry.Structure,seat)&&SourceFurniture.Clear(data.Geometry.Decoration,seat)&&SourceFurniture.Clear(data.Geometry.Glass,seat),"Home source furniture intersects final architecture/details");
                    var half=SourceFurniture.HalfExtents(seat.Source,seat.Yaw,.04f);var hole=house.Blueprint.StairHole;
                    Check(seat.Bottom.X+half.X<hole.MinX-.3f||seat.Bottom.X-half.X>hole.MaxX+.3f||seat.Bottom.Z+half.Z<hole.MinZ-.3f||seat.Bottom.Z-half.Z>hole.MaxZ+.3f,"Furniture obstructs house stairs");
                    int x=(int)MathF.Floor((seat.Bottom.X+house.Width*.5f)/7),z=(int)MathF.Floor((seat.Bottom.Z+house.Depth*.5f)/7);
                    Check(x!=1||z!=0,"Furniture entered the stair cell");
                    Check(seat.Bottom.X+half.X<=house.Blueprint.X(x)-1.2f,"Furniture narrows central cross-route below 2.4 m");
                    foreach(int dx in new[]{-1,0,1})foreach(int dz in new[]{-1,0,1})
                    {var p=seat.Bottom+new V3(dx*half.X,0,dz*half.Z);Check(Math.Abs(Support(data.Geometry.Structure,p))<.001f,"House source footprint lacks floor support");}
                    homeSeats++;sourceTypes.Add(seat.Source);
                }
            }
            var mall=MallPlan.Create(seed,-5,9,true,true,true);var shell=MallArchitecture.Shell(mall);
            for(int f=0;f<mall.Floors;f++)foreach(int sector in new[]{1,mall.Bays/2,mall.Bays})
            {
                var data=MallArchitecture.Sector(mall,f,sector,12).Geometry;mallSectors++;
                Check(data.SourceSeats.Count<=2,"Mall sector exceeds furniture budget");
                foreach(var seat in data.SourceSeats)
                {
                    Check(seat.Slot>=8100&&seat.Bottom.Y==f*MallPlan.FloorHeight+.02f,"Mall furniture floor or stable ownership changed");
                    Check(SourceFurniture.Clear(data.Structure,seat)&&SourceFurniture.Clear(data.Decoration,seat)&&SourceFurniture.Clear(data.Glass,seat)&&SourceFurniture.Clear(shell.Structure,seat)&&SourceFurniture.Clear(shell.Glass,seat),"Mall seat intersects final shell, frontage, railing or furnishing");
                    var half=SourceFurniture.HalfExtents(seat.Source,seat.Yaw,.04f);
                    Check(Math.Abs(seat.Bottom.Z)-half.Z>=20,"Furniture blocks the main mall gallery");
                    Check(Math.Abs(seat.Bottom.Z)+half.Z<=MallPlan.ShopFront-.8f,"Furniture blocks storefront passage");
                    foreach(int dx in new[]{-1,0,1})foreach(int dz in new[]{-1,0,1})
                    {var p=seat.Bottom+new V3(dx*half.X,0,dz*half.Z);Check(mall.FloorSupported(f,p.X,p.Z)&&Math.Abs(Support(shell.Structure,p)-f*MallPlan.FloorHeight)<.001f,"Mall furniture spans atrium or stair void");}
                    mallSeats++;sourceTypes.Add(seat.Source);
                }
            }
        }
        Check(homeSeats>20&&mallSeats>30&&sourceTypes.Contains(3)&&sourceTypes.Contains(4),"Expansion does not populate real houses and malls");
        Console.WriteLine($"Expanded furniture: {homeSeats} supported seats in {homes} actual generated homes; {mallSeats} seats in {mallSectors} multistorey mall sectors. Same licensed families; <=2 per detailed home/sector.");
    }
    static void RegionalSourcePlacement()
    {
        int props=0;var environments=new HashSet<LiminalEnvironment>();var types=new HashSet<int>();
        foreach(var mode in new[]{LiminalGeneration.Poolrooms,LiminalGeneration.IndoorPlay,LiminalGeneration.Supermarkets,LiminalGeneration.Offices,LiminalGeneration.Hotels,LiminalGeneration.WaitingHalls,LiminalGeneration.DiningHalls,LiminalGeneration.SoftPlay})
        foreach(int size in new[]{36,60})for(int seed=0;seed<8;seed++)
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,AutonomousGeneration=true,Procedural=mode,CurveSegments=24};var r=new Layout(c).At(seed-3,seed+6);var g=Architecture.Build(r,c);
            Check(g.SourceProps.Count<=2,"Regional supports created an unbounded source population");var keys=new HashSet<int>();
            foreach(var p in g.SourceProps)
            {
                Check(keys.Add(p.Slot),"Regional object ownership keys collide");
                Check(SourceFurniture.Clear(g.Structure,p)&&SourceFurniture.Clear(g.Decoration,p)&&SourceFurniture.Clear(g.Glass,p)&&SourceFurniture.Clear(g.CollisionOnly,p),"Regional object intersects architecture or a safety collider");
                var half=SourceFurniture.HalfExtents(p.Source,p.Yaw,.025f);
                foreach(int x in new[]{-1,0,1})foreach(int z in new[]{-1,0,1})Check(RegionalSourcePlanner.Supported(g.Structure,new V3(p.Bottom.X+x*half.X,p.Bottom.Y-RegionalSourcePlanner.ContactGap,p.Bottom.Z+z*half.Z)),"Regional source footprint floats over an edge");
                Check(Math.Abs(p.Bottom.X)-half.X>r.Plan.ClearHalfWidth-.1f&&Math.Abs(p.Bottom.Z)-half.Z>r.Plan.ClearHalfWidth-.1f,"Regional object blocks central circulation");props++;environments.Add(r.Environment);types.Add(p.Source);
            }
        }
        Check(props>20&&environments.Count>=4,"Support planner does not populate existing interiors");
        Check(types.Contains(0)&&types.Contains(1)&&types.Contains(6),"Regional source selection misses a real placed source");
        Console.WriteLine($"Regional supports: {props} source placements in {environments.Count} biome types; nine-point support, final geometry clearance and ownership uniqueness; at most two per room.");
    }
    static void UrbanAndQuietRefinement()
    {
        var typologies=new HashSet<int>();var districts=new HashSet<int>();int cameras=0,coastalSeats=0;
        foreach(int size in new[]{28,36,60})for(int seed=0;seed<12;seed++)
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,Procedural=LiminalGeneration.City11};var r=new Layout(c).At(seed-8,seed+4);var g=Architecture.Build(r,c);
            districts.Add(CityStreetDetails.District(c,r.X,r.Z));foreach(var b in r.Plan.City.Buildings)typologies.Add(CityStreetDetails.Typology(b));
            Check(g.VisualMeshes.Count==3,"Urban detail expanded into per-object render batches");
            foreach(var p in r.Plan.City.Props)if(p.Source==6)
            {var q=new SourceFurniturePlacement(0,p.Source,p.Position,p.Yaw);Check(SourceFurniture.Clear(g.Structure,q)&&SourceFurniture.Clear(g.Decoration,q)&&SourceFurniture.Clear(g.CollisionOnly,q),"City standing camera intersects a counter, stairs or window");cameras++;}
            var court=new Room{X=seed-8,Z=seed+4,Exits=15,Environment=LiminalEnvironment.Courtyard188};QuietLevelPlanner.Describe(court,c);var cg=Architecture.Build(court,c);
            // A low capsule crosses from court to apartment through the actual door.
            float inner=size*.25f,doorZ=-size*.5f+11.3f;
            for(float x=inner-.5f;x<=inner+3;x+=.2f)
            {var passage=new SourceFurniturePlacement(0,0,new V3(x,.45f,doorZ),0);Check(SourceFurniture.Clear(cg.CollisionOnly,passage,0),"Ground railing still seals the courtyard wing");}
            Check(cg.CollisionOnly.Positions.Any(p=>Math.Abs(p.X-inner-.08f)<.04f&&p.Y>3.19f&&p.Y<4.31f),"Upper courtyard safety railing was removed");
            var coastalConfig=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,Procedural=LiminalGeneration.Coast48};var coast=new Layout(coastalConfig).At(0,seed-4);var sea=Architecture.Build(coast,coastalConfig);
            foreach(var seat in sea.SourceSeats)
            {Check(SourceFurniture.Clear(sea.Structure,seat)&&SourceFurniture.Clear(sea.Decoration,seat),"Coastal sofa intersects pavilion/table");var half=SourceFurniture.HalfExtents(seat.Source,seat.Yaw,.03f);foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Check(RegionalSourcePlanner.Supported(sea.Structure,new V3(seat.Bottom.X+x*half.X,0,seat.Bottom.Z+z*half.Z)),"Coastal furniture extends beyond real dry floor");coastalSeats++;}
        }
        Check(typologies.Count==6&&districts.Count==4&&cameras>0&&coastalSeats>0,"Refinement coverage missed source placements or architectural forms");
        Console.WriteLine($"Refinement: six facade typologies / four district descriptors, {cameras} standing city cameras, {coastalSeats} supported coastal sofas; ground court passage open and upper rails retained.");
    }
    static void BistroTerraces()
    {
        int terraces=0,items=0;
        foreach(int size in new[]{28,36,60})for(int seed=0;seed<32;seed++)
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,Procedural=LiminalGeneration.City11,EnableAircraft=true};
            var room=new Layout(c).At(seed-14,seed+7);var plan=room.Plan.City;var g=Architecture.Build(room,c);
            var selected=plan.Props.Where(p=>p.Source>=7).ToArray();
            Check(selected.Length==0||selected.Length==3,"Terraces exceeded one table/two chairs per block");
            Check(CityTerraces.TryBuilding(plan.Buildings,out var building)==(selected.Length==3),"Native awning selection diverged from furniture plan");
            if(selected.Length==0)continue;
            Check(CityStreetDetails.Typology(building)!=0&&CityStreetDetails.Typology(building)!=4,"Awning intersects existing arcade porch");
            foreach(var prop in selected)
            {
                var p=new SourceFurniturePlacement(0,prop.Source,prop.Position,prop.Yaw);var half=SourceFurniture.HalfExtents(p.Source,p.Yaw,.03f);
                Check(SourceFurniture.Clear(g.Structure,p)&&SourceFurniture.Clear(g.Decoration,p)&&SourceFurniture.Clear(g.CollisionOnly,p),"Terrace furniture intersects final facade, road fixture or glass");
                Check(Math.Abs(p.Bottom.X)-half.X>3&&Math.Abs(p.Bottom.Z)-half.Z>3,"Terrace blocks six-metre driving cross");
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)Check(Math.Abs(Support(g.Structure,p.Bottom+new V3(x*half.X,0,z*half.Z)))<.001f,"Terrace footprint lacks real pavement support");
                items++;
            }
            for(int i=0;i<selected.Length;i++)for(int k=i+1;k<selected.Length;k++)
            {
                var a=SourceFurniture.HalfExtents(selected[i].Source,selected[i].Yaw,.02f);var b=SourceFurniture.HalfExtents(selected[k].Source,selected[k].Yaw,.02f);var delta=selected[i].Position-selected[k].Position;
                Check(Math.Abs(delta.X)>a.X+b.X||Math.Abs(delta.Z)>a.Z+b.Z,"Terrace source colliders overlap each other");
            }
            // Existing shop door and its central approach stay open to a 1.4m aisle.
            foreach(var p in selected)
            {var delta=p.Position-new V3(building.X,0,building.Z);var half=SourceFurniture.HalfExtents(p.Source,p.Yaw,.02f);Check(Math.Abs(delta.X)-half.X>1.8f,"Cafe furniture narrows the shop entrance approach");}
            var awning=CityTerraces.AwningBottom(building);Check(awning.Y>=3.05f,"Awning intrudes on standing headroom");terraces++;
        }
        Check(terraces>0&&items==terraces*3,"Bistro sources never reach actual world generation");
        Console.WriteLine($"Bistro: {terraces} bounded terraces / {items} interactable table-chair instances, supported pavement, clear shop/road routes, non-overlapping furniture and no competing porch. Native shading/traversal unrun.");
    }
    static float Support(MeshData mesh,V3 origin)
    {
        float highest=float.NegativeInfinity;
        foreach(var indices in mesh.Indices)for(int i=0;i<indices.Count;i+=3)
        {
            var a=mesh.Positions[indices[i]];var b=mesh.Positions[indices[i+1]];var c=mesh.Positions[indices[i+2]];
            float determinant=(b.Z-c.Z)*(a.X-c.X)+(c.X-b.X)*(a.Z-c.Z);if(Math.Abs(determinant)<1e-8f)continue;
            float u=((b.Z-c.Z)*(origin.X-c.X)+(c.X-b.X)*(origin.Z-c.Z))/determinant;
            float v=((c.Z-a.Z)*(origin.X-c.X)+(a.X-c.X)*(origin.Z-c.Z))/determinant;
            if(u<-.00001f||v<-.00001f||u+v>1.00001f)continue;float y=u*a.Y+v*b.Y+(1-u-v)*c.Y;
            if(y<=origin.Y+.0001f&&y>highest)highest=y;
        }
        return highest;
    }
    static void DressingGeneration()
    {
        var shapes=new HashSet<string>();
        for(int motif=0;motif<8;motif++)for(int variant=0;variant<2;variant++)
        {
            var m=ArchitecturalDressing.Fitting(motif,variant);Validate(m);shapes.Add(Hash(m));
            Check(m.TriangleCount<=216,"Fitting detail exceeded budget");
            foreach(var v in m.Positions)Check(Math.Abs(v.X)<=ArchitecturalDressing.HalfWidth&&Math.Abs(v.Y)<=ArchitecturalDressing.HalfHeight&&v.Z>=0&&v.Z<.13f,"Fitting extends outside supported footprint");
        }
        Check(shapes.Count==16,"Geometrical dressing variants are duplicates");
        var portal=new MeshData();Shapes.Portal(portal,18,4,0,4,3,.3f,12,1.2f);string original=Hash(portal);
        int total=0;
        for(uint seed=0;seed<16;seed++)
        {
            var g=ArchitecturalDressing.Build(portal,seed);var again=ArchitecturalDressing.Build(portal,seed);Validate(g.First);Validate(g.Second);
            Check(Hash(g.First,g.Second)==Hash(again.First,again.Second),"Dressing not deterministic");
            Check(g.Anchors.Count>0&&g.Anchors.Count<=24&&g.First.TriangleCount+g.Second.TriangleCount<=5184,"Dressing budgets invalid");
            foreach(var a in g.Anchors)
            {
                Check(Math.Abs(a.Normal.Y)<.005f,"Fitting mounted on floor or inclined arch");
                // This rectangular part of the door is empty regardless of the arch tessellation.
                Check(Math.Abs(a.Center.X)>=2+ArchitecturalDressing.HalfWidth||a.Center.Y-ArchitecturalDressing.HalfHeight>=1,"Dressing bridges a doorway");
                foreach(var b in g.Anchors)if(!a.Center.Equals(b.Center))Check((a.Center-b.Center).Length>=1.1f,"Fittings overlap");
            }
            total+=g.Anchors.Count;
        }
        Check(Hash(portal)==original,"Dressing changed supporting geometry");
        Check(ArchitecturalDressing.Build(new MeshData(),3).Anchors.Count==0,"Dressing invented support in empty space");
        foreach(var mode in new[]{LiminalGeneration.Poolrooms,LiminalGeneration.SoftPlay,LiminalGeneration.IndoorPlay,LiminalGeneration.Supermarkets,LiminalGeneration.Offices,LiminalGeneration.City11,LiminalGeneration.Archive906,LiminalGeneration.Courtyard188,LiminalGeneration.Coast48})
        {
            var layout=new Layout(new LayoutConfig{Seed=12,Continuous=true,Procedural=mode});var g=Architecture.Build(layout.At(0,0),layout);
            Check(g.Dressing!=null,"Room generation missed architectural detail");Validate(g.Dressing.First);Validate(g.Dressing.Second);
        }
        var mall=MallPlan.Create(81,0,0,true,true,true);
        Check(MallArchitecture.Shell(mall).Dressing!=null&&MallArchitecture.Sector(mall,0,1,12).Geometry.Dressing!=null,"Mall path missed architectural detail");
        var house=new TerrainHousePlan{Seed=10,Width=12,Depth=14,Floors=2};
        Check(TerrainHouseArchitecture.Build(house,true).Geometry.Dressing!=null&&TerrainHouseArchitecture.Build(house,false).Geometry.Dressing==null,"Distant houses gained extra detail");
        house.GenerateArchitecture();Check(TerrainHouseArchitecture.Build(house,true).Geometry.Dressing!=null,"Varied houses missed detail");
        var prefs=new GamePreferences{dressingVersion=0,dressingDensity=0};prefs.Sanitize();Check(prefs.dressingDensity==2,"Old preferences did not migrate decoration");
        prefs.dressingDensity=0;prefs.Sanitize();Check(prefs.dressingDensity==0,"Explicit original detail choice lost");
        Console.WriteLine($"Dressing: 8 procedural fitting types / 16 distinct geometry variants; {total} doorway-safe placements in 16 wall samples. Not new downloaded source models.");
    }
    static void ImportedCityBuildings()
    {
        int buildings=0,source1=0,source2=0;
        var config=new LayoutConfig{Seed=481516,Continuous=true,Procedural=LiminalGeneration.City11,CellSize=36,EnableImportedBuildings=true,CurveSegments=12};
        var layout=new Layout(config);
        for(int z=-9;z<9;z++)for(int x=-9;x<9;x++)
        {
            var r=layout.At(x,z);Check(r.Plan.City.Valid(r,config),"Imported building invalidates its city block");
            int perBlock=0;
            foreach(var b in r.Plan.City.Buildings)
            {
                if(b.ExteriorSource==0)continue;
                buildings++;perBlock++;if(b.ExteriorSource==1)source1++;else source2++;
                Check(b.Width==10&&b.Depth==10&&b.Floors==4,"Native building skin and interior do not share a canonical fit");
                Check(Math.Abs(b.X)-5.3f>=4.49f&&Math.Abs(b.Z)-5.3f>=4.49f,"Source exterior encroaches on the street");
                Check(Math.Abs(b.X)+5.3f<18&&Math.Abs(b.Z)+5.3f<18,"Source decoration exceeds reserved room extent");
                var g=Architecture.Build(r,layout);Validate(g.Structure);Validate(g.Decoration);
                foreach(int level in new[]{0,1,2,3})
                {
                    float y=.12f+level*3.2f;
                    Check(Math.Abs(Support(g.Structure,b.Point(1,y+.05f,1))-y)<.001f,"A source building storey has no floor");
                    if(level<3)for(int flight=0;flight<2;flight++)for(int step=0;step<9;step++)
                    {
                        float sx=-2.8f+(flight==0?-.95f:.95f),sz=flight==0?-1.35f+(step+.5f)*.3f:1.35f-(step+.5f)*.3f,top=y+flight*1.6f+(step+1)*1.6f/9;
                        Check(Math.Abs(Support(g.Structure,b.Point(sx,top+.03f,sz))-top)<.001f,"Source building stair support missing");
                        Check(Support(g.Structure,b.Point(sx,top+1.85f,sz))<=top+.04f,"Source building upper slab blocks stair headroom");
                    }
                }
                Check(Support(g.Structure,b.Point(0,2,-4.9f))<=.13f,"Source entry approach is blocked by core architecture");
                if(CityTerraces.TryBuilding(r.Plan.City.Buildings,out var terrace))Check(terrace.ExteriorSource==0,"A competing cafe awning crosses the imported facade");
            }
            Check(perBlock<=1,"Unbounded imported models per streamed block");
        }
        Check(source1>=10&&source2>=10,"One of the licensed building families never generates");
        config.EnableAircraft=true;var aerial=new Layout(config);int structures=0,floors=0,objects=0,imported=0,cars=0,planes=0,helicopters=0;
        for(int z=-9;z<9;z++)for(int x=-9;x<9;x++)
        {
            var p=aerial.At(x,z).Plan.City;structures+=p.Buildings.Length;floors+=p.Buildings.Sum(b=>b.Floors);objects+=p.Props.Length;imported+=p.Buildings.Count(b=>b.ExteriorSource>0);
            if(p.Airfield!=null){if(p.Airfield.AircraftSpawn)planes++;if(p.Airfield.HelicopterSpawn)helicopters++;}
            else if(LiminalRegions.Hash(config.Seed,x,z,0x1100CAu)%9==0)cars++;
        }
        Check(structures>300&&floors>500&&objects>2000&&cars+planes+helicopters>=35,"Integrated city no longer meets its distributed instance targets");
        Console.WriteLine($"R17 integrated city snapshot: {structures} generated buildings, {floors} floor spaces, {objects} source-object instances, {imported} imported exterior skins, {cars} car sites + {planes} plane + {helicopters} helicopter. These are distributed instances, not distinct downloaded models or simultaneous loaded objects.");
        config.EnableAircraft=false;
        config.CellSize=28;var small=new Layout(config);for(int i=-5;i<=5;i++)Check(small.At(i,i).Plan.City.Buildings.All(b=>b.ExteriorSource==0),"Imported facade squeezed into a narrow block");
        config.CellSize=36;config.EnableImportedBuildings=false;var original=new Layout(config);for(int i=-5;i<=5;i++)Check(original.At(i,i).Plan.City.Buildings.All(b=>b.ExteriorSource==0),"Disabled source catalog changes an original city");
        Console.WriteLine($"Imported architecture: {buildings} exterior-skin instances from 2 source families ({source1}/{source2}) in 324 blocks; four physical storeys, U-stair supports/headroom, street clearance and small-block fallback checked. Native visuals unrun.");
    }
    static void CameraLandingResponse()
    {
        foreach(int fps in new[]{10,20,30,60,144,240})
        {
            var response=new CameraLanding();float dt=1f/fps;
            Check(response.Step(true,0,true,1,dt)==0,"Initial spawn causes a false landing impulse");
            response.Step(false,-12,true,1,dt);float impact=response.Step(true,0,true,1,dt);
            Check(impact<0&&impact>=-.075f,"Landing response is missing or excessive");
            Check(response.Step(true,0,true,1,0)==impact,"Camera contact moves while paused");
            float previous=impact;
            for(int frame=0;frame<fps;frame++)
            {float next=response.Step(true,0,true,1,dt);Check(next>=previous&&next<=0,"Camera landing bounces or overshoots");previous=next;}
            Check(Math.Abs(previous)<.00001f,"Landing fails to settle at low FPS");
            response.Step(false,-12,true,1,dt);response.Step(true,0,true,1,dt);
            Check(response.Step(true,0,false,1,dt)==0,"Reduced motion retains landing offset");
            Check(response.Step(false,float.NaN,true,1,dt)==0,"Nonfinite velocity enters camera pose");
        }
        Check(CameraPresentation.Look(CameraViewMode.Architectural).Grain==0&&CameraPresentation.Look(CameraViewMode.Architectural).Chromatic==0,"Architectural look adds visual noise");
        var old=new CameraPreferences{version=3,landingMotion=false,viewMode=CameraViewMode.Nostalgia};old.Sanitize();
        Check(old.version==4&&old.landingMotion&&old.viewMode==CameraViewMode.Nostalgia,"Camera migration loses the selected look");
        old.landingMotion=false;old.Sanitize();Check(!old.landingMotion,"Explicit landing preference overwritten");
        Console.WriteLine("Camera landing: bounded and monotonic at 10–240 FPS; pause, reset and preference migration checked mathematically.");
    }
    static void CivicGeneration()
    {
        int built=0,objects=0;var variants=new HashSet<string>();
        Check((int)LiminalEnvironment.Coast48==11&&(int)SurfaceTheme.Airfield==20,"Existing serialized IDs changed");
        foreach(var mode in new[]{LiminalGeneration.Conservatory,LiminalGeneration.TransitGallery})foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})for(int seed=0;seed<6;seed++)
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,DoorWidth=door,DoorHeight=Math.Max(4.2f,door*.5f+2.2f),Continuous=true,Procedural=mode,CurveSegments=12};var layout=new Layout(c);var r=layout.At(seed-3,7);
            Check(r.Plan.Quiet!=null&&r.Plan.Valid(r,c),"Civic region is not integrated into Layout");
            var g=Architecture.Build(r,layout);Validate(g.Structure);Validate(g.Decoration);Validate(g.Glass);Validate(g.CollisionOnly);
            Check(g.Structure.Theme==(mode==LiminalGeneration.Conservatory?SurfaceTheme.Conservatory:SurfaceTheme.TransitGallery),"New level uses another biome's palette");
            Check(g.Glass.TriangleCount>0,"Civic daylight roof has no glazing");
            Check(Support(g.Structure,new V3(0,r.Height+.3f,0))<.03f,"An opaque roof blocks the skylight");
            var repeat=Architecture.Build(new Layout(c).At(seed-3,7),c);
            Check(Hash(g.Structure,g.Decoration,g.Glass)==Hash(repeat.Structure,repeat.Decoration,repeat.Glass),"Civic generation is not reproducible");
            variants.Add(mode+":"+Hash(g.Structure,g.Decoration,g.Glass));
            for(int d=0;d<4;d++)if(r.HasExit(d))for(float t=0;t<size*.5f-.2f;t+=.45f)
            {
                var point=new V3(Directions.DX[d]*t,1.9f,Directions.DZ[d]*t);
                Check(Math.Abs(Support(g.Structure,point))<.002f,"Civic doorway approach contains a step or obstruction");
            }
            for(int i=0;i<r.Plan.Quiet.Props.Length;i++)
            {
                var p=r.Plan.Quiet.Props[i];int source=SourceFurniture.Index(p.Source);var half=SourceFurniture.HalfExtents(source,p.Yaw);
                Check(Math.Abs(p.Bottom.X)+half.X<size*.5f-.3f&&Math.Abs(p.Bottom.Z)+half.Z<size*.5f-.3f,"Civic furniture penetrates envelope");
                Check(SourceFurniture.Clear(g.Structure,new SourceFurniturePlacement(i,source,p.Bottom,p.Yaw),.005f),"Civic furnishing intersects solid architecture");
                if(source==0)
                {var table=r.Plan.Quiet.Props[i-1];Check(Math.Abs(p.Bottom.Y-(table.Bottom.Y+SourceFurniture.Dimensions(7).Y+.02f))<.0001f,"Bottle unsupported by its cafe table");}
                else Check(Math.Abs(Support(g.Structure,p.Bottom)-p.Bottom.Y+.02f)<.001f,"Civic furniture is unsupported");
                Check(Math.Abs(p.Bottom.X)-half.X>door*.5f&&Math.Abs(p.Bottom.Z)-half.Z>door*.5f,"Furniture blocks central circulation");objects++;
            }
            Check(g.Fixtures.All(f=>f.CustomColor&&f.Power>0&&f.Range>0),"Unspecified civic fixture colour or range");built++;
        }
        var mixed=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.Expanded,EnableCivicLevels=true};
        foreach(var e in new[]{LiminalEnvironment.Conservatory,LiminalEnvironment.TransitGallery})
        {Check(LiminalRegions.Find(mixed,e,0,0,out int x,out int z),"Civic destination missing from mixed exploration / TP");Check(LiminalRegions.At(mixed,x,z)==e,"Civic TP selects a different environment");}
        mixed.EnableCivicLevels=false;Check(!LiminalRegions.Find(mixed,LiminalEnvironment.Conservatory,0,0,out _,out _),"Disabled civic regions still found");
        Check(variants.Count>=24,"New civic rooms lack architectural variation");
        Console.WriteLine($"Civic levels: {built} generated rooms, {variants.Count} geometry variants and {objects} furniture instances checked; 2 new procedural environments, zero new downloaded models.");
    }
    static void QuietGeneration()
    {
        int built=0,pickups=0;
        foreach(var mode in new[]{LiminalGeneration.Archive906,LiminalGeneration.Courtyard188,LiminalGeneration.Coast48})foreach(int size in new[]{28,36,60})for(int seed=0;seed<8;seed++)
        {
            var config=new LayoutConfig{Seed=seed,CellSize=size,Continuous=true,Procedural=mode,CurveSegments=12};var layout=new Layout(config);var room=layout.At(seed-4,-5);
            Check(room.Plan.Quiet!=null&&room.Plan.Valid(room,config),"New level not integrated in actual layout");var data=Architecture.Build(room,layout);
            Validate(data.Structure);Validate(data.Decoration);Validate(data.Glass);Validate(data.Water);Validate(data.CollisionOnly);
            foreach(var link in data.Connectors)if(link!=null)Validate(link.Structure);
            foreach(var prop in room.Plan.Quiet.Props)
            {Check(Math.Abs(prop.Bottom.Y-Support(data.Structure,prop.Bottom)-.02f)<.001f,"New level item floats or overlaps furniture");pickups++;}
            if(mode!=LiminalGeneration.Coast48)
            {
                float half=size*.5f,sx=mode==LiminalGeneration.Archive906?half-3:half*.75f,sz=-half+(mode==LiminalGeneration.Archive906?4.2f:4);
                int floors=mode==LiminalGeneration.Archive906?2:room.Plan.Quiet.Floors;
                foreach(int sign in new[]{-1,1})for(int level=0;level<floors-1;level++)for(int flight=0;flight<2;flight++)for(int step=0;step<9;step++)
                {
                    float x=sign*sx+(flight==0?-.95f:.95f),z=sz+(flight==0?-1.35f+(step+.5f)*.3f:1.35f-(step+.5f)*.3f),y=level*3.2f+flight*1.6f+(step+1)*1.6f/9;
                    Check(Math.Abs(Support(data.Structure,new V3(x,y+.03f,z))-y)<.001f,"New level stair has no support");
                    Check(Support(data.Structure,new V3(x,y+1.85f,z))<=y+.04f,"Upper floor blocks the stair headroom");
                }
            }
            if(mode==LiminalGeneration.Coast48)
            {
                foreach(var p in new[]{new V3(0,.1f,0),new V3(0,.1f,size*.49f),new V3(size*.49f,.1f,0)})Check(Math.Abs(Support(data.Structure,p)-.06f)<.001f,"Coastal boardwalk has a gap");
                if(room.Plan.Quiet.Sea.Width>0)Check(room.Plan.Pools.Length==1&&data.Regions.Count==1,"Sea not registered for native water and swimming");
            }
            built++;
        }
        var settings=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.Poolrooms,Seed=23};var original=new Layout(settings);var r=original.At(4,8);var before=Architecture.Build(r,original);
        settings.EnableQuietLevels=true;var prepared=new Layout(settings);var after=Architecture.Build(prepared.At(4,8),prepared);
        Check(Hash(before.Structure,before.Decoration,before.Water)==Hash(after.Structure,after.Decoration,after.Water),"Preparing future sockets changed existing room geometry");
        Check(after.PreparedTransitions.Count==3&&after.PreparedTransitions.All(p=>!p.Active),"Future transition metadata became active");
        var mixed=new LayoutConfig{Procedural=LiminalGeneration.Expanded,Continuous=true,EnableQuietLevels=true,QuietLevelShare=.5f};
        foreach(var env in new[]{LiminalEnvironment.Archive906,LiminalEnvironment.Courtyard188,LiminalEnvironment.Coast48})
        {int x,z;Check(LiminalRegions.Find(mixed,env,0,0,out x,out z),"New level TP cannot find a region");}
        int coastalEdges=0;
        for(int z=-48;z<=48;z++)for(int x=-48;x<=48;x++)
        {
            if(LiminalRegions.At(mixed,x,z)!=LiminalEnvironment.Coast48)continue;
            var a=new Room{X=x,Z=z,Environment=LiminalEnvironment.Coast48};QuietLevelPlanner.Describe(a,mixed);float half=mixed.CellSize*.5f;
            for(int d=0;d<2;d++)
            {
                int nx=x+Directions.DX[d],nz=z+Directions.DZ[d];if(LiminalRegions.At(mixed,nx,nz)!=LiminalEnvironment.Coast48)continue;
                var b=new Room{X=nx,Z=nz,Environment=LiminalEnvironment.Coast48};QuietLevelPlanner.Describe(b,mixed);
                if(d==1)Check((a.Plan.Quiet.Shore>=half-.01f?0:-2)==(b.Plan.Quiet.Shore<=-half+.01f?-2:0),"Coastal shore repeats discontinuously at east/west cell seam");
                else Check(a.Plan.Quiet.Shore==b.Plan.Quiet.Shore,"Coastal shoreline jumps between north/south neighbours");
                coastalEdges++;
            }
        }
        Check(coastalEdges>0,"No coastal seam samples were tested");
        Console.WriteLine($"Quiet levels: {built} rooms across three sizes; {pickups} actual item supports, stair headroom, sea/boardwalk registration and inactive legacy sockets checked.");
    }
}
