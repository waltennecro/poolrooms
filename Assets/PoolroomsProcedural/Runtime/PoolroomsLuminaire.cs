using UnityEngine;
namespace Poolrooms
{
    public sealed class PoolroomsLuminaire : MonoBehaviour
    {
        public float lumens=5000,width=2,depth=.4f,temperature=5000;
        [HideInInspector] public bool solarBeam,solarFill,poolroomSunlight,ambientSupport;
        [HideInInspector] public bool mallLocal;
        [HideInInspector] public float floorOffset,fullRange,shadowlessRange;
        [HideInInspector] public Light source;
    }
}
