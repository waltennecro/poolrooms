using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // One registry, ticked by each world's existing light-budget cadence. No Update per fitting.
    public sealed class PoolroomsArchitecturalDetail : MonoBehaviour
    {
        private static readonly List<PoolroomsArchitecturalDetail> Live=new List<PoolroomsArchitecturalDetail>();
        private PoolroomsWorld world;
        private Renderer first,second;
        private void OnEnable(){Live.Add(this);}
        private void OnDisable(){Live.Remove(this);}
        public static void Attach(DressingGeometry geometry,Transform parent,PoolroomsProfile profile,PoolroomsChunk owner,PoolroomsWorld world,float offsetY=0)
        {
            if(geometry==null||geometry.Anchors.Count==0)return;
            var root=new GameObject("Architectural fittings");root.transform.SetParent(parent,false);root.transform.localPosition=Vector3.up*offsetY;
            var component=root.AddComponent<PoolroomsArchitecturalDetail>();component.world=world;
            var a=MeshFactory.Create("Fittings 1–12",geometry.First,root.transform,profile,owner,false);
            var b=MeshFactory.Create("Fittings 13–24",geometry.Second,root.transform,profile,owner,false);
            component.first=a!=null?a.GetComponent<Renderer>():null;component.second=b!=null?b.GetComponent<Renderer>():null;
            component.Show(0); // Initial visibility is selected with the same distance/quality policy.
            var settings=world!=null&&world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsGameSettings>():null;
            var preferences=settings!=null?settings.Preferences:null;
            component.Select(world!=null&&world.viewer!=null?world.viewer.position:root.transform.position,preferences!=null?preferences.dressingDensity:2,preferences!=null&&preferences.graphicsQuality==0);
        }
        private void Show(int density)
        {
            if(first!=null&&first.enabled!=(density>0))first.enabled=density>0;
            if(second!=null&&second.enabled!=(density>1))second.enabled=density>1;
        }
        private void Select(Vector3 viewer,int density,bool economy)
        {
            float distance=economy?22:48;float sq=distance*distance;
            bool near=first!=null&&first.bounds.SqrDistance(viewer)<sq||second!=null&&second.bounds.SqrDistance(viewer)<sq;
            Show(near?(economy?Mathf.Min(1,density):density):0);
        }
        public static void Tick(PoolroomsWorld world)
        {
            if(world==null||world.viewer==null)return;
            var settings=world.viewer.GetComponentInParent<PoolroomsGameSettings>();var preferences=settings!=null?settings.Preferences:null;
            int density=preferences!=null?preferences.dressingDensity:2;bool economy=preferences!=null&&preferences.graphicsQuality==0;
            for(int i=Live.Count-1;i>=0;i--){var entry=Live[i];if(entry!=null&&entry.world==world)entry.Select(world.viewer.position,density,economy);}
        }
    }
}
