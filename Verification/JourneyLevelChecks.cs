using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void JourneyLevels()
    {
        Check((int)LiminalEnvironment.MirrorArcade==37&&(int)SurfaceTheme.MallHypermarket==71,"Expansion changed serialized pre-existing IDs");
        Check(Enum.GetValues(typeof(LiminalEnvironment)).Length==RestfulLevelPlanner.EnvironmentCount,"Twenty levels missing from catalogue");
        int cases=0,props=0,stations=0,pools=0;long vertices=0;
        var names=new HashSet<string>();var buildings=new HashSet<string>();
        foreach(int seed in new[]{0,31,-94,481516})
        {
            var c=new LayoutConfig{Seed=seed};WalkableExploration.Configure(c);
            for(int bx=-2;bx<=2;bx++)for(int bz=-2;bz<=2;bz++)
            {
                // The rural city edge is tested by actual 65-biome walking routes; this bank still owns exactly 64 protected labels.
                var counts=new int[WalkableExploration.DiscoveryEnvironmentCount];
                for(int dx=0;dx<8;dx++)for(int dz=0;dz<8;dz++)
                {Check(WalkableExploration.Region(c,bx*8+dx,bz*8+dz,out var e),"Unassigned expanded discovery cell");counts[(int)e]++;}
                Check(counts.All(n=>n>0),"Expanded block hides a biome behind master mode");
            }
            foreach(int x in new[]{int.MinValue,-1,0,int.MaxValue})foreach(int z in new[]{int.MinValue,-1,0,int.MaxValue})
                Check(WalkableExploration.Region(c,x,z,out var e)&&Enum.IsDefined(typeof(LiminalEnvironment),e),"Expanded discovery overflow");
        }
        for(int index=0;index<20;index++)
        {
            var e=(LiminalEnvironment)(38+index);var mode=(LiminalGeneration)((int)LiminalGeneration.AquariumGalleries+index);
            Check(names.Add(InteriorPlanner.Name(e))&&JourneyLevelPlanner.ForMode(mode)==e,"New identity/name mapping aliases another level");
            Check(RoomMemories.At(0,0,0,e).Title==JourneyLevelPlanner.Name(e),"Story belongs to another level");
            var disabled=new LayoutConfig{Procedural=LiminalGeneration.Expanded};Check(!LiminalRegions.Find(disabled,e,0,0,out _,out _),"Optional bank leaked into a legacy profile");
            Check(LiminalRegions.At(new LayoutConfig{Procedural=mode},0,0)==e,"Direct mode missed the level");
            Check(LiminalRegions.Find(new LayoutConfig{Procedural=mode},e,0,0,out _,out _),"Direct mode search rejected its own level");
            foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})foreach(int recipe in new[]{0,3,65535,131071})
            {
                var c=new LayoutConfig{CellSize=size,DoorWidth=door,DoorHeight=door*.5f+2.1f,CurveSegments=12,EncounterExploration=true,ObjectAbundance=3,ExpandedLevelDetails=true};
                var r=new Room{Environment=e,Exits=15,X=17,Z=-23};RestfulLevelPlanner.DescribeRecipe(r,c,recipe);
                var g=Architecture.Build(r,c);Check(r.Plan.Restful.Valid(r,c),"New plan invalid: "+e);
                Validate(g.Structure);Validate(g.Decoration);Validate(g.Glass);Validate(g.Water);
                foreach(var m in g.VisualMeshes)Validate(m);
                Check(g.Structure.Positions.Count<100000&&g.Decoration.Positions.Count<180000,"Unbounded room mesh: "+e);
                for(int side=0;side<4;side++)for(float t=0;t<size*.5f;t+=1.2f)
                {
                    var at=new V3(Directions.DX[side]*t,1.8f,Directions.DZ[side]*t);
                    Check(Math.Abs(Support(g.Structure,at))<.002f,"New level route lacks floor: "+e);
                    Check(SphereClear(g.Structure,new V3(at.X,.9f,at.Z),.34f),"New level blocks a central walking route: "+e);
                }
                foreach(var prop in r.Plan.Restful.Props)
                {
                    if(prop.CatalogId>=0)Check(MallItemCatalog.Get(prop.CatalogId)!=null,"New prop has an invalid item ID");
                    else Check(SourceFurniture.Index(prop.Source)>=0,"New prop has no attributed source binding");
                    Check(Math.Abs(Support(g.Structure,prop.Bottom)-prop.Bottom.Y+.006f)<.002f,"Unsupported new furniture: "+e+" "+prop.Source);
                    props++;
                }
                if(JourneyLevelPlanner.HasPools(e))Check(g.Regions.Count>=4&&g.Water.TriangleCount>0,"Water programme has no physical basin");
                foreach(var water in g.Regions)Check(water.Bottom<water.Level,"Inverted water volume");
                if(size==36&&door==3&&recipe==0)Check(buildings.Add(Hash(g.Structure,g.Decoration)),"Two new programmes only change their label/palette");
                stations+=g.Life?.Stations.Count??0;pools+=g.Regions.Count;vertices+=g.Structure.Positions.Count+g.Decoration.Positions.Count;cases++;
            }
        }
        // Existing twenty gallery programmes receive physical detail only when
        // explicitly upgraded; the original bank stays available for regression.
        for(int id=18;id<38;id++)
        {
            var c=new LayoutConfig{CellSize=36,CurveSegments=12};var r=new Room{Environment=(LiminalEnvironment)id,Exits=15};RestfulLevelPlanner.DescribeRecipe(r,c,31);
            var before=RestfulLevelArchitecture.Build(r,c);c.ExpandedLevelDetails=true;RestfulLevelPlanner.DescribeRecipe(r,c,31);var after=RestfulLevelArchitecture.Build(r,c);
            Check(after.Decoration.TriangleCount>before.Decoration.TriangleCount,"Existing gallery did not gain detail");
            Check(Hash(before.Structure)==Hash(after.Structure),"Detail expansion changed old floor support");
        }
        Check(buildings.Count==20&&stations>0,"Missing architecture or interactable stations");
        Console.WriteLine($"PASS: twenty Journey programmes; {cases} built rooms at three sizes/two door widths; {props} planned pickups; {stations} reachable stations; {pools} water volumes; {vertices} sampled vertices. {RestfulLevelPlanner.EnvironmentCount}-biome discovery and old-gallery opt-in detail checked. Core only; Unity/appearance/PhysX/FPS unrun.");
    }
}
