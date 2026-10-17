using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsLightingMenu
    {
        [MenuItem("Tools/Poolrooms/Iluminación/Ambientes por zona (regenerar al jugar)")]
        public static void Authored(){Apply(LightingDistribution.ZoneDefaults);}
        [MenuItem("Tools/Poolrooms/Iluminación/Ambientes por semilla (regenerar al jugar)")]
        public static void Seeded(){Apply(LightingDistribution.Seeded);}
        [MenuItem("Tools/Poolrooms/Iluminación/Iluminación clásica (regenerar al jugar)")]
        public static void Classic(){Apply(LightingDistribution.Classic);}
        private static void Apply(LightingDistribution mode)
        {
            var world=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            if(world==null)
            {
                var worlds=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);
                if(worlds.Length==1)world=worlds[0];
            }
            if(world==null||world.profile==null){Debug.LogWarning("Selecciona el Poolrooms World de tu escena.");return;}
            Undo.RecordObject(world.profile,"Ambiente Poolrooms");Undo.RecordObject(world,"Regeneración de ambientes");
            world.profile.lighting=mode;world.generateOnStart=true;
            EditorUtility.SetDirty(world.profile);EditorUtility.SetDirty(world);
            if(Application.isPlaying)world.Rebuild();
            else EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log("Ambiente: "+mode+". Guarda y pulsa Play para regenerar con tu semilla y dimensiones actuales. Si usabas geometría congelada/lightmaps, deberás volver a congelar y hornear después de elegir la iluminación.",world);
        }
    }
}
