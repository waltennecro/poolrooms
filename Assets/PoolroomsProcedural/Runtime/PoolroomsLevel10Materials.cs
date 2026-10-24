using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    public static class PoolroomsLevel10Materials
    {
        static readonly Dictionary<int,Material> cache=new Dictionary<int,Material>();
        public static bool IsTheme(SurfaceTheme theme)
        {return theme>=SurfaceTheme.Level10Ground&&theme<=SurfaceTheme.Level10Equipment;}
        public static Material Resolve(SurfaceTheme theme,Surface surface,PoolroomsProfile profile)
        {
            if(surface==Surface.Metal)return PoolroomsJourneyMaterials.ResolveCity(0,Surface.Metal);
            if(surface==Surface.Slide&&profile!=null)return profile.waterPreview;
            if(theme==SurfaceTheme.Level10Ground&&surface==Surface.Accent)
                return PoolroomsForest39Materials.Resolve(SurfaceTheme.Forest39Ground,Surface.Accent);
            if(theme==SurfaceTheme.Level10Equipment&&surface==Surface.Plaster)
                return PoolroomsForest39Materials.Resolve(SurfaceTheme.Forest39Stone,Surface.Tile);
            int key=(int)theme*8+(int)surface;if(cache.TryGetValue(key,out var old)&&old!=null)return old;
            bool grain=theme==SurfaceTheme.Level10Grain,paper=surface==Surface.Glow;
            string bank=theme==SurfaceTheme.Level10Ground?"Meadow":theme==SurfaceTheme.Level10Siding?"Siding":"Planks";
            Color colour=paper?new Color(.87f,.83f,.69f):grain?surface==Surface.Tile?new Color(.64f,.47f,.19f):surface==Surface.Plaster?new Color(.43f,.46f,.22f):new Color(.54f,.48f,.22f):Color.white;
            var m=City11Sources.MaterialFor(new City11Sources.MaterialDefinition
            {
                name="Level10_"+theme+"_"+surface,
                albedo=grain||paper?null:"Level10/Textures/"+bank+"_Albedo",
                normal=grain||paper?null:"Level10/Textures/"+bank+"_Normal",
                mask=grain||paper?null:"Level10/Textures/"+bank+"_Mask",
                colour=new[]{colour.r,colour.g,colour.b,1f},roughness=grain?.72f:.86f,doubleSided=grain
            });
            if(!grain&&!paper)
            {
                var height=Resources.Load<Texture2D>("Level10/Textures/"+bank+"_Height");
                if(height!=null&&m.HasProperty("_HeightMap"))
                {
                    m.SetTexture("_HeightMap",height);Set(m,"_DisplacementMode",2);Set(m,"_HeightAmplitude",bank=="Meadow"?.008f:.012f);
                    Set(m,"_HeightCenter",.5f);Set(m,"_HeightPoMAmplitude",bank=="Meadow"?.8f:1.2f);
                    Set(m,"_PPDMinSamples",5);Set(m,"_PPDMaxSamples",16);Set(m,"_PPDLodThreshold",5);
                    m.EnableKeyword("_HEIGHTMAP");m.EnableKeyword("_PIXEL_DISPLACEMENT");
                }
            }
            if(bank=="Siding"&&!grain&&!paper)foreach(string property in new[]{"_BaseColorMap","_NormalMap","_MaskMap","_HeightMap"})if(m.HasProperty(property))m.SetTextureScale(property,new Vector2(.5f,1));
            cache[key]=m;return m;
        }
        static void Set(Material m,string property,float value){if(m.HasProperty(property))m.SetFloat(property,value);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){cache.Clear();}
    }
}
