using System;

namespace Poolrooms.Core
{
    // Separate from CameraViewMode, whose serialized IDs describe optical styles.
    public enum CameraPerspective { FirstPerson,NearThirdPerson,FarThirdPerson,Front,RightShoulder,LeftShoulder,Overhead,Isometric }
    public readonly struct PerspectivePose
    {
        public readonly V3 Offset;
        public readonly string Name;
        public readonly bool FollowPitch;
        public PerspectivePose(string name,V3 offset,bool followPitch=true){Name=name;Offset=offset;FollowPitch=followPitch;}
    }
    public static class CameraPerspectives
    {
        public const float CollisionRadius=.16f,PhotoRadius=24;
        public static CameraPerspective Next(CameraPerspective value){return (CameraPerspective)(((int)value+1)%8);}
        public static PerspectivePose Get(CameraPerspective value)
        {
            switch(value)
            {
                case CameraPerspective.NearThirdPerson:return new PerspectivePose("Tercera cercana",new V3(0,.35f,-2.6f));
                case CameraPerspective.FarThirdPerson:return new PerspectivePose("Tercera lejana",new V3(0,.7f,-5));
                case CameraPerspective.Front:return new PerspectivePose("Frontal",new V3(0,.25f,3.2f));
                case CameraPerspective.RightShoulder:return new PerspectivePose("Hombro derecho",new V3(.65f,.08f,-1.6f));
                case CameraPerspective.LeftShoulder:return new PerspectivePose("Hombro izquierdo",new V3(-.65f,.08f,-1.6f));
                case CameraPerspective.Overhead:return new PerspectivePose("Cenital",new V3(0,7,-.35f),false);
                case CameraPerspective.Isometric:return new PerspectivePose("Isométrica",new V3(4.5f,4.5f,-4.5f),false);
                default:return new PerspectivePose("Primera persona",new V3());
            }
        }
        public static float SafeDistance(float desired,float hitDistance)
        {return Math.Max(0,Math.Min(desired,hitDistance-.04f));}
        public static float Recovery(float current,float safe,float delta)
        {
            if(safe<=current)return Math.Max(0,safe); // Obstruction contracts immediately; never interpolate through a wall.
            delta=GamePreferences.Clamp(delta,0,.1f,0);
            return current+(safe-current)*(1-(float)Math.Exp(-delta*10));
        }
    }
}
