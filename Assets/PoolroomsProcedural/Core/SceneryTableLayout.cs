using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class SceneryTableLayout
    {
        public static bool TryPlace(SceneryRecipe recipe, int index, SceneryAnchor anchor,
            IReadOnlyList<SceneryPlacement> proposal, out SceneryPlacement placement)
        {
            placement = default;
            if (recipe == null || index < 0 || index >= recipe.Count) return false;
            var piece = recipe.Piece(index);
            var point = anchor.Transform(piece.Offset);
            float yaw = ScenerySafety.NormalizedYaw(anchor.Yaw + piece.Yaw);
            int parentSlot = -1;
            if (piece.Parent >= 0)
            {
                bool found = false;
                for (int i = 0; i < proposal.Count; i++)
                {
                    var parent = proposal[i];
                    if (parent.Piece != piece.Parent) continue;
                    point.Y = parent.Bottom.Y + SourceFurniture.Dimensions(parent.Source).Y +
                        RegionalSourcePlanner.ContactGap;
                    parentSlot = parent.Slot;
                    found = true;
                    break;
                }
                if (!found) return false;
            }
            placement = new SceneryPlacement(
                SceneryAnchors.StableSlot(anchor, recipe.Id, index), recipe.Id,
                anchor.Slot, index, piece.Source, piece.Role, parentSlot, point, yaw);
            return ScenerySafety.ParentPresent(placement, proposal);
        }

        public static SceneryBox AccessBox(V3 at, float radius)
        {
            return new SceneryBox(at - new V3(radius, 0, radius),
                at + new V3(radius, 1.85f, radius));
        }

        public static V3 AccessPoint(SceneryPlacement piece, int side, float distance)
        {
            float a = (piece.Yaw + side * 90) * Shapes.Pi / 180;
            var size = SourceFurniture.Dimensions(piece.Source);
            float extent = side % 2 == 0 ? size.Z * .5f : size.X * .5f;
            return new V3(piece.Bottom.X + (float)Math.Sin(a) * (extent + distance),
                piece.Bottom.Y, piece.Bottom.Z + (float)Math.Cos(a) * (extent + distance));
        }

        public static bool NeedsAccess(SceneryRole role)
        {
            return role == SceneryRole.Seat || role == SceneryRole.Story || role == SceneryRole.Plant;
        }

        public static bool TryConnectedAccess(SceneryPlacement piece, Room room,
            RoomGeometry geometry, ScenerySurfaceIndex surfaces,
            SceneryReservations reservations, IReadOnlyList<SceneryPlacement> proposal,
            out SceneryBox access, out SceneryBox route)
        {
            access = route = default;
            const float radius = .32f;
            for (int side = 0; side < 4; side++)
            {
                var at = AccessPoint(piece, side, SceneryRecipeCatalog.Get(piece.Recipe).Approach);
                var candidate = AccessBox(at, radius);
                if (!ScenerySafety.InsideRoom(room, candidate, .15f)) continue;
                if (!WaterFree(geometry.Regions, candidate)) continue;
                if (!reservations.AccessClear(candidate)) continue;
                if (!SceneryReservations.ProposalAccessClear(candidate, proposal, piece.Slot)) continue;
                if (!surfaces.Clear(candidate, .01f)) continue;
                if (!surfaces.ApproachSupported(piece.Bounds, at, radius)) continue;
                if (!RouteToCross(candidate, room, surfaces, reservations, proposal, out var path)) continue;
                if (!WaterFree(geometry.Regions, path)) continue;
                access = candidate;
                route = path;
                return true;
            }
            return false;
        }

        public static bool WaterFree(IReadOnlyList<WaterRegion> water, SceneryBox approach)
        {
            return ScenerySafety.Dry(water, approach);
        }

        public static bool RouteToCross(SceneryBox access, Room room,
            ScenerySurfaceIndex surfaces, SceneryReservations reservations,
            IReadOnlyList<SceneryPlacement> proposal, out SceneryBox route)
        {
            var c = access.Center;
            // Move to the nearer protected axis without moving vertically.
            var end = Math.Abs(c.X) < Math.Abs(c.Z) ?
                new V3(0, access.Min.Y, c.Z) : new V3(c.X, access.Min.Y, 0);
            const float radius = .32f;
            route = new SceneryBox(
                new V3(Math.Min(c.X, end.X) - radius, access.Min.Y, Math.Min(c.Z, end.Z) - radius),
                new V3(Math.Max(c.X, end.X) + radius, access.Max.Y, Math.Max(c.Z, end.Z) + radius));
            if (!reservations.AccessClear(route)) return false;
            if (!SceneryReservations.ProposalAccessClear(route, proposal, -1)) return false;
            if (!surfaces.Clear(route, .01f)) return false;
            float length = (new V3(c.X, 0, c.Z) - new V3(end.X, 0, end.Z)).Length;
            int samples = Math.Max(1, (int)Math.Ceiling(length / .45f));
            if (samples > 160) return false;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                var p = new V3(c.X + (end.X - c.X) * t,
                    access.Min.Y - RegionalSourcePlanner.ContactGap,
                    c.Z + (end.Z - c.Z) * t);
                if (!surfaces.Supported(p)) return false;
            }
            return true;
        }
    }
}
