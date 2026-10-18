using System;

namespace Poolrooms.Core
{
    public sealed class AquaticFlumeMeshes
    {
        public readonly MeshData Solid=new MeshData(),Glass=new MeshData{Theme=SurfaceTheme.SlideGlass},Flow=new MeshData{Theme=SurfaceTheme.FlumeFlow};
    }
    public static class AquaticFlumeGeometry
    {
        public const float Shell=.075f,FilmDepth=.035f,FilmAngle=.42f;
        public static bool HasShell(AquaticSlideCover cover,int segment,float angle)
        {return Math.Cos(angle)>=0||cover!=AquaticSlideCover.Open&&(cover!=AquaticSlideCover.Segmented||segment/12%2==0);}
        static bool Glass(AquaticSlideCover cover,float angle)
        {return cover==AquaticSlideCover.GlassTube||cover==AquaticSlideCover.GlassCrown&&Math.Abs(angle)>Shapes.Pi*.72f;}
        public static V3 Right(AquaticSlidePath path,int i){var t=path.Tangent(i);return new V3(t.Z,0,-t.X).Unit;}
        public static V3 Up(AquaticSlidePath path,int i){return V3.Cross(path.Tangent(i),Right(path,i)).Unit;}
        public static V3 FilmPoint(AquaticSlidePath path,int i,float angle)
        {var up=Up(path,i);return path.Points[i]+up*path.Radius+(Right(path,i)*Shapes.Sin(angle)-up*Shapes.Cos(angle))*(path.Radius-FilmDepth);}
        public static AquaticFlumeMeshes Build(AquaticSlidePath path,int cross,Surface slot=Surface.Slide)
        {
            cross=Math.Max(16,Math.Min(32,cross));cross=(cross+3)/4*4;
            var o=new AquaticFlumeMeshes();o.Solid.Theme=SurfaceTheme.AquaticPark;o.Solid.AquaticColor=path.Color;o.Flow.FlowProfile=path.Family.SizeProfile;
            var cover=path.Family.Cover;if(cover==AquaticSlideCover.Open)cross*=2;
            for(int i=0;i<path.Points.Length-1;i++)
            {
                V3 t0=path.Tangent(i),t1=path.Tangent(i+1),right0=Right(path,i),right1=Right(path,i+1),up0=Up(path,i),up1=Up(path,i+1);
                V3 p=path.Points[i]+up0*path.Radius,q=path.Points[i+1]+up1*path.Radius;
                for(int j=0;j<cross;j++)
                {
                    float a=-Shapes.Pi+2*Shapes.Pi*j/cross,b=-Shapes.Pi+2*Shapes.Pi*(j+1)/cross,mid=(a+b)*.5f;
                    if(!HasShell(cover,i,mid))continue;MeshData m=Glass(cover,mid)?o.Glass:o.Solid;
                    V3 na=right0*Shapes.Sin(a)-up0*Shapes.Cos(a),nb=right0*Shapes.Sin(b)-up0*Shapes.Cos(b),nc=right1*Shapes.Sin(b)-up1*Shapes.Cos(b),nd=right1*Shapes.Sin(a)-up1*Shapes.Cos(a);
                    for(int outer=0;outer<2;outer++)
                    {
                        float radius=path.Radius+outer*Shell,sign=outer==0?-1:1;
                        m.QuadUV(p+na*radius,p+nb*radius,q+nc*radius,q+nd*radius,na*sign,nb*sign,nc*sign,nd*sign,new V2(a,path.Distances[i]),new V2(b,path.Distances[i]),new V2(b,path.Distances[i+1]),new V2(a,path.Distances[i+1]),slot);
                    }
                    if(i==0||!HasShell(cover,i-1,mid))m.Quad(p+na*path.Radius,p+nb*path.Radius,p+nb*(path.Radius+Shell),p+na*(path.Radius+Shell),t0*-1,slot);
                    if(i==path.Points.Length-2||!HasShell(cover,i+1,mid))m.Quad(q+nd*path.Radius,q+nc*path.Radius,q+nc*(path.Radius+Shell),q+nd*(path.Radius+Shell),t1,slot);
                    foreach(int edge in new[]{0,1})
                    {
                        float neighbor=edge==0?mid-2*Shapes.Pi/cross:mid+2*Shapes.Pi/cross;
                        if(HasShell(cover,i,neighbor))continue;
                        float angle=edge==0?a:b;V3 n=right0*Shapes.Sin(angle)-up0*Shapes.Cos(angle),nn=right1*Shapes.Sin(angle)-up1*Shapes.Cos(angle);
                        V3 normal=(right0*Shapes.Cos(angle)+up0*Shapes.Sin(angle))*(edge==0?-1:1);
                        m.Quad(p+n*path.Radius,p+n*(path.Radius+Shell),q+nn*(path.Radius+Shell),q+nn*path.Radius,normal.Unit,slot);
                    }
                }
                // Thin concave film follows the flume floor; never enters the flat pool-water mask.
                const int ribbons=8;
                for(int j=0;j<ribbons;j++)
                {
                    float a=-FilmAngle+2*FilmAngle*j/ribbons,b=-FilmAngle+2*FilmAngle*(j+1)/ribbons;
                    V3 na=(up0*Shapes.Cos(a)-right0*Shapes.Sin(a)).Unit,nb=(up0*Shapes.Cos(b)-right0*Shapes.Sin(b)).Unit,nc=(up1*Shapes.Cos(b)-right1*Shapes.Sin(b)).Unit,nd=(up1*Shapes.Cos(a)-right1*Shapes.Sin(a)).Unit;
                    o.Flow.QuadUV(FilmPoint(path,i,a),FilmPoint(path,i,b),FilmPoint(path,i+1,b),FilmPoint(path,i+1,a),na,nb,nc,nd,new V2(j/(float)ribbons,path.Distances[i]/AquaticSlideCatalog.FlowTileLength),new V2((j+1)/(float)ribbons,path.Distances[i]/AquaticSlideCatalog.FlowTileLength),new V2((j+1)/(float)ribbons,path.Distances[i+1]/AquaticSlideCatalog.FlowTileLength),new V2(j/(float)ribbons,path.Distances[i+1]/AquaticSlideCatalog.FlowTileLength),Surface.Slide);
                }
            }
            return o;
        }
    }
}
