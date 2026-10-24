using System;

namespace Poolrooms.Core
{
    public sealed class Forest39Canopy
    {
        public readonly MeshData[] Wood = new MeshData[3];
        public readonly MeshData[] Leaves = new MeshData[3];

        public Forest39Canopy()
        {
            for (int i = 0; i < 3; i++)
            {
                Wood[i] = new MeshData { Theme = SurfaceTheme.Forest39Bark };
                Leaves[i] = new MeshData { Theme = SurfaceTheme.Forest39CanopyLeaves };
            }
        }
    }

    // Original authored oak generator. These are geometric variants, not
    // downloaded models. LODs keep the same branch positions and crown bounds.
    public static class Forest39Oak
    {
        public static Forest39Canopy Build(Forest39Plan plan){return Build(plan.Trees);}
        public static Forest39Canopy Build(Forest39Tree[] trees)
        {
            var result = new Forest39Canopy();
            foreach (var tree in trees)
            {
                for (int lod = 0; lod < 3; lod++)
                    Tree(result.Wood[lod], result.Leaves[lod], tree, lod);
            }
            return result;
        }

        static void Tree(MeshData wood, MeshData leaves, Forest39Tree tree, int lod)
        {
            int sides = lod == 0 ? 10 : lod == 1 ? 7 : 5;
            V3 foot = tree.Foot;
            float leanX = (Forest39Plan.Unit(tree.Seed) - .5f) * .7f;
            float leanZ = (Forest39Plan.Unit(tree.Seed * 17) - .5f) * .7f;
            V3 shoulder = foot + new V3(leanX, tree.Height * .56f, leanZ);
            V3 top = foot + new V3(leanX * 1.2f, tree.Height * .93f, leanZ * 1.2f);
            Taper(wood, foot, shoulder, tree.Radius * 1.12f, tree.Radius * .66f, sides, tree.Seed);
            Taper(wood, shoulder, top, tree.Radius * .66f, .045f, sides, tree.Seed + 1);
            // Buttress roots are attached to the flared trunk and terminate
            // below its surrounding terrain rather than floating radially.
            if (lod < 2)
            {
                for (int root = 0; root < 5; root++)
                {
                    float angle = root * Shapes.Pi * .4f + tree.Yaw * Shapes.Pi / 180;
                    V3 end = foot + new V3((float)Math.Cos(angle) * tree.Radius * 2,
                        -.04f, (float)Math.Sin(angle) * tree.Radius * 2);
                    Taper(wood, foot + V3.Up * .27f, end, tree.Radius * .34f, .035f, 5, tree.Seed + (uint)root);
                }
            }
            int branches = 8;
            for (int branch = 0; branch < branches; branch++)
            {
                uint seed = LiminalRegions.Hash(unchecked((int)tree.Seed), branch, 0, 0x390901u);
                float angle = branch * 2.399963f + tree.Yaw * Shapes.Pi / 180;
                float reach = tree.Crown * (.7f + Forest39Plan.Unit(seed) * .35f);
                float height = tree.Height * (.59f + branch * .038f);
                V3 start = foot + new V3(leanX * .8f, height - 1.15f, leanZ * .8f);
                V3 fork = foot + new V3((float)Math.Cos(angle) * reach * .62f,
                    height, (float)Math.Sin(angle) * reach * .62f);
                V3 end = foot + new V3((float)Math.Cos(angle) * reach,
                    height + .5f + Forest39Plan.Unit(seed * 3) * .65f, (float)Math.Sin(angle) * reach);
                Taper(wood, start, fork, tree.Radius * .43f, .085f, sides, seed);
                if (lod < 2) Taper(wood, fork, end, .085f, .018f, Math.Max(4, sides - 2), seed + 1);
                Crown(leaves, end, tree.Crown * .51f, tree.Crown * .38f, seed, lod);
                if (lod == 0)
                {
                    V3 twig = fork + new V3((float)Math.Sin(angle) * .75f, .85f, -(float)Math.Cos(angle) * .75f);
                    Taper(wood, fork, twig, .05f, .008f, 5, seed + 2);
                    Crown(leaves, twig, tree.Crown * .3f, tree.Crown * .28f, seed + 27, lod);
                }
            }
            Crown(leaves, top, tree.Crown * .58f, tree.Crown * .45f, tree.Seed + 127, lod);
        }

        public static void Taper(MeshData mesh, V3 start, V3 end, float lower, float upper, int sides, uint seed)
        {
            V3 axis = (end - start).Unit;
            V3 tangent = V3.Cross(axis, Math.Abs(axis.Y) > .9f ? new V3(1, 0, 0) : V3.Up).Unit;
            V3 bitangent = V3.Cross(axis, tangent).Unit;
            float length = (end - start).Length;
            float circumferenceRepeats = Math.Max(1, (float)Math.Ceiling(lower * Shapes.Pi * 2));
            for (int side = 0; side < sides; side++)
            {
                float a = side * Shapes.Pi * 2 / sides;
                float b = (side + 1) * Shapes.Pi * 2 / sides;
                V3 na = tangent * (float)Math.Cos(a) + bitangent * (float)Math.Sin(a);
                V3 nb = tangent * (float)Math.Cos(b) + bitangent * (float)Math.Sin(b);
                float rippleA = 1 + .07f * (float)Math.Sin(side * 3.7f + seed % 19);
                float rippleB = 1 + .07f * (float)Math.Sin(((side + 1) % sides) * 3.7f + seed % 19);
                V3 p = start + na * (lower * rippleA), q = start + nb * (lower * rippleB);
                V3 r = end + nb * (upper * rippleB), s = end + na * (upper * rippleA);
                float u = side * circumferenceRepeats / sides;
                float v = (side + 1) * circumferenceRepeats / sides;
                mesh.QuadUV(p, q, r, s, na, nb, nb, na,
                    new V2(u, 0), new V2(v, 0), new V2(v, length), new V2(u, length), Surface.Tile);
                mesh.Triangle(end, s, r, axis, axis, axis,
                    new V2(.5f, .5f), new V2(0, 0), new V2(1, 0), Surface.Tile);
            }
        }

        static void Crown(MeshData mesh, V3 center, float width, float height, uint seed, int lod)
        {
            int leaves = lod == 0 ? 70 : lod == 1 ? 22 : 8;
            for (int leaf = 0; leaf < leaves; leaf++)
            {
                uint h = LiminalRegions.Hash(unchecked((int)seed), leaf, lod == 0 ? 0 : 1, 0x390A01u);
                float angle = leaf * 2.399963f;
                float y = 1 - 2 * (leaf + .5f) / leaves;
                float radius = (float)Math.Sqrt(1 - y * y);
                V3 offset = new V3((float)Math.Cos(angle) * radius * width,
                    y * height, (float)Math.Sin(angle) * radius * width);
                V3 normal = new V3(offset.X / width, .4f + offset.Y / height, offset.Z / width).Unit;
                float size = lod == 0 ? .48f : lod == 1 ? .75f : 1.1f;
                Cluster(mesh, center + offset, normal, size, h, lod);
            }
        }

        static void Cluster(MeshData mesh, V3 center, V3 normal, float size, uint seed, int lod)
        {
            V3 right = V3.Cross(normal, Math.Abs(normal.Y) > .9f ? new V3(1, 0, 0) : V3.Up).Unit;
            V3 up = V3.Cross(right, normal).Unit;
            // Scanned alpha-cut foliage preserves photographed branch silhouettes
            // and veins without thousands of rigid star-shaped polygon fans.
            V3 a = center - right * size - up * size;
            V3 b = center + right * size - up * size;
            V3 c = center + right * size + up * size;
            V3 d = center - right * size + up * size;
            V3 bottom = center - up * size + normal * .035f;
            V3 top = center + up * size + normal * .035f;
            mesh.QuadUV(a, bottom, top, d, normal, normal, normal, normal,
                new V2(0,0),new V2(.5f,0),new V2(.5f,1),new V2(0,1),Surface.Tile);
            mesh.QuadUV(bottom, b, c, top, normal, normal, normal, normal,
                new V2(.5f,0),new V2(1,0),new V2(1,1),new V2(.5f,1),Surface.Tile);
        }

        public static void Collider(MeshData mesh, Forest39Tree tree)
        {
            // Only the trunk blocks walking; branches start above human headroom.
            // The collision primitive never changes with visual LOD.
            Shapes.Column(mesh, tree.Foot.X, tree.Foot.Z, tree.Radius * 1.12f,
                tree.Foot.Y, tree.Foot.Y + tree.Height * .55f, 10, Surface.Accent, 1);
        }
    }
}
