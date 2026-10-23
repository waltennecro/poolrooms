using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Materials are created on demand on the Unity main thread. No engine
    // allocation happens in a MonoBehaviour constructor or instance initializer.
    public static class PoolroomsForest39Materials
    {
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

        public static Material Resolve(SurfaceTheme theme, Surface surface)
        {
            int key = (int)theme * 8 + (int)surface;
            if (cache.TryGetValue(key, out var previous) && previous != null) return previous;
            string bank = Forest39Materials.Bank(theme, surface);
            V3 tint = Forest39Materials.Tint(theme, surface);
            var material = City11Sources.MaterialFor(new City11Sources.MaterialDefinition
            {
                name = "Forest39_" + theme + "_" + surface,
                albedo = "Forest39/Textures/" + bank + "_Albedo",
                normal = bank == "Leaf" ? null : "Forest39/Textures/" + bank + "_Normal",
                mask = bank == "Leaf" ? null : "Forest39/Textures/" + bank + "_Mask",
                colour = new[] { tint.X, tint.Y, tint.Z, 1f },
                roughness = Forest39Materials.Roughness(theme),
                alphaClip = bank == "Canopy", alphaCutoff = .45f,
                doubleSided = bank == "Leaf" || bank == "Canopy"
            });
            var height = bank == "Leaf" ? null : Resources.Load<Texture2D>("Forest39/Textures/" + bank + "_Height");
            if (height != null && material.HasProperty("_HeightMap"))
            {
                material.SetTexture("_HeightMap", height);
                // Small relief under grazing light; actual hills, roots and bark
                // silhouette remain geometry. This is not tessellation or RT GI.
                Set(material, "_DisplacementMode", 2);
                Set(material, "_HeightAmplitude", bank == "Bark" ? .012f : .018f);
                Set(material, "_HeightCenter", .5f);
                Set(material, "_HeightPoMAmplitude", bank == "Bark" ? 1.2f : 1.8f);
                Set(material, "_PPDMinSamples", 5);
                Set(material, "_PPDMaxSamples", 16);
                Set(material, "_PPDLodThreshold", 5);
                material.EnableKeyword("_HEIGHTMAP");
                material.EnableKeyword("_PIXEL_DISPLACEMENT");
            }
            cache[key] = material;
            return material;
        }

        static void Set(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            // City11Sources owns the shared materials; do not destroy its cache.
            cache.Clear();
        }
    }
}
