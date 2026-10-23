using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Poolrooms.Core;

static partial class Checks
{
    static void CityBuildings()
    {
        Check(CityBuildingDesign.OrdinaryCount>100&&CityBuildingDesign.StrangeCount>=100,"Requested building bank sizes");
        var designs=new List<object>();var fingerprints=new HashSet<string>();var names=new HashSet<string>();int peak=0,builds=0,walking=0;
        for(int id=0;id<CityBuildingDesign.Count;id++)
        {
            var p=new CityBuildingDesign(id);names.Add(p.Name);
            foreach(float size in new[]{7.8f,10f,18f})
            {
                int floors=size==18?6:4;
                var b=new CityBuildingPlan(0,0,size,size,floors,0,0,25,false,design:id);
                var c=new LayoutConfig{DetailedCityBuildings=true,CurveSegments=16};var a=new RoomGeometry();CityBuildingArchitecture.Build(a,b,c);builds++;
                foreach(var m in new[]{a.Structure,a.Decoration,a.Glass,a.CollisionOnly}.Concat(a.VisualMeshes))Validate(m);
                if(size==10&&(id==0||id==63||id==128||id==255))
                {
                    var owned=RenderSurfaceOwnership.Resolve(new[]{a.Structure,a.Decoration}.Concat(a.VisualMeshes).ToArray());
                    Check(owned[0].TriangleCount==0&&owned[2].TriangleCount>0&&owned[2].Theme==SurfaceTheme.CityArchitecture&&owned[2].PaletteVariant==id,"Styled collision geometry rendered twice or lost its PBR owner");
                }
                if(size==10)
                {
                    string geometryHash=Hash(a.Structure,a.Decoration);
                    Check(fingerprints.Add(geometryHash),"Architectural duplicate independent of colour/props: "+id);
                    designs.Add(new{id,name=p.Name,family=p.Family,variant=p.Variant,strange=p.Strange,structuralSha256=geometryHash,colourAndSourcePropsIncluded=false});
                }
                int triangles=a.Structure.TriangleCount+a.Decoration.TriangleCount+a.VisualMeshes.Sum(m=>m.TriangleCount);
                peak=Math.Max(peak,triangles);Check(triangles<45000,"Single building exceeds geometry budget: "+id+" / "+size+" / "+triangles);
                var index=new ScenerySurfaceIndex(a,new SceneryWorkBudget());Check(index.Valid,"Invalid building support index: "+id);
                // Samples query actual triangles, not the recipe's own floor list.
                float sx=-size*.5f+2.2f;
                for(int floor=0;floor<floors;floor++)
                {
                    if(floor>0)index=new ScenerySurfaceIndex(a,new SceneryWorkBudget());
                    float y=.12f+floor*3.2f;
                    for(int step=0;step<=8;step++)
                    {
                        float x=sx+2.35f+(p.Envelope(b,floor).MaxX-.65f-(sx+2.35f))*step/8;
                        var q=new V3(x,y,-2.05f);
                        Check(index.Supported(q),"Broken upper front gallery support: "+id+"/"+size+"/"+floor+" step "+step+" x "+x);
                        Check(index.Clear(new SceneryBox(q+new V3(-.22f,.03f,-.22f),q+new V3(.22f,1.85f,.22f)),0),"Blocked front gallery: "+id+"/"+size+"/"+floor+" step "+step);
                        walking++;
                    }
                    if(floor==floors-1)continue;
                    for(int half=0;half<2;half++)for(int step=0;step<9;step++)
                    {
                        float z=half==0?-1.35f+(step+.5f)*.3f:1.35f-(step+.5f)*.3f;
                        var q=new V3(sx+(half==0?-.95f:.95f),y+half*1.6f+(step+1)*1.6f/9,z);
                        Check(index.Supported(q),"Stair tread support missing");
                        Check(index.Clear(new SceneryBox(q+new V3(-.24f,.025f,-.10f),q+new V3(.24f,1.85f,.10f)),0),"Stair headroom obstruction: "+id+"/"+size+"/"+floor);
                        walking++;
                    }
                }
                index=new ScenerySurfaceIndex(a,new SceneryWorkBudget());
                foreach(var lamp in a.Fixtures)
                    Check(index.Clear(new SceneryBox(lamp.Position-new V3(.03f,.03f,.03f),lamp.Position+new V3(.03f,.03f,.03f)),0),"Recessed facade traps an interior luminaire");
                foreach(var seat in a.SourceSeats)
                {Check(index.Supported(seat.Bottom-new V3(0,.004f,0)),"Source furniture floats: "+id);Check(SourceFurniture.Clear(a.Structure,seat,.005f),"Source furniture clips architecture: "+id);}
                // Shared 4K binds a full native source map, never a scaled placeholder.
                Check(CityBuildingDesign.Finish(id,Surface.Tile)==0||CityBuildingDesign.Finish(id,Surface.Tile)==19,"Wrong native-4K source binding");
            }
        }
        Check(fingerprints.Count==256&&names.Count==32,"Incomplete distinct design/family bank");
        var seen=new HashSet<int>();int blocks=0,imported=0,maxBlock=0;
        var world=new LayoutConfig{Seed=481516,CellSize=36,Procedural=LiminalGeneration.City11,DetailedCityBuildings=true,DreamCity11=true,EnableImportedBuildings=true,ExpandedVariety=true};
        for(int x=-600;x<=600;x++)
        {
            var r=new Room{X=x,Z=7,Exits=15};City11Planner.Describe(r,world);
            if(r.Plan.City.Airfield!=null)continue;
            Check(r.Plan.City.Valid(r,world),"Actual streamed city plan invalid");
            int pavilion=City11Dreamscape.Pavilion(r.Plan.City);
            for(int k=0;k<r.Plan.City.Buildings.Length;k++)
            {var b=r.Plan.City.Buildings[k];if(k!=pavilion&&b.Design>=0)seen.Add(b.Design);if(b.ExteriorSource==3)imported++;}
            if(x%30!=0)continue;
            var a=Architecture.Build(r,world);blocks++;int tris=a.Structure.TriangleCount+a.Decoration.TriangleCount+a.VisualMeshes.Sum(m=>m.TriangleCount);maxBlock=Math.Max(maxBlock,tris);
            Check(tris<90000,"City block exceeds optional support triangle budget");
            var slots=new HashSet<int>();foreach(var seat in a.SourceSeats)Check(slots.Add(seat.Slot),"Duplicate native source pickup key within room");
            // Keep both axes of the six-metre road clear of new construction.
            var index=new ScenerySurfaceIndex(a,new SceneryWorkBudget());Check(index.Valid,"City block support index exhausted");
            for(int i=-7;i<=7;i++)foreach(int axis in new[]{0,1})
            {
                var q=axis==0?new V3(0,.003f,i*2):new V3(i*2,.003f,0);
                Check(index.Clear(new SceneryBox(q+new V3(-1,.03f,-1),q+new V3(1,2.5f,1)),0),"New building blocks street");
            }
        }
        Check(seen.Count==256,"Some building designs never appear in real walking generation: "+seen.Count);Check(imported>0,"New licensed facade not integrated");
        var unified=new LayoutConfig();WalkableExploration.Configure(unified);Check(unified.DetailedCityBuildings,"Unified generation leaves new bank disabled");
        Check(!new LayoutConfig().DetailedCityBuildings,"Historical opt-in contract changed");
        string output=Environment.GetEnvironmentVariable("POOLROOMS_CITY_BUILDING_REPORT");
        if(!string.IsNullOrEmpty(output))File.WriteAllText(output,JsonSerializer.Serialize(new{ordinaryDesigns=128,strangeDesigns=128,designs,meshSamples=builds,walkingSamples=walking,worldPlanCells=1201,fullWorldBlocks=blocks,newFacadeInstances=imported,peakBuildingTriangles=peak,peakBlockTriangles=maxBlock,nativeUnityExecuted=false},new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine($"City architecture: 128 ordinary + 128 strange designs, 32 structural families; {builds} meshes at 7.8/10/18m, {walking} actual triangle support/clearance samples, 256 unique structural hashes. {blocks} full city blocks / all 256 designs found in 1201 planned walking cells / new licensed facade occurrences {imported}. Peak {peak} per-building, {maxBlock} per-block triangles before render ownership. Native Unity/PhysX/visual quality/FPS pending.");
    }
}
