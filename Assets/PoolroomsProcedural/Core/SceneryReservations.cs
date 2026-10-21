using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Holds authored furnishings and reachable activity envelopes. Temporary
    // proposals live separately so a rejected cluster never leaves reservations.
    public sealed class SceneryReservations
    {
        private readonly List<SceneryBox> occupied = new List<SceneryBox>();
        private readonly List<SceneryBox> approaches = new List<SceneryBox>();
        public bool Valid { get; private set; } = true;
        public int Count { get { return occupied.Count; } }

        public SceneryReservations(Room room, RoomGeometry geometry)
        {
            if (room == null || geometry == null) { Valid = false; return; }
            foreach (var source in geometry.SourceProps) AddSource(source.Source, source.Bottom, source.Yaw);
            foreach (var source in geometry.SourceSeats) AddSource(source.Source, source.Bottom, source.Yaw);
            var life = geometry.Life;
            if (life != null)
            {
                foreach (var piece in life.Furniture)
                {
                    if (piece.Kind == LifeFurnishing.Source)
                        AddSource(piece.Source, piece.Bottom, piece.Yaw);
                    else if (piece.Kind == LifeFurnishing.Catalog)
                        AddCatalog(piece.Source, piece.Bottom, piece.Yaw);
                    else AddBespoke(piece.Kind, piece.Bottom, piece.Yaw);
                }
                if (life.Notice.HasValue) AddActivity(life.Notice.Value, room.Floor);
                foreach (var station in life.Stations) AddActivity(station.Anchor, room.Floor);
            }
            var p = room.Plan;
            var props = p?.Restful?.Props ?? p?.Rain?.Props ?? p?.Aquatic?.Props ??
                p?.Baths?.Props ?? p?.Garden?.Props ?? p?.Quiet?.Props;
            if (props != null)
            {
                for (int i = 0; i < props.Length; i++)
                {
                    if (i >= SceneryWorkBudget.MaximumReservations) { Valid = false; break; }
                    var prop = props[i];
                    if (prop.CatalogId >= 0) AddCatalog(prop.CatalogId, prop.Bottom, prop.Yaw);
                    else if (ScenerySafety.TrySource(prop.Source, out int source))
                        AddSource(source, prop.Bottom, prop.Yaw);
                    else
                    {
                        // Unknown optional source dimensions cannot justify an
                        // optimistic empty space. Keep its entire local envelope.
                        Add(new SceneryBox(prop.Bottom - new V3(.8f, 0, .8f),
                            prop.Bottom + new V3(.8f, 1.8f, .8f)));
                    }
                }
            }
            if (p?.City != null)
                foreach (var prop in p.City.Props) AddSource(prop.Source, prop.Position, prop.Yaw);
        }

        private void AddActivity(DressingAnchor anchor, float floor)
        {
            var center = anchor.Center + anchor.Normal * .65f;
            AddApproach(new SceneryBox(
                new V3(center.X - .65f, floor, center.Z - .65f),
                new V3(center.X + .65f, Math.Max(floor + 2, center.Y + .3f), center.Z + .65f)));
        }

        private void AddSource(int source, V3 bottom, float yaw)
        {
            if (!ScenerySafety.TryBounds(source, bottom, yaw, out var box))
            {
                Valid = false;
                return;
            }
            Add(box);
        }

        private void AddCatalog(int id, V3 bottom, float yaw)
        {
            if (id < 0 || id >= MallItemCatalog.Count || !ScenerySafety.Finite(bottom) || !ScenerySafety.Finite(yaw))
            {
                Valid = false;
                return;
            }
            var size = MallItemCatalog.Get(id).Size;
            float a = yaw * Shapes.Pi / 180;
            float c = Math.Abs((float)Math.Cos(a)), s = Math.Abs((float)Math.Sin(a));
            float x = (size.X * c + size.Z * s) * .5f;
            float z = (size.Z * c + size.X * s) * .5f;
            Add(new SceneryBox(bottom - new V3(x, 0, z), bottom + new V3(x, size.Y, z)));
        }

        private void AddBespoke(LifeFurnishing kind, V3 bottom, float yaw)
        {
            var mesh = EncounterFurniture.Body(kind);
            mesh.TranslateRotate(bottom, yaw * Shapes.Pi / 180);
            if (mesh.Positions.Count == 0) return;
            V3 lo = mesh.Positions[0], hi = lo;
            foreach (var point in mesh.Positions)
            {
                lo.X = Math.Min(lo.X, point.X);
                lo.Y = Math.Min(lo.Y, point.Y);
                lo.Z = Math.Min(lo.Z, point.Z);
                hi.X = Math.Max(hi.X, point.X);
                hi.Y = Math.Max(hi.Y, point.Y);
                hi.Z = Math.Max(hi.Z, point.Z);
            }
            Add(new SceneryBox(lo, hi));
        }

        public void Add(SceneryBox box)
        {
            if (!ScenerySafety.ValidBox(box) || occupied.Count >= SceneryWorkBudget.MaximumReservations)
            {
                Valid = false;
                return;
            }
            occupied.Add(box);
        }

        public void AddApproach(SceneryBox box)
        {
            if (!ScenerySafety.ValidBox(box) || approaches.Count >= SceneryWorkBudget.MaximumReservations)
            {
                Valid = false;
                return;
            }
            approaches.Add(box);
        }

        public bool Clear(SceneryBox box, float gap = .08f)
        {
            if (!Valid || !ScenerySafety.ValidBox(box)) return false;
            for (int i = 0; i < occupied.Count; i++)
                if (box.Overlaps(occupied[i], gap)) return false;
            for (int i = 0; i < approaches.Count; i++)
                if (box.Overlaps(approaches[i], .02f)) return false;
            return true;
        }

        public bool AccessClear(SceneryBox approach)
        {
            if (!Valid || !ScenerySafety.ValidBox(approach)) return false;
            for (int i = 0; i < occupied.Count; i++)
                if (approach.Overlaps(occupied[i], .03f)) return false;
            return true;
        }

        public void Commit(IReadOnlyList<SceneryPlacement> pieces,
            IReadOnlyList<SceneryBox> access)
        {
            for (int i = 0; i < pieces.Count; i++) Add(pieces[i].Bounds);
            for (int i = 0; i < access.Count; i++) AddApproach(access[i]);
        }

        public static bool ProposalClear(SceneryPlacement piece,
            IReadOnlyList<SceneryPlacement> proposal)
        {
            var bounds = piece.Bounds;
            for (int i = 0; i < proposal.Count; i++)
            {
                var other = proposal[i];
                // The support itself is allowed to touch its child at the
                // deliberately reserved contact gap, never penetrate its volume.
                if (other.Slot == piece.ParentSlot) continue;
                if (bounds.Overlaps(other.Bounds, .015f)) return false;
            }
            return true;
        }

        public static bool ProposalAccessClear(SceneryBox approach,
            IReadOnlyList<SceneryPlacement> proposal, int ownSlot)
        {
            for (int i = 0; i < proposal.Count; i++)
            {
                if (proposal[i].Slot == ownSlot) continue;
                if (approach.Overlaps(proposal[i].Bounds, .02f)) return false;
            }
            return true;
        }
    }
}
