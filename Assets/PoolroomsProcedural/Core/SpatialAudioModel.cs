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
                float x=pool.Contains(listener.X,listener.Z)?pool.CenterX:Math.Max(pool.CenterX-pool.Width*.5f,Math.Min(pool.CenterX+pool.Width*.5f,listener.X));
                float z=pool.Contains(listener.X,listener.Z)?pool.CenterZ:Math.Max(pool.CenterZ-pool.Depth*.5f,Math.Min(pool.CenterZ+pool.Depth*.5f,listener.Z));
                var candidate=new V3(x,pool.Level+.12f,z);float d=V3.Dot(candidate-listener,candidate-listener);
                if(d>=closest)continue;selected=candidate;closest=d;found=true;
            }
            return selected;
        }
    }
}
