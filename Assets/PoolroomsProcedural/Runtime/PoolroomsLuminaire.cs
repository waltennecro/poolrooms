using UnityEngine;
namespace Poolrooms
{
    public sealed class PoolroomsLuminaire : MonoBehaviour
    {
        public bool userEnabled=true;
        public float lumens=5000,width=2,depth=.4f,temperature=5000;
        [HideInInspector] public bool solarBeam,solarFill,poolroomSunlight,mallSunlight,ambientSupport;
        [HideInInspector] public bool mallLocal;
        [HideInInspector] public float floorOffset,fullRange,shadowlessRange,storeyHeight;
        [HideInInspector] public Light source;
    }
}
