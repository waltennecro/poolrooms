using System;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
static bool ProvisionFloor(RoomGeometry g,V3 point,out float height)
{
 height=float.NegativeInfinity;
 foreach(var m in new[]{g.Structure,g.CollisionOnly})foreach(var indices in m.Indices)for(int i=0;i<indices.Count;i+=3)
 {
  int ia=indices[i],ib=indices[i+1],ic=indices[i+2];var a=m.Positions[ia];var b=m.Positions[ib];var c=m.Positions[ic];
  if(m.Normals[ia].Y<.98f||Math.Abs(a.Y-b.Y)>.001f||Math.Abs(a.Y-c.Y)>.001f||a.Y>point.Y+.4f||a.Y<point.Y-.25f)continue;
  if(point.X<Math.Min(a.X,Math.Min(b.X,c.X))-.0001f||point.X>Math.Max(a.X,Math.Max(b.X,c.X))+.0001f||point.Z<Math.Min(a.Z,Math.Min(b.Z,c.Z))-.0001f||point.Z>Math.Max(a.Z,Math.Max(b.Z,c.Z))+.0001f)continue;
  double det=(b.Z-c.Z)*(a.X-c.X)+(c.X-b.X)*(a.Z-c.Z);if(Math.Abs(det)<1e-10)continue;
  double u=((b.Z-c.Z)*(point.X-c.X)+(c.X-b.X)*(point.Z-c.Z))/det,v=((c.Z-a.Z)*(point.X-c.X)+(a.X-c.X)*(point.Z-c.Z))/det;
  if(u>=-.0001&&v>=-.0001&&u+v<=1.0001)height=Math.Max(height,a.Y);
 }
 return !float.IsNegativeInfinity(height);
}
static int ProvisionSites(Room r,RoomGeometry g,LayoutConfig c)
{
 var index=new ScenerySurfaceIndex(g,new SceneryWorkBudget());if(!index.Valid)return -1;var s=SourceFurniture.Dimensions(23);bool city=r.Environment==LiminalEnvironment.City11;int valid=0;float clear=Math.Max(c.DoorWidth*.5f+.15f,r.Plan?.ClearHalfWidth??0);
 foreach(var p in ProvisionDistribution.Candidates(c.Seed,r.X,r.Z,r.Width,r.Depth,r.Floor,city))
 {
  if(!city&&(Math.Abs(p.Bottom.X)-s.X*.5f<=clear||Math.Abs(p.Bottom.Z)-s.Z*.5f<=clear))continue;
  if(!ProvisionFloor(g,p.Bottom,out float y))continue;var at=new V3(p.Bottom.X,y,p.Bottom.Z);
  if(g.Regions.Any(w=>w.Contains(at.X,at.Z)&&at.Y<=w.Level+.03f))continue;
  bool fit=true;for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)if(!ProvisionFloor(g,at+new V3(x*(city?s.Z:s.X)*.5f,0,z*(city?s.X:s.Z)*.5f),out float sy)||Math.Abs(sy-y)>.025f)fit=false;
  var half=new V3((city?s.Z:s.X)*.5f+.04f,0,(city?s.X:s.Z)*.5f+.04f);
  if(!fit||!index.Clear(new SceneryBox(at+new V3(-half.X,.02f,-half.Z),at+new V3(half.X,2.02f,half.Z)),0))continue;
  bool access=false;for(int side=-1;side<=1;side+=2)
  {
   var q=at+(city?new V3(side*(s.Z*.5f+.5f),0,0):new V3(0,0,side*(s.Z*.5f+.5f)));
   if(ProvisionFloor(g,q,out float sy)&&Math.Abs(sy-y)<.025f&&index.Clear(new SceneryBox(q+new V3(-.28f,.03f,-.28f),q+new V3(.28f,1.93f,.28f)),0))access=true;
  }
  if(access)valid++;
 }
 return valid;
}
    static void ProvisionFeasibility()
    {
 int pass=0;foreach(var mode in Enum.GetValues<LiminalGeneration>())
 {
  if(mode==LiminalGeneration.Authored||mode==LiminalGeneration.Mixed||mode==LiminalGeneration.MixedWithRetail||mode==LiminalGeneration.Expanded)continue;
  var c=new LayoutConfig();WalkableExploration.Configure(c);c.Procedural=mode;c.DiscoveryCoverage=false;var l=new Layout(c);int sites=0,budget=0,samples=0;
  foreach(int x in new[]{-3,2,7,12}){var r=l.At(x,2);var g=Architecture.Build(r,l);int n=ProvisionSites(r,g,c);samples++;if(n<0)budget++;else sites+=n;}
  Check(sites>0,"No mathematically feasible food station in four actual "+mode+" rooms; budget-rejected rooms: "+budget);if(sites>0)pass++;
 }
        Check(pass==63,"Not every biome had a supported and approachable provision site");
        Console.WriteLine("Provision placement: 252 actual generated rooms across all 63 biomes have at least one supported, dry, approachable station site per biome in a four-room sample. Conservative triangle checks, not Unity PhysX; external mall/house native placement remains pending.");
    }
}
