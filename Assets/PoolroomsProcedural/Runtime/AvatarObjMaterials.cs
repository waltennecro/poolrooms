using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Local MTL diffuse maps only; authored FBX/DAE prefabs retain their Unity/HDRP materials.
    public static class AvatarObjMaterials
    {
        sealed class Entry {public Color Color=Color.white;public string Texture;}
        public static Material Material(string name,Color color)
        {
            bool hdrp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null&&UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline");
            var shader=Shader.Find(hdrp?"HDRP/Lit":"Standard")??Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null)throw new InvalidOperationException("No hay shader Lit disponible para la apariencia.");
            var material=new Material(shader){name=name};
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);if(material.HasProperty("_Color"))material.SetColor("_Color",color);
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.35f);if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.35f);
            PoolroomsBiomeMaterialFactory.Validate(material);
            return material;
        }
        static string Local(string directory,string reference,string relative=null)
        {
            if(string.IsNullOrWhiteSpace(reference)||Path.IsPathRooted(reference))return null;
            string full=Path.GetFullPath(Path.Combine(relative??directory,reference.Replace('\\',Path.DirectorySeparatorChar)));
            string prefix=Path.GetFullPath(directory)+Path.DirectorySeparatorChar;
            return full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)?full:null;
        }
        public static Material[] Create(AvatarObj obj,string file,List<UnityEngine.Object> owned)
        {
            string directory=Path.GetDirectoryName(file);var entries=new Dictionary<string,Entry>();
            foreach(var library in obj.Libraries)
            {
                string path=Local(directory,library);if(path==null||!File.Exists(path)||new FileInfo(path).Length>1024*1024)continue;
                Entry current=null;
                foreach(var raw in File.ReadLines(path))
                {
                    string line=raw.Trim();int split=line.IndexOf(' ');if(split<0)continue;string kind=line.Substring(0,split),value=line.Substring(split+1).Trim();
                    if(kind=="newmtl"){current=new Entry();entries[value]=current;}
                    else if(current!=null&&kind=="Kd")
                    {
                        var words=value.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                        if(words.Length>=3&&float.TryParse(words[0],NumberStyles.Float,CultureInfo.InvariantCulture,out float r)&&float.TryParse(words[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float g)&&float.TryParse(words[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float b))
                            current.Color=new Color(CameraPresentation.Unit(r),CameraPresentation.Unit(g),CameraPresentation.Unit(b));
                    }
                    else if(current!=null&&kind=="map_Kd"&&!value.StartsWith("-",StringComparison.Ordinal))current.Texture=Local(directory,value.Trim('"'),Path.GetDirectoryName(path));
                }
            }
            var materials=new Material[obj.Materials.Count];int textures=0,totalPixels=0;
            for(int i=0;i<materials.Length;i++)
            {
                entries.TryGetValue(obj.Materials[i],out var entry);var material=Material("Avatar / "+obj.Materials[i],entry!=null?entry.Color:Color.white);owned.Add(material);materials[i]=material;
                string path=entry?.Texture;if(path==null||textures>=8||!File.Exists(path)||new FileInfo(path).Length>8*1024*1024)continue;
                string extension=Path.GetExtension(path).ToLowerInvariant();if(extension!=".png"&&extension!=".jpg"&&extension!=".jpeg")continue;
                byte[] data=File.ReadAllBytes(path);if(!SmallImage(data,out int pixels)||totalPixels+pixels>16*1024*1024)continue;
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,true,false){name="Avatar / "+Path.GetFileName(path)};owned.Add(texture);
                if(!ImageConversion.LoadImage(texture,data,false))continue;textures++;totalPixels+=pixels;
                if(material.HasProperty("_BaseColorMap"))material.SetTexture("_BaseColorMap",texture);
                if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",texture);
                PoolroomsBiomeMaterialFactory.Validate(material);
            }
            return materials;
        }
        static bool SmallImage(byte[] data,out int pixels)
        {
            pixels=0;
            // Read dimensions before decoding; an avatar texture never needs an enormous allocation.
            if(data.Length>24&&data[0]==137&&data[1]==80&&data[2]==78&&data[3]==71)
            {
                long width=(long)data[16]<<24|(long)data[17]<<16|(long)data[18]<<8|data[19];
                long height=(long)data[20]<<24|(long)data[21]<<16|(long)data[22]<<8|data[23];
                if(width>0&&height>0&&width<=4096&&height<=4096){pixels=(int)(width*height);return true;}return false;
            }
            if(data.Length<4||data[0]!=255||data[1]!=216)return false;
            int at=2;while(at+3<data.Length)
            {
                if(data[at++]!=255)continue;int marker=data[at++];if(marker==255)continue;if(marker==217||marker==218)return false;
                if(marker==1||marker>=208&&marker<=215)continue;
                int size=data[at]*256+data[at+1];if(size<2||at+size>data.Length)return false;
                if(marker>=192&&marker<=195&&size>=7)
                {int h=data[at+3]*256+data[at+4],w=data[at+5]*256+data[at+6];if(w>0&&h>0&&w<=4096&&h<=4096){pixels=w*h;return true;}return false;}
                at+=size;
            }
            return false;
        }
    }
}
