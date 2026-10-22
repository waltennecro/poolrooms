using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    // A ScriptableObject gives Unity an unambiguous MonoScript anchor for this
    // installation. No project-wide search, folder-name guess or fixed GUID.
    public sealed class PoolroomsPackagePaths : ScriptableObject
    {
        private static string cachedRoot;
        private const string Locator="/Editor/PoolroomsPackagePaths.cs";
        public static string Root
        {
            get
            {
                if(!string.IsNullOrEmpty(cachedRoot)&&AssetDatabase.IsValidFolder(cachedRoot)&&File.Exists(cachedRoot+Locator))return cachedRoot;
                var locator=CreateInstance<PoolroomsPackagePaths>();locator.hideFlags=HideFlags.HideAndDontSave;
                try
                {
                    string path=AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(locator));
                    cachedRoot=RootFromScriptPath(path);return cachedRoot;
                }
                finally{DestroyImmediate(locator);}
            }
        }
        public static string RootFromScriptPath(string path)
        {
            string normalized=(path??string.Empty).Replace('\\','/');
            if(!normalized.StartsWith("Assets/",StringComparison.Ordinal)||!normalized.EndsWith(Locator,StringComparison.Ordinal))
                throw new InvalidOperationException("Unity no pudo localizar el script del paquete Poolrooms: "+normalized);
            return normalized.Substring(0,normalized.Length-Locator.Length);
        }
        public static string Asset(string relative)
        {
            string normalized=(relative??string.Empty).Replace('\\','/');
            if(string.IsNullOrEmpty(normalized)||normalized.StartsWith("/",StringComparison.Ordinal)||normalized.Contains(":"))
                throw new ArgumentException("Se necesita una ruta relativa al paquete.",nameof(relative));
            foreach(string part in normalized.Split('/'))if(part==".."||part==".")
                throw new ArgumentException("La ruta debe permanecer dentro del paquete.",nameof(relative));
            return Root+"/"+normalized;
        }
        public static string RequireFile(string relative)
        {
            string path=Asset(relative);
            if(!File.Exists(path))throw new FileNotFoundException("Falta un archivo del paquete Poolrooms: "+path+". Extrae también sus texturas y recursos.",path);
            return path;
        }
        public static void ValidateDemoTextures()
        {
            foreach(string material in new[]{"Ceramic","Plaster","Metal"})
            foreach(string map in new[]{"Albedo","Normal","Mask"})RequireFile("Textures/"+material+"_"+map+".png");
        }
    }
}
