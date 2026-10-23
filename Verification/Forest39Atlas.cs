using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Poolrooms.Core;

static partial class Checks
{
    static void Forest39Atlas(string directory)
    {
        if (Directory.Exists(directory)) throw new ArgumentException("Choose a new technical preview directory.");
        Directory.CreateDirectory(directory);
        var config = new LayoutConfig
        {
            Continuous = true, CellSize = 36, Seed = 39039,
            Procedural = LiminalGeneration.EnchantedForest39, EncounterExploration = true
        };
        var world = new Layout(config);
        var room = world.At(1, -1);
        var data = Architecture.Build(room, world);
        var surfaces = new[] { data.Structure, data.Decoration }.Concat(data.VisualMeshes).ToArray();
        var owned = RenderSurfaceOwnership.Resolve(surfaces);
        var meshes = new List<object>();
        void Add(string name, MeshData mesh)
        {
            if (mesh == null || mesh.TriangleCount == 0) return;
            meshes.Add(new
            {
                name, theme = mesh.Theme.ToString(),
                p = mesh.Positions.Select(p => new[] { p.X, p.Y, p.Z }).ToArray(),
                n = mesh.Normals.Select(p => new[] { p.X, p.Y, p.Z }).ToArray(),
                uv = mesh.UV.Select(p => new[] { p.X, p.Y }).ToArray(),
                indices = mesh.Indices.Select(i => i.ToArray()).ToArray()
            });
        }
        for (int index = 0; index < owned.Length; index++) Add("physical finish " + index, owned[index]);
        Add("water", data.Water);
        Add("oak branches", data.Forest39Canopy.Wood[0]);
        Add("oak leaves", data.Forest39Canopy.Leaves[0]);
        foreach (var action in Forest39Activities.Plan(room.Plan.Forest39))
        {
            if (action.Kind == Forest39Activity.Listening) continue;
            var mesh = Forest39ActivityGeometry.Body(action.Kind);
            mesh.TranslateRotate(action.Bottom, action.Yaw * Shapes.Pi / 180);
            Add("activity " + action.Kind, mesh);
        }
        var record = new
        {
            description = "Actual Core geometry and source textures, rendered outside Unity for architecture inspection only.",
            nativeUnityExecuted = false, sourceFurnitureOmitted = true,
            environment = room.Environment.ToString(), clearing = room.Plan.Forest39.Clearing.ToString(),
            cellSize = config.CellSize, meshes
        };
        File.WriteAllText(Path.Combine(directory, "forest39-mesh.json"), JsonSerializer.Serialize(record));
        Console.WriteLine("Technical forest geometry exported; native HDRP, imported furniture and physics are not validated by this atlas.");
    }
}
