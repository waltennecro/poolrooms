using UnityEngine;

namespace Poolrooms
{
    public abstract class PoolroomsVehicle : MonoBehaviour
    {
        public abstract bool IsDriving {get;}
        public abstract bool Enter(PoolroomsExplorer actor);
        public abstract bool TryExit();
        public abstract void DetachDriver(PoolroomsExplorer actor);
        public abstract void Control(float horizontal,float vertical,bool brake,float climb);
        public void Control(float horizontal,float vertical,bool brake){Control(horizontal,vertical,brake,0);}
    }
}
