using System;

namespace Poolrooms.Core
{
    public static class Forest39Clearings
    {
        public static void Append(RoomGeometry result, Forest39Plan plan)
        {
            var wood = new MeshData { Theme = SurfaceTheme.Forest39Bark };
            var stone = new MeshData { Theme = SurfaceTheme.Forest39Stone };
            var leaves = new MeshData { Theme = SurfaceTheme.Forest39Leaves };
            float x = plan.Feature.X, z = plan.Feature.Z;
            switch (plan.Clearing)
            {
                case Forest39Clearing.MossGarden:
                    MossCircle(stone, leaves, x, z);
                    break;
                case Forest39Clearing.ReflectionPool:
                    Lookout(wood, x, z);
                    break;
                case Forest39Clearing.ReadingGrove:
                    BookNook(wood, x, z);
                    break;
                case Forest39Clearing.Herbarium:
                    DryingFrames(wood, leaves, x, z);
                    break;
                case Forest39Clearing.FallenOak:
                    FallenTrunk(wood, x, z, plan.Seed);
                    break;
                case Forest39Clearing.StoneCircle:
                    StoneSeats(stone, x, z);
                    break;
                case Forest39Clearing.PicnicGlade:
                    Picnic(wood, x, z);
                    break;
                case Forest39Clearing.FernHollow:
                    FernRing(stone, leaves, x, z, plan.Seed);
                    break;
                case Forest39Clearing.SketchingPlace:
                    Easels(wood, x, z);
                    break;
                case Forest39Clearing.ListeningGrove:
                    WindStudy(wood, stone, x, z);
                    break;
                case Forest39Clearing.RainShelter:
                    LogStore(wood, x, z, plan.Seed);
                    break;
                default:
                    Waymarkers(wood, x, z);
                    break;
            }
            // The collider keeps the real physical volume. Exactly one styled
            // visual owner displays each face through RenderSurfaceOwnership.
            result.Structure.Append(wood);
            result.Structure.Append(stone);
            result.VisualMeshes.Add(wood);
            result.VisualMeshes.Add(stone);
            result.VisualMeshes.Add(leaves);
        }

        static void B(MeshData mesh, float x, float z, float width, float depth,
            float bottom, float top, Surface surface = Surface.Accent)
        {
            Forest39Architecture.Box(mesh, x, z, width, depth, bottom, top, surface);
        }

        static void Post(MeshData mesh, float x, float z, float height)
        {
            Shapes.Column(mesh, x, z, .075f, -.025f, height, 9, Surface.Tile, 1);
        }

        static void Seat(MeshData mesh, float x, float z, float width)
        {
            foreach (int sign in new[] { -1, 1 }) B(mesh, x + sign * width * .34f, z, .13f, .38f, 0, .43f);
            for (int slat = 0; slat < 3; slat++) B(mesh, x, z - .16f + slat * .16f, width, .13f, .43f, .49f);
        }

        static void MossCircle(MeshData stone, MeshData leaves, float x, float z)
        {
            // An open ring of low stones leaves the south approach unblocked.
            for (int i = 0; i < 14; i++)
            {
                float angle = -.1f + i * Shapes.Pi * 1.65f / 13;
                float xx = x + (float)Math.Cos(angle) * 1.45f;
                float zz = z + (float)Math.Sin(angle) * 1.35f;
                Shapes.Column(stone, xx, zz, .15f, -.04f, .13f, 8, Surface.Tile, .7f);
                if (i % 3 == 0) Forest39Ecology.Fern(leaves, new V3(xx, .12f, zz), .22f, (uint)i);
            }
        }

        static void Lookout(MeshData wood, float x, float z)
        {
            // Three 14 cm rises are climbable without jumping. This small
            // platform deliberately stays below the need for an exposed fall.
            for (int step = 0; step < 3; step++)
                B(wood, x, z - 1.2f + step * .28f, 2.1f, .29f, 0, (step + 1) * .14f);
            B(wood, x, z + .25f, 2.3f, 1.8f, .28f, .42f);
            foreach (int sign in new[] { -1, 1 })
            {
                B(wood, x + sign * 1.05f, z + .25f, .08f, 1.8f, .42f, .95f);
                Post(wood, x + sign * 1.05f, z + 1.05f, 1.18f);
            }
            B(wood, x, z + 1.08f, 2.1f, .08f, .98f, 1.08f);
        }

        static void BookNook(MeshData wood, float x, float z)
        {
            Seat(wood, x, z + .85f, 2.3f);
            // A weather-protected exchange shelf is a real open carcass, not a
            // solid block painted with a bookshelf texture.
            foreach (int sign in new[] { -1, 1 }) B(wood, x + sign * .56f, z - 1.1f, .065f, .33f, .35f, 1.55f);
            B(wood, x, z - 1.25f, 1.18f, .035f, .35f, 1.55f);
            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = .35f + shelf * .36f;
                B(wood, x, z - 1.1f, 1.18f, .35f, y, y + .04f);
                if (shelf == 3) continue;
                for (int book = 0; book < 5; book++)
                    B(wood, x - .4f + book * .18f, z - 1.09f, .06f, .21f, y + .04f,
                        y + .25f + (book % 3) * .025f, Surface.Plaster);
            }
            B(wood, x, z - 1.1f, 1.4f, .54f, 1.55f, 1.64f);
        }

        static void DryingFrames(MeshData wood, MeshData leaves, float x, float z)
        {
            foreach (int sign in new[] { -1, 1 })
            {
                float xx = x + sign * 1.2f;
                foreach (int end in new[] { -1, 1 }) Post(wood, xx, z + end * .9f, 1.6f);
                for (int level = 0; level < 3; level++)
                {
                    float y = .6f + level * .38f;
                    B(wood, xx, z, .04f, 1.8f, y, y + .035f);
                    for (int sample = 0; sample < 4; sample++)
                    {
                        var leaf = Forest39ActivityGeometry.Body(Forest39Activity.FallenLeaf);
                        leaf.TranslateRotate(new V3(xx, y + .04f, z - .65f + sample * .4f), sign * .3f);
                        leaves.Append(leaf);
                    }
                }
            }
        }

        static void FallenTrunk(MeshData wood, float x, float z, uint seed)
        {
            Forest39Oak.Taper(wood, new V3(x - 1.6f, .37f, z + .6f),
                new V3(x + 1.6f, .34f, z + .6f), .38f, .32f, 18, seed);
            for (int branch = 0; branch < 3; branch++)
            {
                V3 a = new V3(x - .9f + branch * .85f, .42f, z + .6f);
                V3 b = a + new V3(.2f, .32f, .5f);
                Forest39Oak.Taper(wood, a, b, .08f, .025f, 9, seed + (uint)branch);
            }
            Seat(wood, x, z - .9f, 2.5f);
        }

        static void StoneSeats(MeshData stone, float x, float z)
        {
            for (int seat = 0; seat < 7; seat++)
            {
                float angle = seat * Shapes.Pi * 1.6f / 6;
                float xx = x + (float)Math.Cos(angle) * 1.3f;
                float zz = z + (float)Math.Sin(angle) * 1.25f;
                Shapes.Column(stone, xx, zz, .27f, -.025f, .35f + seat % 3 * .06f, 13, Surface.Tile, .8f);
            }
        }

        static void Picnic(MeshData wood, float x, float z)
        {
            foreach (int sign in new[] { -1, 1 })
            {
                Seat(wood, x, z + sign * .9f, 2.5f);
                B(wood, x + sign * .8f, z, .1f, .7f, 0, .78f);
            }
            for (int plank = 0; plank < 5; plank++)
                B(wood, x, z - .36f + plank * .18f, 2.55f, .15f, .75f, .82f);
        }

        static void FernRing(MeshData stone, MeshData leaves, float x, float z, uint seed)
        {
            for (int plant = 0; plant < 14; plant++)
            {
                float angle = plant * Shapes.Pi * 2 / 14;
                V3 foot = new V3(x + (float)Math.Cos(angle) * 1.5f, 0, z + (float)Math.Sin(angle) * 1.25f);
                Forest39Ecology.Fern(leaves, foot, .65f + plant % 3 * .08f, seed + (uint)plant);
                if (plant % 4 == 0) Shapes.Column(stone, foot.X, foot.Z, .13f, -.04f, .1f, 9, Surface.Tile, .6f);
            }
        }

        static void Easels(MeshData wood, float x, float z)
        {
            foreach (int sign in new[] { -1, 1 })
            {
                float xx = x + sign * 1.05f;
                Forest39Oak.Taper(wood, new V3(xx - .25f, 0, z + .6f), new V3(xx, 1.4f, z + .8f), .025f, .02f, 6, 1);
                Forest39Oak.Taper(wood, new V3(xx + .25f, 0, z + .6f), new V3(xx, 1.4f, z + .8f), .025f, .02f, 6, 2);
                Forest39Oak.Taper(wood, new V3(xx, 0, z + 1.2f), new V3(xx, 1.2f, z + .8f), .025f, .02f, 6, 3);
                B(wood, xx, z + .78f, .52f, .035f, .7f, 1.3f, Surface.Plaster);
                B(wood, xx, z + .72f, .64f, .12f, .64f, .7f);
            }
        }

        static void WindStudy(MeshData wood, MeshData metal, float x, float z)
        {
            Post(wood, x - .8f, z + .8f, 2.4f);
            Post(wood, x + .8f, z + .8f, 2.4f);
            B(wood, x, z + .8f, 1.75f, .12f, 2.3f, 2.42f);
            for (int pipe = 0; pipe < 6; pipe++)
            {
                float xx = x - .58f + pipe * .23f;
                Shapes.Column(metal, xx, z + .8f, .022f, 1.35f + pipe * .055f, 2.16f, 9, Surface.Metal, 1);
                Shapes.Column(wood, xx, z + .8f, .003f, 2.16f, 2.3f, 5, Surface.Tile, 1);
            }
        }

        static void LogStore(MeshData wood, float x, float z, uint seed)
        {
            for (int row = 0; row < 3; row++)
            {
                int count = 4 - row;
                for (int log = 0; log < count; log++)
                {
                    float xx = x + (log - (count - 1) * .5f) * .3f;
                    float y = .15f + row * .26f;
                    Forest39Oak.Taper(wood, new V3(xx, y, z - .2f), new V3(xx, y, z + .9f), .15f, .14f, 12,
                        seed + (uint)(row * 4 + log));
                }
            }
        }

        static void Waymarkers(MeshData wood, float x, float z)
        {
            foreach (int sign in new[] { -1, 1 })
            {
                float xx = x + sign * 1.4f;
                Post(wood, xx, z + .7f, 1.75f);
                B(wood, xx, z + .7f, .65f, .06f, 1.3f, 1.5f);
                B(wood, xx, z + .7f, .48f, .06f, 1.55f, 1.72f);
                // Raised chevrons remain legible without a new UI overlay.
                for (int mark = 0; mark < 3; mark++)
                    B(wood, xx - .18f + mark * .14f, z + .735f, .04f, .014f, 1.35f, 1.43f, Surface.Metal);
            }
        }
    }
}
