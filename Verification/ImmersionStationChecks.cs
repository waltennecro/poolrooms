using System;
using System.Linq;
using System.Collections.Generic;
using Poolrooms.Core;
static partial class Checks
{
    static void ImmersionStationWorld()
    {
        var hashes=new HashSet<string>();
        foreach(StationKind kind in Enum.GetValues(typeof(StationKind)))for(int variant=0;variant<3;variant++)
        {
            var mesh=ImmersionStations.Model(kind,variant);Validate(mesh);hashes.Add(Hash(mesh));
            Check(mesh.TriangleCount>20&&mesh.TriangleCount<2000,"Station model has no detail or exceeds budget");
            foreach(var p in mesh.Positions)Check(Math.Abs(p.X)<=ArchitecturalDressing.HalfWidth&&Math.Abs(p.Y)<=ArchitecturalDressing.HalfHeight&&p.Z>=-.001f&&p.Z<=.19f,"Station leaves its supported wall footprint");
            Check(!string.IsNullOrEmpty(ImmersionStations.Name(kind))&&(int)ImmersionStations.Sound(kind)<16,"Missing station action/sound");
        }
        Check(hashes.Count>=12,"Station geometries collapsed");
        for(uint seed=0;seed<1000;seed++)
        {
            var game=new SlidingTiles(seed);Check(!game.Complete&&game.Tiles.Distinct().Count()==16&&game.Tiles.Min()==0&&game.Tiles.Max()==15,"Invalid sliding puzzle");
            int inversions=0;for(int a=0;a<16;a++)for(int b=a+1;b<16;b++)if(game.Tiles[a]>0&&game.Tiles[b]>0&&game.Tiles[a]>game.Tiles[b])inversions++;
            int rowFromBottom=4-Array.IndexOf(game.Tiles,0)/4;Check((inversions+rowFromBottom)%2==1,"Sliding puzzle is unsolvable");
            int before=game.Moves;Check(!game.Slide(-1)&&!game.Slide(16)&&game.Moves==before,"Invalid puzzle moves modify state");
            int blank=Array.IndexOf(game.Tiles,0),next=Enumerable.Range(0,16).First(x=>SlidingTiles.Adjacent(x,blank));Check(game.Slide(next)&&game.Tiles[next]==0&&game.Moves==before+1,"Valid slide failed");
            var mosaic=new MosaicTiles(seed);Check(!mosaic.Complete,"Mosaic already solved");for(int i=0;i<9;i++)for(int attempt=0;attempt<4&&mosaic.Turns[i]!=mosaic.Target[i];attempt++)mosaic.Turn(i);Check(mosaic.Complete,"Mosaic is unsolvable");
        }
        int rooms=0,stations=0,withStations=0;var kinds=new HashSet<StationKind>();
        foreach(int seed in new[]{0,31,481516})
        {
            var config=new LayoutConfig{Seed=seed,CellSize=36,Zones=ZoneMode.Random,Grammar=ProceduralGrammar.SolarAtlas,AutonomousGeneration=true};WalkableExploration.Configure(config);var layout=new Layout(config);
            for(int x=-4;x<=4;x++)for(int z=-4;z<=4;z++)
            {
                if(ConnectedTerrainTopology.Reserved(config,x,z))continue;var room=layout.At(x,z);var geometry=Architecture.Build(room,layout);rooms++;
                var list=geometry.Life.Stations;Check(list.Count<=4,"Station count exceeds budget");if(list.Count>0)withStations++;
                foreach(var p in list)
                {
                    Check(ArchitecturalDressing.FootprintFits(p.Anchor.Center,p.Anchor.Normal,p.Anchor.A,p.Anchor.B,p.Anchor.C),"Station not supported by a real wall");
                    Check(p.Anchor.Center.Y>=room.Floor+.95f&&p.Anchor.Center.Y<=room.Floor+1.75f,"Station is out of reach");
                    if(geometry.Life.Notice.HasValue)Check((p.Anchor.Center-geometry.Life.Notice.Value.Center).Length>=1.05f,"Station overlaps the room notice");
                    foreach(var other in list)if(other.Slot!=p.Slot)Check((p.Anchor.Center-other.Anchor.Center).Length>=1.1f,"Overlapping stations");
                    stations++;kinds.Add(p.Kind);
                }
            }
        }
        Check(withStations>rooms/2&&stations>rooms&&kinds.Count==WorkshopCatalog.StationCount,"New activities are too rare or missing from unified exploration");
        Console.WriteLine($"Immersion: {rooms} actual unified rooms, {withStations} rooms with activities, {stations} supported stations and {kinds.Count} activity types; 1000 solvable sliding and mosaic puzzles, {WorkshopCatalog.StationCount*3} model/finish combinations checked. Native Unity unrun.");
    }
}
