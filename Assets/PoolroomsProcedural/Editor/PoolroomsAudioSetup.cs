using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsAudioSetup
    {
        [MenuItem("Tools/Poolrooms/Sonido/Instalar ambiente y pasos")]
        public static void Install()
        {
            if(Application.isPlaying){Debug.LogWarning("Sal de Play antes de guardar los componentes de sonido.");return;}
            var world=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            if(world==null)
            {
                var worlds=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);
                if(worlds.Length==1)world=worlds[0];
            }
            var explorer=world!=null&&world.viewer!=null?world.viewer.GetComponent<PoolroomsExplorer>():null;
            if(explorer==null){Debug.LogWarning("Selecciona Poolrooms World con su Explorer asignado.");return;}
            var sound=explorer.GetComponent<PoolroomsAmbientAudio>();
            if(sound==null)sound=Undo.AddComponent<PoolroomsAmbientAudio>(explorer.gameObject);
            Undo.RecordObject(sound,"Sonido ambiental Poolrooms");sound.explorer=explorer;sound.world=world;
            EditorUtility.SetDirty(sound);EditorSceneManager.MarkSceneDirty(explorer.gameObject.scene);
            var listener=explorer.eyes!=null?explorer.eyes.GetComponent<AudioListener>():explorer.GetComponentInChildren<AudioListener>();
            if(listener==null)Debug.LogWarning("La cámara del Explorer necesita un AudioListener activo. Mantén un solo listener en la escena.",explorer);
            Debug.Log("Sonido instalado sin regenerar las salas. Guarda la escena y pulsa Play. Ajusta los niveles en PoolroomsAmbientAudio del Explorer.",sound);
        }
    }

    // Import once; no procedural synthesis, file decoding or asset searches during movement.
    public sealed class PoolroomsAudioImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if(!assetPath.Contains("/PoolroomsProcedural/Runtime/Resources/PoolroomsAudio/")&&!assetPath.Contains("/PoolroomsProcedural/Runtime/Resources/EncounterAudio/")&&!assetPath.Contains("/PoolroomsProcedural/Runtime/Resources/ImmersionAudio/"))return;
            var importer=(AudioImporter)assetImporter;
            var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;
            importer.defaultSampleSettings=settings;importer.forceToMono=!assetPath.EndsWith("Loop.wav");
        }
    }
}
