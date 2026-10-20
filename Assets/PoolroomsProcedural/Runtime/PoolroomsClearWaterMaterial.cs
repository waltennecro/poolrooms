using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    // An inexpensive Lit fallback remains transparent when native HDRP Water is off.
    // It receives ordinary lights and reflection probes; no extra refraction capture or fake sun is added.
    public static class PoolroomsClearWaterMaterial
    {
        public static Material Resolve(PoolroomsProfile profile,PoolroomsChunk owner)
        {
            const int key=32768;Material result;
            if(owner.paletteMaterials.TryGetValue(key,out result))return result;
            var pipeline=GraphicsSettings.currentRenderPipeline;
            string pipelineName=pipeline!=null?pipeline.GetType().Name:"";
            string shaderName=pipelineName.Contains("HDRenderPipeline")?"HDRP/Lit":pipelineName.Contains("Universal")?"Universal Render Pipeline/Lit":"Standard";
            var shader=profile.waterPreview!=null?profile.waterPreview.shader:Shader.Find(shaderName)??Shader.Find("Standard");
            if(shader==null)return null;
            result=profile.waterPreview!=null?new Material(profile.waterPreview):new Material(shader);
            result.name="Crystal clear water / Lit fallback";result.enableInstancing=true;
            var tint=new Color(.97f,.995f,.991f,PoolWaterModel.ClearFallbackOpacity);
            // The preview may be an old opaque or emissive material. Start with a clean optical layer.
            foreach(string property in new[]{"_BaseColorMap","_BaseMap","_MainTex"})if(result.HasProperty(property))result.SetTexture(property,Texture2D.whiteTexture);
            foreach(string property in new[]{"_MaskMap","_MetallicGlossMap","_EmissionMap","_EmissiveColorMap","_DetailMap","_HeightMap","_SpecularColorMap","_SpecGlossMap"})if(result.HasProperty(property))result.SetTexture(property,null);
            Color(result,"_EmissiveColor",UnityEngine.Color.black);Color(result,"_EmissionColor",UnityEngine.Color.black);
            Float(result,"_UseEmissiveIntensity",0);Float(result,"_EmissiveIntensity",0);
            foreach(string keyword in new[]{"_EMISSION","_ALPHATEST_ON","_MASKMAP","_METALLICGLOSSMAP","_SPECGLOSSMAP","_EMISSIVE_COLOR_MAP","_DETAIL_MAP","_HEIGHTMAP","_SPECULARCOLORMAP"})result.DisableKeyword(keyword);
            Color(result,"_BaseColor",tint);Color(result,"_Color",tint);
            Float(result,"_SurfaceType",1);Float(result,"_Surface",1);Float(result,"_BlendMode",0);Float(result,"_Mode",3);
            Float(result,"_SrcBlend",(float)BlendMode.One);Float(result,"_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            Float(result,"_AlphaSrcBlend",(float)BlendMode.One);Float(result,"_AlphaDstBlend",(float)BlendMode.OneMinusSrcAlpha);
            Float(result,"_AlphaCutoffEnable",0);Float(result,"_AlphaClip",0);Float(result,"_AlphaToMask",0);
            Float(result,"_TransparentDepthPrepassEnable",0);Float(result,"_TransparentDepthPostpassEnable",0);
            Float(result,"_RefractionModel",0); // Native Water handles depth refraction; the fallback stays inexpensive.
            Float(result,"_DoubleSidedEnable",1);Float(result,"_DoubleSidedNormalMode",1);Float(result,"_CullMode",0);Float(result,"_CullModeForward",0);Float(result,"_Cull",0);
            Float(result,"_ZWrite",0);Float(result,"_TransparentZWrite",0);Float(result,"_Metallic",0);
            Float(result,"_Smoothness",.965f);Float(result,"_Glossiness",.965f);Float(result,"_Ior",1.333f);
            Float(result,"_EnableBlendModePreserveSpecularLighting",1);Float(result,"_ReceivesSSRTransparent",1);
            Float(result,"_NormalScale",PoolWaterModel.ClearNormalStrength);Float(result,"_BumpScale",PoolWaterModel.ClearNormalStrength);
            var normal=Resources.Load<Texture2D>("PoolroomsBiomeTextures/CalmWater_Normal");
            if(normal!=null)
            {
                foreach(string property in new[]{"_NormalMap","_BumpMap"})if(result.HasProperty(property))result.SetTexture(property,normal);
                result.EnableKeyword("_NORMALMAP");result.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            }
            result.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");result.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            result.SetOverrideTag("RenderType","Transparent");result.renderQueue=3000;result.SetShaderPassEnabled("ShadowCaster",false);
            PoolroomsBiomeMaterialFactory.Validate(result);
            // Reassert pass state after HDRP validation; a clear pool must not hide its bed in the depth buffer.
            result.SetShaderPassEnabled("ShadowCaster",false);result.SetShaderPassEnabled("TransparentDepthPrepass",false);result.SetShaderPassEnabled("TransparentDepthPostpass",false);
            owner.paletteMaterials.Add(key,result);owner.ownedMaterials.Add(result);return result;
        }
        static void Float(Material material,string property,float value){if(material.HasProperty(property))material.SetFloat(property,value);}
        static void Color(Material material,string property,UnityEngine.Color value){if(material.HasProperty(property))material.SetColor(property,value);}
    }
}
