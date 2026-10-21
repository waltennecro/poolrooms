using System;

namespace Poolrooms.Core
{
    public readonly struct AcousticLook
    {
        public readonly float Ventilation,Water,Decay,HighDecay,ReflectionsDelay,ReverbDelay;
        public AcousticLook(float ventilation,float water,float decay,float highDecay,float reflectionsDelay,float reverbDelay)
        {Ventilation=ventilation;Water=water;Decay=decay;HighDecay=highDecay;ReflectionsDelay=reflectionsDelay;ReverbDelay=reverbDelay;}
    }

    public static class AmbientAudioModel
    {
        public static AcousticLook ForRoom(Room room)
        {
            if(room==null)return new AcousticLook(0,0,.8f,.5f,.01f,.02f);
            if(room.Plan!=null)
            {
                float proceduralDelay=Math.Min(.045f,(room.Width+room.Depth)/1600);
                if(room.Plan.Restful!=null)
                {
                    bool absorbent=JourneyLevelPlanner.Absorbent(room.Environment)||room.Environment==LiminalEnvironment.SunsetLibrary||room.Environment==LiminalEnvironment.MusicStudios;
                    return new AcousticLook(absorbent?.025f:.045f,room.Plan.Pools.Length>0?.14f:0,absorbent?.78f:1.75f,absorbent?.35f:.6f,proceduralDelay,proceduralDelay*1.5f);
                }
                if(room.Environment==LiminalEnvironment.RainHouses)return new AcousticLook(.015f,0,.65f,.35f,proceduralDelay*.6f,proceduralDelay);
                if(room.Environment==LiminalEnvironment.AquaticPark)return new AcousticLook(.015f,.18f,2.5f,.55f,proceduralDelay,proceduralDelay*1.7f);
                if(room.Environment==LiminalEnvironment.ThermalBaths)return new AcousticLook(.035f,.14f,2.05f,.52f,proceduralDelay,proceduralDelay*1.7f);
                if(room.Environment==LiminalEnvironment.Orangerie)return new AcousticLook(.035f,0,1.25f,.42f,proceduralDelay,proceduralDelay*1.5f);
                if(room.Environment==LiminalEnvironment.Conservatory)return new AcousticLook(.08f,0,1.65f,.58f,proceduralDelay,proceduralDelay*1.6f);
                if(room.Environment==LiminalEnvironment.TransitGallery)return new AcousticLook(.12f,0,2.1f,.63f,proceduralDelay,proceduralDelay*1.8f);
                if(room.Plan.Interior!=null)
                {
                    bool soft=room.Environment==LiminalEnvironment.Office||room.Environment==LiminalEnvironment.Hotel;
                    return new AcousticLook(soft?.10f:.15f,0,soft?.75f:1.35f,soft?.32f:.48f,proceduralDelay,proceduralDelay*1.4f);
                }
                if(room.Environment==LiminalEnvironment.Supermarket)return new AcousticLook(.21f,0,Math.Min(1.8f,.6f+room.Height*.13f),.5f,proceduralDelay,proceduralDelay*1.5f);
                return room.Environment==LiminalEnvironment.IndoorPlay||room.Environment==LiminalEnvironment.SoftPlay?
                    new AcousticLook(.13f,0,Math.Min(1.3f,.5f+room.Height*.07f),.42f,proceduralDelay,proceduralDelay*1.4f):
                    new AcousticLook(.16f,room.Plan.Pools.Length>0?.2f:0,Math.Min(2.7f,.6f+room.Height*.2f),.66f,proceduralDelay,proceduralDelay*1.7f);
            }
            bool dry=room.ZoneId>=0&&ZoneCatalog.Get(room.ZoneId).Pools==PoolPlan.Dry;
            ZoneFamily family=room.ZoneId>=0?ZoneCatalog.Get(room.ZoneId).Family:ZoneFamily.Natatoriums;
            float decay=Clamp(.45f+room.Height*.2f,.9f,2.7f);
            float vent=.18f,water=dry?0:.2f,high=.66f;
            if(family==ZoneFamily.ThermalCells||family==ZoneFamily.ChangingRooms||family==ZoneFamily.QuietLounges)
            {decay*=.65f;vent=.12f;water*=.65f;high=.48f;}
            if(family==ZoneFamily.ServiceGalleries){vent=.3f;water*=.6f;decay*=.8f;}
            if(family==ZoneFamily.LightWells||family==ZoneFamily.RecessedCourts){decay*=.8f;vent*=.65f;}
            float delay=Clamp((room.Width+room.Depth)/1600,.012f,.045f);
            return new AcousticLook(vent,water,decay,high,delay,delay*1.7f);
        }
        public static AcousticLook Blend(AcousticLook a,AcousticLook b,float weight)
        {
            float t=CameraPresentation.Unit(weight);
            return new AcousticLook(Lerp(a.Ventilation,b.Ventilation,t),Lerp(a.Water,b.Water,t),Lerp(a.Decay,b.Decay,t),
                Lerp(a.HighDecay,b.HighDecay,t),Lerp(a.ReflectionsDelay,b.ReflectionsDelay,t),Lerp(a.ReverbDelay,b.ReverbDelay,t));
        }
        public static float PassageBlend(float distance,float roomSize,float neighbourSize,float cellSize)
        {
            float start=roomSize*.5f-1,end=cellSize-neighbourSize*.5f+1;
            return end>start?CameraPresentation.Unit((distance-start)/(end-start)):0;
        }
        public static float SplashVolume(float strength){return .12f+.55f*CameraPresentation.Unit(strength);}
        private static float Clamp(float value,float min,float max){return Math.Max(min,Math.Min(max,value));}
        private static float Lerp(float a,float b,float t){return a+(b-a)*t;}
    }

    // Accumulate actual horizontal travel, never input or frames. One sound at most per update.
    public sealed class AudioStrideTracker
    {
        private float travelled;
        public void Reset(){travelled=0;}
        public bool Sample(float distance,bool grounded,bool swimming,bool wet,float deltaTime,bool sliding=false)
        {
            if(sliding){Reset();return false;}
            if(deltaTime<=0)return false;
            if(float.IsNaN(distance)||float.IsInfinity(distance)||distance<0||distance>Math.Max(2,deltaTime*8))
            {Reset();return false;}
            if(!grounded&&!swimming){Reset();return false;}
            if(distance/deltaTime<.12f)return false;
            float stride=swimming?1.35f:wet?1.05f:1.45f;
            travelled+=distance;
            if(travelled<stride)return false;
            travelled%=stride;return true;
        }
    }
}
