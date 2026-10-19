using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Poolrooms.Core;

static partial class Checks
{
    static void RestfulRefinementSafety()
    {
        int samples=0;
        foreach(int size in new[]{28,36,60})foreach(int layout in new[]{0,1,2,3})foreach(int digits in new[]{0,131068})
        {
            var c=new LayoutConfig{CellSize=size,DoorWidth=6,DoorHeight=5.1f,ObjectAbundance=3,CurveSegments=12};
            int recipe=digits+layout;
            for(int level=0;level<10;level++)
            {
                var r=new Room{Environment=(LiminalEnvironment)(18+level),Exits=15};RestfulLevelPlanner.DescribeRecipe(r,c,recipe);
                var g=RestfulLevelArchitecture.Build(r,c);var p=r.Plan.Restful;
                Check(g.VisualMeshes.Count==0&&g.Fixtures.Count==2,"Refinement adds rendering batches or unbounded lights");
                foreach(var mesh in new[]{g.Structure,g.Decoration,g.Glass})Validate(mesh);
                // Table edges have real bevel geometry while their flat support area
                // still accommodates every old pickup. Old slot IDs remain in place.
                for(int bay=0;bay<4;bay++)
                {
                    var b=p.Bays[bay];int sign=bay<2?-1:1;float tableZ=sign*(size*.5f-1.25f);
                    Check(Math.Abs(Support(g.Structure,new V3(b.X,1,tableZ))-RestfulLevelArchitecture.TableTop)<.001f,"Chamfer changed the usable table height");
                    var edge=NearestVisibleNormal(g.Structure,new V3(b.X+1.31f,1,tableZ),V3.Up*-1,out float edgeDepth);
                    Check(edge.Y>.25f&&edge.Y<.9f&&edge.X>.25f&&edgeDepth>.18f&&edgeDepth<.21f,"Table edge has no physical sloped bevel");
                    for(int i=0;i<15;i++)
                    {
                        var prop=p.Props[bay*15+i];Check(i<6?prop.CatalogId<0:prop.CatalogId>=0,"Existing pickup slot identity reordered");
                        if(i>=6)Check(Math.Abs(prop.Bottom.Y-.826f)<.0001f,"Old tabletop item support moved");
                    }
                    if(level==4)
                    {
                        var plant=p.Props[60+bay];Check(plant.Source=="G_PottedPlant"&&Math.Abs(plant.Bottom.Y-.401f)<.0001f,"Garden plant not appended above its bed");
                        var half=SourceFurniture.HalfExtents(9,plant.Yaw);
                        foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Check(Math.Abs(Support(g.Structure,plant.Bottom+new V3(sx*half.X,.001f,sz*half.Z))-.395f)<.001f,"Garden plant overhangs the raised support");
                    }
                    if(level==5)
                    {
                        // A 0.64 m controller needs more than the old .70 m bridge.
                        foreach(float x in new[]{-.5f,0,.5f})for(float z=b.MinZ+.2f;z<=b.MaxZ-.2f;z+=.35f)
                        {
                            Check(Math.Abs(Support(g.Structure,new V3(b.X+x,1,z))-.055f)<.002f,"Canal bridge lacks a full-width walking deck");
                            Check(SphereClear(g.Structure,new V3(b.X+x,.96f,z),.32f),"Canal bridge railing obstructs safe walking width");
                        }
                    }
                    if(level==8)
                    {
                        int count=2+p.Layout;float width=Math.Min(.8f,(b.Width-.25f)/count);
                        float x=b.MinX+b.Width/count*.5f,z=b.MaxZ-.5f;
                        var origin=new V3(x,.51f,z-1);var forward=new V3(0,0,1);
                        var wall=NearestVisibleNormal(g.Structure,origin,forward,out float depth);
                        Check(depth>.9f&&depth<1.05f&&wall.Z<-.9f,"Washer window is painted onto a solid front panel");
                        var glass=NearestVisibleNormal(g.Glass,origin,forward,out float pane);
                        Check(Math.Abs(pane-.558f)<.002f&&glass.Z<-.9f,"Washer lacks a separate outward-facing glass pane");
                        NearestVisibleNormal(g.Decoration,origin,forward,out float drum);
                        Check(drum>pane+.3f&&drum<1,"Washer has no visible recessed drum behind glass");
                    }
                    if(level==6)
                    {
                        var origin=new V3(b.X,1.02f,b.MaxZ-.45f-.8f);var forward=new V3(0,0,1);
                        NearestVisibleNormal(g.Structure,origin,forward,out float body);
                        var normal=NearestVisibleNormal(g.Decoration,origin,forward,out float rest);
                        Check(body-rest>.03f&&normal.Z<-.9f,"Music rest is hidden inside the opaque piano housing");
                    }
                    if(level==0)
                    {
                        var approach=new V3(b.X,1,b.MaxZ-.9f);
                        Check(Math.Abs(Support(g.Structure,approach))<.01f&&SphereClear(g.Structure,approach,.32f),"Bookcase overlaps the reading bench approach");
                    }
                }
                samples++;
            }
        }
        Console.WriteLine($"PASS: {samples} refinement geometry samples across 10 levels, 3 scales and layout/extreme recipes; bevel supports, garden planting, canal width, recessed glazed washers and bounded render cost. {assertions} core assertions. Native Unity unrun.");
    }
    // Stable R33 sample, now processed in reviewable groups rather than relabelled
    // as newly authored rooms. Room identity and recipe permutation never change.
    const int RestfulSamplePerLevel=20001,RestfulBatchSize=10000;
    static void RestfulBatches(string directory,string previous)
    {
        Directory.CreateDirectory(directory);
        var summaries=new List<object>();var unique=new HashSet<string>();long totalTriangles=0;
        var c=new LayoutConfig{CellSize=36,CurveSegments=12,ObjectAbundance=3};
        for(int batch=0;batch<21;batch++)
        {
            int start=batch*RestfulBatchSize,end=Math.Min(10*RestfulSamplePerLevel,start+RestfulBatchSize);
            string file=Path.Combine(directory,$"batch-{batch+1:D2}.jsonl");
            Check(!File.Exists(file),"Refusing to overwrite batch evidence: "+file);
            string[] before=previous==null?null:File.ReadAllLines(Path.Combine(previous,Path.GetFileName(file)));
            Check(before==null||before.Length==end-start,"Baseline batch has missing rooms");
            int changed=0,structureChanged=0;long triangles=0,beforeTriangles=0;int maximum=0;var levels=new HashSet<int>();
            using(var writer=new StreamWriter(file,false,new UTF8Encoding(false)))
            for(int ordinal=start;ordinal<end;ordinal++)
            {
                int level=ordinal/RestfulSamplePerLevel,index=ordinal%RestfulSamplePerLevel;
                int recipe=(int)(((long)index*65539+level*73)&131071);
                var r=new Room{X=17,Z=-23,Environment=(LiminalEnvironment)(18+level),Exits=15};
                RestfulLevelPlanner.DescribeRecipe(r,c,recipe);var g=RestfulLevelArchitecture.Build(r,c);
                string hash=Hash(g.Structure,g.Decoration,g.Glass,g.Water),structure=Hash(g.Structure);
                int count=g.Structure.TriangleCount+g.Decoration.TriangleCount+g.Glass.TriangleCount+g.Water.TriangleCount;
                Check(unique.Add(hash),"Duplicate architecture in refined batches");
                if(before!=null)
                {
                    using var record=JsonDocument.Parse(before[ordinal-start]);var old=record.RootElement;
                    Check(old.GetProperty("ordinal").GetInt32()==ordinal&&old.GetProperty("level").GetInt32()==level&&old.GetProperty("recipe").GetInt32()==recipe,"Recipe identity moved between reviews");
                    bool different=old.GetProperty("mesh").GetString()!=hash;
                    Check(different,"Batch contains unchanged architecture: "+ordinal);changed++;
                    if(old.GetProperty("structure").GetString()!=structure)structureChanged++;
                    int oldCount=old.GetProperty("triangles").GetInt32();beforeTriangles+=oldCount;
                    Check(count<=oldCount+5000,"Room detail exceeds explicit additional triangle budget");
                }
                // Finiteness/bounds checked on the actual generated mesh, not labels.
                foreach(var m in new[]{g.Structure,g.Decoration,g.Glass,g.Water})
                foreach(var v in m.Positions)
                    Check(float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.X)<=18.3f&&Math.Abs(v.Z)<=18.3f&&v.Y>=-1&&v.Y<=r.Height+1,"Invalid or escaping batch vertex");
                writer.WriteLine(JsonSerializer.Serialize(new{ordinal,level,recipe,mesh=hash,structure,triangles=count,props=r.Plan.Restful.Props.Length}));
                triangles+=count;maximum=Math.Max(maximum,count);levels.Add(level);
            }
            var bytes=File.ReadAllBytes(file);string digest=Convert.ToHexString(SHA256.HashData(bytes));
            summaries.Add(new{batch=batch+1,startInclusive=start,endExclusive=end,rooms=end-start,levels=levels.OrderBy(v=>v).ToArray(),changedMeshes=changed,changedStructures=structureChanged,triangles,beforeTriangles,maximumTriangles=maximum,sha256=digest});
            totalTriangles+=triangles;
            File.WriteAllText(Path.Combine(directory,"summary.json"),JsonSerializer.Serialize(new{comparison=previous!=null,completedBatches=summaries.Count,rooms=unique.Count,triangles=totalTriangles,nativeUnityExecuted=false,batches=summaries},new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine($"PASS batch {batch+1:D2}: {end-start} rooms, {changed} changed meshes / {structureChanged} structures, max {maximum} triangles, SHA256 {digest}");
        }
        Console.WriteLine($"PASS: {unique.Count} distinct architectures in 21 batches; {assertions} core assertions. Native Unity unrun.");
    }
}
