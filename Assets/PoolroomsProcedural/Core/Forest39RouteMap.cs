using System;
using System.Text;

namespace Poolrooms.Core
{
    public readonly struct Forest39RoutePosition
    {
        public readonly int Side, Segment;
        public readonly V2 Point;
        public readonly float DistanceFromPath, MetresToExit;

        public Forest39RoutePosition(int side, int segment, V2 point, float distance, float remaining)
        {
            Side = side;
            Segment = segment;
            Point = point;
            DistanceFromPath = distance;
            MetresToExit = remaining;
        }
    }

    public static class Forest39RouteMap
    {
        public static Forest39RoutePosition Nearest(Forest39Plan plan, float x, float z)
        {
            var closest = new Forest39RoutePosition(-1, -1, new V2(), float.MaxValue, 0);
            foreach (var trail in plan.Trails)
            {
                float travelled = 0;
                for (int index = 1; index < trail.Points.Length; index++)
                {
                    V2 a = trail.Points[index - 1], b = trail.Points[index];
                    float dx = b.X - a.X, dz = b.Y - a.Y;
                    float squared = dx * dx + dz * dz;
                    float t = squared < 1e-8f ? 0 : Math.Max(0,
                        Math.Min(1, ((x - a.X) * dx + (z - a.Y) * dz) / squared));
                    V2 projection = new V2(a.X + t * dx, a.Y + t * dz);
                    float distance = (float)Math.Sqrt((x - projection.X) * (x - projection.X) +
                        (z - projection.Y) * (z - projection.Y));
                    float segmentLength = (float)Math.Sqrt(squared);
                    if (distance < closest.DistanceFromPath)
                        closest = new Forest39RoutePosition(trail.Side, index - 1, projection,
                            distance, Math.Max(0, trail.Length - travelled - segmentLength * t));
                    travelled += segmentLength;
                }
            }
            return closest;
        }

        public static string Describe(Layout world, Room room, float localX, float localZ)
        {
            if (world == null || room?.Plan?.Forest39 == null) return "";
            var plan = room.Plan.Forest39;
            var text = new StringBuilder();
            text.AppendLine("Los senderos de este claro");
            for (int side = 0; side < 4; side++)
            {
                if (!room.HasExit(side)) continue;
                var other = world.Neighbour(room, side);
                text.Append(Direction(side));
                text.Append(" · ");
                if (other.Plan?.Forest39 != null)
                    text.Append(Forest39Stories.Title(other.Plan.Forest39.Clearing));
                else
                    text.Append(InteriorPlanner.Name(other.Environment));
                text.Append(" · ");
                text.Append((int)Math.Ceiling(plan.Trails[side].Length));
                text.AppendLine(" m desde el centro");
            }
            if (plan.Loop != null)
                text.AppendLine("Hay un paseo circular entre los robles del noroeste. Vuelve al mismo claro.");
            var nearest = Nearest(plan, localX, localZ);
            if (nearest.Side >= 0)
            {
                text.Append("Sendero más cercano: ");
                text.Append(Direction(nearest.Side));
                text.Append(". Quedan aproximadamente ");
                text.Append((int)Math.Ceiling(nearest.MetresToExit));
                text.AppendLine(" m hasta ese borde del claro.");
            }
            text.Append("El mapa describe caminos físicos. No activa portales ni cambia tu posición.");
            return text.ToString();
        }

        public static string Direction(int side)
        {
            switch (side)
            {
                case 0: return "Norte";
                case 1: return "Este";
                case 2: return "Sur";
                case 3: return "Oeste";
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }
    }
}
