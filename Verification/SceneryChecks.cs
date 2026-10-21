using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static RoomGeometry SceneryFloor(float width=40,float depth=40)
    {
        var geometry=new RoomGeometry();
        geometry.Structure.Box(new V3(-width*.5f,-.25f,-depth*.5f),
            new V3(width*.5f,0,depth*.5f),Surface.Tile);
        return geometry;
    }
    static string ScenerySignature(SceneryPlan plan)
    {
        return string.Join("|",plan.Placements.Select(p=>p.Slot+":"+p.Source+":"+
            BitConverter.SingleToInt32Bits(p.Bottom.X)+":"+BitConverter.SingleToInt32Bits(p.Bottom.Y)+":"+
            BitConverter.SingleToInt32Bits(p.Bottom.Z)+":"+BitConverter.SingleToInt32Bits(p.Yaw)));
    }
    static void SceneryWorld()
    {
        int objects=0,clusters=0,plans=0;
        var recipes=new HashSet<int>();
        var stories=new HashSet<SceneryStory>();
        for(int i=0;i<SceneryRecipeCatalog.Count;i++)
        {
            var recipe=SceneryRecipeCatalog.Get(i);
            Check(recipe.Id==i&&recipe.Count>=2&&recipe.Count<=10,"Unstable scenery recipe IDs");
            stories.Add(recipe.Story);
            for(int p=0;p<recipe.Count;p++)
            {
                var piece=recipe.Piece(p);
                Check(ScenerySafety.Source(piece.Source),"Recipe points outside licensed source bank");
                Check(piece.Parent<p&&piece.Parent>=-1,"Invalid table dependency");
                if(piece.Parent>=0)Check(recipe.Piece(piece.Parent).Role==SceneryRole.Table,"Non-table support");
            }
        }
        Check(stories.Count==12&&SceneryRecipeCatalog.Count==48,"Authored recipe/story count changed");
        foreach(LiminalEnvironment environment in Enum.GetValues(typeof(LiminalEnvironment)))
        foreach(int seed in new[]{0,17,-500,int.MinValue,31,53,79,101,151,997,481516,int.MaxValue})
        {
            var config=new LayoutConfig{Seed=seed,EncounterExploration=true,ObjectAbundance=3};
            var room=new Room{Kind=RoomKind.Atrium,X=seed%11,Z=seed%7,Width=40,Depth=40,Height=7,Environment=environment};
            var geometry=SceneryFloor();
            var plan=SceneryPlanner.Build(room,geometry,config);
            var repeat=SceneryPlanner.Build(room,geometry,config);
            Check(ScenerySignature(plan)==ScenerySignature(repeat),"Worker choice is not deterministic");
            Check(SceneryPlanner.ValidPlan(plan,room,config),"Invalid generated scenery plan");
            Check(plan.Clusters>0,"Empty unobstructed scenery for "+environment+" seed "+seed);
            Check(!plan.InvalidInput,"Valid input unexpectedly rejected");
            Check(plan.Candidates<=SceneryWorkBudget.MaximumAnchors*8,"Candidate budget escaped");
            var keys=new HashSet<string>();
            foreach(var p in plan.Placements)
            {
                Check(keys.Add(p.Key(seed,room.X,room.Z)),"Duplicate scenery pickup key");
                recipes.Add(p.Recipe);
                if(p.ParentSlot>=0)Check(ScenerySafety.ParentPresent(p,plan.Placements),"Floating tabletop item");
            }
            objects+=plan.Placements.Count;clusters+=plan.Clusters;plans++;
        }
        Check(recipes.Count>=40,"Too few arrangements reached in deterministic biome/seed sample");
        for(int id=0;id<SceneryRecipeCatalog.Count;id++)
        {
            var c=new LayoutConfig{EncounterExploration=true,ObjectAbundance=3};
            var r=new Room{Kind=RoomKind.Atrium,Width=40,Depth=40,Height=7};
            var g=SceneryFloor();var f=new SceneryWorkBudget();var index=new ScenerySurfaceIndex(g,f);
            var proposed=new List<SceneryPlacement>();var access=new List<SceneryBox>();
            bool ok=SceneryPlanner.TryCluster(SceneryRecipeCatalog.Get(id),new SceneryAnchor(new V3(10,.006f,10),0,0,0),r,g,c,index,new SceneryReservations(r,g),16,proposed,access);
            Check(ok,"Unreachable isolated recipe: "+id+" "+SceneryRecipeCatalog.Get(id).Name);
        }

        var dryConfig=new LayoutConfig{EncounterExploration=true};
        var dryRoom=new Room{Kind=RoomKind.Atrium,Width=36,Depth=36,Height=7,Environment=LiminalEnvironment.Office};
        var floor=SceneryFloor(36,36);
        var basePlan=SceneryPlanner.Build(dryRoom,floor,dryConfig);
        Check(basePlan.Placements.Count>0,"Control floor failed");
        // A narrow wet strip through a footprint must be rejected even when
        // none of the four corners falls inside the pool.
        var wet=new[]{new WaterRegion{Width=.1f,Depth=4,Bottom=-1,Level=.3f}};
        Check(!ScenerySafety.Dry(wet,new SceneryBox(new V3(-1,0,-1),new V3(1,1,1))),"Water strip missed");
        floor.Regions.Add(new WaterRegion{Width=36,Depth=36,Bottom=-1,Level=.3f});
        Check(SceneryPlanner.Build(dryRoom,floor,dryConfig).Placements.Count==0,"Underwater furniture accepted");
        floor=SceneryFloor(36,36);
        floor.Structure.Box(new V3(-17,0,-17),new V3(17,5,17),Surface.Plaster);
        Check(SceneryPlanner.Build(dryRoom,floor,dryConfig).Placements.Count==0,"Solid architecture furnished");
        floor=new RoomGeometry();
        Check(SceneryPlanner.Build(dryRoom,floor,dryConfig).Placements.Count==0,"Furniture floats above absent floor");
        dryConfig.EncounterExploration=false;
        Check(SceneryPlanner.Build(dryRoom,SceneryFloor(),dryConfig).Placements.Count==0,"Disabled encounter flag ignored");
        dryConfig.EncounterExploration=true;
        dryRoom.MallGateway=true;
        Check(SceneryPlanner.Build(dryRoom,SceneryFloor(),dryConfig).Placements.Count==0,"Gateway route obstructed");
        dryRoom.MallGateway=false;
        var bad=SceneryFloor();bad.Structure.Indices[0].Add(int.MaxValue);
        Check(SceneryPlanner.Build(dryRoom,bad,dryConfig).InvalidInput,"Malformed triangle was accepted");
        bad=SceneryFloor();bad.Structure.Positions[0]=new V3(float.NaN,0,0);
        Check(SceneryPlanner.Build(dryRoom,bad,dryConfig).InvalidInput,"NaN mesh accepted");
        Check(!SourceFurniture.Clear(bad.Structure,new SourceFurniturePlacement(0,0,new V3(0,.01f,0),0)),"Invalid mesh query threw or passed");
        Check(!SourceFurniture.Clear(SceneryFloor().Structure,new SourceFurniturePlacement(0,-1,new V3(),0)),"Invalid source query passed");
        Check(!ScenerySafety.TrySource("missing-source",out _),"Missing source became a default bottle");
        Check(!ScenerySafety.KnownRecipe(-1)&&!ScenerySafety.KnownRecipe(int.MaxValue),"Bad recipe accepted");
        var fuel=new SceneryWorkBudget();
        for(int i=0;i<SceneryWorkBudget.MaximumQueries;i++)Check(fuel.Query(),"Premature query exhaustion");
        Check(!fuel.Query()&&fuel.Exhausted,"Fuel budget did not settle");
        Check(SceneryWorkBudget.ObjectLimit(int.MaxValue)<=16&&SceneryWorkBudget.ObjectLimit(int.MinValue)>=0,"Unbounded abundance");
        ScenerySupportRegressions();
        SceneryRealRooms();
        Console.WriteLine($"PASS: scenery {plans} open-room biome/seed plans, {clusters} clusters, {objects} instances, {recipes.Count} distinct authored arrangements, 12 stories; native Unity/physics/FPS unrun.");
    }
    static void ScenerySupportRegressions()
    {
        foreach(float yaw in new[]{0f,45f,90f,180f,270f})
        {
            var anchor=new SceneryAnchor(new V3(8,.006f,8),yaw,0,0);
            var recipe=SceneryRecipeCatalog.Get(0);
            var proposal=new List<SceneryPlacement>();
            Check(SceneryTableLayout.TryPlace(recipe,0,anchor,proposal,out var table),"Table could not be placed");
            proposal.Add(table);
            Check(SceneryTableLayout.TryPlace(recipe,3,anchor,proposal,out var cup),"Rotated cup lost support");
            Check(ScenerySafety.SupportedBy(cup,table),"Cup height differs from actual table top");
            Check(!ScenerySafety.ParentPresent(cup,Array.Empty<SceneryPlacement>()),"Orphan tabletop object accepted");
        }
        var geometry=SceneryFloor();var fuel=new SceneryWorkBudget();var index=new ScenerySurfaceIndex(geometry,fuel);
        Check(index.Valid&&index.Supported(new V3(0,0,0)),"Indexed floor support missing");
        Check(!index.Supported(new V3(0,1,0)),"Indexed floor accepted wrong height");
        Check(!index.Supported(new V3(30,0,0)),"Indexed floor supports outside mesh");
        geometry.Structure.Box(new V3(5,0,5),new V3(6,3,6),Surface.Tile);
        index=new ScenerySurfaceIndex(geometry,new SceneryWorkBudget());
        Check(!index.Clear(new SceneryBox(new V3(4.9f,0,4.9f),new V3(6.1f,1,6.1f))),"Triangle bounds miss solid object");
    }
    static void SceneryRealRooms()
    {
        int accepted=0,instances=0;
        foreach(LiminalGeneration mode in Enum.GetValues(typeof(LiminalGeneration)))
        {
            var config=new LayoutConfig{Procedural=mode,Seed=481516,EncounterExploration=true,AutonomousGeneration=true,ExpandedVariety=true,CurveSegments=12,Continuous=true};
            var layout=new Layout(config);
            for(int sample=0;sample<2;sample++)
            {
                var room=layout.At(sample*7+1,sample*-3+2);
                var geometry=Architecture.Build(room,layout);
                Check(geometry.Life!=null&&geometry.Life.Scenery!=null,"Scenery not wired into actual Architecture");
                var plan=geometry.Life.Scenery;
                Check(SceneryPlanner.ValidPlan(plan,room,config),"Real room plan failed boundary validation: "+mode);
                if(plan.Placements.Count>0){accepted++;instances+=plan.Placements.Count;}
                foreach(var p in plan.Placements)
                    Check(ScenerySafety.Dry(geometry.Regions,p.Bounds),"Real-room object submerged");
            }
        }
        Check(accepted>=4,"Layer never reaches real generated rooms");
        Console.WriteLine($"Scenery real-room sample: {accepted} furnished rooms, {instances} additional instances; empty/complex rooms may conservatively reject additions.");
    }
}
