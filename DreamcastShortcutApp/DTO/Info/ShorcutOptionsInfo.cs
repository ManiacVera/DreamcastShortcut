namespace DreamcastShortcutApp.DTO.Info
{
    public class ShorcutOptionsInfo
    {
        // ALL OPTIONS
        public int ActiveOption { get; set; }
        public int DreamshellVersion { get; set; }
        public string DreamshellPartition { get; set; }

        // SHORCUT OPTIONS
        public bool SD { get; set; }
        public string GamesFolder { get; set; }
        public string PresetFolder { get; set; }
        public string CoversFolder { get; set; }
        public string DestinationFolder { get; set; }
        public string DreamshellFolderGames { get; set; }
        //public bool UseGamesFolder { get; set; }
        public bool RemoveShortcuts { get; set; }
        public int ImageSize { get; set; }
        public bool IsCoversPng { get; set; }
        public bool ShowName { get; set; }
        public int MaxSizeName { get; set; } = 0;        
        public int MaxShortcuts { get; set; }
        public bool RandomShortcuts { get; set; }
        public int GameMoreOneDisk { get; set; }  
        public int ShortcutsSource { get; set; }
        
        // PRESET OPTIONS
        public bool OverwritePreset { get; set; }
    }
}
