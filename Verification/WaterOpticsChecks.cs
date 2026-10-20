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
        Check(Math.Abs(PoolWaterModel.Fresnel(1)-.02037f)<.0001f&&Math.Abs(PoolWaterModel.Fresnel(0)-1)<.0001f,"Water must reflect about 2% at normal incidence and all light at grazing angles");
        float previousFresnel=1;
        for(int i=0;i<=1000;i++)
        {float f=PoolWaterModel.Fresnel(i/1000f);Check(f<=previousFresnel+1e-6f&&f>=.02f&&f<=1,"Invalid water Fresnel energy");previousFresnel=f;}
        for(int t=0;t<160;t++)for(int d=0;d<400;d++)
        {float packet=PoolWaterModel.RipplePacket(d*.025f,t*.025f);Check(float.IsFinite(packet)&&Math.Abs(packet)<=1,"Dispersive ripple packet adds unbounded energy");}
        Check(PoolWaterModel.RipplePacket(1,float.NaN)==0&&PoolWaterModel.RipplePacket(-1,0)==0,"Invalid wave input contaminates the surface");
        foreach(float dt in new[]{1f/240,1f/60,1f/20})
        {
            var occlusion=new OccludedSound(1,18000);
            for(int i=0;i<(int)(2/dt);i++)occlusion=SpatialAudioModel.Step(occlusion,true,false,dt);
            Check(Math.Abs(occlusion.Gain-.3f)<.001f&&Math.Abs(occlusion.Cutoff-2100)<1,"Frame-dependent or instant wall occlusion");
            for(int i=0;i<(int)(2/dt);i++)occlusion=SpatialAudioModel.Step(occlusion,false,false,dt);
            Check(Math.Abs(occlusion.Gain-1)<.001f&&Math.Abs(occlusion.Cutoff-18000)<1,"Opening a passage does not restore sound");
        }
        var pools=new[]{new WaterRegion{CenterX=4,Width=4,Depth=8,Bottom=-1,Level=-.08f},new WaterRegion{CenterX=-12,Width=3,Depth=3,Bottom=-1,Level=-.08f}};
        var anchor=SpatialAudioModel.WaterAnchor(pools,new V3(0,1.6f,0),out bool found);
        Check(found&&Math.Abs(anchor.X-2)<.001f&&anchor.Y>pools[0].Level,"Water ambience detached from closest pool");
        anchor=SpatialAudioModel.WaterAnchor(pools,new V3(4.1f,1.6f,0),out found);
        Check(found&&Math.Abs(anchor.X-4)<.001f,"Swimming ambience follows listener instead of pool centre");
        var rounded=new WaterRegion{Width=4,Depth=4,Radius=1,Bottom=-1,Level=-.08f};
        anchor=SpatialAudioModel.WaterAnchor(new[]{rounded},new V3(-8,1.6f,-8),out found);
        Check(found&&rounded.Contains(anchor.X,anchor.Z),"Rounded-pool sound anchor lies on the surrounding dry floor");
        foreach(float radius in new[]{0,.5f,2})for(int direction=0;direction<32;direction++)
        {
            var shape=new WaterRegion{CenterX=-3,CenterZ=7,Width=4,Depth=6,Radius=radius,Bottom=-1,Level=-.08f};
            var listener=new V3(-3+12*MathF.Cos(direction*MathF.PI/16),1.6f,7+12*MathF.Sin(direction*MathF.PI/16));
            anchor=SpatialAudioModel.WaterAnchor(new[]{shape},listener,out found);
            Check(found&&shape.Contains(anchor.X,anchor.Z),"Spatial source escapes a rectangular, rounded or capsule pool");
        }
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
#if !POOLROOMS_HISTORICAL_CORE
        Check((int)PoolWaterAppearance.ClearCeramic==0&&(int)PoolWaterAppearance.Custom==3&&(int)PoolWaterAppearance.CrystalClear==4,"Serialized water appearance IDs changed");
        foreach(PoolWaterSetting setting in Enum.GetValues(typeof(PoolWaterSetting)))
        foreach(PoolWaterAppearance appearance in Enum.GetValues(typeof(PoolWaterAppearance)))
        {
            if(appearance==PoolWaterAppearance.Custom)continue;
            var look=PoolWaterModel.Look(appearance,setting);var last=new V3(1,1,1);
            for(int i=0;i<=400;i++)
            {
                var transmission=PoolWaterModel.Transmission(look,i*.05f);
                Check(transmission.X<=last.X&&transmission.Y<=last.Y&&transmission.Z<=last.Z,"Clear water gets brighter with depth");last=transmission;
            }
            var shallow=PoolWaterModel.Transmission(look,.5f);var twoMeters=PoolWaterModel.Transmission(look,2);
            Check(shallow.X>.98f&&shallow.Y>.98f&&shallow.Z>.98f,"Shallow water hides its floor");
            Check(twoMeters.X>.95f&&twoMeters.Y>.95f&&twoMeters.Z>.95f,"Default water still strongly tints a two-metre pool");
            Check(look.Scattering.Length<.07f,"Milky scattering remains in the clear presets");
        }
        var legacy=new PoolWaterLook(new V3(.9f,.985f,.975f),new V3(.025f,.13f,.115f),7,.65f);
        var upgraded=PoolWaterModel.Look(PoolWaterAppearance.ClearCeramic);var crystal=PoolWaterModel.Look(PoolWaterAppearance.CrystalClear);
        Check(PoolWaterModel.Transmission(upgraded,5).X>PoolWaterModel.Transmission(legacy,5).X+.15f,"New clear water did not improve deep red transmission");
        Check(PoolWaterModel.Transmission(crystal,5).X>PoolWaterModel.Transmission(upgraded,5).X,"Crystal option adds no clarity");
        Check(crystal.RefractionDistance<upgraded.RefractionDistance&&PoolWaterModel.ClearFallbackOpacity<.1f&&PoolWaterModel.ClearNormalStrength<.15f,"Clear fallback remains opaque or heavily distorted");
        var preferences=new GamePreferences{waterAppearance=PoolWaterAppearance.CrystalClear};preferences.Sanitize();Check(preferences.waterAppearance==PoolWaterAppearance.CrystalClear,"Settings reject the appended crystal preset");
#endif
        Console.WriteLine("PASS: water optical budgets, bounded captures and clear-water transmission (core math).");
    }

    static void IntegrationSourceContracts()
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        string[][] configs={Array.Empty<string>(),new[]{"UNITY_EDITOR","UNITY_INCLUDE_TESTS","ENABLE_LEGACY_INPUT_MANAGER"},new[]{"POOLROOMS_HDRP_17","UNITY_EDITOR","UNITY_INCLUDE_TESTS"},new[]{"POOLROOMS_INPUT_SYSTEM","ENABLE_INPUT_SYSTEM","UNITY_INCLUDE_TESTS"},new[]{"UNITY_STANDALONE_WIN","ENABLE_LEGACY_INPUT_MANAGER","UNITY_INCLUDE_TESTS"}};
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
        Console.WriteLine($"PASS: C# syntax/Tooltip contracts in {configs.Length} symbol configurations; {bindings} reduced LightUnit binding checks. Native Unity APIs NOT compiled.");
    }
}
