using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    // An inexpensive Lit fallback remains transparent when native HDRP Water is off.
    // It receives ordinary lights/SSR/probes; no camera, extra RT pass or fake sun is added.
    public static class PoolroomsClearWaterMaterial
    {
        public static Material Resolve(PoolroomsProfile profile,PoolroomsChunk owner)
        {
            const int key=32768;Material result;
            if(owner.paletteMaterials.TryGetValue(key,out result))return result;
            if(profile.waterPreview==null)return null;
            result=new Material(profile.waterPreview){name="Clear calm water / Lit fallback",enableInstancing=true};
            var tint=new Color(.78f,.94f,.93f,.16f);
            Color(result,"_BaseColor",tint);Color(result,"_Color",tint);
            Float(result,"_SurfaceType",1);Float(result,"_Surface",1);Float(result,"_BlendMode",0);Float(result,"_Mode",3);
            Float(result,"_SrcBlend",(float)BlendMode.One);Float(result,"_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            Float(result,"_AlphaSrcBlend",(float)BlendMode.One);Float(result,"_AlphaDstBlend",(float)BlendMode.OneMinusSrcAlpha);
            Float(result,"_ZWrite",0);Float(result,"_TransparentZWrite",0);Float(result,"_Metallic",0);
            Float(result,"_Smoothness",.965f);Float(result,"_Glossiness",.965f);Float(result,"_Ior",1.333f);
            Float(result,"_EnableBlendModePreserveSpecularLighting",1);Float(result,"_ReceivesSSRTransparent",1);
            Float(result,"_NormalScale",.18f);Float(result,"_BumpScale",.18f);
            var normal=Resources.Load<Texture2D>("PoolroomsBiomeTextures/CalmWater_Normal");
            if(normal!=null)
            {
                foreach(string property in new[]{"_NormalMap","_BumpMap"})if(result.HasProperty(property))result.SetTexture(property,normal);
                result.EnableKeyword("_NORMALMAP");result.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            }
            result.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");result.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            result.SetOverrideTag("RenderType","Transparent");result.renderQueue=3000;result.SetShaderPassEnabled("ShadowCaster",false);
            PoolroomsBiomeMaterialFactory.Validate(result);
            owner.paletteMaterials.Add(key,result);owner.ownedMaterials.Add(result);return result;
        }
        static void Float(Material material,string property,float value){if(material.HasProperty(property))material.SetFloat(property,value);}
        static void Color(Material material,string property,UnityEngine.Color value){if(material.HasProperty(property))material.SetColor(property,value);}
    }
}
