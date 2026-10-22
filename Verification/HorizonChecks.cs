using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Poolrooms.Core;

static partial class Checks
{
    static void HorizonLevels()
    {
        Check(Enum.GetValues(typeof(LiminalEnvironment)).Length==63,"Five appended levels missing");
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        var ids=JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,int>>>(File.ReadAllText(Path.Combine(root,"Verification/horizon_previous_ids.json")));
        foreach(var bank in ids)
        foreach(var id in bank.Value)
        {
            Type type=bank.Key=="LiminalGeneration"?typeof(LiminalGeneration):bank.Key=="LiminalEnvironment"?typeof(LiminalEnvironment):typeof(SurfaceTheme);
            Check(Convert.ToInt32(Enum.Parse(type,id.Key))==id.Value,"Old serialized ID changed: "+id.Key);
        }
        var signatures=new HashSet<string>();int rooms=0,props=0,activities=0,glass=0;
        for(int index=0;index<HorizonLevelPlanner.Count;index++)
        {
            var environment=(LiminalEnvironment)(HorizonLevelPlanner.FirstEnvironment+index);
            var mode=(LiminalGeneration)((int)LiminalGeneration.MagnoliaCourts+index);
            Check(HorizonLevelPlanner.ForMode(mode)==environment,"Direct mode mapping is shifted");
            Check(LiminalRegions.At(new LayoutConfig{Procedural=mode},7,-9)==environment,"Direct mode does not generate requested level");
            Check(LiminalRegions.Find(new LayoutConfig{Procedural=mode},environment,0,0,out _,out _),"Direct mode cannot find itself");
            Check(!LiminalRegions.Find(new LayoutConfig{Procedural=LiminalGeneration.Expanded},environment,0,0,out _,out _),"Disabled bank leaked into old custom profiles");
            Check(InteriorPlanner.Name(environment)==HorizonLevelPlanner.Name(environment),"Frontend name not localized");
            Check(RoomMemories.At(0,0,0,environment).Title==HorizonLevelPlanner.Name(environment),"Missing own narrative");
            foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})
            foreach(int recipe in new[]{0,3,257,2048,16384,65535,100000,131071})
            {
                var c=new LayoutConfig{CellSize=size,DoorWidth=door,DoorHeight=door*.5f+2.1f,
                    CurveSegments=12,EncounterExploration=true,ExpandedLevelDetails=true,ObjectAbundance=3};
                var r=new Room{Environment=environment,Exits=15,X=17,Z=-13};
                RestfulLevelPlanner.DescribeRecipe(r,c,recipe);var g=Architecture.Build(r,c);
                Check(r.Plan.Valid(r,c),"Invalid five-level plan "+environment);
                foreach(var mesh in new[]{g.Structure,g.Decoration,g.Glass,g.CollisionOnly,g.Water})Validate(mesh);
                foreach(var mesh in g.VisualMeshes)Validate(mesh);
                Check(g.Structure.Positions.Count<100000&&g.Decoration.Positions.Count<180000,"Unbounded new geometry");
                foreach(var prop in r.Plan.Restful.Props)
                {
                    Check(Math.Abs(Support(g.Structure,prop.Bottom)-prop.Bottom.Y+.006f)<.002f,"Unsupported new level item "+environment+" "+prop.Source);
                    if(prop.Source!=null)Check(SourceFurniture.Index(prop.Source)>=0,"Missing licensed source");
                    props++;
                }
                for(int side=0;side<4;side++)for(float distance=0;distance<size*.5f;distance+=.5f)
                {
                    var p=new V3(Directions.DX[side]*distance,.9f,Directions.DZ[side]*distance);
                    Check(Math.Abs(Support(g.Structure,p))<.002f,"New central route lacks floor");
                    Check(SphereClear(g.Structure,p,.32f)&&SphereClear(g.CollisionOnly,p,.32f),"New level blocks main crossing");
                }
                Check(g.Life.Stations.Count==4,"Missing four native activities");
                foreach(var station in g.Life.Stations)
                {
                    Check(ArchitecturalDressing.FootprintFits(station.Anchor.Center,station.Anchor.Normal,station.Anchor.A,station.Anchor.B,station.Anchor.C),"Activity lacks a complete real support triangle");
                    var approach=station.Anchor.Center+station.Anchor.Normal*.65f;
                    approach.Y=.9f;
                    Check(SphereClear(g.Structure,approach,.3f)&&SphereClear(g.CollisionOnly,approach,.3f),"Activity approach obstructed "+environment);
                    Check(Math.Abs(Support(g.Structure,approach))<.002f,"Activity lacks a floor");
                    activities++;
                }
                if(environment==LiminalEnvironment.FunicularTerminal)CheckFunicularEntry(g,r.Plan.Restful);
                if(environment==LiminalEnvironment.LighthouseCourts)CheckLighthouseSteps(g,r.Plan.Restful);
                if(environment==LiminalEnvironment.StainedGlassHalls)
                {
                    Check(g.VisualMeshes.Count==3,"Stained glass lacks three independent tinted batches");
                    Check(g.CollisionOnly.TriangleCount>0,"Stained windows have no physical barrier");
                    glass+=g.VisualMeshes.Sum(m=>m.TriangleCount);
                }
                if(size==36&&door==3&&recipe==0)
                    Check(signatures.Add(Hash(g.Structure,g.Decoration)),"Two levels only change names/colors");
                rooms++;
            }
        }
        var legacy=new LayoutConfig();WalkableExploration.Configure(legacy);legacy.EnableHorizonLevels=false;
        var expanded=new LayoutConfig();WalkableExploration.Configure(expanded);
        for(int bx=-2;bx<=2;bx++)for(int bz=-2;bz<=2;bz++)
        {
            var seen=new HashSet<LiminalEnvironment>();
            for(int x=0;x<8;x++)for(int z=0;z<8;z++)
            {
                WalkableExploration.Region(legacy,bx*8+x,bz*8+z,out var before);
                WalkableExploration.Region(expanded,bx*8+x,bz*8+z,out var after);
                seen.Add(after);
                if((int)after<58)Check(after==before,"A non-spare old discovery assignment moved");
            }
            Check(seen.Count==63,"A new level requires teleport/master mode");
        }
        Console.WriteLine($"PASS: five Horizon programmes, {rooms} physical rooms at three sizes/two door widths, {props} supported pickups, {activities} reachable activities, {glass} colored glazing triangles; previous IDs and 63-biome coverage preserved. Unity/HDRP/PhysX/rendering/FPS unrun.");
    }
    static void CheckFunicularEntry(RoomGeometry g,RestfulLevelPlan plan)
    {
        foreach(var bay in plan.Bays)
        {
            float d=Math.Min(3,bay.Depth*.67f),z=bay.Z+.12f;
            for(int i=0;i<=8;i++)
            {
                var p=new V3(bay.X,1.02f,z-d*.5f+.12f+i*(d-.24f)/8);
                Check(Math.Abs(Support(g.Structure,p)-.12f)<.002f,"Cabin aisle does not support player");
                Check(SphereClear(g.Structure,p,.32f)&&SphereClear(g.CollisionOnly,p,.32f),"Display cabin cannot be entered");
            }
        }
    }
    static void CheckLighthouseSteps(RoomGeometry g,RestfulLevelPlan plan)
    {
        foreach(var bay in plan.Bays)
        {
            int count=Math.Min(4+plan.Layout,Math.Max(3,(int)((bay.Depth*.5f-.65f)/.29f)));
            float z=bay.Z+.3f;
            for(int step=0;step<count;step++)
            {
                float at=z-.9f-count*.29f+(step+.5f)*.29f,height=(step+1)*.14f;
                var p=new V3(bay.X,height+.9f,at);
                Check(Math.Abs(Support(g.Structure,p)-height)<.002f,"Missing physical lookout tread");
                Check(SphereClear(g.Structure,p,.32f),"Lookout stair headroom obstructed");
            }
            var landing=new V3(bay.X,count*.14f+.9f,z);
            Check(SphereClear(g.Structure,landing,.32f),"Lookout platform is a sealed prop");
        }
    }
}
