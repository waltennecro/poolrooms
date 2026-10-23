using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static partial class Checks
{
    static void TravelNativeContract(string root,List<MetadataReference> references)
    {
        string Read(string file)=>File.ReadAllText(Path.Combine(root,"Assets/PoolroomsProcedural/Runtime",file+".cs"));
        var explorer=CSharpSyntaxTree.ParseText(Read("PoolroomsExplorer")).GetRoot();
        foreach(string property in new[]{"Travel","IsPaused","InputCaptured","IsAquaticRiding","IsFlying","IsReady","Vehicle","Seating","Views","Inventory","Aim"})
            Check(explorer.DescendantNodes().OfType<PropertyDeclarationSyntax>().Any(p=>p.Identifier.ValueText==property&&p.Modifiers.Any(t=>t.Text=="public")),"Travel references missing/private Explorer API: "+property);
        var view=CSharpSyntaxTree.ParseText(Read("PoolroomsViewRig")).GetRoot();
        Check(view.DescendantNodes().OfType<PropertyDeclarationSyntax>().Any(p=>p.Identifier.ValueText=="PhotoActive"),"Photo gate does not match actual camera API");
        const string stubs=@"
using System;using Poolrooms.Core;
namespace UnityEngine {
 public class Object{}
 public class Component:Object {public Transform transform;public GameObject gameObject;public string name;public T GetComponent<T>()=>default;public T GetComponentInParent<T>()=>default;}
 public class MonoBehaviour:Component {public bool isActiveAndEnabled;public static void Destroy(Object o){} }
 public class GameObject:Object {public Transform transform;public bool activeInHierarchy;public GameObject(string name){} public T AddComponent<T>()where T:new()=>new T();}
 public class Transform:Component {public Vector3 position,forward,right;public void SetParent(Transform p,bool world){} public Vector3 TransformPoint(Vector3 p)=>p;public Vector3 InverseTransformPoint(Vector3 p)=>p;public bool IsChildOf(Transform t)=>false;}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public float sqrMagnitude=>x*x+y*y;}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 up=>new Vector3(0,1,0);public static Vector3 zero=>new Vector3();public static Vector3 down=>new Vector3(0,-1,0);public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public float sqrMagnitude=>x*x+y*y+z*z;public Vector3 normalized=>this;public static float Dot(Vector3 a,Vector3 b)=>0;public static Vector3 operator+(Vector3 a,Vector3 b)=>a;public static Vector3 operator-(Vector3 a,Vector3 b)=>a;public static Vector3 operator*(Vector3 a,float b)=>a;public static Vector3 operator/(Vector3 a,float b)=>a;}
 public class Collider:Component {public Rigidbody attachedRigidbody;}
 public class Rigidbody:Component{}
 public class MeshCollider:Collider{}
 public struct RaycastHit {public Collider collider;public float distance;public Vector3 point,normal;}
 [Flags]public enum CollisionFlags{None=0,Sides=1,Above=2,Below=4}
 public class CharacterController:Collider{public float radius;public bool enabled;public CollisionFlags Move(Vector3 d)=>CollisionFlags.None;}
 public enum QueryTriggerInteraction{Ignore}
 public static class Physics{public static int RaycastNonAlloc(Vector3 o,Vector3 d,RaycastHit[] h,float r,int l,QueryTriggerInteraction q)=>0;}
 public class Material:Object{}
 public class LineRenderer:Component{public Material sharedMaterial;public bool useWorldSpace,receiveShadows,enabled,generateLightingData;public int positionCount,numCapVertices;public float startWidth,endWidth;public Rendering.ShadowCastingMode shadowCastingMode;public void SetPosition(int i,Vector3 p){} }
 public static class Time{public static float unscaledTime;}
 public static class PlayerPrefs{public static string GetString(string k,string d)=>d;public static void SetString(string k,string v){}public static void Save(){} }
 public static class JsonUtility{public static T FromJson<T>(string t)=>default;public static string ToJson(object t)=>string.Empty;}
 [AttributeUsage(AttributeTargets.Class)]public class DisallowMultipleComponent:Attribute{}
 [AttributeUsage(AttributeTargets.Class)]public class RequireComponent:Attribute{public RequireComponent(Type a,Type b){} }
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode{Off}}
namespace Poolrooms {
 public class PoolroomsProfile{public UnityEngine.Material metal;}
 public class PoolroomsWorld:UnityEngine.MonoBehaviour{public bool ViewerReady;public PoolroomsProfile profile;}
 public class PoolroomsChunk:UnityEngine.MonoBehaviour{}
 public class PoolroomsPickup:UnityEngine.MonoBehaviour{}
 public class PoolroomsSoundscape:UnityEngine.MonoBehaviour{public void PlayStation(StationSound s,UnityEngine.Vector3 p){} }
 public class PoolroomsInventory{public void Toast(string s,float t=0){} }
 public class PoolroomsViewRig{public bool PhotoActive;}
 public class PoolroomsSeating{public bool IsSeated;}
 public class PoolroomsExplorer:UnityEngine.MonoBehaviour {public bool IsPaused,InputCaptured,IsAquaticRiding,IsFlying,IsReady;public object Vehicle;public PoolroomsWorld world;public PoolroomsSeating Seating;public PoolroomsViewRig Views;public PoolroomsInventory Inventory;public UnityEngine.Transform Aim;}
}
";
        var parsed=new CSharpParseOptions(LanguageVersion.CSharp9);
        CSharpCompilation Compile(string actual)=>CSharpCompilation.Create("TravelRuntimeProbe"+Guid.NewGuid().ToString("N"),
            new[]{CSharpSyntaxTree.ParseText(stubs,parsed),CSharpSyntaxTree.ParseText(actual,parsed)},references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Check(!Compile(string.Empty).GetDiagnostics().Any(d=>d.Severity==DiagnosticSeverity.Error),"Reduced travel interfaces do not compile on their own");
        string actual=Read("PoolroomsTravel");var compiled=Compile(actual);
        var errors=compiled.GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error).ToArray();
        Check(errors.Length==0,"Full actual travel component reduced semantic compilation: "+string.Join("\n",errors.Select(e=>e.ToString())));
        var negative=Compile(actual.Replace(".PhotoActive",".IsPhotoMode")).GetDiagnostics();
        Check(negative.Any(e=>e.Id=="CS1061"),"Travel API probe fails to reject a nonexistent photo property");
        using var stream=new MemoryStream();Check(compiled.Emit(stream).Success,"Travel component did not emit against reduced interfaces");
        Console.WriteLine("PASS: entire actual travel component compiled against real Core and reduced Unity interfaces; Explorer/camera API names checked, missing photo API rejected. NOT native Unity compilation.");
    }
}
