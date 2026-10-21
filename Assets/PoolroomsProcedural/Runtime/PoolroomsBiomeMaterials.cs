using System;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    [CreateAssetMenu(menuName="Poolrooms/Independent biome materials",fileName="PoolroomsBiomeMaterials")]
    public sealed class PoolroomsBiomeMaterials : ScriptableObject
    {
        public Material[] surfaces=new Material[Enum.GetValues(typeof(SurfaceTheme)).Length*6];
        public Material Find(SurfaceTheme theme,int slot)
        {int index=(int)theme*6+slot;return surfaces!=null&&index<surfaces.Length?surfaces[index]:null;}
    }
    public static class PoolroomsBiomeMaterialFactory
    {
        private static PoolroomsBiomeMaterials shared;
        public const string TextureRoot="PoolroomsBiomeTextures/";
        public static Material Resolve(SurfaceTheme theme,int slot,PoolroomsProfile profile,PoolroomsChunk owner)
        {
            var library=profile.biomeMaterials;
            if(library==null){if(shared==null)shared=Resources.Load<PoolroomsBiomeMaterials>("PoolroomsBiomeMaterials");library=shared;}
            if(library!=null){Material asset=library.Find(theme,slot);if(asset!=null)return asset;}
            int key=4096+(int)theme*6+slot;Material cached;
            if(owner.paletteMaterials.TryGetValue(key,out cached))return cached;
            var material=Create(theme,(Surface)slot);owner.paletteMaterials.Add(key,material);owner.ownedMaterials.Add(material);return material;
        }
        public static Material SlideColor(int color,PoolroomsChunk owner)
        {
            int key=24576+color;Material material;if(owner.paletteMaterials.TryGetValue(key,out material))return material;
            material=Create(SurfaceTheme.AquaticPark,Surface.Slide,"AquaticFiberglass");
            var neutral=Resources.Load<Texture2D>(TextureRoot+"AquaticGelcoat_Albedo");
            foreach(string property in new[]{"_BaseColorMap","_BaseMap","_MainTex"})SetTexture(material,property,neutral);
            V3 rgb=AquaticSlideCatalog.Palette(color);var tint=new Color(rgb.X,rgb.Y,rgb.Z);SetColor(material,"_BaseColor",tint);SetColor(material,"_Color",tint);
            material.name="Aquatic gelcoat colour "+color;Validate(material);owner.paletteMaterials.Add(key,material);owner.ownedMaterials.Add(material);return material;
        }
        public static Material Weathered(int slot,PoolroomsChunk owner)
        {
            int key=12288+slot;Material material;if(owner.paletteMaterials.TryGetValue(key,out material))return material;
            material=Create(SurfaceTheme.Mall,(Surface)slot,slot==(int)Surface.Tile?"DustyStone":"WeatheredPaint");
            owner.paletteMaterials.Add(key,material);owner.ownedMaterials.Add(material);return material;
        }
        public static Material Create(SurfaceTheme theme,Surface slot,string textureOverride=null)
        {
            Shader shader=Shader.Find("HDRP/Lit");
            if(shader==null)shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null)shader=Shader.Find("Standard");
            if(shader==null)throw new InvalidOperationException("Poolrooms: no compatible Lit shader is available.");
            var m=new Material(shader){name="Liminal "+theme+" / "+slot,enableInstancing=true};
            // Specular anti-aliasing filters unresolved normals without changing texture,
            // mesh LOD, reflection sample counts or the streaming distance.
            if(m.HasProperty("_EnableGeometricSpecularAA"))
            {
                SetFloat(m,"_EnableGeometricSpecularAA",1);SetFloat(m,"_SpecularAAScreenSpaceVariance",.1f);SetFloat(m,"_SpecularAAThreshold",.2f);
                m.EnableKeyword("_ENABLE_GEOMETRIC_SPECULAR_AA");
            }
            Color color=Color.white;
            if(theme==SurfaceTheme.CityStreet&&slot==Surface.Tile)color=new Color(.19f,.2f,.21f);
            if(theme==SurfaceTheme.IndoorPlay)
            {V3 c=LiminalPalette.Color(slot,0);color=slot==Surface.Tile||slot==Surface.Plaster?new Color(.87f,.89f,.85f):new Color(c.X,c.Y,c.Z);}
            if(theme==SurfaceTheme.Hotel&&slot==Surface.Plaster)color=new Color(.95f,.91f,.84f);
            if(theme==SurfaceTheme.Orangerie&&slot==Surface.Metal)color=new Color(.34f,.42f,.36f);
            if(theme==SurfaceTheme.OrangerieFoliage)
            {
                // Leaf triangles supply both faces explicitly; no alpha overdraw or clipped shadows.
                SetFloat(m,"_Metallic",0);SetFloat(m,"_DoubleSidedEnable",0);
            }
            if(theme==SurfaceTheme.Conservatory&&slot==Surface.Slide)color=new Color(.16f,.31f,.12f);
            if(theme==SurfaceTheme.Conservatory&&slot==Surface.Metal)color=new Color(.3f,.36f,.3f);
            if(theme==SurfaceTheme.TransitGallery&&slot==Surface.Plaster)color=new Color(.91f,.89f,.82f);
            SetColor(m,"_BaseColor",color);SetColor(m,"_Color",color);
            string set=textureOverride??BiomeSurfaces.TextureSet(theme,slot);float smooth=BiomeSurfaces.Smoothness(set);
            bool soft=theme==SurfaceTheme.SoftPlay||theme==SurfaceTheme.SoftPlayFinish||theme==SurfaceTheme.SoftPlayNet||theme==SurfaceTheme.SoftPlayFloor;
            float metallic=slot==Surface.Metal&&theme!=SurfaceTheme.IndoorPlay&&!soft?.8f:0;
            foreach(string name in new[]{"_Metallic","_MetallicRemapMax"})SetFloat(m,name,metallic);
            SetFloat(m,"_MetallicRemapMin",0);SetFloat(m,"_SmoothnessRemapMin",0);
            foreach(string name in new[]{"_Smoothness","_Glossiness","_SmoothnessRemapMax"})SetFloat(m,name,smooth);
            SetFloat(m,"_NormalScale",theme==SurfaceTheme.Merchandise?.08f:.35f);SetFloat(m,"_BumpScale",.35f);
            if(set!=null)
            {
                var albedo=Resources.Load<Texture2D>(TextureRoot+set+"_Albedo");
                var normal=Resources.Load<Texture2D>(TextureRoot+set+"_Normal");
                var mask=Resources.Load<Texture2D>(TextureRoot+set+"_Mask");
                foreach(string name in new[]{"_BaseColorMap","_BaseMap","_MainTex"})SetTexture(m,name,albedo);
                foreach(string name in new[]{"_NormalMap","_BumpMap"})SetTexture(m,name,normal);
                SetTexture(m,"_MaskMap",mask);SetFloat(m,"_AORemapMin",0);SetFloat(m,"_AORemapMax",1);
                if(normal!=null){m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_NORMALMAP_TANGENT_SPACE");}
                if(mask!=null){m.EnableKeyword("_MASKMAP");SetFloat(m,"_SmoothnessRemapMax",theme==SurfaceTheme.Merchandise&&slot==Surface.Slide?.3f:1);}
            }
            if((theme==SurfaceTheme.ThermalWicker&&slot!=Surface.Plaster||(theme==SurfaceTheme.AquaticPark||theme==SurfaceTheme.AquaticCoral)&&slot==Surface.Slide)&&m.HasProperty("_CoatMask"))
            {SetFloat(m,"_CoatMask",1);m.EnableKeyword("_MATERIAL_FEATURE_CLEAR_COAT");}
            if(slot==Surface.Glow)
            {SetColor(m,"_EmissiveColor",Color.white);SetColor(m,"_EmissionColor",Color.white);m.EnableKeyword("_EMISSION");}
            if(theme==SurfaceTheme.Glass||theme==SurfaceTheme.SlideGlass||theme==SurfaceTheme.FlumeFlow)
            {
                SetColor(m,"_BaseColor",new Color(.84f,.94f,.97f,.13f));SetColor(m,"_Color",new Color(.84f,.94f,.97f,.13f));
                SetFloat(m,"_DoubleSidedEnable",1);SetFloat(m,"_DoubleSidedNormalMode",1);
                SetFloat(m,"_CullMode",0);SetFloat(m,"_CullModeForward",0);
                SetFloat(m,"_SurfaceType",1);SetFloat(m,"_Surface",1);SetFloat(m,"_BlendMode",0);SetFloat(m,"_Mode",3);
                SetFloat(m,"_SrcBlend",(float)BlendMode.One);SetFloat(m,"_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                SetFloat(m,"_AlphaSrcBlend",(float)BlendMode.One);SetFloat(m,"_AlphaDstBlend",(float)BlendMode.OneMinusSrcAlpha);
                SetFloat(m,"_ZWrite",0);SetFloat(m,"_TransparentZWrite",0);SetFloat(m,"_Smoothness",.93f);SetFloat(m,"_Metallic",0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.EnableKeyword("_ALPHAPREMULTIPLY_ON");m.SetOverrideTag("RenderType","Transparent");
                m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);
            }
            if(theme==SurfaceTheme.SlideGlass||theme==SurfaceTheme.FlumeFlow)
            {
                bool film=theme==SurfaceTheme.FlumeFlow;
                var tint=film?new Color(.88f,.98f,.99f,.24f):new Color(.9f,.97f,.98f,.08f);SetColor(m,"_BaseColor",tint);SetColor(m,"_Color",tint);
                // Both wall faces already exist in the tube mesh. Cull backfaces instead of blending them twice.
                SetFloat(m,"_DoubleSidedEnable",film?1:0);SetFloat(m,"_CullMode",film?0:2);SetFloat(m,"_CullModeForward",film?0:2);
                SetFloat(m,"_EnableBlendModePreserveSpecularLighting",1);SetFloat(m,"_Ior",film?1.333f:1.49f);
                SetFloat(m,"_NormalScale",film?.65f:.1f);SetFloat(m,"_BumpScale",film?.65f:.1f);
                SetFloat(m,"_Smoothness",film?.96f:.94f);SetFloat(m,"_SmoothnessRemapMax",1);
            }
            if(theme==SurfaceTheme.SpecialMirror)
            {
                // Existing SSR/ray-traced reflections and probe fallback; no mirror cameras.
                SetColor(m,"_BaseColor",new Color(.94f,.94f,.94f));SetColor(m,"_Color",new Color(.94f,.94f,.94f));
                SetFloat(m,"_Metallic",1);SetFloat(m,"_MetallicRemapMax",1);
                foreach(string name in new[]{"_Smoothness","_Glossiness","_SmoothnessRemapMax"})SetFloat(m,name,.985f);
                SetFloat(m,"_NormalScale",0);SetFloat(m,"_BumpScale",0);
            }
            Validate(m);return m;
        }
        public static void Validate(Material material)
        {
            // Avoid a hard HDRP assembly reference in Runtime. HDMaterial is public in HDRP 17.
            Type type=Type.GetType("UnityEngine.Rendering.HighDefinition.HDMaterial, Unity.RenderPipelines.HighDefinition.Runtime");
            type?.GetMethod("ValidateMaterial",new[]{typeof(Material)})?.Invoke(null,new object[]{material});
        }
        private static void SetColor(Material m,string name,Color value){if(m.HasProperty(name))m.SetColor(name,value);}
        private static void SetFloat(Material m,string name,float value){if(m.HasProperty(name))m.SetFloat(name,value);}
        private static void SetTexture(Material m,string name,Texture value){if(m.HasProperty(name)&&value!=null)m.SetTexture(name,value);}
    }
}
