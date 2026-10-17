namespace Poolrooms.Core
{
    public enum PickupSurface { Packaging,Rigid,SoftBall,Foam,Rubber }
    public readonly struct PickupPhysics
    {
        public readonly float AirDamping,AngularDamping,Friction,Restitution;
        public PickupPhysics(PickupSurface surface)
        {
            AirDamping=surface==PickupSurface.Packaging?.12f:.015f;
            AngularDamping=surface==PickupSurface.Foam?.3f:.12f;
            Friction=surface==PickupSurface.Rubber?.8f:surface==PickupSurface.Foam?.7f:.5f;
            Restitution=surface==PickupSurface.SoftBall?.45f:surface==PickupSurface.Rubber?.32f:surface==PickupSurface.Foam?.03f:.08f;
        }
        public static V3 ReleaseVelocity(V3 playerVelocity)
        {
            if(float.IsNaN(playerVelocity.X)||float.IsInfinity(playerVelocity.X)||float.IsNaN(playerVelocity.Y)||float.IsInfinity(playerVelocity.Y)||float.IsNaN(playerVelocity.Z)||float.IsInfinity(playerVelocity.Z))return new V3();
            float length=playerVelocity.Length;
            if(float.IsInfinity(length))return new V3();
            return length>8?playerVelocity*(2.8f/length):playerVelocity*.35f;
        }
    }
}
