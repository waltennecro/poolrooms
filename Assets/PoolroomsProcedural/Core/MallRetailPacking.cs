using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Furnishing density follows real floor area. Bounded local packets reuse the
    // audited packer; no district-wide rejection loop or renderer per stock item.
    public static class MallRetailPacking
    {
        public const int MaximumAssemblies=96;
        public static GeneratedAssembly[] Fill(PlanRect bounds,List<PlanRect> obstacles,AssemblyKind[] kinds,uint seed)
        {
            var result=new List<GeneratedAssembly>();
            int columns=Math.Max(1,(int)Math.Ceiling(bounds.Width/16)),rows=Math.Max(1,(int)Math.Ceiling(bounds.Depth/14));
            for(int row=0;row<rows&&result.Count<MaximumAssemblies;row++)for(int col=0;col<columns&&result.Count<MaximumAssemblies;col++)
            {
                var part=new PlanRect(bounds.MinX+col*bounds.Width/columns+.3f,bounds.MinZ+row*bounds.Depth/rows+.3f,
                    bounds.MinX+(col+1)*bounds.Width/columns-.3f,bounds.MinZ+(row+1)*bounds.Depth/rows-.3f);
                int target=Math.Min(MaximumAssemblies-result.Count,Math.Max(2,(int)Math.Ceiling(part.Width*part.Depth/28)));
                var placements=AssemblyPacking.Fill(part,obstacles,kinds,LiminalRegions.Hash(unchecked((int)seed),col,row,0x4A110u),target,1.0f);
                result.AddRange(placements);
            }
            return result.ToArray();
        }
    }
}
