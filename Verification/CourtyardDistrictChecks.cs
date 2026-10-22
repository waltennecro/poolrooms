using System;
using System.Linq;
using System.Collections.Generic;
using Poolrooms.Core;

static partial class Checks
{
    static void CourtyardDistricts()
    {
        long start=assertions;int chunks=0,suites=0,windows=0,sources=0;var roles=new HashSet<string>();var sourceIds=new HashSet<int>();
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})
        {
            var c=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.Courtyard188,RebuildCourtyard188=true,CellSize=size,DoorWidth=door,DoorHeight=door*.5f+2,EncounterExploration=true,CurveSegments=12};
            var layout=new Layout(c);
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
            {
                var r=layout.At(col-4,row-4);var p=r.Plan?.Quiet?.Courtyard;
                Check(p!=null&&p.Complete,"Complete direct-mode hotel must not fall back to annexes");
                Check(p.Column==col&&p.Row==row,"Negative-coordinate district seam mismatch");
                Check(p.Court==(col>=1&&col<=2&&row>=1&&row<=2),"Four central chunks must share one court");
                var g=Architecture.Build(r,layout);chunks++;suites+=p.Suites.Length;sources+=g.SourceSeats.Count+g.SourceProps.Count;
                Check(g.Structure.TriangleCount<100000&&g.Decoration.TriangleCount<100000,"Hotel geometry budget exceeded");
                Check(g.SourceProps.Count+g.SourceSeats.Count<=48,"Hotel source placement budget exceeded");
                Check(g.Life!=null&&g.Life.Stations.Count==1,"Hotel lacks its native activity");
                foreach(var mesh in new[]{g.Structure,g.Decoration,g.Glass,g.CollisionOnly})Validate(mesh);
                foreach(var mesh in g.VisualMeshes)Validate(mesh);
                foreach(var s in p.Suites)
                {
                    Check(s.Bounds.Width>=3.5f&&s.Bounds.Depth>=3.5f,"Hotel suite is too small for its furnishing");
                    Check(!p.StairCore||!s.Bounds.Overlaps(p.Stair.Expanded(.49f)),"A suite seals the stair shaft");
                }
                var station=g.Life.Stations.Single();
                Check(ArchitecturalDressing.FootprintFits(station.Anchor.Center,station.Anchor.Normal,station.Anchor.A,station.Anchor.B,station.Anchor.C),"Hotel activity support does not fit the actual triangle");
                var approach=station.Anchor.Center+station.Anchor.Normal*.7f;approach.Y=.95f;
                Check(SphereClear(g.Structure,approach,.3f),"Hotel activity approach is blocked");
                foreach(var prop in g.SourceProps.Concat(g.SourceSeats))
                {
                    sourceIds.Add(prop.Source);
                    Check(prop.Source>=0&&prop.Source<SourceFurniture.Count,"Hotel source ID is invalid");
                    float support=Support(g.Structure,prop.Bottom);
                    Check(Math.Abs(support-prop.Bottom.Y)<.04f,"Unsupported hotel source "+SourceFurniture.Id(prop.Source)+" at "+prop.Bottom.X+","+prop.Bottom.Y+","+prop.Bottom.Z+" support "+support+" cell "+col+","+row+" size "+size);
                    Check(SourceFurniture.Clear(g.Structure,prop,.003f),"Hotel furniture intersects architecture: "+SourceFurniture.Id(prop.Source)+" at "+prop.Bottom.X+","+prop.Bottom.Y+","+prop.Bottom.Z+" cell "+col+","+row+" size "+size);
                }
                var placed=g.SourceProps.Concat(g.SourceSeats).ToArray();
                for(int i=0;i<placed.Length;i++)for(int j=i+1;j<placed.Length;j++)
                {
                    var a=placed[i];var b=placed[j];var ah=SourceFurniture.HalfExtents(a.Source,a.Yaw);var bh=SourceFurniture.HalfExtents(b.Source,b.Yaw);
                    bool overlap=Math.Abs(a.Bottom.X-b.Bottom.X)<ah.X+bh.X-.01f&&Math.Abs(a.Bottom.Z-b.Bottom.Z)<ah.Z+bh.Z-.01f&&
                        Math.Min(a.Bottom.Y+ah.Y*2,b.Bottom.Y+bh.Y*2)-Math.Max(a.Bottom.Y,b.Bottom.Y)>.01f;
                    Check(!overlap,"Hotel sources overlap each other: "+SourceFurniture.Id(a.Source)+" and "+SourceFurniture.Id(b.Source));
                }
                foreach(var lamp in g.Fixtures)
                {
                    Check(lamp.MallLocal&&lamp.StoreyHeight>0,"Hotel light lacks its actual storey bounds");
                    int own=-1;
                    for(int f=0;f<CourtyardDistrictPlan.Floors;f++)if(Math.Abs(lamp.Position.Y-lamp.FloorOffset-p.FloorY(f))<.001f)own=f;
                    Check(own>=0,"Hotel light floor offset uses absolute elevation instead of mount height");
                    for(int f=0;f<CourtyardDistrictPlan.Floors;f++)
                        Check(MallLighting.FloorEligible(p.FloorY(f)-lamp.Position.Y,lamp.FloorOffset,lamp.StoreyHeight)==(f==own),"Hotel light is enabled on the wrong storey");
                    Check(lamp.ShadowlessRange>lamp.FloorOffset,"A shadowless hotel light cannot reach its own floor");
                }
                for(int side=0;side<4;side++)
                {
                    var neighbor=layout.Neighbour(r,side);Check(r.HasExit(side)&&neighbor.HasExit(Directions.Opposite(side)),"Hotel graph is not reciprocal");
                    // Internal shared court edges must be open at every height,
                    // while hotel-to-hotel doors exist on all five storeys.
                    if(p.Court&&neighbor.Plan.Quiet.Courtyard.Court)
                    {
                        for(int y=1;y<18;y+=3)Check(SphereClear(g.Structure,new V3(Directions.DX[side]*(size*.5f-.2f),y,Directions.DZ[side]*(size*.5f-.2f)),.15f),"A square-room wall partitions the shared courtyard");
                    }
                    else if(!p.Court&&!neighbor.Plan.Quiet.Courtyard.Court)
                    {
                        for(int f=0;f<CourtyardDistrictPlan.Floors;f++)
                        {var at=new V3(Directions.DX[side]*(size*.5f-.2f),p.FloorY(f)+1,Directions.DZ[side]*(size*.5f-.2f));Check(SphereClear(g.Structure,at,.3f),"Hotel floor seam is sealed");}
                    }
                }
                if(p.Court)
                {
                    Check(g.Glass.TriangleCount==0&&p.Suites.Length==0,"Court must not be a roofed room");
                    Check(float.IsNegativeInfinity(CourtyardCeiling(g.Structure,0,0,.2f)),"The central court has an opaque roof");
                    roles.Add("court");
                }
                else
                {
                    int count=CourtyardDistrictArchitecture.Openings(r,c).Count(w=>!w.Door);windows+=count;
                    Check(CourtyardDistrictArchitecture.Openings(r,c).Count(w=>w.Door)==p.Suites.Length,"A suite has no real door");
                    roles.Add(p.StairCore?"stair":"wing");
                }
                if(p.StairCore)CheckHotelStairs(g,p);
                if(size==36&&door==3&&row==0&&col==1)
                    Check(Hash(g.Structure,g.Decoration)==Hash(Architecture.Build(r,c).Structure,Architecture.Build(r,c).Decoration),"Hotel generation is not deterministic");
            }
        }
        Check(roles.Count==3,"Hotel architecture collapsed into a single square-room programme");
        foreach(int span in new[]{3,4})
        {
            var finite=new LayoutConfig{RebuildCourtyard188=true,Columns=span,Rows=span,Procedural=LiminalGeneration.Courtyard188};
            var layout=new Layout(finite);int courts=0;
            foreach(var room in layout.Rooms)
            {
                if(room.Plan.Quiet.Courtyard.Court)courts++;
                for(int side=0;side<4;side++)
                {
                    var neighbor=layout.Neighbour(room,side);
                    Check(neighbor==null?!room.HasExit(side):room.HasExit(side)&&neighbor.HasExit(Directions.Opposite(side)),"Finite hotel leaves an unguarded world edge or closed internal seam");
                }
            }
            Check(courts==(span==4?4:0),"Finite hotel advertises an incomplete courtyard");
        }
        // The legacy path remains available and preserves its historical defaults.
        var old=new LayoutConfig{Procedural=LiminalGeneration.Courtyard188,Continuous=true};
        Check(new Layout(old).At(1,1).Plan.Quiet.Courtyard==null,"Historical fixture defaults changed");
        var unified=new LayoutConfig{LargeProceduralRegions=true};WalkableExploration.Configure(unified);
        Check(unified.RebuildCourtyard188,"Unified exploration never enables rebuilt architecture");
        int complete=0,annex=0;
        foreach(int seed in new[]{17,481516,-917})
        {
            unified.Seed=seed;int x,z;Check(LiminalRegions.Find(unified,LiminalEnvironment.Courtyard188,0,0,out x,out z),"188 cannot be found walking in the mixed world");
            var mixed=new Layout(unified);
            for(int dz=-12;dz<=12;dz++)for(int dx=-12;dx<=12;dx++)
            {
                if(!CourtyardDistrictPlanner.Available(unified,x+dx,z+dz))continue;
                var r=mixed.At(x+dx,z+dz);var p=r.Plan.Quiet.Courtyard;
                if(p.Complete)complete++;else annex++;
                Check(p.Complete||!p.Court,"Partial mixed region spawns half a floating courtyard");
                for(int side=0;side<4;side++)if(CourtyardDistrictPlanner.Same(unified,r.X,r.Z,side))
                {var n=mixed.Neighbour(r,side);Check(r.HasExit(side)&&n.HasExit(Directions.Opposite(side)),"Mixed-world hotel path is not reciprocal");}
            }
        }
        Check(complete>0&&annex>0,"Mixed world does not contain both whole hotels and boundary annexes");
        UnityNativeLifetimes();
        Console.WriteLine($"PASS: rebuilt Level188, {chunks} chunks, {suites} suites, {windows} physical windows, {sources} licensed-source instances/{sourceIds.Count} distinct source models; {complete} whole-hotel cells/{annex} boundary annexes sampled; {assertions-start} mathematical/source assertions. Native Unity visual/physics tests unrun.");
    }
    static float CourtyardCeiling(MeshData mesh,float x,float z,float above)
    {
        var reversed=new MeshData();foreach(var p in mesh.Positions)reversed.Positions.Add(new V3(p.X,-p.Y,p.Z));
        for(int i=0;i<6;i++)reversed.Indices[i].AddRange(mesh.Indices[i]);
        return Support(reversed,new V3(x,-above,z));
    }
    static void CheckHotelStairs(RoomGeometry g,CourtyardDistrictPlan p)
    {
        var b=p.Stair;float start=b.MinZ+CourtyardDistrictPlan.Landing,end=start+CourtyardDistrictPlan.Flight;
        for(int floor=0;floor<CourtyardDistrictPlan.Floors;floor++)
        {
            int n=p.Steps(floor);float rise=p.Rise(floor),tread=CourtyardDistrictPlan.Flight/n;
            Check(rise/(n*2)<=.181f&&tread>=.25f,"Hotel stairs exceed human step dimensions");
            for(int flight=0;flight<2;flight++)for(int step=0;step<n;step++)
            {
                float y=p.FloorY(floor)+rise*flight*.5f+(step+1)*rise/(n*2);
                var point=new V3(b.X+(flight==0?-.9f:.9f),y+.01f,flight==0?start+(step+.5f)*tread:end-(step+.5f)*tread);
                Check(Math.Abs(Support(g.Structure,point)-y)<.003f,"A hotel stair tread is missing");
                point.Y=y+1.1f;Check(SphereClear(g.Structure,point,.3f),"Hotel stair has no headroom");
            }
            var landing=new V3(b.X,p.FloorY(floor)+rise*.5f+.01f,(end+b.MaxZ)*.5f);
            Check(Math.Abs(Support(g.Structure,landing)-(p.FloorY(floor)+rise*.5f))<.003f,"Dogleg stair landing is missing");
        }
    }
}
