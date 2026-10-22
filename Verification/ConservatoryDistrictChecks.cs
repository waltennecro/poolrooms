using System;
using System.Linq;
using System.Collections.Generic;
using Poolrooms.Core;
static partial class Checks
{
    static void ConservatoryDistricts()
    {
        long before=assertions;int chunks=0,ponds=0,instances=0;var uses=new HashSet<ConservatoryUse>();var models=new HashSet<int>();var fingerprints=new HashSet<string>();
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})foreach(bool tall in new[]{false,true})
        {
            var c=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.Conservatory,RebuildConservatory=true,CellSize=size,DoorWidth=door,DoorHeight=tall?5.5f:door*.5f+2,EncounterExploration=true,CurveSegments=12};var layout=new Layout(c);
            for(int z=-4;z<0;z++)for(int x=-4;x<0;x++)
            {
                var r=layout.At(x,z);var p=r.Plan?.Quiet?.Conservatory;Check(p!=null&&p.Complete,"Direct conservatory lost its coherent district");
                Check(p.Column==x+4&&p.Row==z+4,"Negative-coordinate dome alignment changed");uses.Add(p.Use);
                var g=Architecture.Build(r,layout);chunks++;fingerprints.Add(Hash(g.Structure,g.Decoration));
                foreach(var m in new[]{g.Structure,g.Decoration,g.Glass,g.CollisionOnly}.Concat(g.VisualMeshes))Validate(m);
                Check(g.Structure.TriangleCount<45000&&g.Decoration.TriangleCount<45000&&g.VisualMeshes.Sum(m=>m.TriangleCount)<60000,"Conservatory geometry budget exceeded: "+g.Structure.TriangleCount+" / "+g.Decoration.TriangleCount+" / "+g.VisualMeshes.Sum(m=>m.TriangleCount));
                Check(g.Fixtures.Count<=4&&g.SourceProps.Count+g.SourceSeats.Count<=28,"Conservatory optional work is unbounded");
                Check(g.Glass.TriangleCount>=128,"The greenhouse roof has no physical glass");
                Check(g.Life!=null&&g.Life.Stations.Count==3&&g.Life.Furniture.Count==1,"The conservatory has no real activities/storage");
                foreach(var station in g.Life.Stations)
                {
                    var a=station.Anchor;Check(ArchitecturalDressing.FootprintFits(a.Center,a.Normal,a.A,a.B,a.C),"An activity panel does not fit its support");
                    var at=a.Center+a.Normal*.8f;float support=Support(g.Structure,at);Check(!float.IsNegativeInfinity(support),"A conservatory activity hangs over a void");at.Y=support+.95f;
                    Check(SphereClear(g.Structure,at,.31f),"A conservatory activity is inaccessible");
                }
                foreach(var cabinet in g.Life.Furniture)
                {Check(Math.Abs(Support(g.Structure,cabinet.Bottom)-cabinet.Bottom.Y)<.025f,"Functional cabinet has no floor support");}
                foreach(var b in p.Beds)
                {
                    Check(Math.Abs(b.X)-b.Width*.5f>p.Aisle-.01f&&Math.Abs(b.Z)-b.Depth*.5f>p.Aisle-.01f,"A garden bed seals a through route");
                    Check(!b.Bounds.Overlaps(p.Stair.Expanded(.3f)),"A planter occupies the stair core");
                    Check(p.Pond.Width<=0||!b.Bounds.Overlaps(p.PondBounds.Expanded(.3f)),"A planted bed intersects the reflecting pond");
                }
                var placed=g.SourceProps.Concat(g.SourceSeats).ToArray();instances+=placed.Length;
                foreach(var source in placed)
                {
                    models.Add(source.Source);float support=Support(g.Structure,source.Bottom);
                    foreach(var candidate in placed)if(candidate.Source==7&&source.Bottom.Y>candidate.Bottom.Y+.5f&&Math.Abs(source.Bottom.X-candidate.Bottom.X)<.4f&&Math.Abs(source.Bottom.Z-candidate.Bottom.Z)<.4f)
                        support=Math.Max(support,candidate.Bottom.Y+SourceFurniture.Dimensions(7).Y);
                    Check(Math.Abs(support-source.Bottom.Y)<.025f,"Unsupported conservatory source "+SourceFurniture.Id(source.Source)+" size "+size+" at "+source.Bottom.X+","+source.Bottom.Y+","+source.Bottom.Z+" floor "+support);
                    Check(SourceFurniture.Clear(g.Structure,source,.003f)&&SourceFurniture.Clear(g.CollisionOnly,source,.003f),"Conservatory source intersects architecture or rails: "+SourceFurniture.Id(source.Source)+" size "+size+" cell "+x+","+z+" at "+source.Bottom.X+","+source.Bottom.Y+","+source.Bottom.Z);
                }
                for(int i=0;i<placed.Length;i++)for(int j=i+1;j<placed.Length;j++)
                {
                    var a=placed[i];var b=placed[j];var ah=SourceFurniture.HalfExtents(a.Source,a.Yaw);var bh=SourceFurniture.HalfExtents(b.Source,b.Yaw);
                    bool overlap=Math.Abs(a.Bottom.X-b.Bottom.X)<ah.X+bh.X-.01f&&Math.Abs(a.Bottom.Z-b.Bottom.Z)<ah.Z+bh.Z-.01f&&Math.Min(a.Bottom.Y+ah.Y*2,b.Bottom.Y+bh.Y*2)-Math.Max(a.Bottom.Y,b.Bottom.Y)>.01f;
                    Check(!overlap,"Conservatory source objects overlap each other");
                }
                for(int side=0;side<4;side++)
                {
                    var other=layout.Neighbour(r,side);var np=other.Plan.Quiet.Conservatory;
                    Check(r.HasExit(side)&&other.HasExit(Directions.Opposite(side)),"Conservatory walking graph is not reciprocal");
                    for(int sample=-4;sample<=4;sample++)
                    {
                        float along=sample*size*.12f,px=side%2==0?along:Directions.DX[side]*size*.5f,pz=side%2==0?Directions.DZ[side]*size*.5f:along;
                        float nx=px-Directions.DX[side]*size,nz=pz-Directions.DZ[side]*size;
                        Check(Math.Abs(p.RoofY(px,pz)-np.RoofY(nx,nz))<.0001f,"Dome panes split at a chunk/district seam");
                    }
                    foreach(float y in new[]{.95f,p.Upper+.95f})
                    {
                        var at=new V3(Directions.DX[side]*(size*.5f-.4f),y,Directions.DZ[side]*(size*.5f-.4f));
                        Check(SphereClear(g.Structure,at,.32f),"A greenhouse through route is sealed by a room wall");
                        Check(Math.Abs(Support(g.Structure,new V3(at.X,y-.85f,at.Z))-(y>.95f?p.Upper:0))<.025f,"Upper gallery seam lacks a floor");
                    }
                }
                for(int side=0;side<4;side++)for(int probe=0;probe<=10;probe++)
                {
                    var point=new V3(Directions.DX[side]*size*.45f*probe/10,.95f,Directions.DZ[side]*size*.45f*probe/10);
                    Check(SphereClear(g.Structure,point,.32f),"A programme blocked the common ground crossing");
                }
                CheckConservatoryStairs(g,p);
                if(p.Pond.Width>0)
                {
                    ponds++;Check(g.Regions.Count==1&&g.Water.TriangleCount>0,"The reflecting pond has no water region/render footprint");
                    Check(Math.Abs(Support(g.Structure,new V3(p.Pond.CenterX,.1f,p.Pond.CenterZ))-p.Pond.Bottom)<.025f,"An opaque floor hides the pond bottom: size "+size+" use "+p.Use+" cell "+x+","+z+" pond "+p.Pond.CenterX+","+p.Pond.CenterZ+" support "+Support(g.Structure,new V3(p.Pond.CenterX,.1f,p.Pond.CenterZ)));
                    float sign=p.Court?(p.Column==1?-1:1):-1;
                    for(int step=0;step<3;step++)
                    {
                        float xx=p.Pond.CenterX+sign*(p.Pond.Width*.5f-.22f-step*.38f);
                        Check(Math.Abs(Support(g.Structure,new V3(xx,.15f,p.Pond.CenterZ))-(-step*.215f))<.025f,"The pond exit stairs do not climb towards the edge");
                    }
                }
                // The roof above a true garden void is glass, not a hidden opaque slab.
                float sampleX=p.Aisle+.25f,sampleZ=p.Aisle+.25f;
                Check(float.IsNegativeInfinity(CourtyardCeiling(g.Structure,sampleX,sampleZ,p.Upper+.3f)),"An opaque ceiling fills the conservatory garden void");
            }
        }
        Check(uses.Count==8&&fingerprints.Count>16,"Conservatory programmes lack structural/plant variety");
        foreach(int span in new[]{1,3,4})
        {
            var c=new LayoutConfig{Columns=span,Rows=span,Procedural=LiminalGeneration.Conservatory,RebuildConservatory=true};var l=new Layout(c);int courts=0;
            foreach(var r in l.Rooms)
            {
                if(r.Plan.Quiet.Conservatory.Court)courts++;
                for(int side=0;side<4;side++){var n=l.Neighbour(r,side);Check(n==null?!r.HasExit(side):r.HasExit(side)&&n.HasExit(Directions.Opposite(side)),"Finite greenhouse exposes a world edge");}
            }
            Check(courts==(span==4?4:0),"A finite partial greenhouse advertises an incomplete shared pond");
        }
        foreach(int x in new[]{int.MinValue,-100000,-1,0,int.MaxValue})
        {var p=new ConservatoryDistrictPlan(new LayoutConfig(),x,x,false);Check(p.Column>=0&&p.Column<4&&p.Row>=0&&p.Row<4&&(int)p.Use>=0&&(int)p.Use<8,"Greenhouse indices fail at extreme coordinates");}
        var legacy=new Layout(new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.Conservatory}).At(0,0);Check(legacy.Plan.Quiet.Conservatory==null,"Historical civic generator was silently replaced");
        var mixed=new LayoutConfig{Continuous=true};WalkableExploration.Configure(mixed);var world=new Layout(mixed);int cells=0,edges=0;
        for(int z=-40;z<=40;z++)for(int x=-40;x<=40;x++)
        {
            var r=world.At(x,z);if(r.Plan?.Quiet?.Conservatory==null)continue;cells++;
            for(int side=0;side<4;side++)if(r.HasExit(side)){var n=world.Neighbour(r,side);Check(n.HasExit(Directions.Opposite(side)),"Mixed-world greenhouse has a one-way entrance");if(n.Environment!=r.Environment)edges++;}
        }
        Check(cells>0&&edges>0&&mixed.RebuildConservatory,"Greenhouse cannot be found walking in unified exploration");
        Console.WriteLine("Crystal conservatory: "+chunks+" built chunks, 8 wing uses, "+ponds+" sampled ponds, "+models.Count+" reused source models / "+instances+" sample instances, "+cells+" mixed-world cells / "+edges+" walking boundary entrances; "+(assertions-before)+" mathematical/source assertions. Zero new downloaded models; one new CC0 texture set. Native Unity unrun.");
    }
    static void CheckConservatoryStairs(RoomGeometry g,ConservatoryDistrictPlan p)
    {
        float start=p.Stair.MinZ+ConservatoryDistrictPlan.Landing,end=start+ConservatoryDistrictPlan.Flight,tread=ConservatoryDistrictPlan.Flight/p.Steps,rise=p.Upper/(2*p.Steps);
        Check(tread>=.28f&&rise<=.18001f,"Greenhouse stairs exceed human scale");
        for(int flight=0;flight<2;flight++)for(int step=0;step<p.Steps;step++)
        {
            float x=p.Stair.X+(flight==0?-.9f:.9f),z=flight==0?start+(step+.5f)*tread:end-(step+.5f)*tread,y=flight*p.Upper*.5f+(step+1)*rise;
            Check(Math.Abs(Support(g.Structure,new V3(x,y+.02f,z))-y)<.02f,"A greenhouse stair tread is missing");
            Check(SphereClear(g.Structure,new V3(x,y+1.1f,z),.32f),"Greenhouse stair headroom is obstructed");
        }
    }
}
