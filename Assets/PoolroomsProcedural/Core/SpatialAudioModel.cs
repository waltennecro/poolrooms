using System;

namespace Poolrooms.Core
{
    public readonly struct OccludedSound
    {
        public readonly float Gain,Cutoff;
        public OccludedSound(float gain,float cutoff){Gain=gain;Cutoff=cutoff;}
    }
    public static class SpatialAudioModel
    {
        // Low-pass transmission through an intervening wall, never a binary mute.
        // Smooth in seconds so slow frames do not change the acoustic time constant.
        public static OccludedSound Step(OccludedSound previous,bool blocked,bool submerged,float dt)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return previous;
            float targetGain=submerged?.42f:blocked?.3f:1,targetCutoff=submerged?900:blocked?2100:18000;
            float weight=1-(float)Math.Exp(-Math.Min(dt,1)*5);
            return new OccludedSound(previous.Gain+(targetGain-previous.Gain)*weight,previous.Cutoff+(targetCutoff-previous.Cutoff)*weight);
        }
        public static V3 WaterAnchor(WaterRegion[] regions,V3 listener,out bool found)
        {
            found=false;float closest=float.PositiveInfinity;var selected=listener;
            if(regions==null)return selected;
            foreach(var pool in regions)
            {
                if(pool.Width<=0||pool.Depth<=0||pool.Level<=pool.Bottom)continue;
                // Anchor to a pool, not the camera. Inside a pool keep its centre;
                // outside use the closest edge to avoid a point source far across a hall.
                float x=pool.CenterX,z=pool.CenterZ;
                if(!pool.Contains(listener.X,listener.Z))
                {
                    float radius=Math.Max(0,Math.Min(pool.Radius,Math.Min(pool.Width,pool.Depth)*.5f));
                    // Project onto the rounded rectangle (inner box plus radius),
                    // not its bounding box, whose dry corners are outside the pool.
                    x=Math.Max(pool.CenterX-pool.Width*.5f+radius,Math.Min(pool.CenterX+pool.Width*.5f-radius,listener.X));
                    z=Math.Max(pool.CenterZ-pool.Depth*.5f+radius,Math.Min(pool.CenterZ+pool.Depth*.5f-radius,listener.Z));
                    float dx=listener.X-x,dz=listener.Z-z,length=(float)Math.Sqrt(dx*dx+dz*dz);
                    if(length>0){x+=dx*radius/length;z+=dz*radius/length;}
                }
                var candidate=new V3(x,pool.Level+.12f,z);float d=V3.Dot(candidate-listener,candidate-listener);
                if(d>=closest)continue;selected=candidate;closest=d;found=true;
            }
            return selected;
        }
    }
}
