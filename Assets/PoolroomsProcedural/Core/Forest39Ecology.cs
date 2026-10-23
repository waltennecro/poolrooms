using System;

namespace Poolrooms.Core
{
    // Small, deterministic ground details are batched by material. They never
    // acquire a Rigidbody or an Update component, and respect the same protected
    // routes as trunks. The patterns differ by the actual clearing programme.
    public static class Forest39Ecology
    {
        public const int MaximumPatches = 32;

        public static void Append(RoomGeometry output, Forest39Plan plan)
        {
            var leaves = new MeshData { Theme = SurfaceTheme.Forest39Leaves };
            var wood = new MeshData { Theme = SurfaceTheme.Forest39Bark };
            var stone = new MeshData { Theme = SurfaceTheme.Forest39Stone };
            int accepted = 0;
            for (int candidate = 0; candidate < 96 && accepted < MaximumPatches; candidate++)
            {
                uint seed = LiminalRegions.Hash(unchecked((int)plan.Seed), candidate, 0, 0x391000u);
                float x = (Forest39Plan.Unit(seed) * 2 - 1) * (plan.Half - 2);
                float z = (Forest39Plan.Unit(seed * 137) * 2 - 1) * (plan.Half - 2);
                if (!plan.Clear(x, z, .6f)) continue;
                bool blocked = false;
                foreach (var tree in plan.Trees)
                    if ((tree.Foot - new V3(x, tree.Foot.Y, z)).Length < tree.Radius + .7f) blocked = true;
                foreach (var rock in plan.Stones)
                    if (Math.Abs(rock.Foot.X - x) < rock.Width * .5f + .6f &&
                        Math.Abs(rock.Foot.Z - z) < rock.Depth * .5f + .6f) blocked = true;
                if (blocked) continue;
                V3 foot = new V3(x, Forest39Terrain.MeshHeight(plan, x, z) + .008f, z);
                switch (plan.Clearing)
                {
                    case Forest39Clearing.MossGarden:
                        Moss(stone, leaves, foot, seed);
                        break;
                    case Forest39Clearing.ReflectionPool:
                        Reed(leaves, wood, foot, .42f, seed);
                        break;
                    case Forest39Clearing.ReadingGrove:
                        FallenLeaves(leaves, foot, seed);
                        break;
                    case Forest39Clearing.Herbarium:
                        Fern(leaves, foot, .5f, seed);
                        Mushroom(wood, foot + new V3(.17f, 0, .08f), .08f, seed);
                        break;
                    case Forest39Clearing.FallenOak:
                        Twig(wood, foot, seed);
                        break;
                    case Forest39Clearing.StoneCircle:
                        Pebbles(stone, foot, seed);
                        break;
                    case Forest39Clearing.PicnicGlade:
                        Grass(leaves, foot, .3f, seed);
                        break;
                    case Forest39Clearing.FernHollow:
                        Fern(leaves, foot, .75f, seed);
                        break;
                    case Forest39Clearing.SketchingPlace:
                        Fern(leaves, foot, .38f, seed);
                        FallenLeaves(leaves, foot, seed);
                        break;
                    case Forest39Clearing.ListeningGrove:
                        Reed(leaves, wood, foot, .7f, seed);
                        break;
                    case Forest39Clearing.RainShelter:
                        Mushroom(wood, foot, .12f, seed);
                        break;
                    default:
                        Grass(leaves, foot, .4f, seed);
                        Pebbles(stone, foot, seed);
                        break;
                }
                accepted++;
            }
            output.VisualMeshes.Add(leaves);
            output.VisualMeshes.Add(wood);
            output.VisualMeshes.Add(stone);
        }

        static V3 Point(V3 origin, float angle, float distance, float height)
        {
            return origin + new V3((float)Math.Cos(angle) * distance, height, (float)Math.Sin(angle) * distance);
        }

        public static void Fern(MeshData mesh, V3 foot, float size, uint seed)
        {
            for (int frond = 0; frond < 6; frond++)
            {
                float angle = frond * Shapes.Pi / 3 + Forest39Plan.Unit(seed) * Shapes.Pi;
                V3 side = new V3(-(float)Math.Sin(angle), 0, (float)Math.Cos(angle));
                for (int segment = 0; segment < 5; segment++)
                {
                    float t = (segment + .5f) / 5;
                    V3 center = Point(foot, angle, t * size * .7f, size * (.25f + .55f * (float)Math.Sin(t * 2)));
                    float half = size * .16f * (1 - t * .75f);
                    V3 forward = new V3((float)Math.Cos(angle) * size * .12f, .025f, (float)Math.Sin(angle) * size * .12f);
                    foreach (int sign in new[] { -1, 1 })
                    {
                        V3 tip = center + side * (sign * half) + forward;
                        mesh.Triangle(center - forward, tip, center + forward, V3.Up, V3.Up, V3.Up,
                            new V2(.5f, 0), new V2(sign < 0 ? 0 : 1, .5f), new V2(.5f, 1), Surface.Tile);
                    }
                }
            }
        }

        static void Grass(MeshData mesh, V3 foot, float size, uint seed)
        {
            for (int blade = 0; blade < 9; blade++)
            {
                float angle = blade * 2.399963f + seed % 13;
                float distance = .09f * (blade % 3);
                V3 basePoint = Point(foot, angle, distance, 0);
                V3 right = new V3((float)Math.Sin(angle) * .018f, 0, -(float)Math.Cos(angle) * .018f);
                V3 bend = Point(basePoint, angle, size * .32f, size * (.6f + blade % 4 * .12f));
                V3 middle = (basePoint + bend) * .5f + V3.Up * .04f;
                V3 n = V3.Cross(right, bend - basePoint).Unit;
                mesh.Quad(basePoint - right, basePoint + right, middle + right * .5f, middle - right * .5f, n, Surface.Tile, size);
                mesh.Triangle(middle - right * .5f, middle + right * .5f, bend, n, n, n,
                    new V2(0, .5f), new V2(1, .5f), new V2(.5f, 1), Surface.Tile);
            }
        }

        static void Reed(MeshData leaves, MeshData wood, V3 foot, float size, uint seed)
        {
            for (int stem = 0; stem < 4; stem++)
            {
                float angle = stem * 2.399963f + seed % 31;
                V3 basePoint = Point(foot, angle, .07f * stem, 0);
                V3 top = Point(basePoint, angle, .11f, size);
                Forest39Oak.Taper(wood, basePoint, top, .012f, .004f, 5, seed);
                V3 side = new V3((float)Math.Cos(angle) * .08f, 0, (float)Math.Sin(angle) * .08f);
                V3 middle = basePoint + V3.Up * (size * .65f);
                leaves.Triangle(middle, top + side, top - side, V3.Up, V3.Up, V3.Up,
                    new V2(.5f, 0), new V2(0, 1), new V2(1, 1), Surface.Tile);
            }
        }

        static void FallenLeaves(MeshData mesh, V3 foot, uint seed)
        {
            for (int i = 0; i < 4; i++)
            {
                var leaf = Forest39ActivityGeometry.Body(Forest39Activity.FallenLeaf);
                float angle = i * 2.399963f + seed % 11;
                leaf.TranslateRotate(new V3(foot.X + (float)Math.Cos(angle) * .17f,
                    foot.Y, foot.Z + (float)Math.Sin(angle) * .17f), angle);
                mesh.Append(leaf);
            }
        }

        static void Twig(MeshData mesh, V3 foot, uint seed)
        {
            float angle = Forest39Plan.Unit(seed) * Shapes.Pi * 2;
            V3 end = Point(foot, angle, .6f, .035f);
            Forest39Oak.Taper(mesh, foot + V3.Up * .015f, end, .019f, .006f, 5, seed);
            V3 fork = Point((foot + end) * .5f, angle + .7f, .21f, .02f);
            Forest39Oak.Taper(mesh, (foot + end) * .5f, fork, .011f, .003f, 5, seed);
        }

        static void Mushroom(MeshData mesh, V3 foot, float size, uint seed)
        {
            Forest39Oak.Taper(mesh, foot, foot + V3.Up * size, size * .12f, size * .09f, 7, seed);
            const int sides = 12;
            V3 top = foot + V3.Up * (size * 1.18f);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Shapes.Pi * 2 / sides, b = (i + 1) * Shapes.Pi * 2 / sides;
                V3 p = Point(foot, a, size * .48f, size * .83f);
                V3 q = Point(foot, b, size * .48f, size * .83f);
                mesh.Triangle(top, p, q, V3.Up, V3.Up, V3.Up,
                    new V2(.5f, .5f), new V2(0, 0), new V2(1, 0), Surface.Plaster);
                mesh.Triangle(foot + V3.Up * (size * .85f), q, p, V3.Up * -1, V3.Up * -1, V3.Up * -1,
                    new V2(.5f, .5f), new V2(1, 0), new V2(0, 0), Surface.Tile);
            }
        }

        static void Moss(MeshData stone, MeshData leaves, V3 foot, uint seed)
        {
            Pebbles(stone, foot, seed);
            Grass(leaves, foot + V3.Up * .03f, .09f, seed);
        }

        static void Pebbles(MeshData mesh, V3 foot, uint seed)
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2.399963f + seed % 13;
                V3 p = Point(foot, angle, .12f, 0);
                Shapes.Column(mesh, p.X, p.Z, .045f + i * .01f, p.Y - .02f, p.Y + .035f, 7, Surface.Tile, .2f);
            }
        }
    }
}
