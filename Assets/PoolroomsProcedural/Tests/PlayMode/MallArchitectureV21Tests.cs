#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class MallArchitectureV21Tests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void BridgeSupportsCharacterAndGlazedBalconyBlocksBothDirections(bool autonomous)
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            try
            {
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
                var p=MallPlan.Create(21,0,0,true,true,autonomous);var geometry=MallArchitecture.Shell(p);
                root=new GameObject("Refined mall collision test");root.transform.position=Vector3.up*100;
                var owner=root.AddComponent<PoolroomsChunk>();
                MeshFactory.Create("Structural collision",geometry.Structure,root.transform,profile,owner,true);
                MeshFactory.Create("Closed balcony glazing",geometry.Glass,root.transform,profile,owner,true);
                float x=p.Design.Bridges(1)[0],y=MallPlan.FloorHeight;
                actor=new GameObject("Bridge capsule");var body=actor.AddComponent<CharacterController>();
                body.height=1.8f;body.center=Vector3.up*.9f;body.radius=.32f;body.skinWidth=.025f;body.stepOffset=.25f;
                actor.transform.position=root.transform.position+new Vector3(x,y+.06f,-18);Physics.SyncTransforms();
                for(int i=0;i<72;i++)body.Move(new Vector3(0,-.08f,.5f));
                Assert.That(actor.transform.position.z,Is.GreaterThan(17.5f),"A railing cannot close either bridge entrance.");
                Assert.That(actor.transform.position.y-root.transform.position.y,Is.EqualTo(y).Within(.1f));
                var hole=p.Design.Voids(1).First();float mid=(hole.X0+hole.X1)*.5f;
                Vector3 edge=root.transform.position+new Vector3(mid,y+.6f,hole.Z0);
                Assert.That(Physics.Raycast(edge+Vector3.back,Vector3.forward,1.2f),Is.True,"Pane should collide from the gallery side.");
                Assert.That(Physics.Raycast(edge+Vector3.forward,Vector3.back,1.2f),Is.True,"Pane should collide from the court side.");
            }
            finally
            {if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
    }
}
#endif
