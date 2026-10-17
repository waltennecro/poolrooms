using System;

namespace Poolrooms.Core
{
    public readonly struct SourceFurniturePlacement
    {
        public readonly int Slot,Source;
        public readonly V3 Bottom;
        public readonly float Yaw;
        public SourceFurniturePlacement(int slot,int source,V3 bottom,float yaw)
        {Slot=slot;Source=source;Bottom=bottom;Yaw=yaw;}
    }
    // Existing city IDs 0..2 retain their meaning; only new source choices append.
    // Dimensions are normalized from the adapted model bounds and audited against
    // the actual resource JSON, so placement uses the same scale as native loading.
    public static class SourceFurniture
    {
        public static string Id(int source)
        {
            switch(source){case 0:return "WaterBottle";case 1:return "BoomBox";case 2:return "SheenChair";case 3:return "I_GlamVelvetSofa";case 4:return "I_ChairDamaskPurplegold";case 5:return "U_SheenWoodLeatherSofa";case 6:return "U_AntiqueCamera";default:throw new ArgumentOutOfRangeException(nameof(source));}
        }
        public static bool IsSeat(int source){return source>=2&&source<=5;}
        public static int Index(string source){for(int i=0;i<7;i++)if(Id(i)==source)return i;throw new ArgumentException("Unknown placed source: "+source);}
        public static float Longest(int source){return source==0?.28f:source==1?.4f:source==3||source==5?2:source==6?1.5f:1;}
        public static float Longest(string source)
        {if(source=="Lantern")return 1.3f;return Longest(Index(source));}
        public static V3 HalfExtents(int source,float yaw,float margin=0)
        {
            var size=Dimensions(source);float a=yaw*(float)Math.PI/180,co=Math.Abs((float)Math.Cos(a)),si=Math.Abs((float)Math.Sin(a));
            return new V3((size.X*co+size.Z*si)*.5f+margin,size.Y*.5f,(size.Z*co+size.X*si)*.5f+margin);
        }
        // Conservative triangle bounds intentionally reject ambiguous candidates.
        // This runs on the pure geometry worker, never as a per-frame physics scan.
        public static bool Clear(MeshData mesh,SourceFurniturePlacement p,float margin=.04f)
        {
            var half=HalfExtents(p.Source,p.Yaw,margin);var center=p.Bottom+V3.Up*half.Y;
            foreach(var indices in mesh.Indices)for(int i=0;i<indices.Count;i+=3)
            {
                var a=mesh.Positions[indices[i]];var b=mesh.Positions[indices[i+1]];var c=mesh.Positions[indices[i+2]];
                if(Math.Max(a.X,Math.Max(b.X,c.X))<center.X-half.X||Math.Min(a.X,Math.Min(b.X,c.X))>center.X+half.X||
                   Math.Max(a.Y,Math.Max(b.Y,c.Y))<center.Y-half.Y||Math.Min(a.Y,Math.Min(b.Y,c.Y))>center.Y+half.Y||
                   Math.Max(a.Z,Math.Max(b.Z,c.Z))<center.Z-half.Z||Math.Min(a.Z,Math.Min(b.Z,c.Z))>center.Z+half.Z)continue;
                return false;
            }
            return true;
        }
        public static V3 Dimensions(int source)
        {
            switch(source)
            {
                case 0:return new V3(.11707850f,.28f,.11707854f);
                case 1:return new V3(.39384329f,.38790759f,.4f);
                case 2:return new V3(1,.83024678f,.68992799f);
                case 3:return new V3(2,.71972764f,.93475457f);
                case 4:return new V3(1,.82952308f,.69010345f);
                case 5:return new V3(2,.81979754f,.67736536f);
                case 6:return new V3(.5404695f,1.5f,.7063373f);
                default:throw new ArgumentOutOfRangeException(nameof(source));
            }
        }
    }
}
