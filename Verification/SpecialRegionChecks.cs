using System;
using System.Linq;
using System.Collections.Generic;
using Poolrooms.Core;

static partial class Checks
{
    static void SpecialRegions()
    {
        int samples=0,activities=0,vertices=0;var identities=new HashSet<string>();var names=new HashSet<string>();
        Check((int)LiminalEnvironment.MineralSanctuary==27&&(int)SurfaceTheme.EncounterFurniture==44,"Old serialized IDs changed");
        Check(Enum.GetValues(typeof(LiminalEnvironment)).Length==38,"Special environments missing from catalogue");
        foreach(int seed in new[]{0,31,-94,481516})
        {
            var config=new LayoutConfig{Seed=seed};WalkableExploration.Configure(config);
            for(int bx=-2;bx<=2;bx++)for(int bz=-2;bz<=2;bz++)
            {
                var coverage=new HashSet<LiminalEnvironment>();
                for(int x=0;x<16;x++)for(int z=0;z<8;z++)
                {
                    int rx=bx*8+x,rz=bz*8+z;WalkableExploration.Region(config,rx,rz,out var next);
                    coverage.Add(next);config.EnableSpecialRegions=false;
                    bool assigned=WalkableExploration.Region(config,rx,rz,out var old);config.EnableSpecialRegions=true;
                    if(assigned)Check(next==old,"New regions replaced a protected original discovery slot");
                }
                for(int i=0;i<10;i++)Check(coverage.Contains((LiminalEnvironment)(28+i)),"Two adjacent region blocks miss a special biome");
            }
            foreach(int x in new[]{int.MinValue,-1,0,int.MaxValue})foreach(int z in new[]{int.MinValue,-1,0,int.MaxValue})
            {Check(WalkableExploration.Region(config,x,z,out var e)&&Enum.IsDefined(typeof(LiminalEnvironment),e),"Extreme special coverage overflow");}
        }
        for(int i=0;i<10;i++)
        {
            var e=(LiminalEnvironment)(28+i);var mode=(LiminalGeneration)((int)LiminalGeneration.SolarObservatory+i);
            Check(names.Add(SpecialRegionPlanner.Name(e))&&SpecialRegionPlanner.ForMode(mode)==e,"Special identity mapping repeats");
            Check(BiomeSurfaces.ForEnvironment(e)==SpecialRegionPlanner.Theme(e),"Special palette mapping missing");
            Check(RoomMemories.At(0,0,0,e).Title==SpecialRegionPlanner.Name(e),"Room history does not match the place");
            Check(LiminalRegions.At(new LayoutConfig{Procedural=mode},0,0)==e,"Dedicated special generation mode unavailable");
            Check(LiminalRegions.Find(new LayoutConfig{Procedural=mode},e,0,0,out _,out _),"Dedicated search confused a special region with a disabled quiet biome");
            var disabled=new LayoutConfig{Procedural=LiminalGeneration.Expanded};Check(!LiminalRegions.Find(disabled,e,0,0,out _,out _),"Disabled special leaks into authored/custom worlds");
            foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})foreach(int recipe in new[]{0,1,2,3,131071})
            {
                var c=new LayoutConfig{CellSize=size,DoorWidth=door,DoorHeight=door*.5f+2.1f,CurveSegments=12,EncounterExploration=true,ObjectAbundance=3};
                var r=new Room{Environment=e,Exits=15,X=17,Z=-23};RestfulLevelPlanner.DescribeRecipe(r,c,recipe);var g=Architecture.Build(r,c);
                Check(r.Plan.Restful.Valid(r,c),"Special plan is not valid");Validate(g.Structure);Validate(g.Decoration);Validate(g.Glass);Validate(g.Water);
                foreach(var visual in g.VisualMeshes)Validate(visual);
                Check(g.Structure.Positions.Count<50000&&g.Decoration.Positions.Count<100000,"Unbounded special geometry");
                for(int axis=0;axis<4;axis++)for(float t=0;t<size*.5f;t+=.8f)
                {
                    var p=new V3(Directions.DX[axis]*t,1.8f,Directions.DZ[axis]*t);
                    Check(Math.Abs(Support(g.Structure,p))<.002f,"Special central route has no floor");
                    Check(SphereClear(g.Structure,new V3(p.X,.9f,p.Z),.34f),"Special central route obstructed");
                }
                foreach(var prop in r.Plan.Restful.Props)
                {
                    if(prop.CatalogId>=0)Check(prop.CatalogId<MallItemCatalog.Count,"New item ID out of bounds");
                    else Check(SourceFurniture.Index(prop.Source)>=0,"New source has no licensed model");
                    Check(Math.Abs(Support(g.Structure,prop.Bottom)-prop.Bottom.Y+.006f)<.002f,"Special furniture floats or intersects its support");
                }
                var life=RoomLifePlanner.Build(r,g,c);activities+=life.Stations.Count;
                if(size==36&&door==3&&recipe==0)Check(identities.Add(Hash(g.Structure,g.Decoration)),"Special programmes share identical architecture");
                if(e==LiminalEnvironment.IndoorMarina)Check(g.Regions.Count==4&&g.Water.TriangleCount>0,"Indoor marina lacks cut-out water basins");
                if(e==LiminalEnvironment.MirrorArcade)Check(g.VisualMeshes.Any(m=>m.Theme==SurfaceTheme.SpecialMirror),"Mirror gallery has no reflective surfaces");
                samples++;vertices+=g.Structure.Positions.Count+g.Decoration.Positions.Count;
            }
        }
        Check(identities.Count==10&&activities>0,"Missing distinct special buildings or activities");
        Console.WriteLine($"PASS: ten special programmes, {samples} built room cases at three scales/two door widths, {activities} reachable activity placements, {vertices} sampled vertices; original discovery slots preserved. Core only; Unity/PhysX/visuals/FPS unrun.");
    }
}
