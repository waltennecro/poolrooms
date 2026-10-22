using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static partial class Checks
{
    static void UnityNativeLifetimes()
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../Assets/PoolroomsProcedural"));
        var classes=Directory.GetFiles(root,"*.cs",SearchOption.AllDirectories)
            .SelectMany(file=>CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot().DescendantNodes()
                .OfType<ClassDeclarationSyntax>().Select(type=>(file,type))).ToArray();
        var behaviours=new HashSet<string>{"MonoBehaviour"};bool changed;
        do{changed=false;foreach(var entry in classes)if(entry.type.BaseList!=null&&entry.type.BaseList.Types.Any(b=>behaviours.Contains(b.Type.ToString().Split('.').Last())))changed|=behaviours.Add(entry.type.Identifier.ValueText);}while(changed);
        var native=new HashSet<string>{"MaterialPropertyBlock","Mesh","Material","GameObject","Texture2D","RenderTexture"};
        foreach(var entry in classes.Where(e=>behaviours.Contains(e.type.Identifier.ValueText)))
        foreach(var field in entry.type.Members.OfType<FieldDeclarationSyntax>().Where(f=>!f.Modifiers.Any(t=>t.Text=="static")))
        foreach(var variable in field.Declaration.Variables.Where(v=>v.Initializer!=null))
        foreach(var creation in variable.Initializer.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            bool deferred=creation.Ancestors().TakeWhile(a=>a!=variable).Any(a=>a is AnonymousFunctionExpressionSyntax);
            Check(deferred||!native.Contains(creation.Type.ToString().Split('.').Last()),entry.file+": Unity native allocation in instance initializer "+variable.Identifier);
        }
        var tree=CSharpSyntaxTree.ParseText("class Bad:MonoBehaviour { MaterialPropertyBlock block=new MaterialPropertyBlock(); }");
        Check(tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value is ObjectCreationExpressionSyntax,"Native-allocation guard fixture must exercise the reported failure");
        Console.WriteLine("PASS: Unity resource instance-field allocation audit (source guard; native AddComponent tests remain separate).");
    }
}
