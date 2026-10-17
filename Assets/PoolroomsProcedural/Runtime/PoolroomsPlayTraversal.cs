using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Physical traversal of an optional slide, without teleporting, locking controls or changing the camera.
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsPlayTraversal : MonoBehaviour
    {
        private PoolroomsExplorer explorer;
        private CharacterController body;
        private float speed;
        private void Awake(){explorer=GetComponent<PoolroomsExplorer>();body=GetComponent<CharacterController>();}
        private void LateUpdate()
        {
            if(explorer==null||!explorer.isActiveAndEnabled||!explorer.IsReady||explorer.IsPaused||explorer.InputCaptured||explorer.IsFlying||
                body==null||!body.enabled||!body.isGrounded||Time.deltaTime<=0||explorer.world==null)
            {speed=0;return;}
            Room room=explorer.world.RoomAtViewer();var plan=room!=null&&room.Plan!=null?room.Plan.SoftPlay:null;
            if(plan==null){speed=0;return;}
            Vector3 feet=transform.TransformPoint(body.center-Vector3.up*(body.height*.5f));
            Vector3 local=feet-explorer.world.RoomWorldPosition(room);V3 direction;
            if(!plan.Slide.Glide(new V3(local.x,local.y,local.z),body.radius,out direction)){speed=0;return;}
            speed=Mathf.MoveTowards(speed,6.5f,5*Time.deltaTime);
            // Use the controller's collision sweep; stepping away and jumping remain available.
            float distance=speed*Mathf.Min(Time.deltaTime,.05f);
            body.Move(new Vector3(direction.X,direction.Y,direction.Z)*distance);
        }
        private void OnDisable(){speed=0;}
    }
}
