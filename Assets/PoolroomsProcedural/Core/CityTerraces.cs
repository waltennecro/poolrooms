using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // One small cafe terrace per eligible block. Existing props retain their
    // array indices/ownership keys; terraces append after all original items.
    public static class CityTerraces
    {
        public const float AwningLongest=2.1f;
        public static bool TryBuilding(CityBuildingPlan[] buildings,out CityBuildingPlan building)
        {
            foreach(var b in buildings)
            {
                int type=CityStreetDetails.Typology(b);
                if(b.ExteriorSource==0&&b.Width>=10&&b.Depth>=10&&type!=0&&type!=4&&(b.Seed%3)==0){building=b;return true;}
            }
            building=default;return false;
        }
        public static V3 AwningBottom(CityBuildingPlan b){return b.Point(b.Width*.32f,3.05f,-b.Depth*.5f-.77f);}
        public static void Append(List<CityPropPlan> props,CityBuildingPlan[] buildings)
        {
            if(!TryBuilding(buildings,out var b))return;
            float x=b.Width*.32f,z=-b.Depth*.5f-.77f,yaw=b.Turn?180:0;
            props.Add(new CityPropPlan(b.Point(x,.02f,z),7,yaw));
            props.Add(new CityPropPlan(b.Point(x-.86f,.02f,z),8,yaw-90));
            props.Add(new CityPropPlan(b.Point(x+.86f,.02f,z),8,yaw+90));
        }
    }
}
