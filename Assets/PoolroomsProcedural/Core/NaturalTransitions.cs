using System;

namespace Poolrooms.Core
{
    public enum NaturalPassage { ChangingRoom,CommercialArcade,GardenGallery,ResidentialPorch,ReadingVestibule,ArcadedLanding }
    // The same edge has one physical shell; material halves share vertices at its midpoint.
    public static class NaturalTransitions
    {
        public static NaturalPassage Kind(LiminalEnvironment a,LiminalEnvironment b)
        {
            bool Has(LiminalEnvironment e){return a==e||b==e;}
            if(Has(LiminalEnvironment.AmberRestaurant)||Has(LiminalEnvironment.IndoorBoardwalk)||Has(LiminalEnvironment.CeramicVillage))return NaturalPassage.CommercialArcade;
            if(Has(LiminalEnvironment.CoveredMarket)||Has(LiminalEnvironment.PostalArcade)||Has(LiminalEnvironment.Supermarket)||Has(LiminalEnvironment.City11))return NaturalPassage.CommercialArcade;
            if(Has(LiminalEnvironment.RainHouses))return NaturalPassage.ResidentialPorch;
            if(Has(LiminalEnvironment.BambooRetreat)||Has(LiminalEnvironment.HangingOrchards)||Has(LiminalEnvironment.LunarGreenhouse)||Has(LiminalEnvironment.RailwayWintergarden))return NaturalPassage.GardenGallery;
            if(Has(LiminalEnvironment.MagnoliaCourts)||Has(LiminalEnvironment.Conservatory)||Has(LiminalEnvironment.Orangerie)||Has(LiminalEnvironment.WinterGarden)||Has(LiminalEnvironment.TeaPavilions))return NaturalPassage.GardenGallery;
            if(Has(LiminalEnvironment.CloudBaths)||Has(LiminalEnvironment.MosaicCistern)||Has(LiminalEnvironment.AquariumGalleries))return NaturalPassage.ChangingRoom;
            if(Has(LiminalEnvironment.Poolrooms)||Has(LiminalEnvironment.AquaticPark)||Has(LiminalEnvironment.ThermalBaths))return NaturalPassage.ChangingRoom;
            if(Has(LiminalEnvironment.SunkenLibrary)||Has(LiminalEnvironment.RainyBookshops)||Has(LiminalEnvironment.MapmakersArchive)||Has(LiminalEnvironment.VelvetTheatre))return NaturalPassage.ReadingVestibule;
            if(Has(LiminalEnvironment.PaperWorkshop)||Has(LiminalEnvironment.RadioSalon)||Has(LiminalEnvironment.SolarObservatory)||Has(LiminalEnvironment.Archive906)||Has(LiminalEnvironment.SunsetLibrary)||Has(LiminalEnvironment.MusicStudios))return NaturalPassage.ReadingVestibule;
            return NaturalPassage.ArcadedLanding;
        }
        public static bool Applies(Room a,Room b,LayoutConfig c)
        {return c.EncounterExploration&&a!=null&&b!=null&&a.Environment!=b.Environment;}
        public static CorridorGeometry Build(Room a,Room b,int direction,LayoutConfig c)
        {
            if(direction<0||direction>1)throw new ArgumentOutOfRangeException(nameof(direction));
            var result=new CorridorGeometry{Style=CorridorStyle.WideGallery,Natural=true};
            MeshData first=new MeshData{Theme=BiomeSurfaces.ForEnvironment(a.Environment)},second=new MeshData{Theme=BiomeSurfaces.ForEnvironment(b.Environment)};
            float start=(direction==0?a.Depth:a.Width)*.5f,end=c.CellSize-(direction==0?b.Depth:b.Width)*.5f,length=end-start;
            if(length<=.02f)return null; // Shared-wall joins retain their purpose-built existing geometry.
            int back=direction+2;float from=SolarConnections.DoorWidth(a,direction,c)*.5f,to=SolarConnections.DoorWidth(b,back,c)*.5f;
            float height=Math.Min(SolarConnections.DoorHeight(a,direction,c),SolarConnections.DoorHeight(b,back,c)),thickness=c.WallThickness,uv=c.TextureWorldSize;
            var kind=Kind(a.Environment,b.Environment);int steps=Math.Max(4,(int)Math.Ceiling(length/.7f));if(steps%2!=0)steps++;
            for(int i=0;i<steps;i++)
            {
                float p=(float)i/steps,q=(float)(i+1)/steps,z0=start+length*p,z1=start+length*q;
                float Width(float t){float blend=(float)Math.Sin(Math.PI*t);return from+(to-from)*t+Math.Min(1.25f,length*.12f)*blend*blend;}
                float w0=Width(p),w1=Width(q),f0=a.Floor+(b.Floor-a.Floor)*p,f1=a.Floor+(b.Floor-a.Floor)*q;
                MeshData mesh=i<steps/2?first:second;
                Face(mesh,new V3(-w0,f0,z0),new V3(-w1,f1,z1),new V3(w1,f1,z1),new V3(w0,f0,z0),V3.Up,Surface.Tile,uv);
                Face(mesh,new V3(-w0,f0-.25f,z0),new V3(w0,f0-.25f,z0),new V3(w1,f1-.25f,z1),new V3(-w1,f1-.25f,z1),V3.Up*-1,Surface.Tile,uv);
                // Collars start at the outer faces of existing door walls. No overlapping ceiling/jamb faces.
                float sz0=z0+(i==0?thickness*.5f:0),sz1=z1-(i==steps-1?thickness*.5f:0);
                if(sz1<=sz0)continue;
                float sw0=Width((sz0-start)/length),sw1=Width((sz1-start)/length);
                foreach(int sign in new[]{-1,1})
                {
                    float x0=sign*sw0,x1=sign*sw1;V3 inward=new V3(-sign,0,0);
                    Surface wall=(i<steps/2?a:b).Environment==LiminalEnvironment.Poolrooms?Surface.Tile:Surface.Plaster;
                    Face(mesh,new V3(x0,f0-.25f,sz0),new V3(x1,f1-.25f,sz1),new V3(x1,height,sz1),new V3(x0,height,sz0),inward,wall,uv);
                    float o0=x0+sign*thickness,o1=x1+sign*thickness;
                    Face(mesh,new V3(o0,f0-.25f,sz0),new V3(o0,height+.18f,sz0),new V3(o1,height+.18f,sz1),new V3(o1,f1-.25f,sz1),inward*-1,Surface.Plaster,uv);
                    Face(mesh,new V3(x0,height,sz0),new V3(x1,height,sz1),new V3(o1,height+.18f,sz1),new V3(o0,height+.18f,sz0),V3.Up,Surface.Plaster,uv);
                    if(i==0)Face(mesh,new V3(x0,f0-.25f,sz0),new V3(x0,height,sz0),new V3(o0,height+.18f,sz0),new V3(o0,f0-.25f,sz0),new V3(0,0,-1),wall,uv);
                    if(i==steps-1)Face(mesh,new V3(x1,f1-.25f,sz1),new V3(o1,f1-.25f,sz1),new V3(o1,height+.18f,sz1),new V3(x1,height,sz1),new V3(0,0,1),wall,uv);
                    ZoneShapes.Beam(mesh,new V3(x0-sign*.025f,f0+.07f,sz0),new V3(x1-sign*.025f,f1+.07f,sz1),.045f,Surface.Accent,uv,8);
                }
                var roof=kind==NaturalPassage.GardenGallery?result.TransitionGlass:mesh;
                Face(roof,new V3(-sw0,height,sz0),new V3(sw0,height,sz0),new V3(sw1,height,sz1),new V3(-sw1,height,sz1),V3.Up*-1,Surface.Plaster,uv);
                if(kind!=NaturalPassage.GardenGallery)Face(mesh,new V3(-sw0,height+.18f,sz0),new V3(-sw1,height+.18f,sz1),new V3(sw1,height+.18f,sz1),new V3(sw0,height+.18f,sz0),V3.Up,Surface.Plaster,uv);
            }
            for(int n=1;n<=2;n++)
            {
                float z=start+length*n/3,half=(from+(to-from)*n/3)+.05f;
                var mesh=n==1?first:second;
                foreach(int side in new[]{-1,1})ZoneShapes.Box(mesh,side*half,z,.08f,.15f,0,height,Surface.Accent,uv);
                ZoneShapes.Box(mesh,0,z,half*2,.18f,height-.16f,height,Surface.Accent,uv);
            }
            float middle=(start+end)*.5f,midFloor=(a.Floor+b.Floor)*.5f;
            float widest=(from+to)*.5f+Math.Min(1.25f,length*.12f);
            float pocket=Math.Max(.15f,widest-.3f),detailDepth=Math.Min(1.8f,length*.32f);
            if(kind==NaturalPassage.ChangingRoom||kind==NaturalPassage.ReadingVestibule||kind==NaturalPassage.ResidentialPorch)
            {
                foreach(int side in new[]{-1,1})
                {
                    float xx=side*pocket;
                    ZoneShapes.Box(first,xx,middle,.42f,detailDepth,midFloor+.4f,midFloor+.46f,Surface.Accent,uv);
                    foreach(int zsign in new[]{-1,1})ZoneShapes.Box(first,xx,middle+zsign*detailDepth*.32f,.25f,.09f,midFloor,midFloor+.4f,Surface.Metal,uv);
                    if(kind==NaturalPassage.ChangingRoom)
                        for(int hook=0;hook<3;hook++)ZoneShapes.Box(first,side*(widest-.06f),middle+(hook-1)*detailDepth*.28f,.1f,.06f,1.6f,1.68f,Surface.Metal,uv);
                    if(kind==NaturalPassage.ReadingVestibule)
                        for(int book=0;book<6;book++)ZoneShapes.Box(first,xx,middle-.4f+book*.14f,.24f,.1f,midFloor+.46f,midFloor+.65f+book%3*.035f,book%2==0?Surface.Accent:Surface.Plaster,uv);
                }
            }
            if(kind==NaturalPassage.CommercialArcade||kind==NaturalPassage.ArcadedLanding)
                for(int rib=0;rib<3;rib++)ZoneShapes.Box(first,0,start+length*(rib+1)/4,Math.Min(from,to)*2,.09f,height-.21f,height-.1f,Surface.Accent,uv);
            if(kind==NaturalPassage.GardenGallery)
            {
                result.ShutterPosition=new V3(0,height+.06f,middle-.7f);
                for(int rib=0;rib<4;rib++)ZoneShapes.Box(first,0,start+length*(rib+1)/5,Math.Min(from,to)*2,.065f,height-.06f,height,Surface.Metal,uv);
            }
            if(kind==NaturalPassage.ResidentialPorch)
                for(int slat=0;slat<8;slat++)ZoneShapes.Box(first,0,start+length*(slat+.5f)/8,Math.Min(from,to)*2,.08f,height-.12f,height,Surface.Accent,uv);
            first.EmissionStrength=0;first.EmissionTint=new V3(.19f,.22f,.2f);
            string label=kind==NaturalPassage.ChangingRoom?"VESTUARIOS":kind==NaturalPassage.GardenGallery?"JARDIN":kind==NaturalPassage.CommercialArcade?"GALERIA":kind==NaturalPassage.ReadingVestibule?"LECTURA":kind==NaturalPassage.ResidentialPorch?"PATIO":"PASEO";
            ZoneShapes.Box(first,0,start+length*.18f+.03f,label.Length*.15f+.16f,.045f,height-.65f,height-.25f,Surface.Plaster,uv);
            foreach(int sign in new[]{-1,1})ZoneShapes.Box(first,sign*(label.Length*.075f-.05f),start+length*.18f+.03f,.025f,.035f,height-.25f,height,Surface.Metal,uv);
            MallGlyphs.Write(first,label,new V3(-label.Length*.075f,height-.58f,start+length*.18f),.025f);
            // A real ceiling luminaire marks the approach, with the same white-point at both ends.
            for(int n=0;n<2;n++)result.Fixtures.Add(new Fixture{Position=new V3(0,height-.12f,start+length*(n==0?.28f:.72f)),Width=1,Depth=.18f,Power=950,Temperature=kind==NaturalPassage.ChangingRoom?4200:3600,Range=Math.Max(6,length)});
            result.Structure.Append(first);result.Structure.Append(second);
            result.Structure.Append(result.TransitionGlass);
            if(direction==1){result.TransitionGlass.TranslateRotate(new V3(),Shapes.Pi*.5f);result.ShutterPosition=new V3(result.ShutterPosition.Z,result.ShutterPosition.Y,-result.ShutterPosition.X);result.ShutterYaw=90;result.Structure.TranslateRotate(new V3(),Shapes.Pi*.5f);first.TranslateRotate(new V3(),Shapes.Pi*.5f);second.TranslateRotate(new V3(),Shapes.Pi*.5f);for(int i=0;i<result.Fixtures.Count;i++){var f=result.Fixtures[i];f.Position=new V3(f.Position.Z,f.Position.Y,-f.Position.X);result.Fixtures[i]=f;}}
            result.Finishes.Add(first);result.Finishes.Add(second);result.Passage=kind;return result;
        }
        private static void Face(MeshData mesh,V3 a,V3 b,V3 c,V3 d,V3 desired,Surface surface,float uv)
        {var normal=V3.Cross(b-a,c-a).Unit;if(V3.Dot(normal,desired)<0)normal*= -1;mesh.Quad(a,b,c,d,normal,surface,uv);}
    }
}
