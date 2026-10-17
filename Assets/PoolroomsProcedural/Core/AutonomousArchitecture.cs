using System;

namespace Poolrooms.Core
{
    public static class AutonomousArchitecture
    {
        public static void Append(RoomGeometry output,Room room,LayoutConfig c)
        {
            if(!c.AutonomousGeneration||room.Plan==null)return;
            foreach(var a in room.Plan.Assemblies)Build(output.Structure,output.Decoration,a,c.CurveSegments,c.TextureWorldSize);
        }
        public static void Interior(MeshData physical,MeshData detail,InteriorBay bay,LiminalEnvironment environment,LayoutConfig c)
        {foreach(var a in AutonomousPlanner.Interior(bay,environment))Build(physical,detail,a,c.CurveSegments,c.TextureWorldSize);}
        public static void Build(MeshData physical,MeshData decoration,GeneratedAssembly a,int quality,float uv=1.2f,bool furnishedProps=true)
        {
            var solid=new MeshData();var detail=new MeshData();var random=new StableRandom(a.Seed);
            float w=a.Width,d=a.Depth;
            Action<MeshData,float,float,float,float,float,float,Surface> box=(m,x,z,bw,bd,y,top,s)=>ZoneShapes.Box(m,x,z,bw,bd,y,top,s,uv);
            Surface finish=random.Range(2)==0?Surface.Accent:Surface.Slide;
            int count=Math.Max(1,Math.Min(5,(int)(w/.65f)));float height=random.Range(1.5f,1.95f);
            if(!GeneratedFurnishing.Build(solid,detail,a,quality,uv,furnishedProps))switch(a.Kind)
            {
                case AssemblyKind.Bench:case AssemblyKind.SeatGroup:
                    box(solid,0,0,w,d*.72f,.4f,.52f,finish);
                    box(solid,0,d*.36f,w,d*.12f,.52f,.98f,finish);
                    foreach(int side in new[]{-1,1})box(solid,side*w*.38f,0,.1f,d*.65f,0,.4f,Surface.Metal);
                    for(int seat=0;seat<count;seat++)
                    {
                        float x=-w*.5f+(seat+.5f)*w/count;
                        box(detail,x,-d*.05f,w/count*.88f,d*.56f,.52f,.55f,Surface.Plaster);
                        if(a.Kind==AssemblyKind.SeatGroup)box(solid,x+w/count*.43f,0,.055f,d*.6f,.52f,.72f,Surface.Metal);
                    }
                    break;
                case AssemblyKind.Desk:
                    var desk=new MeshData();Table(desk,w,d*.5f,.79f,finish,uv);desk.TranslateRotate(new V3(0,0,d*.22f),0);solid.Append(desk);
                    Chair(solid,0,-d*.3f,0,finish,uv);
                    box(detail,w*.05f,d*.32f,Math.Min(.55f,w*.5f),.08f,.79f,1.17f,Surface.Metal);
                    box(detail,w*.05f,d*.13f,.4f,.15f,.79f,.815f,Surface.Plaster);
                    box(detail,-w*.33f,d*.35f,.16f,.2f,.79f,.86f,finish);
                    break;
                case AssemblyKind.Table:
                    Table(solid,w-1.4f,d-1.4f,.79f,finish,uv);
                    foreach(int side in new[]{-1,1})
                    {
                        Chair(solid,side*(w*.5f-.31f),0,side<0?1:3,finish,uv);
                        Chair(solid,0,side*(d*.5f-.31f),side<0?0:2,finish,uv);
                    }
                    if(quality>12&&furnishedProps)
                    {
                        box(detail,-w*.15f,0,.18f,.18f,.79f,.805f,Surface.Plaster);
                        box(detail,w*.12f,d*.1f,.09f,.09f,.79f,.93f,Surface.Plaster);
                    }
                    break;
                case AssemblyKind.Bed:
                    box(solid,0,0,w,d,0,.38f,finish);box(solid,0,0,w*.98f,d*.98f,.38f,.57f,Surface.Plaster);
                    box(solid,0,d*.46f,w,.12f,.57f,1.08f,finish);
                    box(detail,0,-d*.15f,w*.96f,d*.62f,.57f,.59f,Surface.Slide);
                    foreach(int side in new[]{-1,1})box(detail,side*w*.25f,d*.3f,w*.38f,.38f,.57f,.7f,Surface.Plaster);
                    break;
                case AssemblyKind.SoftBlocks:
                    int blocks=2+random.Range(3);
                    for(int i=0;i<blocks;i++)
                    {
                        float bw=w/blocks*.86f,h=random.Range(.25f,.65f),x=-w*.5f+(i+.5f)*w/blocks;
                        box(solid,x,0,bw,d*.75f,0,h,finish);
                        box(detail,x,0,bw*.9f,d*.68f,h,h+.015f,Surface.Plaster);
                    }
                    break;
                case AssemblyKind.ClothesRail:
                    foreach(int side in new[]{-1,1})
                    {box(solid,side*(w*.5f-.07f),0,.08f,.08f,.08f,1.85f,Surface.Metal);box(solid,side*(w*.5f-.07f),0,.16f,d,0,.08f,Surface.Metal);}
                    box(solid,0,0,w,.065f,1.78f,1.85f,Surface.Metal);
                    for(int i=0;i<Math.Min(7,(int)(w/.3f));i++)
                    {
                        float x=-w*.4f+i*.3f;
                        box(detail,x,0,.06f,.4f,1.7f,1.75f,Surface.Metal);
                        box(detail,x,0,.07f,.48f,.95f,1.68f,i%2==0?finish:Surface.Plaster);
                    }
                    break;
                case AssemblyKind.ArcadeCabinet:
                    box(solid,0,0,w,d,0,1.75f,finish);
                    box(detail,0,-d*.501f,w*.75f,.016f,1,1.58f,Surface.Slide);
                    box(detail,0,-d*.501f,w*.65f,.016f,.78f,.87f,Surface.Metal);
                    for(int key=0;key<3;key++)box(detail,(key-1)*.15f,-d*.516f,.06f,.035f,.8f,.84f,Surface.Plaster);
                    break;
                default:
                    bool shelf=a.Kind==AssemblyKind.DisplayRack||a.Kind==AssemblyKind.Cubbies;
                    box(solid,0,d*.46f,w,d*.08f,0,height,finish);
                    foreach(int side in new[]{-1,1})box(solid,side*(w*.5f-.04f),0,.08f,d,0,height,finish);
                    if(shelf)
                    {
                        int tiers=a.Kind==AssemblyKind.DisplayRack?3:2+random.Range(2);
                        for(int tier=0;tier<tiers;tier++)
                        {float y=a.Kind==AssemblyKind.DisplayRack?.4f+tier*.5f:(tier+1)*height/(tiers+1);box(solid,0,0,w-.08f,d,y-.055f,y,Surface.Plaster);}
                        if(a.Kind==AssemblyKind.Cubbies)for(int i=1;i<count;i++)box(solid,-w*.5f+i*w/count,0,.045f,d,0,height,finish);
                    }
                    else
                    {
                        box(solid,0,0,w,d,0,height,finish);
                        for(int i=0;i<count;i++)
                        {
                            float x=-w*.5f+(i+.5f)*w/count;
                            box(detail,x,-d*.505f,w/count*.93f,.014f,.1f,height-.08f,Surface.Plaster);
                            box(detail,x+w/count*.25f,-d*.53f,.035f,.035f,height*.48f,height*.58f,Surface.Metal);
                            if(a.Kind==AssemblyKind.Locker&&quality>12)for(int slot=0;slot<3;slot++)box(detail,x,-d*.518f,w/count*.55f,.009f,height*.8f+slot*.045f,height*.815f+slot*.045f,Surface.Metal);
                        }
                    }
                    break;
            }
            if(furnishedProps)GeneratedFurnishing.Dress(detail,a,quality,uv);
            // All collision is stable across quality settings. Only tiny decorations differ.
            V3 offset=new V3(a.Bounds.X,0,a.Bounds.Z);float angle=a.Turns*Shapes.Pi*.5f;
            solid.TranslateRotate(offset,angle);detail.TranslateRotate(offset,angle);physical.Append(solid);decoration.Append(detail);
        }
        private static void Chair(MeshData m,float x,float z,int turns,Surface finish,float uv)
        {
            var part=new MeshData();ZoneShapes.Box(part,0,0,.54f,.54f,.4f,.47f,finish,uv);
            ZoneShapes.Box(part,0,-.24f,.54f,.065f,.47f,.95f,finish,uv);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})ZoneShapes.Box(part,sx*.2f,sz*.2f,.045f,.045f,0,.4f,Surface.Metal,uv);
            part.TranslateRotate(new V3(x,0,z),turns*Shapes.Pi*.5f);m.Append(part);
        }
        private static void Table(MeshData m,float w,float d,float top,Surface finish,float uv)
        {
            ZoneShapes.Box(m,0,0,w,d,top-.07f,top,finish,uv);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})ZoneShapes.Box(m,sx*w*.4f,sz*d*.38f,.075f,.075f,0,top-.07f,Surface.Metal,uv);
        }
    }
}
