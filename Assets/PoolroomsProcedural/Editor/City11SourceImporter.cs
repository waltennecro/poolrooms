using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public sealed class City11SourceImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/City11/Textures/"))return;
            var importer=(TextureImporter)assetImporter;
            bool normal=assetPath.EndsWith("_normal.png"),mask=assetPath.EndsWith("_mask.png");
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!normal&&!mask;importer.mipmapEnabled=true;importer.maxTextureSize=assetPath.Contains("SheenChair_")?2048:1024;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;
        }
        void OnPreprocessModel()
        {
            if(!assetPath.Contains("/Resources/City11/Models/"))return;
            var importer=(ModelImporter)assetImporter;importer.globalScale=1;importer.useFileScale=false;
            importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.addCollider=false;importer.isReadable=false;
        }
    }
}
