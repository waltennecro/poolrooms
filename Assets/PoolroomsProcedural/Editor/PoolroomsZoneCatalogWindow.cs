using System;
using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public sealed class PoolroomsZoneCatalogWindow : EditorWindow
    {
        private PoolroomsWorld world;
        private string search="";
        private int family,selected;
        private Vector2 scroll;
        private string status="";
        [MenuItem("Tools/Poolrooms/Catálogo de 170 zonas")]
        public static void Open(){GetWindow<PoolroomsZoneCatalogWindow>("Poolrooms · 170 zonas");}
        private void OnEnable()
        {
            minSize=new Vector2(460,450);
            if(Selection.activeGameObject!=null)world=Selection.activeGameObject.GetComponent<PoolroomsWorld>();
            if(world==null)world=UnityEngine.Object.FindFirstObjectByType<PoolroomsWorld>();
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("170 zonas · 30 familias",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Arquitectura acuática y espacios liminales sin criaturas ni anomalías.",EditorStyles.wordWrappedLabel);
            world=(PoolroomsWorld)EditorGUILayout.ObjectField("Mundo de la escena",world,typeof(PoolroomsWorld),true);
            search=EditorGUILayout.TextField("Buscar nombre, PR- o ambiente",search);
            string[] options=new string[ZoneCatalog.FamilyCount+1];options[0]="Todas las familias";
            for(int i=0;i<ZoneCatalog.FamilyCount;i++)options[i+1]=ZoneCatalog.FamilyName((ZoneFamily)i);
            family=EditorGUILayout.Popup("Familia",family,options);
            scroll=EditorGUILayout.BeginScrollView(scroll,GUILayout.MinHeight(170));
            for(int i=0;i<ZoneCatalog.Count;i++)
            {
                ZoneRecipe z=ZoneCatalog.Get(i);
                if(family>0&&(int)z.Family!=family-1)continue;
                if(search.Length>0&&(z.Code+" "+z.Name+" "+ZoneLighting.Get(ZoneLighting.DefaultFor(i)).Name).IndexOf(search,StringComparison.OrdinalIgnoreCase)<0)continue;
                GUIStyle style=i==selected?EditorStyles.miniButtonMid:EditorStyles.miniButton;
                if(GUILayout.Button(z.Code+"   "+z.Name,style))selected=i;
            }
            EditorGUILayout.EndScrollView();
            ZoneRecipe current=ZoneCatalog.Get(selected);
            EditorGUILayout.Space();EditorGUILayout.LabelField(current.Code+" · "+current.Name,EditorStyles.boldLabel);
            EditorGUILayout.LabelField(current.Description,EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Atmósfera base: "+ZoneLighting.Get(ZoneLighting.DefaultFor(selected)).Name);
            EditorGUILayout.LabelField("Agua: "+current.Pools+" · Altura base: "+current.Height.ToString("0.0")+" m");
            EditorGUILayout.HelpBox("Vista individual cambia el perfil a 1×1 / Single. Recorrer las 170 cambia a 17×10 / Catalog. Se conserva semilla, materiales y presupuesto de luces. Usa Undo para recuperar los valores del perfil.",MessageType.Info);
            using(new EditorGUI.DisabledScope(world==null||world.profile==null))
            {
                if(GUILayout.Button("Ver solo esta zona"))Apply(true);
                if(GUILayout.Button("Recorrer las 170 zonas (streaming)"))Apply(false);
                using(new EditorGUI.DisabledScope(!Application.isPlaying))
                    if(GUILayout.Button("Ir a esta zona en el mapa actual"))
                        status=world.VisitZone(selected)?"Trasladado; espera a que se cargue la sala.":"La zona no está en este mapa. Usa Recorrer las 170.";
            }
            if(world==null&&GUILayout.Button("Crear demo de 170 zonas"))
            {PoolroomsSetup.CreateCatalogDemo();world=UnityEngine.Object.FindFirstObjectByType<PoolroomsWorld>();}
            if(status.Length>0)EditorGUILayout.HelpBox(status,MessageType.Info);
        }
        private void Apply(bool single)
        {
            Undo.RecordObject(world.profile,"Cambiar catálogo Poolrooms");Undo.RecordObject(world,"Cambiar streaming Poolrooms");
            world.profile.zoneMode=single?ZoneMode.Single:ZoneMode.Catalog;
            world.profile.liminalGeneration=LiminalGeneration.Authored;
            world.unifiedExploration=false;world.profile.continuousWorld=false; // The catalogue is an explicit finite design showcase.
            world.profile.singleZone=selected;world.profile.zoneFamilyFilter=-1;
            world.profile.columns=single?1:17;world.profile.rows=single?1:10;
            world.streaming=true;world.generateOnStart=true;
            EditorUtility.SetDirty(world.profile);EditorUtility.SetDirty(world);
            if(Application.isPlaying)world.Rebuild();
            else
            {
                // Do not synchronously cook 170 rooms just to select the streamed showcase.
                if(single)world.BuildAllImmediate();else world.ClearPreview();
                if(world.viewer!=null)world.viewer.position=world.transform.position+Vector3.up*.1f;
                EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
                SceneView.lastActiveSceneView?.LookAt(world.transform.position+Vector3.up*2,Quaternion.Euler(28,35,0),24);
            }
            status=single?"Zona individual lista. Pulsa Play para recorrerla.":"Mapa de 170 zonas preparado. Pulsa Play; usa Ir a esta zona para saltar entre diseños.";
        }
    }
}
