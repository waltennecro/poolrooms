using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Local meshes use the same footprint as the packing plan. No extra light, rigidbody or renderer.
    public static class GeneratedFurnishing
    {
        private static void B(MeshData m,float x,float z,float w,float d,float low,float high,Surface s,float uv)
        {ZoneShapes.Box(m,x,z,w,d,low,high,s,uv);}
        public static bool Build(MeshData solid,MeshData detail,GeneratedAssembly a,int quality,float uv,bool props)
        {
            if(a.Kind<AssemblyKind.TowelRack)return false;
            float w=a.Width,d=a.Depth;bool fine=quality>12;
            var r=new StableRandom(a.Seed^0x25001u);Surface finish=r.Range(2)==0?Surface.Accent:Surface.Slide;
            Action<MeshData,float,float,float,float,float,float,Surface> b=(m,x,z,bw,bd,lo,hi,s)=>B(m,x,z,bw,bd,lo,hi,s,uv);
            Action<float> shelf=y=>b(solid,0,0,w-.1f,d-.06f,y-.055f,y,Surface.Plaster);
            Action<float> carcass=h=>
            {
                b(solid,0,d*.45f,w,d*.1f,0,h,finish);
                foreach(int side in new[]{-1,1})b(solid,side*(w*.5f-.045f),0,.09f,d,0,h,finish);
            };
            switch(a.Kind)
            {
                case AssemblyKind.TowelRack:
                    carcass(1.65f);shelf(.42f);shelf(.95f);shelf(1.48f);
                    if(props)for(int tier=0;tier<3;tier++)for(int i=0;i<2;i++)
                    {float x=(i-.5f)*w*.43f,y=.42f+tier*.53f;b(detail,x,0,w*.32f,d*.68f,y,y+.08f,Surface.Slide);b(detail,x,.04f,w*.3f,d*.58f,y+.08f,y+.13f,Surface.Plaster);}
                    break;
                case AssemblyKind.ShowerStand:
                    b(solid,0,0,w,d,0,.035f,Surface.Tile);
                    foreach(int side in new[]{-1,1})b(solid,side*(w*.5f-.045f),d*.35f,.09f,.09f,.035f,2.1f,Surface.Metal);
                    b(solid,0,d*.38f,w,.08f,.75f,1.8f,Surface.Plaster);
                    foreach(int side in new[]{-1,1})
                    {float x=side*w*.25f;b(detail,x,d*.28f,.055f,.08f,.9f,1.92f,Surface.Metal);b(detail,x,d*.14f,.18f,d*.28f,1.9f,1.94f,Surface.Metal);b(detail,x,d*.19f,.16f,.08f,1.05f,1.1f,Surface.Metal);}
                    b(detail,0,-d*.25f,.22f,.22f,.036f,.043f,Surface.Metal);break;
                case AssemblyKind.DrinkingStation:
                    b(solid,0,0,w*.82f,d*.82f,0,.78f,finish);b(solid,0,0,w,d,.78f,.84f,Surface.Metal);
                    b(detail,0,0,w*.55f,d*.55f,.841f,.85f,Surface.Plaster);
                    b(detail,0,d*.2f,.06f,.06f,.84f,1.02f,Surface.Metal);b(detail,0,d*.1f,.06f,d*.2f,.985f,1.025f,Surface.Metal);
                    if(fine)for(int slat=0;slat<4;slat++)b(detail,0,-d*.411f,w*.53f,.018f,.25f+slat*.09f,.275f+slat*.09f,Surface.Metal);break;
                case AssemblyKind.EquipmentCart:
                    foreach(float y in new[]{.22f,.72f,1.08f})shelf(y);
                    foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                    {b(solid,sx*(w*.5f-.07f),sz*(d*.5f-.07f),.065f,.065f,.06f,1.14f,Surface.Metal);b(detail,sx*(w*.5f-.07f),sz*(d*.5f-.07f),.13f,.1f,0,.11f,Surface.Metal);}
                    b(solid,0,d*.4f,w-.08f,.06f,1.14f,1.2f,Surface.Metal);
                    if(props){b(detail,-w*.23f,0,w*.3f,d*.64f,.72f,.95f,finish);b(detail,w*.23f,0,w*.3f,d*.64f,.22f,.5f,Surface.Plaster);}break;
                case AssemblyKind.Bookcase:case AssemblyKind.Showcase:
                    carcass(1.95f);foreach(float y in new[]{.4f,.9f,1.4f})shelf(y);shelf(1.95f);
                    if(a.Kind==AssemblyKind.Showcase)b(detail,0,-d*.46f,w-.15f,.025f,1.74f,1.86f,Surface.Metal);
                    if(props)for(int tier=0;tier<3;tier++)for(int i=0;i<(fine?5:3);i++)
                    {float x=-w*.34f+i*w*.68f/(fine?4:2),lo=.4f+tier*.5f;b(detail,x,0,.09f,d*.48f,lo,lo+r.Range(.19f,.31f),i%2==0?finish:Surface.Plaster);}break;
                case AssemblyKind.PrinterStation:
                    b(solid,0,0,w,d,0,.73f,finish);b(solid,0,0,w,d,.73f,.8f,Surface.Plaster);
                    b(solid,0,d*.06f,w*.66f,d*.7f,.8f,1.16f,Surface.Metal);b(detail,0,d*.06f,w*.63f,d*.65f,1.16f,1.2f,Surface.Plaster);
                    b(detail,0,-d*.37f,w*.42f,.025f,.95f,1.08f,Surface.Slide);b(detail,0,-d*.24f,w*.47f,.25f,.84f,.87f,Surface.Plaster);
                    b(detail,w*.18f,-d*.31f,.12f,.13f,1.09f,1.11f,Surface.Slide);break;
                case AssemblyKind.FilingCabinet:
                    b(solid,0,0,w,d,0,1.55f,Surface.Metal);
                    for(int row=0;row<4;row++)foreach(int side in new[]{-1,1})
                    {float x=side*w*.25f,y=.07f+row*.37f;b(detail,x,-d*.49f,w*.45f,.018f,y,y+.32f,Surface.Plaster);b(detail,x,-d*.475f,w*.12f,.04f,y+.21f,y+.24f,Surface.Metal);b(detail,x,-d*.501f,w*.15f,.009f,y+.09f,y+.14f,Surface.Accent);}break;
                case AssemblyKind.LuggageBench:case AssemblyKind.ShoeBench:
                    b(solid,0,0,w,d*.9f,.43f,.52f,finish);
                    foreach(int side in new[]{-1,1})b(solid,side*w*.41f,0,.08f,d*.86f,0,.43f,Surface.Metal);
                    if(a.Kind==AssemblyKind.ShoeBench)
                    {shelf(.18f);if(props)for(int i=0;i<3;i++)b(detail,(i-1)*w*.26f,0,.19f,d*.57f,.18f,.32f,Surface.Plaster);}
                    else for(int strip=0;strip<4;strip++)b(detail,0,(strip-1.5f)*d*.2f,w*.88f,.025f,.521f,.53f,Surface.Metal);break;
                case AssemblyKind.Vanity:
                    b(solid,0,d*.23f,w,d*.5f,.73f,.8f,finish);
                    foreach(int side in new[]{-1,1})b(solid,side*w*.4f,d*.23f,.08f,d*.42f,0,.73f,Surface.Metal);
                    b(solid,0,-d*.29f,.5f,.5f,0,.46f,Surface.Slide);
                    b(solid,0,d*.46f,w*.7f,.06f,.8f,1.74f,Surface.Metal);b(detail,0,d*.435f,w*.63f,.015f,.89f,1.65f,Surface.Plaster);break;
                case AssemblyKind.VendingMachine:
                    b(solid,0,0,w,d,0,1.95f,finish);
                    b(detail,-w*.13f,-d*.493f,w*.64f,.024f,.58f,1.73f,Surface.Metal);
                    for(int row=0;row<3;row++)for(int col=0;col<3;col++)b(detail,-w*.35f+col*w*.22f,-d*.47f,w*.14f,.09f,.7f+row*.31f,.9f+row*.31f,(row+col)%2==0?Surface.Plaster:Surface.Slide);
                    b(detail,w*.35f,-d*.493f,w*.15f,.023f,1.14f,1.58f,Surface.Slide);b(detail,0,-d*.495f,w*.6f,.023f,.2f,.4f,Surface.Metal);break;
                case AssemblyKind.NoticeStand:
                    foreach(int side in new[]{-1,1}){b(solid,side*w*.4f,0,.08f,d,0,.07f,Surface.Metal);b(solid,side*w*.4f,0,.065f,.065f,.07f,1.9f,Surface.Metal);}
                    b(solid,0,0,w,.085f,.83f,1.88f,finish);
                    for(int i=0;i<3;i++)b(detail,(i-1)*w*.27f,-.045f,w*.22f,.01f,1.1f,1.63f,Surface.Plaster);break;
                case AssemblyKind.Sideboard:
                    b(solid,0,0,w,d,0,.84f,finish);shelf(.9f);b(solid,0,d*.45f,w,.08f,.9f,1.35f,Surface.Plaster);
                    foreach(int side in new[]{-1,1})b(detail,side*w*.25f,-d*.48f,w*.18f,.04f,.65f,.68f,Surface.Metal);
                    if(props){b(detail,-w*.23f,0,.23f,.23f,.9f,.95f,Surface.Plaster);b(detail,w*.22f,0,.17f,.17f,.9f,1.16f,Surface.Metal);}break;
                case AssemblyKind.Checkout:
                    b(solid,0,0,w,d,0,.86f,finish);shelf(.92f);
                    b(solid,-w*.18f,0,w*.51f,d*.71f,.92f,.935f,Surface.Metal);
                    b(solid,w*.34f,d*.13f,w*.2f,d*.44f,.92f,1.02f,Surface.Metal);
                    b(detail,w*.34f,d*.2f,w*.2f,.055f,1.02f,1.3f,Surface.Slide);b(detail,w*.34f,0,w*.17f,.18f,1.022f,1.035f,Surface.Plaster);break;
                case AssemblyKind.PlayTable:
                    b(solid,0,0,w*.72f,d*.44f,.49f,.56f,finish);
                    foreach(int sx in new[]{-1,1})b(solid,sx*w*.25f,0,.08f,d*.4f,0,.49f,Surface.Metal);
                    foreach(int sz in new[]{-1,1}){b(solid,0,sz*d*.37f,w*.82f,d*.17f,.25f,.31f,Surface.Slide);foreach(int sx in new[]{-1,1})b(solid,sx*w*.3f,sz*d*.37f,.07f,d*.15f,0,.25f,Surface.Metal);}
                    if(props)for(int i=0;i<3;i++)b(detail,(i-1)*w*.2f,0,.15f,.15f,.56f,.67f,i%2==0?Surface.Plaster:Surface.Accent);break;
            }
            return true;
        }
        public static void Dress(MeshData detail,GeneratedAssembly a,int quality,float uv)
        {
            // A separate stream keeps supports and solid geometry unchanged by embellishment.
            var r=new StableRandom(a.Seed^0x25002u);float w=a.Width,d=a.Depth;
            if(a.Kind==AssemblyKind.Bench||a.Kind==AssemblyKind.SeatGroup)
            {
                int bands=quality>12?5:2;
                for(int i=0;i<bands;i++)B(detail,(i-(bands-1)*.5f)*w/(bands+1),d*.29f,.015f,d*.015f,.62f,.89f,Surface.Metal,uv);
            }
            if(a.Kind==AssemblyKind.Bed&&r.Range(2)==0)B(detail,0,-d*.35f,w*.95f,d*.19f,.59f,.615f,Surface.Accent,uv);
            if(a.Kind==AssemblyKind.Desk){B(detail,-w*.29f,d*.1f,.2f,.27f,.79f,.817f,Surface.Plaster,uv);if(quality>12)B(detail,-w*.2f,d*.1f,.018f,.16f,.817f,.828f,Surface.Metal,uv);}
            if(a.Kind==AssemblyKind.DisplayRack||a.Kind==AssemblyKind.Cubbies)
            {
                var same=new StableRandom(a.Seed);same.Range(2);float h=same.Range(1.5f,1.95f);
                float top=a.Kind==AssemblyKind.DisplayRack?.4f:h/((2+same.Range(2))+1);
                int count=Math.Max(1,Math.Min(5,(int)(w/.65f)));
                float x=-w*.5f+w/count*.5f;
                B(detail,x,0,Math.Min(.3f,w/count*.65f),d*.53f,top,top+.12f,Surface.Slide,uv);
            }
        }
        // Product supports are authored with the same dimensions as Build above.
        public static IEnumerable<V3> StockSupports(GeneratedAssembly a)
        {
            bool table=a.Kind==AssemblyKind.Table,shelf=a.Kind==AssemblyKind.DisplayRack||a.Kind==AssemblyKind.Bookcase||a.Kind==AssemblyKind.Showcase;
            if(!table&&!shelf&&a.Kind!=AssemblyKind.Sideboard&&a.Kind!=AssemblyKind.Checkout&&a.Kind!=AssemblyKind.PlayTable)return Array.Empty<V3>();
            var points=new List<V3>();float width=table?a.Width-1.4f:a.Kind==AssemblyKind.PlayTable?a.Width*.72f:a.Kind==AssemblyKind.Checkout?a.Width*.51f:a.Width-.3f;
            int columns=Math.Max(1,(int)(width/.6f)),tiers=shelf?3:1;
            float centre=a.Kind==AssemblyKind.Checkout?-a.Width*.18f:0;
            for(int tier=0;tier<tiers;tier++)for(int col=0;col<columns;col++)
            {
                float top=shelf?.4f+tier*.5f:table?.79f:a.Kind==AssemblyKind.Sideboard?.9f:a.Kind==AssemblyKind.Checkout?.935f:.56f;
                float z=shelf?-a.Depth*.5f+.19f:0;
                points.Add(a.Point(centre+(col-(columns-1)*.5f)*.6f,top,z));
            }
            return points;
        }
    }
}
