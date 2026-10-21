using System;

namespace Poolrooms.Core
{
    public static class WorkshopStateGeometry
    {
        private static readonly Surface[] yarn = { Surface.Plaster, Surface.Slide, Surface.Metal, Surface.Accent };

        public static MeshData Build(StationKind kind, WorkshopBoard board, int variant)
        {
            if (!WorkshopCatalog.IsWorkshop(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (board == null) throw new ArgumentNullException(nameof(board));
            var mesh = new MeshData { Theme = SurfaceTheme.EncounterFurniture, EmissionStrength = 0 };
            switch (kind)
            {
                case StationKind.LanternCircuit:
                    for (int i = 0; i < 9; i++)
                    {
                        float x = (i % 3 - 1) * .14f, y = (1 - i / 3) * .095f;
                        Plate(mesh, x, y, .049f, .039f, board.Value(i) == 1 ? Surface.Plaster : Surface.Metal);
                    }
                    break;
                case StationKind.WaterChannels:
                    var channels = (ChannelBoard)board;
                    for (int i = 0; i < 16; i++)
                    {
                        float x = -.165f + i % 4 * .11f, y = .108f - i / 4 * .072f;
                        int mask = channels.Mask(i);
                        for (int direction = 0; direction < 4; direction++)
                        {
                            if ((mask & (1 << direction)) == 0) continue;
                            float dx = direction == 1 ? .05f : direction == 3 ? -.05f : 0;
                            float dy = direction == 0 ? .031f : direction == 2 ? -.031f : 0;
                            Line(mesh, x, y, x + dx, y + dy, .009f, Surface.Metal);
                        }
                    }
                    break;
                case StationKind.Weaving:
                    for (int i = 0; i < 16; i++)
                        Plate(mesh, -.147f + i % 4 * .098f, .105f - i / 4 * .07f,
                            .086f, .055f, yarn[board.Value(i)]);
                    break;
                case StationKind.Herbarium:
                    for (int i = 0; i < 6; i++)
                    {
                        float x = (i % 3 - 1) * .14f, y = .07f - i / 3 * .14f;
                        for (int mark = 0; mark <= board.Value(i); mark++)
                            Plate(mesh, x - .035f + mark * .013f, y - .041f, .007f, .01f, Surface.Metal);
                    }
                    break;
                case StationKind.Sundial:
                    for (int i = 0; i < 3; i++)
                        Pointer(mesh, (i - 1) * .15f, .012f, board.Value(i) * Math.PI / 6, .042f);
                    break;
                case StationKind.ParcelBalance:
                    var balance = (BalanceBoard)board;
                    for (int i = 0; i < 6; i++)
                    {
                        float x = -.17f + i * .068f;
                        Plate(mesh, x, -.12f, .029f, .018f, board.Value(i) == 1 ? Surface.Slide : Surface.Metal);
                    }
                    float tilt = Math.Max(-1, Math.Min(1, (balance.CurrentWeight - balance.DesiredWeight) / 12f));
                    Line(mesh, -.16f, .07f + tilt * .024f, .16f, .07f - tilt * .024f, .004f, Surface.Slide);
                    foreach (int side in new[] { -1, 1 })
                    {
                        float drop = -side * tilt * .024f;
                        Line(mesh, side * .15f, .07f + drop, side * .15f, -.025f + drop, .002f, Surface.Metal);
                        Plate(mesh, side * .15f, -.032f + drop, .13f, .009f, Surface.Metal);
                    }
                    break;
                case StationKind.StarChart:
                    V2 previous = default;
                    bool havePrevious = false;
                    for (int order = 1; order <= 9; order++)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            if (board.Target(i) != order || board.Value(i) == 0) continue;
                            V2 point = WorkshopModels.StarPosition(i, variant);
                            if (havePrevious) Line(mesh, previous.X, previous.Y, point.X, point.Y, .002f, Surface.Slide);
                            Plate(mesh, point.X, point.Y, .009f, .009f, Surface.Plaster);
                            previous = point;
                            havePrevious = true;
                        }
                    }
                    break;
                case StationKind.BellSequence:
                    int entered = ((BellBoard)board).Entered;
                    for (int i = 0; i < 6; i++)
                        Plate(mesh, -.15f + i * .06f, -.145f, .035f, .01f, i < entered ? Surface.Slide : Surface.Metal);
                    break;
                case StationKind.TideLocks:
                    for (int i = 0; i < 4; i++)
                    {
                        float x = -.174f + i * .116f;
                        float height = .19f * board.Value(i) / TideBoard.Maximum;
                        if (height > 0) Plate(mesh, x, -.1f + height * .5f, .047f, height, Surface.Slide);
                        Plate(mesh, x, -.1f + .19f * board.Target(i) / TideBoard.Maximum, .062f, .003f, Surface.Plaster);
                    }
                    break;
                case StationKind.Bookbinding:
                    for (int i = 0; i < 9; i++)
                        Plate(mesh, (i % 3 - 1) * .142f, .099f - i / 3 * .099f,
                            .09f, .055f, yarn[board.Value(i)]);
                    break;
                case StationKind.WindRose:
                    for (int i = 0; i < 4; i++)
                        Pointer(mesh, (i % 2 == 0 ? -1 : 1) * .105f,
                            (i < 2 ? 1 : -1) * .077f, board.Value(i) * Math.PI / 2, .041f);
                    break;
                case StationKind.SeedNursery:
                    for (int i = 0; i < 6; i++)
                    {
                        float x = (i % 3 - 1) * .144f, y = .067f - i / 3 * .14f;
                        float height = .04f * board.Value(i) / NurseryBoard.Maximum;
                        if (height > 0) Plate(mesh, x + .033f, y - .048f + height * .5f, .008f, height, Surface.Slide);
                        Plate(mesh, x + .033f, y - .048f + .04f * board.Target(i) / NurseryBoard.Maximum,
                            .014f, .002f, Surface.Plaster);
                    }
                    break;
            }
            // A physical status tag uses reflected ambient light rather than
            // adding an emissive light or a directional sun to every room.
            Plate(mesh, .205f, -.155f, .026f, .012f, board.Complete ? Surface.Slide : Surface.Metal);
            return mesh;
        }

        private static void Pointer(MeshData mesh, float x, float y, double angle, float radius)
        {
            float dx = (float)Math.Sin(angle), dy = (float)Math.Cos(angle);
            Line(mesh, x - dx * .01f, y - dy * .01f, x + dx * radius, y + dy * radius, .003f, Surface.Slide);
            float endX = x + dx * radius, endY = y + dy * radius;
            Line(mesh, endX, endY, endX - dx * .012f - dy * .007f,
                endY - dy * .012f + dx * .007f, .002f, Surface.Slide);
            Line(mesh, endX, endY, endX - dx * .012f + dy * .007f,
                endY - dy * .012f - dx * .007f, .002f, Surface.Slide);
        }

        private static void Plate(MeshData mesh, float x, float y, float width, float height, Surface surface)
        {
            mesh.Box(new V3(x - width * .5f, y - height * .5f, .114f),
                new V3(x + width * .5f, y + height * .5f, .12f), surface, .4f);
        }

        private static void Line(MeshData mesh, float x0, float y0, float x1, float y1, float radius, Surface surface)
        {
            ZoneShapes.Beam(mesh, new V3(x0, y0, .117f), new V3(x1, y1, .117f), radius, surface, .4f, 8);
        }
    }
}
