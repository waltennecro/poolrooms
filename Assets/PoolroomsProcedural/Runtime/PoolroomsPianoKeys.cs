using UnityEngine;
namespace Poolrooms
{
    public sealed class PoolroomsPianoKeys : MonoBehaviour
    {
        public readonly Transform[] keys=new Transform[8];
        private readonly float[] heldUntil=new float[8],depth=new float[8];
        public void Press(int note){if(note<0||note>=keys.Length)return;heldUntil[note]=Time.time+.16f;enabled=true;}
        private void Update()
        {
            if(Time.deltaTime<=0)return;bool moving=false;
            for(int i=0;i<keys.Length;i++)if(keys[i]!=null)
            {
                float target=Time.time<heldUntil[i]?.008f:0;depth[i]=Mathf.MoveTowards(depth[i],target,Time.deltaTime*.09f);
                var position=keys[i].localPosition;position.y=.905f-depth[i];keys[i].localPosition=position;
                if(target>0||depth[i]>0)moving=true;
            }
            if(!moving)enabled=false;
        }
    }
}
