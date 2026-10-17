using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Batched geometric lettering works with the project's PBR/emissive materials.
    // No runtime font, incompatible text shader, brands or advertising characters.
    public static class MallGlyphs
    {
        private static readonly Dictionary<char,string> Font=new Dictionary<char,string>
        {
            {'A',"01110/10001/10001/11111/10001/10001/10001"},{'B',"11110/10001/10001/11110/10001/10001/11110"},
            {'C',"01111/10000/10000/10000/10000/10000/01111"},{'D',"11110/10001/10001/10001/10001/10001/11110"},
            {'E',"11111/10000/10000/11110/10000/10000/11111"},{'F',"11111/10000/10000/11110/10000/10000/10000"},
            {'G',"01111/10000/10000/10111/10001/10001/01111"},{'H',"10001/10001/10001/11111/10001/10001/10001"},
            {'I',"11111/00100/00100/00100/00100/00100/11111"},{'J',"00111/00010/00010/00010/10010/10010/01100"},
            {'K',"10001/10010/10100/11000/10100/10010/10001"},{'L',"10000/10000/10000/10000/10000/10000/11111"},
            {'M',"10001/11011/10101/10101/10001/10001/10001"},{'N',"10001/11001/10101/10011/10001/10001/10001"},
            {'O',"01110/10001/10001/10001/10001/10001/01110"},{'P',"11110/10001/10001/11110/10000/10000/10000"},
            {'Q',"01110/10001/10001/10001/10101/10010/01101"},{'R',"11110/10001/10001/11110/10100/10010/10001"},
            {'S',"01111/10000/10000/01110/00001/00001/11110"},{'T',"11111/00100/00100/00100/00100/00100/00100"},
            {'U',"10001/10001/10001/10001/10001/10001/01110"},{'V',"10001/10001/10001/10001/10001/01010/00100"},
            {'W',"10001/10001/10001/10101/10101/11011/10001"},{'X',"10001/10001/01010/00100/01010/10001/10001"},
            {'Y',"10001/10001/01010/00100/00100/00100/00100"},{'Z',"11111/00001/00010/00100/01000/10000/11111"},
            {'0',"01110/10001/10011/10101/11001/10001/01110"},{'1',"00100/01100/00100/00100/00100/00100/01110"},
            {'2',"01110/10001/00001/00010/00100/01000/11111"},{'3',"11110/00001/00001/01110/00001/00001/11110"},
            {'4',"00010/00110/01010/10010/11111/00010/00010"},{'5',"11111/10000/10000/11110/00001/00001/11110"},
            {'6',"01110/10000/10000/11110/10001/10001/01110"},{'7',"11111/00001/00010/00100/01000/01000/01000"},
            {'8',"01110/10001/10001/01110/10001/10001/01110"},{'9',"01110/10001/10001/01111/00001/00001/01110"}
        };
        public static void Write(MeshData mesh,string text,V3 origin,float pixel)
        {
            for(int i=0;i<text.Length;i++)
            {
                string glyph;if(!Font.TryGetValue(char.ToUpperInvariant(text[i]),out glyph))continue;
                string[] rows=glyph.Split('/');
                for(int row=0;row<7;row++)for(int col=0;col<5;)
                {
                    if(rows[row][col]=='0'){col++;continue;}int start=col;while(col<5&&rows[row][col]=='1')col++;
                    float x=origin.X+(i*6+start)*pixel,y=origin.Y+(6-row)*pixel,w=(col-start)*pixel;
                    mesh.Quad(new V3(x,y,origin.Z),new V3(x+w,y,origin.Z),new V3(x+w,y+pixel*.92f,origin.Z),new V3(x,y+pixel*.92f,origin.Z),new V3(0,0,-1),Surface.Glow,1);
                }
            }
        }
        public static MeshData SnackBag()
        {
            var m=new MeshData{RetailPalette=1,PaletteVariant=3};
            // Tapered, puffed packaging with crimps and a printed geometric label.
            var ring=new[]{new V3(-.13f,-.19f,-.018f),new V3(.13f,-.19f,-.018f),new V3(.16f,-.12f,-.065f),new V3(.16f,.12f,-.065f),new V3(.13f,.19f,-.018f),new V3(-.13f,.19f,-.018f),new V3(-.16f,.12f,-.065f),new V3(-.16f,-.12f,-.065f)};
            for(int i=0;i<ring.Length;i++)
            {
                int next=(i+1)%ring.Length;V3 a=ring[i],b=ring[next],c=new V3(b.X,b.Y,-b.Z),d=new V3(a.X,a.Y,-a.Z);
                m.Quad(a,b,c,d,new V3((a.X+b.X)*.5f,(a.Y+b.Y)*.5f,0).Unit,Surface.Accent,.15f);
                m.Triangle(new V3(0,0,-.075f),b,a,new V3(0,0,-1),new V3(0,0,-1),new V3(0,0,-1),new V2(),new V2(1,0),new V2(0,1),Surface.Accent);
                m.Triangle(new V3(0,0,.075f),d,c,new V3(0,0,1),new V3(0,0,1),new V3(0,0,1),new V2(),new V2(0,1),new V2(1,0),Surface.Accent);
            }
            foreach(float y in new[]{-.185f,.185f})for(int i=0;i<12;i++)MallArchitecture.Box(m,-.12f+i*.021f,0,.007f,.04f,y-.005f,y+.005f,Surface.Metal);
            MallGlyphs.Write(m,"PAPAS",new V3(-.116f,.03f,-.079f),.0078f);
            MallArchitecture.Box(m,0,-.078f,.2f,.008f,-.05f,-.018f,Surface.Plaster);
            for(int i=-1;i<=1;i++)MallArchitecture.Box(m,i*.05f,-.079f,.035f,.008f,-.13f,-.09f,Surface.Tile);
            return m;
        }
    }
}
