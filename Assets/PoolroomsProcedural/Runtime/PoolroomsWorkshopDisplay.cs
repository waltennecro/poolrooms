using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsWorkshopDisplay : MonoBehaviour
    {
        private PoolroomsStation station;
        private Mesh mesh;
        private WeakReference<WorkshopBoard> displayed;
        private int revision = -1;
        private readonly List<Vector3> positions = new List<Vector3>(2048);
        private readonly List<Vector3> normals = new List<Vector3>(2048);
        private readonly List<Vector2> coordinates = new List<Vector2>(2048);

        public static void Show(PoolroomsStation source, WorkshopBoard board)
        {
            if (source == null || board == null || !WorkshopCatalog.IsWorkshop(source.kind)) return;
            var view = source.GetComponent<PoolroomsWorkshopDisplay>();
            if (view == null) view = source.gameObject.AddComponent<PoolroomsWorkshopDisplay>();
            view.station = source;
            if (!view.Prepare()) return;
            if (view.displayed != null && view.displayed.TryGetTarget(out var prior) &&
                ReferenceEquals(prior, board) && view.revision == board.Revision) return;
            view.Upload(WorkshopStateGeometry.Build(source.kind, board, source.variant));
            view.displayed = new WeakReference<WorkshopBoard>(board);
            view.revision = board.Revision;
        }

        private bool Prepare()
        {
            if (mesh != null) return true;
            if (station.world == null || station.world.profile == null) return false;
            var chunk = GetComponentInParent<PoolroomsChunk>();
            if (chunk == null) return false;
            var node = new GameObject("Workshop state · shared PBR");
            node.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Workshop state" };
            mesh.MarkDynamic();
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>();
            var materials = new Material[6];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = PoolroomsWorkshopMaterials.Resolve(station.kind, i, station.world.profile, chunk);
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            // This component owns precisely one reusable mesh. It is not added
            // to chunk.ownedMeshes, preventing double destruction on unload.
            return true;
        }

        private void Upload(MeshData data)
        {
            positions.Clear();
            normals.Clear();
            coordinates.Clear();
            foreach (var point in data.Positions) positions.Add(new Vector3(point.X, point.Y, point.Z));
            foreach (var normal in data.Normals) normals.Add(new Vector3(normal.X, normal.Y, normal.Z));
            foreach (var uv in data.UV) coordinates.Add(new Vector2(uv.X, uv.Y));
            mesh.Clear();
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, coordinates);
            mesh.subMeshCount = 6;
            for (int i = 0; i < 6; i++) mesh.SetTriangles(data.Indices[i], i, false);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        }

        private void OnDestroy()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
            mesh = null;
        }
    }
}
