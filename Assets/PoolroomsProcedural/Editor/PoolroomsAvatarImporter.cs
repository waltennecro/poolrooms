using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsAvatarImporter
    {
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
                Debug.Log("Apariencia creada. Ajusta altura, orientación, materiales HDRP y controlador Animator si el modelo tiene rig. Aparecerá en Ajustes > Cámara > Apariencia. Materiales simples convertidos sin modificar los originales; revisa transparencias y mapas PBR avanzados. Conserva la licencia del modelo.",definition);
            }
            finally{if(holder!=null)Object.DestroyImmediate(holder);}
        }
        [MenuItem("Tools/Poolrooms/Personaje/Abrir carpeta de OBJ del jugador")]
        public static void Folder(){string path=Path.Combine(Application.persistentDataPath,"Avatars");Directory.CreateDirectory(path);EditorUtility.RevealInFinder(path);}
    }
}
