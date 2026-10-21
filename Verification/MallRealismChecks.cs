using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void MallRetailWorld()
    {
        int sectors=0,shops=0,sources=0,fixtures=0,assemblies=0,products=0;var kinds=new HashSet<MallShopKind>();var models=new HashSet<int>();
        var finishes=new HashSet<int>();var patterns=new HashSet<int>();
        foreach(bool spacious in new[]{false,true})foreach(int seed in new[]{0,31,481516})
        {
            var plan=MallPlan.Create(seed,13,0,true,true,true,true,true,spacious,true);
            Check(plan.Valid()&&MallRealism.Enabled(plan),"Realistic mall no longer matches its reservation");
            var shell=MallArchitecture.Shell(plan,6,4.8f);Validate(shell.Structure);Validate(shell.Glass);
            Check(shell.Fixtures.Any(f=>f.MallSunlight),"Mall cannot hand aperture lights to the regional sun");
            var seen=new HashSet<int>();var built=new HashSet<int>();
            foreach(var s in plan.Shops)
            {
                kinds.Add(s.Kind);patterns.Add((int)((s.Seed>>8)%6));shops++;
                if(s.Program!=null)
                {
                    Check(s.Program.Assemblies.Length<=MallRetailPacking.MaximumAssemblies,"Unbounded retail furniture density");assemblies+=s.Program.Assemblies.Length;
                    foreach(var a in s.Program.Assemblies)foreach(var route in s.Program.Routes)Check(!a.Bounds.Overlaps(route,.29f),"Denser furnishings block a marked walking route");
                }
                foreach(Surface surface in Enum.GetValues(typeof(Surface)))
                {
                    int finish=MallRealism.Finish(MallRealism.Theme(s.Kind),surface,(int)(s.Seed%4));
                    Check(surface==Surface.Glow?finish==-1:finish>=0&&finish<MallRealism.FinishCount,"Invalid retail finish index");
                    if(finish>=0)finishes.Add(finish);
                }
                var grid=MallRetailLighting.Grid(s).ToArray();Check(grid.Length>1&&grid.Length<=64,"Unbounded or empty retail lighting grid");
                foreach(var lamp in grid)Check(lamp.X>-s.Width*.5f&&lamp.X<s.Width*.5f&&lamp.Z>0&&lamp.Z<s.Depth&&lamp.Y==4.97f,"Luminaire lies outside the shop/ceiling");
                int key=s.Floor*32+s.Sector;
                // Full geometry for a bounded but category-complete sector sample.
                if(!built.Add(key)||s.Id>35&&s.Floor>0)continue;
                var g=MallArchitecture.Sector(plan,s.Floor,s.Sector,16);sectors++;
                Validate(g.Geometry.Structure);Validate(g.Geometry.CollisionOnly);Validate(g.Geometry.Decoration);
                Check(g.Geometry.CollisionOnly.TriangleCount>0&&g.Geometry.VisualMeshes.Count>0,"Detailed interiors have no physical furniture or visuals");
                foreach(var visual in g.Geometry.VisualMeshes){Validate(visual);Check(MallRealism.IsTheme(visual.Theme),"A shop still uses generic biome finishes");}
                foreach(var source in g.Geometry.SourceSeats)
                {
                    Check(seen.Add(source.Slot),"Source furniture slot aliases another store/floor");
                    Check(source.Source>=0&&source.Source<SourceFurniture.Count&&ExplorationItems.Resolve("source:"+SourceFurniture.Id(source.Source))!=null,"Licensed mall source cannot be stored/resolved");
                    var half=SourceFurniture.HalfExtents(source.Source,source.Yaw);
                    foreach(int dx in new[]{-1,1})foreach(int dz in new[]{-1,1})Check(plan.FloorSupported(s.Floor,source.Bottom.X+dx*half.X,source.Bottom.Z+dz*half.Z),"Furniture hangs over the atrium");
                    Check(source.Bottom.Y>=s.Floor*MallPlan.FloorHeight-.001f,"Licensed furniture is below the mall slab");
                    if(source.Source>=14)Check(SourceFurniture.Dimensions(source.Source).Y>0,"New licensed model has no metric bounds");
                    sources++;models.Add(source.Source);
                }
                foreach(var lamp in g.Geometry.Fixtures)
                {
                    Check(lamp.MallLocal&&MallLighting.FloorEligible(s.Floor*MallPlan.FloorHeight+1.8f-lamp.Position.Y,lamp.FloorOffset),"Retail lamp belongs to another floor");
                    Check(lamp.Power>0&&lamp.Power<=15000&&lamp.Range<=11,"Unbounded shop floodlight remains");fixtures++;
                }
                products+=g.Pickups.Count;Check(g.Pickups.Select(p=>p.Id).Distinct().Count()==g.Pickups.Count,"Stock IDs alias after increasing density");
                Check(g.Pickups.All(p=>p.ItemId>=0&&p.ItemId<MallItemCatalog.Count),"Detailed merchandise has an invalid catalogue ID");
            }
        }
        Check(kinds.Count==15&&patterns.Count==6,"Lost retail categories or six structural ceiling programmes");
        Console.WriteLine("Retail source coverage: "+string.Join(",",models.OrderBy(i=>i)));
        Check(Enumerable.Range(14,14).All(models.Contains),"New furniture exists only in files, not the generated mall");
        foreach(int turn in new[]{0,1,2,3})
        {
            float yaw=turn*Shapes.Pi*.5f;var size=new V3(2,.5f,.2f);float expected=turn%2==0?2:2.9f;
            Check(MallMerchandise.RayOrientedBox(new V3(3,0,0),new V3(-1,0,0),new V3(),size,yaw,4,out float distance)&&Math.Abs(distance-expected)<.00001f,"Picking ignores the displayed object's rotation");
            Check(!MallMerchandise.RayOrientedBox(new V3(3,2,0),new V3(-1,0,0),new V3(),size,yaw,4,out _),"Picking hits empty air above stock");
        }
        Check(finishes.Count>=18,"Retail finishes are present only as unused files");
        Console.WriteLine($"PASS: mall retail; {shops} described shops / {assemblies} packed furnishing assemblies, {sectors} fully built sectors, {sources} supported licensed-source instances / {models.Count} source IDs, {fixtures} floor-local luminaires, {products} supported catalogue stock instances, {kinds.Count} shop categories, {patterns.Count} ceiling structures, {finishes.Count} bound source finishes. Core geometry/data only; Unity/shaders/appearance/FPS unrun.");
    }
}
