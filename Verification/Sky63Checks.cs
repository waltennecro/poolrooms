using System;
using System.Linq;
using System.Collections.Generic;
using Poolrooms.Core;
static partial class Checks
{
    static void Sky63()
    {
        long before=assertions;int chunks=0,instances=0,entrances=0;var uses=new HashSet<Sky63Use>();var models=new HashSet<int>();var shapes=new HashSet<string>();
        foreach(int size in new[]{28,36,60})foreach(int door in new[]{3,6})foreach(bool tall in new[]{false,true})
        {
            var c=new LayoutConfig{Continuous=true,RebuildSky63=true,Procedural=LiminalGeneration.CloudTerminal,CellSize=size,DoorWidth=door,DoorHeight=tall?5.5f:door*.5f+2,EncounterExploration=true};var layout=new Layout(c);
            for(int z=-4;z<0;z++)for(int x=-4;x<0;x++)
            {
                var r=layout.At(x,z);var p=r.Plan?.Quiet?.Sky63;Check(p!=null,"Sky63 was sent to the generic box builder");uses.Add(p.Use);
                var g=Architecture.Build(r,layout);chunks++;shapes.Add(Hash(g.Structure,g.Decoration));
                foreach(var m in new[]{g.Structure,g.Decoration,g.Glass,g.CollisionOnly}.Concat(g.VisualMeshes))Validate(m);
                Check(g.Structure.TriangleCount<25000&&g.Decoration.TriangleCount<40000&&g.VisualMeshes.Sum(v=>v.TriangleCount)<25000,"Sky63 unbounded geometry");
                Check(g.Glass.TriangleCount==0&&g.Water.TriangleCount==0&&g.Fixtures.Count==0,"Sky63 acquired an indoor ceiling, artificial fixture or fake pool");
                Check(g.Life!=null&&g.Life.Stations.Count==3&&g.Life.Furniture.Count==1,"Sky63 has no usable activities/storage");
                foreach(var station in g.Life.Stations)
                {
                    var a=station.Anchor;Check(ArchitecturalDressing.FootprintFits(a.Center,a.Normal,a.A,a.B,a.C),"Sky activity protrudes from its panel");
                    var at=a.Center+a.Normal*.8f;float y=Support(g.Structure,at);Check(!float.IsNegativeInfinity(y),"Sky activity approach has no floor");at.Y=y+.95f;
                    Check(SphereClear(g.Structure,at,.31f)&&SphereClear(g.CollisionOnly,at,.31f),"Sky activity approach is blocked");
                }
                foreach(var f in g.Life.Furniture)Check(Math.Abs(Support(g.Structure,f.Bottom)-f.Bottom.Y)<.02f,"Sky cabinet floats");
                for(int side=0;side<4;side++)
                {
                    var n=layout.Neighbour(r,side);Check(r.HasExit(side)&&n.HasExit(Directions.Opposite(side)),"Sky walking edges are not reciprocal");
                    foreach(float y in new[]{.95f,p.Upper+.95f})
                    {
                        var a=new V3(Directions.DX[side]*(p.Half-.2f),y,Directions.DZ[side]*(p.Half-.2f));
                        Check(SphereClear(g.Structure,a,.32f)&&SphereClear(g.CollisionOnly,a,.32f),"A sky boundary walkway is sealed");
                        a.Y=y-.9f;Check(Math.Abs(Support(g.Structure,a)-(y-.95f))<.01f,"A sky boundary has no matching floor");
                    }
                }
                // No infinite flat floor: each corner and area between routes must include sky voids.
                float empty=0;
                for(int iz=0;iz<12;iz++)for(int ix=0;ix<12;ix++)if(!p.Supports(-p.Half+(ix+.5f)*size/12,-p.Half+(iz+.5f)*size/12,false))empty++;
                Check(empty>=12,"Sky63 collapsed into a full square room floor");
                float start=p.Stair.MinZ+Sky63Plan.Landing,tread=Sky63Plan.Flight/p.Steps;
                Check(p.Upper/(p.Steps*2)<=.181f&&tread>=.28f,"Sky stair is not human scale");
                for(int flight=0;flight<2;flight++)for(int step=0;step<p.Steps;step++)
                {
                    float expected=flight*p.Upper*.5f+(step+1)*p.Upper/(p.Steps*2);
                    var a=new V3(p.Stair.X+(flight==0?-.9f:.9f),expected+.03f,flight==0?start+(step+.5f)*tread:start+Sky63Plan.Flight-(step+.5f)*tread);
                    Check(Math.Abs(Support(g.Structure,a)-expected)<.015f,"Sky stair tread has no real support");a.Y=expected+1;
                    Check(SphereClear(g.Structure,a,.31f)&&SphereClear(g.CollisionOnly,a,.31f),"Sky stair is blocked or has insufficient headroom");
                }
                foreach(var a in g.SourceProps.Concat(g.SourceSeats))
                {
                    instances++;models.Add(a.Source);float y=Support(g.Structure,a.Bottom);
                    foreach(var table in g.SourceProps)if(table.Source==7&&Math.Abs(a.Bottom.X-table.Bottom.X)<.4f&&Math.Abs(a.Bottom.Z-table.Bottom.Z)<.4f&&a.Bottom.Y>.5f)y=Math.Max(y,table.Bottom.Y+SourceFurniture.Dimensions(7).Y);
                    Check(Math.Abs(y-a.Bottom.Y)<.025f,"Sky source floats: "+SourceFurniture.Id(a.Source));
                    Check(SourceFurniture.Clear(g.Structure,a,.003f)&&SourceFurniture.Clear(g.CollisionOnly,a,.003f),"Sky source intersects a rail/structure");
                }
                Check(g.SourceSeats.Count>=3,"Sky meditation/tea seats were all rejected");
            }
        }
        Check(uses.Count==8&&shapes.Count>=48,"Sky programmes/scales lost variation");
        foreach(int x in new[]{int.MinValue,-1,0,int.MaxValue})
        {var p=new Sky63Plan(new LayoutConfig{CellSize=28},x,x);Check(p.Column>=0&&p.Column<4&&p.Row>=0&&p.Row<4,"Sky coordinate modulo overflow");}
        foreach(int size in new[]{1,3,4})
        {
            var c=new LayoutConfig{Continuous=false,Columns=size,Rows=size,RebuildSky63=true,Procedural=LiminalGeneration.CloudTerminal,CellSize=28};var layout=new Layout(c);
            foreach(var r in layout.Rooms)for(int side=0;side<4;side++)if((r.X==0&&side==3)||(r.Z==0&&side==2)||(r.X==size-1&&side==1)||(r.Z==size-1&&side==0))Check(!r.HasExit(side),"Finite sky edge opens outside the map");
        }
        var mixed=new LayoutConfig();WalkableExploration.Configure(mixed);var world=new Layout(mixed);int cells=0;
        for(int z=-18;z<=18;z++)for(int x=-18;x<=18;x++)
        {
            var r=world.At(x,z);if(r.Plan?.Quiet?.Sky63==null)continue;cells++;
            for(int d=0;d<4;d++)if(r.HasExit(d))
            {var other=world.Neighbour(r,d);if(other.Plan?.Quiet?.Sky63==null){Check(other.HasExit(Directions.Opposite(d)),"Sky entry is not reciprocal");var g=d<2?Corridors.Build(r,other,d,mixed):Corridors.Build(other,r,d-2,mixed);Validate(g.Structure);entrances++;}}
        }
        Check(cells>0&&entrances>0,"Sky63 is only available in master mode");
        Check(!new LayoutConfig().RebuildSky63,"Historical fixtures no longer preserve the generic programme");
        Check(Sky63Plan.Recover(-25,false,false)&&!Sky63Plan.Recover(-25,true,false)&&!Sky63Plan.Recover(-25,false,true)&&!Sky63Plan.Recover(float.NaN,false,false)&&!Sky63Plan.Recover(-20,false,false),"Sky fall recovery ignores pause/wings/invalid input");
        Console.WriteLine($"Sky63: {chunks} built chunks, {uses.Count} programmes, {models.Count} existing source models / {instances} sample instances; {cells} mixed-world cells / {entrances} walking entrances. {assertions-before} mathematical/source assertions. No new downloaded models; native Unity/HDRP/physics/audio/FPS unrun.");
    }
}
