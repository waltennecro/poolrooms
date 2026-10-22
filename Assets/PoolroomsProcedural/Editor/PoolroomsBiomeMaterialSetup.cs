using System.IO;
using Poolrooms.Core;
using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public sealed class PoolroomsBiomeTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/PoolroomsBiomeTextures/"))return;
            var importer=(TextureImporter)assetImporter;bool normal=assetPath.EndsWith("_Normal.png"),mask=assetPath.EndsWith("_Mask.png");
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!normal&&!mask;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
            bool refined=assetPath.Contains("/CalmWater")||assetPath.Contains("/Rain")||assetPath.Contains("/FlumeFlow")||assetPath.Contains("/Aquatic")||assetPath.Contains("/Bath")||assetPath.Contains("/Garden")||assetPath.Contains("/PlayFinish")||assetPath.Contains("/QuietSand")||assetPath.Contains("/AirfieldAsphalt");
            bool conservatory=assetPath.Contains("/ConservatoryGround");
            importer.anisoLevel=refined||conservatory?8:4;importer.maxTextureSize=conservatory?2048:refined?1024:512;importer.textureCompression=TextureImporterCompression.Compressed;
            if(refined||conservatory)importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency=false;
        }
    }
    public static class PoolroomsBiomeMaterialSetup
    {
        private static string Folder {get{return PoolroomsPackagePaths.Asset("Resources/BiomeMaterials");}}
        private static string Library {get{return PoolroomsPackagePaths.Asset("Resources/PoolroomsBiomeMaterials.asset");}}
        [InitializeOnLoadMethod]
        private static void OnImport(){EditorApplication.delayCall+=EnsureOnImport;}
        private static void EnsureOnImport()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureOnImport;return;}
            if(Application.isPlaying||!Directory.Exists(PoolroomsPackagePaths.Asset("Resources/PoolroomsBiomeTextures")))return;
            var library=AssetDatabase.LoadAssetAtPath<PoolroomsBiomeMaterials>(Library);
            if(library==null)Build();else{EnsureSoftPlay(library);UpgradeWinterGardenFinish(library);}
        }
        // R33 generated this unused slot with leaf maps. R34 uses it for planter
        // rims. Patch only that known generated asset/default map combination;
        // preserve its GUID, colour edits and user-assigned/custom material maps.
        private static void UpgradeWinterGardenFinish(PoolroomsBiomeMaterials library)
        {
            var material=library.Find(SurfaceTheme.WinterGarden,(int)Surface.Slide);
            string path=Folder+"/WinterGarden_Slide.mat";
            if(material==null||AssetDatabase.GetAssetPath(material)!=path)return;
            string[][] properties={new[]{"_BaseColorMap","_BaseMap","_MainTex"},new[]{"_NormalMap","_BumpMap"},new[]{"_MaskMap"}};
            string[] suffix={"Albedo","Normal","Mask"};var replacements=new Texture2D[3];bool foundOldAlbedo=false;
            for(int group=0;group<properties.Length;group++)
            {
                var previous=Resources.Load<Texture2D>(PoolroomsBiomeMaterialFactory.TextureRoot+"GardenLeaves_"+suffix[group]);
                replacements[group]=Resources.Load<Texture2D>(PoolroomsBiomeMaterialFactory.TextureRoot+"GardenTerracotta_"+suffix[group]);
                if(previous==null||replacements[group]==null)return;
                foreach(string property in properties[group])if(material.HasProperty(property))
                {
                    var current=material.GetTexture(property);
                    if(current!=null&&current!=previous)return;
                    if(group==0&&current==previous)foundOldAlbedo=true;
                }
            }
            if(!foundOldAlbedo)return;
            for(int group=0;group<properties.Length;group++)foreach(string property in properties[group])
                if(material.HasProperty(property))material.SetTexture(property,replacements[group]);
            float smooth=BiomeSurfaces.Smoothness("GardenTerracotta");
            foreach(string property in new[]{"_Smoothness","_Glossiness"})if(material.HasProperty(property))material.SetFloat(property,smooth);
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
        }
        // Existing libraries retain all their materials and GUIDs. Add only the new biome slots.
        private static void EnsureSoftPlay(PoolroomsBiomeMaterials library)
        {
            int capacity=System.Enum.GetValues(typeof(SurfaceTheme)).Length*6;
            if(library.surfaces!=null&&library.surfaces.Length>=capacity&&library.Find(SurfaceTheme.TransitGallery,5)!=null)return;
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            if(library.surfaces==null)library.surfaces=new Material[capacity];
            else if(library.surfaces.Length<capacity)System.Array.Resize(ref library.surfaces,capacity);
            for(int theme=(int)SurfaceTheme.SoftPlay;theme<System.Enum.GetValues(typeof(SurfaceTheme)).Length;theme++)for(int slot=0;slot<6;slot++)
            {
                if(library.Find((SurfaceTheme)theme,slot)!=null)continue;
                string path=Folder+"/"+(SurfaceTheme)theme+"_"+(Surface)slot+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=PoolroomsBiomeMaterialFactory.Create((SurfaceTheme)theme,(Surface)slot);PoolroomsBiomeMaterialFactory.Validate(material);AssetDatabase.CreateAsset(material,path);}
                library.surfaces[theme*6+slot]=material;
            }
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Poolrooms/Centro comercial completo v20/Preparar materiales PBR independientes")]
        public static void Prepare(){Build();}
        public static PoolroomsBiomeMaterials Build()
        {
            if(Application.isPlaying)return AssetDatabase.LoadAssetAtPath<PoolroomsBiomeMaterials>(Library);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var library=AssetDatabase.LoadAssetAtPath<PoolroomsBiomeMaterials>(Library);
            if(library==null){library=ScriptableObject.CreateInstance<PoolroomsBiomeMaterials>();AssetDatabase.CreateAsset(library,Library);}
            library.surfaces=new Material[System.Enum.GetValues(typeof(SurfaceTheme)).Length*6];
            for(int theme=(int)SurfaceTheme.IndoorPlay;theme<System.Enum.GetValues(typeof(SurfaceTheme)).Length;theme++)for(int slot=0;slot<6;slot++)
            {
                string path=Folder+"/"+(SurfaceTheme)theme+"_"+(Surface)slot+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);var generated=PoolroomsBiomeMaterialFactory.Create((SurfaceTheme)theme,(Surface)slot);
                if(m==null){m=generated;AssetDatabase.CreateAsset(m,path);}else{m.CopyPropertiesFromMaterial(generated);Object.DestroyImmediate(generated);}
                PoolroomsBiomeMaterialFactory.Validate(m);EditorUtility.SetDirty(m);library.surfaces[theme*6+slot]=m;
            }
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();return library;
        }
    }
}
