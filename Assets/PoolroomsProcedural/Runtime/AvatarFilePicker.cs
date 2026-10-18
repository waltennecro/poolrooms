using System;
using System.Runtime.InteropServices;

namespace Poolrooms
{
    public static class AvatarFilePicker
    {
        public static string Open()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.OpenFilePanelWithFilters("Importar skin humanoide","",new[]{"Modelos 3D","obj,fbx,dae,3ds,dxf,blend,glb,gltf","Todos","*"});
#elif UNITY_STANDALONE_WIN
            var dialog=new OpenFileName();dialog.size=Marshal.SizeOf<OpenFileName>();dialog.file=Marshal.AllocHGlobal(32768*2);Marshal.Copy(new byte[32768*2],0,dialog.file,32768*2);dialog.maxFile=32768;
            dialog.filter="Skin 3D\0*.obj;*.glb;*.gltf\0Todos\0*.*\0\0";dialog.title="Importar skin humanoide";
            dialog.flags=0x80000|0x1000|0x800|0x8; // Explorer, existing file/path, preserve working directory.
            try{return GetOpenFileName(dialog)?Marshal.PtrToStringUni(dialog.file):"";}finally{Marshal.FreeHGlobal(dialog.file);}
#else
            return ""; // Pasting a local path remains available on every desktop platform.
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        sealed class OpenFileName
        {
            public int size;public IntPtr owner,instance;public string filter,customFilter;public int maxCustomFilter,filterIndex;
            public IntPtr file;
            public int maxFile;public string fileTitle;public int maxFileTitle;public string initialDirectory,title;public int flags;
            public short fileOffset,extensionOffset;public string defaultExtension;public IntPtr customData,hook;public string template;
            public IntPtr reserved;public int reservedInt,flagsEx;
        }
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,EntryPoint="GetOpenFileNameW",SetLastError=true)]
        [return:MarshalAs(UnmanagedType.Bool)]static extern bool GetOpenFileName([In,Out]OpenFileName dialog);
#endif
    }
}
