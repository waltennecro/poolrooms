using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Bounded A* for kilometre-scale regions. The old local BFS remains unchanged.
    // All steps use the real topology and reservations; this does not move a player.
    public static class LargeRegionRoutes
    {
        public const int MaximumVisited=16384;
        readonly struct Node
        {
            public readonly long Key,Score;public readonly int Cost;
            public Node(long key,long score,int cost){Key=key;Score=score;Cost=cost;}
        }
        sealed class Heap
        {
            readonly List<Node> values=new List<Node>();public int Count {get{return values.Count;}}
            static bool Before(Node a,Node b){return a.Score<b.Score||a.Score==b.Score&&a.Cost>b.Cost;}
            public void Push(Node n){int i=values.Count;values.Add(n);while(i>0){int p=(i-1)/2;if(!Before(n,values[p]))break;values[i]=values[p];i=p;}values[i]=n;}
            public Node Pop(){Node result=values[0],last=values[values.Count-1];values.RemoveAt(values.Count-1);int i=0;while(i*2+1<values.Count){int child=i*2+1;if(child+1<values.Count&&Before(values[child+1],values[child]))child++;if(!Before(values[child],last))break;values[i]=values[child];i=child;}if(values.Count>0)values[i]=last;return result;}
        }
        static long Distance(int x,int z,int tx,int tz){return Math.Abs((long)x-tx)+Math.Abs((long)z-tz);}
        static bool Target(LayoutConfig c,int x,int z,RouteDestination destination,LiminalEnvironment selected,out int tx,out int tz)
        {
            tx=x;tz=z;
            if(destination==RouteDestination.SpecificBiome)return LiminalRegions.Find(c,selected,x,z,out tx,out tz);
            if(destination==RouteDestination.DifferentBiome)return LargeRegions.Different(c,x,z,out tx,out tz);
            long best=long.MaxValue;bool found=false;
            if(destination==RouteDestination.Meadow)
            {
                if(!ConnectedTerrainTopology.Nearest(c,x,z,out var site))return false;
                for(int side=0;side<4;side++){int xx=site.GateX(side),zz=site.GateZ(side);long distance=Distance(x,z,xx,zz);if(distance<best){best=distance;tx=xx;tz=zz;found=true;}}
            }
            else if(destination==RouteDestination.Mall)
            {
                int bx=ConnectedMallTopology.Block(x),bz=ConnectedMallTopology.Block(z);
                for(int dz=-4;dz<=4;dz++)for(int dx=-4;dx<=4;dx++)
                {
                    if(!ConnectedMallTopology.Site(c,bx+dx,bz+dz,out var site))continue;
                    foreach(int side in new[]{-1,1}){int xx=site.X+side*(site.RadiusX+1),zz=site.Z;long distance=Distance(x,z,xx,zz);if(distance<best){best=distance;tx=xx;tz=zz;found=true;}}
                }
            }
            return found;
        }
        public static EncounterRoute Find(LayoutConfig c,int x,int z,RouteDestination destination,LiminalEnvironment selected)
        {
            if(c==null||!c.Continuous||ConnectedTerrainTopology.Reserved(c,x,z))return null;
            if(!Target(c,x,z,destination,selected,out int tx,out int tz)||ConnectedTerrainTopology.Reserved(c,tx,tz))return null;
            var topology=new ContinuousTopology(c);var costs=new Dictionary<long,int>();var parents=new Dictionary<long,(long key,int direction)>();var heap=new Heap();
            long origin=ContinuousTopology.CellKey(x,z),goal=ContinuousTopology.CellKey(tx,tz);costs[origin]=0;heap.Push(new Node(origin,Distance(x,z,tx,tz),0));
            // Updates can put stale nodes in the heap. Bound them as well as the unique
            // coordinates so a failed search cannot leave an unbounded priority queue.
            int expanded=0;const int maximumQueued=MaximumVisited*8;
            while(heap.Count>0&&expanded<MaximumVisited*4)
            {
                var node=heap.Pop();if(!costs.TryGetValue(node.Key,out int cost)||cost!=node.Cost)continue;expanded++;
                int xx=(int)(node.Key>>32),zz=unchecked((int)node.Key);
                if(node.Key==goal)
                {
                    var path=new List<int>();long at=goal;while(at!=origin){var step=parents[at];path.Add(step.direction);at=step.key;}path.Reverse();
                    string label=destination==RouteDestination.Meadow?"Colinas · nivel 94":destination==RouteDestination.Mall?"Centro comercial":InteriorPlanner.Name(LiminalRegions.At(c,tx,tz));
                    return new EncounterRoute(x,z,tx,tz,costs.Count,path.ToArray(),label);
                }
                int exits=ConnectedTerrainTopology.Exits(c,xx,zz,ConnectedMallTopology.Exits(c,xx,zz,topology.Exits(xx,zz)));
                for(int side=0;side<4;side++)if((exits&(1<<side))!=0)
                {
                    long nx=(long)xx+Directions.DX[side],nz=(long)zz+Directions.DZ[side];
                    if(nx<int.MinValue||nx>int.MaxValue||nz<int.MinValue||nz>int.MaxValue||ConnectedTerrainTopology.Reserved(c,(int)nx,(int)nz))continue;
                    long key=ContinuousTopology.CellKey((int)nx,(int)nz);bool known=costs.TryGetValue(key,out int previous);
                    if(known&&previous<=cost+1||!known&&costs.Count>=MaximumVisited||heap.Count>=maximumQueued)continue;
                    costs[key]=cost+1;parents[key]=(node.Key,side);heap.Push(new Node(key,cost+1+Distance((int)nx,(int)nz,tx,tz),cost+1));
                }
            }
            return null;
        }
    }
}
