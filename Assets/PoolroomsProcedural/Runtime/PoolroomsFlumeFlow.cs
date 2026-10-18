using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Five shared Lit materials per world, not an Update or a material per water strip.
    [DisallowMultipleComponent]
    public sealed class PoolroomsFlumeFlow : MonoBehaviour
    {
        private static readonly string[] OffsetProperties={"_BaseColorMap","_BaseMap","_MainTex","_NormalMap","_BumpMap","_MaskMap","_MetallicGlossMap"};
        private readonly Material[] materials=new Material[5];
        private readonly float[] phases=new float[5];
        private PoolroomsWorld world;
        private PoolroomsExplorer explorer;
        private Transform boundViewer;
        public static PoolroomsFlumeFlow For(PoolroomsWorld owner)
        {var component=owner.GetComponent<PoolroomsFlumeFlow>();if(component==null)component=owner.gameObject.AddComponent<PoolroomsFlumeFlow>();component.world=owner;return component;}
        public Material Resolve(int profile)
        {
            profile=Mathf.Clamp(profile,0,4);
            if(materials[profile]==null)materials[profile]=PoolroomsBiomeMaterialFactory.Create(SurfaceTheme.FlumeFlow,Surface.Slide);
            return materials[profile];
        }
        private void Update()
        {
            if(world==null||!world.isActiveAndEnabled)return;
            if(boundViewer!=world.viewer){boundViewer=world.viewer;explorer=boundViewer!=null?boundViewer.GetComponentInParent<PoolroomsExplorer>():null;}
            Advance(Time.deltaTime,explorer!=null&&explorer.IsPaused);
        }
        public void Advance(float dt,bool paused)
        {
            for(int i=0;i<materials.Length;i++)if(materials[i]!=null)
            {
                float next=AquaticSlideCatalog.AdvanceFlow(phases[i],dt,i,paused);if(next==phases[i])continue;phases[i]=next;
                var offset=new Vector2(0,-next);var material=materials[i];
                foreach(string property in OffsetProperties)if(material.HasProperty(property))material.SetTextureOffset(property,offset);
            }
        }
        private void OnDestroy(){foreach(var material in materials)if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }
}
