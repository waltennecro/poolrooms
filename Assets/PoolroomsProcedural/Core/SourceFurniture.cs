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
        public const int Count=28;
        private static readonly float[] mallLengths={2.1f,.75f,.30f,.65f,.65f,.09f,.16f,.32f,1f,1.3f,.45f,.95f,.28f,1.25f};
        public static string Id(int source)
        {
            switch(source){case 0:return "WaterBottle";case 1:return "BoomBox";case 2:return "SheenChair";case 3:return "I_GlamVelvetSofa";case 4:return "I_ChairDamaskPurplegold";case 5:return "U_SheenWoodLeatherSofa";case 6:return "U_AntiqueCamera";case 7:return "B_CafeTable";case 8:return "B_CafeChair";case 9:return "G_PottedPlant";case 10:return "T_ArtNouveauLamp";case 11:return "R_Teacup";case 12:return "R_FlowerVase";case 13:return "R_BarnLamp";case 14:return "M_Refrigerator";case 15:return "M_Pouf";case 16:return "M_Shoe";case 17:return "M_StainedLamp";case 18:return "M_DeskLamp";case 19:return "M_Mandarin";case 20:return "M_Avocado";case 21:return "M_OliveDish";case 22:return "M_IndoorChair";case 23:return "M_IndoorTable";case 24:return "M_CashRegister";case 25:return "M_Stool";case 26:return "M_Napkins";case 27:return "M_MenuSign";default:throw new ArgumentOutOfRangeException(nameof(source));}
        }
        public static bool IsSeat(int source){return source>=2&&source<=5||source==8||source==15||source==22||source==25;}
        public static int Index(string source){for(int i=0;i<Count;i++)if(Id(i)==source)return i;throw new ArgumentException("Unknown placed source: "+source);}
        public static float Longest(int source){if(source>=14&&source<Count)return mallLengths[source-14];if(source>=Count||source<0)throw new ArgumentOutOfRangeException(nameof(source));return source==11?.19f:source==12?.42f:source==13?.65f:source==10?.55f:source==9?1.15f:source==0?.28f:source==1?.4f:source==3||source==5?2:source==6?1.5f:source==7?.9f:1;}
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
                case 7:return new V3(.89995969f,.84249021f,.9f);
                case 8:return new V3(.5709673f,1,.62584087f);
                case 9:return new V3(0.95857222f,1.15000000f,0.89873235f);
                case 10:return new V3(0.33261171f,0.55000000f,0.21296960f);
                case 11:return new V3(.18367949f,.08698847f,.19f);
                case 12:return new V3(.42f,.39283096f,.27544475f);
                case 13:return new V3(.48592149f,.65f,.57691791f);
                case 14:return new V3(0.821502568f,2.100000000f,0.907854268f);
                case 15:return new V3(0.749857739f,0.243148415f,0.750000000f);
                case 16:return new V3(0.300000000f,0.154191139f,0.116622521f);
                case 17:return new V3(0.415096209f,0.650000000f,0.415097432f);
                case 18:return new V3(0.405910173f,0.650000000f,0.412145218f);
                case 19:return new V3(0.081640425f,0.090000000f,0.061916644f);
                case 20:return new V3(0.108272595f,0.160000000f,0.070257159f);
                case 21:return new V3(0.319999941f,0.198891267f,0.320000000f);
                case 22:return new V3(0.524371458f,1.000000000f,0.519829992f);
                case 23:return new V3(1.300000000f,0.751304363f,0.703803934f);
                case 24:return new V3(0.450000000f,0.418905552f,0.348906060f);
                case 25:return new V3(0.390566826f,0.950000000f,0.374196594f);
                case 26:return new V3(0.280000000f,0.153677761f,0.086385908f);
                case 27:return new V3(0.763994234f,1.250000000f,0.124002989f);
                default:throw new ArgumentOutOfRangeException(nameof(source));
            }
        }
    }
}
