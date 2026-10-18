using System;

namespace Poolrooms.Core
{
    // Unity-free vertical state. Only a real landing restores both jumps.
    public sealed class ExplorerLocomotion
    {
        public const float Gravity=14,JumpImpulse=4.2f,TerminalSpeed=18,MaxFrameTime=.25f,MaxStepTime=1f/30;
        public const float EdgeGraceTime=.10f,JumpBufferTime=.12f;
        private float edgeGrace,jumpBuffer;
        public float VerticalSpeed { get; private set; }
        public int JumpsUsed { get; private set; }=1;
        public int RemainingJumps { get { return 2-JumpsUsed; } }
        public static float FrameTime(float dt)
        {return float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0?0:Math.Min(dt,MaxFrameTime);}
        public static int Steps(float dt)
        {return Math.Max(1,Math.Min(8,(int)Math.Ceiling(FrameTime(dt)/MaxStepTime)));}
        public void Reset(){VerticalSpeed=0;JumpsUsed=1;edgeGrace=jumpBuffer=0;}
        public void ClearJumpIntent(){edgeGrace=jumpBuffer=0;}
        public void Suspend(){VerticalSpeed=0;ClearJumpIntent();} // Loading/pausing never grants another airborne jump.
        public void Land(){if(VerticalSpeed<=0){JumpsUsed=0;VerticalSpeed=-2;edgeGrace=EdgeGraceTime;}}
        public void HitCeiling(){if(VerticalSpeed>0)VerticalSpeed=0;}
        public bool Step(float dt,bool grounded,bool swimming,float waterOffset,bool jumpPressed)
        {
            dt=FrameTime(dt);if(dt<=0)return false;
            if(swimming)
            {
                edgeGrace=jumpBuffer=0;
                float buoyancy=waterOffset*5;
                if(float.IsNaN(buoyancy)||float.IsInfinity(buoyancy))buoyancy=0;
                VerticalSpeed=Math.Max(-2,Math.Min(2,buoyancy))+(jumpPressed?2:0);
                JumpsUsed=1;return false;
            }
            // CharacterController.isGrounded can still describe the previous Move after takeoff.
            bool landed=grounded&&VerticalSpeed<=0;
            if(landed)Land();else if(JumpsUsed==0)JumpsUsed=1; // Walking off an edge leaves one air jump.
            if(landed)edgeGrace=EdgeGraceTime;else edgeGrace=Math.Max(0,edgeGrace-dt);
            if(jumpPressed)jumpBuffer=JumpBufferTime;else jumpBuffer=Math.Max(0,jumpBuffer-dt);
            bool issued=jumpBuffer>0&&JumpsUsed<2;
            if(issued)
            {
                // An edge jump is still the first jump; never restore an already spent air jump.
                if(edgeGrace>0&&JumpsUsed==1)JumpsUsed=0;
                JumpsUsed++;VerticalSpeed=JumpImpulse;edgeGrace=jumpBuffer=0;
            }
            VerticalSpeed=Math.Max(-TerminalSpeed,VerticalSpeed-Gravity*dt);return issued;
        }
    }
}
