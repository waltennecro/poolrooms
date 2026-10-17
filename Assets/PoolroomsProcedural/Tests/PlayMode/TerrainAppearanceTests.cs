#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Poolrooms.Tests
{
    public sealed class TerrainAppearanceTests
    {
        [Test]
        public void GroundUploadsNativeLayerWeightsAndUsesSeparateRoofAndFurnitureMaterials()
        {
            var root=new GameObject("v34 material test");PoolroomsProfile profile=null;
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var set=new TerrainMaterialSet(owner);
                Assert.That(set.Roof[(int)Surface.Accent],Is.Not.SameAs(set.Interior[(int)Surface.Accent]));
                Assert.That(set.Roof[(int)Surface.Tile],Is.Not.SameAs(set.Roof[(int)Surface.Accent]));
                var data=TerrainArchitecture.Ground(TerrainRegionPlan.Create(94,0,0),0,0,32);
                var go=MeshFactory.Create("v34 ground",data,root.transform,profile,owner,true,false,set.ForGround(TerrainVisualQuality.High));
                var physics=MeshFactory.CreateCollider("v34 physics only",data,root.transform,owner);Assert.That(physics.GetComponent<Renderer>(),Is.Null);Assert.That(physics.sharedMesh.vertexCount,Is.EqualTo(data.Positions.Count));Assert.That(physics.sharedMesh.triangles.Length,Is.EqualTo(data.TriangleCount*3));
                var mesh=go.GetComponent<MeshFilter>().sharedMesh;Assert.That(mesh.colors.Length,Is.EqualTo(mesh.vertexCount));
                for(int i=0;i<mesh.vertexCount;i+=31)
                {
                    Assert.That(mesh.colors[i].r,Is.EqualTo(data.BlendWeights[i].X).Within(.00001f));
                    Assert.That(mesh.colors[i].a,Is.EqualTo(1));
                }
                if(set.GroundHigh[0].shader.name=="HDRP/LayeredLit")
                {
                    Assert.That(set.GroundHigh[0].GetFloat("_LayerCount"),Is.EqualTo(2));
                    Assert.That(set.GroundHigh[0].IsKeywordEnabled("_LAYER_MASK_VERTEX_COLOR_MUL"),Is.True);
                }
            }
            finally{Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [UnityTest]
        public IEnumerator QualitySwitchKeepsSupportAndGrassRemainsBoundedWhileMoving()
        {
            string key=PoolroomsGameSettings.PreferencesKey,backup=PlayerPrefs.GetString(key);bool had=PlayerPrefs.HasKey(key);
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause;var cursor=UnityEngine.Cursor.lockState;bool visible=UnityEngine.Cursor.visible;
            try
            {
                PlayerPrefs.DeleteKey(key);Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;profile.continuousWorld=false;
                profile.enableTerrainRegions=true;profile.enableGiantMalls=false;profile.terrainEntranceChance=0;profile.terrainViewRadius=2;
                root=new GameObject("v34 appearance world");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;world.unifiedExploration=false;
                actor=new GameObject("v34 explorer");actor.SetActive(false);var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.world=world;explorer.externalInput=true;world.viewer=actor.transform;
                var eyes=new GameObject("v34 eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                float deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.Terrain.VisitNearest(),Is.True);deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True);
                deadline=Time.realtimeSinceStartup+90;while(world.Terrain.LoadedCount<25&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.Terrain.LoadedCount,Is.EqualTo(25));for(int frame=0;frame<4;frame++)yield return null;
                var settings=actor.GetComponent<PoolroomsGameSettings>();Vector3 position=actor.transform.position;
                var support=Support(actor.transform.position);var supportMesh=((MeshCollider)support).sharedMesh;
                int colliders=world.Terrain.RegionRoot.GetComponentsInChildren<Collider>().Length;
                explorer.SetPaused(true);Time.timeScale=0;settings.Preferences.terrainVisuals=TerrainVisualQuality.High;settings.Preferences.terrainGrass=TerrainGrassQuality.High;settings.Change(SettingsChange.Graphics);
                for(int frame=0;frame<12;frame++)yield return null;
                Assert.That(Vector3.Distance(actor.transform.position,position),Is.LessThan(.01f));
                Assert.That(Support(position),Is.SameAs(support));Assert.That(((MeshCollider)support).sharedMesh,Is.SameAs(supportMesh));
                Assert.That(world.Terrain.RegionRoot.GetComponentsInChildren<Collider>().Length,Is.EqualTo(colliders));
                Time.timeScale=1;explorer.SetPaused(false);var grass=world.GetComponent<PoolroomsTerrainGrass>();
                deadline=Time.realtimeSinceStartup+60;
                while(grass.PatchCount<25&&Time.realtimeSinceStartup<deadline){Assert.That(grass.RenderedTriangles,Is.LessThanOrEqualTo(60480));yield return null;}
                Assert.That(grass.PatchCount,Is.EqualTo(25));
                for(int step=0;step<4;step++)
                {
                    float x=step*16+8,z=8;explorer.Teleport(world.Terrain.RegionRoot.TransformPoint(new Vector3(x,world.Terrain.Plan.Height(x,z)+.15f,z)));
                    for(int frame=0;frame<40;frame++){Assert.That(grass.PatchCount,Is.LessThanOrEqualTo(25));Assert.That(grass.RenderedTriangles,Is.LessThanOrEqualTo(60480));yield return null;}
                }
                settings.Preferences.terrainVisuals=TerrainVisualQuality.Economy;settings.Preferences.terrainGrass=TerrainGrassQuality.Off;settings.Change(SettingsChange.Graphics);settings.Flush();yield return null;yield return null;
                Assert.That(grass.PatchCount,Is.Zero);Assert.That(grass.RenderedTriangles,Is.Zero);Assert.That(Support(actor.transform.position),Is.Not.Null);
                var saved=JsonUtility.FromJson<GamePreferences>(PlayerPrefs.GetString(key));Assert.That(saved.terrainVisuals,Is.EqualTo(TerrainVisualQuality.Economy));
                Assert.That(saved.terrainGrass,Is.EqualTo(TerrainGrassQuality.Off));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                if(had)PlayerPrefs.SetString(key,backup);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                Time.timeScale=time;AudioListener.pause=audio;UnityEngine.Cursor.lockState=cursor;UnityEngine.Cursor.visible=visible;
            }
        }
        private static Collider Support(Vector3 position)
        {RaycastHit hit;Assert.That(Physics.Raycast(position+Vector3.up*.5f,Vector3.down,out hit,1,~0,QueryTriggerInteraction.Ignore),Is.True);return hit.collider;}
    }
}
#endif
