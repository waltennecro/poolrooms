using System;

namespace Poolrooms.Core
{
    public enum SoundscapeMode { Peaceful,Uneasy }
    public enum AtmosphereCue { None,RemoteSteps,PipeResonance,WoodCreak,AirMovement }
    [Serializable]
    public sealed class SoundscapePreferences
    {
        public SoundscapeMode mode;
        public float detail=.65f,tension=.55f,dynamicRange=.55f;
        public bool roomDetails=true;
        public bool distantSteps=true,movementSounds=true,objectSounds=true,duckWithMusic=true;
        public void Sanitize()
        {
            if(!Enum.IsDefined(typeof(SoundscapeMode),mode))mode=SoundscapeMode.Peaceful;
            detail=GamePreferences.Clamp(detail,0,1,.65f);tension=GamePreferences.Clamp(tension,0,1,.55f);dynamicRange=GamePreferences.Clamp(dynamicRange,0,1,.55f);
        }
    }
    public static class ImmersiveAudio
    {
        public const int AdditionalSourceBudget=8;
        public static float Approach(float current,float target,float dt,float response)
        {if(!Finite(dt)||dt<=0)return current;return current+(target-current)*(1-(float)Math.Exp(-Math.Min(dt,1)*response));}
        public static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
        public static float ImpactGain(float speed,float mass,bool soft)
        {
            if(!Finite(speed)||!Finite(mass)||speed<.35f||mass<=0)return 0;
            float energy=.5f*Math.Min(40,mass)*Math.Min(144,speed*speed);
            return Math.Min(soft?.13f:.28f,(float)Math.Log(1+energy)*.055f)*(soft?.65f:1);
        }
        public static float Transient(float gain,float dynamicRange)
        {gain=GamePreferences.Clamp(gain,0,1,0);float compressed=gain/(1+gain*.8f);return compressed+(gain-compressed)*GamePreferences.Clamp(dynamicRange,0,1,.55f);}
        public static float Outdoor(Room room,bool terrainOutside)
        {return terrainOutside?1:room!=null&&(room.Environment==LiminalEnvironment.MagnoliaCourts||room.Environment==LiminalEnvironment.Orangerie||room.Environment==LiminalEnvironment.Conservatory||room.Environment==LiminalEnvironment.Coast48||room.Environment==LiminalEnvironment.WinterGarden||room.Environment==LiminalEnvironment.LanternCourts||room.Environment==LiminalEnvironment.BambooRetreat||room.Environment==LiminalEnvironment.HangingOrchards||room.Environment==LiminalEnvironment.LunarGreenhouse)? .65f:0;}
    }
    // Long rests and distance, never a stinger, vocal, close event or frame-based chance.
    public sealed class AtmosphereDirector
    {
        private StableRandom random=new StableRandom(0x5011D0u);
        private float remaining=18;
        private SoundscapeMode mode;
        public void Reset(){remaining=18;}
        public AtmosphereCue Step(float dt,SoundscapeMode selected,float intensity,bool footsteps)
        {
            if(selected!=mode){mode=selected;Reset();}
            if(!ImmersiveAudio.Finite(dt)||dt<=0||selected!=SoundscapeMode.Uneasy||intensity<=0)return AtmosphereCue.None;
            remaining-=Math.Min(dt,1);if(remaining>0)return AtmosphereCue.None;
            remaining=random.Range(18,38)/(.5f+.5f*GamePreferences.Clamp(intensity,0,1,.55f));
            int cue=random.Range(footsteps?4:3);
            return footsteps?(AtmosphereCue)(cue+1):(AtmosphereCue)(cue+2);
        }
    }
}
