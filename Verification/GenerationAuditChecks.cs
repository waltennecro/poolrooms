using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void GenerationAudit()
    {
        RoomSelectionRegressions();
        int planned=0,meshed=0,props=0;
        var sources=new HashSet<string>(StringComparer.Ordinal);
        var items=new HashSet<int>();
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        void Prop(QuietPropPlan p)
        {
            Check(float.IsFinite(p.Bottom.X)&&float.IsFinite(p.Bottom.Y)&&float.IsFinite(p.Bottom.Z)&&float.IsFinite(p.Yaw),"Nonfinite prop transform");
            if(p.CatalogId>=0)
            {
                MallItemCatalog.Get(p.CatalogId);
                if(items.Add(p.CatalogId))Validate(MallItemCatalog.Model(p.CatalogId));
            }
            else
            {
                Check(!string.IsNullOrEmpty(p.Source),"Missing prop source");
                Check(SourceFurniture.Longest(p.Source)>0,"Invalid prop scale");
                if(sources.Add(p.Source))Check(Directory.GetFiles(Path.Combine(root,"Assets/PoolroomsProcedural/Resources"),p.Source+".json",SearchOption.AllDirectories).Length==1,"Missing/ambiguous prop resource: "+p.Source);
            }
            props++;
        }
        void Plan(Room room,LayoutConfig config)
        {
            if(room.Plan==null)return;
            Check(room.Plan.Valid(room,config),"Invalid plan "+room.Environment+" at "+room.X+","+room.Z);
            var p=room.Plan;
            foreach(var prop in p.Quiet?.Props??p.Garden?.Props??p.Baths?.Props??p.Aquatic?.Props??p.Rain?.Props??Array.Empty<QuietPropPlan>())Prop(prop);
            if(p.City!=null)foreach(var prop in p.City.Props)Prop(new QuietPropPlan(prop.Position,-1,SourceFurniture.Id(prop.Source),prop.Yaw));
        }
        void Mesh(MeshData mesh)
        {
            Validate(mesh);
            Check(mesh.BlendWeights.Count==0||mesh.BlendWeights.Count==mesh.Positions.Count,"Truncated mesh blend weights");
            foreach(var n in mesh.Normals)Check(float.IsFinite(n.X)&&float.IsFinite(n.Y)&&float.IsFinite(n.Z),"Nonfinite mesh normal");
            foreach(var uv in mesh.UV)Check(float.IsFinite(uv.X)&&float.IsFinite(uv.Y),"Nonfinite mesh UV");
        }
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})
        {
            var c=new LayoutConfig{CellSize=size,DoorWidth=door,DoorHeight=door*.5f+2.1f,Continuous=true,CurveSegments=12};
            for(int recipe=0;recipe<5600;recipe++)
            {var room=new Room{Exits=15};AquaticParkPlanner.DescribeRecipe(room,c,recipe);Plan(room,c);planned++;}
            for(int recipe=0;recipe<2500;recipe++)
            {
                foreach(var generation in new[]{LiminalGeneration.Orangerie,LiminalGeneration.ThermalBaths,LiminalGeneration.RainHouses})
                {
                    var room=new Room{Exits=15};
                    if(generation==LiminalGeneration.Orangerie)OrangeriePlanner.DescribeRecipe(room,c,recipe);
                    else if(generation==LiminalGeneration.ThermalBaths)ThermalBathPlanner.DescribeRecipe(room,c,recipe);
                    else RainHousePlanner.DescribeRecipe(room,c,recipe);
                    Plan(room,c);planned++;
                }
            }
        }
        Console.WriteLine($"Generation data audit: {planned} recipe/scale plans and {props} prop placements resolved through the actual catalogues.");
        foreach(LiminalGeneration mode in Enum.GetValues(typeof(LiminalGeneration)))
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})foreach(int seed in new[]{0,-1,481516})
        {
            var config=new LayoutConfig{Seed=seed,CellSize=size,DoorWidth=door,DoorHeight=door*.5f+2.1f,WallThickness=size==28?.6f:.15f,
                WaterLevel=size==28?-.18f:-.03f,TextureWorldSize=size==60?.1f:10,CurveSegments=size==60?96:12,
                Continuous=true,Procedural=mode,Grammar=ProceduralGrammar.SolarAtlas,AutonomousGeneration=true};
            if(mode==LiminalGeneration.Expanded)WalkableExploration.Configure(config);
            var layout=new Layout(config);
            for(int sample=0;sample<8;sample++)
            {
                int x=sample==0?0:sample*7919-29000,z=sample==0?0:sample*-3571+14000;
                try
                {
                    var room=layout.At(x,z);Plan(room,config);planned++;
                    if(sample!=1)continue;
                    var data=Architecture.Build(room,layout);Mesh(data.Structure);Mesh(data.CollisionOnly);Mesh(data.Water);Mesh(data.Decoration);Mesh(data.Glass);
                    foreach(var visual in data.VisualMeshes)Mesh(visual);
                    foreach(var connection in data.Connectors)if(connection!=null)Mesh(connection.Structure);
                    foreach(var placement in data.SourceSeats.Concat(data.SourceProps))Prop(new QuietPropPlan(placement.Bottom,-1,SourceFurniture.Id(placement.Source),placement.Yaw));
                    meshed++;
                }
                catch(Exception e){throw new Exception($"Generation audit: {mode}, seed {seed}, cell {size}, door {door}, sample {sample}, coordinates {x},{z}",e);}
            }
        }
        Console.WriteLine($"PASS: generation audit, {planned} plans, {meshed} full room/connector meshes, {props} prop placements, {items.Count} catalogue items and {sources.Count} named source references. Native Unity loading/physics unrun.");
    }
    static void RoomSelectionRegressions()
    {
        Room Legacy(Layout layout,Dictionary<long,bool> loaded,int fx,int fz,int radius,bool local,IReadOnlyList<RoomCell> anchors)
        {
            Room chosen=null;long best=long.MaxValue;var c=layout.Config;
            int x0=c.Continuous?fx-radius:local?Math.Max(0,fx-radius):0,x1=c.Continuous?fx+radius:local?Math.Min(c.Columns-1,fx+radius):c.Columns-1;
            int z0=c.Continuous?fz-radius:local?Math.Max(0,fz-radius):0,z1=c.Continuous?fz+radius:local?Math.Min(c.Rows-1,fz+radius):c.Rows-1;
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
            {
                if(ConnectedTerrainTopology.Reserved(c,x,z))continue;var r=layout.At(x,z);
                if(r==null||loaded.ContainsKey(ContinuousTopology.CellKey(x,z)))continue;
                long dx=(long)x-fx,dz=(long)z-fz,score=dx*dx+dz*dz;
                if(score<best){best=score;chosen=r;}
            }
            if(chosen==null)foreach(var p in anchors)for(int z=p.Z-1;z<=p.Z+1;z++)for(int x=p.X-1;x<=p.X+1;x++)
            {
                if(ConnectedTerrainTopology.Reserved(c,x,z)||loaded.ContainsKey(ContinuousTopology.CellKey(x,z)))continue;
                var r=layout.At(x,z);if(r==null)continue;long dx=x-p.X,dz=z-p.Z,score=dx*dx+dz*dz;
                if(score<best){best=score;chosen=r;}
            }
            return chosen;
        }
        int cases=0;
        foreach(bool continuous in new[]{false,true})foreach(bool connected in new[]{false,true})foreach(int radius in new[]{1,2,5,12})foreach(int seed in new[]{0,31,481516})
        {
            var c=new LayoutConfig{Continuous=continuous,Seed=seed,CurveSegments=12};
            if(connected&&continuous)WalkableExploration.Configure(c);
            var old=new Layout(c);var actual=new Layout(c);var loaded=new Dictionary<long,bool>();var anchors=new List<RoomCell>();
            int fx=continuous?9:2,fz=continuous?0:2;
            if(ConnectedMallTopology.Site(c,0,0,out var site)){anchors.Add(new RoomCell(site.X-site.RadiusX-1,site.Z));anchors.Add(new RoomCell(site.X+site.RadiusX+1,site.Z));}
            for(int step=0;step<8;step++)
            {
                int cached=actual.CachedRoomCount;var expected=Legacy(old,loaded,fx,fz,radius,true,anchors);
                var next=RoomStreamingSearch.Next(actual,loaded,fx,fz,true,radius,anchors);
                Check((expected==null)==(next==null),"Room selection lost a candidate");
                if(next!=null)
                {
                    Check(next.X==expected.X&&next.Z==expected.Z,"Room selection changed nearest/tie/anchor order");
                    Check(actual.CachedRoomCount-cached<=1,"Selecting one room eagerly described other candidates");
                    loaded.Add(ContinuousTopology.CellKey(next.X,next.Z),true);
                }
                cases++;
            }
        }
        var legacy=new Layout(new LayoutConfig{Continuous=true,CurveSegments=12});var lazy=new Layout(new LayoutConfig{Continuous=true,CurveSegments=12});var empty=new Dictionary<long,bool>();
        Legacy(legacy,empty,0,0,12,true,Array.Empty<RoomCell>());RoomStreamingSearch.Next(lazy,empty,0,0,true,12,Array.Empty<RoomCell>());
        Check(legacy.CachedRoomCount==625&&lazy.CachedRoomCount==1,"Wide-view regression must demonstrate 625 eager plans versus the one selected plan");
        var finite=new Layout(new LayoutConfig{Columns=4,Rows=3});var all=new Dictionary<long,bool>();
        foreach(var r in finite.Rooms)all.Add(ContinuousTopology.CellKey(r.X,r.Z),true);
        Check(RoomStreamingSearch.Next(finite,all,0,0,false,0,Array.Empty<RoomCell>())==null,"Fully loaded finite map should settle");
        all.Remove(ContinuousTopology.CellKey(3,2));Check(RoomStreamingSearch.Next(finite,all,0,0,false,0,null)==finite.At(3,2),"Nonstreaming finite maps must still reach distant rooms");
        // Previously the int loop wrapped at MaxValue and could never terminate.
        var edge=new Layout(new LayoutConfig{Continuous=true,CurveSegments=12});
        Check(RoomStreamingSearch.Next(edge,empty,int.MaxValue-1,int.MinValue+1,true,2,null)!=null,"Clipped coordinate window failed");
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})foreach(int seed in new[]{0,31,481516})
        {
            var c=new LayoutConfig{Seed=seed,CellSize=size,DoorWidth=door};var site=new ConnectedMallSite(c,9,0);
            foreach(int side in new[]{-1,1})for(int step=0;step<=100;step++)
            {
                var gateway=new Room{X=site.X+side*(site.RadiusX+1),Z=site.Z,Width=Math.Min(22,size-6)};
                var p=ConnectedMallArchitecture.Point(site,gateway,c,step/100f);
                Check(ConnectedMallArchitecture.ContainsSupportFootprint(site,c,p.X,p.Z),"Actual mall bridge centre rejected as unsupported");
            }
            Check(!ConnectedMallArchitecture.ContainsSupportFootprint(site,c,0,site.Depth*.5f+1),"Empty mall reservation mistaken for a real floor");
        }
        Console.WriteLine($"Room streaming search: {cases} loading-order comparisons; a radius-12 choice describes 1 plan instead of 625; finite-map completion and integer-edge termination checked. Not a measured Unity FPS claim.");
    }
}
