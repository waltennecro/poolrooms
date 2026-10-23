using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Original spatial recipes; 256 is not a count of downloaded source models.
    // Every ID changes actual floor openings, bridges, partitions and roof sections.
    public readonly struct CityBuildingDesign
    {
        public const int OrdinaryCount=128,StrangeCount=128,Count=256;
        public readonly int Id;
        public int Family {get{return Id/8;}}
        public int Variant {get{return Id%8;}}
        public bool Strange {get{return Id>=OrdinaryCount;}}
        public string Name {get{return Names[Family];}}
        static readonly string[] Names={
            "Casa de patios","Galería comercial","Biblioteca urbana","Talleres escalonados",
            "Jardín de invierno","Patios gemelos","Escuela de artes","Pasaje con puentes",
            "Lonja porticada","Hotel de galerías","Mercado cubierto","Sala de espera urbana",
            "Estudios con terrazas","Oficinas de atrio","Conservatorio","Casa cívica",
            "Habitaciones en anillo","Puentes desplazados","Patio comprimido","Fachada plegada",
            "Cámaras suspendidas","Pórticos multiplicados","Cañón interior","Galerías entretejidas",
            "Torre de linternas","Salones en cascada","Patios encajados","Calle vertical",
            "Galerías anidadas","Gran casetón","Cubiertas inclinadas","Monolito hueco"};
        public CityBuildingDesign(int id)
        {if(id<0||id>=Count)throw new ArgumentOutOfRangeException(nameof(id));Id=id;}
        public static int Select(uint seed){return (int)(StableRandom.Hash(unchecked((int)seed),0,0,0xC17AB1u)%Count);}
        public PlanRect Shaft(CityBuildingPlan b)
        {float x=-b.Width*.5f+2.2f;return new PlanRect(x-2.1f,-3.6f,x+2.1f,3.6f);}
        // A continuous front gallery connects the stair return to every upper wing.
        // No floor opening reaches this protected route or the exterior perimeter.
        public PlanRect Court(CityBuildingPlan b,int floor)
        {
            float left=-b.Width*.5f+4.55f,right=b.Width*.5f-1.22f;
            float width=(right-left)*(.62f+Variant*.035f);
            float center=(left+right)*.5f+((Family%3)-1)*(right-left-width)*.33f;
            float depth=b.Depth*(.22f+Variant*.011f+(Family%4)*.018f+((Family%16)/4)*.017f);
            float minZ=-.8f+Variant*.025f+(Family%3)*.03f;
            if(Strange)minZ+=((floor+Family)%3-1)*.14f;
            float maxZ=Math.Min(b.Depth*.5f-1.35f,minZ+depth);
            return new PlanRect(center-width*.5f,minZ,center+width*.5f,maxZ);
        }
        public List<PlanRect> Openings(CityBuildingPlan b,int floor)
        {
            var hole=Court(b,floor);var result=new List<PlanRect>();
            if(Family%4==1&&hole.Depth>2.3f)
            {
                // Twin lightwells separated by a real, 1.10m-wide floor bridge.
                float gap=1.1f;
                result.Add(new PlanRect(hole.MinX,hole.MinZ,hole.MaxX,hole.Z-gap*.5f));
                result.Add(new PlanRect(hole.MinX,hole.Z+gap*.5f,hole.MaxX,hole.MaxZ));
            }
            else if(Family%4==2&&hole.Width>2.8f)
            {
                result.Add(new PlanRect(hole.MinX,hole.MinZ,hole.X-.55f,hole.MaxZ));
                result.Add(new PlanRect(hole.X+.55f,hole.MinZ,hole.MaxX,hole.MaxZ));
            }
            else result.Add(hole);
            return result;
        }
        public List<PlanRect> Floor(CityBuildingPlan b,int level)
        {
            if(level<0||level>=b.Floors)throw new ArgumentOutOfRangeException(nameof(level));
            var parts=new List<PlanRect>{new PlanRect(-b.Width*.5f,-b.Depth*.5f,b.Width*.5f,b.Depth*.5f)};
            if(level==0)return parts;
            PlanRect.Subtract(parts,Shaft(b));
            foreach(var hole in Openings(b,level))PlanRect.Subtract(parts,hole);
            return parts;
        }
        // Reuses native 4K source maps where authored at 4K; no image upscaling.
        public static int Finish(int design,Surface slot)
        {
            if(design<0||design>=Count||slot==Surface.Glow)return -1;
            var p=new CityBuildingDesign(design);
            if(slot==Surface.Tile)return p.Strange?0:(p.Family%3==0?19:0);
            if(slot==Surface.Plaster)return p.Family%3==0?33:p.Family%3==1?14:22;
            return slot==Surface.Accent?40:slot==Surface.Metal?41:42;
        }
    }
}
