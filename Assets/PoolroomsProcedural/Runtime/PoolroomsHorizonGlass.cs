using UnityEngine;
using Poolrooms.Core;

namespace Poolrooms
{
    public static class PoolroomsHorizonGlass
    {
        public static Material Resolve(int color, int slot, PoolroomsProfile profile, PoolroomsChunk owner)
        {
            if (slot != (int)Surface.Tile) return PoolroomsBiomeMaterialFactory.Resolve(SurfaceTheme.Glass, slot, profile, owner);
            int key = 40000 + color * 6 + slot;
            if (owner.paletteMaterials.TryGetValue(key, out var cached)) return cached;
            var source = PoolroomsBiomeMaterialFactory.Resolve(SurfaceTheme.Glass, slot, profile, owner);
            var glass = new Material(source) { name = "Horizon stained glass " + color, enableInstancing = true };
            var tint = color == 0 ? new Color(.92f, .65f, .28f, .28f) :
                color == 1 ? new Color(.31f, .68f, .7f, .25f) : new Color(.8f, .43f, .51f, .26f);
            foreach (string property in new[] { "_BaseColor", "_Color" })
                if (glass.HasProperty(property)) glass.SetColor(property, tint);
            // HDRP consumes transmittance only if the project's glass/refraction
            // configuration enables it. Never invent colored projected shadows.
            if (glass.HasProperty("_TransmittanceColor"))
                glass.SetColor("_TransmittanceColor", new Color(tint.r, tint.g, tint.b));
            owner.paletteMaterials.Add(key, glass);
            owner.ownedMaterials.Add(glass);
            return glass;
        }
    }
}
