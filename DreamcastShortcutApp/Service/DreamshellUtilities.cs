using DreamcastShortcutApp.DTO.Struct;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DreamcastShortcutApp.Service
{
    public class DreamshellUtilities
    {
        private const string DREAMSHELL_UTILITIES_DLL = "DreamshellUtilities.dll";
        private readonly IntPtr classPtr = IntPtr.Zero;

        [DllImport(DREAMSHELL_UTILITIES_DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateInstance();

        [DllImport(DREAMSHELL_UTILITIES_DLL, CharSet = CharSet.Ansi)]
        private static extern void DestroyInstance(IntPtr classPtr);

        [DllImport(DREAMSHELL_UTILITIES_DLL, CharSet = CharSet.Ansi)]
        private static extern IntPtr MakePresetFileName(IntPtr classPtr, IntPtr trackFilePtr, bool sd);

        [DllImport(DREAMSHELL_UTILITIES_DLL, CharSet = CharSet.Ansi)]
        private static extern void FreeCharPointer(IntPtr charPtr);

        [DllImport(DREAMSHELL_UTILITIES_DLL, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Struct)]
        private static extern IPBinMetaStruct ReadIpData(IntPtr classPtr, string trackFile);

        public DreamshellUtilities()
        {
            classPtr = CreateInstance();
        }

        public string MakePresetFileName(string trackFile, bool sd = false)
        {
            string presetFile = null;

            IntPtr charPtr = MakePresetFileName(classPtr, Marshal.StringToHGlobalAnsi(trackFile), false);
            if (charPtr != IntPtr.Zero)
            {
                presetFile = Marshal.PtrToStringAnsi(charPtr);
                FreeCharPointer(charPtr);
                charPtr = IntPtr.Zero;
            }

            return presetFile;
        }

        public IPBinMetaStruct ReadIpDataStruct(string trackFile)
        {
            return ReadIpData(classPtr, trackFile);
        }

        public void Dispose()
        {
            DestroyInstance(classPtr);
        }
    }
}
