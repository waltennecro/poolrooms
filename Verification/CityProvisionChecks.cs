using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Poolrooms.Core;

static partial class Checks
{
    static void CityProvisions()
    {
        Check(ProvisionCatalog.FoodCount>100&&ProvisionCatalog.Count==125&&ProvisionCatalog.AlmondWater==124,"Stable food catalogue size");
        var source=new HashSet<string>();var names=new HashSet<string>();
        for(int id=0;id<ProvisionCatalog.Count;id++)
        {
            var d=ProvisionCatalog.Get(id);Check(d.Id==id&&source.Add(d.Source)&&names.Add(d.Name),"Duplicated source/name/ID: "+id);
            var item=ExplorationItems.Resolve(d.Key);Check(item!=null&&item.Key==d.Key&&item.Consumable&&item.Source==d.Source&&item.Use==(d.Drink?ItemUse.Drink:ItemUse.Eat),"Food use/key mismatch");
            Check(d.Size.X>0&&d.Size.Y>0&&d.Size.Z>0&&d.Longest<=.381f,"Food has wrong metric carry size");
        }
        Check(Enumerable.Range(0,117).Select(i=>ProvisionCatalog.Get(i).FoodType).Distinct().Count()==103,"Food count must exclude cut/presentation duplicates");
        Check(!Enumerable.Range(0,117).Any(i=>ProvisionCatalog.Get(i).Drink),"Drink counted as edible food");
        var inventory=new ExplorationInventory();BigInteger huge=BigInteger.Pow(10,100);
        Check(inventory.Add("food:0",huge)&&inventory.Add("food:0",huge)&&inventory.Count("food:0")==huge*2,"Food stacks overflow or split");
        Check(inventory.Remove("food:0",1)&&inventory.Count("food:0")==huge*2-1,"Eating must consume exactly one");
        for(int i=0;i<249;i++)Check(inventory.Add("catalog:"+i,1),"Fill inventory for atomicity");
        Check(inventory.Add("food:0",huge)&&!inventory.Add("food:1",1)&&inventory.Count("food:1")==0,"Full inventory must retain existing stacks and reject a new type atomically");
        Check(!inventory.Add("food:001",1)&&!inventory.Add("food:-1",1)&&!inventory.Add("food:125",1),"Noncanonical/invalid food key accepted");
        Check(ExplorationInventory.Capacity==250&&ExplorationInventory.Pages==5&&ExplorationItems.ToolCount==10,"Existing inventory layout changed");
        var found=new HashSet<int>();
        foreach(var biome in Enum.GetValues<LiminalEnvironment>())
        {
            for(int coordinate=-100;coordinate<100;coordinate++)
            {
                var a=ProvisionDistribution.Candidates(481516,coordinate,(int)biome,36,36,0,biome==LiminalEnvironment.City11);
                var b=ProvisionDistribution.Candidates(481516,coordinate,(int)biome,36,36,0,biome==LiminalEnvironment.City11);
                Check(a.Length==36&&b.Length==36,"Biome excluded from food distribution");
                var slots=new HashSet<int>();
                for(int i=0;i<a.Length;i++)
                {
                    Check(slots.Add(a[i].Slot)&&a[i].Slot==b[i].Slot&&(a[i].Bottom-b[i].Bottom).Length<.00001f,"Food candidate identity changed");
                    Check(Math.Abs(a[i].Bottom.X)<17&&Math.Abs(a[i].Bottom.Z)<17,"Food candidate outside bounds");
                }
                for(int i=0;i<4;i++)found.Add(ProvisionDistribution.Item(481516,coordinate,(int)biome,a[0].Slot,i));
                Check(ProvisionDistribution.Item(481516,coordinate,(int)biome,a[0].Slot,3)==ProvisionCatalog.AlmondWater,"Almond water missing from sharing stations");
            }
        }
        Check(found.Count==125,"Every food, drink and almond water must be reachable by distribution");
        Check(ProvisionDistribution.Candidates(1,0,0,float.NaN,36,0).Length==0&&ProvisionDistribution.Candidates(1,0,0,3,36,0).Length==0,"Hostile candidate dimensions accepted");
        var keys=new HashSet<string>();foreach(string domain in new[]{"room","mall-2-3","home-7"})for(int floor=0;floor<5;floor++)for(int slot=0;slot<36;slot++)for(int piece=0;piece<5;piece++)Check(keys.Add(ProvisionDistribution.Key(domain,7,0,floor,slot,piece)),"External floor/house pickup key collision");
        // Actual tabletop footprints cannot overlap each other or hang over the edge.
        for(int id=0;id<ProvisionCatalog.Count;id++)for(int piece=0;piece<3;piece++)
        {
            var p=ProvisionDistribution.OnTable(piece);var size=ProvisionCatalog.Get(id).Size;Check(Math.Abs(p.X)+size.X*.5f<.65f&&Math.Abs(p.Z)+size.Z*.5f<.352f,"Food overhangs source table");
            var almond=ProvisionCatalog.Get(ProvisionCatalog.AlmondWater).Size;var drink=ProvisionDistribution.OnTable(3);
            Check(Math.Abs(p.Z-drink.Z)>(size.Z+almond.Z)*.5f+.002f,"Almond carton overlaps food");
        }
        int samples=0,maxTriangles=0;var programs=new HashSet<int>();
        foreach(int seed in new[]{7,481516})foreach(float cell in new[]{28f,36f,60f})for(int x=-3;x<=3;x++)
        {
            var c=new LayoutConfig{Seed=seed,CellSize=cell,Procedural=LiminalGeneration.City11,DreamCity11=true,EnableImportedBuildings=true,ExpandedVariety=true,CurveSegments=16};
            var r=new Room{X=x,Z=2,Exits=15};City11Planner.Describe(r,c);var a=Architecture.Build(r,c);var b=Architecture.Build(r,c);
            Check(r.Plan.City.Valid(r,c)&&Hash(a.Structure,a.Decoration)==Hash(b.Structure,b.Decoration),"Dream city generation not deterministic");
            int pavilion=City11Dreamscape.Pavilion(r.Plan.City);Check(pavilion>=0,"No public pavilion");programs.Add(City11Dreamscape.Program(r.Plan.City.Buildings[pavilion]));
            Check(a.SourceSeats.Count>=2&&a.SourceSeats.Count<=5,"Dream city source budget");
            foreach(var mesh in new[]{a.Structure,a.Decoration,a.Glass,a.CollisionOnly}.Concat(a.VisualMeshes))Validate(mesh);
            Check(a.Fixtures.Count<=38,"City light budget regression");
            int triangles=a.Structure.TriangleCount+a.Decoration.TriangleCount+a.VisualMeshes.Sum(m=>m.TriangleCount);maxTriangles=Math.Max(maxTriangles,triangles);Check(triangles<55000,"Dream city triangle budget regression");
            // Public arches keep the middle of the pavilion open and grounded.
            var plot=r.Plan.City.Buildings[pavilion];var publicBody=new RoomGeometry();City11Dreamscape.BuildPavilion(publicBody,plot,c);
            var surfaces=new ScenerySurfaceIndex(publicBody,new SceneryWorkBudget());Check(surfaces.Valid,"Invalid public pavilion support mesh");
            for(int step=0;step<=8;step++)
            {
                var point=plot.Point(0,.12f,-plot.Depth*.42f+step*plot.Depth*.105f);
                Check(surfaces.Supported(point),"Pavilion walkway has no floor");
                Check(surfaces.Clear(new SceneryBox(point+new V3(-.3f,.02f,-.25f),point+new V3(.3f,1.9f,.25f)),0),"Pavilion arch blocks the central walking route");
            }
            samples++;
        }
        Check(programs.Count==3,"Missing architectural public programme");
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        string runtime=File.ReadAllText(Path.Combine(root,"Assets/PoolroomsProcedural/Runtime/PoolroomsProvisions.cs"));
        Check(runtime.Contains("Physics.RaycastNonAlloc")&&runtime.Contains("Physics.OverlapBoxNonAlloc")&&runtime.Contains("CreatedItems++")&&!runtime.Contains("private void Update("),"Food population is not finite and physically checked");
        foreach(string file in new[]{"PoolroomsWorld.cs","PoolroomsConnectedMalls.cs","PoolroomsMallSystem.cs","PoolroomsTerrainSystem.cs"})Check(File.ReadAllText(Path.Combine(root,"Assets/PoolroomsProcedural/Runtime",file)).Contains("PoolroomsProvisions.Attach("),"Missing external level distribution hook: "+file);
        ProvisionFeasibility();
        Console.WriteLine($"City/provisions: {samples} actual dreamcore city blocks at 3 scales / 2 seeds, 3 open pavilion programmes, peak {maxTriangles} triangles; 117 distinct food definitions + 8 drinks, all {RestfulLevelPlanner.EnvironmentCount} catalogue biomes eligible, unlimited-stack transactions and native placement hooks. Native physics/appearance pending.");
    }
}
