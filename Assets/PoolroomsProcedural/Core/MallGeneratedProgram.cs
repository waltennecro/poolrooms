using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // A shop's furniture and its interactive stock share one immutable, seed-derived description.
    public sealed class MallGeneratedProgram
    {
        public readonly GeneratedAssembly[] Assemblies;
        public readonly PlanRect[] Routes;
        private readonly MallShop shop;
        public MallGeneratedProgram(MallShop s)
        {
            shop=s;var random=new StableRandom(s.Seed^0x22010u);
            var routes=new List<PlanRect>{new PlanRect(-1.75f,0,1.75f,s.Kind==MallShopKind.Cinema?s.Depth-1.05f:s.Depth),new PlanRect(-s.Width*.5f,0,s.Width*.5f,3.25f),
                new PlanRect(-s.Width*.5f,s.Depth-2.3f,s.Width*.5f,s.Kind==MallShopKind.Cinema?s.Depth-1.05f:s.Depth)};
            float pitch=random.Range(6.2f,8.2f);
            for(float z=8;z<s.Depth-4;z+=pitch)routes.Add(new PlanRect(-s.Width*.5f,z-.8f,s.Width*.5f,z+.8f));
            Routes=routes.ToArray();
            var obstacles=new List<PlanRect>(routes);
            if(s.Kind==MallShopKind.Cafe||s.Kind==MallShopKind.FoodCourt)obstacles.Add(new PlanRect(2.25f,4.4f,5.35f,5.6f));
            if(s.Realistic&&(s.Kind==MallShopKind.Home||s.Kind==MallShopKind.Shoes||s.Kind==MallShopKind.Hypermarket||s.Kind==MallShopKind.Accessories))
                foreach(int side in new[]{-1,1}){float px=side*(s.Width*.5f-2);obstacles.Add(new PlanRect(px-1.3f,s.Depth-5.3f,px+1.3f,s.Depth-2.5f));}
            var bounds=new PlanRect(-s.Width*.5f+.65f,3.6f,s.Width*.5f-.65f,s.Depth-2.4f);
            // Keep enough approach space to select products on the lower shelves.
            Assemblies=s.Kind==MallShopKind.Restrooms?new GeneratedAssembly[0]:s.Realistic?MallRetailPacking.Fill(bounds,obstacles,Kinds(s.Kind),random.Next()):AssemblyPacking.Fill(bounds,obstacles,Kinds(s.Kind),random.Next(),s.Kind==MallShopKind.Hypermarket?24:9+random.Range(7),1.6f);
        }
        private static AssemblyKind[] Kinds(MallShopKind kind)
        {
            switch(kind)
            {
                case MallShopKind.Fashion:return new[]{AssemblyKind.ClothesRail,AssemblyKind.ClothesRail,AssemblyKind.DisplayRack,AssemblyKind.Showcase,AssemblyKind.Checkout};
                case MallShopKind.Shoes:return new[]{AssemblyKind.DisplayRack,AssemblyKind.ShoeBench,AssemblyKind.Showcase,AssemblyKind.Checkout};
                case MallShopKind.Books:return new[]{AssemblyKind.Bookcase,AssemblyKind.Bookcase,AssemblyKind.Table,AssemblyKind.Bench,AssemblyKind.Checkout};
                case MallShopKind.Electronics:case MallShopKind.Accessories:return new[]{AssemblyKind.Table,AssemblyKind.Showcase,AssemblyKind.Checkout};
                case MallShopKind.Home:return new[]{AssemblyKind.Bed,AssemblyKind.Bench,AssemblyKind.Table,AssemblyKind.Sideboard,AssemblyKind.Bookcase,AssemblyKind.Vanity,AssemblyKind.Showcase};
                case MallShopKind.Cafe:case MallShopKind.FoodCourt:return new[]{AssemblyKind.Table,AssemblyKind.Table,AssemblyKind.SeatGroup,AssemblyKind.Sideboard};
                case MallShopKind.Arcade:return new[]{AssemblyKind.ArcadeCabinet,AssemblyKind.ArcadeCabinet,AssemblyKind.Table};
                case MallShopKind.Cinema:return new[]{AssemblyKind.SeatGroup,AssemblyKind.SeatGroup,AssemblyKind.Bench};
                case MallShopKind.Toys:return new[]{AssemblyKind.Cubbies,AssemblyKind.DisplayRack,AssemblyKind.PlayTable,AssemblyKind.Checkout};
                default:return new[]{AssemblyKind.DisplayRack,AssemblyKind.DisplayRack,AssemblyKind.Showcase,AssemblyKind.Checkout};
            }
        }
        public void Build(MeshData physical,MeshData detail,int quality)
        {
            if(shop.Kind==MallShopKind.Restrooms)
            {MallFurnishing.Store(physical,detail,shop,quality,false);return;}
            foreach(var a in Assemblies)
            {
                bool replaced=false;
                if(MallRealism.Replaces(shop,a))foreach(var source in MallRealism.AssemblySources(shop,a,0)){replaced=true;break;}
                if(!replaced)AutonomousArchitecture.Build(physical,detail,a,quality,1.2f,false);
            }
            float w=shop.Width,d=shop.Depth;
            // Perimeter finishes and equipment belong to the shop, with no extra dynamic light.
            foreach(int side in new[]{-1,1})
            {
                float x=side*(w*.5f-.2f);
                MallArchitecture.Box(detail,x,d*.5f,.05f,d-.4f,.04f,.17f,Surface.Metal);
                var r=new StableRandom(shop.Seed^(side<0?0x22011u:0x22012u));
                for(float z=4;z<d-2;z+=r.Range(4.5f,7))
                {
                    MallArchitecture.Box(detail,x,z,.025f,1.8f,2.2f,3.1f,Surface.Slide);
                    MallArchitecture.Box(detail,x-side*.02f,z,.02f,1.7f,2.28f,3.02f,Surface.Plaster);
                }
            }
            if(!shop.Realistic)for(float z=4;z<d-2;z+=5.5f)
            {
                MallArchitecture.Box(detail,0,z,1.1f,.7f,4.86f,4.89f,Surface.Glow);
                if(quality>12)for(int fin=0;fin<4;fin++)MallArchitecture.Box(detail,w*.3f+(fin-1.5f)*.1f,z,.04f,.7f,4.88f,4.9f,Surface.Metal);
            }
            if(shop.Kind==MallShopKind.Cinema)
            {
                MallArchitecture.Box(physical,0,d-.6f,w-1.4f,.18f,.3f,3.7f,Surface.Slide);
                MallArchitecture.Box(detail,0,d-.71f,w-2,.025f,.75f,3.2f,Surface.Plaster);
            }
            if(shop.Kind==MallShopKind.Cafe||shop.Kind==MallShopKind.FoodCourt)
            {
                // Service counter uses the reserved entrance-side bay; the back aisle stays open.
                MallArchitecture.Box(physical,3.8f,5,3,1.1f,0,1.1f,Surface.Accent);
                MallArchitecture.Box(detail,3.8f,5,3.1f,1.15f,1.1f,1.15f,Surface.Plaster);
                MallArchitecture.Box(detail,3.8f,5,.6f,.55f,1.15f,1.6f,Surface.Metal);
            }
        }
        public IEnumerable<MallLooseProp> Stock()
        {
            var result=new List<MallLooseProp>();var random=new StableRandom(shop.Seed^0x22020u);var categories=MallMerchandise.Categories(shop.Kind);
            // The familiar front displays remain part of the v21 architecture, with reliable supports.
            foreach(int side in new[]{-1,1})for(int col=0;col<4;col++)
                Add(result,categories,ref random,new V3(side*5+(col-1.5f)*.55f,1,1.95f),0);
            foreach(var a in Assemblies)
            {
                if(MallRealism.Replaces(shop,a))
                {bool replaced=false;foreach(var source in MallRealism.AssemblySources(shop,a,0)){replaced=true;break;}if(replaced)continue;}
                foreach(var point in GeneratedFurnishing.StockSupports(a))
                    Add(result,categories,ref random,point,a.Turns);
            }
            return result;
        }
        private void Add(List<MallLooseProp> result,MallItemShape[] categories,ref StableRandom random,V3 support,int turns)
        {
            MallItemShape shape=categories[random.Range(categories.Length)];
            // Folded clothes fit real shelves; hanging variants need a different support program.
            int variant=random.Range(shape==MallItemShape.Shirt?4:8),id=(int)shape*8+variant;
            var item=MallItemCatalog.Get(id);support.Y+=item.Size.Y*.5f+.003f;
            float angle=(shop.Side==1?0:Shapes.Pi)+turns*Shapes.Pi*.5f*(shop.FurnishingMirror<0?-1:1);
            result.Add(new MallLooseProp(shop.Id*4096+result.Count,shop.World(support.X,support.Y,support.Z),id,angle));
        }
    }
}
