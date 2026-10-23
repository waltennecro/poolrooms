using UnityEngine;

namespace Poolrooms
{
    // Start runs after the world's existing source-furniture attachment loop.
    // Reuse its proven support monitor rather than simulate all loose props.
    public sealed class PoolroomsForest39Support : MonoBehaviour
    {
        public PoolroomsWorld world;

        void Start()
        {
            var objects = GetComponentsInChildren<PoolroomsPickup>();
            PoolroomsPickup table = null;
            foreach (var pickup in objects)
                if (pickup.SpawnKey!=null&&pickup.SpawnKey.EndsWith(":390000",System.StringComparison.Ordinal)) table = pickup;
            if (table == null)
            {
                foreach(var pickup in objects)if(Belongs(pickup))pickup.Release(transform,pickup.transform.position,Vector3.zero);
                return;
            }
            var support = gameObject.AddComponent<PoolroomsProvisionsSupport>();
            support.Configure(table, world);
            foreach (var pickup in objects)
            {
                if (pickup == table || pickup.InventoryKey == null) continue;
                if (Belongs(pickup)) support.Bind(pickup);
            }
        }
        static bool Belongs(PoolroomsPickup pickup)
        {
            if(pickup==null||pickup.SpawnKey==null||pickup.InventoryKey==null)return false;
            return pickup.SpawnKey.EndsWith(":390001",System.StringComparison.Ordinal)||
                pickup.SpawnKey.StartsWith("forest39:",System.StringComparison.Ordinal)&&
                pickup.InventoryKey.StartsWith("food:",System.StringComparison.Ordinal);
        }
    }
}
