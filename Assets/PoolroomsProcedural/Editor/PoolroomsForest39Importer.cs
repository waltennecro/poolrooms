using System;
using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public sealed class PoolroomsForest39Importer : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Forest39/Textures/")) return;
            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("_Normal.png", StringComparison.Ordinal);
            bool colour = assetPath.EndsWith("_Albedo.png", StringComparison.Ordinal);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = colour;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = true;
            bool foliage = assetPath.EndsWith("Canopy_Albedo.png", StringComparison.Ordinal);
            importer.alphaIsTransparency = foliage;
            importer.mipMapsPreserveCoverage = foliage;
            if(foliage)importer.alphaTestReferenceValue = .45f;
            importer.streamingMipmaps = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaSource = (assetPath.EndsWith("_Mask.png", StringComparison.Ordinal) || assetPath.EndsWith("Canopy_Albedo.png", StringComparison.Ordinal)) ?
                TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        }
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("/Resources/Forest39/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = true;
            var samples = importer.defaultSampleSettings;
            samples.compressionFormat = AudioCompressionFormat.Vorbis;
            samples.quality = .85f;
            samples.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            samples.loadType = assetPath.EndsWith("OakWind.wav", StringComparison.Ordinal) ?
                AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            importer.defaultSampleSettings = samples;
        }
    }
}
