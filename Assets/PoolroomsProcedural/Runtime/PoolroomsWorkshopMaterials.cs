using System;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    public static class PoolroomsWorkshopMaterials
    {
        private const int PaletteKey = 45000;
        private static readonly Color[] colours =
        {
            new Color(.79f,.68f,.43f), new Color(.35f,.57f,.43f),
            new Color(.35f,.53f,.7f), new Color(.73f,.43f,.34f)
        };

        public static Color Tint(int colour)
        {
            if (colour < 0 || colour >= colours.Length) throw new ArgumentOutOfRangeException(nameof(colour));
            return colours[colour];
        }

        public static Material Resolve(StationKind kind, int slot, PoolroomsProfile profile, PoolroomsChunk owner)
        {
            int colour = ColourIndex((Surface)slot);
            bool woven = kind == StationKind.Weaving || kind == StationKind.Bookbinding;
            if (!woven || colour < 0)
                return PoolroomsBiomeMaterialFactory.Resolve(SurfaceTheme.EncounterFurniture, slot, profile, owner);
            int key = PaletteKey + colour;
            if (owner.paletteMaterials.TryGetValue(key, out var cached)) return cached;
            // Copy the woven PBR material once per colour and chunk. Its normal,
            // roughness and texture maps remain shared, rather than turning blue
            // threads into the brass material used by ordinary station hardware.
            var source = PoolroomsBiomeMaterialFactory.Resolve(SurfaceTheme.EncounterFurniture,
                (int)Surface.Tile, profile, owner);
            var material = new Material(source)
            {
                name = "Workshop woven colour " + colour,
                enableInstancing = true
            };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Tint(colour));
            if (material.HasProperty("_Color")) material.SetColor("_Color", Tint(colour));
            owner.paletteMaterials.Add(key, material);
            owner.ownedMaterials.Add(material);
            return material;
        }

        private static int ColourIndex(Surface surface)
        {
            switch (surface)
            {
                case Surface.Plaster: return 0;
                case Surface.Slide: return 1;
                case Surface.Metal: return 2;
                case Surface.Accent: return 3;
                default: return -1;
            }
        }
    }
}
