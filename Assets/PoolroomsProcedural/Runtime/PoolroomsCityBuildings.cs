using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // One imported exterior per eligible streamed block, sharing source meshes/materials.
    // Primary architecture owns collision, floors, furniture, windows and stairs.
    public static class PoolroomsCityBuildings
    {
        public static void Attach(City11Plan plan,Transform parent,LayoutConfig config)
        {
            if(!config.EnableImportedBuildings||parent.Find("Licensed building exteriors")!=null)return;
            Transform group=null;
            foreach(var building in plan.Buildings)
            {
                if(building.ExteriorSource==0)continue;
                if(group==null){group=new GameObject("Licensed building exteriors").transform;group.SetParent(parent,false);}
                string source=building.ExteriorSource==1?"B_BuildingCourtyard":"B_BuildingCorner";
                var root=City11Sources.Create(source,group,new Vector3(building.X,0,building.Z),12.92f,out var size,0,true);
                if(root==null){Debug.LogError("Missing licensed building source: "+source,parent);continue;}
                Vector3 facing=Vector3.zero;
                foreach(var part in root.GetComponentsInChildren<Transform>(true))
                {
                    if(!part.name.Contains("EntryOrientationMarker"))continue;
                    if(facing==Vector3.zero&&City11Sources.TryLocalBounds(part,out var bounds))
                        facing=Vector3.ProjectOnPlane(City11Sources.ModelPoint(part,root.transform,bounds.center),Vector3.up);
                    foreach(var renderer in part.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                }
                if(facing.sqrMagnitude<1)
                {
                    Debug.LogError("Missing native building orientation marker: "+source,parent);
                    if(Application.isPlaying)Object.Destroy(root);else Object.DestroyImmediate(root);continue;
                }
                root.transform.localRotation=Quaternion.FromToRotation(facing.normalized,building.Turn?Vector3.forward:Vector3.back);
                // The native source importer never creates a mesh collider. Window and
                // door clearance comes from the same City11 physical shell at all LODs.
            }
        }
    }
}
