using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsValidationMenu
    {
        [MenuItem("Tools/Poolrooms/Validación/Capturar 30 segundos de rendimiento")]
        public static void Capture()
        {
            if(!Application.isPlaying){Debug.LogWarning("Inicia Play y espera a que cargue el mundo antes de capturar rendimiento.");return;}
            var world=Object.FindFirstObjectByType<PoolroomsWorld>();if(world==null||world.viewer==null)return;
            var current=world.GetComponent<PoolroomsPerformanceCapture>();if(current!=null&&current.enabled){Debug.LogWarning("Ya hay una captura de rendimiento en curso.");return;}
            string path=EditorUtility.SaveFilePanel("Guardar captura real de rendimiento",Application.temporaryCachePath,"Poolrooms_Rendimiento","json");
            if(string.IsNullOrEmpty(path))return;
            // Keep existing reports; replace only the completed diagnostic component.
            if(current!=null)Object.DestroyImmediate(current);
            var capture=world.gameObject.AddComponent<PoolroomsPerformanceCapture>();
            capture.Begin(world,path);Debug.Log("Poolrooms: captura iniciada. Recorre la zona sin abrir menús; el informe se guarda tras 30 s de exploración cargada.");
        }
    }
}
