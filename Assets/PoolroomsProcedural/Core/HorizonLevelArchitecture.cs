using System;

namespace Poolrooms.Core
{
    public static partial class RestfulLevelArchitecture
    {
        static void HorizonBay(RoomGeometry o, PlanRect b, RestfulLevelPlan p)
        {
            switch (p.Environment)
            {
                case LiminalEnvironment.MagnoliaCourts: MagnoliaCourt(o, b, p); break;
                case LiminalEnvironment.FunicularTerminal: FunicularBay(o, b, p); break;
                case LiminalEnvironment.StainedGlassHalls: StainedHall(o, b, p); break;
                case LiminalEnvironment.BellCourts: BellCourt(o, b, p); break;
                case LiminalEnvironment.LighthouseCourts: LighthouseCourt(o, b, p); break;
                default: throw new ArgumentOutOfRangeException(nameof(p.Environment));
            }
            // The wall-mounted activity is on its own physical post. Its inner-edge
            // approach stays outside the bay programme and the through-routes.
            int corner = (b.X > 0 ? 1 : 0) + (b.Z > 0 ? 2 : 0);
            var anchor = HorizonLevelPlanner.ActivityAnchor(b, corner, p.Layout);
            var pole = anchor.Center - anchor.Normal * .07f;
            Box(o.Structure, pole.X, pole.Z, .12f, .12f, 0, 1.55f, Surface.Metal);
            var a = anchor.A; var bb = anchor.B; var cc = anchor.C;
            var n = anchor.Normal; var back = n * -.055f;
            // One real triangle contains the complete interaction footprint.
            // Linen-backed notice panels do not receive another automatic fitting.
            o.Structure.Triangle(a, bb, cc, n, n, n, new V2(0, 0), new V2(1, 0), new V2(.5f, 1), Surface.Slide);
            o.Structure.Triangle(cc + back, bb + back, a + back, n * -1, n * -1, n * -1,
                new V2(.5f, 1), new V2(1, 0), new V2(0, 0), Surface.Slide);
            var points = new[] { a, bb, cc };
            for (int edge = 0; edge < 3; edge++)
            {
                var from = points[edge]; var to = points[(edge + 1) % 3];
                var outward = V3.Cross(to - from, n).Unit;
                o.Structure.Quad(from, to, to + back, from + back, outward, Surface.Metal);
            }
        }
        static void MagnoliaCourt(RoomGeometry o, PlanRect b, RestfulLevelPlan p)
        {
            float radius = Math.Min(b.Width, b.Depth) * .25f;
            float x = b.X, z = b.Z;
            Shapes.Column(o.Structure, x, z, radius, 0, .35f, 24, Surface.Plaster, 1.2f);
            Shapes.Column(o.Decoration, x, z, radius - .08f, .35f, .37f, 24, Surface.Tile, 1.2f);
            float crown = 2.2f + p.Elevation * .055f;
            Shapes.Column(o.Structure, x, z, .1f, .36f, crown, 12, Surface.Accent, 1.2f);
            int branches = 4 + p.Layout;
            for (int i = 0; i < branches; i++)
            {
                float a = (i + .25f) * Shapes.Pi * 2 / branches;
                var tip = new V3(x + radius * .8f * Shapes.Cos(a), crown + .3f, z + radius * .8f * Shapes.Sin(a));
                Beam(o.Structure, new V3(x, crown - .6f, z), tip, .045f, Surface.Accent);
                for (int flower = 0; flower < 3; flower++)
                {
                    var center = tip + new V3(.13f * Shapes.Cos(a + flower), flower * .13f, .13f * Shapes.Sin(a + flower));
                    MagnoliaBloom(o.Decoration, center, .15f + p.Rhythm * .006f);
                }
            }
            SlattedBench(o, b.X, b.MaxZ - .42f, b.Width * .72f, .55f);
            // Open pergola, with no painted leaf plane replacing the crown.
            float height = 3.7f + p.Layout * .15f;
            Frame(o, b, height, .18f, Surface.Accent);
            for (int rib = 0; rib < 3 + p.Rhythm; rib++)
                Box(o.Decoration, b.MinX + (rib + .5f) * b.Width / (3 + p.Rhythm), b.Z,
                    .06f, b.Depth, height, height + .08f, Surface.Accent);
        }
        static void MagnoliaBloom(MeshData mesh, V3 center, float radius)
        {
            const int petals = 6;
            for (int i = 0; i < petals; i++)
            {
                float a = i * Shapes.Pi * 2 / petals;
                var stem = center - V3.Up * .045f;
                var left = center + new V3(radius * .55f * Shapes.Cos(a - .35f), radius * .25f, radius * .55f * Shapes.Sin(a - .35f));
                var tip = center + new V3(radius * Shapes.Cos(a), radius * .55f, radius * Shapes.Sin(a));
                var right = center + new V3(radius * .55f * Shapes.Cos(a + .35f), radius * .25f, radius * .55f * Shapes.Sin(a + .35f));
                var normal = V3.Cross(left - stem, tip - stem).Unit;
                mesh.QuadUV(stem, left, tip, right, normal, normal, normal, normal,
                    new V2(.5f, 0), new V2(0, .5f), new V2(.5f, 1), new V2(1, .5f), Surface.Plaster);
                // A real thin reverse shell keeps petals visible from below.
                mesh.QuadUV(right - normal * .0015f, tip - normal * .0015f,
                    left - normal * .0015f, stem - normal * .0015f,
                    normal * -1, normal * -1, normal * -1, normal * -1,
                    new V2(1, .5f), new V2(.5f, 1), new V2(0, .5f), new V2(.5f, 0), Surface.Plaster);
            }
        }
        static void FunicularBay(RoomGeometry o, PlanRect b, RestfulLevelPlan p)
        {
            float w = Math.Min(2.6f, b.Width * .6f), d = Math.Min(3, b.Depth * .67f);
            float x = b.X, z = b.Z + .12f;
            // Walk-in stationary display cabin at floor level. The threshold is
            // below the player's step height, with a 1.2 m opening and 2.1 m headroom.
            ChamferedTop(o.Structure, x, z, w, d, 0, .12f, .02f, Surface.Metal);
            foreach (int side in new[] { -1, 1 })
            {
                Box(o.Structure, x + side * (w * .5f - .05f), z, .1f, d, .12f, .8f, Surface.Accent);
                Box(o.Glass, x + side * (w * .5f - .035f), z, .035f, d - .16f, .82f, 2.18f, Surface.Tile);
                Box(o.CollisionOnly, x + side * (w * .5f - .035f), z, .035f, d - .16f, .82f, 2.18f, Surface.Tile);
                Box(o.Structure, x + side * (w * .5f - .04f), z, .08f, d, 2.18f, 2.3f, Surface.Metal);
                foreach (int end in new[] { -1, 1 })
                    Box(o.Structure, x + side * (w * .5f - .05f), z + end * (d * .5f - .05f), .1f, .1f, .12f, 2.3f, Surface.Metal);
            }
            Box(o.Structure, x, z, w + .16f, d + .16f, 2.3f, 2.44f, Surface.Accent);
            // Open front and rear avoid a decorative box that cannot be entered.
            // A narrow seat along one side leaves a continuous aisle.
            Box(o.Structure, x + w * .5f - .25f, z, .36f, d * .55f, .12f, .48f, Surface.Accent);
            foreach (int side in new[] { -1, 1 })
                Beam(o.Decoration, new V3(x + side * .32f, 2.44f, z), new V3(x + side * .32f, 3.5f, z), .035f, Surface.Metal);
            Beam(o.Decoration, new V3(x, 3.5f, b.MinZ), new V3(x, 3.5f, b.MaxZ), .025f, Surface.Metal);
            for (int sleeper = 0; sleeper < 4 + p.Rhythm; sleeper++)
                Box(o.Decoration, x, b.MinZ + (sleeper + .5f) * b.Depth / (4 + p.Rhythm),
                    w * .72f, .075f, .015f, .035f, Surface.Accent);
        }
        static void StainedHall(RoomGeometry o, PlanRect b, RestfulLevelPlan p)
        {
            float height = 3.4f + p.Elevation * .11f;
            Frame(o, b, height, .2f, Surface.Plaster);
            int panes = 3 + p.Layout;
            float z = b.MaxZ - .24f;
            for (int pane = 0; pane < panes; pane++)
            {
                float w = (b.Width - .5f) / panes;
                float x = b.MinX + .25f + (pane + .5f) * w;
                Box(HorizonGlazing(o, pane % 3), x, z, w - .06f, .025f, .7f, height - .2f, Surface.Tile);
                Box(o.CollisionOnly, x, z, w - .06f, .025f, .7f, height - .2f, Surface.Tile);
                foreach (int sign in new[] { -1, 1 })
                    Box(o.Structure, x + sign * (w * .5f - .02f), z, .04f, .075f, .55f, height, Surface.Metal);
                for (int band = 0; band < 4; band++)
                {
                    float y = .7f + band * (height - .9f) / 4;
                    Box(o.Decoration, x, z - .035f, w - .02f, .04f, y, y + .025f, Surface.Metal);
                    // Leaded diamond lattice has physical depth and keeps the
                    // glass clear, rather than layering opaque colour over it.
                    Beam(o.Decoration, new V3(x - w * .35f, y, z - .03f),
                        new V3(x, y + .4f, z - .03f), .012f, Surface.Metal);
                    Beam(o.Decoration, new V3(x, y + .4f, z - .03f),
                        new V3(x + w * .35f, y, z - .03f), .012f, Surface.Metal);
                }
            }
            SlattedBench(o, b.X, b.Z, b.Width * .65f, .6f);
        }
        static MeshData HorizonGlazing(RoomGeometry output, int color)
        {
            foreach (var mesh in output.VisualMeshes)
                if (mesh.Theme == SurfaceTheme.Glass && mesh.PaletteVariant == 100 + color) return mesh;
            var result = new MeshData { Theme = SurfaceTheme.Glass, PaletteVariant = 100 + color };
            output.VisualMeshes.Add(result);
            return result;
        }
        static void BellCourt(RoomGeometry o, PlanRect b, RestfulLevelPlan p)
        {
            float height = 3.1f + p.Elevation * .08f;
            float z = b.MaxZ - .55f, width = b.Width * .72f;
            foreach (int side in new[] { -1, 1 })
            {
                Shapes.Column(o.Structure, b.X + side * width * .5f, z, .13f, 0, height, 12, Surface.Plaster, 1.2f);
                Shapes.Column(o.Structure, b.X + side * width * .5f, z, .2f, 0, .18f, 12, Surface.Plaster, 1.2f);
            }
            Box(o.Structure, b.X, z, width + .3f, .3f, height, height + .17f, Surface.Accent);
            int count = 3 + p.Layout;
            for (int bell = 0; bell < count; bell++)
            {
                float x = b.X - width * .5f + (bell + .5f) * width / count;
                float top = height - .15f, radius = Math.Min(.17f, width / count * .32f);
                Beam(o.Decoration, new V3(x, height, z), new V3(x, top - .2f, z), .014f, Surface.Metal);
                Shapes.Column(o.Decoration, x, z, radius * .6f, top - .4f, top - .2f, 16, Surface.Metal, 1.2f);
                Shapes.Column(o.Decoration, x, z, radius, top - .44f, top - .39f, 16, Surface.Metal, 1.2f);
                Shapes.Column(o.Decoration, x, z, .025f, top - .53f, top - .4f, 10, Surface.Metal, 1.2f);
            }
            SlattedBench(o, b.X, b.Z - .2f, b.Width * .65f);
        }
        static void LighthouseCourt(RoomGeometry o, PlanRect b, RestfulLevelPlan p)
        {
            float x = b.X, z = b.Z + .3f;
            float width = Math.Min(1.8f, b.Width * .44f);
            int steps = Math.Min(4 + p.Layout, Math.Max(3, (int)((b.Depth * .5f - .65f) / .29f)));
            float rise = .14f, tread = .29f, depth = steps * tread;
            // Stairs are wide enough for the controller, not a miniature prop.
            for (int step = 0; step < steps; step++)
                Box(o.Structure, x, z - .9f - depth + (step + .5f) * tread, width,
                    tread, 0, (step + 1) * rise, Surface.Plaster);
            float deck = steps * rise;
            Box(o.Structure, x, z, width, 1.8f, 0, deck, Surface.Plaster);
            float height = 3.5f + p.Elevation * .12f;
            foreach (int sign in new[] { -1, 1 })
            {
                float xx = x + sign * (width * .5f - .06f);
                foreach (int end in new[] { -1, 1 })
                    Box(o.Structure, xx, z + end * .84f, .12f, .12f, deck, height, Surface.Plaster);
                for (int rail = 0; rail < 3; rail++)
                    Box(o.Structure, xx, z, .07f, 1.65f, deck + .45f + rail * .25f, deck + .48f + rail * .25f, Surface.Metal);
            }
            Box(o.Structure, x, z, width + .25f, 2.05f, height, height + .16f, Surface.Accent);
            // Open entrance and back; glazing sits above the parapet, outside
            // the standing volume. No extra directional light is introduced.
            Box(o.Structure, x, z + .88f, width, .1f, deck, deck + 1.05f, Surface.Plaster);
            Box(o.Glass, x, z + .88f, width - .25f, .025f, deck + 1.08f, height - .1f, Surface.Tile);
            Box(o.CollisionOnly, x, z + .88f, width - .25f, .025f, deck + 1.08f, height - .1f, Surface.Tile);
            Shapes.Column(o.Glass, x, z, .27f, height + .17f, height + .73f, 16, Surface.Tile, 1.2f);
            Shapes.Column(o.Decoration, x, z, .11f, height + .2f, height + .65f, 16, Surface.Glow, 1.2f);
            Shapes.Column(o.Decoration, x, z, .3f, height + .73f, height + .82f, 16, Surface.Metal, 1.2f);
            o.Fixtures.Add(new Fixture { Position = new V3(x, height + .45f, z), Power = 80,
                Width = .3f, Depth = .3f, Temperature = 3000, Range = 3.5f, AmbientSupport = true });
        }
    }
}
