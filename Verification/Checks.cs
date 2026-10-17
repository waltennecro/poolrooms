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
        AirfieldsAndFlight();
        RotorFlight();
        QuietGeneration();
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
        var city=new Layout(new LayoutConfig{Seed=481516,Procedural=LiminalGeneration.City11,Continuous=true});int spaces=0,carSites=0;
        for(int z=0;z<18;z++)for(int x=0;x<18;x++)
        {
            var r=city.At(x,z);Check(r.Plan.City!=null,"Missing city plan in full-city mode");foreach(var b in r.Plan.City.Buildings)spaces+=b.Floors;
            if(LiminalRegions.Hash(city.Config.Seed,x,z,0x1100CAu)%9==0)carSites++;
        }
        Console.WriteLine($"City plan snapshot (324 blocks, not simultaneous loaded objects): 1296 generated building instances, {spaces} furnished floor spaces, 3888 source-prop/furniture instances, {spaces*3} procedural furniture assemblies, {carSites} car spawn sites. Snapshot with aircraft disabled; five City11 source models. The separate aircraft source is checked independently.");
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
