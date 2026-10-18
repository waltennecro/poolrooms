#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class RainHouseWorldTests
    {
        [UnityTest]
        public IEnumerator EveryLodgeHasWalkablePorchAndDoorWithPhysicalRoof()
        {
            foreach(int recipe in new[]{0,626,1252,1878,2499})
            {
                var cfg=new LayoutConfig{CellSize=28,DoorWidth=6,DoorHeight=5.1f,Continuous=true,Procedural=LiminalGeneration.RainHouses};
                var room=new Room{Exits=15};RainHousePlanner.DescribeRecipe(room,cfg,recipe);var g=RainHouseArchitecture.Build(room,cfg);
                var root=new GameObject("Rain lodge native structure");root.transform.position=new Vector3(29000,1300,29000);var actor=new GameObject("Rain lodge walker");
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("Lodge walls",g.Structure,root.transform,owner);MeshFactory.CreateCollider("Physical roofs and wood floors",g.CollisionOnly,root.transform,owner);
                    var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.3f;body.minMoveDistance=0;
                    foreach(var h in room.Plan.Rain.Houses)
                    {
                        var begin=h.ToRoom(new V3(0,.05f,-h.Depth*.5f-1));var direction=h.Turns==0?Vector3.forward:Vector3.back;
                        body.enabled=false;actor.transform.position=root.transform.position+new Vector3(begin.X,begin.Y,begin.Z);body.enabled=true;Physics.SyncTransforms();
                        for(int i=0;i<60;i++){body.Move(direction*.05f-Vector3.up*.02f);if(i%15==0)yield return null;}
                        Assert.That(Vector3.Dot(actor.transform.position-root.transform.position-new Vector3(begin.X,0,begin.Z),direction),Is.GreaterThan(2.7f),"Door, sill or interior spine blocks walking");
                        Assert.That(actor.transform.position.y-root.transform.position.y,Is.InRange(-.03f,.15f),"Lodge floor swallowed the player");
                        Assert.That(Physics.Raycast(root.transform.position+new Vector3(h.X,8,h.Z),Vector3.down,out var hit,9),Is.True,"Missing roof collision");
                        Assert.That(hit.point.y-root.transform.position.y,Is.GreaterThan(3),"Only ground exists under the roof");
                    }
                }
                finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
            }
        }
        [Test]
        public void RainResourcesAndIndependentPbrMaterialsLoad()
        {
            var audio=Resources.Load<AudioClip>("PoolroomsRain/QuietRainLoop");Assert.That(audio,Is.Not.Null);Assert.That(audio.length,Is.EqualTo(16).Within(.05));
            var streak=Resources.Load<Texture2D>("PoolroomsRain/RainStreak");Assert.That(streak,Is.Not.Null);
            foreach(string set in new[]{"RainOak","RainCedar","RainPaving","RainSlate"})foreach(string suffix in new[]{"Albedo","Normal","Mask"})Assert.That(Resources.Load<Texture2D>(PoolroomsBiomeMaterialFactory.TextureRoot+set+"_"+suffix),Is.Not.Null);
            foreach(var theme in new[]{SurfaceTheme.RainLodge,SurfaceTheme.RainRoof,SurfaceTheme.RainInterior})
            {
                var m=PoolroomsBiomeMaterialFactory.Create(theme,Surface.Tile);
                try{Assert.That(m.shader,Is.Not.Null);Assert.That(m.renderQueue,Is.LessThan(3000));}
                finally{Object.DestroyImmediate(m);}
            }
        }
    }
}
#endif
