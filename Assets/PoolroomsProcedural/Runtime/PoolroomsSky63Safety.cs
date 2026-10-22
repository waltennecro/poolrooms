using Poolrooms.Core;
using UnityEngine;
namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsSky63Safety : MonoBehaviour
    {
        private PoolroomsExplorer explorer;
        private void Awake(){explorer=GetComponent<PoolroomsExplorer>();}
        private void LateUpdate()
        {
            if(explorer==null||!explorer.IsReady||explorer.IsPaused||Time.deltaTime<=0||explorer.IsFlying||explorer.Vehicle!=null)return;
            var world=explorer.world;if(world==null||!world.isActiveAndEnabled||!world.ViewerReady||world.InsideMall)return;
            var room=world.RoomAtViewer();if(room?.Plan?.Quiet?.Sky63==null)return;
            var origin=world.RoomWorldPosition(room);
            if(Sky63Plan.Recover(transform.position.y-origin.y,false,false))explorer.Teleport(origin+Vector3.up*.08f);
        }
    }
}
