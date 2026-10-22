using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Poolrooms.Editor
{
    // A serialized material keeps this native HDRP shader and its used keywords in player builds.
    // No shader is copied or added to Always Included Shaders.
    [InitializeOnLoad]
    public sealed class PoolroomsTerrainShaderSetup : IPreprocessBuildWithReport
    {
        private static string Path {get{return PoolroomsPackagePaths.Asset("Resources/PoolroomsTerrainLayeredTemplate.mat");}}
        static PoolroomsTerrainShaderSetup(){EditorApplication.delayCall+=Prepare;}
        public int callbackOrder {get{return -100;}}
        public void OnPreprocessBuild(BuildReport report){Prepare();}
        private static void Prepare()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)
            {EditorApplication.delayCall+=Prepare;return;}
            // Earlier installers could create this shared template outside the
            // actual package. Reuse it rather than create duplicate Resources.
            if(AssetDatabase.LoadAssetAtPath<Material>(Path)!=null||Resources.Load<Material>("PoolroomsTerrainLayeredTemplate")!=null||Shader.Find("HDRP/LayeredLit")==null)return;
            var material=TerrainMaterialSet.CreateLayeredGround();if(material==null)return;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            AssetDatabase.CreateAsset(material,Path);AssetDatabase.SaveAssets();
        }
    }
}
