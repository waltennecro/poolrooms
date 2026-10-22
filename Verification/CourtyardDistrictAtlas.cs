using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Poolrooms.Core;

static partial class Checks
{
    static void CourtyardAtlas(string output)
    {
        output=Path.GetFullPath(output);Directory.CreateDirectory(output);string file=Path.Combine(output,"courtyard-mesh.json");
        if(File.Exists(file))throw new ArgumentException("Choose a new preview output directory.");
        var c=new LayoutConfig{Continuous=true,RebuildCourtyard188=true,Procedural=LiminalGeneration.Courtyard188,CellSize=36,EncounterExploration=true};
        var layout=new Layout(c);var meshes=new List<object>();var sources=new List<object>();int suites=0,windows=0;
        void Mesh(MeshData m,int x,int z,string name)
        {
            if(m.TriangleCount==0)return;
            meshes.Add(new{name,theme=m.Theme.ToString(),tex=Enumerable.Range(0,6).Select(i=>BiomeSurfaces.TextureSet(m.Theme,(Surface)i)),
                p=m.Positions.Select(v=>new[]{v.X+x*c.CellSize,v.Y,v.Z+z*c.CellSize}),uv=m.UV.Select(v=>new[]{v.X,v.Y}),indices=m.Indices});
        }
        for(int z=0;z<4;z++)for(int x=0;x<4;x++)
        {
            var r=layout.At(x,z);var g=Architecture.Build(r,c);suites+=r.Plan.Quiet.Courtyard.Suites.Length;
            windows+=CourtyardDistrictArchitecture.Openings(r,c).Count(w=>!w.Door);
            Mesh(g.Structure,x,z,"structure");Mesh(g.Decoration,x,z,"decoration");Mesh(g.Glass,x,z,"glass");
            foreach(var m in g.VisualMeshes)Mesh(m,x,z,"visual");
            foreach(var p in g.SourceProps.Concat(g.SourceSeats))sources.Add(new{id=SourceFurniture.Id(p.Source),p=new[]{p.Bottom.X+x*c.CellSize,p.Bottom.Y,p.Bottom.Z+z*c.CellSize},yaw=p.Yaw,length=SourceFurniture.Longest(p.Source)});
        }
        File.WriteAllText(file,JsonSerializer.Serialize(new{meshes,sources}));
        var record=new{chunks=16,suites,windows,sourceInstances=sources.Count,seed=c.Seed,cell=c.CellSize,
            nativeUnityExecuted=false,notGameplayScreenshot=true,
            limits="Technical architecture only. Native doors/blinds, imported source furniture, HDRP lighting and post-processing are not rendered by the preview."};
        File.WriteAllText(Path.Combine(output,"preview.json"),JsonSerializer.Serialize(record,new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine(JsonSerializer.Serialize(record));
    }
}
