using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Poolrooms.Core;

namespace Poolrooms.Editor
{
    public static class PoolroomsSetup
    {
        private const string Textures="Assets/PoolroomsProcedural/Textures/";
        [MenuItem("Tools/Poolrooms/Crear demo")]
        public static void CreateDemo()
        {CreateDemo(false);}
        [MenuItem("Tools/Poolrooms/Crear demo de 255 zonas")]
        public static void CreateCatalogDemo()
        {CreateDemo(true);}
        private static void CreateDemo(bool catalogue)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/PoolroomsDemo");
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SceneManager.SetActiveScene(scene);
            var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
            profile.zoneMode=catalogue?ZoneMode.Catalog:ZoneMode.Random;
            profile.continuousWorld=!catalogue;
            profile.liminalGeneration=catalogue?LiminalGeneration.Authored:LiminalGeneration.Expanded;
            profile.proceduralGrammar=catalogue?ProceduralGrammar.CrossV12:ProceduralGrammar.SolarAtlas;
            profile.composition=SpatialComposition.Exploration;profile.lighting=LightingDistribution.Composed;
            if(catalogue){profile.columns=17;profile.rows=15;profile.distribution=LayoutStyle.Network;}
            AssetDatabase.CreateAsset(profile,folder+"/PoolroomsProfile.asset");
            CreateMaterials(profile,folder);
            var go=new GameObject("Poolrooms World");var world=go.AddComponent<PoolroomsWorld>();world.profile=profile;world.unifiedExploration=!catalogue;
            if(!catalogue){profile.autonomousGeneration=true;profile.independentBiomeMaterials=true;profile.enableTerrainRegions=true;profile.enableGiantMalls=true;profile.enhancedMallArchitecture=profile.refinedMallArchitecture=true;profile.biomeMaterials=PoolroomsBiomeMaterialSetup.Build();}
            var player=new GameObject("Explorer");player.transform.position=new Vector3(0,.1f,0);
            var body=player.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;
            body.stepOffset=.34f;body.skinWidth=.025f;body.slopeLimit=48;
            var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";cameraObject.transform.SetParent(player.transform,false);
            cameraObject.transform.localPosition=Vector3.up*1.65f;var camera=cameraObject.AddComponent<Camera>();
            camera.nearClipPlane=.06f;camera.farClipPlane=180;camera.fieldOfView=68;camera.allowHDR=true;cameraObject.AddComponent<AudioListener>();
            var explorer=player.AddComponent<PoolroomsExplorer>();explorer.world=world;explorer.eyes=cameraObject.transform;world.viewer=player.transform;
            var sound=player.AddComponent<PoolroomsAmbientAudio>();sound.explorer=explorer;sound.world=world;
            Type input=Type.GetType("Poolrooms.PoolroomsInputSystem, Poolrooms.InputSystem");
            if(input!=null)player.AddComponent(input);
            Type hdrp=Type.GetType("Poolrooms.HDRP.PoolroomsHDRP, Poolrooms.HDRP");
            if(IsHDRP()&&hdrp!=null)
            {
                go.AddComponent(hdrp);
                Type setup=Type.GetType("Poolrooms.HDRP.PoolroomsHDRPSetup, Poolrooms.HDRP");
                setup?.GetMethod("CreateEnvironment",BindingFlags.Public|BindingFlags.Static)?.Invoke(null,new object[]{go,camera,folder});
            }
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene,folder+"/PoolroomsDemo.unity");Selection.activeGameObject=go;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0,2,0),Quaternion.Euler(20,30,0),25);
            Debug.Log("Poolrooms creado. Abre "+folder+"/PoolroomsDemo.unity y pulsa Play. "+
                (catalogue?"255 zonas distintas; el streaming carga solo las cercanas. ":"Mundo liminal continuo: diseños nuevos de Poolrooms y juegos infantiles, cargados por proximidad. ")+"Catálogo: Tools > Poolrooms > Catálogo de 255 zonas.");
        }
        private static bool IsHDRP(){var p=GraphicsSettings.currentRenderPipeline;return p!=null&&p.GetType().Name.Contains("HDRenderPipeline");}
        private static void CreateMaterials(PoolroomsProfile p,string folder)
        {
            Texture2D[] ceramic={LoadMap("Ceramic_Albedo.png",false,false),LoadMap("Ceramic_Normal.png",true,true),LoadMap("Ceramic_Mask.png",true,false)};
            Texture2D[] plasterMaps={LoadMap("Plaster_Albedo.png",false,false),LoadMap("Plaster_Normal.png",true,true),LoadMap("Plaster_Mask.png",true,false)};
            Texture2D[] metalMaps={LoadMap("Metal_Albedo.png",false,false),LoadMap("Metal_Normal.png",true,true),LoadMap("Metal_Mask.png",true,false)};
            string shaderName=IsHDRP()?"HDRP/Lit":(GraphicsSettings.currentRenderPipeline!=null?"Universal Render Pipeline/Lit":"Standard");
            Shader shader=Shader.Find(shaderName);if(shader==null)throw new InvalidOperationException("Shader unavailable: "+shaderName);
            // The mask maps carry real smoothness/occlusion, so the smoothness remap is 0..1 and the
            // colour only tints. Ceramic glaze is glossy; grout, plaster and brushed metal are rougher.
            p.tile=Make("Ceramic Ivory",new Color(.98f,.98f,.93f),1f,0,ceramic,.55f);
            p.accent=Make("Ceramic Mint",new Color(.5f,.8f,.75f),1f,0,ceramic,.55f);
            p.plaster=Make("Ceiling",new Color(1f,1f,.97f),1f,0,plasterMaps,1f);
            p.metal=Make("Brushed Steel",Color.white,1f,1f,metalMaps,.6f);
            p.glow=Make("Luminous Panels",new Color(1,.94f,.77f),.2f,0,null,1f);
            SetColor(p.glow,"_EmissiveColor",new Color(1,.94f,.77f)*12);SetColor(p.glow,"_EmissionColor",new Color(1,.94f,.77f)*4);
            p.glow.EnableKeyword("_EMISSION");ValidateHDRP(p.glow);EditorUtility.SetDirty(p.glow);
            p.slide=Make("Pastel Slides",new Color(.76f,.48f,.59f),.86f,0,null,1f);
            p.waterPreview=Make("Water Preview (opaque fallback)",new Color(.09f,.48f,.44f),.98f,0,null,1f);

            Material Make(string name,Color color,float smooth,float metallic,Texture2D[] maps,float normalScale)
            {
                var m=new Material(shader){name=name,enableInstancing=true};
                SetColor(m,"_BaseColor",color);SetColor(m,"_Color",color);
                // Reduce sparkling at grazing angles on fine tile normals (HDRP/Lit only).
                SetFloat(m,"_EnableGeometricSpecularAA",1f);
                SetFloat(m,"_SpecularAAScreenSpaceVariance",.1f);SetFloat(m,"_SpecularAAThreshold",.2f);
                SetFloat(m,"_Smoothness",smooth);SetFloat(m,"_Glossiness",smooth);SetFloat(m,"_Metallic",metallic);
                if(maps!=null)
                {
                    SetTexture(m,"_BaseColorMap",maps[0]);SetTexture(m,"_BaseMap",maps[0]);SetTexture(m,"_MainTex",maps[0]);
                    SetTexture(m,"_NormalMap",maps[1]);SetTexture(m,"_BumpMap",maps[1]);SetTexture(m,"_MaskMap",maps[2]);
                    SetFloat(m,"_NormalScale",normalScale);SetFloat(m,"_BumpScale",normalScale);
                    SetFloat(m,"_SmoothnessRemapMin",0f);SetFloat(m,"_SmoothnessRemapMax",smooth);
                    SetFloat(m,"_AORemapMin",0f);SetFloat(m,"_AORemapMax",1f);
                    SetFloat(m,"_MetallicRemapMin",0f);SetFloat(m,"_MetallicRemapMax",metallic>0?1f:0f);
                    m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_MASKMAP");
                }
                ValidateHDRP(m);AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");return m;
            }
        }
        private static Texture2D LoadMap(string file,bool linear,bool normal)
        {
            string path=Textures+file;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new FileNotFoundException("Copia la carpeta PoolroomsProcedural directamente en Assets. Falta "+path);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!linear;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=8;
            importer.alphaIsTransparency=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static void SetColor(Material m,string p,Color v){if(m.HasProperty(p))m.SetColor(p,v);}
        private static void SetFloat(Material m,string p,float v){if(m.HasProperty(p))m.SetFloat(p,v);}
        private static void SetTexture(Material m,string p,Texture v){if(m.HasProperty(p))m.SetTexture(p,v);}
        private static void ValidateHDRP(Material m)
        {
            if(!IsHDRP())return;
            Type t=Type.GetType("UnityEngine.Rendering.HighDefinition.HDMaterial, Unity.RenderPipelines.HighDefinition.Runtime");
            t?.GetMethod("ValidateMaterial",new[]{typeof(Material)})?.Invoke(null,new object[]{m});
        }
        public static void Freeze(PoolroomsWorld world)
        {
            if(Application.isPlaying)return;
            if(world.profile!=null&&world.profile.continuousWorld)
            {Debug.LogWarning("El mundo continuo se genera por streaming. Usa un perfil finito para congelar mallas y hornear un mapa completo.",world);return;}
            world.BuildAllImmediate();if(world.GeneratedRoot==null)return;
            string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/PoolroomsFrozen");Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(var chunk in world.Chunks)
            {
                foreach(Mesh mesh in chunk.ownedMeshes)
                {
                    bool isWater=chunk.water!=null&&chunk.water.GetComponent<MeshFilter>().sharedMesh==mesh;
                    if(!isWater)Unwrapping.GenerateSecondaryUVSet(mesh);
                    string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/Room_"+chunk.roomId+"_"+mesh.name+".asset");
                    AssetDatabase.CreateAsset(mesh,path);
                }
                foreach(Material material in chunk.ownedMaterials)if(material!=null&&!AssetDatabase.Contains(material))
                    AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath(folder+"/Room_"+chunk.roomId+"_Panels.mat"));
                chunk.meshesAreAssets=true;
                foreach(MeshRenderer renderer in chunk.GetComponentsInChildren<MeshRenderer>(true))if(renderer!=chunk.water)
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
                    renderer.receiveGI=ReceiveGI.Lightmaps;
                }
                foreach(Light light in chunk.GetComponentsInChildren<Light>()) {light.lightmapBakeType=LightmapBakeType.Mixed;light.enabled=true;}
                EditorUtility.SetDirty(chunk);
            }
            world.generateOnStart=false;world.streaming=false;EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log("Geometría guardada en "+folder+". Guarda la escena y hornea iluminación. Mantén Generate On Start y Streaming desactivados para conservar los lightmaps.",world);
        }
    }
    [CustomEditor(typeof(PoolroomsWorld))]
    public sealed class PoolroomsWorldInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var world=(PoolroomsWorld)target;
            EditorGUILayout.Space();
            if(world.profile!=null&&GUILayout.Button("Nueva semilla y regenerar"))
            {
                Undo.RecordObject(world.profile,"Nueva semilla Poolrooms");
                world.profile.seed=Guid.NewGuid().GetHashCode();EditorUtility.SetDirty(world.profile);
                if(Application.isPlaying)world.Rebuild();
                else {world.BuildAllImmediate();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);}
            }
            if(Application.isPlaying)
            {
                if(GUILayout.Button("Regenerar"))world.Rebuild();return;
            }
            EditorGUILayout.HelpBox("La vista previa usa mallas temporales. Congela y guarda mallas para conservarlas al cerrar Unity y poder hornear iluminación.",MessageType.Info);
            if(GUILayout.Button("Generar vista previa completa")){world.BuildAllImmediate();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);}
            if(GUILayout.Button("Congelar y guardar mallas para bake"))PoolroomsSetup.Freeze(world);
            if(GUILayout.Button("Limpiar vista previa")){world.ClearPreview();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);}
        }
    }
}
