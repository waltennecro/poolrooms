using System;

namespace Poolrooms.Core
{
    // Catalogue IDs retain their original geometry. Generated plans also carry their
    // own address seed and grown footprint; an ID alone no longer identifies those plants.
    public sealed class TerrainHouseBlueprint
    {
        public const int FamilyCount=20, ProfilesPerFamily=40, Count=FamilyCount*ProfilesPerFamily;
        public const float Module=7;
        private static readonly string[] Shapes={
            "111/111/111", "111/110/110", "111/011/011", "111/010/111",
            "111/101/111", "111/100/111", "111/001/111", "010/111/010",
            "110/011/110", "011/110/011", "1111/0110/0110", "1111/1001/1111",
            "0110/1111/0110", "0110/0111/1100/1100", "0110/1110/0011/0011",
            "1110/0100/0111/0010", "0111/0101/1101/1111", "1100/1111/0011/0011",
            "0110/1111/1001/1111", "0111/1101/0111/1100"};
        private static readonly string[] Names={"Compacta", "Ala izquierda", "Ala derecha", "Casa puente",
            "Patio interior", "Galería oeste", "Galería este", "Casa cruz", "Zigzag oeste", "Zigzag este",
            "Frente ancho", "Claustro", "Pabellón", "Alas escalonadas", "Casa desfasada",
            "Pasillo ramificado", "Patios dobles", "Casa extendida", "Casa laberinto", "Casa desmedida"};
        public readonly int Id,Family,Floors,Basements,Layout,Columns,Rows,RoomCount,FarX,FarZ;
        public readonly bool Attic;
        public readonly bool Generated;
        public readonly uint PlanSeed;
        private readonly float roofRise;
        private readonly string[] rows;
        private TerrainHouseBlueprint(int id)
        {
            Id=id;Family=id/ProfilesPerFamily;int profile=id%ProfilesPerFamily;
            Floors=1+profile%5;Basements=profile/5%2;Attic=profile/10%2==1;Layout=profile/20;
            rows=Shapes[Family].Split('/');Rows=rows.Length;Columns=rows[0].Length;
            int count=0,furthest=-1;
            for(int z=0;z<Rows;z++)for(int x=0;x<Columns;x++)if(Has(x,z))
            {count++;int distance=Math.Abs(x-1)+z;if(distance>furthest){furthest=distance;FarX=x;FarZ=z;}}
            RoomCount=count;
            roofRise=Layout==0?2.3f:3.3f;
        }
        // Keep catalogue profiles as a compatibility reference; exploration grows a new
        // connected footprint at each address. Plans belong to their district, never a global cache.
        private TerrainHouseBlueprint(int id,uint seed):this(id)
        {
            Generated=true;PlanSeed=seed;var random=new StableRandom(seed);
            Columns=3+random.Range(2);Rows=3+random.Range(2);
            var cells=new bool[Columns*Rows];cells[1]=true;
            int target=6+random.Range(Math.Min(12,cells.Length)-5),count=1;
            int[] frontier=new int[cells.Length];
            while(count<target)
            {
                int n=0;
                for(int z=0;z<Rows;z++)for(int x=0;x<Columns;x++)
                    if(!cells[z*Columns+x]&&((x>0&&cells[z*Columns+x-1])||(x+1<Columns&&cells[z*Columns+x+1])||
                        (z>0&&cells[(z-1)*Columns+x])||(z+1<Rows&&cells[(z+1)*Columns+x])))frontier[n++]=z*Columns+x;
                cells[frontier[random.Range(n)]]=true;count++;
            }
            rows=new string[Rows];int furthest=-1;FarX=1;FarZ=0;
            for(int z=0;z<Rows;z++)
            {
                var row=new char[Columns];
                for(int x=0;x<Columns;x++)
                {
                    row[x]=cells[z*Columns+x]?'1':'0';
                    if(cells[z*Columns+x]&&Math.Abs(x-1)+z>furthest){furthest=Math.Abs(x-1)+z;FarX=x;FarZ=z;}
                }
                rows[z]=new string(row);
            }
            RoomCount=count;roofRise=random.Range(2.3f,3.3f);
        }
        public static TerrainHouseBlueprint Generate(int seed,int x,int z)
        {return new TerrainHouseBlueprint(Select(seed,x,z),StableRandom.Hash(seed,x,z,0x9437Au));}
        // Every shared edge retains a passage. Independent per-floor choices merge rooms
        // into halls or use wider portals without moving the protected stair landing.
        public bool Merge(int level,int x,int z,int side)
        {
            int nx=x+(side==1?1:0),nz=z+(side==2?1:0);
            if(x==1&&z==0||nx==1&&nz==0)return false;
            return Generated?StableRandom.Hash(unchecked((int)PlanSeed),level,x+z*4+side*16,0x9437Bu)%100<28:
                Family>=12&&(x+z+level+Layout)%3==0;
        }
        public float PortalHalfWidth(int level,int x,int z,int side)
        {return Generated?1.2f+(StableRandom.Hash(unchecked((int)PlanSeed),level,x+z*4+side*16,0x9437Cu)%3)*.4f:1.2f;}
        private static readonly TerrainHouseBlueprint[] All=CreateAll();
        private static TerrainHouseBlueprint[] CreateAll()
        {var all=new TerrainHouseBlueprint[Count];for(int i=0;i<all.Length;i++)all[i]=new TerrainHouseBlueprint(i);return all;}
        public static TerrainHouseBlueprint Get(int id)
        {if(id<0||id>=Count)throw new ArgumentOutOfRangeException(nameof(id));return All[id];}
        public static int Select(int seed,int x,int z)
        {
            var random=new StableRandom(StableRandom.Hash(seed,x,z,0x9435Au));int family=random.Range(FamilyCount),roll=random.Range(100);
            int floors=roll<40?0:roll<70?1:roll<85?2:roll<95?3:4;
            return family*ProfilesPerFamily+floors+(random.Range(100)<30?5:0)+(random.Range(100)<45?10:0)+random.Range(2)*20;
        }
        public string Name {get{return Generated?"Casa generada por dirección":Names[Family];}}
        public float Width {get{return Columns*Module;}}
        public float Depth {get{return Rows*Module;}}
        public int Lowest {get{return -Basements;}}
        public int Highest {get{return Floors+(Attic?1:0)-1;}}
        public float RoofEave {get{return Attic?Floors*TerrainHousePlan.FloorHeight+2.5f:Floors*TerrainHousePlan.FloorHeight;}}
        public float RoofRise {get{return roofRise;}}
        public bool Has(int x,int z){return x>=0&&x<Columns&&z>=0&&z<Rows&&rows[z][x]=='1';}
        public float X(int cell){return (cell+.5f)*Module-Width*.5f;}
        public float Z(int cell){return (cell+.5f)*Module-Depth*.5f;}
        // A 7 m U-flight cell is present at the entrance in every footprint.
        public float StairX {get{return X(1);}}
        public float StairZ {get{return Z(0);}}
        public PlanRect StairHole {get{return new PlanRect(StairX-2,StairZ-1.4f,StairX+2,StairZ+2.5f);}}
        public bool Contains(float x,float y,float z)
        {
            if(y<Lowest*TerrainHousePlan.FloorHeight-.2f)return false;
            int cx=(int)Math.Floor((x+Width*.5f)/Module),cz=(int)Math.Floor((z+Depth*.5f)/Module);
            if(!Has(cx,cz))return false;
            float top=Attic?Floors*TerrainHousePlan.FloorHeight+2.5f+RoofRise*(1-Math.Abs(x-X(cx))/3.54f)-.12f:Floors*TerrainHousePlan.FloorHeight-.08f;
            return y<top;
        }
    }
}
