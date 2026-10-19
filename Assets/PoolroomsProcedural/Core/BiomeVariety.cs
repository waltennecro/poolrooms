using System;

namespace Poolrooms.Core
{
    public static class BiomeVariety
    {
        // A complete coordinate bank, including negative/extreme addresses. The multiplier
        // is coprime to the bank size so every recipe is reachable, not just a larger modulus.
        public static int RecipeAt(int seed,int x,int z,int columns,int rows,uint salt)
        {
            int count=checked(columns*rows);
            long xx=((long)x%columns+columns)%columns,zz=((long)z%rows+rows)%rows;
            uint hash=LiminalRegions.Hash(seed,ContinuousTopology.FloorDivide(x,columns),ContinuousTopology.FloorDivide(z,rows),salt);
            int multiplier=1+(int)(hash%(uint)count);
            while(Gcd(multiplier,count)!=1)multiplier=multiplier==count?1:multiplier+1;
            return (int)(((xx*rows+zz)*multiplier+(hash>>10))%count);
        }
        static int Gcd(int a,int b){while(b!=0){int t=a%b;a=b;b=t;}return a;}
    }
}
