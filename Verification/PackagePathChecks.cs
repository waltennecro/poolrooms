using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static partial class Checks
{
    static void PackagePathContracts()
    {
        string repo=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        string editor=Path.Combine(repo,"Assets/PoolroomsProcedural/Editor");
        string locator=File.ReadAllText(Path.Combine(editor,"PoolroomsPackagePaths.cs"));
        // Compile the entire actual locator with a narrowly shaped AssetDatabase
        // backed by real temporary folders. These substitutes are NOT Unity.
        string bridge=@"namespace UnityEngine {
            public enum HideFlags { HideAndDontSave }
            public class Object { public HideFlags hideFlags; public static int Live; public static void DestroyImmediate(Object o){Live--;} }
            public class ScriptableObject:Object { public static T CreateInstance<T>() where T:ScriptableObject,new(){Live++;return new T();} }
        }
        namespace UnityEditor {
            public class MonoScript:UnityEngine.Object { public static MonoScript FromScriptableObject(UnityEngine.ScriptableObject o){return new MonoScript();} }
            public static class AssetDatabase { public static string ScriptPath; public static string GetAssetPath(UnityEngine.Object o){return ScriptPath;}
            public static bool IsValidFolder(string path){return System.IO.Directory.Exists(path);} }
        }";
        var options=new CSharpParseOptions(LanguageVersion.CSharp9);
        var references=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(p=>MetadataReference.CreateFromFile(p));
        var compilation=CSharpCompilation.Create("PackageLocatorContract",new[]{CSharpSyntaxTree.ParseText(locator,options),CSharpSyntaxTree.ParseText(bridge,options)},references,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var binary=new MemoryStream();var emit=compilation.Emit(binary);
        Check(emit.Success,"Actual package locator failed reduced compilation: "+string.Join("\n",emit.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));
        var assembly=Assembly.Load(binary.ToArray());var type=assembly.GetType("Poolrooms.Editor.PoolroomsPackagePaths");
        var database=assembly.GetType("UnityEditor.AssetDatabase");var engineObject=assembly.GetType("UnityEngine.Object");
        string Root()=>(string)type.GetProperty("Root").GetValue(null);
        string Asset(string p)=>(string)type.GetMethod("Asset").Invoke(null,new object[]{p});
        void Validate()=>type.GetMethod("ValidateDemoTextures").Invoke(null,null);
        bool Throws<T>(Action action) where T:Exception {try{action();return false;}catch(TargetInvocationException e){return e.InnerException is T;}}
        string previous=Directory.GetCurrentDirectory(),sandbox=Path.Combine(Path.GetTempPath(),"poolrooms-paths-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        int layouts=0;
        try
        {
            Directory.SetCurrentDirectory(sandbox);
            foreach(string root in new[]{"Assets/PoolroomsProcedural","Assets/Poolrooms_Compilacion_Corregida_Assets/PoolroomsProcedural","Assets/Mi juego/Áreas liminales/PoolroomsProcedural","Assets/Carpeta renombrada"})
            {
                Directory.CreateDirectory(root+"/Editor");Directory.CreateDirectory(root+"/Textures");
                File.WriteAllText(root+"/Editor/PoolroomsPackagePaths.cs",locator);
                foreach(string material in new[]{"Ceramic","Plaster","Metal"})foreach(string map in new[]{"Albedo","Normal","Mask"})
                    File.WriteAllText(root+"/Textures/"+material+"_"+map+".png","path fixture, not an image validation");
                type.GetField("cachedRoot",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null);
                database.GetField("ScriptPath").SetValue(null,root+"/Editor/PoolroomsPackagePaths.cs");
                Check(Root()==root&&Root()==root,"Direct/nested locator root mismatch");Validate();
                foreach(string resource in new[]{"Textures/Ceramic_Albedo.png","Resources/BiomeMaterials","Resources/PoolroomsBiomeMaterials.asset","Resources/TerrainMaterials","Resources/PoolroomsTerrainMaterials.asset","Resources/PoolroomsTerrainLayeredTemplate.mat"})
                    Check(Asset(resource)==root+"/"+resource,"Generated material escaped its actual package");
                foreach(string invalid in new[]{"../elsewhere","Resources/../../elsewhere","/tmp/file","C:/file","./file",""})
                    Check(Throws<ArgumentException>(()=>Asset(invalid)),"Unsafe relative asset path accepted");
                string texture=root+"/Textures/Ceramic_Albedo.png";File.Delete(texture);
                Check(Throws<FileNotFoundException>(Validate),"Missing real texture was not diagnosed");File.WriteAllText(texture,"restored fixture");
                string moved=root+"_moved";Directory.Move(root,moved);
                database.GetField("ScriptPath").SetValue(null,(moved+"/Editor/PoolroomsPackagePaths.cs").Replace('/','\\'));
                Check(Root()==moved,"Moved folder reused a stale cached root");Validate();
                Check((int)engineObject.GetField("Live").GetValue(null)==0,"Temporary locator instance leaked");
                layouts++;
            }
            type.GetField("cachedRoot",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null);
            database.GetField("ScriptPath").SetValue(null,"");
            Check(Throws<InvalidOperationException>(()=>Root()),"Missing MonoScript anchor did not fail clearly");
            Check((int)engineObject.GetField("Live").GetValue(null)==0,"Failed lookup leaked its locator");
        }
        finally{Directory.SetCurrentDirectory(previous);Directory.Delete(sandbox,true);}
        foreach(string file in Directory.GetFiles(editor,"*.cs"))
        {
            var syntax=CSharpSyntaxTree.ParseText(File.ReadAllText(file),options).GetRoot();
            foreach(var literal in syntax.DescendantNodes().OfType<LiteralExpressionSyntax>())
                Check(!literal.Token.ValueText.StartsWith("Assets/PoolroomsProcedural/",StringComparison.Ordinal),file+": fixed installation path returned");
        }
        var setup=CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(editor,"PoolroomsSetup.cs")),options).GetRoot();
        var demo=setup.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m=>m.Identifier.ValueText=="CreateDemo"&&m.ParameterList.Parameters.Count==1);
        var calls=demo.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        Check(calls.Single(c=>c.Expression.ToString()=="PoolroomsPackagePaths.ValidateDemoTextures").SpanStart<calls.Single(c=>c.Expression.ToString()=="EditorSceneManager.NewScene").SpanStart,"Demo replaced the scene before validating textures");
        foreach(string material in new[]{"Ceramic","Plaster","Metal"})foreach(string map in new[]{"Albedo","Normal","Mask"})
            Check(File.Exists(Path.Combine(repo,"Assets/PoolroomsProcedural/Textures",material+"_"+map+".png")),"Shipped base texture missing");
        Console.WriteLine($"PASS: actual package locator compiled with reduced Unity stand-ins; {layouts} physical installation layouts + four moves/cache invalidations; nine shipped textures present; fixed Editor resource paths rejected. Native Unity AssetDatabase unrun.");
    }
}
