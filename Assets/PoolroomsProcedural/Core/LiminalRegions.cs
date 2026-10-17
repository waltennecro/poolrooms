using System;

namespace Poolrooms.Core
{
    // Appended, opt-in settings keep all existing authored worlds intact.
    public enum LiminalGeneration { Authored, Poolrooms, IndoorPlay, Mixed, Supermarkets, MixedWithRetail, Expanded, Offices, Hotels, WaitingHalls, DiningHalls, SoftPlay, City11 }
    public enum LiminalEnvironment { Poolrooms, IndoorPlay, Supermarket, Office, Hotel, WaitingHall, DiningHall, SoftPlay, City11 }

    public static class LiminalRegions
    {
        // Separate 64-bit coordinate mixing avoids seed/x equivalences of the legacy hash.
        // Legacy seeds and authored meshes keep their existing hash function.
        public static uint Hash(int seed,int x,int z,uint salt)
        {
            unchecked
            {
                ulong key=(ulong)ContinuousTopology.CellKey(x,z)+(ulong)(uint)seed*0xD6E8FEB86659FD93UL;
                key^=(ulong)salt*0x9E3779B97F4A7C15UL;
                key=(key^(key>>30))*0xBF58476D1CE4E5B9UL;
                key=(key^(key>>27))*0x94D049BB133111EBUL;
                key^=key>>31;return (uint)(key^(key>>32));
            }
        }
        public static LiminalEnvironment At(LayoutConfig c,int x,int z)
        {
            LiminalEnvironment single;
            if(Single(c.Procedural,out single))return single;
            if(c.Procedural==LiminalGeneration.Supermarkets)return LiminalEnvironment.Supermarket;
            if(c.Procedural==LiminalGeneration.IndoorPlay)return LiminalEnvironment.IndoorPlay;
            if(c.Procedural!=LiminalGeneration.Mixed&&c.Procedural!=LiminalGeneration.MixedWithRetail&&c.Procedural!=LiminalGeneration.Expanded)return LiminalEnvironment.Poolrooms;
            int size=c.EnvironmentRegionSize,rx=ContinuousTopology.FloorDivide(x,size),rz=ContinuousTopology.FloorDivide(z,size);
            double best=double.MaxValue;int selectedX=0,selectedZ=0;
            // Jittered Voronoi regions: no distance stages, stripes or prescribed biome order.
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                int sx=rx+dx,sz=rz+dz;
                uint a=Hash(c.Seed,sx,sz,0xE01u),b=Hash(c.Seed,sx,sz,0xE02u);
                double px=((long)sx*size)+(a>>8)*(size/16777216.0);
                double pz=((long)sz*size)+(b>>8)*(size/16777216.0);
                // The origin region starts in Poolrooms without a scripted sequence afterwards.
                if(sx==0&&sz==0){px=0;pz=0;}
                double xx=x-px,zz=z-pz,distance=xx*xx+zz*zz;
                if(distance<best){best=distance;selectedX=sx;selectedZ=sz;}
            }
            if(selectedX==0&&selectedZ==0)return LiminalEnvironment.Poolrooms;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableCity11&&Unit(Hash(c.Seed,ContinuousTopology.FloorDivide(selectedX,2),ContinuousTopology.FloorDivide(selectedZ,2),0x1100A0u))<c.City11Share)return LiminalEnvironment.City11;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableSoftPlay&&Unit(Hash(c.Seed,selectedX,selectedZ,0x39000u))<c.SoftPlayShare)return LiminalEnvironment.SoftPlay;
            if(c.Procedural==LiminalGeneration.Expanded&&Unit(Hash(c.Seed,selectedX,selectedZ,0x170Au))<c.AdditionalEnvironmentShare)
                return (LiminalEnvironment)((int)LiminalEnvironment.Office+Hash(c.Seed,selectedX,selectedZ,0x170Bu)%4);
            if((c.Procedural==LiminalGeneration.MixedWithRetail||c.Procedural==LiminalGeneration.Expanded)&&Unit(Hash(c.Seed,selectedX,selectedZ,0xE05u))<c.SupermarketShare)return LiminalEnvironment.Supermarket;
            return Unit(Hash(c.Seed,selectedX,selectedZ,0xE03u))<c.IndoorPlayShare?
                LiminalEnvironment.IndoorPlay:LiminalEnvironment.Poolrooms;
        }
        public static bool UsesAuthored(LayoutConfig c,int x,int z)
        {
            if(PoolroomArchitectureMix.Applies(c,x,z))return !PoolroomArchitectureMix.UsesSolar(c,x,z);
            return c.Procedural==LiminalGeneration.Authored||
                (At(c,x,z)==LiminalEnvironment.Poolrooms&&(x!=0||z!=0)&&
                 Unit(Hash(c.Seed,x,z,0xE04u))<c.AuthoredRoomChance);
        }
        public static bool Find(LayoutConfig c,LiminalEnvironment environment,int x,int z,out int foundX,out int foundZ)
        {
            foundX=x;foundZ=z;
            if(!Enum.IsDefined(typeof(LiminalEnvironment),environment))return false;
            if(environment==LiminalEnvironment.City11&&c.Procedural!=LiminalGeneration.City11&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableCity11||c.City11Share<=0))return false;
            if(environment==LiminalEnvironment.SoftPlay&&c.Procedural!=LiminalGeneration.SoftPlay&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableSoftPlay||c.SoftPlayShare<=0))return false;
            LiminalEnvironment single;
            if(Single(c.Procedural,out single))return environment==single;
            if(InteriorPlanner.IsInterior(environment)&&(c.Procedural!=LiminalGeneration.Expanded||c.AdditionalEnvironmentShare==0))return false;
            if(c.Procedural==LiminalGeneration.Expanded&&c.AdditionalEnvironmentShare==1&&!InteriorPlanner.IsInterior(environment)&&environment!=LiminalEnvironment.Poolrooms&&environment!=LiminalEnvironment.City11)return false;
            if(c.Procedural==LiminalGeneration.Supermarkets&&environment!=LiminalEnvironment.Supermarket)return false;
            if(environment==LiminalEnvironment.Supermarket&&
                (c.Procedural!=LiminalGeneration.Supermarkets&&c.Procedural!=LiminalGeneration.MixedWithRetail&&c.Procedural!=LiminalGeneration.Expanded||(c.Procedural==LiminalGeneration.MixedWithRetail||c.Procedural==LiminalGeneration.Expanded)&&c.SupermarketShare==0))return false;
            if((c.Procedural==LiminalGeneration.MixedWithRetail||c.Procedural==LiminalGeneration.Expanded)&&environment==LiminalEnvironment.IndoorPlay&&(c.SupermarketShare==1||c.IndoorPlayShare==0))return false;
            if(c.Procedural==LiminalGeneration.Authored)return environment==LiminalEnvironment.Poolrooms;
            if(c.Procedural==LiminalGeneration.Poolrooms&&environment!=LiminalEnvironment.Poolrooms)return false;
            if(c.Procedural==LiminalGeneration.IndoorPlay&&environment!=LiminalEnvironment.IndoorPlay)return false;
            if(c.Procedural==LiminalGeneration.Mixed&&c.IndoorPlayShare==0&&environment==LiminalEnvironment.IndoorPlay)return false;
            for(int radius=0;radius<=128;radius++)for(int offset=-radius;offset<=radius;offset++)for(int side=0;side<4;side++)
            {
                long xx=(long)x+(side==0||side==2?offset:side==1?radius:-radius);
                long zz=(long)z+(side==1||side==3?offset:side==0?radius:-radius);
                if(xx<int.MinValue||xx>int.MaxValue||zz<int.MinValue||zz>int.MaxValue)continue;
                if(At(c,(int)xx,(int)zz)!=environment)continue;foundX=(int)xx;foundZ=(int)zz;return true;
            }
            return false;
        }
        private static bool Single(LiminalGeneration mode,out LiminalEnvironment environment)
        {
            switch(mode){case LiminalGeneration.City11:environment=LiminalEnvironment.City11;return true;case LiminalGeneration.SoftPlay:environment=LiminalEnvironment.SoftPlay;return true;case LiminalGeneration.Offices:environment=LiminalEnvironment.Office;return true;
                case LiminalGeneration.Hotels:environment=LiminalEnvironment.Hotel;return true;
                case LiminalGeneration.WaitingHalls:environment=LiminalEnvironment.WaitingHall;return true;
                case LiminalGeneration.DiningHalls:environment=LiminalEnvironment.DiningHall;return true;
                default:environment=LiminalEnvironment.Poolrooms;return false;}
        }
        private static float Unit(uint value){return (value>>8)*(1f/16777216f);}
    }
}
