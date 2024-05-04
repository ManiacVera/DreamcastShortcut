using CommunityToolkit.Mvvm.ComponentModel;

namespace DreamcastShortcutApp.DTO.Info
{
    public partial class LanguageInfo : ObservableObject
    {
        [ObservableProperty]
        private string language;

        [ObservableProperty]
        private string languageCode;
    }
}
