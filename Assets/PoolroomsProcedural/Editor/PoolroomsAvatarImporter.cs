using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Poolrooms.Editor
{
    [InitializeOnLoad]
    public static class PoolroomsAvatarImporter
    {
        static PoolroomsAvatarImporter(){PoolroomsAvatar.ProjectImporter=ImportFile;}
        public static PoolroomsAvatarDefinition ImportFile(string file)
        {
            string extension=Path.GetExtension(file).ToLowerInvariant();
            if(extension!=".fbx"&&extension!=".dae"&&extension!=".3ds"&&extension!=".dxf"&&extension!=".blend")throw new NotSupportedException("Formato no compatible. Usa OBJ, FBX, DAE, 3DS, DXF, BLEND o GLB/glTF con glTFast.");
            if(new FileInfo(file).Length>128*1024*1024)throw new FormatException("El modelo supera 128 MB. Exporta una versión optimizada para el jugador.");
            string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/ImportedSkins/"+Path.GetFileNameWithoutExtension(file));Directory.CreateDirectory(folder);
            string target=folder+"/"+Path.GetFileName(file);File.Copy(file,target);int count=0;long bytes=0;
            // Only explicit adjacent texture folders, never recursively copy a Downloads/Documents tree.
            foreach(string directory in new[]{Path.GetDirectoryName(file),Path.Combine(Path.GetDirectoryName(file),"Textures"),Path.Combine(Path.GetDirectoryName(file),"textures")})
            {
                if(!Directory.Exists(directory))continue;
                foreach(string image in Directory.GetFiles(directory))
                {
                    string ext=Path.GetExtension(image).ToLowerInvariant();if(ext!=".png"&&ext!=".jpg"&&ext!=".jpeg"&&ext!=".tga"&&ext!=".tif"&&ext!=".exr")continue;
                    long size=new FileInfo(image).Length;if(size>16*1024*1024||count>=32||bytes+size>128*1024*1024)continue;
                    string sub=directory==Path.GetDirectoryName(file)?"":Path.GetFileName(directory)+"/";Directory.CreateDirectory(folder+"/"+sub);
                    string destination=folder+"/"+sub+Path.GetFileName(image);if(File.Exists(destination))continue;File.Copy(image,destination);count++;bytes+=size;
                }
            }
            AssetDatabase.Refresh();AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
            if(AssetImporter.GetAtPath(target) is ModelImporter importer){importer.isReadable=true;importer.SaveAndReimport();}
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(target);if(model==null)throw new FormatException("Unity no pudo importar el modelo. BLEND requiere Blender instalado; consulta la consola.");
            Selection.activeObject=model;FromSelection();return Selection.activeObject as PoolroomsAvatarDefinition;
        }
        [MenuItem("Tools/Poolrooms/Personaje/Crear apariencia desde modelo seleccionado")]
        public static void FromSelection()
        {
            var model=Selection.activeObject as GameObject;string source=AssetDatabase.GetAssetPath(model);
            if(model==null||string.IsNullOrEmpty(source)||model.GetComponentsInChildren<Renderer>(true).Length==0)
            {EditorUtility.DisplayDialog("Apariencia 3D","Arrastra un FBX, OBJ, DAE, 3DS o DXF a Assets y selecciona el modelo importado. Para GLB/glTF usa un importador compatible y crea una definición de apariencia asignando el prefab.","Aceptar");return;}
            string directory="Assets/Resources/PoolroomsAvatars";Directory.CreateDirectory(directory);AssetDatabase.Refresh();
            string name=Path.GetFileNameWithoutExtension(source),folder=AssetDatabase.GenerateUniqueAssetPath(directory+"/"+name+"_Assets");Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            GameObject holder=null;
            try
            {
                holder=new GameObject("Avatar import");holder.SetActive(false);var copy=Object.Instantiate(model,holder.transform);
                bool hdrp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null&&UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline");
                var shader=hdrp?Shader.Find("HDRP/Lit"):null;var converted=new Dictionary<Material,Material>();
                foreach(var renderer in copy.GetComponentsInChildren<Renderer>(true))
                {
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)
                    {
                        var old=materials[i];if(shader==null||old==null||old.shader==null||!(old.shader.name=="Standard"||old.shader.name=="Universal Render Pipeline/Lit"))continue;
                        if(!converted.TryGetValue(old,out var replacement))
                        {
                            replacement=new Material(shader){name=old.name+" HDRP"};string colour=old.HasProperty("_BaseColor")?"_BaseColor":"_Color";
                            if(old.HasProperty(colour))replacement.SetColor("_BaseColor",old.GetColor(colour));
                            string texture=old.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
                            if(old.HasProperty(texture)){replacement.SetTexture("_BaseColorMap",old.GetTexture(texture));replacement.SetTextureScale("_BaseColorMap",old.GetTextureScale(texture));replacement.SetTextureOffset("_BaseColorMap",old.GetTextureOffset(texture));}
                            if(old.HasProperty("_BumpMap")&&old.GetTexture("_BumpMap")!=null){replacement.SetTexture("_NormalMap",old.GetTexture("_BumpMap"));replacement.EnableKeyword("_NORMALMAP_TANGENT_SPACE");replacement.EnableKeyword("_NORMALMAP");}
                            if(old.HasProperty("_BumpScale"))replacement.SetFloat("_NormalScale",old.GetFloat("_BumpScale"));
                            if(old.HasProperty("_Metallic"))replacement.SetFloat("_Metallic",old.GetFloat("_Metallic"));
                            string smoothness=old.HasProperty("_Smoothness")?"_Smoothness":"_Glossiness";if(old.HasProperty(smoothness))replacement.SetFloat("_Smoothness",old.GetFloat(smoothness));
                            bool cutout=old.HasProperty("_Mode")&&old.GetFloat("_Mode")==1||old.HasProperty("_AlphaClip")&&old.GetFloat("_AlphaClip")>0;
                            bool transparent=old.HasProperty("_Mode")&&old.GetFloat("_Mode")>=2||old.HasProperty("_Surface")&&old.GetFloat("_Surface")>0;
                            if(cutout){replacement.SetFloat("_AlphaCutoffEnable",1);replacement.SetFloat("_AlphaCutoff",old.HasProperty("_Cutoff")?old.GetFloat("_Cutoff"):.5f);replacement.SetFloat("_AlphaCutoffShadow",old.HasProperty("_Cutoff")?old.GetFloat("_Cutoff"):.5f);}
                            if(transparent){replacement.SetFloat("_SurfaceType",1);replacement.SetFloat("_BlendMode",0);replacement.SetFloat("_ZWrite",0);replacement.SetFloat("_TransparentZWrite",0);replacement.renderQueue=3000;}
                            PoolroomsBiomeMaterialFactory.Validate(replacement);
                            AssetDatabase.CreateAsset(replacement,AssetDatabase.GenerateUniqueAssetPath(folder+"/Material_"+converted.Count+".mat"));converted.Add(old,replacement);
                        }
                        materials[i]=replacement;
                    }
                    renderer.sharedMaterials=materials;
                }
                var prefab=PrefabUtility.SaveAsPrefabAsset(copy,folder+"/Avatar.prefab");
                var definition=ScriptableObject.CreateInstance<PoolroomsAvatarDefinition>();definition.model=prefab;
                string path=AssetDatabase.GenerateUniqueAssetPath(directory+"/"+name+".asset");AssetDatabase.CreateAsset(definition,path);AssetDatabase.SaveAssets();
                Selection.activeObject=definition;EditorGUIUtility.PingObject(definition);
                Debug.Log("Apariencia creada. Ajusta altura, orientación y materiales HDRP. Aparecerá en Ajustes > Skins. Humanoid conserva sus pesos y utiliza la animación común; otros modelos humanoides legibles reciben pesos automáticos. Materiales simples convertidos sin modificar los originales; revisa transparencias y mapas PBR avanzados. Conserva la licencia del modelo.",definition);
            }
            finally{if(holder!=null)Object.DestroyImmediate(holder);}
        }
        [MenuItem("Tools/Poolrooms/Personaje/Abrir carpeta de OBJ del jugador")]
        public static void Folder(){string path=Path.Combine(Application.persistentDataPath,"Avatars");Directory.CreateDirectory(path);EditorUtility.RevealInFinder(path);}
    }
}
