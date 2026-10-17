using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class MallDisplayMesh
    {
        // Private immutable templates: at most 256 CPU meshes for the entire application.
        // Never returned to callers or transformed in-place, and never own Unity objects.
        private static readonly MeshData[] Templates=new MeshData[MallItemCatalog.Count];
        private static readonly object Gate=new object();
        private static MeshData Template(int id)
        {
            lock(Gate){if(Templates[id]==null)Templates[id]=MallItemCatalog.Model(id);return Templates[id];}
        }
        public static MeshData Build(IReadOnlyList<MallLooseProp> slots,float baseY,Func<int,bool> include,out int count)
        {
            var result=new MeshData{Theme=SurfaceTheme.Merchandise,EmissionStrength=0};count=0;
            for(int i=0;i<slots.Count;i++)
            {
                if(include!=null&&!include(i))continue;
                var slot=slots[i];var model=Template(slot.ItemId);
                float c=(float)Math.Cos(slot.Yaw),s=(float)Math.Sin(slot.Yaw);int offset=result.Positions.Count;
                V3 translation=slot.Position-new V3(0,baseY,0);
                for(int j=0;j<model.Positions.Count;j++)
                {
                    V3 v=model.Positions[j];result.Positions.Add(new V3(c*v.X+s*v.Z,v.Y,-s*v.X+c*v.Z)+translation);
                    v=model.Normals[j];result.Normals.Add(new V3(c*v.X+s*v.Z,v.Y,-s*v.X+c*v.Z));
                }
                result.UV.AddRange(model.UV);
                for(int material=0;material<6;material++)foreach(int index in model.Indices[material])result.Indices[material].Add(offset+index);
                count++;
            }
            return result;
        }
    }
}
