using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Poolrooms.Core;

static partial class Checks
{
    static void EncounterWorld()
    {
        foreach(int tool in new[]{2,3,4})
        {
            var mesh=EncounterTools.Build(tool);Check(mesh.TriangleCount>20&&mesh.TriangleCount<4000,"Tool mesh missing or excessive");
            var size=EncounterTools.Size(tool);float center=tool==2?.045f:0;
            foreach(var p in mesh.Positions)Check(HumanoidRig.Finite(p)&&Math.Abs(p.X-center)<=size.X*.5f+.001f&&Math.Abs(p.Z)<=size.Z*.5f+.001f&&p.Y>=-.001f&&p.Y<=size.Y+.001f,"Tool collider does not contain its visible mesh "+tool+" "+p.X+","+p.Y+","+p.Z);
            foreach(var indices in mesh.Indices)for(int i=0;i<indices.Count;i+=3){int a=indices[i],b=indices[i+1],c=indices[i+2];Check(V3.Dot(V3.Cross(mesh.Positions[b]-mesh.Positions[a],mesh.Positions[c]-mesh.Positions[a]).Unit,mesh.Normals[a])>.8f,"Tool normal/winding mismatch");}
        }
        foreach(float height in new[]{.6f,1.75f,2.5f})foreach(int fps in new[]{10,60,144})foreach(HumanoidGesture gesture in Enum.GetValues(typeof(HumanoidGesture)))
        {
            var rig=new HumanoidRig(height,HumanoidRestPose.A);var animation=new HumanoidAnimation();
            for(int i=0;i<=fps;i++)
            {
                animation.Step(rig,new HumanoidMotion{Grounded=true,Gesture=gesture,GestureProgress=(float)i/fps},1f/fps);
                foreach(var joint in animation.Joints)Check(HumanoidRig.Finite(joint),"Gesture generated invalid joint");
                foreach(int arm in new[]{5,9}){Check(Math.Abs((animation.Joints[arm+2]-animation.Joints[arm+1]).Length-(rig.Rest[arm+2]-rig.Rest[arm+1]).Length)<.0001f,"Gesture changed upper arm length");Check(Math.Abs((animation.Joints[arm+3]-animation.Joints[arm+2]).Length-(rig.Rest[arm+3]-rig.Rest[arm+2]).Length)<.0001f,"Gesture changed forearm length");}
            }
        }
        var inventory=new ExplorationInventory();Check(ExplorationInventory.Pages==5&&ExplorationInventory.SlotsPerPage==50&&ExplorationInventory.Capacity==250,"Inventory dimensions");
        for(int i=0;i<250;i++)Check(inventory.Add("catalog:"+i,1),"Could not fill inventory");
        Check(!inventory.Add("catalog:250",1)&&!inventory.Add("tool:01",1)&&!inventory.Add("catalog:01",1)&&!inventory.Add("play:00:1",1),"Full or noncanonical inventory insertion");
        var huge=BigInteger.Pow(10,1000);Check(inventory.Add("catalog:0",huge)&&inventory.Count("catalog:0")==huge+1,"Stack count overflow");
        Check(!inventory.Remove("catalog:1",2)&&inventory.Count("catalog:1")==1,"Underflow consumed an item");
        Check(!inventory.Add("catalog:3",-1)&&!inventory.Add("not-an-item",1),"Invalid inventory entry");
        Check(inventory.Bind(0,0),"Cannot bind hotbar");Check(inventory.Move(0,249)&&inventory.Binding(0)=="catalog:0"&&inventory.Count(inventory.Binding(0))==huge+1,"Slot moves broke hotbar identity");
        Check(inventory.Remove("catalog:1",1)&&inventory.Add("catalog:250",1),"Empty slot was not reusable");
        Check(!inventory.Move(-1,0)&&!inventory.Move(0,250)&&!inventory.Bind(8,0),"Invalid page/hotbar index");
        Check(ExplorationInventory.DisplayCount(huge).Length<50,"Unbounded UI quantity label");
        var sum=BigInteger.Zero;for(int i=0;i<250;i++)sum+=inventory.Slot(i).Count;
        var random=new StableRandom(94);for(int i=0;i<5000;i++)inventory.Move(random.Range(250),random.Range(250));
        BigInteger after=0;for(int i=0;i<250;i++)after+=inventory.Slot(i).Count;Check(sum==after,"Rearranging pages lost or duplicated objects");
        for(int i=0;i<10;i++)Check(ExplorationItems.Tool(i)!=null&&ExplorationItems.Resolve("tool:"+i)==ExplorationItems.Tool(i),"Tool definition mismatch");
        for(int i=0;i<14;i++)Check(ExplorationItems.Resolve("source:"+SourceFurniture.Id(i))!=null,"Existing licensed furniture cannot be stored");
        for(int seed=0;seed<100;seed++)
        {
            var game=new PairingGame((uint)seed);Check(game.Cards.GroupBy(v=>v).All(g=>g.Count()==2)&&game.Cards.Distinct().Count()==8,"Unsolvable game deal");
            foreach(var group in Enumerable.Range(0,16).GroupBy(i=>game.Cards[i])){int a=group.First(),b=group.Last();Check(game.Turn(a)&&!game.Turn(a)&&game.Turn(b),"Invalid pairing state");}
            Check(game.Complete&&game.Matched==8&&game.Attempts==8,"Solved game did not finish");
        }
        var journal=new ExplorationJournal();Check(journal.Visit(0,0,LiminalEnvironment.Poolrooms)&&!journal.Visit(0,0,LiminalEnvironment.Poolrooms)&&journal.Rooms==1,"Visit counted twice");
        Check(journal.Read("welcome")&&!journal.Read("welcome")&&journal.Memories==1,"Read reward duplicated");
        Check(journal.Record(RoomActivity.Plant,"pot")&&!journal.Record(RoomActivity.Plant,"pot")&&journal.Count(RoomActivity.Plant)==1,"Care reward duplicated");
        var identities=new HashSet<string>();for(int x=-20;x<=20;x++)for(int z=-20;z<=20;z++){var memory=RoomMemories.At(481516,x,z,LiminalEnvironment.Poolrooms);identities.Add(memory.Title+memory.Note);Check(!memory.Note.Contains("matar")&&!string.IsNullOrEmpty(memory.Title),"Unwelcoming or missing story");}
        Check(identities.Count==144,"Story variants collapsed");
        Check(journal.VisitLandmark("Level94")&&!journal.VisitLandmark("Level94")&&journal.Biomes==2,"Meadow and rain houses conflated");
        int passages=0,lifeRooms=0,nooks=0,notices=0;var passageHashes=new HashSet<string>();
        foreach(float cell in new[]{28,36,60})foreach(int seed in new[]{0,31,481516})
        {
            var c=new LayoutConfig{Seed=seed,CellSize=cell,Zones=ZoneMode.Random,Grammar=ProceduralGrammar.SolarAtlas,AutonomousGeneration=true,IndependentBiomeMaterials=true};WalkableExploration.Configure(c);c.Validate();
            Check(c.EnvironmentRegionSize==3&&c.LoopChance>=.65f&&c.EncounterExploration,"Unified world did not enable frequent discovery");
            ConnectedTerrainSite terrain;Check(ConnectedTerrainTopology.Site(c,0,0,out terrain)&&terrain.GateZ(0)<0&&Math.Abs(terrain.GateZ(0))<=12,"First meadow still too far from spawn");
            Check(!terrain.Contains(0,0),"Meadow swallowed spawn");
            foreach(var destination in new[]{RouteDestination.DifferentBiome,RouteDestination.Meadow,RouteDestination.Mall})
            {
                var route=EncounterRoutes.Find(c,0,0,destination);Check(route!=null&&route.Visited<=EncounterRoutes.MaximumVisited,"Missing bounded walkable route to "+destination+" seed="+seed);
                int xx=0,zz=0;var topology=new ContinuousTopology(c);
                foreach(int d in route.Directions)
                {
                    int exits=ConnectedTerrainTopology.Exits(c,xx,zz,ConnectedMallTopology.Exits(c,xx,zz,topology.Exits(xx,zz)));
                    Check((exits&(1<<d))!=0,"Route crossed a sealed doorway");xx+=Directions.DX[d];zz+=Directions.DZ[d];
                    Check(!ConnectedTerrainTopology.Reserved(c,xx,zz),"Route crosses reserved terrain");
                }
                Check(xx==route.EndX&&zz==route.EndZ,"Route ends at wrong coordinates");
                if(destination==RouteDestination.DifferentBiome)Check(route.Directions.Length<=8,"First alternate biome too far on foot");
                if(destination==RouteDestination.Meadow)Check(route.Directions.Length<=20,"First meadow detour too long");
            }
            var layout=new Layout(c);int differentNear=0;
            for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)if(!ConnectedTerrainTopology.Reserved(c,x,z))
            {
                var room=layout.At(x,z);if(room.Environment!=LiminalEnvironment.Poolrooms)differentNear++;
                if(Math.Abs(x)>2||Math.Abs(z)>2)continue;
                var geometry=Architecture.Build(room,layout);lifeRooms++;
                Check(geometry.Life!=null,"Room has no life plan");if(geometry.Life.Notice.HasValue)notices++;
                Check(geometry.Life.Furniture.Count<=RoomLifePlanner.MaxFurniture,"Unbounded furnishings");if(geometry.Life.Furniture.Count>0)nooks++;
                foreach(var placement in geometry.Life.Furniture)
                {
                    Check(HumanoidRig.Finite(placement.Bottom),"Invalid furniture location");
                    if(placement.Kind==LifeFurnishing.Source&&(placement.Source==7||placement.Source==8||placement.Source==3||placement.Source==9))
                    {
                        var half=SourceFurniture.HalfExtents(placement.Source,placement.Yaw,.08f);
                        foreach(int dx in new[]{-1,0,1})foreach(int dz in new[]{-1,0,1})Check(RegionalSourcePlanner.Supported(geometry.Structure,new V3(placement.Bottom.X+dx*half.X,placement.Bottom.Y-.006f,placement.Bottom.Z+dz*half.Z)),"Unsupported furniture foot");
                    }
                }
                var table=geometry.Life.Furniture.FirstOrDefault(p=>p.Kind==LifeFurnishing.Source&&p.Source==7);
                foreach(var prop in geometry.Life.Furniture.Where(p=>p.Kind==LifeFurnishing.Catalog||p.Kind==LifeFurnishing.Source&&(p.Source==11||p.Source==0||p.Source==12)))
                {
                    float top=table.Bottom.Y+SourceFurniture.Dimensions(7).Y+.006f;
                    Check(Math.Abs(prop.Bottom.Y-top)<.001f,"Tabletop object has no vertical support");
                    var size=prop.Kind==LifeFurnishing.Catalog?MallItemCatalog.Get(prop.Source).Size:SourceFurniture.Dimensions(prop.Source);
                    float angle=-table.Yaw*(float)Math.PI/180;var delta=prop.Bottom-table.Bottom;float xx=(float)Math.Cos(angle)*delta.X+(float)Math.Sin(angle)*delta.Z,zz=-(float)Math.Sin(angle)*delta.X+(float)Math.Cos(angle)*delta.Z;
                    Check(Math.Abs(xx)+Math.Max(size.X,size.Z)*.5f<.45f&&Math.Abs(zz)+Math.Max(size.X,size.Z)*.5f<.45f,"Tabletop object overhangs its real support");
                }
                for(int d=0;d<2;d++)
                {
                    var connector=geometry.Connectors[d];if(connector==null||!connector.Natural)continue;passages++;passageHashes.Add(Hash(connector.Structure));
                    Check(connector.Finishes.Count==2&&connector.Structure.TriangleCount<6000,"Transition budget or finish split");
                    var next=layout.Neighbour(room,d);float start=(d==0?room.Depth:room.Width)*.5f,end=cell-(d==0?next.Depth:next.Width)*.5f;
                    for(int i=0;i<=20;i++)
                    {
                        float p=(float)i/20,z0=start+(end-start)*p,y0=room.Floor+(next.Floor-room.Floor)*p;
                        var pos=d==0?new V3(0,y0+.92f,z0):new V3(z0,y0+.92f,0);
                        Check(SphereClear(connector.Structure,pos,.34f),"Furnishing or transition blocks the central walkway");
                    }
                    foreach(var mesh in connector.Finishes.Append(connector.TransitionGlass))
                        foreach(var indices in mesh.Indices)for(int t=0;t<indices.Count;t+=3)
                        {
                            int ia=indices[t],ib=indices[t+1],ic=indices[t+2];var cross=V3.Cross(mesh.Positions[ib]-mesh.Positions[ia],mesh.Positions[ic]-mesh.Positions[ia]);
                            Check(cross.Length>1e-7f&&V3.Dot(cross.Unit,mesh.Normals[ia])>.8f,"Invalid transition normal/winding "+room.Environment+" -> "+next.Environment+" at "+room.X+","+room.Z+" d="+d+" dot="+V3.Dot(cross.Unit,mesh.Normals[ia])+" length="+cross.Length+" vertices="+mesh.Positions[ia]+" / "+mesh.Positions[ib]+" / "+mesh.Positions[ic]);
                        }
                }
            }
            Check(differentNear>3,"Spawn remains isolated in one biome");
        }
        Check(passages>10&&passageHashes.Count>10&&notices>10&&nooks>0,"New content not exercised in actual unified rooms");
        Console.WriteLine($"Encounter: {lifeRooms} actual room plans, {nooks} furnished nooks, {notices} wall-supported notices and {passages} natural transitions ({passageHashes.Count} meshes) checked; 250-slot arbitrary precision inventory and 100 solved pairing games. Native Unity unrun.");
    }
}
