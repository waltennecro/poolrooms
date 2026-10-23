using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Resources.Load fetches only surfaces used by loaded rooms; source originals
    // and authoring previews are not included in this playable resource bank.
    public static class PoolroomsJourneyMaterials
    {
        [System.Serializable] sealed class Palette
        {public City11Sources.MaterialDefinition[] materials=new City11Sources.MaterialDefinition[0];}
        static Palette palette;
        public static Material Resolve(SurfaceTheme theme,int slot)
        {
            return At(JourneyLevelPlanner.Finish(theme,(Surface)slot));
        }
        public static Material ResolveCity(int design,Surface slot)
        {return At(CityBuildingDesign.Finish(design,slot));}
        static Material At(int index)
        {
            if(index<0)return null;
            if(palette==null)
            {
                var file=Resources.Load<TextAsset>("JourneyFinishes/palette");
                if(file==null)return null;palette=JsonUtility.FromJson<Palette>(file.text);
            }
            if(index>=palette.materials.Length)return null;
            return City11Sources.MaterialFor(palette.materials[index]);
        }
    }
}
