using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void VarietyExpansion()
    {
        Check(ZoneCatalog.Count*2==ZoneCatalog.BaseCount*3&&SolarCatalog.Count*2==SolarCatalog.BaseCount*3,"Poolroom catalogues must grow by exactly 50 percent");
        Check(OrangeriePlan.CatalogCount==3750&&ThermalBathPlan.CatalogCount==3750&&RainHousePlan.CatalogCount==3750&&AquaticParkPlan.CatalogCount==8400,"Incomplete regional expansion");
        Check(TerrainHouseBlueprint.Count==1200&&TerrainHouseBlueprint.FamilyCount==30,"Incomplete house expansion");
        foreach(var bank in new[]{(75,50),(120,70)})foreach(int sign in new[]{-1,0,1})
        {
            var ids=new HashSet<int>();
            for(int x=sign*bank.Item1;x<(sign+1)*bank.Item1;x++)for(int z=sign*bank.Item2;z<(sign+1)*bank.Item2;z++)
                ids.Add(BiomeVariety.RecipeAt(48,x,z,bank.Item1,bank.Item2,0x310190u));
            Check(ids.Count==bank.Item1*bank.Item2,"Incomplete new coordinate bank");
            foreach(int x in new[]{int.MinValue,-1,0,int.MaxValue})foreach(int z in new[]{int.MinValue,-1,0,int.MaxValue})
            {int id=BiomeVariety.RecipeAt(48,x,z,bank.Item1,bank.Item2,0x310190u);Check(id>=0&&id<ids.Count,"Extreme recipe address overflow");}
        }
        // Topology assigns these flags after procedural planning. A room's previous
        // ceiling program must not leak into its dedicated regional gateway geometry.
        foreach(bool mall in new[]{false,true})
        {
            var gateway=new Room{Width=22,Depth=22,Height=6,ZoneId=-1,ExpansionProgram=2,MallGateway=mall,TerrainGateway=!mall};
            var geometry=new RoomGeometry();RoomPrograms.Append(geometry,gateway,new LayoutConfig{ExpandedVariety=true});
            Check(geometry.Structure.TriangleCount==0,"Regional gateway inherited another biome's ceiling program");
        }
        int plans=0,meshes=0;
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})
        {
            var c=new LayoutConfig{CellSize=size,DoorWidth=door,DoorHeight=door*.5f+2.1f,Continuous=true,CurveSegments=12,ExpandedVariety=true};
            for(int family=0;family<4;family++)
            {
                int count=family==0?AquaticParkPlan.CatalogCount:3750,prior=family==0?AquaticParkPlan.BaseCatalogCount:2500;
                void Describe(Room r,int id)
                {if(family==0)AquaticParkPlanner.DescribeRecipe(r,c,id);else if(family==1)OrangeriePlanner.DescribeRecipe(r,c,id);else if(family==2)ThermalBathPlanner.DescribeRecipe(r,c,id);else RainHousePlanner.DescribeRecipe(r,c,id);RoomPrograms.Describe(r,c);}
                for(int id=prior;id<count;id++)
                {
                    var room=new Room{Exits=15};Describe(room,id);Check(room.Plan.Valid(room,c),"Invalid expanded regional plan "+family+":"+id+":"+size+":"+door);plans++;
                    if(id%125!=0&&id!=count-1)continue;
                    var g=Architecture.Build(room,c);Validate(g.Structure);Validate(g.CollisionOnly);Validate(g.Decoration);Validate(g.Glass);foreach(var visual in g.VisualMeshes)Validate(visual);meshes++;
                    var old=new Room{Exits=15};Describe(old,id%prior);var before=Architecture.Build(old,c);
                    Check(Hash(g.Structure,g.CollisionOnly)!=Hash(before.Structure,before.CollisionOnly),"New ID has no physical geometry change");
                    Check(g.Fixtures.Count==before.Fixtures.Count,"Expansion increased per-room light count");
                }
            }
            for(int id=SolarCatalog.BaseCount;id<SolarCatalog.Count;id++)
            {
                var room=new Room{Exits=15};SolarPlanner.Describe(room,c,id);Check(room.Plan.Valid(room,c)&&!room.Plan.IsFallback,"New solar recipe is invalid or empty");plans++;
                if(id%32!=0)continue;RoomPrograms.Describe(room,c);var g=Architecture.Build(room,c);Validate(g.Structure);Validate(g.CollisionOnly);meshes++;
            }
        }
        // Verify actual exploration selectors, not only forced-ID preview methods.
        foreach(var mode in new[]{LiminalGeneration.Poolrooms,LiminalGeneration.AquaticPark,LiminalGeneration.Orangerie,LiminalGeneration.ThermalBaths,LiminalGeneration.RainHouses})
        {
            var c=new LayoutConfig{ExpandedVariety=true,Continuous=true,Procedural=mode,Grammar=ProceduralGrammar.SolarAtlas,AuthoredRoomChance=0};
            var layout=new Layout(c);bool reached=false;
            for(int i=0;i<160;i++)
            {
                var room=layout.At(i*7-600,i*11-700);var p=room.Plan;
                Check(p!=null,"Missing actual procedural plan: "+mode);
                int id=p.Solar?.Recipe??p.Aquatic?.Recipe??p.Garden?.Recipe??p.Baths?.Recipe??p.Rain?.Recipe?? -1;
                int prior=mode==LiminalGeneration.Poolrooms?SolarCatalog.BaseCount:mode==LiminalGeneration.AquaticPark?AquaticParkPlan.BaseCatalogCount:2500;
                Check(id>=0&&p.Valid(room,c),"Actual expanded selector failed: "+mode);reached|=id>=prior;
            }
            Check(reached,"New catalogue IDs are only available in previews: "+mode);
        }
        var randomZones=new LayoutConfig{ExpandedVariety=true,Zones=ZoneMode.Random};
        for(int family=-1;family<ZoneCatalog.FamilyCount;family++)
        {
            randomZones.ZoneFamilyFilter=family;var ids=new HashSet<int>();
            for(int i=0;i<10000;i++)
            {int id=ZoneCatalog.RandomAt(randomZones,i-5000,i*3);ids.Add(id);Check(family<0||(int)ZoneCatalog.Get(id).Family==family,"New ID escaped the selected authored family");}
            Check(ids.Count==(family<0?ZoneCatalog.Count:ZoneCatalog.FamilySize(family)+(family<25?3:2)),"Authored recipe never selected during exploration");
        }
        var footprints=new HashSet<string>();
        for(int id=0;id<TerrainHouseBlueprint.Count;id++)
        {
            var b=TerrainHouseBlueprint.Get(id);var seen=new HashSet<int>{1};var pending=new Queue<int>();pending.Enqueue(1);
            while(pending.Count>0)
            {
                int cell=pending.Dequeue(),x=cell%b.Columns,z=cell/b.Columns;
                for(int side=0;side<4;side++){int nx=x+Directions.DX[side],nz=z+Directions.DZ[side],key=nz*b.Columns+nx;if(b.Has(nx,nz)&&seen.Add(key))pending.Enqueue(key);}
            }
            Check(b.Has(1,0)&&seen.Count==b.RoomCount,"Disconnected house footprint "+id);
            if(id%40==0)
            {
                string shape=b.Columns+":"+b.Rows+":"+string.Concat(Enumerable.Range(0,b.Rows).SelectMany(z=>Enumerable.Range(0,b.Columns).Select(x=>b.Has(x,z)?'1':'0')));
                Check(footprints.Add(shape),"House family repeats an existing footprint");
            }
        }
        var newHouses=new HashSet<int>();
        for(int x=-35;x<35;x++)for(int z=-7;z<7;z++)
        {
            var b=TerrainHouseBlueprint.Generate(94,x,z,true);newHouses.Add(b.Family);
            Check(TerrainHouseBlueprint.Select(94,x,z)<800,"Legacy house selector changed its domain");
            if(b.Family<20||x%7!=0)continue;
            var h=new TerrainHousePlan{Seed=94,CellX=x,CellZ=z};h.GenerateArchitecture(true);Check(h.Blueprint.Generated&&h.Blueprint.Family==b.Family,"House expansion missed generated path");
            var model=TerrainHouseArchitecture.Build(h,true);Validate(model.Geometry.Structure);Validate(model.Geometry.CollisionOnly);meshes++;
        }
        Check(newHouses.Count==30,"Not all new house families are reachable");
        var envelopes=new HashSet<string>();
        var mallConfig=new LayoutConfig{ExpandedVariety=true};
        for(int seed=0;seed<3500;seed++)
        {
            mallConfig.Seed=seed;var site=new ConnectedMallSite(mallConfig,0,0);var p=MallPlan.Create(seed,0,0,true,true,true,true);
            Check(p.Valid()&&p.Depth==site.Depth&&p.Width==site.Width&&p.Floors==site.Floors,"Mall plan disagrees with world reservation");
            Check(site.RadiusZ*mallConfig.CellSize>=p.Depth*.5f,"New mall exceeds reserved depth");
            envelopes.Add(p.Floors+":"+p.Bays+":"+p.Depth);
        }
        Check(envelopes.Count==75,"Not all 75 mall envelopes can generate");
        var authored=new LayoutConfig{ExpandedVariety=true,Procedural=LiminalGeneration.Authored,Zones=ZoneMode.Single,Columns=1,Rows=1};
        for(int id=ZoneCatalog.BaseCount;id<ZoneCatalog.Count;id++)
        {
            authored.SingleZone=id;var layout=new Layout(authored);var room=layout.At(0,0);var g=Architecture.Build(room,layout);Validate(g.Structure);Validate(g.CollisionOnly);meshes++;
            Check(room.ZoneId==id&&ZoneCatalog.Get(id).ExpansionStyle>0,"Authored expansion lost its stable ID or construction");
            var baseline=ZoneCatalog.Get(ZoneCatalog.Get(id).BaseIndex);Check(ZoneLighting.DefaultFor(id)==ZoneLighting.DefaultFor(baseline.Index),"New recipe acquired unrelated family lighting");
        }
        var modes=new[]{LiminalGeneration.SoftPlay,LiminalGeneration.Supermarkets,LiminalGeneration.IndoorPlay,LiminalGeneration.Offices,LiminalGeneration.Hotels,LiminalGeneration.WaitingHalls,LiminalGeneration.DiningHalls,LiminalGeneration.Archive906,LiminalGeneration.Courtyard188,LiminalGeneration.Coast48,LiminalGeneration.Conservatory,LiminalGeneration.TransitGallery,LiminalGeneration.City11};
        foreach(var mode in modes)
        {
            var c=new LayoutConfig{ExpandedVariety=true,Continuous=true,Procedural=mode,AutonomousGeneration=true,CurveSegments=12};var seen=new HashSet<int>();
            for(int i=0;i<100;i++)
            {
                var layout=new Layout(c);var room=layout.At(i-40,i*3-100);Check(room.Plan!=null&&room.Plan.Valid(room,c),"Expanded mode invalid: "+mode);plans++;
                int style=room.Plan.Interior?.Variant??room.Plan.SoftPlay?.RoofStyle??room.Plan.Quiet?.Variant??(room.Plan.Store!=null?(int)room.Plan.Store.Variant:-1);
                if(room.Plan.City!=null)foreach(var b in room.Plan.City.Buildings)seen.Add(CityStreetDetails.Typology(b));
                else if(mode==LiminalGeneration.IndoorPlay)foreach(var m in room.Plan.Modules)seen.Add((int)m.Kind);
                else seen.Add(style);
                if(i%20!=0)continue;var g=Architecture.Build(room,layout);Validate(g.Structure);Validate(g.Decoration);Validate(g.CollisionOnly);meshes++;
            }
            int expected=mode==LiminalGeneration.City11||mode==LiminalGeneration.IndoorPlay?9:mode==LiminalGeneration.Archive906||mode==LiminalGeneration.Courtyard188||mode==LiminalGeneration.Coast48?9:mode==LiminalGeneration.Conservatory||mode==LiminalGeneration.TransitGallery?12:5;
            Check(seen.Count==expected,"Expanded programs are unreachable for "+mode+": "+seen.Count);
        }
        AquaticRideClearance(new[]{4,5});
        IntegrationSourceContracts();
        Console.WriteLine($"PASS: variety expansion, {plans} plans, {meshes} mesh samples, 30 connected house footprints, 75 mall envelopes; {assertions} core assertions. Unity native validation unrun.");
    }
}
