using System;
using System.Linq;
using System.Text.Json;
using Poolrooms.Core;

static partial class Checks
{
    static void ExportMallAtlas()
    {
        var plans=new[]{0,31,81,942,481516,10111}.Select(seed=>MallPlan.Create(seed,13,0,true,true,true,true,true,true,true)).ToArray();
        var data=plans.Select(p=>new{seed=p.Seed,width=p.Width,depth=p.Depth,floors=p.Floors,atrium=p.Design.Shape.ToString(),roof=p.Design.Roof.ToString(),
            shops=p.Shops.Select(s=>new{id=s.Id,kind=s.Kind.ToString(),floor=s.Floor,x=s.X,width=s.Width,side=s.Side,depth=s.Depth,ceiling=(s.Seed>>8)%6,finish=s.Seed%4}),
            slabs=Enumerable.Range(0,p.Floors).Select(f=>p.FloorPlates(f).Select(r=>new{x0=r.X0,z0=r.Z0,x1=r.X1,z1=r.Z1})),
            holes=Enumerable.Range(0,p.Floors).Select(f=>p.Design.Voids(f).Select(r=>new{x0=r.X0,z0=r.Z0,x1=r.X1,z1=r.Z1}))});
        Console.WriteLine(JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
    }
}
