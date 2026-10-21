using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class SceneryPlanner
    {
        public static SceneryPlan Build(Room room, RoomGeometry geometry, LayoutConfig config)
        {
            var plan = new SceneryPlan();
            if (!SceneryWorkBudget.Eligible(room, config)) return plan;
            if (geometry == null || !ScenerySafety.AllWaterValid(geometry.Regions))
            {
                plan.InvalidInput = true;
                return plan;
            }
            var fuel = new SceneryWorkBudget();
            var reservations = new SceneryReservations(room, geometry);
            if (!reservations.Valid) { plan.InvalidInput = true; return plan; }
            var surfaces = new ScenerySurfaceIndex(geometry, fuel);
            if (!surfaces.Valid)
            {
                plan.BudgetExhausted = fuel.Exhausted;
                plan.InvalidInput = !fuel.Exhausted;
                return plan;
            }
            var anchors = SceneryAnchors.Build(room, config);
            int objectLimit = SceneryWorkBudget.ObjectLimit(config.ObjectAbundance);
            int clusterLimit = SceneryWorkBudget.ClusterLimit(room);
            var usedRecipes = new HashSet<int>();
            var usedStories = new HashSet<SceneryStory>();
            var proposal = new List<SceneryPlacement>(10);
            var access = new List<SceneryBox>(10);
            foreach (var anchor in anchors)
            {
                if (!SceneryWorkBudget.Continue(plan, fuel, objectLimit) || plan.Clusters >= clusterLimit) break;
                if (!SceneryAnchors.CandidateSeparated(anchor, plan.Placements)) continue;
                int start = SceneryAnchors.RecipeStart(room, config, anchor);
                for (int trial = 0; trial < SceneryWorkBudget.MaximumRecipesPerAnchor; trial++)
                {
                    if (fuel.Exhausted) break;
                    int id = (start + trial * 7) % SceneryRecipeCatalog.Count;
                    var recipe = SceneryRecipeCatalog.Get(id);
                    if (usedRecipes.Contains(id) || usedStories.Contains(recipe.Story)) continue;
                    if (!SceneryMoods.Accept(recipe, room, anchor.Rank + (uint)trial)) continue;
                    if (plan.Placements.Count + RequiredCount(recipe) > objectLimit) continue;
                    plan.Candidates++;
                    proposal.Clear();
                    access.Clear();
                    if (!TryCluster(recipe, anchor, room, geometry, config, surfaces,
                        reservations, objectLimit - plan.Placements.Count, proposal, access)) continue;
                    if (fuel.Exhausted) break;
                    // Atomic commit: a missing required chair/table or blocked
                    // approach cancels everything in this candidate, including cups.
                    for (int i = 0; i < proposal.Count; i++) plan.Add(proposal[i]);
                    reservations.Commit(proposal, access);
                    usedRecipes.Add(id);
                    usedStories.Add(recipe.Story);
                    plan.Clusters++;
                    break;
                }
            }
            plan.BudgetExhausted = fuel.Exhausted;
            return plan;
        }

        private static int RequiredCount(SceneryRecipe recipe)
        {
            int count = 0;
            for (int i = 0; i < recipe.Count; i++) if (recipe.Piece(i).Required) count++;
            return count;
        }

        internal static bool TryCluster(SceneryRecipe recipe, SceneryAnchor anchor,
            Room room, RoomGeometry geometry, LayoutConfig config,
            ScenerySurfaceIndex surfaces, SceneryReservations reservations,
            int maximum, List<SceneryPlacement> proposal, List<SceneryBox> access)
        {
            for (int index = 0; index < recipe.Count; index++)
            {
                var definition = recipe.Piece(index);
                SceneryPlacement piece;
                bool fits = SceneryTableLayout.TryPlace(recipe, index, anchor, proposal, out piece) &&
                    proposal.Count < maximum;
                if (!fits)
                {
                    if (definition.Required) return false;
                    continue;
                }
                fits = Fits(piece, room, geometry, config, surfaces, reservations, proposal);
                if (!fits)
                {
                    if (definition.Required) return false;
                    continue;
                }
                proposal.Add(piece);
            }
            if (proposal.Count < 2 || !ScenerySafety.DistinctSlots(proposal)) return false;
            int accessible = 0;
            for (int i = 0; i < proposal.Count; i++)
            {
                var piece = proposal[i];
                if (!SceneryTableLayout.NeedsAccess(piece.Role)) continue;
                if (!SceneryTableLayout.TryConnectedAccess(piece, room, geometry, surfaces,
                    reservations, proposal, out var entry, out var route)) return false;
                access.Add(entry);
                access.Add(route);
                accessible++;
            }
            return accessible > 0;
        }

        private static bool Fits(SceneryPlacement piece, Room room, RoomGeometry geometry,
            LayoutConfig config, ScenerySurfaceIndex surfaces, SceneryReservations reservations,
            IReadOnlyList<SceneryPlacement> proposal)
        {
            if (!ScenerySafety.ValidPlacement(piece, room, config)) return false;
            var bounds = piece.Bounds;
            if (!ScenerySafety.Dry(geometry.Regions, bounds)) return false;
            if (!reservations.Clear(bounds)) return false;
            if (!SceneryReservations.ProposalClear(piece, proposal)) return false;
            if (!surfaces.Clear(bounds)) return false;
            if (piece.ParentSlot >= 0) return ScenerySafety.ParentPresent(piece, proposal);
            return surfaces.FootprintSupported(piece);
        }

        // Used by audits and the native attachment boundary. It is independent
        // from candidate selection and never mutates geometry or source profiles.
        public static bool ValidPlan(SceneryPlan plan, Room room, LayoutConfig config)
        {
            if (plan == null || room == null || config == null) return false;
            if (plan.Placements.Count > SceneryWorkBudget.ObjectLimit(config.ObjectAbundance)) return false;
            if (plan.Clusters < 0 || plan.Clusters > SceneryWorkBudget.ClusterLimit(room)) return false;
            if (!ScenerySafety.DistinctSlots(plan.Placements)) return false;
            for (int i = 0; i < plan.Placements.Count; i++)
            {
                var p = plan.Placements[i];
                if (!ScenerySafety.ValidPlacement(p, room, config) || !ScenerySafety.KnownRecipe(p.Recipe)) return false;
                if (!ScenerySafety.ParentPresent(p, plan.Placements)) return false;
                for (int j = 0; j < i; j++)
                {
                    var other = plan.Placements[j];
                    if (p.ParentSlot == other.Slot || other.ParentSlot == p.Slot) continue;
                    if (p.Bounds.Overlaps(other.Bounds)) return false;
                }
            }
            return true;
        }
    }
}
