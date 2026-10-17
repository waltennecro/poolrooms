using System;
using System.Collections.Generic;
using UnityEngine;
using Poolrooms.Core;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsChunk : MonoBehaviour
    {
        [Serializable] public struct SourceProp
        {
            public int slot,source;public Vector3 bottom;public float yaw;
            public SourceProp(SourceFurniturePlacement p){slot=p.Slot;source=p.Source;bottom=new Vector3(p.Bottom.X,p.Bottom.Y,p.Bottom.Z);yaw=p.Yaw;}
            public SourceFurniturePlacement Plan(){return new SourceFurniturePlacement(slot,source,new V3(bottom.x,bottom.y,bottom.z),yaw);}
        }
        [HideInInspector] public int roomId,cellX,cellZ;
        [HideInInspector] public WaterRegion[] waterRegions;
        [HideInInspector] public GameObject[] caps=new GameObject[4];
        [HideInInspector] public GameObject[] links=new GameObject[2];
        [HideInInspector] public MeshRenderer water;
        [HideInInspector] public List<Mesh> ownedMeshes=new List<Mesh>();
        [HideInInspector] public List<Material> ownedMaterials=new List<Material>();
        [HideInInspector] public bool meshesAreAssets;
        [HideInInspector] public bool connectedMall;
        [HideInInspector] public SourceProp[] sourceProps=new SourceProp[0];
        [NonSerialized] public PreparedTransition[] preparedTransitions=new PreparedTransition[0];
        [NonSerialized] public readonly Dictionary<int,Material> paletteMaterials=new Dictionary<int,Material>();
        private void OnDestroy()
        {
            if(meshesAreAssets)return;
            foreach(Mesh mesh in ownedMeshes) if(mesh!=null)
            {
                if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);
            }
            ownedMeshes.Clear();
            foreach(Material material in ownedMaterials)if(material!=null)
            {if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
            ownedMaterials.Clear();
        }
    }
}
