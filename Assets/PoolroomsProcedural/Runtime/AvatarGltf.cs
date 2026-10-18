using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Poolrooms
{
    // Optional Unity glTFast adapter: no package types in Runtime's assembly dependencies.
    public sealed class AvatarGltf : IDisposable
    {
        static Type ImportType {get{return Type.GetType("GLTFast.GltfImport, glTFast",false);}}
        public static bool Available {get{return ImportType!=null;}}
        readonly object importer;
        public AvatarGltf()
        {
            var type=ImportType;if(type==null)throw new InvalidOperationException("Unity glTFast no está instalado.");
            foreach(var ctor in type.GetConstructors())
            {
                var parameters=ctor.GetParameters();bool optional=true;foreach(var p in parameters)optional&=p.IsOptional;
                if(optional){importer=ctor.Invoke(Arguments(parameters));break;}
            }
            if(importer==null)throw new NotSupportedException("La API de glTFast no es compatible. Usa Unity glTFast 6.7.1.");
        }
        static object[] Arguments(ParameterInfo[] parameters)
        {
            var values=new object[parameters.Length];for(int i=0;i<values.Length;i++)
            {var p=parameters[i];values[i]=p.DefaultValue==null||p.DefaultValue==DBNull.Value||p.DefaultValue==Missing.Value?(p.ParameterType.IsValueType?Activator.CreateInstance(p.ParameterType):null):p.DefaultValue;}return values;
        }
        async Task<bool> Call(string name,Type first,object value)
        {
            foreach(var method in importer.GetType().GetMethods())
            {
                var parameters=method.GetParameters();if(method.Name!=name||parameters.Length==0||parameters[0].ParameterType!=first)continue;
                bool optional=true;for(int i=1;i<parameters.Length;i++)optional&=parameters[i].IsOptional;if(!optional)continue;
                var args=Arguments(parameters);args[0]=value;if(!(method.Invoke(importer,args) is Task<bool> task))throw new NotSupportedException("API glTFast inesperada.");return await task;
            }
            throw new MissingMethodException("glTFast",name);
        }
        public async Task<bool> Load(string path,Transform parent)
        {if(!await Call("Load",typeof(string),new Uri(path).AbsoluteUri))return false;return await Call("InstantiateMainSceneAsync",typeof(Transform),parent);}
        public void Dispose(){(importer as IDisposable)?.Dispose();}
    }
}
