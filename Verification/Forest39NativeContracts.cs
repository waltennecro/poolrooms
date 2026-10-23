using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static partial class Checks
{
    static void Forest39FactoryContract(string root, List<MetadataReference> references)
    {
        CompilationUnitSyntax Read(string name) => CSharpSyntaxTree.ParseText(File.ReadAllText(
            Path.Combine(root, "Assets/PoolroomsProcedural/Runtime", name + ".cs"))).GetCompilationUnitRoot();
        var pickup = Read("PoolroomsPickup");
        var factory = pickup.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(m => m.Identifier.ValueText == "CreateForestItem").ToString();
        var wrapper = Read("PoolroomsForest39").DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(m => m.Identifier.ValueText == "CreateItem").ToString();
        var properties = pickup.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(p => new[] { "SpawnKey", "InventoryKey", "CarryRadius" }.Contains(p.Identifier.ValueText)).ToArray();
        Check(properties.Length == 3 && properties.All(p => p.AccessorList.Accessors.Any(a =>
            a.IsKind(SyntaxKind.SetAccessorDeclaration) && a.Modifiers.Any(SyntaxKind.PrivateKeyword))),
            "Forest factory widened existing pickup setters instead of preserving encapsulation");
        var fields = pickup.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(f => f.Declaration.Variables.Any(v => new[] { "shape", "mass", "surface" }.Contains(v.Identifier.ValueText)));
        string members = string.Join("\n", properties.Select(p => p.ToString()).Concat(fields.Select(f => f.ToString())));
        string unity = @"
namespace UnityEngine {
 public struct Vector3 {
  public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 up=>new Vector3(0,1,0);
  public static Vector3 operator*(Vector3 p,float s)=>new Vector3(p.x*s,p.y*s,p.z*s);
 }
 public class Transform { public Vector3 position; }
 public class GameObject {
  public Transform transform=new Transform();public GameObject(string name){}
  public T AddComponent<T>() where T:new()=>new T();
 }
 public class Collider{} public class BoxCollider:Collider {public Vector3 size,center;}
}
";
        string Source(bool outside) => "using Poolrooms.Core;using UnityEngine;" + unity + @"
namespace Poolrooms {
 public class PoolroomsProfile{} public class PoolroomsChunk{}
 public static class MeshFactory {
  public static MeshData Last;
  public static object Create(string n,MeshData d,Transform t,PoolroomsProfile p,PoolroomsChunk o,bool c){Last=d;return null;}
 }
 public class PoolroomsPickup {" + members +
            " public float ProbeRead()=>mass+(shape==null?0:1)+(int)surface;" + (outside ? "" : factory) + "}" +
            "public static class PoolroomsForest39 {" + (outside ? factory : wrapper) + "} }";
        CSharpCompilation Compile(string source) => CSharpCompilation.Create("ForestFactoryProbe" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp9)) },
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var actual = Compile(Source(false));
        var errors = actual.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Check(errors.Length == 0, "Actual forest item factory binding: " + string.Join("\n", errors.Select(e => e.ToString())));
        var regression = Compile(Source(true)).GetDiagnostics();
        Check(regression.Any(d => d.Id == "CS0272") && regression.Any(d => d.Id == "CS0122"),
            "Factory probe does not reject external access to private pickup state");
        using var stream = new MemoryStream();
        Check(actual.Emit(stream).Success, "Reduced forest factory did not emit");
        var assembly = Assembly.Load(stream.ToArray());
        var method = assembly.GetType("Poolrooms.PoolroomsForest39").GetMethod("CreateItem");
        var vector = assembly.GetType("UnityEngine.Vector3");
        var position = Activator.CreateInstance(vector, 0f, 0f, 0f);
        foreach (int id in new[] { -1, 0, 1, 2, 3 })
        {
            var item = method.Invoke(null, new[] { (object)id, "probe", position, null });
            if (id < 0 || id > 2) { Check(item == null, "Invalid forest item created"); continue; }
            Check((string)item.GetType().GetProperty("InventoryKey").GetValue(item) == "forest39:" + id,
                "Forest factory changed its inventory identity");
            Check((float)item.GetType().GetProperty("CarryRadius").GetValue(item) > 0, "Forest item has no carry radius");
        }
        Console.WriteLine("PASS: actual forest pickup factory/wrapper compiled with reduced Unity types; CS0272/CS0122 replayed and rejected; three keepsakes/invalid IDs checked. NOT Unity API compilation.");
    }
}
