using System;

namespace Poolrooms.Core
{
    public enum TerrainVisualQuality { Economy, High }
    public static class TerrainAppearance
    {
        public static int SkySize(TerrainVisualQuality quality){return quality==TerrainVisualQuality.High?256:128;}
        public static int ShadowSize(TerrainVisualQuality quality){return quality==TerrainVisualQuality.High?2048:512;}
        // Shared world coordinates: neighbouring meshes and the coarse horizon sample one field.
        public static float SoilWeight(TerrainRegionPlan p,float x,float z)
        {
            float variation=p.Noise(x/83,z/83,34)*.16f+p.Noise(x/21,z/21,35)*.06f;
            // Use concrete cached path lists; no boxed iterator or temporary collection per vertex.
            float distance=p.PathDistance(x,z,true);
            float edge=1-TerrainRegionPlan.Smooth(distance,2.8f,5.9f);
            return Math.Max(variation,edge*.88f);
        }
        public static float Shelter(TerrainHousePlan h,V3 point)
        {
            if(h==null||!h.Contains(point.X,point.Y,point.Z))return 0;
            float x=Math.Abs(point.X-h.X),z=point.Z-h.Z;
            float wall=Math.Min(h.Width*.5f-x,h.Depth*.5f-Math.Abs(z));
            float sheltered=.72f+.28f*TerrainRegionPlan.Smooth(wall,.2f,2.8f);
            // A broad entrance transition leaves direct daylight at the open doorway.
            if(Math.Abs(point.X-h.EntranceX)<1.3f)
            {
                float y=point.Y-h.Base;
                float entryLevel=h.Blueprint==null?(y<TerrainHousePlan.FloorHeight?1:0):
                    TerrainRegionPlan.Smooth(y,-.65f,-.05f)*(1-TerrainRegionPlan.Smooth(y,2.2f,TerrainHousePlan.FloorHeight));
                // U-flights end close to the front door: fade vertically to prevent exposure jumps on cellar/upper landings.
                sheltered*=1-(1-TerrainRegionPlan.Smooth(z+h.Depth*.5f,0,4))*entryLevel;
            }
            return sheltered;
        }
        public static float Exposure(TerrainDaylight daylight,float shelter,bool directSun=true)
        {
            float outdoor=daylight==TerrainDaylight.SoftClouds?11.35f:daylight==TerrainDaylight.WarmAfternoon?11.7f:12;
            if(!directSun)outdoor-=1.8f; // Keep skylight readable when the global shadow budget disables the connected sun.
            return outdoor+(8.7f-outdoor)*CameraPresentation.Unit(shelter);
        }
        private static float Cloud(TerrainRegionPlan p,float x,float z)
        {
            return p.Noise(x,z,9)*.52f+p.Noise(x*2.07f+5,z*2.07f-9,10)*.28f+
                p.Noise(x*4.13f-12,z*4.13f+6,11)*.14f+p.Noise(x*8.31f,z*8.31f,12)*.06f;
        }
        public static V3 Sky(TerrainRegionPlan p,V3 direction)
        {
            V3 d=direction.Unit;bool warm=p.Daylight==TerrainDaylight.WarmAfternoon;
            float elevation=TerrainRegionPlan.Smooth(d.Y,-.05f,.85f);
            V3 horizon=warm?new V3(.81f,.79f,.73f):new V3(.72f,.82f,.91f);
            V3 zenith=warm?new V3(.22f,.42f,.67f):new V3(.12f,.33f,.68f);
            V3 color=horizon*(1-elevation)+zenith*elevation;
            float cover=0;
            if(d.Y>.02f)
            {
                float scale=3.5f/(d.Y+.23f),x=d.X*scale+14,z=d.Z*scale-8;
                float mass=Cloud(p,x,z),threshold=p.Daylight==TerrainDaylight.SoftClouds?.405f:.535f;
                cover=TerrainRegionPlan.Smooth(mass,threshold,threshold+.19f)*TerrainRegionPlan.Smooth(d.Y,.02f,.13f);
                // Baked self-shading and lit rims; no realtime volumetric cloud pass.
                float neighbour=Cloud(p,x+p.SunDirection.X*.24f,z+p.SunDirection.Z*.24f);
                float shade=CameraPresentation.Unit(.61f+(mass-neighbour)*3.2f);
                float rim=(1-TerrainRegionPlan.Smooth(mass,threshold+.035f,threshold+.14f))*.16f;
                V3 shadow=warm?new V3(.64f,.63f,.60f):new V3(.59f,.66f,.74f);
                V3 lit=warm?new V3(1,.94f,.83f):new V3(1,.99f,.96f);
                V3 cloud=shadow*(1-shade)+lit*(shade+rim);
                color=color*(1-cover)+cloud*cover;
            }
            float dot=Math.Max(0,V3.Dot(d,p.SunDirection));
            float halo=(float)Math.Pow(dot,64)*.17f*(1-cover*.8f);
            float disk=TerrainRegionPlan.Smooth(dot,.99965f,.99997f)*(p.Daylight==TerrainDaylight.SoftClouds?5:18)*(1-cover*.93f);
            // A dim lower hemisphere avoids the former blue glow under hills.
            if(d.Y<-.05f)color=new V3(.15f,.19f,.11f);
            return color+new V3(1,.94f,.81f)*(halo+disk);
        }
    }
}
