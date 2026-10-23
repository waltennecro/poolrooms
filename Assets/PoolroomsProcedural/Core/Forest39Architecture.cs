using System;

namespace Poolrooms.Core
{
    public static class Forest39Architecture
    {
        public static RoomGeometry Build(Room room, LayoutConfig config)
        {
            var plan = room.Plan?.Forest39;
            if (plan == null) throw new ArgumentException("A Level 39 plan is required.");
            var result = new RoomGeometry();
            result.Structure.Theme = SurfaceTheme.Forest39Ground;
            result.Decoration.Theme = SurfaceTheme.Forest39Bark;
            result.CollisionOnly.Theme = SurfaceTheme.Forest39Bark;
            result.Structure.Append(Forest39Terrain.Build(plan));
            Forest39Terrain.Water(result, plan);
            foreach (var tree in plan.Trees) Forest39Oak.Collider(result.CollisionOnly, tree);
            var stones = new MeshData { Theme = SurfaceTheme.Forest39Stone };
            foreach (var stone in plan.Stones) Rock(stones, stone);
            result.VisualMeshes.Add(stones);
            result.CollisionOnly.Append(stones);
            Boundary(result, room, config, plan);
            Furnish(result, plan);
            Forest39Clearings.Append(result, plan);
            Understory(result, plan);
            Forest39Ecology.Append(result, plan);
            if (config.EncounterExploration)
            {
                Activities(result, plan);
                result.Life.Scenery = SceneryPlanner.Build(room, result, config);
            }
            result.Forest39Canopy = Forest39Oak.Build(plan);
            return result;
        }

        public static void Box(MeshData mesh, float x, float z, float width, float depth,
            float bottom, float top, Surface surface = Surface.Accent)
        {
            if (width <= 0 || depth <= 0 || top <= bottom) return;
            ZoneShapes.Box(mesh, x, z, width, depth, bottom, top, surface, 1);
        }

        static void Rock(MeshData mesh, Forest39Stone stone)
        {
            const int sides = 9;
            V3 top = stone.Foot + V3.Up * stone.Height;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Shapes.Pi * 2 / sides;
                float b = (i + 1) * Shapes.Pi * 2 / sides;
                float ra = .43f + .07f * (float)Math.Sin(i * 2.3f + stone.Seed % 17);
                float rb = .43f + .07f * (float)Math.Sin(((i + 1) % sides) * 2.3f + stone.Seed % 17);
                V3 p = stone.Foot + new V3((float)Math.Cos(a) * stone.Width * ra, 0,
                    (float)Math.Sin(a) * stone.Depth * ra);
                V3 q = stone.Foot + new V3((float)Math.Cos(b) * stone.Width * rb, 0,
                    (float)Math.Sin(b) * stone.Depth * rb);
                V3 shoulderP = p + new V3(0, stone.Height * .57f, 0);
                V3 shoulderQ = q + new V3(0, stone.Height * .57f, 0);
                V3 normal = V3.Cross(q - p, shoulderP - p).Unit;
                mesh.Quad(p, q, shoulderQ, shoulderP, normal, Surface.Tile, .8f);
                mesh.Triangle(top, shoulderP, shoulderQ, V3.Up, normal, normal,
                    new V2(.5f, .5f), new V2(0, 0), new V2(1, 0), Surface.Tile);
            }
        }

        static void Boundary(RoomGeometry result, Room room, LayoutConfig config, Forest39Plan plan)
        {
            float opening = config.DoorWidth * .5f + .2f;
            var wood = result.Decoration;
            var boundary = new MeshData { Theme = SurfaceTheme.Forest39Stone };
            for (int side = 0; side < 4; side++)
            {
                if (Forest39Planner.Same(config, room.X, room.Z, side)) continue;
                // Forest edges use a planted, sloping earth berm and a timber
                // threshold. No full-height square room walls or invisible exits.
                var local = new MeshData { Theme = SurfaceTheme.Forest39Stone };
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float start = sign < 0 ? -plan.Half : opening;
                    float end = sign < 0 ? -opening : plan.Half;
                    int segments = Math.Max(1, (int)((end - start) / 1.8f));
                    for (int i = 0; i < segments; i++)
                    {
                        float a = start + (end - start) * i / segments;
                        float b = start + (end - start) * (i + 1) / segments;
                        float y = 2.8f + .35f * (float)Math.Sin(i * .71f + plan.Seed % 5);
                        V3 p = new V3(a, 0, plan.Half - 1.35f);
                        V3 q = new V3(b, 0, plan.Half - 1.35f);
                        V3 r = new V3(b, y, plan.Half);
                        V3 s = new V3(a, y, plan.Half);
                        local.Quad(p, q, r, s, new V3(0, .4f, -1).Unit, Surface.Tile, 1.8f);
                        local.Quad(s, r, new V3(b, -.3f, plan.Half), new V3(a, -.3f, plan.Half),
                            new V3(0, 0, 1), Surface.Tile, 1.8f);
                    }
                }
                local.TranslateRotate(new V3(), side * Shapes.Pi * .5f);
                boundary.Append(local);
                if (!room.HasExit(side))
                {
                    var closure = new MeshData();
                    Box(closure, 0, plan.Half - .2f, opening * 2, .4f, 0, 2.8f);
                    closure.TranslateRotate(new V3(), side * Shapes.Pi * .5f);
                    boundary.Append(closure);
                    continue;
                }
                float x = Directions.DX[side] * (plan.Half - .35f);
                float z = Directions.DZ[side] * (plan.Half - .35f);
                float rightX = Directions.DZ[side], rightZ = -Directions.DX[side];
                foreach (int sign in new[] { -1, 1 })
                    Forest39Oak.Taper(wood, new V3(x + sign * rightX * opening, 0, z + sign * rightZ * opening),
                        new V3(x + sign * rightX * opening, config.DoorHeight + .5f, z + sign * rightZ * opening),
                        .15f, .11f, 8, plan.Seed);
                Forest39Oak.Taper(wood, new V3(x - rightX * opening, config.DoorHeight + .35f, z - rightZ * opening),
                    new V3(x + rightX * opening, config.DoorHeight + .35f, z + rightZ * opening), .13f, .13f, 8, plan.Seed);
            }
            result.VisualMeshes.Add(boundary);
            result.CollisionOnly.Append(boundary);
        }

        static void Furnish(RoomGeometry result, Forest39Plan plan)
        {
            var furniture = new RoomGeometry();
            furniture.Structure.Theme=SurfaceTheme.Forest39Bark;
            float x = plan.Rest.X, z = plan.Rest.Z;
            // This platform is flush with the level clearing. Props have exact
            // analytical support, independent from curved terrain approximation.
            // The ground itself supports the level clearing; no coplanar platform overlay.
            float benchZ = z - 1.1f;
            foreach (float xx in new[] { x - 1.5f, x + 1.5f })
                Box(furniture.Structure, xx, benchZ, .16f, .42f, 0, .43f);
            for (int slat = 0; slat < 4; slat++)
                Box(furniture.Structure, x, benchZ - .22f + slat * .145f, 3.5f, .12f, .43f, .5f);
            Box(furniture.Structure, x, benchZ - .34f, 3.5f, .08f, .5f, .95f);
            // One reused licensed table and one porcelain cup per clearing.
            // The source models are already physically dimensioned and credited.
            result.SourceProps.Add(new SourceFurniturePlacement(390000, 7, new V3(x, .004f, z + .5f), 0));
            result.SourceProps.Add(new SourceFurniturePlacement(390001, 11, Forest39Supplies.Cup(plan), 0));
            result.SourceProps.Add(new SourceFurniturePlacement(390002, 30, new V3(x + 1.65f, .004f, z + 1.2f), 0));
            if (plan.Clearing == Forest39Clearing.RainShelter || plan.Clearing == Forest39Clearing.ReadingGrove)
                Shelter(furniture, plan);
            result.Structure.Append(furniture.Structure);
            result.VisualMeshes.Add(furniture.Structure);
            result.VisualMeshes.AddRange(furniture.VisualMeshes);
            result.CollisionOnly.Append(furniture.CollisionOnly);
        }

        static void Shelter(RoomGeometry result, Forest39Plan plan)
        {
            float x = plan.Rest.X, z = plan.Rest.Z;
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                    Forest39Oak.Taper(result.Structure, new V3(x + sx * 2.1f, 0, z + sz * 1.65f),
                        new V3(x + sx * 2.1f, 2.7f, z + sz * 1.65f), .09f, .075f, 8, plan.Seed);
            var roof = new MeshData { Theme = SurfaceTheme.Forest39Bark };
            for (int sign = -1; sign <= 1; sign += 2)
            {
                var a = new V3(x - 2.4f, 3.3f, z);
                var b = new V3(x + 2.4f, 3.3f, z);
                var c = new V3(x + 2.4f, 2.7f, z + sign * 1.95f);
                var d = new V3(x - 2.4f, 2.7f, z + sign * 1.95f);
                roof.Quad(a, b, c, d, V3.Up, Surface.Accent, 1.2f);
                roof.Quad(a - V3.Up * .08f, b - V3.Up * .08f, c - V3.Up * .08f, d - V3.Up * .08f,
                    V3.Up * -1, Surface.Accent, 1.2f);
            }
            result.VisualMeshes.Add(roof);
            result.CollisionOnly.Append(roof);
        }

        static void Understory(RoomGeometry result, Forest39Plan plan)
        {
            var plants = new MeshData { Theme = SurfaceTheme.Forest39Leaves };
            var fallen = result.Decoration;
            foreach (var tree in plan.Trees)
            {
                for (int leaf = 0; leaf < 7; leaf++)
                {
                    float a = leaf * 2.399963f + tree.Yaw;
                    float x = tree.Foot.X + (float)Math.Cos(a) * (.7f + leaf * .09f);
                    float z = tree.Foot.Z + (float)Math.Sin(a) * (.7f + leaf * .09f);
                    if (Math.Abs(x) > plan.Half - .4f || Math.Abs(z) > plan.Half - .4f) continue;
                    float y = Forest39Terrain.MeshHeight(plan, x, z) + .012f;
                    V3 center = new V3(x, y, z);
                    for (int frond = 0; frond < 5; frond++)
                    {
                        float angle = frond * Shapes.Pi * .4f + a;
                        V3 end = center + new V3((float)Math.Cos(angle) * .38f, .32f, (float)Math.Sin(angle) * .38f);
                        V3 side = new V3(-(float)Math.Sin(angle) * .08f, 0, (float)Math.Cos(angle) * .08f);
                        plants.Triangle(center, end - side, end + side, V3.Up, V3.Up, V3.Up,
                            new V2(.5f, 0), new V2(0, 1), new V2(1, 1), Surface.Tile);
                    }
                }
            }
            if (plan.Clearing == Forest39Clearing.FallenOak)
            {
                // A fallen limb beside the path, never across an exit.
                float x = plan.Half * .57f, z = -plan.Half * .55f;
                V3 a = new V3(x - 1.2f, Forest39Terrain.MeshHeight(plan, x - 1.2f, z) + .18f, z);
                V3 b = new V3(x + 1.2f, Forest39Terrain.MeshHeight(plan, x + 1.2f, z) + .18f, z);
                Forest39Oak.Taper(fallen, a, b, .22f, .19f, 12, plan.Seed);
            }
            result.VisualMeshes.Add(plants);
        }

        static void Activities(RoomGeometry result, Forest39Plan plan)
        {
            var life = new RoomLifePlan
            {
                Vignette = new RoomVignette("Nivel 39 · " + Forest39Stories.Title(plan.Clearing),
                    Forest39Stories.Note(plan.Clearing), RoomActivity.Rest, (int)(plan.Seed % 144))
            };
            float x = plan.Rest.X + 1.45f, z = plan.Rest.Z - .55f;
            life.Notice = Panel(result, x, z, .85f);
            life.Stations.Add(new StationPlacement(Panel(result, x, z + 1.1f, .85f),
                plan.Clearing == Forest39Clearing.Herbarium ? StationKind.Herbarium :
                plan.Clearing == Forest39Clearing.ListeningGrove ? StationKind.Chimes : StationKind.Sketch,
                390100, (int)plan.Clearing % 3));
            result.Life = life;
        }

        static DressingAnchor Panel(RoomGeometry result, float x, float z, float y)
        {
            var panel = new MeshData { Theme = SurfaceTheme.Forest39Bark };
            Box(panel, x, z, .09f, .09f, 0, .75f);
            Box(panel, x, z, 1.4f, .1f, .75f, 1.9f);
            result.Structure.Append(panel);
            result.VisualMeshes.Add(panel);
            var a = new V3(x - .68f, .78f, z + .052f);
            var b = new V3(x + .68f, .78f, z + .052f);
            var c = new V3(x - .68f, 1.87f, z + .052f);
            return new DressingAnchor(new V3(x - .36f, 1.15f, z + .052f),
                new V3(0, 0, 1), a, b, c, 0, 0);
        }
    }
}
