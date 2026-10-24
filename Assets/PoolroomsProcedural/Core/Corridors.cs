using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public sealed class CorridorGeometry
    {
        public readonly MeshData Structure = new MeshData();
        public readonly List<Fixture> Fixtures = new List<Fixture>();
        public bool Natural;
        public NaturalPassage Passage;
        public readonly List<MeshData> Finishes=new List<MeshData>();
        public readonly MeshData TransitionGlass=new MeshData{Theme=SurfaceTheme.Glass};
        public V3 ShutterPosition;
        public float ShutterYaw;
        public CorridorStyle Style;
        public SolarConnectionKind SolarConnection;
    }

    // Canonical north-facing coordinates, relative to the owner room. East links rotate once at the end.
    public sealed class CorridorPath
    {
        public readonly CorridorStyle Style;
        public readonly float Start, End, HalfWidth, Collar, Bend;
        public readonly int RevealEnd;
        public float Length { get { return End - Start; } }
        public CorridorPath(CorridorStyle style, float start, float end, float halfWidth, float bend, float thickness, int revealEnd = 0)
        {
            RevealEnd = revealEnd;
            Style = style; Start = start; End = end; HalfWidth = halfWidth;
            Collar = Math.Min(1.1f, Length * .18f);
            float span = Length - 2 * Collar;
            // Curvature * outer half-width stays below 1, so offset walls cannot fold over themselves.
            float divisor = style == CorridorStyle.Serpentine ? 90 : 40;
            Bend = Math.Sign(bend) * Math.Min(Math.Abs(bend) * 3.2f, span * span / (divisor * (halfWidth + thickness)));
        }

        public void Sample(float progress, out V3 centre, out V3 right, out float halfWidth)
        {
            float z = Start + Length * Math.Max(0, Math.Min(1, progress));
            float span = Length - 2 * Collar, u = (z - Start - Collar) / span;
            float offset = 0, slope = 0; halfWidth = HalfWidth;
            if (u > 0 && u < 1)
            {
                float p = Shapes.Pi, wave = Shapes.Sin(p * u), blend = wave * wave;
                if (Style == CorridorStyle.Curved)
                {
                    offset = Bend * blend; slope = Bend * p * Shapes.Sin(2 * p * u) / span;
                }
                else if (Style == CorridorStyle.Serpentine)
                {
                    offset = Bend * (.5f * Shapes.Sin(2 * p * u) - .25f * Shapes.Sin(4 * p * u));
                    slope = Bend * p * (Shapes.Cos(2 * p * u) - Shapes.Cos(4 * p * u)) / span;
                }
                if (Style == CorridorStyle.WideGallery) halfWidth += 1.6f * blend;
                if (RevealEnd != 0)
                {
                    float travel = RevealEnd > 0 ? u : 1 - u;
                    // A contained approach followed by a wider landing; exact doorway collars remain unchanged.
                    halfWidth *= 1 + blend * (-.32f + .68f * SpaceComposition.Smooth(travel));
                }
                else if (Style == CorridorStyle.Narrow) halfWidth *= 1 - .28f * blend;
            }
            centre = new V3(offset, 0, z); right = new V3(1, 0, -slope).Unit;
        }
    }

    public static class Corridors
    {
        public static CorridorStyle ResolveStyle(Room from, Room to, int direction, LayoutConfig c)
        {
            if (c.Corridors != CorridorStyle.Mixed) return c.Corridors;
            var random = new StableRandom((c.Zones==ZoneMode.Random||c.Continuous||c.Procedural!=LiminalGeneration.Authored)
                ?StableRandom.Hash(c.Seed,from.X,from.Z,8917u+(uint)direction)
                :StableRandom.Hash(c.Seed, from.Id, to.Id, 8917u + (uint)direction));
            bool compose = c.Composition != SpatialComposition.None;
            float preferred = 0;
            if (compose) for (int i = 0; i < c.CorridorWeights.Length; i++)
                if (SpaceComposition.IsPreferred((CorridorStyle)(i+1), from, to, c)) preferred += c.CorridorWeights[i];
            float total = preferred;
            if (total <= 0) { compose = false; foreach (float weight in c.CorridorWeights) total += weight; }
            float target = random.Value() * total;
            for (int i = 0; i < c.CorridorWeights.Length; i++)
            {
                if (compose && !SpaceComposition.IsPreferred((CorridorStyle)(i+1), from, to, c)) continue;
                target -= c.CorridorWeights[i];
                if (target < 0) return (CorridorStyle)(i + 1);
            }
            for(int i=c.CorridorWeights.Length-1;i>=0;i--)
                if(c.CorridorWeights[i]>0&&(!compose||SpaceComposition.IsPreferred((CorridorStyle)(i+1),from,to,c)))
                    return (CorridorStyle)(i+1);
            throw new InvalidOperationException("No enabled corridor style.");
        }

        public static CorridorPath Describe(Room from, Room to, int direction, LayoutConfig c)
        {
            if (direction != 0 && direction != 1) throw new ArgumentOutOfRangeException(nameof(direction), "Only north/east links have an owner mesh.");
            if (from == null || to == null) throw new ArgumentNullException("Corridor endpoints must exist.");
            float start = (direction == 0 ? from.Depth : from.Width) * .5f;
            float end = c.CellSize - (direction == 0 ? to.Depth : to.Width) * .5f;
            uint bendSeed=(c.Zones==ZoneMode.Random||c.Continuous||c.Procedural!=LiminalGeneration.Authored)?StableRandom.Hash(c.Seed,from.X,from.Z,691u+(uint)direction):StableRandom.Hash(c.Seed, from.Id, to.Id, 691u);
            float sign = (bendSeed & 1) == 0 ? 1 : -1;
            CorridorStyle style = ResolveStyle(from, to, direction, c);
            int reveal = style == CorridorStyle.Narrow || style == CorridorStyle.Straight ? SpaceComposition.RevealEnd(from, to, c) : 0;
            return new CorridorPath(style, start, end, c.DoorWidth * .5f, sign * c.CorridorBend, c.WallThickness, reveal);
        }

        public static CorridorGeometry Build(Room from, Room to, int direction, LayoutConfig c)
        {
            if(from?.Plan?.Level10!=null&&to?.Plan?.Level10!=null)return new CorridorGeometry{Style=CorridorStyle.WideGallery};
            if(from?.Plan?.Forest39!=null&&to?.Plan?.Forest39!=null)return new CorridorGeometry{Style=CorridorStyle.WideGallery};
            if(from?.Plan?.Quiet?.Sky63!=null&&to?.Plan?.Quiet?.Sky63!=null)return new CorridorGeometry{Style=CorridorStyle.WideGallery};
            if(from?.Plan?.Quiet?.Conservatory!=null&&to?.Plan?.Quiet?.Conservatory!=null)return new CorridorGeometry{Style=CorridorStyle.WideGallery};
            if(from?.Plan?.Quiet?.Archive!=null&&to?.Plan?.Quiet?.Archive!=null)return new CorridorGeometry{Style=CorridorStyle.WideGallery};
            if(from?.Plan?.Quiet?.Courtyard!=null&&to?.Plan?.Quiet?.Courtyard!=null)return new CorridorGeometry{Style=CorridorStyle.WideGallery};
            // Full-cell floors meet exactly. An extra overlapping strip causes z-fighting.
            if(from?.Plan?.Restful?.Large==true&&to?.Plan?.Restful?.Large==true&&LargeRegions.OpenEdge(c,from.X,from.Z,direction,from.Environment))return new CorridorGeometry{Style=CorridorStyle.WideGallery};
            if(NaturalTransitions.Applies(from,to,c)){var natural=NaturalTransitions.Build(from,to,direction,c);if(natural!=null)return natural;}
            if(from!=null&&to!=null&&(from.Plan!=null&&(from.Plan.Restful!=null||from.Plan.Rain!=null||from.Plan.Quiet!=null||from.Plan.Garden!=null||from.Plan.Baths!=null||from.Plan.Aquatic!=null)||to.Plan!=null&&(to.Plan.Restful!=null||to.Plan.Rain!=null||to.Plan.Quiet!=null||to.Plan.Garden!=null||to.Plan.Baths!=null||to.Plan.Aquatic!=null))&&
                c.CellSize-(direction==0?from.Depth+to.Depth:from.Width+to.Width)*.5f<=.01f)
                return QuietLevelArchitecture.Join(from.Plan!=null&&(from.Plan.Restful!=null||from.Plan.Rain!=null||from.Plan.Quiet!=null||from.Plan.Garden!=null||from.Plan.Baths!=null||from.Plan.Aquatic!=null)?from:to,direction,c);
            if(from!=null&&to!=null&&(from.Plan!=null&&from.Plan.City!=null||to.Plan!=null&&to.Plan.City!=null)&&
                c.CellSize-(direction==0?from.Depth+to.Depth:from.Width+to.Width)*.5f<=.01f)return City11Architecture.Join(from,direction,c);
            if(from!=null&&to!=null&&from.Plan!=null&&to.Plan!=null&&from.Plan.SoftPlay!=null&&to.Plan.SoftPlay!=null)return SoftPlayArchitecture.Join(from,to,direction,c);
            // A solar room can also occupy its full cell on this axis. Do not feed a zero-length
            // connector into curved corridor sampling when it shares a wall with a play hall.
            if(from!=null&&to!=null&&(from.Plan!=null&&from.Plan.SoftPlay!=null||to.Plan!=null&&to.Plan.SoftPlay!=null)&&
                c.CellSize-(direction==0?from.Depth+to.Depth:from.Width+to.Width)*.5f<=.01f)
                return SoftPlayArchitecture.Join(from,to,direction,c);
            if(SolarConnections.Uses(from,to,c))return SolarConnections.Build(from,to,direction,c);
            CorridorPath path = Describe(from, to, direction, c);
            var output = new CorridorGeometry { Style = path.Style };
            MeshData mesh = output.Structure;
            if(c.DiscoveryCoverage)mesh.Theme=BiomeSurfaces.ForEnvironment(from.Environment);
            float uv = c.TextureWorldSize, thickness = c.WallThickness;
            int steps = Math.Max(c.CurveSegments, (int)Math.Ceiling(path.Length / .65f));
            V3[] left, right, outerLeft, outerRight;
            Edges(path, steps, 0, 1, thickness, out left, out right, out outerLeft, out outerRight);
            // Keep the dry central deck at zero, but meet each room's actual floor with a gentle
            // threshold ramp. Shallow rooms have a floor at -0.26 m, not at the corridor's datum.
            var floorHeights = new float[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float distance = path.Length * i / steps;
                floorHeights[i] = from.Floor * Math.Max(0, 1 - distance / path.Collar)
                    + to.Floor * Math.Max(0, 1 - (path.Length - distance) / path.Collar);
            }
            Strip(mesh, left, right, Math.Min(from.Floor, to.Floor) - .3f, 0, Surface.Tile, uv, floorHeights);

            // Floors meet room floors at the wall centre. Shells begin at the wall's outer face,
            // avoiding duplicate coplanar portal surfaces and keeping a straight doorway collar.
            float inset = thickness * .5f / path.Length;
            Edges(path, steps, inset, 1 - inset, thickness, out left, out right, out outerLeft, out outerRight);
            float wallTop = path.Style == CorridorStyle.BarrelVault ? c.DoorHeight - path.HalfWidth : c.DoorHeight;
            Strip(mesh, left, outerLeft, -.3f, wallTop, Surface.Tile, uv);
            Strip(mesh, right, outerRight, -.3f, wallTop, Surface.Tile, uv);
            if (path.Style == CorridorStyle.BarrelVault)
                Vault(mesh, path.Start + thickness * .5f, path.End - thickness * .5f, path.HalfWidth, c);
            else Strip(mesh, left, right, c.DoorHeight, c.DoorHeight + .25f, Surface.Plaster, uv);

            if (path.RevealEnd != 0)
            {
                // Two real arch frames reveal the destination; no objects, lamps or surfaces added per frame.
                foreach (float progress in new[] { .30f, .70f })
                {
                    V3 centre, side; float half; path.Sample(progress, out centre, out side, out half);
                    ZoneShapes.Portal(mesh, 0, centre.Z, 0, half*2 + thickness*2, c.DoorHeight, 0,
                        half*2 - .18f, c.DoorHeight - .15f, .18f, 1, Math.Max(12,c.CurveSegments/2), uv);
                }
            }
            if(c.Composition!=SpatialComposition.None&&path.Style==CorridorStyle.WideGallery&&
                (from.Role==SpatialRole.Rest||to.Role==SpatialRole.Rest))
            {
                // Rest seats in the wide landing, outside the straight walking/sight corridor.
                // Size against the actual width at BOTH ends, even in the shortest connector.
                float length=Math.Min(2.2f,path.Length*.24f),half;
                V3 centre,side;path.Sample(.5f-length*.5f/path.Length,out centre,out side,out half);
                float lateral=half-.38f;
                for(int sign=-1;sign<=1;sign+=2)
                {
                    ZoneShapes.Box(mesh,sign*lateral,(path.Start+path.End)*.5f,.5f,length,0,.42f,Surface.Tile,uv);
                    ZoneShapes.Box(mesh,sign*lateral,(path.Start+path.End)*.5f,.52f,length+.02f,.42f,.48f,Surface.Accent,uv);
                }
            }
            int repeats = Math.Max(1, (int)(path.Length / 3.8f));
            for (int i = 0; i < repeats; i++)
            {
                float z = path.Start + path.Length * (i + 1f) / (repeats + 1f);
                if (path.Style == CorridorStyle.Arcade)
                {
                    var arch = new MeshData();
                    Shapes.Portal(arch, c.DoorWidth + 2 * thickness, c.DoorHeight, 0, c.DoorWidth - .5f,
                        c.DoorHeight - .18f, .3f, Math.Max(12, c.CurveSegments / 2), uv);
                    arch.TranslateRotate(new V3(0, 0, z), 0); mesh.Append(arch);
                }
                else if (path.Style == CorridorStyle.Colonnade)
                {
                    float radius = Math.Min(.32f, path.HalfWidth * .14f);
                    for (int side = -1; side <= 1; side += 2)
                        Shapes.Column(mesh, side * (path.HalfWidth - radius - .08f), z, radius, 0,
                            c.DoorHeight, Math.Max(12, c.CurveSegments / 2), Surface.Tile, uv);
                }
                else if(path.Style==CorridorStyle.Coffered)
                    ZoneShapes.Box(mesh,0,z,c.DoorWidth,.3f,c.DoorHeight-.35f,c.DoorHeight,Surface.Accent,uv);
                else if(path.Style==CorridorStyle.PortalSequence)
                    ZoneShapes.Portal(mesh,0,z,0,c.DoorWidth+2*thickness,c.DoorHeight,0,c.DoorWidth-.5f,
                        c.DoorHeight-.15f,.26f,i%4,c.CurveSegments,uv);
                else if(path.Style==CorridorStyle.OffsetScreens)
                {
                    float side=i%2==0?-1:1,depth=Math.Min(.5f,path.Length*.08f),width=path.HalfWidth*.25f;
                    ZoneShapes.Box(mesh,side*(path.HalfWidth-width*.5f),z,width,depth,0,c.DoorHeight,Surface.Tile,uv);
                }
                else if(path.Style==CorridorStyle.LightSlots)
                    ZoneShapes.Box(mesh,0,z,c.DoorWidth*.8f,.12f,c.DoorHeight-.055f,c.DoorHeight-.025f,Surface.Glow,uv);
                else if(path.Style==CorridorStyle.DoubleColonnade)
                    for(int side=-1;side<=1;side+=2)for(int pair=-1;pair<=1;pair+=2)
                        Shapes.Column(mesh,side*(path.HalfWidth-.23f),z+pair*.33f,.14f,0,c.DoorHeight,
                            Math.Max(12,c.CurveSegments/2),Surface.Tile,uv);
            }
            if(path.Style==CorridorStyle.PipeGallery)
                for(int side=-1;side<=1;side+=2)
                    ZoneShapes.Beam(mesh,new V3(side*(path.HalfWidth-.22f),c.DoorHeight-.6f,path.Start+thickness),
                        new V3(side*(path.HalfWidth-.22f),c.DoorHeight-.6f,path.End-thickness),.12f,Surface.Metal,uv,12);

            int count = Math.Max(1, (int)Math.Ceiling(path.Length / c.CorridorLightSpacing));
            for (int i = 0; i < count; i++)
            {
                V3 centre, side; float width;
                path.Sample((i + .5f) / count, out centre, out side, out width);
                float lightWidth = Math.Min(1.2f, c.DoorWidth * .3f);
                V3 position = centre + V3.Up * (c.DoorHeight - .45f);
                mesh.Box(position + new V3(-lightWidth * .5f, .04f, -.12f),
                    position + new V3(lightWidth * .5f, .08f, .12f), Surface.Glow, uv);
                output.Fixtures.Add(new Fixture { Position = position, Width = lightWidth, Depth = .24f,
                    Power = 1100, Temperature = path.Style == CorridorStyle.BarrelVault ? 6200 : 4800 });
            }
            if (direction == 1)
            {
                mesh.TranslateRotate(new V3(), Shapes.Pi * .5f);
                for (int i = 0; i < output.Fixtures.Count; i++)
                {
                    Fixture f = output.Fixtures[i]; f.Position = East(f.Position);
                    float width = f.Width; f.Width = f.Depth; f.Depth = width;
                    output.Fixtures[i] = f;
                }
            }
            if(c.Procedural!=LiminalGeneration.Authored)
                mesh.PlayPalette=((from.Environment==LiminalEnvironment.IndoorPlay?1:0)+(to.Environment==LiminalEnvironment.IndoorPlay?1:0))*.5f;
            mesh.RetailPalette=((from.Environment==LiminalEnvironment.Supermarket?1:0)+(to.Environment==LiminalEnvironment.Supermarket?1:0))*.5f;
            if(from.Plan!=null&&from.Plan.Interior!=null||to.Plan!=null&&to.Plan.Interior!=null)
            {
                mesh.RetailPalette=((from.Environment==LiminalEnvironment.Supermarket||InteriorPlanner.IsInterior(from.Environment)?1:0)+
                    (to.Environment==LiminalEnvironment.Supermarket||InteriorPlanner.IsInterior(to.Environment)?1:0))*.5f;
                mesh.PaletteVariant=from.Plan!=null&&from.Plan.Interior!=null?from.Plan.Palette:to.Plan.Palette;
            }
            ZoneLighting.Corridor(output,from,to,c);
            return output;
        }

        public static V3 East(V3 point) { return new V3(point.Z, point.Y, -point.X); }

        private static void Edges(CorridorPath path, int steps, float start, float end, float thickness,
            out V3[] left, out V3[] right, out V3[] outerLeft, out V3[] outerRight)
        {
            left = new V3[steps + 1]; right = new V3[steps + 1];
            outerLeft = new V3[steps + 1]; outerRight = new V3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                V3 centre, side; float half;
                path.Sample(start + (end - start) * i / steps, out centre, out side, out half);
                left[i] = centre - side * half; right[i] = centre + side * half;
                outerLeft[i] = centre - side * (half + thickness); outerRight[i] = centre + side * (half + thickness);
            }
        }

        // One continuous solid extrusion, with no internal segment caps. Side UVs follow real edge length.
        private static void Strip(MeshData mesh, V3[] a, V3[] b, float bottom, float top, Surface surface, float uv,
            float[] topHeights = null)
        {
            for (int i = 0; i < a.Length - 1; i++)
            {
                int j = i + 1;
                float y0 = topHeights != null ? topHeights[i] : top;
                float y1 = topHeights != null ? topHeights[j] : top;
                V3 p = a[i] + V3.Up * y0, q = b[i] + V3.Up * y0;
                V3 r = b[j] + V3.Up * y1, s = a[j] + V3.Up * y1;
                // Sloping thresholds need geometric normals for PBR shading and collision tests.
                TopTriangle(mesh, p, q, r, surface, uv);
                TopTriangle(mesh, p, r, s, surface, uv);
                Shapes.Horizontal(mesh, a[i] + V3.Up * bottom, b[i] + V3.Up * bottom,
                    b[j] + V3.Up * bottom, a[j] + V3.Up * bottom, surface, uv, false);
            }
            VerticalEdge(mesh, a, b, bottom, top, surface, uv, topHeights);
            VerticalEdge(mesh, b, a, bottom, top, surface, uv, topHeights);
            int last = a.Length - 1;
            V3 firstNormal = ((a[0] + b[0]) - (a[1] + b[1])).Unit;
            V3 lastNormal = ((a[last] + b[last]) - (a[last - 1] + b[last - 1])).Unit;
            float firstTop = topHeights != null ? topHeights[0] : top;
            float lastTop = topHeights != null ? topHeights[last] : top;
            mesh.Quad(a[0] + V3.Up * bottom, b[0] + V3.Up * bottom, b[0] + V3.Up * firstTop, a[0] + V3.Up * firstTop, firstNormal, surface, uv);
            mesh.Quad(a[last] + V3.Up * bottom, b[last] + V3.Up * bottom, b[last] + V3.Up * lastTop, a[last] + V3.Up * lastTop, lastNormal, surface, uv);
        }

        private static void TopTriangle(MeshData mesh, V3 a, V3 b, V3 c, Surface surface, float uv)
        {
            V3 normal = V3.Cross(b - a, c - a).Unit;
            if (normal.Y < 0) normal = normal * -1;
            mesh.Triangle(a, b, c, normal, normal, normal,
                new V2(a.X / uv, a.Z / uv), new V2(b.X / uv, b.Z / uv), new V2(c.X / uv, c.Z / uv), surface);
        }

        private static void VerticalEdge(MeshData mesh, V3[] edge, V3[] opposite, float bottom, float top, Surface surface, float uv,
            float[] topHeights = null)
        {
            var normals = new V3[edge.Length]; var distance = new float[edge.Length];
            for (int i = 0; i < edge.Length; i++)
            {
                V3 tangent = edge[Math.Min(i + 1, edge.Length - 1)] - edge[Math.Max(i - 1, 0)];
                V3 normal = V3.Cross(tangent, V3.Up).Unit;
                normals[i] = V3.Dot(normal, edge[i] - opposite[i]) < 0 ? normal * -1 : normal;
                if (i > 0) distance[i] = distance[i - 1] + (edge[i] - edge[i - 1]).Length;
            }
            for (int i = 0; i < edge.Length - 1; i++)
            {
                int j = i + 1;
                float y0 = topHeights != null ? topHeights[i] : top;
                float y1 = topHeights != null ? topHeights[j] : top;
                mesh.QuadUV(edge[i] + V3.Up * bottom, edge[j] + V3.Up * bottom, edge[j] + V3.Up * y1, edge[i] + V3.Up * y0,
                    normals[i], normals[j], normals[j], normals[i], new V2(distance[i] / uv, bottom / uv),
                    new V2(distance[j] / uv, bottom / uv), new V2(distance[j] / uv, y1 / uv), new V2(distance[i] / uv, y0 / uv), surface);
            }
        }

        private static void Vault(MeshData mesh, float start, float end, float radius, LayoutConfig c)
        {
            int segments = Math.Max(12, c.CurveSegments);
            float spring = c.DoorHeight - radius, uv = c.TextureWorldSize;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Shapes.Pi / segments, b = (i + 1) * Shapes.Pi / segments;
                for (int shell = 0; shell < 2; shell++)
                {
                    float r = radius + shell * c.WallThickness, sign = shell == 0 ? -1 : 1;
                    V3 na = new V3(Shapes.Cos(a), Shapes.Sin(a), 0) * sign, nb = new V3(Shapes.Cos(b), Shapes.Sin(b), 0) * sign;
                    mesh.QuadUV(Arch(a, r, spring, start), Arch(b, r, spring, start), Arch(b, r, spring, end), Arch(a, r, spring, end),
                        na, nb, nb, na, new V2(a * r / uv, start / uv), new V2(b * r / uv, start / uv),
                        new V2(b * r / uv, end / uv), new V2(a * r / uv, end / uv), Surface.Tile);
                }
                for (int side = 0; side < 2; side++)
                {
                    float z = side == 0 ? start : end, outer = radius + c.WallThickness;
                    mesh.Quad(Arch(a, radius, spring, z), Arch(b, radius, spring, z), Arch(b, outer, spring, z), Arch(a, outer, spring, z),
                        new V3(0, 0, side == 0 ? -1 : 1), Surface.Tile, uv);
                }
            }
        }

        private static V3 Arch(float angle, float radius, float spring, float z)
        { return new V3(Shapes.Cos(angle) * radius, spring + Shapes.Sin(angle) * radius, z); }
    }
}
