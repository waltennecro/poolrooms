using System;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    [CreateAssetMenu(menuName="Poolrooms/Independent biome materials",fileName="PoolroomsBiomeMaterials")]
    public sealed class PoolroomsBiomeMaterials : ScriptableObject
    {
        public Material[] surfaces=new Material[90];
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
            Color color=Color.white;
            if(theme==SurfaceTheme.IndoorPlay)
            {V3 c=LiminalPalette.Color(slot,0);color=slot==Surface.Tile||slot==Surface.Plaster?new Color(.87f,.89f,.85f):new Color(c.X,c.Y,c.Z);}
            if(theme==SurfaceTheme.Hotel&&slot==Surface.Plaster)color=new Color(.95f,.91f,.84f);
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
            if(slot==Surface.Glow)
            {SetColor(m,"_EmissiveColor",Color.white);SetColor(m,"_EmissionColor",Color.white);m.EnableKeyword("_EMISSION");}
            if(theme==SurfaceTheme.Glass)
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
