using System;

namespace Poolrooms.Core
{
    // Small original joinery fits the same validated wall envelope as existing
    // stations. It adds no floor collider, light, reflection probe or texture.
    public static class WorkshopModels
    {
        public static MeshData Build(StationKind kind, int variant)
        {
            if (!WorkshopCatalog.IsWorkshop(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (variant < 0 || variant > 2) throw new ArgumentOutOfRangeException(nameof(variant));
            var mesh = NewMesh();
            Frame(mesh, variant);
            switch (kind)
            {
                case StationKind.LanternCircuit: Lanterns(mesh); break;
                case StationKind.WaterChannels: Channels(mesh, variant); break;
                case StationKind.Weaving: Loom(mesh, variant); break;
                case StationKind.Herbarium: Herbarium(mesh, variant); break;
                case StationKind.Sundial: Sundial(mesh); break;
                case StationKind.ParcelBalance: Balance(mesh, variant); break;
                case StationKind.StarChart: Stars(mesh, variant); break;
                case StationKind.BellSequence: Bells(mesh, variant); break;
                case StationKind.TideLocks: Reservoirs(mesh); break;
                case StationKind.Bookbinding: Binding(mesh, variant); break;
                case StationKind.WindRose: Compasses(mesh); break;
                case StationKind.SeedNursery: Nursery(mesh, variant); break;
            }
            return mesh;
        }

        private static MeshData NewMesh()
        {
            return new MeshData { Theme = SurfaceTheme.EncounterFurniture, EmissionStrength = 0 };
        }

        private static void Frame(MeshData mesh, int variant)
        {
            Box(mesh, 0, 0, .021f, .53f, .39f, .036f, Surface.Accent);
            Box(mesh, 0, 0, .044f, .48f, .34f, .012f, Surface.Plaster);
            foreach (int side in new[] { -1, 1 })
            {
                Box(mesh, side * .251f, 0, .056f, .022f, .37f, .028f, Surface.Metal);
                Box(mesh, 0, side * .183f, .056f, .49f, .02f, .028f, Surface.Accent);
                Box(mesh, side * .228f, -.159f, .074f, .008f, .008f, .01f, Surface.Metal);
                Box(mesh, side * .228f, .159f, .074f, .008f, .008f, .01f, Surface.Metal);
            }
            // Three physical mounting styles, not duplicated material files.
            int divisions = 3 + variant;
            for (int i = 0; i < divisions; i++)
                Box(mesh, -.17f + i * .34f / (divisions - 1), -.181f, .094f,
                    .023f, .015f, .056f, Surface.Metal);
        }

        private static void Box(MeshData mesh, float x, float y, float z,
            float width, float height, float depth, Surface material)
        {
            mesh.Box(new V3(x - width * .5f, y - height * .5f, z - depth * .5f),
                new V3(x + width * .5f, y + height * .5f, z + depth * .5f), material, .4f);
        }

        private static void Beam(MeshData mesh, float x0, float y0, float x1, float y1,
            float z, float radius, Surface material)
        {
            ZoneShapes.Beam(mesh, new V3(x0, y0, z), new V3(x1, y1, z), radius, material, .4f, 8);
        }

        private static void Ring(MeshData mesh, float x, float y, float radius, float z, Surface material)
        {
            const float thickness = .0025f;
            for (int i = 0; i < 16; i++)
            {
                double a = i * Math.PI / 8, b = (i + 1) * Math.PI / 8;
                var normalA = new V3((float)Math.Cos(a), (float)Math.Sin(a), 0);
                var normalB = new V3((float)Math.Cos(b), (float)Math.Sin(b), 0);
                var center = new V3(x, y, z);
                var outerA = center + normalA * (radius + thickness);
                var outerB = center + normalB * (radius + thickness);
                var innerA = center + normalA * (radius - thickness);
                var innerB = center + normalB * (radius - thickness);
                var back = new V3(0, 0, -thickness);
                mesh.Quad(outerA, outerB, innerB, innerA, new V3(0, 0, 1), material, .4f);
                mesh.Quad(outerA + back, outerB + back, outerB, outerA,
                    (normalA + normalB).Unit, material, .4f);
                mesh.Quad(innerB + back, innerA + back, innerA, innerB,
                    (normalA + normalB).Unit * -1, material, .4f);
            }
        }

        private static void Lanterns(MeshData mesh)
        {
            for (int i = 0; i < 9; i++)
            {
                float x = (i % 3 - 1) * .14f, y = (1 - i / 3) * .095f;
                Box(mesh, x, y, .072f, .075f, .065f, .026f, Surface.Metal);
                Box(mesh, x, y, .089f, .052f, .042f, .008f, Surface.Slide);
                if (i % 3 != 2) Beam(mesh, x + .04f, y, x + .1f, y, .063f, .003f, Surface.Metal);
                if (i < 6) Beam(mesh, x, y - .035f, x, y - .06f, .063f, .003f, Surface.Metal);
            }
        }

        private static void Channels(MeshData mesh, int variant)
        {
            for (int i = 0; i < 16; i++)
            {
                float x = -.165f + i % 4 * .11f, y = .108f - i / 4 * .072f;
                Box(mesh, x, y, .064f, .096f, .061f, .018f, Surface.Accent);
                Box(mesh, x, y, .08f, .014f, .014f, .012f, Surface.Metal);
            }
        }

        private static void Loom(MeshData mesh, int variant)
        {
            for (int i = 0; i < 12; i++)
            {
                float x = -.18f + i * .032f;
                Beam(mesh, x, -.13f, x, .13f, .067f, .0015f, Surface.Metal);
            }
            for (int i = 0; i < 8; i++)
            {
                float y = -.105f + i * .03f;
                Beam(mesh, -.19f, y, .19f, y, .071f, .0035f,
                    (i + variant) % 3 == 0 ? Surface.Slide : Surface.Accent);
            }
            Box(mesh, 0, -.147f, .082f, .42f, .014f, .035f, Surface.Accent);
        }

        private static void Herbarium(MeshData mesh, int variant)
        {
            for (int i = 0; i < 6; i++)
            {
                float x = (i % 3 - 1) * .14f, y = .07f - i / 3 * .14f;
                Box(mesh, x, y, .062f, .115f, .12f, .012f, Surface.Accent);
                Box(mesh, x, y, .071f, .101f, .105f, .006f, Surface.Plaster);
                Beam(mesh, x, y - .03f, x + .008f, y + .025f, .079f, .002f, Surface.Metal);
                for (int leaf = 0; leaf < 3; leaf++)
                {
                    float yy = y - .012f + leaf * .015f;
                    float sign = (leaf + variant) % 2 == 0 ? -1 : 1;
                    Beam(mesh, x, yy, x + sign * .022f, yy + .012f, .08f, .005f, Surface.Slide);
                }
            }
        }

        private static void Sundial(MeshData mesh)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * .15f;
                Ring(mesh, x, .012f, .062f, .075f, Surface.Metal);
                for (int tick = 0; tick < 12; tick++)
                {
                    double angle = tick * Math.PI / 6;
                    float a = (float)Math.Cos(angle), b = (float)Math.Sin(angle);
                    Beam(mesh, x + a * .048f, .012f + b * .048f,
                        x + a * .055f, .012f + b * .055f, .077f, .0015f, Surface.Metal);
                }
            }
        }

        private static void Balance(MeshData mesh, int variant)
        {
            Beam(mesh, 0, -.09f, 0, .1f, .084f, .009f, Surface.Metal);
            for (int i = 0; i < 6; i++)
                Box(mesh, -.17f + i * .068f, -.12f, .077f,
                    .038f, .025f + (i + variant) % 3 * .009f, .034f, Surface.Accent);
        }

        private static void Stars(MeshData mesh, int variant)
        {
            for (int i = 0; i < 9; i++)
            {
                var at = StarPosition(i, variant);
                Ring(mesh, at.X, at.Y, .009f + i % 3 * .002f, .08f, Surface.Metal);
            }
        }

        public static V2 StarPosition(int index, int variant)
        {
            return new V2(-.17f + index % 3 * .17f + (index / 3 % 2 == 0 ? -.008f : .008f),
                .1f - index / 3 * .1f + ((index + variant) % 2 == 0 ? .012f : -.012f));
        }

        private static void Bells(MeshData mesh, int variant)
        {
            Beam(mesh, -.21f, .13f, .21f, .13f, .078f, .006f, Surface.Accent);
            for (int i = 0; i < 8; i++)
            {
                float x = -.19f + i * .054f, bottom = -.11f + ((i + variant) % 5) * .018f;
                Beam(mesh, x, .13f, x, .085f, .077f, .0015f, Surface.Metal);
                Beam(mesh, x, .085f, x, bottom, .084f, .011f, Surface.Metal);
            }
        }

        private static void Reservoirs(MeshData mesh)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = -.174f + i * .116f;
                Box(mesh, x, 0, .067f, .084f, .24f, .02f, Surface.Metal);
                Box(mesh, x, 0, .082f, .062f, .21f, .012f, Surface.Slide);
                for (int tick = 0; tick < 6; tick++)
                    Box(mesh, x + .024f, -.085f + tick * .034f, .094f, .016f, .003f, .003f, Surface.Plaster);
            }
        }

        private static void Binding(MeshData mesh, int variant)
        {
            for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
            {
                float x = (col - 1) * .142f, y = .099f - row * .099f;
                Box(mesh, x, y, .065f, .126f, .081f, .018f, Surface.Accent);
                for (int page = 0; page < 3; page++)
                    Box(mesh, x + .008f, y - .025f + page * .023f, .078f,
                        .092f, .003f, .005f, (row + col + variant) % 2 == 0 ? Surface.Slide : Surface.Plaster);
            }
        }

        private static void Compasses(MeshData mesh)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * .105f, y = (i < 2 ? 1 : -1) * .077f;
                Ring(mesh, x, y, .056f, .075f, Surface.Metal);
            }
        }

        private static void Nursery(MeshData mesh, int variant)
        {
            for (int i = 0; i < 6; i++)
            {
                float x = (i % 3 - 1) * .144f, y = .067f - i / 3 * .14f;
                Box(mesh, x, y - .02f, .081f, .095f, .067f, .052f, Surface.Accent);
                Box(mesh, x, y + .016f, .082f, .104f, .012f, .058f, Surface.Metal);
                Beam(mesh, x, y + .02f, x, y + .051f, .087f, .002f, Surface.Metal);
                float sign = (i + variant) % 2 == 0 ? -1 : 1;
                Beam(mesh, x, y + .04f, x + sign * .026f, y + .058f, .087f, .006f, Surface.Slide);
            }
        }
    }
}
