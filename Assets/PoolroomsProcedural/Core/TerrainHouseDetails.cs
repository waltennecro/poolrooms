using System;

namespace Poolrooms.Core
{
    public static class TerrainHouseDetails
    {
        public static void Floor(TerrainHouseGeometry output,TerrainHousePlan house,int floor)
        {
            var g=output.Interior;float w=house.Width,d=house.Depth,y=floor*TerrainHousePlan.FloorHeight;
            var r=new StableRandom(StableRandom.Hash(house.Seed,floor,0,0x25095u));
            Action<float,float,float,float,float,float,Surface> b=(x,z,bw,bd,lo,hi,s)=>ZoneShapes.Box(g.Decoration,x,z,bw,bd,y+lo,y+hi,s,1.2f);
            // Floor-edge trim and sockets stay on the walls, outside all door openings.
            foreach(int side in new[]{-1,1})
            {
                b(side*(w*.5f-.14f),0,.025f,d-.4f,.015f,.105f,Surface.Accent);
                b(side*(w*.5f-.148f),-d*.1f,.022f,.09f,.29f,.41f,Surface.Metal);
                b(side*w*.29f,d*.5f-.15f,1.5f,.04f,.8f,.85f,Surface.Accent);
                // Narrow curtain folds flank rear windows. No curtain covers the front doorway.
                if(r.Range(2)==0)for(int fold=0;fold<3;fold++)
                    b(side*w*.29f+side*(.68f+fold*.035f),d*.5f-.16f,.045f,.065f,.95f,2.25f,Surface.Slide);
            }
            if(floor==0)
            {
                AddAssembly(g,AssemblyKind.Bookcase,new PlanRect(-w*.29f-.8f,-.84f,-w*.29f+.8f,-.14f),0,r.Next(),y);
                AddItem(output,house,13,MallItemShape.Book,new V3(-w*.29f,y+.4f,-.6f));
                AddAssembly(g,AssemblyKind.Sideboard,new PlanRect(-w*.27f-.55f,d*.5f-.87f,-w*.27f+.55f,d*.5f-.17f),0,r.Next(),y);
                AddItem(output,house,11,MallItemShape.FoodBox,new V3(-w*.27f-.25f,y+.9f,d*.5f-.52f));
                AddItem(output,house,12,MallItemShape.Mug,new V3(-w*.27f+.25f,y+.9f,d*.5f-.52f));
                // Kitchen wall cupboards, extractor and cooking plates, above existing worktop.
                float x=-w*.5f+.65f,z=d*.27f;
                b(x,z,.95f,2.25f,1.52f,2.32f,Surface.Accent);
                for(int i=0;i<3;i++)b(x+.49f,z-.7f+i*.7f,.025f,.33f,1.58f,2.26f,Surface.Plaster);
                foreach(int side in new[]{-1,1})Shapes.Column(g.Decoration,x,z+side*.23f,.13f,y+.956f,y+.964f,8,Surface.Metal,1.2f);
                b(0,-d*.5f+.85f,1.2f,.65f,.001f,.006f,Surface.Slide); // Entrance mat, no raised collision.
                // Sofa variation uses textile shapes rather than extra lights or objects.
                for(int pillow=0;pillow<1+r.Range(2);pillow++)b(-w*.27f+(pillow==0?-.65f:.65f),-d*.5f+1.08f,.37f,.31f,.55f,.78f,house.Variant%2==0?Surface.Plaster:Surface.Accent);
            }
            else
            {
                float end=w*.5f-2.45f,x=(.9f+end)*.5f,width=end-.9f-.8f,z=-d*.5f+.72f;
                AddAssembly(g,AssemblyKind.FilingCabinet,new PlanRect(x-width*.5f,z-.36f,x+width*.5f,z+.36f),0,r.Next(),y);
                AddItem(output,house,floor*16+11,MallItemShape.Notebook,new V3(x-.18f,y+1.55f,z));
                AddItem(output,house,floor*16+12,MallItemShape.PencilCase,new V3(x+.18f,y+1.55f,z));
                // Small wall cabinet beside the study, away from the rear stair landing.
                b(-w*.5f+.32f,d*.27f,.32f,1.4f,1.3f,2.2f,Surface.Accent);
            }
            // Calm abstract wall panels: no people, faces or creature imagery.
            b(-w*.5f+.145f,-d*.24f,.026f,1.18f,1.6f,2.18f,Surface.Metal);
            b(-w*.5f+.16f,-d*.24f,.013f,1.06f,1.65f,2.12f,house.Variant%2==0?Surface.Slide:Surface.Plaster);
        }
        private static void AddAssembly(RoomGeometry g,AssemblyKind kind,PlanRect bounds,int turns,uint seed,float y)
        {
            var solid=new MeshData();var detail=new MeshData();AutonomousArchitecture.Build(solid,detail,new GeneratedAssembly(kind,bounds,turns,seed),12,1.2f,false);
            solid.TranslateRotate(new V3(0,y,0),0);detail.TranslateRotate(new V3(0,y,0),0);g.Structure.Append(solid);g.Decoration.Append(detail);
        }
        private static void AddItem(TerrainHouseGeometry output,TerrainHousePlan h,int slot,MallItemShape shape,V3 support)
        {
            int id=(int)shape*8+(int)(StableRandom.Hash(h.Seed,slot,0,0x25096u)%8);
            support.Y+=MallItemCatalog.Get(id).Size.Y*.5f+.012f;output.Items.Add(new TerrainHouseItem(slot,id,support));
        }
    }
}
