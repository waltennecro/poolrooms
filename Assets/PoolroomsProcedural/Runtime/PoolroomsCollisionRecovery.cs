using UnityEngine;

namespace Poolrooms
{
    public static class PoolroomsCollisionRecovery
    {
        // A disabled primitive is usable by ComputePenetration even against non-convex architecture.
        // Only actual intersections move the player; clear air does not trigger a spawn/ground snap.
        public static bool TryResolve(CharacterController body,out Vector3 position)
        {
            position=body.transform.position;Vector3 start=position;
            Physics.SyncTransforms();
            var proxyObject=new GameObject("Temporary local collision probe");proxyObject.SetActive(false);
            proxyObject.transform.SetParent(body.transform,false);
            var proxy=proxyObject.AddComponent<CapsuleCollider>();proxy.enabled=false;proxy.direction=1;
            float inset=Mathf.Max(.005f,body.skinWidth*.5f);
            proxy.center=body.center;proxy.radius=Mathf.Max(.01f,body.radius-inset);
            proxy.height=Mathf.Max(proxy.radius*2,body.height-inset*2);
            proxyObject.SetActive(true);
            var hits=new Collider[64];Vector3 scale=body.transform.lossyScale;
            float radius=proxy.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            float segment=Mathf.Max(0,proxy.height*Mathf.Abs(scale.y)*.5f-radius);
            Vector3 centerOffset=body.transform.TransformVector(body.center),up=body.transform.up;
            try
            {
                for(int attempt=0;attempt<16;attempt++)
                {
                    Vector3 center=position+centerOffset;
                    int count=Physics.OverlapCapsuleNonAlloc(center-up*segment,center+up*segment,radius,hits,~0,QueryTriggerInteraction.Ignore);
                    if(count==hits.Length)return false; // Incomplete query must not claim the space is clear.
                    bool overlap=false;
                    for(int i=0;i<count;i++)
                    {
                        var hit=hits[i];if(hit==null||hit.transform.IsChildOf(body.transform))continue;
                        Vector3 direction;float distance;
                        if(!Physics.ComputePenetration(proxy,position,body.transform.rotation,hit,hit.transform.position,hit.transform.rotation,out direction,out distance)||distance<=.001f)continue;
                        position+=direction*(distance+.003f);overlap=true;
                        if((position-start).sqrMagnitude>9)return false;
                        break; // Requery after each correction; don't use stale overlapping pairs.
                    }
                    if(!overlap)return true;
                }
                return false;
            }
            finally
            {
                proxyObject.SetActive(false);
                if(Application.isPlaying)Object.Destroy(proxyObject);else Object.DestroyImmediate(proxyObject);
            }
        }
    }
}
