using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public sealed class PoolroomsMallImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/MallFinishes/Textures/")&&!assetPath.Contains("/Resources/MallSources/Textures/"))return;
            var importer=(TextureImporter)assetImporter;
            bool normal=assetPath.EndsWith("_normal.png"),data=normal||assetPath.EndsWith("_mask.png");
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!data;importer.mipmapEnabled=true;importer.streamingMipmaps=true;
            importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=8;importer.isReadable=false;
        }
        void OnPreprocessModel()
        {
            if(!assetPath.Contains("/Resources/MallSources/Models/"))return;
            var importer=(ModelImporter)assetImporter;
            importer.globalScale=1;importer.useFileScale=false;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.addCollider=false;importer.isReadable=false;importer.preserveHierarchy=true;
        }
    }
}
