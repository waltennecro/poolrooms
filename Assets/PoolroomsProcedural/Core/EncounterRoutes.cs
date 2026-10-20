using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum RouteDestination { DifferentBiome,Mall,Meadow,SpecificBiome }
    public sealed class EncounterRoute
    {
        public readonly int StartX,StartZ,EndX,EndZ,Visited;
        public readonly int[] Directions;
        public readonly string Label;
        public EncounterRoute(int x,int z,int endX,int endZ,int visited,int[] directions,string label){StartX=x;StartZ=z;EndX=endX;EndZ=endZ;Visited=visited;Directions=directions;Label=label;}
    }
    // Finds actual graph edges, never builds room meshes, teleports or changes the world.
    public static class EncounterRoutes
    {
        public const int MaximumVisited=4096;
        public static EncounterRoute Find(LayoutConfig config,int x,int z,RouteDestination destination,LiminalEnvironment selected=LiminalEnvironment.Poolrooms)
        {
            if(config==null||!config.Continuous||ConnectedTerrainTopology.Reserved(config,x,z))return null;
            var topology=new ContinuousTopology(config);var parents=new Dictionary<long,(long parent,int direction)>();var queue=new Queue<long>();
            var initial=LiminalRegions.At(config,x,z);long origin=ContinuousTopology.CellKey(x,z);parents.Add(origin,(origin,-1));queue.Enqueue(origin);
            while(queue.Count>0)
            {
                long key=queue.Dequeue();int xx=(int)(key>>32),zz=unchecked((int)key);bool goal=false;string label="";
                if(destination==RouteDestination.Mall&&ConnectedMallTopology.Gateway(config,xx,zz,out _)){goal=true;label="Centro comercial";}
                else if(destination==RouteDestination.Meadow&&ConnectedTerrainTopology.Gateway(config,xx,zz,out _)){goal=true;label="Colinas · nivel 94";}
                else if(destination==RouteDestination.DifferentBiome||destination==RouteDestination.SpecificBiome)
                {var at=LiminalRegions.At(config,xx,zz);goal=destination==RouteDestination.DifferentBiome?at!=initial:at==selected;label=at.ToString();}
                if(goal)
                {
                    var path=new List<int>();long at=key;while(at!=origin){var step=parents[at];path.Add(step.direction);at=step.parent;}path.Reverse();
                    return new EncounterRoute(x,z,xx,zz,parents.Count,path.ToArray(),label);
                }
                int bits=ConnectedTerrainTopology.Exits(config,xx,zz,ConnectedMallTopology.Exits(config,xx,zz,topology.Exits(xx,zz)));
                for(int d=0;d<4;d++)if((bits&(1<<d))!=0)
                {
                    long nx=(long)xx+Poolrooms.Core.Directions.DX[d],nz=(long)zz+Poolrooms.Core.Directions.DZ[d];
                    if(nx<int.MinValue||nx>int.MaxValue||nz<int.MinValue||nz>int.MaxValue||ConnectedTerrainTopology.Reserved(config,(int)nx,(int)nz))continue;
                    long next=ContinuousTopology.CellKey((int)nx,(int)nz);if(parents.ContainsKey(next)||parents.Count>=MaximumVisited)continue;
                    parents.Add(next,(key,d));queue.Enqueue(next);
                }
            }
            return null;
        }
    }
}
