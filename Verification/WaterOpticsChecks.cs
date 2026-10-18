using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Poolrooms.Core;

static partial class Checks
{
    static void WaterOptics()
    {
        var previous=new PoolWaterOptics(PoolWaterDetail.Performance);
        foreach(PoolWaterDetail detail in Enum.GetValues(typeof(PoolWaterDetail)))
        {
            var optics=new PoolWaterOptics(detail);
            Check(optics.FarSmoothness>=previous.FarSmoothness&&optics.FarSmoothness<1,"Optical quality must preserve roughness and increase monotonically");
            Check(optics.FadeStart>=25&&optics.FadeDistance>=optics.FadeStart,"Smoothness fades before the nearby pool is visible");
            Check(optics.ProbeResolution>=128&&optics.ProbeResolution<=512&&(optics.ProbeResolution&(optics.ProbeResolution-1))==0,"Unbounded/non-power-of-two cubemap");
            foreach(PoolWaterAppearance look in Enum.GetValues(typeof(PoolWaterAppearance)))
            {
                var palette=PoolWaterModel.Look(look);
                Check(palette.RefractionDistance*optics.RefractionScale>0&&palette.RefractionDistance*optics.RefractionScale<palette.RefractionDistance,"Refraction refinement must reduce distortion without flattening the water");
                foreach(float depth in new[]{0,.1f,1,2,5})
                {
                    var transmission=PoolWaterModel.Transmission(palette,depth);
                    Check(transmission.X>0&&transmission.Y>0&&transmission.Z>0&&transmission.X<=1&&transmission.Y<=1&&transmission.Z<=1,"Optics loses bounded Beer-Lambert transmission");
                }
            }
            previous=optics;
        }
        Console.WriteLine("PASS: water optical budgets, bounded captures and clear-water transmission (core math).");
    }

    static void IntegrationSourceContracts()
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        string[][] configs={Array.Empty<string>(),new[]{"UNITY_EDITOR","UNITY_INCLUDE_TESTS","ENABLE_LEGACY_INPUT_MANAGER"},new[]{"POOLROOMS_HDRP_17","UNITY_EDITOR","UNITY_INCLUDE_TESTS"},new[]{"POOLROOMS_INPUT_SYSTEM","ENABLE_INPUT_SYSTEM","UNITY_INCLUDE_TESTS"}};
        int bindings=0;var references=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(p=>MetadataReference.CreateFromFile(p)).ToArray();
        foreach(string file in Directory.GetFiles(Path.Combine(root,"Assets"),"*.cs",SearchOption.AllDirectories))
        foreach(var config in configs)
        {
            var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file),new CSharpParseOptions(LanguageVersion.CSharp9,preprocessorSymbols:config));
            foreach(var error in tree.GetDiagnostics())Check(error.Severity!=DiagnosticSeverity.Error,file+": "+error);
            var syntax=tree.GetCompilationUnitRoot();
            foreach(var member in syntax.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                int tooltips=member.ChildNodes().OfType<AttributeListSyntax>().SelectMany(a=>a.Attributes)
                    .Count(a=>a.Name.ToString().Split('.').Last() is "Tooltip" or "TooltipAttribute");
                Check(tooltips<=1,file+": duplicate non-repeatable Unity Tooltip attribute");
            }
            if(!syntax.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n=>n.Identifier.ValueText=="LightUnit"))continue;
            // A deliberately reduced name-binding contract: use the file's actual imports with the
            // official Unity 6 namespace/enum shape. This is NOT a Unity/HDRP API compilation.
            string imports=string.Join("\n",syntax.Usings.Select(u=>u.ToString()));
            string fixture="namespace UnityEngine {} namespace Poolrooms.Core {} namespace UnityEngine.Rendering.HighDefinition {} "+
                "namespace UnityEngine.Rendering { public enum LightUnit { Lumen, Lux } } "+
                "namespace Poolrooms.HDRP { class Binding { object lumen=LightUnit.Lumen, lux=LightUnit.Lux; } }";
            Func<string,Diagnostic[]> diagnostics=usings=>CSharpCompilation.Create("LightUnitNameBinding",new[]{CSharpSyntaxTree.ParseText(usings+"\n"+fixture)},references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error).ToArray();
            Check(diagnostics(imports).Length==0,file+": LightUnit namespace is unresolved");
            Check(diagnostics(imports.Replace("using UnityEngine.Rendering;","")).Any(d=>d.Id=="CS0103"),"Namespace guard must reproduce the reported missing-import failure");
            bindings++;
        }
        Check(bindings>=6,"HDRP LightUnit binding checks did not execute");
        Console.WriteLine($"PASS: C# syntax/Tooltip contracts in four symbol configurations; {bindings} reduced LightUnit binding checks. Native Unity APIs NOT compiled.");
    }
}
