using System.Collections;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Optional, bounded population after structural streaming. No Update loop,
    // survival state, separate HUD, runtime mesh generation or scene-wide scan.
    [DisallowMultipleComponent]
    public sealed class PoolroomsProvisions : MonoBehaviour
    {
        private PoolroomsWorld world;
        private ProvisionCandidate[] candidates;
        private PoolroomsCarry carry;
        private string domain;
        private int seed,x,z;
        private bool city;
        private float clearHalf;
        private WaterRegion[] water;
        private readonly RaycastHit[] rays=new RaycastHit[16];
        private readonly Collider[] overlaps=new Collider[24];
        public int CreatedItems {get;private set;}
        public int RejectedSites {get;private set;}
        public static void Attach(PoolroomsWorld world,Transform parent,string domain,int seed,int x,int z,float width,float depth,float floor=0,bool city=false,V3 center=default,WaterRegion[] waters=null,float clearHalf=0)
        {
            if(!Application.isPlaying||world==null||world.CurrentLayout==null||!world.CurrentLayout.Config.EnableProvisions||parent==null||parent.GetComponent<PoolroomsProvisions>()!=null)return;
            var candidates=ProvisionDistribution.Candidates(seed,x,z,width,depth,floor,city);
            if(candidates.Length==0)return;
            for(int i=0;i<candidates.Length;i++)candidates[i]=new ProvisionCandidate(candidates[i].Slot,candidates[i].Bottom+center);
            var component=parent.gameObject.AddComponent<PoolroomsProvisions>();component.world=world;component.domain=domain;component.seed=seed;component.x=x;component.z=z;component.city=city;component.candidates=candidates;component.water=waters;component.clearHalf=clearHalf;
            var actor=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;component.carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
        }
        private IEnumerator Start()
        {
            // Start runs only after inactive staged houses have been published.
            yield return null;
            if(world==null||candidates==null)yield break;
            Physics.SyncTransforms();
            for(int i=0;i<candidates.Length;i++)
            {
                while(world!=null&&world.FrontendSuspended)yield return null;
                if(world==null||!world.BuildColliders)yield break;
                var p=candidates[i];float yaw=city?90:0;var size=SourceFurniture.Dimensions(23);
                if(!TrySite(p.Bottom,yaw,size,out var bottom)){RejectedSites++;if(i%2==1)yield return null;continue;}
                string tableKey=ProvisionDistribution.Key(domain,seed,x,z,p.Slot,0);
                if(carry!=null&&carry.WasTaken(tableKey))yield break;
                var table=PoolroomsPickup.CreateSource(tableKey,"M_IndoorTable",bottom,transform,SourceFurniture.Longest(23));
                if(table==null)yield break;
                table.transform.localRotation=Quaternion.Euler(0,yaw,0);
                var support=gameObject.AddComponent<PoolroomsProvisionsSupport>();support.Configure(table,world);
                for(int item=0;item<ProvisionDistribution.ItemsPerStation;item++)
                {
                    if(table==null||table.transform.parent!=transform)yield break;
                    string key=ProvisionDistribution.Key(domain,seed,x,z,p.Slot,item+1);
                    if(carry!=null&&carry.WasTaken(key))continue;
                    int id=ProvisionDistribution.Item(seed,x,z,p.Slot,item);V3 local=ProvisionDistribution.OnTable(item);
                    Vector3 point=bottom+Quaternion.Euler(0,yaw,0)*new Vector3(local.X,local.Y,local.Z);
                    var food=PoolroomsPickup.CreateFood(key,id,point,transform,world.profile);
                    if(food!=null){food.transform.localRotation=Quaternion.Euler(0,yaw,0);support.Bind(food);CreatedItems++;}
                    yield return null;
                }
                // One sharing table / four real pickups per room or external sector.
                // This remains independent from render distance and structural budgets.
                yield break;
            }
        }
        private bool FloorAt(Vector3 local,out Vector3 worldPoint)
        {
            worldPoint=default;Vector3 origin=transform.TransformPoint(local+Vector3.up*.4f);
            int count=Physics.RaycastNonAlloc(origin,-transform.up,rays,.65f,~0,QueryTriggerInteraction.Ignore);
            if(count==rays.Length)return false;float nearest=float.MaxValue;int index=-1;
            for(int i=0;i<count;i++)if(rays[i].collider!=null&&rays[i].distance<nearest){nearest=rays[i].distance;index=i;}
            if(index<0||Vector3.Dot(rays[index].normal,transform.up)<.98f||rays[index].collider.GetComponentInParent<PoolroomsPickup>()!=null)return false;
            var chunk=rays[index].collider.GetComponentInParent<PoolroomsChunk>();
            // Never use another room, a roof above this floor, or the player's body as support.
            bool sharedMall=domain.StartsWith("mall-",System.StringComparison.Ordinal)||domain.StartsWith("legacy-mall-",System.StringComparison.Ordinal);
            if(chunk==null||!rays[index].collider.transform.IsChildOf(sharedMall?transform.parent:transform))return false;
            worldPoint=rays[index].point;return true;
        }
        private bool ClearBox(Vector3 center,Vector3 half,Quaternion rotation)
        {
            int count=Physics.OverlapBoxNonAlloc(transform.TransformPoint(center),half,overlaps,transform.rotation*rotation,~0,QueryTriggerInteraction.Ignore);
            return count==0;
        }
        private bool TrySite(V3 candidate,float yaw,V3 size,out Vector3 bottom)
        {
            bottom=new Vector3(candidate.X,candidate.Y,candidate.Z);Quaternion rotation=Quaternion.Euler(0,yaw,0);
            if(!city&&clearHalf>0&&(Mathf.Abs(candidate.X)-size.X*.5f<=clearHalf||Mathf.Abs(candidate.Z)-size.Z*.5f<=clearHalf))return false;
            if(!FloorAt(bottom,out var floor))return false;bottom=transform.InverseTransformPoint(floor)+Vector3.up*.003f;
            if(water!=null)foreach(var region in water)
            {if(region.Contains(bottom.x,bottom.z)&&bottom.y<=region.Level+.03f)return false;}
            // Nine support samples reject holes, slopes and stair edges.
            for(int a=-1;a<=1;a++)for(int b=-1;b<=1;b++)
            {
                Vector3 q=bottom+rotation*new Vector3(a*size.X*.5f,0,b*size.Z*.5f);
                if(!FloorAt(q,out var contact)||Mathf.Abs(transform.InverseTransformPoint(contact).y-bottom.y)>.025f)return false;
            }
            if(!ClearBox(bottom+Vector3.up*1.02f,new Vector3(size.X*.5f+.04f,1,size.Z*.5f+.04f),rotation))return false;
            // An adjacent standing bay has a real floor and 1.9m of head clearance.
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 approach=bottom+rotation*new Vector3(0,0,side*(size.Z*.5f+.5f));
                if(!FloorAt(approach,out var contact)||Mathf.Abs(transform.InverseTransformPoint(contact).y-bottom.y)>.025f)continue;
                if(ClearBox(approach+Vector3.up*.98f,new Vector3(.28f,.95f,.28f),Quaternion.identity))return true;
            }
            return false;
        }
    }
}
