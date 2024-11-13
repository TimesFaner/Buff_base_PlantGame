using System;
using System.Runtime.InteropServices;

namespace QFramework
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public IntPtr custData = IntPtr.Zero;
        public string customFilter = null;
        public string defExt = null;
        public IntPtr dlgOwner = IntPtr.Zero;
        public string file = null;
        public short fileExtension = 0;
        public short fileOffset = 0;
        public string fileTitle = null;
        public string filter = null;
        public int filterIndex = 0;
        public int flags = 0;
        public int flagsEx = 0;
        public IntPtr hook = IntPtr.Zero;
        public string initialDir = null;
        public IntPtr instance = IntPtr.Zero;
        public int maxCustFilter = 0;
        public int maxFile = 0;
        public int maxFileTitle = 0;
        public int reservedInt = 0;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int structSize = 0;
        public string templateName = null;
        public string title = null;
    }

    public class LocalDialog
    {
        //链接指定系统函数       打开文件对话框
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In] [Out] OpenFileName ofn);

        public static bool GetOFN([In] [Out] OpenFileName ofn)
        {
            return GetOpenFileName(ofn);
        }

        //链接指定系统函数        另存为对话框
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In] [Out] OpenFileName ofn);

        public static bool GetSFN([In] [Out] OpenFileName ofn)
        {
            return GetSaveFileName(ofn);
        }
    }
}