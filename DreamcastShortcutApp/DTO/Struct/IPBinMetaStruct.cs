using System.Runtime.InteropServices;

namespace DreamcastShortcutApp.DTO.Struct
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct IPBinMetaStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string HardwareID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string MakerID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string DeviceInfo;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string CountryCodes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        public string Ctrl;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        public string Dev;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        public string VGA;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        public string WinCE;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        public string Unk;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string ProductID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 6)]
        public string ProductVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string ReleaseDate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string BootFile;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string SoftwareMakerInfo;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Title;
    }
}
