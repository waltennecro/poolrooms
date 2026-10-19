using System;

namespace Poolrooms.Core
{
    // Bounded street furniture and facade construction, combined with each block.
    // The six-metre driving cross and protected building entrances stay untouched.
    public static class CityStreetDetails
    {
        static void Box(MeshData m,float x,float z,float w,float d,float low,float high,Surface surface)
        {ZoneShapes.Box(m,x,z,w,d,low,high,surface,1.2f);}
        public static int District(LayoutConfig c,int x,int z)
        {return (int)(LiminalRegions.Hash(c.Seed,ContinuousTopology.FloorDivide(x,6),ContinuousTopology.FloorDivide(z,6),0x1311D1u)%4);}
        public static int Typology(CityBuildingPlan b){return b.FacadeTypology>=0?b.FacadeTypology:(int)((b.Seed>>12)%6);}
        public static void Append(RoomGeometry o,Room r,LayoutConfig c)
        {
            var paving=new MeshData{Theme=SurfaceTheme.CityFacade,EmissionTint=o.Decoration.EmissionTint,EmissionStrength=.4f};var paint=new MeshData{Theme=SurfaceTheme.Airfield};
            float half=c.CellSize*.5f;int district=District(c,r.X,r.Z);
            foreach(int axis in new[]{0,1})foreach(int sign in new[]{-1,1})
            {
                var kerb=new MeshData();
                // Interrupt kerbs at the junction; ramped, eight-centimetre edges
                // can be stepped over without adding an invisible tall collider.
                foreach(int end in new[]{-1,1})Box(kerb,sign*3.04f,end*(half+4.8f)*.5f,.08f,half-4.8f,0,.08f,Surface.Plaster);
                if(axis==1)kerb.TranslateRotate(new V3(),Shapes.Pi*.5f);o.Structure.Append(kerb);
                // Drain grates and zebra paint remain visual, with no raised bumps.
                float x=axis==0?sign*3.18f:sign*7.1f,z=axis==0?sign*7.1f:sign*3.18f;
                Box(paving,x,z,axis==0?.22f:.65f,axis==0?.65f:.22f,.004f,.014f,Surface.Metal);
                for(int stripe=-2;stripe<=2;stripe++)
                {float a=axis==0?stripe*.95f:sign*5.1f,b=axis==0?sign*5.1f:stripe*.95f;Box(paint,a,b,axis==0?.5f:1.6f,axis==0?1.6f:.5f,.016f,.018f,Surface.Accent);}
            }
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                float px=x*3.75f,pz=z*3.75f;
                Shapes.Column(o.Structure,px,pz,.06f,0,4.1f,8,Surface.Metal,1.2f);
                Box(paving,px,pz,.35f,.35f,4.05f,4.18f,Surface.Metal);Box(paving,px,pz,.26f,.26f,4.025f,4.055f,Surface.Glow);
                o.Fixtures.Add(new Fixture{Position=new V3(px,4,pz),Power=1100,Width=.26f,Depth=.26f,Range=8,CustomColor=true,LightColor=o.Decoration.EmissionTint});
                // Concrete footing and street bin outside both driving and entrance axes.
                Box(o.Structure,px,pz,.2f,.2f,0,.12f,Surface.Metal);
                if((district+x+z+4)%3==0){Box(o.Structure,x*4.1f,z*6.1f,.42f,.42f,0,.78f,Surface.Metal);Box(paving,x*4.1f,z*6.1f,.46f,.46f,.78f,.83f,Surface.Metal);}
            }
            o.VisualMeshes.Add(paving);o.VisualMeshes.Add(paint);
        }
        public static void Facade(MeshData solid,MeshData detail,CityBuildingPlan b)
        {
            float w=b.Width,d=b.Depth,h=b.Floors*3.2f+.12f;int type=Typology(b);
            if(type==0||type==4)
            {
                // Covered arcade/entry porch; posts sit beyond the central door.
                foreach(int side in new[]{-1,1})Shapes.Column(solid,side*Math.Min(w*.35f,3.6f),-d*.5f-.85f,.1f,0,3.05f,12,Surface.Plaster,1.2f);
                Box(solid,0,-d*.5f-.6f,w*.82f,1.3f,3.05f,3.18f,Surface.Plaster);
                if(type==4)for(int rib=0;rib<7;rib++)Box(detail,(rib-3)*w*.1f,-d*.5f-.6f,.045f,1.3f,3.19f,3.28f,Surface.Accent);
            }
            if(type==1||type==5)
            {
                // Real stepped cornice / roof lantern. No new hidden floor or stair.
                for(int tier=0;tier<3;tier++)Box(detail,0,0,w+.25f-tier*.18f,d+.25f-tier*.18f,h+.18f+tier*.11f,h+.27f+tier*.11f,Surface.Accent);
                if(type==5)
                {
                    Box(solid,w*.22f,d*.12f,w*.3f,d*.24f,h+.18f,h+1.5f,Surface.Plaster);
                    Box(detail,w*.22f,d*.12f,w*.34f,d*.28f,h+1.5f,h+1.65f,Surface.Metal);
                }
            }
            if(type==6)
            {
                // Two narrow rooftop monitor roofs, entirely inside the building footprint.
                foreach(int side in new[]{-1,1})
                {
                    float x=side*w*.24f;
                    Box(solid,x,0,w*.2f,d*.6f,h+.1f,h+.85f,Surface.Plaster);
                    Box(detail,x,0,w*.24f,d*.66f,h+.85f,h+1,Surface.Metal);
                }
            }
            if(type==7)
            {
                // Continuous brise-soleil above each floor's windows, clear of the entrance.
                for(int level=0;level<b.Floors;level++)for(int rib=0;rib<3;rib++)
                    Box(detail,0,-d*.5f-.15f-rib*.09f,w*.88f,.08f,level*3.2f+2.65f,level*3.2f+2.72f,Surface.Metal);
            }
            if(type==8)
            {
                // A stepped roof garden parapet, without an inaccessible extra storey.
                for(int tier=0;tier<3;tier++)foreach(int side in new[]{-1,1})
                {
                    float inset=.28f+tier*.25f,y=h+.1f+tier*.18f;
                    Box(solid,side*(w*.5f-inset),0,.15f,d-inset*2,y,y+.15f,Surface.Plaster);
                    Box(solid,0,side*(d*.5f-inset),w-inset*2,.15f,y,y+.15f,Surface.Plaster);
                }
            }
            if(type==2)
                for(int level=1;level<b.Floors;level++)foreach(int side in new[]{-1,1})
                {float x=side*w*.32f;Box(detail,x,-d*.5f-.12f,.22f,.24f,level*3.2f+.25f,level*3.2f+2.9f,Surface.Accent);}
            if(type==3)
                for(int level=0;level<b.Floors;level++)for(int slat=0;slat<4;slat++)Box(detail,w*.34f,-d*.5f-.16f,w*.14f,.12f,level*3.2f+.65f+slat*.16f,level*3.2f+.7f+slat*.16f,Surface.Metal);
        }
    }
}
