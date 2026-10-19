using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Identical catalogue items share meshes/materials while their interaction,
    // collider, taken state and rigidbody remain independent. Retained items keep a
    // reference through chunk unloading; the last item releases the native assets.
    public static class PoolroomsPickupVisualCache
    {
        internal sealed class Entry
        {
            public long Key;public int References;public GameObject Root;
            public Mesh Mesh;public Material[] Materials;
        }
        private static readonly Dictionary<long,Entry> entries=new Dictionary<long,Entry>();
        public static int EntryCount {get{return entries.Count;}}
        public static GameObject Create(int catalogId,Transform parent,PoolroomsProfile profile)
        {
            MallItemCatalog.Get(catalogId);
            if(profile==null)throw new ArgumentNullException(nameof(profile));
            long key=((long)profile.GetInstanceID()<<32)|(uint)catalogId;
            if(!entries.TryGetValue(key,out var entry)||entry.Root==null)
            {
                var root=new GameObject("Shared catalogue visual "+catalogId){hideFlags=HideFlags.HideAndDontSave};root.SetActive(false);
                if(Application.isPlaying)UnityEngine.Object.DontDestroyOnLoad(root);
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();
                    var visual=MeshFactory.Create("Shared item mesh",MallItemCatalog.Model(catalogId),root.transform,profile,owner,false);
                    entry=new Entry{Key=key,Root=root,Mesh=visual.GetComponent<MeshFilter>().sharedMesh,Materials=visual.GetComponent<MeshRenderer>().sharedMaterials};
                    entries[key]=entry;
                }
                catch{Destroy(root);throw;}
            }
            var instance=new GameObject("Articulo");instance.transform.SetParent(parent,false);
            instance.AddComponent<MeshFilter>().sharedMesh=entry.Mesh;
            var renderer=instance.AddComponent<MeshRenderer>();renderer.sharedMaterials=entry.Materials;
            renderer.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            renderer.motionVectorGenerationMode=MotionVectorGenerationMode.Object;
            entry.References++;instance.AddComponent<PoolroomsPickupVisualLease>().Entry=entry;return instance;
        }
        internal static void Release(Entry entry)
        {
            if(entry==null||--entry.References>0)return;
            if(entries.TryGetValue(entry.Key,out var current)&&ReferenceEquals(current,entry))entries.Remove(entry.Key);
            if(entry.Root!=null)Destroy(entry.Root);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach(var entry in entries.Values)if(entry.Root!=null)Destroy(entry.Root);
            entries.Clear();
        }
        private static void Destroy(UnityEngine.Object obj)
        {if(Application.isPlaying)UnityEngine.Object.Destroy(obj);else UnityEngine.Object.DestroyImmediate(obj);}
    }
    [DisallowMultipleComponent]
    internal sealed class PoolroomsPickupVisualLease : MonoBehaviour
    {
        internal PoolroomsPickupVisualCache.Entry Entry;
        private void OnDestroy(){PoolroomsPickupVisualCache.Release(Entry);Entry=null;}
    }
}
