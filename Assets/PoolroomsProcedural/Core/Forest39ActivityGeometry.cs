using System;

namespace Poolrooms.Core
{
    public static class Forest39ActivityGeometry
    {
        public static MeshData Body(Forest39Activity kind, int position = 0, int limit = 6)
        {
            var mesh = new MeshData { Theme = SurfaceTheme.Forest39Bark };
            switch (kind)
            {
                case Forest39Activity.LeafPress: Press(mesh, position, limit); break;
                case Forest39Activity.StoneBalance: Balance(mesh, position, limit); break;
                case Forest39Activity.TrailCompass: Compass(mesh, position, limit); break;
                case Forest39Activity.WaterStudy: WaterGauge(mesh, position, limit); break;
                case Forest39Activity.Listening: Listening(mesh); break;
                case Forest39Activity.FallenLeaf: Leaf(mesh); break;
                case Forest39Activity.Acorn: Acorn(mesh); break;
                case Forest39Activity.FieldNotebook:
                    Table(mesh);
                    var book=new MeshData{Theme=SurfaceTheme.Forest39Bark};
                    Notebook(book);book.TranslateRotate(new V3(0,.805f,0),0);mesh.Append(book);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            return mesh;
        }

        public static MeshData Keepsake(int id)
        {
            if(id<0||id>2)throw new ArgumentOutOfRangeException(nameof(id));
            if(id<2)return Body(id==0?Forest39Activity.FallenLeaf:Forest39Activity.Acorn);
            var mesh=new MeshData{Theme=SurfaceTheme.Forest39Bark};Notebook(mesh);return mesh;
        }

        public static V3 Size(Forest39Activity kind)
        {
            switch (kind)
            {
                case Forest39Activity.FallenLeaf: return new V3(.3f, .065f, .4f);
                case Forest39Activity.Acorn: return new V3(.1f, .14f, .1f);
                case Forest39Activity.FieldNotebook: return new V3(.84f, .85f, .72f);
                case Forest39Activity.Listening: return new V3(1.1f, .65f, .5f);
                default: return new V3(.85f, 1.15f, .75f);
            }
        }

        static void B(MeshData mesh, float x, float z, float w, float d, float y, float top,
            Surface slot = Surface.Accent)
        {
            Forest39Architecture.Box(mesh, x, z, w, d, y, top, slot);
        }

        static void Table(MeshData mesh)
        {
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                    B(mesh, sx * .32f, sz * .25f, .07f, .07f, 0, .78f);
            B(mesh, 0, 0, .84f, .72f, .75f, .8f);
            B(mesh, 0, -.26f, .64f, .045f, .23f, .3f);
            B(mesh, 0, .26f, .64f, .045f, .23f, .3f);
        }

        static void Marks(MeshData mesh, int count, float y)
        {
            for (int mark = 0; mark < count; mark++)
                B(mesh, -.3f + mark * .115f, -.285f, .025f, .05f, y, y + .008f, Surface.Metal);
        }

        static void Press(MeshData mesh, int position, int limit)
        {
            Table(mesh);
            B(mesh, 0, 0, .56f, .43f, .8f, .84f);
            B(mesh, 0, 0, .52f, .4f, .86f + position * .025f, .9f + position * .025f);
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                {
                    Shapes.Column(mesh, sx * .22f, sz * .16f, .018f, .8f, 1.1f, 8, Surface.Metal, 1);
                    B(mesh, sx * .22f, sz * .16f, .1f, .035f, 1.04f, 1.075f, Surface.Metal);
                }
            Marks(mesh, limit, .801f);
        }

        static void Balance(MeshData mesh, int position, int limit)
        {
            Table(mesh);
            for (int i = 0; i <= position; i++)
                Shapes.Column(mesh, 0, 0, .17f - i * .017f, .8f + i * .055f,
                    .85f + i * .055f, 11, Surface.Tile, 1);
            Marks(mesh, limit, .801f);
        }

        static void Compass(MeshData mesh, int position, int limit)
        {
            Table(mesh);
            Shapes.Column(mesh, 0, 0, .28f, .8f, .84f, 32, Surface.Accent, 1);
            float angle = position * Shapes.Pi * 2 / limit;
            V3 a = new V3(-(float)Math.Sin(angle) * .065f, .855f, -(float)Math.Cos(angle) * .065f);
            V3 b = new V3((float)Math.Sin(angle) * .065f, .855f, (float)Math.Cos(angle) * .065f);
            V3 tip = new V3((float)Math.Cos(angle) * .23f, .855f, -(float)Math.Sin(angle) * .23f);
            mesh.Triangle(a, b, tip, V3.Up, V3.Up, V3.Up, new V2(0, 0), new V2(1, 0), new V2(.5f, 1), Surface.Metal);
            Marks(mesh, limit, .801f);
        }

        static void WaterGauge(MeshData mesh, int position, int limit)
        {
            Table(mesh);
            // A physical sample ruler, not an opaque fake water container.
            B(mesh, -.2f, .15f, .075f, .075f, .8f, 1.12f);
            for (int i = 0; i < limit; i++)
                B(mesh, -.2f, .105f, .07f, .01f, .84f + i * .045f, .849f + i * .045f, Surface.Metal);
            B(mesh, -.2f, .092f, .12f, .015f, .84f + position * .045f, .858f + position * .045f, Surface.Metal);
            B(mesh, .16f, -.03f, .24f, .28f, .8f, .82f);
        }

        static void Listening(MeshData mesh)
        {
            B(mesh, 0, 0, .82f, .42f, .42f, .48f);
            foreach (int sign in new[] { -1, 1 })
                B(mesh, sign * .31f, 0, .09f, .32f, 0, .42f);
        }

        static void Leaf(MeshData mesh)
        {
            mesh.Theme = SurfaceTheme.Forest39Leaves;
            var center = new V3(0, .02f, 0);
            const int count = 18;
            for (int i = 0; i < count; i++)
            {
                float a = i * Shapes.Pi * 2 / count;
                float b = (i + 1) * Shapes.Pi * 2 / count;
                float ra = .11f * (1 + .22f * (float)Math.Cos(a * 6));
                float rb = .11f * (1 + .22f * (float)Math.Cos(b * 6));
                var p = new V3((float)Math.Cos(a) * ra, .012f, (float)Math.Sin(a) * ra * 1.4f);
                var q = new V3((float)Math.Cos(b) * rb, .012f, (float)Math.Sin(b) * rb * 1.4f);
                mesh.Triangle(center, p, q, V3.Up, V3.Up, V3.Up,
                    new V2(.5f, .5f), new V2(p.X * 3 + .5f, p.Z * 2 + .5f),
                    new V2(q.X * 3 + .5f, q.Z * 2 + .5f), Surface.Tile);
            }
        }

        static void Acorn(MeshData mesh)
        {
            Shapes.Column(mesh, 0, 0, .033f, .01f, .085f, 12, Surface.Accent, 1);
            Shapes.Column(mesh, 0, 0, .041f, .07f, .11f, 12, Surface.Tile, 1);
            Shapes.Column(mesh, 0, 0, .007f, .1f, .13f, 7, Surface.Tile, 1);
        }

        static void Notebook(MeshData mesh)
        {
            B(mesh, 0, 0, .31f, .23f, 0, .005f);
            B(mesh, 0, 0, .29f, .21f, .005f, .024f, Surface.Plaster);
            B(mesh, -.015f, 0, .015f, .21f, .024f, .028f);
            for (int line = 0; line < 6; line++)
                B(mesh, .06f, -.075f + line * .027f, .1f, .0017f, .0241f, .025f, Surface.Metal);
        }
    }
}
