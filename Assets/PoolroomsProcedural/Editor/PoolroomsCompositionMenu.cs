using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsCompositionMenu
    {
        [MenuItem("Tools/Poolrooms/Composición/Activar mundo aleatorio continuo v11")]
        public static void Enable(){Apply(SpatialComposition.Exploration,true);}
        [MenuItem("Tools/Poolrooms/Composición/Aleatorio local en mapa finito v11")]
        public static void Finite(){Apply(SpatialComposition.Exploration,false);}
        [MenuItem("Tools/Poolrooms/Composición/Desactivar composición local (mapa finito)")]
        public static void Disable(){Apply(SpatialComposition.None,false);}

        private static void Apply(SpatialComposition composition,bool continuous)
        {
            var world=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            if(world==null)
            {
                var worlds=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);
                if(worlds.Length==1)world=worlds[0];
            }
            if(world==null||world.profile==null){Debug.LogWarning("Selecciona el Poolrooms World de tu escena.");return;}
            if(Application.isPlaying)
            {Debug.LogWarning("Activa la composición fuera de Play para guardar un perfil propio y regenerar la escena.",world);return;}
            if(world.profile.composition==composition&&world.profile.continuousWorld==continuous&&
                (composition==SpatialComposition.None||world.profile.zoneMode==ZoneMode.Random))
            {Debug.Log("Este perfil ya usa "+composition+". Pulsa Play o regenera la vista previa.",world);return;}
            // Clone the profile so another scene sharing it is not silently recomposed.
            string original=AssetDatabase.GetAssetPath(world.profile);
            string folder=string.IsNullOrEmpty(original)?"Assets":System.IO.Path.GetDirectoryName(original).Replace('\\','/');
            var copy=Object.Instantiate(world.profile);copy.name=world.profile.name+" - Composición";
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(folder+"/PoolroomsComposition.asset"));
            Undo.RegisterCreatedObjectUndo(copy,"Perfil de composición Poolrooms");
            Undo.RecordObject(world,"Composición Poolrooms");world.profile=copy;
            Undo.RecordObject(world.profile,"Composición Poolrooms");world.profile.composition=composition;world.profile.continuousWorld=continuous;
            if(composition==SpatialComposition.Exploration)
            {
                if(world.profile.zoneMode==ZoneMode.Catalog||world.profile.zoneMode==ZoneMode.Mixed)world.profile.zoneMode=ZoneMode.Random;
                if(world.profile.lighting==LightingDistribution.ZoneDefaults||world.profile.lighting==LightingDistribution.Seeded)
                {
                    world.profile.compositionPreviousLighting=world.profile.lighting;world.profile.compositionRemembersLighting=true;
                    world.profile.lighting=LightingDistribution.Composed;
                }
            }
            else if(world.profile.lighting==LightingDistribution.Composed)
            {
                world.profile.lighting=world.profile.compositionRemembersLighting?
                    world.profile.compositionPreviousLighting:LightingDistribution.ZoneDefaults;
                world.profile.compositionRemembersLighting=false;
            }
            world.generateOnStart=true;if(continuous)world.streaming=true;
            // Frozen/baked previews describe the old arrangement: discard them before the next run.
            world.ClearPreview();EditorUtility.SetDirty(world.profile);EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Selection.activeGameObject=world.gameObject;
            Debug.Log("Composición local: "+composition+"; mundo continuo: "+continuous+". No hay etapas ni ruta principal. Guarda y pulsa Play. Se conservan semilla, recetas, agua, sonido y presupuestos; el perfil anterior queda disponible.",world);
        }
    }
}
