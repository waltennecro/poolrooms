using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void RestfulMeshCatalogue()
    {
        const int perLevel=20001;var unique=new HashSet<string>();long triangles=0;
        var c=new LayoutConfig{CellSize=36,CurveSegments=12,ObjectAbundance=3};
        for(int level=0;level<10;level++)
        {
            var e=(LiminalEnvironment)(18+level);
            for(int index=0;index<perLevel;index++)
            {
                // Odd multiplier is a permutation of the 2^17 catalogue. Its large
                // stride exercises combined digits instead of only a low-ID prefix.
                int recipe=(int)(((long)index*65539+level*73)&131071);
                var r=new Room{X=17,Z=-23,Environment=e,Exits=15};RestfulLevelPlanner.DescribeRecipe(r,c,recipe);
                var g=RestfulLevelArchitecture.Build(r,c);
                Check(unique.Add(Hash(g.Structure,g.Decoration,g.Glass,g.Water)),"Repeated built architecture in new catalogue: "+e+" / "+recipe);
                triangles+=g.Structure.TriangleCount+g.Decoration.TriangleCount+g.Glass.TriangleCount+g.Water.TriangleCount;
            }
            Console.WriteLine($"Mesh catalogue {e}: {perLevel} distinct built architectures, same coordinate/scale, excluding props, colour and lighting; {unique.Count} cumulative.");
        }
        Check(unique.Count>200000,"New zone target not covered by distinct generated mesh fingerprints");
        Console.WriteLine($"PASS: {unique.Count} distinct new built room meshes across ten structural programmes; {triangles} generated triangles traversed for fingerprints. Not handcrafted rooms, source models, 100000 families per level or simultaneous instances. Native Unity unrun.");
    }
    static void RestfulExpansion(bool exhaustive)
    {
        var migrated=new GamePreferences{spatialAudioVersion=0,spatialAmbience=false,soundOcclusion=false};migrated.Sanitize();
        Check(migrated.spatialAudioVersion==1&&migrated.spatialAmbience&&migrated.soundOcclusion,"Old preferences omit spatial audio upgrade");
        migrated.spatialAmbience=migrated.soundOcclusion=false;migrated.Sanitize();Check(!migrated.spatialAmbience&&!migrated.soundOcclusion,"Explicit audio choice lost after migration");
        Check((int)LiminalEnvironment.RainHouses==17&&(int)LiminalGeneration.RainHouses==21&&(int)SurfaceTheme.RainInterior==33,"Existing serialized IDs moved");
        Check(RestfulLevelPlan.RecipeCount==4*8*8*8*8*8,"Recipe decoding mismatch");
        int plans=0,meshes=0,props=0,licensed=0;var names=new HashSet<string>();var sourceNames=new HashSet<string>();
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        for(int level=0;level<10;level++)
        {
            var e=(LiminalEnvironment)(18+level);var mode=(LiminalGeneration)(22+level);
            Check(RestfulLevelPlanner.ForMode(mode)==e&&names.Add(InteriorPlanner.Name(e)),"New level mapping or name is not unique");
            var c=new LayoutConfig{Procedural=mode,Continuous=true,AutonomousGeneration=true,ObjectAbundance=3,IndependentBiomeMaterials=true,CurveSegments=12};
            var layout=new Layout(c);var generated=layout.At(-21,19);Check(generated.Environment==e&&generated.Plan.Restful!=null,"Dedicated generation missed new level");
            Check(!new LayoutConfig().EnableRestfulLevels,"Legacy profiles must opt in to new environment field");
            // Prove the selector visits the complete bank rather than hashing into an
            // enlarged number space with unreachable recipes. Negative tile included.
            if(exhaustive)
            {
                var seen=new bool[RestfulLevelPlan.RecipeCount];
                for(int x=-512;x<0;x++)for(int z=-256;z<0;z++)
                {int recipe=RestfulLevelPlanner.RecipeAt(c,e,x,z);Check(!seen[recipe],"Recipe bank repeats before covering catalogue");seen[recipe]=true;}
                Check(seen.All(v=>v),"Unreachable architectural recipe");
            }
            int stride=exhaustive?1:257;
            for(int recipe=0;recipe<RestfulLevelPlan.RecipeCount;recipe+=stride)
            {
                var r=new Room{Environment=e,Exits=15};RestfulLevelPlanner.DescribeRecipe(r,c,recipe);var p=r.Plan.Restful;
                Check(p.Valid(r,c)&&r.Plan.Valid(r,c),"Invalid new level plan: "+e+" / "+recipe);
                Check(recipe==p.Layout+4*p.Span+32*p.Reach+256*p.Elevation+2048*p.Roof+16384*p.Rhythm,"Recipe tuple aliases another recipe");
                Check(p.Props.Length==60,"+200% tabletop slot target changed");plans++;
            }
            // Check each geometry axis independently at all supported extreme cell sizes.
            foreach(int size in new[]{28,36,60})foreach(int axis in new[]{1,4,32,256,2048,16384})
            {
                c.CellSize=size;c.DoorWidth=size==28?6:4.4f;c.DoorHeight=c.DoorWidth*.5f+2.1f;c.Validate();
                var hashes=new HashSet<string>();
                for(int digit=0;digit<(axis==1?4:8);digit++)
                {
                    var r=new Room{Environment=e,Exits=15};RestfulLevelPlanner.DescribeRecipe(r,c,axis*digit);var p=r.Plan.Restful;var g=Architecture.Build(r,c);
                    Validate(g.Structure);Validate(g.Decoration);Validate(g.Glass);Validate(g.Water);Validate(g.CollisionOnly);meshes++;
                    Check(hashes.Add(Hash(g.Structure,g.Decoration,g.Glass,g.Water)),"Structural parameter only changes a label: "+e+" / "+axis+" / "+digit);
                    Check(g.SourceProps.Count==0&&r.Plan.Assemblies.Length==0,"Generic packing overwrites curated level");
                    foreach(var prop in p.Props)
                    {
                        props++;
                        if(prop.CatalogId>=0)Check(MallItemCatalog.Get(prop.CatalogId).Size.Length<.5f,"Oversized tabletop item");
                        else
                        {
                            licensed++;
                            if(sourceNames.Add(prop.Source))Check(Directory.GetFiles(Path.Combine(root,"Assets/PoolroomsProcedural/Resources"),prop.Source+".json",SearchOption.AllDirectories).Length==1,"Missing licensed source");
                        }
                        float support=Support(g.Structure,prop.Bottom+V3.Up*.001f);
                        Check(Math.Abs(support-prop.Bottom.Y)<.008f,"Floating furnishing "+e+" / "+prop.Source+" at "+prop.Bottom.Y+" support "+support);
                        V3 extent=prop.CatalogId>=0?MallItemCatalog.Get(prop.CatalogId).Size*.5f:SourceFurniture.HalfExtents(SourceFurniture.Index(prop.Source),prop.Yaw);
                        foreach(int dx in new[]{-1,1})foreach(int dz in new[]{-1,1})
                        {
                            var foot=prop.Bottom+new V3(dx*extent.X,0,dz*extent.Z);
                            Check(Math.Abs(Support(g.Structure,foot+V3.Up*.001f)-prop.Bottom.Y)<.008f,"Furnishing footprint spans absent support: "+e+" / "+prop.Source);
                        }
                        if(prop.CatalogId<0)
                        {
                            var placement=new SourceFurniturePlacement(0,SourceFurniture.Index(prop.Source),prop.Bottom,prop.Yaw);
                            Check(SourceFurniture.Clear(g.Structure,placement,.003f)&&SourceFurniture.Clear(g.Decoration,placement,.003f),"Source furnishing intersects new level structure: "+e+" / "+prop.Source);
                        }
                    }
                    // All four exits have level supporting floor and full capsule clearance.
                    foreach(int side in new[]{0,1,2,3})for(float distance=0;distance<=size*.5f-.25f;distance+=.5f)
                    {
                        var q=new V3(Directions.DX[side]*distance,0,Directions.DZ[side]*distance);
                        Check(Math.Abs(Support(g.Structure,q+V3.Up*1.5f))<.02f,"New environment has an unsupported walking cross");
                        Check(SphereClear(g.Structure,q+V3.Up*.9f,.33f)&&SphereClear(g.Structure,q+V3.Up*1.6f,.23f),"New environment blocks walking cross");
                    }
                }
            }
            c.CellSize=36;c.DoorWidth=4.4f;c.DoorHeight=4.8f;
            var thin=new Room{Environment=e,Exits=15};c.ObjectAbundance=1;RestfulLevelPlanner.DescribeRecipe(thin,c,127);
            var dense=new Room{Environment=e,Exits=15};c.ObjectAbundance=3;RestfulLevelPlanner.DescribeRecipe(dense,c,127);
            Check(dense.Plan.Restful.Props.Count(p=>p.CatalogId>=0)==3*thin.Plan.Restful.Props.Count(p=>p.CatalogId>=0),"Object appearance increase is not +200%");
            Check(Hash(Architecture.Build(thin,new LayoutConfig{ObjectAbundance=1}).Structure)==Hash(Architecture.Build(dense,new LayoutConfig{ObjectAbundance=3}).Structure),"Object abundance changes architecture");
        }
        Console.WriteLine($"PASS: ten new structural programmes; 131072 recipe tuples per level (1310720 total capacity); {plans} planned recipes checked; {meshes} full geometry/axis/scale samples; {props} supported prop placements including {licensed} licensed-source instances using {sourceNames.Count} distinct existing source models. These are not 100000 structural families or 20000 distinct downloaded models. Native Unity unrun. {assertions} core assertions.");
    }
    static void RestfulWorldSnapshot()
    {
        var c=new LayoutConfig{Seed=481516,Zones=ZoneMode.Random,Grammar=ProceduralGrammar.SolarAtlas,AutonomousGeneration=true};WalkableExploration.Configure(c);
        var layout=new Layout(c);int rooms=0,interactions=0,licensed=0;var kinds=new HashSet<string>();var levels=new int[10];var keys=new HashSet<string>();
        for(int x=-64;x<=64;x++)for(int z=-64;z<=64;z++)
        {
            if(ConnectedTerrainTopology.Reserved(c,x,z)||!RestfulLevelPlanner.IsLevel(LiminalRegions.At(c,x,z)))continue;
            var r=layout.DescribeAt(x,z);var p=r.Plan?.Restful;if(p==null)continue;rooms++;levels[(int)r.Environment-18]++;
            for(int slot=0;slot<p.Props.Length;slot++)
            {
                var item=p.Props[slot];Check(keys.Add(x+":"+z+":"+slot),"Duplicated live-world pickup identity");interactions++;
                if(item.CatalogId<0){licensed++;kinds.Add(item.Source);}
                else Check(MallItemCatalog.Get(item.CatalogId)!=null,"Live-world pickup catalogue is unresolved");
            }
        }
        Check(levels.All(v=>v>0),"New level missing from actual unified world snapshot");
        Check(licensed>20000&&interactions>4000,"Object targets absent from the actual coordinate snapshot");
        Console.WriteLine($"PASS: actual unified-world coordinate snapshot seed {c.Seed}, [-64,64]^2 excluding reserved terrain: {rooms} new-level rooms, {interactions} distinct pickup keys, {licensed} planned licensed-source instances using {kinds.Count} distinct source models. All ten new levels present; streaming loads a bounded local subset. Not simultaneous objects, source-model counts or a native runtime snapshot.");
    }
}
