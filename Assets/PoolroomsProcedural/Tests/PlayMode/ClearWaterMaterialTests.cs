#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms.Tests
{
    public sealed class ClearWaterMaterialTests
    {
        private static Shader ActiveLitShader()
        {
            var asset=GraphicsSettings.currentRenderPipeline;string name=asset!=null?asset.GetType().Name:"";
            return Shader.Find(name.Contains("HDRenderPipeline")?"HDRP/Lit":name.Contains("Universal")?"Universal Render Pipeline/Lit":"Standard");
        }
        [Test]
        public void DirtyOpaquePreviewBecomesClearWithoutMutatingTheProfile()
        {
            var shader=ActiveLitShader();if(shader==null)Assert.Ignore("Needs a Lit shader available to the active rendering pipeline.");
            GameObject root=null;PoolroomsProfile profile=null;Material preview=null;
            try
            {
                root=new GameObject("Clear water material regression");var chunk=root.AddComponent<PoolroomsChunk>();profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
                preview=new Material(shader);profile.waterPreview=preview;
                string colour=preview.HasProperty("_BaseColor")?"_BaseColor":"_Color";
                preview.SetColor(colour,new Color(.1f,.3f,.2f,1));
                if(preview.HasProperty("_EmissionColor"))preview.SetColor("_EmissionColor",Color.white*8);
                if(preview.HasProperty("_EmissiveColor"))preview.SetColor("_EmissiveColor",Color.white*8);
                if(preview.HasProperty("_ZWrite"))preview.SetFloat("_ZWrite",1);
                preview.EnableKeyword("_ALPHATEST_ON");preview.EnableKeyword("_EMISSION");
                var material=PoolroomsClearWaterMaterial.Resolve(profile,chunk);
                Assert.That(material,Is.Not.Null);Assert.That(material,Is.Not.SameAs(preview));Assert.That(PoolroomsClearWaterMaterial.Resolve(profile,chunk),Is.SameAs(material));
                Assert.That(preview.GetColor(colour).a,Is.EqualTo(1));Assert.That(material.GetColor(colour).a,Is.EqualTo(PoolWaterModel.ClearFallbackOpacity).Within(.0001f));
                Assert.That(material.GetShaderPassEnabled("ShadowCaster"),Is.False);
                Assert.That(material.GetShaderPassEnabled("TransparentDepthPostpass"),Is.False);
                if(material.HasProperty("_ZWrite"))Assert.That(material.GetFloat("_ZWrite"),Is.Zero);
                if(material.HasProperty("_Metallic"))Assert.That(material.GetFloat("_Metallic"),Is.Zero);
                if(material.HasProperty("_EmissiveColor"))Assert.That(material.GetColor("_EmissiveColor").maxColorComponent,Is.Zero);
                if(material.HasProperty("_EmissionColor"))Assert.That(material.GetColor("_EmissionColor").maxColorComponent,Is.Zero);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"),Is.False);
                Assert.That(chunk.ownedMaterials.Count,Is.EqualTo(1));
            }
            finally{if(root!=null)Object.DestroyImmediate(root);if(preview!=null)Object.DestroyImmediate(preview);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void MissingPreviewUsesTheActivePipelineAndStillProducesClearWater()
        {
            var shader=ActiveLitShader();if(shader==null)Assert.Ignore("No supported Lit shader available.");
            GameObject root=null;PoolroomsProfile profile=null;
            try
            {
                root=new GameObject("Missing preview regression");var chunk=root.AddComponent<PoolroomsChunk>();profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.waterPreview=null;
                var material=PoolroomsClearWaterMaterial.Resolve(profile,chunk);Assert.That(material,Is.Not.Null);Assert.That(material.shader,Is.EqualTo(shader));Assert.That(profile.waterPreview,Is.Null);
                var colour=material.GetColor(material.HasProperty("_BaseColor")?"_BaseColor":"_Color");Assert.That(colour.a,Is.LessThan(.1f));Assert.That(colour.a,Is.GreaterThan(0));
            }
            finally{if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
    }
}
#endif
