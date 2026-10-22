using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Poolrooms.Core;
static partial class Checks
{
    static double SurfaceArea(MeshData m,int surface=-1)
    {
        double area=0;for(int s=0;s<6;s++)if(surface<0||surface==s)
        {var t=m.Indices[s];for(int i=0;i<t.Count;i+=3)area+=V3.Cross(m.Positions[t[i+1]]-m.Positions[t[i]],m.Positions[t[i+2]]-m.Positions[t[i]]).Length*.5;}
        return area;
    }
    static void Sheet(MeshData m,float x0,float z0,float x1,float z1,float y=0,Surface s=Surface.Tile,bool reverse=false)
    {m.Quad(new V3(x0,y,z0),new V3(x1,y,z0),new V3(x1,y,z1),new V3(x0,y,z1),V3.Up*(reverse?-1:1),s,1);}
    static void RenderSurfaces()
    {
        var baseWall=new MeshData{Theme=SurfaceTheme.Hotel,PaletteVariant=2};Sheet(baseWall,0,0,4,4);Sheet(baseWall,1,1,3,3,s:Surface.Accent);
        for(int i=0;i<baseWall.Positions.Count;i++)baseWall.BlendWeights.Add(new V3(baseWall.Positions[i].X/4,0,0));
        string original=Hash(baseWall);var clean=RenderSurfaceOwnership.Resolve(baseWall)[0];Validate(clean);
        Check(Hash(baseWall)==original&&baseWall.TriangleCount==4,"Render cleanup changed source/physics geometry");
        Check(Math.Abs(SurfaceArea(clean)-16)<.00001&&Math.Abs(SurfaceArea(clean,(int)Surface.Accent)-4)<.00001,"Inset finish lost area or retained an overlapping base");
        Check(clean.Theme==baseWall.Theme&&clean.PaletteVariant==2,"Surface cleanup lost material metadata");
        foreach(int id in clean.Indices[(int)Surface.Tile])
        {var p=clean.Positions[id];Check(Math.Abs(clean.UV[id].X-p.X)<.00001&&Math.Abs(clean.UV[id].Y-p.Z)<.00001,"Clipping stretched world UV coordinates");Check(Math.Abs(clean.BlendWeights[id].X-p.X/4)<.00001,"Clipping lost material blend weights");}
        Check(ReferenceEquals(RenderSurfaceOwnership.Resolve(clean)[0],clean),"Surface cleanup is not idempotent");
        var duplicate=new MeshData();Sheet(duplicate,0,0,2,2);Sheet(duplicate,0,0,2,2,s:Surface.Accent);
        Check(RenderSurfaceOwnership.Resolve(duplicate)[0].TriangleCount==2,"Exact duplicate opaque faces still compete");
        var opposite=new MeshData();Sheet(opposite,0,0,2,2);Sheet(opposite,0,0,2,2,reverse:true);
        Check(ReferenceEquals(opposite,RenderSurfaceOwnership.Resolve(opposite)[0]),"Opposite-facing surfaces were removed");
        var offset=new MeshData();Sheet(offset,0,0,2,2);Sheet(offset,0,0,2,2,.004f);
        Check(ReferenceEquals(offset,RenderSurfaceOwnership.Resolve(offset)[0]),"Intentional relief was flattened");
        foreach(var theme in new[]{SurfaceTheme.Glass,SurfaceTheme.CygnusGlass,SurfaceTheme.SlideGlass,SurfaceTheme.FlumeFlow,SurfaceTheme.SoftPlayNet})
        {duplicate.Theme=theme;Check(ReferenceEquals(duplicate,RenderSurfaceOwnership.Resolve(duplicate)[0]),"Transparent layer was trimmed: "+theme);}
        var rotated=new MeshData();var a=new V3(1,2,3);var b=new V3(3,4,6);var c=new V3(5,2,1);var n=V3.Cross(b-a,c-a).Unit;
        for(int i=0;i<2;i++)rotated.Triangle(a,b,c,n,n,n,new V2(),new V2(),new V2(),(Surface)i);
        Check(RenderSurfaceOwnership.Resolve(rotated)[0].TriangleCount==1,"Rotated duplicate triangles remain");
        var huge=new MeshData();Sheet(huge,-500,-500,500,500);Sheet(huge,-500,-500,500,500,s:Surface.Accent);
        Check(RenderSurfaceOwnership.Resolve(huge)[0].TriangleCount==2,"Bounded grid fallback lost large/negative geometry");
        // Keep nonlinear UV mappings intact instead of changing their diagonal interpolation.
        var special=new MeshData();Sheet(special,0,0,4,4);special.UV[2]=new V2(7,8);Sheet(special,1,1,3,3,s:Surface.Accent);
        Check(ReferenceEquals(special,RenderSurfaceOwnership.Resolve(special)[0]),"A nonaffine UV mapping was treated as a plain rectangle");
        var under=new MeshData();Sheet(under,0,0,4,4);var finish=new MeshData();Sheet(finish,1,1,3,3,s:Surface.Metal);
        var group=RenderSurfaceOwnership.Resolve(under,finish);
        Check(Math.Abs(SurfaceArea(group[0])+SurfaceArea(group[1])-16)<.00001&&ReferenceEquals(group[1],finish),"Cross-renderer finish ownership failed");
        var body=RenderBoundary.Split(new[]{under},8,out var edge);
        Check(Math.Abs(SurfaceArea(body[0])+SurfaceArea(edge[0])-SurfaceArea(under))<.00001,"Seam extraction changed the surface area");
        var left=new MeshData();left.Quad(new V3(18,0,-18),new V3(18,0,18),new V3(18,8,18),new V3(18,8,-18),new V3(-1,0,0),Surface.Plaster);
        var right=RenderBoundary.Translate(left,new V3(-36,0,0));
        var main=RenderBoundary.Split(new[]{left},36,out var seam);
        Check(main[0].TriangleCount==0&&seam[0].TriangleCount==2,"Full shared wall escaped narrow seam extraction");
        var hidden=RenderBoundary.Resolve(seam,new[]{RenderBoundary.Translate(right,new V3(36,0,0))});
        Check(hidden[0].TriangleCount==0&&RenderBoundary.Resolve(seam,Array.Empty<MeshData>())[0].TriangleCount==2,"Shared walls do not restore after neighbour unload");
        Check(RenderBoundary.HigherPriority(int.MinValue,0,int.MaxValue,0)&&!RenderBoundary.HigherPriority(3,5,3,5),"Seam ownership is unstable at extreme coordinates");
        var report=new List<object>();int changed=0,chunks=0;var watch=Stopwatch.StartNew();
        foreach(var mode in Enum.GetValues<LiminalGeneration>())
        {
            if(mode==LiminalGeneration.Authored||mode==LiminalGeneration.Mixed||mode==LiminalGeneration.MixedWithRetail||mode==LiminalGeneration.Expanded)continue;
            var config=new LayoutConfig();WalkableExploration.Configure(config);config.Procedural=mode;config.DiscoveryCoverage=false;var layout=new Layout(config);
            foreach(int x in new[]{-3,2})
            {
                var r=layout.At(x,2);var g=Architecture.Build(r,layout);var input=new[]{g.Structure,g.Decoration}.Concat(g.VisualMeshes).ToArray();string before=Hash(input);
                var output=RenderSurfaceOwnership.Resolve(input);int oldOverlap=CoplanarPairs(input),after=CoplanarPairs(output);
                Check(oldOverlap>=after&&after==0,"Rectangular coplanar overlap remains in "+mode+" at "+x+",2: "+after);
                Check(Hash(input)==before,"Upload cleanup mutated the deterministic generator");
                Check(Hash(RenderSurfaceOwnership.Resolve(output))==Hash(output),"Generated mesh cleanup is not idempotent: "+mode);
                for(int i=0;i<output.Length;i++){Validate(output[i]);if(!ReferenceEquals(output[i],input[i]))changed++;}
                // Adjacent rooms: compare their actual local-coordinate seam meshes.
                var neighbour=layout.At(x+1,2);var ng=Architecture.Build(neighbour,layout);
                var ownBody=RenderBoundary.Split(output,config.CellSize,out var ownSeam);
                var neighbourClean=RenderSurfaceOwnership.Resolve(new[]{ng.Structure,ng.Decoration}.Concat(ng.VisualMeshes).ToArray());
                var neighbourBody=RenderBoundary.Split(neighbourClean,config.CellSize,out var neighbourSeam);
                var worldNeighbour=neighbourSeam.Select(m=>RenderBoundary.Translate(m,new V3(config.CellSize,0,0))).ToArray();
                var resolvedSeam=RenderBoundary.Resolve(ownSeam,worldNeighbour);
                var worldBody=neighbourBody.Select(m=>RenderBoundary.Translate(m,new V3(config.CellSize,0,0)));
                Check(CoplanarPairs(ownBody.Concat(resolvedSeam).Concat(worldBody).Concat(worldNeighbour).ToArray())==0,"Cross-chunk seam overlap remains in "+mode);
                int sources=input.Sum(m=>m.TriangleCount),rendered=output.Sum(m=>m.TriangleCount);
                report.Add(new{mode=mode.ToString(),x,z=2,overlapPairsBefore=oldOverlap,overlapPairsAfter=after,sourceTriangles=sources,renderTriangles=rendered,seamTriangles=ownSeam.Sum(m=>m.TriangleCount)});chunks++;
            }
        }
        int mallSectors=0;
        foreach(bool spacious in new[]{false,true})
        {
            var plan=MallPlan.Create(31,13,0,true,true,true,true,true,spacious,true);
            foreach(int floor in new[]{0,1})foreach(int sector in new[]{0,plan.SectorCount/2,plan.SectorCount-1})
            {
                var g=MallArchitecture.Sector(plan,floor,sector,16).Geometry;float y=-floor*MallPlan.FloorHeight;
                var inputs=new[]{g.Structure,g.Decoration}.Concat(g.VisualMeshes).Select(m=>RenderBoundary.Translate(m,new V3(0,y,0))).ToArray();
                var output=RenderSurfaceOwnership.Resolve(inputs);Check(CoplanarPairs(output)==0,"Local-floor retail finishes still overlap");mallSectors++;
            }
        }
        int terrainHouses=0;
        var region=TerrainRegionPlan.Create(31,3,-4);
        foreach(var house in region.Houses.Take(12))foreach(bool detailed in new[]{false,true})
        {
            var g=TerrainHouseArchitecture.Build(house,detailed,region);
            var input=detailed?new[]{g.Envelope.Structure,g.Roof,g.WallFinish,g.Interior.Structure,g.Interior.Decoration,g.Envelope.Decoration}:new[]{g.Geometry.Structure,g.Envelope.Decoration};
            var output=RenderSurfaceOwnership.Resolve(input);Check(CoplanarPairs(output)==0,"Level94 house finishes still overlap");terrainHouses++;
        }
        watch.Stop();
        File.WriteAllText(Environment.GetEnvironmentVariable("POOLROOMS_SURFACE_REPORT")??Path.Combine(Path.GetTempPath(),"poolrooms-surface-audit.json"),JsonSerializer.Serialize(new{chunks,mallSectors,terrainHouses,changedMeshes=changed,elapsedMilliseconds=watch.ElapsedMilliseconds,nativeUnityExecuted=false,samples=report},new JsonSerializerOptions{WriteIndented=true}));
        var runtime=File.ReadAllText("Assets/PoolroomsProcedural/Runtime/MeshFactory.cs");
        Check(runtime.Contains("PhysicsMesh(name+\" / original collision\",collisionSource,owner)")&&runtime.Contains("water?data:renderData??"),"Runtime lost original collision or water bypass");
        Console.WriteLine($"PASS: coplanar surface ownership, UV/blend/area preservation, transparent exclusions, seam unload restoration; {chunks} real chunks + {mallSectors} mall sectors + {terrainHouses} near/far house samples, {changed} changed render meshes, {watch.ElapsedMilliseconds} ms including architecture/audit (not FPS).");
    }
    // Independent detector: cardinal six-index rectangles, same-facing plane,
    // strict positive intersection. Opposite faces and transparent layers excluded.
    sealed class RenderAuditRect
    {public int Axis,Sign;public float Plane;public PlanRect Rect;}
    static float RenderCoordinate(V3 v,int axis){return axis==0?v.X:axis==1?v.Y:v.Z;}
    static int CoplanarPairs(MeshData[] meshes)
    {
        var faces=new Dictionary<(int,int,int),List<RenderAuditRect>>();int pairs=0;
        foreach(var m in meshes.Where(RenderSurfaceOwnership.IsOpaque))for(int s=0;s<6;s++)
        {
            var t=m.Indices[s];for(int i=0;i+5<t.Count;i+=3)
            {
                var p=t.Skip(i).Take(6).Select(j=>m.Positions[j]).ToArray();var n=t.Skip(i).Take(6).Select(j=>m.Normals[j]).ToArray();int axis=-1;
                for(int a=0;a<3;a++)if(p.All(v=>Math.Abs(RenderCoordinate(v,a)-RenderCoordinate(p[0],a))<.00001)&&n.All(v=>Math.Abs(RenderCoordinate(v,a))>.99999&&Math.Sign(RenderCoordinate(v,a))==Math.Sign(RenderCoordinate(n[0],a))))axis=a;
                if(axis<0)continue;int u=(axis+1)%3,vv=(axis+2)%3;float x=p.Min(v=>RenderCoordinate(v,u)),z=p.Min(v=>RenderCoordinate(v,vv)),right=p.Max(v=>RenderCoordinate(v,u)),top=p.Max(v=>RenderCoordinate(v,vv));
                if(right-x<.00001||top-z<.00001)continue;
                var corners=p.Select(v=>(RenderCoordinate(v,u),RenderCoordinate(v,vv))).Distinct().ToArray();
                if(corners.Length!=4||corners.Any(v=>Math.Abs(v.Item1-x)>.00001&&Math.Abs(v.Item1-right)>.00001||Math.Abs(v.Item2-z)>.00001&&Math.Abs(v.Item2-top)>.00001))continue;
                float area=(V3.Cross(p[1]-p[0],p[2]-p[0]).Length+V3.Cross(p[4]-p[3],p[5]-p[3]).Length)*.5f;
                if(Math.Abs(area-(right-x)*(top-z))>.0001)continue;
                var f=new RenderAuditRect{Axis=axis,Plane=RenderCoordinate(p[0],axis),Sign=Math.Sign(RenderCoordinate(n[0],axis)),Rect=new PlanRect(x,z,right,top)};int plane=(int)Math.Round(f.Plane*10000);
                for(int delta=-1;delta<=1;delta++)if(faces.TryGetValue((axis,f.Sign,plane+delta),out var bucket))foreach(var other in bucket)
                    if(Math.Abs(other.Plane-f.Plane)<.00001&&Math.Min(right,other.Rect.MaxX)-Math.Max(x,other.Rect.MinX)>.0001&&Math.Min(top,other.Rect.MaxZ)-Math.Max(z,other.Rect.MinZ)>.0001)pairs++;
                if(!faces.TryGetValue((axis,f.Sign,plane),out var own)){own=new List<RenderAuditRect>();faces.Add((axis,f.Sign,plane),own);}own.Add(f);i+=3;
            }
        }
        return pairs;
    }
}
