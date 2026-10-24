using System;

namespace Poolrooms.Core
{
    public sealed class Level10CropPatch
    {
        public readonly MeshData[] Meshes=new MeshData[3];
        public Level10CropPatch(){for(int i=0;i<3;i++)Meshes[i]=new MeshData{Theme=SurfaceTheme.Level10Grain};}
    }
    public static class Level10Crops
    {
        public const int PatchAxis=4,PatchCount=16;
        public static Level10CropPatch[] Build(Level10Plan plan)
        {
            var patches=new Level10CropPatch[PatchCount];for(int i=0;i<patches.Length;i++)patches[i]=new Level10CropPatch();
            foreach(var root in plan.Stalks)
            {
                int x=Math.Min(3,Math.Max(0,(int)((root.Foot.X+plan.Half)/plan.Cell*4)));
                int z=Math.Min(3,Math.Max(0,(int)((root.Foot.Z+plan.Half)/plan.Cell*4)));
                var patch=patches[z*4+x];
                for(int lod=0;lod<3;lod++)for(int tiller=0;tiller<(lod==2?2:4);tiller++)
                {
                    float a=root.Yaw*Shapes.Pi/180+tiller*2.4f;
                    V3 foot=root.Foot+new V3((float)Math.Cos(a)*.11f,0,(float)Math.Sin(a)*.11f);
                    foot.Y=Level10Terrain.MeshHeight(plan,foot.X,foot.Z);
                    float height=root.Height*(1-tiller*.075f);
                    Stalk(patch.Meshes[lod],foot,height,a,root.Lean,lod==0&&tiller==0?0:lod==2?2:1);
                }
            }
            return patches;
        }
        public static void Stalk(MeshData mesh,V3 foot,float height,float yaw,float lean,int detail)
        {
            V3 tip=foot+new V3((float)Math.Cos(yaw)*lean,height*.84f,(float)Math.Sin(yaw)*lean);
            Stem(mesh,foot,tip,detail==0?.009f:.012f,detail==0?5:3,Surface.Accent);
            V3 ear=tip+V3.Up*(height*.075f);
            if(detail==0)
            {
                for(int row=0;row<5;row++)for(int sign=-1;sign<=1;sign+=2)
                {
                    float a=yaw+sign*1.1f;V3 lateral=new V3((float)Math.Cos(a),0,(float)Math.Sin(a));
                    V3 center=tip+V3.Up*(.018f+row*.031f)+lateral*(.020f+row*.0005f)*sign;
                    Grain(mesh,center,.018f,.025f,.012f,Surface.Tile);
                    V3 point=center+lateral*sign*.025f+V3.Up*.055f;
                    V3 side=new V3(-lateral.Z*.0018f,0,lateral.X*.0018f);
                    mesh.Triangle(center-side,center+side,point,V3.Up,V3.Up,V3.Up,new V2(),new V2(1,0),new V2(.5f,1),Surface.Accent);
                }
            }
            else Grain(mesh,ear,.037f,height*.09f,.025f,Surface.Tile);
            if(detail<2)for(int leaf=0;leaf<2;leaf++)
            {
                float a=yaw+leaf*Shapes.Pi;V3 center=foot+(tip-foot)*(.40f+leaf*.19f);
                V3 end=center+new V3((float)Math.Cos(a)*.32f,.04f,(float)Math.Sin(a)*.32f);
                V3 wide=new V3(-(float)Math.Sin(a)*.027f,0,(float)Math.Cos(a)*.027f);
                mesh.Triangle(center,end-wide,end+wide,V3.Up,V3.Up,V3.Up,new V2(.5f,0),new V2(0,1),new V2(1,1),Surface.Plaster);
            }
        }
        public static void Stem(MeshData mesh,V3 a,V3 b,float radius,int sides,Surface surface)
        {
            V3 axis=(b-a).Unit,right=V3.Cross(axis,new V3(0,0,1)).Unit,forward=V3.Cross(axis,right).Unit;
            for(int i=0;i<sides;i++)
            {
                float aa=i*Shapes.Pi*2/sides,bb=(i+1)*Shapes.Pi*2/sides;
                V3 ra=right*(float)Math.Cos(aa)+forward*(float)Math.Sin(aa),rb=right*(float)Math.Cos(bb)+forward*(float)Math.Sin(bb);
                V3 p=a+ra*radius,q=a+rb*radius,r=b+rb*radius*.65f,s=b+ra*radius*.65f;
                mesh.QuadUV(p,q,r,s,ra,rb,rb,ra,new V2(0,0),new V2(1,0),new V2(1,1),new V2(0,1),surface);
            }
        }
        public static void Grain(MeshData mesh,V3 c,float x,float y,float z,Surface surface)
        {
            var ring=new[]{c+new V3(x,0,0),c+new V3(0,0,z),c+new V3(-x,0,0),c+new V3(0,0,-z)};
            for(int i=0;i<4;i++)
            {
                int j=(i+1)%4;V3 a=ring[i],b=ring[j],top=c+V3.Up*y,bottom=c-V3.Up*y;
                V3 normal=new V3((a.X-c.X)/x,.15f,(a.Z-c.Z)/z).Unit;
                V3 next=new V3((b.X-c.X)/x,.15f,(b.Z-c.Z)/z).Unit;
                mesh.Triangle(top,a,b,V3.Up,normal,next,new V2(.5f,1),new V2(0,.5f),new V2(1,.5f),surface);
                mesh.Triangle(bottom,b,a,V3.Up*-1,next,normal,new V2(.5f,0),new V2(1,.5f),new V2(0,.5f),surface);
            }
        }
    }
}
