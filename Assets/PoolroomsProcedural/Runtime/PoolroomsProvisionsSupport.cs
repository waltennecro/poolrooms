using System.Collections.Generic;
using UnityEngine;

namespace Poolrooms
{
    // The monitor belongs to the room, not the removable table. Its food objects
    // stay independent when the table is carried/stored and begin real physics.
    public sealed class PoolroomsProvisionsSupport : MonoBehaviour
    {
        private PoolroomsPickup table;
        private PoolroomsWorld world;
        private Vector3 position;
        private Quaternion rotation;
        private readonly List<PoolroomsPickup> children=new List<PoolroomsPickup>(4);
        private float checkAt;
        internal void Configure(PoolroomsPickup support,PoolroomsWorld world)
        {table=support;this.world=world;position=support.transform.localPosition;rotation=support.transform.localRotation;}
        internal void Bind(PoolroomsPickup child){if(child!=null&&children.Count<4)children.Add(child);}
        private void Update()
        {
            if(world==null||world.FrontendSuspended||Time.deltaTime<=0||Time.time<checkAt)return;
            checkAt=Time.time+.15f;
            if(table!=null&&table.transform.parent==transform&&(table.transform.localPosition-position).sqrMagnitude<.0004f&&Quaternion.Angle(rotation,table.transform.localRotation)<.5f)return;
            foreach(var child in children)if(child!=null&&child.transform.parent==transform)child.Release(transform,child.transform.position,Vector3.zero);
            children.Clear();enabled=false;
        }
    }
}
