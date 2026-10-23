using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Poolrooms.Core;

static partial class Checks
{
    // Compile the real expressions at the reported Core/Runtime boundaries. Only
    // Coroutine/legacy Component.animation are stand-ins; this is NOT Unity API validation.
    static void RuntimeTypeContracts()
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        CompilationUnitSyntax Read(string name)=>CSharpSyntaxTree.ParseText(File.ReadAllText(
            Path.Combine(root,"Assets/PoolroomsProcedural/Runtime",name+".cs"))).GetCompilationUnitRoot();
        MethodDeclarationSyntax Method(CompilationUnitSyntax tree,string name)=>tree.DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(m=>m.Identifier.ValueText==name);
        var notice=Method(Read("PoolroomsRoomLife"),"AttachNotice");
        string room=notice.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single(n=>n.Type.ToString()=="Room").ToString();
        string stations=notice.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(n=>n.Expression.ToString()=="ImmersionStations.Plan").ToString();
        var tool=Method(Read("PoolroomsPickup"),"CreateTool");
        string size=tool.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(n=>n.Identifier.ValueText=="size").Initializer.Value.ToString();
        string radius=tool.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single(n=>n.Left.ToString()=="item.CarryRadius").Right.ToString();
        var sound=Read("PoolroomsSoundscape");
        string random=sound.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Single(n=>n.Declaration.Variables.Any(v=>v.Identifier.ValueText=="random")).ToString();
        string steps=Method(sound,"Distant").DescendantNodes().OfType<AssignmentExpressionSyntax>()
            .Single(n=>n.Left.ToString()=="remainingSteps").Right.ToString();
        var station=Read("PoolroomsStation");
        var fields=station.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(n=>n.Declaration.Type.ToString()=="Coroutine").ToArray();
        Check(fields.Length>0,"Station coroutine fields were not checked");
        string skeleton="class LegacyComponent { public object animation; } class Coroutine {} "+
            "class StationBinding:LegacyComponent { "+string.Join("\n",fields.Select(n=>n.ToString()))+" }";
        string Source(string roomExpression,string radiusExpression,string stepsExpression,string members)=>
            "using Poolrooms.Core; class World { public Layout CurrentLayout; } "+
            "class Probe { "+random+" public static Room MakeRoom(int x,int z,LiminalEnvironment environment,float offsetY) { return "+roomExpression+"; } "+
            "public static float Radius(int id) { var size="+size+"; return "+radiusExpression+"; } "+
            "public int Steps() { return "+stepsExpression+"; } "+
            "public static void Stations(int x,int z,LiminalEnvironment environment,float offsetY,RoomGeometry geometry,RoomLifePlan plan,World world) { var room="+roomExpression+"; var stations="+stations+"; } } "+members;
        var references=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Select(p=>(MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(Room).Assembly.Location));
        CSharpCompilation Compile(string source)=>CSharpCompilation.Create("RuntimeBoundaryProbe",
            new[]{CSharpSyntaxTree.ParseText(source,new CSharpParseOptions(LanguageVersion.CSharp9))},references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        bool Failure(Diagnostic d)=>d.Severity==DiagnosticSeverity.Error||d.Id=="CS0108";
        var compilation=Compile(Source(room,radius,steps,skeleton));
        var errors=compilation.GetDiagnostics().Where(Failure).ToArray();
        Check(errors.Length==0,"Runtime/Core semantic boundary: "+string.Join("\n",errors.Select(d=>d.ToString())));
        // Replay each original failure independently: prove this guard detects types,
        // rather than accepting syntax or simply looking for replacement text.
        var regressions=new Dictionary<string,string>
        {
            ["CS0200"]=Source("new Room{X=x,Z=z,Floor=-offsetY,Environment=environment}",radius,steps,skeleton),
            ["CS1061"]=Source(room,"size.magnitude*.5f+.015f",steps,skeleton),
            ["CS0266"]=Source(room,radius,"random.Range(2,5)",skeleton),
            ["CS0108"]=Source(room,radius,steps,"class LegacyComponent { public object animation; } class Coroutine {} class StationBinding:LegacyComponent { private Coroutine animation,flowAnimation; }")
        };
        foreach(var regression in regressions)
            Check(Compile(regression.Value).GetDiagnostics().Any(d=>d.Id==regression.Key),"Failed to reproduce "+regression.Key);
        using var image=new MemoryStream();var emitted=compilation.Emit(image);
        Check(emitted.Success,"Reduced runtime type probe failed to emit");
        var probe=Assembly.Load(image.ToArray()).GetType("Probe");
        var radiusMethod=probe.GetMethod("Radius");
        for(int id=0;id<=4;id++)if(EncounterTools.Available(id))
        {
            float actual=(float)radiusMethod.Invoke(null,new object[]{id});
            Check(actual>0&&Math.Abs(actual-(EncounterTools.Size(id).Length*.5f+.015f))<.00001f,"Tool carry radius mismatch");
        }
        var actor=Activator.CreateInstance(probe);var stepMethod=probe.GetMethod("Steps");var seen=new HashSet<int>();
        for(int i=0;i<4096;i++){int count=(int)stepMethod.Invoke(actor,null);Check(count>=2&&count<=4,"Remote step burst outside integer bounds");seen.Add(count);}
        Check(seen.Count==3,"Remote step variation collapsed");
        Forest39FactoryContract(root,references);
        StationFloorCoordinates();
        Console.WriteLine("PASS: four real Runtime/Core type contracts; original CS0200/CS1061/CS0266/CS0108 reproduced and rejected; 4096 bounded sound bursts. Reduced probes, NOT Unity compilation.");
    }

    static void StationFloorCoordinates()
    {
        var config=new LayoutConfig{EncounterExploration=true,Seed=537};
        foreach(var environment in new[]{LiminalEnvironment.Supermarket,LiminalEnvironment.RainHouses})
        foreach(float floor in new[]{-3.2f,0,3.2f,5.4f,10.8f,27f})
        {
            var room=new Room{X=2,Z=-3,Environment=environment};
            var geometry=new RoomGeometry{Dressing=new DressingGeometry()};
            var expected=new List<int>();
            for(int i=0;i<4;i++)
            {
                float y=i==0?.85f:i==3?1.9f:1.2f;
                var center=new V3(-5+i*3,floor+y,-5);var normal=new V3(0,0,1);
                geometry.Dressing.Anchors.Add(new DressingAnchor(center,normal,center,center,center,0,0));
                if(i==1||i==2)expected.Add(i);
            }
            float originalFloor=room.Floor;
            var stations=ImmersionStations.Plan(room,geometry,new RoomLifePlan(),config,floor);
            Check(stations.Select(s=>s.Slot).SequenceEqual(expected),"Upper-floor stations lost their local support elevation");
            foreach(var p in stations)Check(Math.Abs(p.Anchor.Center.Y-floor-1.2f)<.00001f,"Station moved vertically");
            Check(room.Floor==originalFloor,"Station planning mutated room floor");
        }
        var legacyRoom=new Room{Environment=LiminalEnvironment.Poolrooms};
        var legacyGeometry=new RoomGeometry{Dressing=new DressingGeometry()};
        var point=new V3(2,legacyRoom.Floor+1.2f,-5);
        legacyGeometry.Dressing.Anchors.Add(new DressingAnchor(point,new V3(0,0,1),point,point,point,0,0));
        var implicitFloor=ImmersionStations.Plan(legacyRoom,legacyGeometry,new RoomLifePlan(),config);
        var explicitFloor=ImmersionStations.Plan(legacyRoom,legacyGeometry,new RoomLifePlan(),config,legacyRoom.Floor);
        Check(implicitFloor.Count==1&&explicitFloor.Count==1&&implicitFloor[0].Slot==explicitFloor[0].Slot,"Legacy floor default changed");
        Console.WriteLine("PASS: station floor coordinates at six elevations in mall/house contexts; legacy floor default preserved.");
    }
}
