using System;
using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public sealed class PoolroomsLevel10Importer : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/Level10/Textures/"))return;
            var importer=(TextureImporter)assetImporter;
            bool normal=assetPath.EndsWith("_Normal.png",StringComparison.Ordinal);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=assetPath.EndsWith("_Albedo.png",StringComparison.Ordinal);
            importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=true;importer.streamingMipmaps=true;
            importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=8;importer.isReadable=false;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            if(assetPath.EndsWith("Siding_Height.png",StringComparison.Ordinal))
            {
                // Keep the native scalar depth on the GPU, too; normal/albedo
                // remain compressed and all maps retain streaming mipmaps.
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                var platform=importer.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.R16;
                importer.SetPlatformTextureSettings(platform);
            }
            importer.alphaSource=assetPath.EndsWith("_Mask.png",StringComparison.Ordinal)?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
        }
        void OnPreprocessAudio()
        {
            if(!assetPath.Contains("/Resources/Level10/Audio/"))return;
            var importer=(AudioImporter)assetImporter;importer.forceToMono=true;importer.loadInBackground=true;
            var samples=importer.defaultSampleSettings;samples.compressionFormat=AudioCompressionFormat.Vorbis;samples.quality=.85f;
            samples.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            samples.loadType=assetPath.EndsWith("FieldWind.wav",StringComparison.Ordinal)?AudioClipLoadType.Streaming:AudioClipLoadType.CompressedInMemory;
            importer.defaultSampleSettings=samples;
        }
    }
}
