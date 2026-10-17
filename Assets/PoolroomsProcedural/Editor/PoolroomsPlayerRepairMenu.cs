using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsPlayerRepairMenu
    {
        [MenuItem("Tools/Poolrooms/Jugador v26/Reparar apoyo y cámara")]
        public static void Repair()
        {
            if(Application.isPlaying){Debug.LogWarning("Repara el jugador fuera de Play y guarda la escena.");return;}
            var selected=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsExplorer>():null;
            var all=Object.FindObjectsByType<PoolroomsExplorer>(FindObjectsSortMode.None);var actor=selected!=null?selected:all.Length==1?all[0]:null;
            if(actor==null){Debug.LogWarning("Selecciona Explorer con PoolroomsExplorer.");return;}
            var body=actor.GetComponent<CharacterController>();Undo.RecordObject(body,"Reparar cápsula del jugador");actor.ConfigureBody();
            if(actor.eyes==null)
            {var camera=actor.GetComponentInChildren<Camera>();if(camera!=null){Undo.RecordObject(actor,"Asignar cámara del jugador");actor.eyes=camera.transform;}}
            if(actor.eyes!=null&&actor.eyes.IsChildOf(actor.transform))
            {
                float height=Vector3.Dot(actor.eyes.position-actor.transform.position,actor.transform.up);
                if(height<.5f*actor.transform.lossyScale.y)
                {Undo.RecordObject(actor.eyes,"Restaurar altura de cámara");actor.eyes.position+=actor.transform.up*((body.height-.15f)*actor.transform.lossyScale.y-height);EditorUtility.SetDirty(actor.eyes);}
            }
            EditorUtility.SetDirty(body);EditorUtility.SetDirty(actor);EditorSceneManager.MarkSceneDirty(actor.gameObject.scene);
            Debug.Log("Jugador corregido: pivote en los pies, cápsula con colisiones y cámara sobre el suelo. Guarda la escena. El doble salto está activo automáticamente. Si el mapa estaba congelado sin colliders, regenera o vuelve a hornearlo.",actor);
        }
    }
}
