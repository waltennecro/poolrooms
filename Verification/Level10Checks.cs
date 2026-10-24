using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Poolrooms.Core;

static partial class Checks
{
    static void Level10()
    {
        long before=assertions;int sectors=0,stalks=0,lofts=0,lakes=0,placed=0;
        var programmes=new HashSet<Level10Place>();var roofs=new HashSet<int>();var signatures=new HashSet<string>();var sources=new HashSet<int>();int[] maxTriangles=new int[3];
        foreach(int size in new[]{28,36,60})foreach(int seed in new[]{10,1010,-44})
        {
            var config=new LayoutConfig{Continuous=true,Seed=seed,CellSize=size,Procedural=LiminalGeneration.WheatFields10,EncounterExploration=true};var world=new Layout(config);
            for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)
            {
                var r=world.At(x,z);var p=r.Plan?.Level10;Check(p!=null&&r.Plan.Valid(r,config),"Level10 fell back to a generic room");
                Check(r.Width==size&&r.Depth==size,"Countryside is boxed inside a room");
                var g=Architecture.Build(r,world);sectors++;stalks+=p.Stalks.Length;programmes.Add(p.Place);roofs.Add(p.RoofStyle);signatures.Add(Hash(g.Structure,g.CollisionOnly));
                foreach(var mesh in new[]{g.Structure,g.Decoration,g.Water,g.CollisionOnly,g.Glass}.Concat(g.VisualMeshes))Validate(mesh);
                Check(g.Fixtures.Count==0,"Country day must not depend on artificial fixture lights");
                Check(g.Structure.TriangleCount<=32768&&g.CollisionOnly.TriangleCount<15000,"Country physics budget exceeded");
                Check(p.Trees.Length>=1&&p.Trees.Length<=2&&g.Forest39Canopy!=null,"Rural windbreak trees missing or unbounded");
                Check(p.Stalks.Length>=100&&p.Stalks.Length<=Level10Plan.MaximumStalks,"Missing or unbounded wheat");
                var allCollision=new MeshData();allCollision.Append(g.Structure);allCollision.Append(g.CollisionOnly);
                foreach(var route in p.Routes)
                {
                    for(int i=0;i<route.Points.Length;i++)
                    {
                        var point=route.Points[i];float floor=Level10Terrain.MeshHeight(p,point.X,point.Y);
                        Check(Math.Abs(floor)<.025f,"Country track lacks level support");
                        Check(SphereClear(allCollision,new V3(point.X,floor+.95f,point.Y),.30f),"Country track or building approach obstructed: "+p.Place+" "+point.X+","+point.Y);
                    }
                }
                Check(g.Level10Crops!=null&&g.Level10Crops.Length==16,"Wheat lost its bounded spatial LOD batches");
                int[] triangles=new int[3];
                foreach(var patch in g.Level10Crops)for(int lod=0;lod<3;lod++)
                {Validate(patch.Meshes[lod]);triangles[lod]+=patch.Meshes[lod].TriangleCount;}
                Check(triangles[0]>triangles[1]&&triangles[1]>triangles[2]&&triangles[0]<260000,"Wheat LOD complexity is not bounded/reduced");
                for(int i=0;i<3;i++)maxTriangles[i]=Math.Max(maxTriangles[i],triangles[i]);
                foreach(var plant in p.Stalks)
                {
                    Check(p.CropClear(plant.Foot.X,plant.Foot.Z),"Wheat occupies a protected route/farm/lake");
                    Check(Math.Abs(Level10Terrain.MeshHeight(p,plant.Foot.X,plant.Foot.Z)-plant.Foot.Y)<.0001f,"Floating wheat root");
                }
                foreach(var prop in g.SourceProps)
                {
                    placed++;sources.Add(prop.Source);
                    float y=prop.Source==11?(prop.Slot==100021?p.LoftY:0)+SourceFurniture.Dimensions(7).Y+.003f:
                        Support(allCollision,prop.Bottom+V3.Up*.015f);
                    Check(Math.Abs(y-prop.Bottom.Y)<.009f,"Unsupported country source model: "+prop.Slot+" "+p.Place+" y="+y+" bottom="+prop.Bottom.Y);
                    Check(SourceFurniture.Clear(allCollision,prop,.001f),"Country source clips architecture: "+prop.Slot+" "+p.Place);
                }
                Check(g.SourceProps.Count>=3,"Country furnishing source hooks are missing");
                if(p.Loft){Level10Stairs(allCollision,p);lofts++;}
                if(p.Lake.Width>0)
                {
                    lakes++;Check(g.Water.TriangleCount>12&&g.Regions.Count==1,"Missing native water footprint");
                    foreach(var point in g.Water.Positions)
                        Check(Level10Terrain.MeshHeight(p,point.X,point.Z)<=p.Lake.Level+.0002f,"Water lies over dry ground");
                    Check(p.Lake.Level-p.Lake.Bottom<1&&p.Lake.Radius<=3.6f,"Lake lost its shallow accessible form");
                }
                foreach(var site in Level10Activities.Plan(p))
                {
                    Validate(Level10Equipment.Body(site.Kind));Validate(Level10Equipment.Moving(site.Kind));
                    Check(site.Slot>=0&&site.Slot<5,"Unstable country action address");
                }
                Check(g.Life!=null&&g.Life.Notice.HasValue&&g.Life.Scenery!=null&&SceneryPlanner.ValidPlan(g.Life.Scenery,r,config),"Country story or supported shared furnishing layer is missing");
                var a=g.Life.Notice.Value;Check(ArchitecturalDressing.FootprintFits(a.Center,a.Normal,a.A,a.B,a.C),"Country notice extends beyond support");
                for(int side=0;side<4;side++)
                {
                    var next=world.Neighbour(r,side);Check(r.HasExit(side)&&next.HasExit(Directions.Opposite(side)),"Nonreciprocal rural seam");
                    for(int sample=0;sample<=12;sample++)
                    {
                        float offset=-p.Half+p.Cell*sample/12;
                        float px=side%2==0?offset:Directions.DX[side]*p.Half,pz=side%2==0?Directions.DZ[side]*p.Half:offset;
                        float nx=px-Directions.DX[side]*p.Cell,nz=pz-Directions.DZ[side]*p.Cell;
                        Check(Math.Abs(Level10Terrain.MeshHeight(p,px,pz)-Level10Terrain.MeshHeight(next.Plan.Level10,nx,nz))<.0002f,"Rural ground seam cracked");
                    }
                    if(side<2)Check(Corridors.Build(r,next,side,config).Structure.TriangleCount==0,"Internal rural border acquired a corridor tube");
                }
            }
        }
        // Prove every named programme actually builds, rather than expecting a
        // small random snapshot to happen to contain every one of the twelve.
        for(int seed=0;seed<1024&&programmes.Count<12;seed++)
        {
            var c=new LayoutConfig{CellSize=36,Continuous=true,Seed=seed,Procedural=LiminalGeneration.WheatFields10,EncounterExploration=true};
            var w=new Layout(c);var r=w.At(0,0);if(programmes.Contains(r.Plan.Level10.Place))continue;
            var g=Architecture.Build(r,w);programmes.Add(r.Plan.Level10.Place);roofs.Add(r.Plan.Level10.RoofStyle);signatures.Add(Hash(g.Structure,g.CollisionOnly));
            Validate(g.Structure);Validate(g.CollisionOnly);sectors++;stalks+=r.Plan.Level10.Stalks.Length;placed+=g.SourceProps.Count;
            foreach(var prop in g.SourceProps)sources.Add(prop.Source);
            if(r.Plan.Level10.Loft)lofts++;if(r.Plan.Level10.Lake.Width>0)lakes++;
        }
        Check(programmes.Count==12&&roofs.Count==3&&signatures.Count>60,"Country variety incomplete: programmes="+programmes.Count+" roofs="+roofs.Count+" meshes="+signatures.Count);
        Check(lofts>0&&lakes>0,"Missing upstairs or water contexts");Level10Interactions();Level10Discovery();Level10AreaScaling();Level10Coordinates();Level10Contracts();
        var report=new{sectors,actualStalkClumps=stalks,programmes=programmes.Count,roofProfiles=roofs.Count,distinctPhysicalSectorMeshes=signatures.Count,lofts,lakes,existingFurnitureSourceModels=sources.Count,sourcePlacements=placed,newExternalModels=0,newNativeTextureSets=3,peakWheatTriangles=maxTriangles,checks=assertions-before,nativeUnityExecuted=false};
        string path=Environment.GetEnvironmentVariable("POOLROOMS_LEVEL10_REPORT");if(!string.IsNullOrEmpty(path))File.WriteAllText(path,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("Level10: "+JsonSerializer.Serialize(report));
    }
    static void Level10Stairs(MeshData physical,Level10Plan p)
    {
        float start=p.Farm.MinZ+.55f,run=Math.Min(4.8f,p.Farm.Depth-1.4f),x=p.Farm.MinX+.85f;
        for(int i=0;i<16;i++)
        {
            float z=start+(i+.5f)*run/16,y=p.LoftY*(i+1)/16;
            Check(run/16>=.28f&&p.LoftY/16<=.18001f,"Rural stairs exceed humanoid scale");
            Check(Math.Abs(Support(physical,new V3(x,y+.02f,z))-y)<.01f,"Missing rural stair tread");
            float ceiling=-CourtyardCeiling(physical,x,z,y+.04f);
            Check(float.IsPositiveInfinity(ceiling)||ceiling-y>=2.0f,"Rural loft blocks stair headroom");
            Check(SphereClear(physical,new V3(x,y+1.45f,z),.25f),"Rural stair hits a rail or roof");
        }
        float landing=start+run;
        for(int sample=0;sample<=12;sample++)
        {
            float z=landing+.03f+(p.Farm.MaxZ-.25f-landing-.03f)*sample/12;
            Check(Math.Abs(Support(physical,new V3(x,p.LoftY+.02f,z))-p.LoftY)<.01f,"A gap separates stair from back loft");
        }
    }
    static void Level10Interactions()
    {
        foreach(Level10Action kind in new[]{Level10Action.GrainMill,Level10Action.HandPump,Level10Action.WindVane,Level10Action.SeedCabinet})for(uint seed=0;seed<32;seed++)
        {
            var state=new Level10ActionState(kind,seed);int actions=0;
            while(!state.Complete&&actions<8){Check(state.Advance(),"Farm action failed to advance");actions++;}
            Check(state.Complete&&actions>=1&&actions<=4,"Unsolvable farm activity");Check(!state.Advance(),"Completed farm state must not overflow");
            var restored=new Level10ActionState(kind,seed);restored.Restore(state.Position,state.Complete);Check(restored.Complete,"Farm activity cannot restore after streaming");
        }
        var inventory=new ExplorationInventory();
        for(int id=0;id<3;id++)
        {
            var item=Level10Activities.Item(id);Check(item!=null&&ExplorationItems.Resolve(item.Key).Name==item.Name,"Missing rural item definition");
            Check(inventory.Add(item.Key,BigInteger.Pow(10,100)),"Rural item stack is bounded");Validate(Level10Equipment.Keepsake(id));
        }
        Check(Level10Activities.Item(-1)==null&&Level10Activities.Item(3)==null,"Invalid rural item address accepted");
        Validate(Level10Equipment.WaterFlow());Check(Level10Equipment.WaterFlow().Indices[(int)Surface.Slide].Count>0,"Pump has no actual transparent water geometry");
    }
    static void Level10Discovery()
    {
        Check((int)LiminalEnvironment.WheatFields10==64&&(int)LiminalEnvironment.EnchantedForest39==63,"Serialized biome IDs changed");
        Check(!new LayoutConfig().EnableWheatFields10,"Historical core must keep country opt-in");
        foreach(int seed in new[]{10,194,8321})foreach(bool large in new[]{false,true})
        {
            var c=new LayoutConfig{CellSize=36,Seed=seed,LargeProceduralRegions=large,LargeRegionScale=4};WalkableExploration.Configure(c);
            Check(c.EnableWheatFields10&&WalkableExploration.Enabled(c,LiminalEnvironment.WheatFields10),"Unified world excludes country");
            Check(LiminalRegions.Find(c,LiminalEnvironment.WheatFields10,0,0,out int x,out int z),"Country locator failed: seed="+seed+" large="+large);
            var world=new Layout(c);var room=world.At(x,z);Check(room.Plan?.Level10!=null,"Country locator found an unbuilt destination");
            bool boundary=false;int limit=(int)Math.Ceiling(BiomeExtent.RegionSpacing(c)*2)+5;
            for(int dx=-limit;dx<=limit&&!boundary;dx++)for(int dz=-3;dz<=3&&!boundary;dz++)
            {
                int xx=x+dx,zz=z+dz;if(!Level10Planner.Available(c,xx,zz))continue;
                for(int side=0;side<4;side++)
                {
                    int nx=xx+Directions.DX[side],nz=zz+Directions.DZ[side];
                    if(!Level10Planner.Available(c,xx,zz)||LiminalRegions.At(c,nx,nz)!=LiminalEnvironment.City11)continue;
                    var a=world.At(xx,zz);var b=world.At(nx,nz);if(a.Plan?.Level10==null||b.Environment!=LiminalEnvironment.City11)continue;
                    Check(a.HasExit(side)&&b.HasExit(Directions.Opposite(side)),"City road does not connect to countryside");boundary=true;break;
                }
            }
            Check(boundary,"No actual reciprocal walking boundary to Level11");
            for(int rz=0;rz<8;rz++)for(int rx=0;rx<8;rx++)
            {
                bool enabled=c.EnableWheatFields10;WalkableExploration.Region(c,rx+8,rz+8,out var first);c.EnableWheatFields10=false;
                WalkableExploration.Region(c,rx+8,rz+8,out var old);c.EnableWheatFields10=enabled;Check(first==old,"Country stole a previous discovery label");
            }
        }
    }
    sealed class CountryArea
    {public int Total,Fields;public bool Edge;}
    static Dictionary<long,CountryArea> CountryAreas(LayoutConfig c,int radius)
    {
        var result=new Dictionary<long,CountryArea>();
        for(int x=-radius;x<radius;x++)for(int z=-radius;z<radius;z++)
        {
            if(LiminalRegions.BaseAt(c,x,z)!=LiminalEnvironment.City11)continue;
            BiomeExtent.NearestRegion(c,x,z,out int rx,out int rz);long key=ContinuousTopology.CellKey(rx,rz);
            if(!result.TryGetValue(key,out var entry)){entry=new CountryArea();result.Add(key,entry);}
            entry.Total++;if(LiminalRegions.At(c,x,z)==LiminalEnvironment.WheatFields10)entry.Fields++;
            entry.Edge|=x==-radius||z==-radius||x==radius-1||z==radius-1;
        }
        return result;
    }
    static void Level10AreaScaling()
    {
        long totalBefore=0,totalAfter=0,fieldsBefore=0,fieldsAfter=0;
        foreach(int seed in new[]{10,194,8321})
        {
            var a=new LayoutConfig{Seed=seed,LargeProceduralRegions=true,LargeRegionScale=4};WalkableExploration.Configure(a);a.ExpandedBiomeAreas=false;
            var b=new LayoutConfig{Seed=seed,LargeProceduralRegions=true,LargeRegionScale=4};WalkableExploration.Configure(b);
            var before=CountryAreas(a,144);var after=CountryAreas(b,177);
            foreach(var pair in before)
            {
                if(pair.Value.Edge||!after.TryGetValue(pair.Key,out var next)||next.Edge)continue;
                totalBefore+=pair.Value.Total;totalAfter+=next.Total;fieldsBefore+=pair.Value.Fields;fieldsAfter+=next.Fields;
            }
        }
        double parent=totalAfter/(double)totalBefore,fields=fieldsAfter/(double)fieldsBefore;
        Check(totalBefore>1000&&parent>1.46&&parent<1.54,"Country changed the protected parent district +50% expansion");
        Check(fieldsBefore>300&&fields>1.35&&fields<1.65,"Country subdivision failed physical area growth: "+fields);
        Console.WriteLine("Level10 area: "+totalBefore+" -> "+totalAfter+" combined city/country cells ("+parent+"x), "+fieldsBefore+" -> "+fieldsAfter+" rural cells ("+fields+"x). Integer frontier quantisation is not another catalogue.");
    }
    static void Level10Coordinates()
    {
        var c=new LayoutConfig{CellSize=36,Procedural=LiminalGeneration.WheatFields10};
        foreach(int coordinate in new[]{-1234567,0,1234567,int.MinValue+2,int.MaxValue-2})
        {
            Level10Plan a=new Level10Plan(c,coordinate,coordinate),b=new Level10Plan(c,coordinate,coordinate);
            Check(a.Valid()&&b.Valid()&&a.Seed==b.Seed&&a.Stalks.Length==b.Stalks.Length,"Rural extreme-coordinate determinism failed");
        }
        foreach(int size in new[]{24,256})Check(new Level10Plan(new LayoutConfig{CellSize=size},0,0).Valid(),"Country scale boundary invalid");
    }
    static void Level10Contracts()
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));string Read(string path)=>File.ReadAllText(Path.Combine(root,"Assets/PoolroomsProcedural",path));
        string world=Read("Runtime/PoolroomsWorld.cs");Check(world.Contains("PoolroomsLevel10Crops.Attach")&&world.Contains("PoolroomsLevel10.Attach"),"Country generated resources are not attached natively");
        Check(Read("Runtime/MeshFactory.cs").Contains("PoolroomsLevel10Materials.Resolve"),"Native farm PBR binding missing");
        Check(Read("Runtime/PoolroomsInventory.cs").Contains("PoolroomsLevel10.CreateItem"),"Rural items cannot leave inventory");
        string wind=Read("Runtime/PoolroomsLevel10Crops.cs");Check(wind.Contains("updates>=2")&&wind.Contains(".125f")&&wind.Contains("DynamicGeometry"),"Wheat motion has no bounded visible-patch budget");
        string action=Read("Runtime/PoolroomsLevel10Activity.cs");Check(action.Contains("inventory.MarkCollected")&&action.Contains("operatorActor.IsPaused")&&!action.Contains("Teleport"),"Optional farm activities bypass safety/session ownership");
    }
}
