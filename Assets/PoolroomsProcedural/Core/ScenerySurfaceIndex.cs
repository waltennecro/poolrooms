using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // A local broad phase, constructed once on the geometry worker. It avoids
    // rescanning every triangle for each chair, cup and floor-support sample.
    public sealed class ScenerySurfaceIndex
    {
        private const float Cell = 2;
        private const int MaximumCellSpan = 96;
        private readonly List<TriangleRecord> triangles = new List<TriangleRecord>();
        private readonly Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>();
        private readonly SceneryWorkBudget budget;
        private int[] visited;
        private int querySerial;
        public bool Valid { get; private set; } = true;
        public int Count { get { return triangles.Count; } }

        private readonly struct TriangleRecord
        {
            public readonly V3 A;
            public readonly V3 B;
            public readonly V3 C;
            public readonly SceneryBox Bounds;
            public readonly bool Floor;
            public readonly float VerticalNormal;

            public TriangleRecord(V3 a, V3 b, V3 c, bool floor, float verticalNormal)
            {
                A = a;
                B = b;
                C = c;
                Floor = floor;
                VerticalNormal = verticalNormal;
                Bounds = new SceneryBox(
                    new V3(Math.Min(a.X, Math.Min(b.X, c.X)),
                        Math.Min(a.Y, Math.Min(b.Y, c.Y)), Math.Min(a.Z, Math.Min(b.Z, c.Z))),
                    new V3(Math.Max(a.X, Math.Max(b.X, c.X)),
                        Math.Max(a.Y, Math.Max(b.Y, c.Y)), Math.Max(a.Z, Math.Max(b.Z, c.Z))));
            }

            public bool ProjectedHeight(float x, float z, out float height)
            {
                height = 0;
                if (!Bounds.ContainsXZ(x, z, -.00001f)) return false;
                double det = (double)(B.Z - C.Z) * (A.X - C.X) +
                    (double)(C.X - B.X) * (A.Z - C.Z);
                if (Math.Abs(det) < 1e-10) return false;
                double u = ((double)(B.Z - C.Z) * (x - C.X) +
                    (double)(C.X - B.X) * (z - C.Z)) / det;
                double v = ((double)(C.Z - A.Z) * (x - C.X) +
                    (double)(A.X - C.X) * (z - C.Z)) / det;
                if (u < -.00001 || v < -.00001 || u + v > 1.00001) return false;
                height = (float)(u * A.Y + v * B.Y + (1 - u - v) * C.Y);
                return true;
            }

            public bool Supports(V3 point)
            {
                if (!Floor || Math.Abs(point.Y - A.Y) > .002f) return false;
                if (!Bounds.ContainsXZ(point.X, point.Z, -.0001f)) return false;
                double det = (double)(B.Z - C.Z) * (A.X - C.X) +
                    (double)(C.X - B.X) * (A.Z - C.Z);
                if (Math.Abs(det) < 1e-10) return false;
                double u = ((double)(B.Z - C.Z) * (point.X - C.X) +
                    (double)(C.X - B.X) * (point.Z - C.Z)) / det;
                double v = ((double)(C.Z - A.Z) * (point.X - C.X) +
                    (double)(A.X - C.X) * (point.Z - C.Z)) / det;
                return u >= -.00001 && v >= -.00001 && u + v <= 1.00001;
            }
        }

        public ScenerySurfaceIndex(RoomGeometry geometry, SceneryWorkBudget budget)
        {
            this.budget = budget ?? throw new ArgumentNullException(nameof(budget));
            if (geometry == null) { Valid = false; return; }
            Add(geometry.Structure, true);
            if (Valid) Add(geometry.Decoration, false);
            if (Valid) Add(geometry.Glass, false);
            if (Valid) Add(geometry.CollisionOnly, false);
            visited = new int[triangles.Count];
        }

        private void Add(MeshData mesh, bool maySupport)
        {
            if (mesh == null || mesh.Normals.Count != mesh.Positions.Count)
            {
                Valid = false;
                return;
            }
            foreach (var indices in mesh.Indices)
            {
                if (indices.Count % 3 != 0) { Valid = false; return; }
                for (int i = 0; i < indices.Count; i += 3)
                {
                    if (!budget.Triangle()) { Valid = false; return; }
                    int ia = indices[i], ib = indices[i + 1], ic = indices[i + 2];
                    if (!ScenerySafety.Triangle(mesh, ia, ib, ic)) { Valid = false; return; }
                    var a = mesh.Positions[ia];
                    var b = mesh.Positions[ib];
                    var c = mesh.Positions[ic];
                    if (!ScenerySafety.Finite(mesh.Normals[ia])) { Valid = false; return; }
                    bool floor = maySupport && mesh.Normals[ia].Y > .9f &&
                        Math.Abs(a.Y - b.Y) < .001f && Math.Abs(a.Y - c.Y) < .001f;
                    var triangle = new TriangleRecord(a, b, c, floor, mesh.Normals[ia].Y);
                    if (!Insert(triangle)) { Valid = false; return; }
                }
            }
        }

        private static int Coordinate(float value)
        {
            return (int)Math.Floor(value / Cell);
        }

        private static long Key(int x, int z)
        {
            return ((long)x << 32) | (uint)z;
        }

        private bool Insert(TriangleRecord triangle)
        {
            var bounds = triangle.Bounds;
            // Local room bounds are small. Reject hostile inputs before casts,
            // integer wraparound, or a dictionary proportional to world space.
            if (!ScenerySafety.ValidBox(bounds) || Math.Abs(bounds.Min.X) > 4096 ||
                Math.Abs(bounds.Max.X) > 4096 || Math.Abs(bounds.Min.Z) > 4096 ||
                Math.Abs(bounds.Max.Z) > 4096) return false;
            int x0 = Coordinate(bounds.Min.X), x1 = Coordinate(bounds.Max.X);
            int z0 = Coordinate(bounds.Min.Z), z1 = Coordinate(bounds.Max.Z);
            if (x1 - x0 > MaximumCellSpan || z1 - z0 > MaximumCellSpan) return false;
            int index = triangles.Count;
            triangles.Add(triangle);
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                if (!budget.Link()) return false;
                long key = Key(x, z);
                if (!cells.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    cells.Add(key, list);
                }
                list.Add(index);
            }
            return true;
        }

        private bool BeginQuery()
        {
            if (!Valid || !budget.Query()) return false;
            if (querySerial == int.MaxValue)
            {
                Array.Clear(visited, 0, visited.Length);
                querySerial = 1;
            }
            else querySerial++;
            return true;
        }

        private bool FirstVisit(int index)
        {
            if (visited[index] == querySerial) return false;
            visited[index] = querySerial;
            return true;
        }

        public bool Clear(SceneryBox box, float margin = .025f)
        {
            if (!ScenerySafety.ValidBox(box) || !BeginQuery()) return false;
            var query = new SceneryBox(
                new V3(box.Min.X - margin, box.Min.Y + .002f, box.Min.Z - margin),
                new V3(box.Max.X + margin, box.Max.Y + margin, box.Max.Z + margin));
            int x0 = Coordinate(query.Min.X), x1 = Coordinate(query.Max.X);
            int z0 = Coordinate(query.Min.Z), z1 = Coordinate(query.Max.Z);
            if (x1 - x0 > MaximumCellSpan || z1 - z0 > MaximumCellSpan) return false;
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                if (!cells.TryGetValue(Key(x, z), out var list)) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    int index = list[i];
                    if (!FirstVisit(index)) continue;
                    if (!budget.Visit()) return false;
                    var bounds = triangles[index].Bounds;
                    // Triangle AABBs may be flat, so strict box overlap would
                    // incorrectly miss vertical walls and horizontal shelves.
                    if (bounds.Max.X < query.Min.X || bounds.Min.X > query.Max.X ||
                        bounds.Max.Y < query.Min.Y || bounds.Min.Y > query.Max.Y ||
                        bounds.Max.Z < query.Min.Z || bounds.Min.Z > query.Max.Z) continue;
                    return false;
                }
            }
            return !InsideSolid(query.Center);
        }

        private bool InsideSolid(V3 point)
        {
            if (!BeginQuery()) return true;
            if (!cells.TryGetValue(Key(Coordinate(point.X), Coordinate(point.Z)), out var list)) return false;
            float closest = float.MaxValue;
            float normal = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (!budget.Visit()) return true;
                var triangle = triangles[list[i]];
                if (Math.Abs(triangle.VerticalNormal) < .1f) continue;
                if (!triangle.ProjectedHeight(point.X, point.Z, out float y)) continue;
                if (y <= point.Y + .001f || y >= closest - .00001f) continue;
                closest = y;
                normal = triangle.VerticalNormal;
            }
            // The first upward-facing surface above a point is an exit from
            // solid geometry. A downward-facing roof is the normal empty room.
            return closest < float.MaxValue && normal > 0;
        }

        public bool Supported(V3 point)
        {
            if (!ScenerySafety.Finite(point) || !BeginQuery()) return false;
            if (!cells.TryGetValue(Key(Coordinate(point.X), Coordinate(point.Z)), out var list)) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (!budget.Visit()) return false;
                if (triangles[list[i]].Supports(point)) return true;
            }
            return false;
        }

        public bool FootprintSupported(SceneryPlacement placement)
        {
            var half = SourceFurniture.HalfExtents(placement.Source, placement.Yaw);
            float y = placement.Bottom.Y - RegionalSourcePlanner.ContactGap;
            // Nine contact samples are conservative across tile seams and stair
            // edges; a single center ray is insufficient for a long sofa.
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
                if (!Supported(new V3(placement.Bottom.X + x * half.X,
                    y, placement.Bottom.Z + z * half.Z))) return false;
            return true;
        }

        public bool ApproachSupported(SceneryBox objectBounds, V3 entry, float radius)
        {
            float y = entry.Y - RegionalSourcePlanner.ContactGap;
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var point = new V3(entry.X + x * radius, y, entry.Z + z * radius);
                if (objectBounds.ContainsXZ(point.X, point.Z)) return false;
                if (!Supported(point)) return false;
            }
            return true;
        }
    }
}
