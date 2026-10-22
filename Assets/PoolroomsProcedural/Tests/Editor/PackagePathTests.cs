#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor.Tests
{
    public sealed class PackagePathTests
    {
        [Test]
        public void InstalledLocatorFindsAllNineDemoTextures()
        {
            Assert.That(AssetDatabase.IsValidFolder(PoolroomsPackagePaths.Root),Is.True);
            PoolroomsPackagePaths.ValidateDemoTextures();
            foreach(string material in new[]{"Ceramic","Plaster","Metal"})
            foreach(string map in new[]{"Albedo","Normal","Mask"})
            {
                string path=PoolroomsPackagePaths.Asset("Textures/"+material+"_"+map+".png");
                Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(path),Is.Not.Null,path);
                Assert.That(AssetImporter.GetAtPath(path),Is.TypeOf<TextureImporter>(),path);
            }
        }
        [TestCase("Assets/PoolroomsProcedural")]
        [TestCase("Assets/Poolrooms_Compilacion_Corregida_Assets/PoolroomsProcedural")]
        [TestCase("Assets/Mi juego/Áreas liminales/PoolroomsProcedural")]
        public void ScriptAnchorPreservesActualInstallationFolder(string root)
        {
            Assert.That(PoolroomsPackagePaths.RootFromScriptPath(root+"/Editor/PoolroomsPackagePaths.cs"),Is.EqualTo(root));
            Assert.That(PoolroomsPackagePaths.RootFromScriptPath((root+"/Editor/PoolroomsPackagePaths.cs").Replace('/','\\')),Is.EqualTo(root));
        }
    }
}
#endif
