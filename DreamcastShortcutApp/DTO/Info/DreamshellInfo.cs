namespace DreamcastShortcutApp.DTO.Info
{
    public class DreamshellInfo
    {
        public string DreamshellVersion { get; set; }
        public string Game { get; set; }
        public string Region { get; set; }
        public string LoaderVersion { get; set; }
        public string LoaderFile { get; set; }
        public int? FormatGdi { get; set; }
        public int? FormatCdi { get; set; }
        public int? FormatIso { get; set; }
        public string BootType { get; set; }
        public string BootMemory { get; set; }
        public string BootMode { get; set; }
        public int? FastBoot { get; set; }
        public int? Irq { get; set; }
        public int? Dma { get; set; }
        public int? LowLevel { get; set; }
        public string Async { get; set; }
        public string HeapMemory { get; set; }
        public int? Cdda { get; set; }
        public string CddaSource { get; set; }
        public string CddaDestination { get; set; }
        public string CddaPosition { get; set; }
        public string CddaChannel { get; set; }
        public string Vmu { get; set; }
        public string ScrHotkey { get; set; }
        public string PatchA1 { get; set; }
        public string PatchA2 { get; set; }
        public string PatchV1 { get; set; }
        public string PatchV2 { get; set; }
    }
}
