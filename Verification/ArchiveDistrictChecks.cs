using System;
using System.Linq;
using System.Collections.Generic;
using Poolrooms.Core;

static partial class Checks
{
    static void ArchiveDistricts()
    {
        long start=assertions;int chunks=0,sourceCount=0;var uses=new HashSet<ArchiveWingUse>();var sources=new HashSet<int>();var shapes=new HashSet<string>();
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})foreach(bool tall in new[]{false,true})
        {
            var c=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.Archive906,RebuildArchive906=true,CellSize=size,DoorWidth=door,DoorHeight=tall?5.5f:door*.5f+2,EncounterExploration=true,CurveSegments=12};
            var layout=new Layout(c);
            for(int z=-1;z<=1;z++)for(int x=-2;x<=0;x++)
            {
                var r=layout.At(x,z);var p=r.Plan.Quiet.Archive;Check(p!=null,"Missing reconstructed Archive906 plan");chunks++;if(!p.Nave)for(int f=0;f<3;f++)uses.Add((ArchiveWingUse)(((int)p.Use+f)%5));
                Check(p.Column>=0&&p.Column<3&&p.Nave==(p.Column==1),"Library macro alignment fails at negative coordinates");
                var g=Architecture.Build(r,layout);shapes.Add(Hash(g.Structure,g.Decoration));
                Check(g.Structure.Theme==SurfaceTheme.CygnusWood,"Generic room envelope returned instead of dedicated library");
                Check(g.Structure.TriangleCount<80000&&g.VisualMeshes.Sum(m=>m.TriangleCount)<250000,"Library geometry exceeds bounded chunk budget");
                Check(g.SourceProps.Count+g.SourceSeats.Count<=48,"Library source placement budget exceeded");
                foreach(var m in new[]{g.Structure,g.Decoration,g.Glass,g.CollisionOnly})Validate(m);
                foreach(var m in g.VisualMeshes)Validate(m);
                Check(g.Life!=null&&g.Life.Stations.Count==3,"Library lacks three floor-specific activities");
                if(!p.Nave)for(int f=0;f<3;f++)if(((int)p.Use+f)%5==(int)ArchiveWingUse.Guest)
                    Check(g.VisualMeshes.Any(m=>m.Theme==SurfaceTheme.CygnusVelvet&&m.Positions.Any(v=>Math.Abs(v.Y-p.FloorY(f)-.55f)<.001f)),"Guest cushions must use textile PBR instead of the paper binding slot");
                foreach(var station in g.Life.Stations)
                {
                    var a=station.Anchor;Check(ArchitecturalDressing.FootprintFits(a.Center,a.Normal,a.A,a.B,a.C),"Library activity board footprint is invalid");
                    var approach=a.Center+a.Normal*.8f;float floor=Support(g.Structure,new V3(approach.X,approach.Y,approach.Z));
                    Check(!float.IsNegativeInfinity(floor),"Library activity hangs over an atrium void");approach.Y=floor+.95f;
                    Check(SphereClear(g.Structure,approach,.31f),"Library activity approach is obstructed");
                }
                var placed=g.SourceProps.Concat(g.SourceSeats).ToArray();sourceCount+=placed.Length;
                foreach(var prop in placed)
                {
                    sources.Add(prop.Source);float support=Support(g.Structure,prop.Bottom);
                    Check(Math.Abs(support-prop.Bottom.Y)<.025f,"Unsupported library source "+SourceFurniture.Id(prop.Source)+" cell "+x+","+z+" size "+size+" y "+prop.Bottom.Y+" support "+support);
                    Check(SourceFurniture.Clear(g.Structure,prop,.003f),"Library source intersects architecture: "+SourceFurniture.Id(prop.Source)+" cell "+x+","+z+" size "+size+" at "+prop.Bottom.X+","+prop.Bottom.Y+","+prop.Bottom.Z);
                }
                for(int i=0;i<placed.Length;i++)for(int j=i+1;j<placed.Length;j++)
                {
                    var a=placed[i];var b=placed[j];var ah=SourceFurniture.HalfExtents(a.Source,a.Yaw);var bh=SourceFurniture.HalfExtents(b.Source,b.Yaw);
                    bool overlap=Math.Abs(a.Bottom.X-b.Bottom.X)<ah.X+bh.X-.01f&&Math.Abs(a.Bottom.Z-b.Bottom.Z)<ah.Z+bh.Z-.01f&&Math.Min(a.Bottom.Y+ah.Y*2,b.Bottom.Y+bh.Y*2)-Math.Max(a.Bottom.Y,b.Bottom.Y)>.01f;
                    Check(!overlap,"Library furniture overlaps: "+SourceFurniture.Id(a.Source)+" / "+SourceFurniture.Id(b.Source));
                }
                foreach(var lamp in g.Fixtures.Where(f=>f.MallLocal))
                {
                    int own=-1;for(int f=0;f<3;f++)if(Math.Abs(lamp.Position.Y-lamp.FloorOffset-p.FloorY(f))<.001f)own=f;
                    Check(own>=0,"Library floor-local light uses absolute mount offset");
                    for(int f=0;f<3;f++)Check(MallLighting.FloorEligible(p.FloorY(f)-lamp.Position.Y,lamp.FloorOffset,lamp.StoreyHeight)==(f==own),"Library light leaks to a different storey eligibility");
                }
                Check(g.Fixtures.Count<=20,"Library fixture count is not bounded");
                for(int sample=-4;sample<=4;sample++)
                {
                    var at=new V3(p.Nave?size*.5f-2:0,.9f,sample*size*.11f);
                    Check(g.Fixtures.Any(l=>(l.Position-at).Length<l.Range),"Large library route lies outside every local fixture range");
                }
                for(int side=0;side<4;side++)
                {
                    var neighbor=layout.Neighbour(r,side);Check(r.HasExit(side)&&neighbor.HasExit(Directions.Opposite(side)),"Library walking seams are not reciprocal");
                    for(int f=0;f<3;f++)
                    {
                        // The nave continues along its side galleries; cross bridges link the wings.
                        float offset=p.Nave&&side%2==0?size*.5f-2:0;
                        var at=new V3(Directions.DX[side]*(size*.5f-.35f)+(side%2==0?offset:0),p.FloorY(f)+1,Directions.DZ[side]*(size*.5f-.35f));
                        Check(SphereClear(g.Structure,at,.3f),"A square-room wall blocks a library gallery seam");
                        Check(Math.Abs(Support(g.Structure,new V3(at.X,p.FloorY(f)+.1f,at.Z))-p.FloorY(f))<.025f,"Library gallery seam has no walkable floor");
                    }
                }
                CheckArchiveStairs(g,p);
                if(p.Nave)
                {
                    Check(g.Glass.TriangleCount>0,"Nave has no true glazed roof apertures");
                    Check(Math.Abs(Support(g.Structure,new V3(0,p.FloorY(2)+.1f,size*.25f)))<.01f,"A generic upper slab fills the library nave");
                    Check(-CourtyardCeiling(g.Structure,0,size*.25f,.2f)>p.Spring,"Library vault collapsed into a flat low ceiling");
                    // At least one glass panel must have no opaque roof underneath it.
                    var v=(g.Glass.Positions[0]+g.Glass.Positions[1]+g.Glass.Positions[2])*(1f/3);
                    Check(float.IsNegativeInfinity(CourtyardCeiling(g.Structure,v.X,v.Z,p.Spring-.01f)),"An opaque slab backs the stained glass");
                }
            }
        }
        Check(uses.Count==5&&shapes.Count>12,"Library programmes lack structural variety");
        foreach(int span in new[]{1,3})
        {
            var c=new LayoutConfig{Columns=span,Rows=span,RebuildArchive906=true,Procedural=LiminalGeneration.Archive906};var l=new Layout(c);
            foreach(var r in l.Rooms)for(int side=0;side<4;side++)
            {var n=l.Neighbour(r,side);Check(n==null?!r.HasExit(side):r.HasExit(side)&&n.HasExit(Directions.Opposite(side)),"Finite library world edges are unguarded");}
        }
        foreach(int z in new[]{int.MinValue,-100000,-123,-11,0,123,int.MaxValue})
        {var plan=new ArchiveDistrictPlan(new LayoutConfig(),-100000,z);Check((int)plan.Use>=0&&(int)plan.Use<5,"Archive programme index fails at negative/extreme coordinates");}
        var legacy=new Layout(new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.Archive906}).At(0,0);
        Check(legacy.Plan.Quiet.Archive==null&&legacy.Plan.Quiet.Floors==2,"Historical Archive906 geometry was silently changed");
        var unified=new LayoutConfig{Continuous=true};WalkableExploration.Configure(unified);
        Check(unified.RebuildArchive906,"Unified exploration omitted the new library");
        var mixed=new Layout(unified);int found=0,boundaries=0;
        for(int z=-40;z<=40;z++)for(int x=-40;x<=40;x++)
        {
            var r=mixed.At(x,z);if(r.Plan?.Quiet?.Archive==null)continue;found++;
            for(int side=0;side<4;side++)if(r.HasExit(side))
            {var n=mixed.Neighbour(r,side);Check(n.HasExit(Directions.Opposite(side)),"Mixed-world library entrance is one-way");if(n.Environment!=r.Environment)boundaries++;}
        }
        Check(found>0&&boundaries>0,"Library cannot be encountered walking from another biome");
        Console.WriteLine("Cygnus library: "+chunks+" built chunks, "+uses.Count+" wing uses, "+sources.Count+" reused source models / "+sourceCount+" sample instances, "+found+" mixed-world cells, "+boundaries+" boundary entrances; "+(assertions-start)+" mathematical/source assertions. Native Unity not executed.");
    }
    static void CheckArchiveStairs(RoomGeometry g,ArchiveDistrictPlan p)
    {
        float start=p.Stair.MinZ+ArchiveDistrictPlan.Landing,end=start+ArchiveDistrictPlan.Flight;
        for(int f=0;f<2;f++)
        {
            int steps=p.Steps(f);float tread=ArchiveDistrictPlan.Flight/steps,rise=p.Rise(f)*.5f/steps;
            Check(tread>=.28f&&rise<=.18001f,"Library stairs exceed human scale");
            for(int flight=0;flight<2;flight++)for(int step=0;step<steps;step++)
            {
                float x=p.Stair.X+(flight==0?-.9f:.9f),z=flight==0?start+(step+.5f)*tread:end-(step+.5f)*tread;
                float y=p.FloorY(f)+flight*p.Rise(f)*.5f+(step+1)*rise;
                Check(Math.Abs(Support(g.Structure,new V3(x,y+.02f,z))-y)<.02f,"Library stair tread is missing");
                float ceiling=-CourtyardCeiling(g.Structure,x,z,y+.06f);
                Check(float.IsPositiveInfinity(ceiling)||ceiling-y>2.05f,"A library floor slab blocks stair headroom: cell "+p.Cell+" nave "+p.Nave+" f "+f+" flight "+flight+" step "+step+" y "+y+" ceiling "+ceiling);
            }
        }
    }
}
