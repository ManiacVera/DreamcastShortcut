using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DreamcastShortcutApp.DTO.Constants;
using DreamcastShortcutApp.DTO.Enum;
using DreamcastShortcutApp.DTO.Info;
using DreamcastShortcutApp.Service;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DreamcastShortcutApp.ViewModel
{
    public partial class MainWindowVM : ObservableObject
    {
        private string configFile = null;
        private Encoding utf8WithoutBom = null;

        [ObservableProperty]
        private ObservableCollection<LanguageInfo> languageList;

        [ObservableProperty]
        private LanguageInfo currentLanguage;

        [ObservableProperty]
        private GeneralOptionsInfo generalData;

        [ObservableProperty]
        private ShorcutOptionsInfo optionsData;

        [ObservableProperty]
        private ObservableCollection<ComboBoxInfo> imageSizeList;

        [ObservableProperty]
        private ObservableCollection<ComboBoxInfo> gameMoreDiskList;

        [ObservableProperty]
        private bool deviceIDE;

        [ObservableProperty]
        private bool deviceSD;

        [ObservableProperty]
        private bool configShortcuts;

        [ObservableProperty]
        private bool configPresets;

        [ObservableProperty]
        private ObservableCollection<ComboBoxInfo> partitionList;

        [ObservableProperty]
        private ObservableCollection<ComboBoxInfo> dreamshellVersionList;

        [ObservableProperty]
        private ObservableCollection<ComboBoxInfo> shortcutsSourceList;        

        [ObservableProperty]
        private string gamesFolder;

        public MainWindowVM()
        {
            //string dump = DreamcastShortcutService.DumpPresetsConfigFormat(@"C:\Users\c.lverar\Desktop\Main DS\presets", false);

            configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.CONFIG_FILE);
            utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            ConfigShortcuts = true;
            ConfigPresets = false;
            GeneralData = new GeneralOptionsInfo();
            OptionsData = new ShorcutOptionsInfo();                        
            DeviceIDE = true;
            DeviceSD = false;
            LanguageList = RetrieveLanguageList();
            ShortcutsSourceList = new ObservableCollection<ComboBoxInfo>(Enum.GetValues(typeof(ShortcutSourceEnum)).Cast<ShortcutSourceEnum>().Select(s => new ComboBoxInfo(s.ToString(), ((int)s).ToString())));
            ImageSizeList = new ObservableCollection<ComboBoxInfo>(Enum.GetValues(typeof(ShortcutImageSizeEnum)).Cast<ShortcutImageSizeEnum>().Select(s => new ComboBoxInfo(s.ToString(), ((int)s).ToString())));
            GameMoreDiskList = new ObservableCollection<ComboBoxInfo>(Enum.GetValues(typeof(GameMoreOneDiskEnum)).Cast<GameMoreOneDiskEnum>().Select(s => new ComboBoxInfo(s.ToString(), ((int)s).ToString())));
            DreamshellVersionList = new ObservableCollection<ComboBoxInfo>(Enum.GetValues(typeof(DreamshellVersionEnum)).Cast<DreamshellVersionEnum>().Select(s => new ComboBoxInfo(s.ToString(), ((int)s).ToString())));
            LoadPartitionDisk();
            LoadData();

            SetLanguage(currentLanguage.LanguageCode);
        }

        private void LoadData()
        {
            ShortcutDataInfo dataInfo = ReadData();

            if (dataInfo != null)
            {
                if (dataInfo.GeneralOptions != null)
                {
                    if (!string.IsNullOrEmpty(dataInfo.GeneralOptions.LanguageCode))
                    {
                        LanguageInfo languageInfo = LanguageList.FirstOrDefault(f => f.LanguageCode == dataInfo.GeneralOptions.LanguageCode);
                        if (languageInfo != null)
                        {
                            LanguageList = RetrieveLanguageList(languageInfo.LanguageCode);
                            CurrentLanguage = LanguageList.FirstOrDefault(f => f.LanguageCode == dataInfo.GeneralOptions.LanguageCode);
                        }
                    }
                }

                if (dataInfo.ShorcutOptions != null)
                {
                    OptionsData = dataInfo.ShorcutOptions;
                    DeviceSD = optionsData.SD;
                    DeviceIDE = !optionsData.SD;
                    GamesFolder = optionsData.GamesFolder;
                    ConfigShortcuts = optionsData.ActiveOption <= 1;
                    ConfigPresets = optionsData.ActiveOption == 2;
                }
            }
            else
            {
                CurrentLanguage = LanguageList[0];
            }

            if (currentLanguage == null)
            {
                CurrentLanguage = LanguageList[0];
            }
        }

        private void LoadPartitionDisk()
        {
            PartitionList = new ObservableCollection<ComboBoxInfo>();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable)
                {
                    PartitionList.Add(new ComboBoxInfo(drive.Name, drive.Name));
                }
            }

            // LUIS VERA: NO SELECCIONABA LA PARTICION, NO ES NECESARIO
            //if (string.IsNullOrEmpty(optionsData.DreamshellPartition)
            //    && partitionList?.Count > 0)
            //{
            //    OptionsData.DreamshellPartition = partitionList[0].Value;
            //}
        }

        private void SetLanguage(string languageCode)
        {
            CultureInfo culture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            //FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), 
            //    new FrameworkPropertyMetadata(Markup.XmlLanguage.GetLanguage(Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)));

            //FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
            //    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            //App.OnStartup(e);
        }

        private string ValidateShortcutData()
        {
            string message = string.Empty;

            if (optionsData != null)
            {
                if (string.IsNullOrEmpty(optionsData.DreamshellPartition))
                {
                    message += $"{i18n.MainWindow.PartitionMessage}\n";
                }

                if (optionsData.MaxSizeName < 0)
                {
                    message += $"{i18n.MainWindow.MaxSizeNameMessage}\n";
                }

                if (optionsData.MaxShortcuts < 0)
                {
                    message += $"{i18n.MainWindow.MaxShortcutsMessage}\n";
                }

                if (string.IsNullOrWhiteSpace(optionsData.GamesFolder))
                {
                    message += $"{i18n.MainWindow.GameFolderMessage}\n";
                }
            }
            else
            {
                message = $"{i18n.MainWindow.ObjectNullMessage}";
            }

            return message;
        }

        private string ValidatePresetData()
        {
            string message = string.Empty;

            if (optionsData != null)
            {
                if (string.IsNullOrEmpty(optionsData.DreamshellPartition))
                {
                    message += $"{i18n.MainWindow.PartitionMessage}\n";
                }

                if (optionsData.DreamshellVersion < 1)
                {
                    message += $"{i18n.MainWindow.DreamshellVersionMessage}\n";
                }

                if (string.IsNullOrWhiteSpace(optionsData.GamesFolder))
                {
                    message += $"{i18n.MainWindow.GameFolderMessage}\n";
                }
            }
            else
            {
                message = $"{i18n.MainWindow.ObjectNullMessage}";
            }

            return message;
        }

        private ObservableCollection<LanguageInfo> RetrieveLanguageList(string languageCode = "es-MX")
        {
            ObservableCollection<LanguageInfo> languageList = null;

            if (languageCode == "es-MX")
            {
                languageList = new ObservableCollection<LanguageInfo>
                {
                    new LanguageInfo { Language = "ESPAÑOL", LanguageCode = "es-MX" },
                    new LanguageInfo { Language = "INGLES", LanguageCode = "en-US" }
                };
            }
            else
            {
                languageList = new ObservableCollection<LanguageInfo>
                {
                    new LanguageInfo { Language = "ENGLISH", LanguageCode = "en-US" },
                    new LanguageInfo { Language = "SPANISH", LanguageCode = "es-MX" }
                };
            }

            return languageList;
        }

        [RelayCommand]
        private async Task Refresh()
        {
            LoadPartitionDisk();
        }

        [RelayCommand]
        private async Task LanguageSelected()
        {
            RetrieveLanguageList(currentLanguage.LanguageCode);
            SetLanguage(currentLanguage.LanguageCode);

            generalData.LanguageCode = currentLanguage.LanguageCode;
        }

        [RelayCommand]
        private async Task CreateShorcuts()
        {
            if (deviceSD)
            {
                OptionsData.SD = true;
            }
            else
            {
                OptionsData.SD = false;
            }

            if (configShortcuts)
            {
                OptionsData.ActiveOption = 1;
            }
            else
            {
                OptionsData.ActiveOption = 2;
            }

            string message = ValidateShortcutData();
            if (string.IsNullOrEmpty(message))
            {
                SaveData();

                optionsData.IsCoversPng = true;
                using (DreamcastShortcutService dreamcast = new DreamcastShortcutService(optionsData))
                {
                    dreamcast.CreateShortcutStructureFile();
                }
                MessageBox.Show(i18n.MainWindow.SaveShorcutsMessage, "DREAMCAST");
            }
            else
            {
                MessageBox.Show(message, "DREAMCAST");
            }
        }

        [RelayCommand]
        private async Task CreatePresets()
        {
            string message = ValidatePresetData();
            if (string.IsNullOrEmpty(message))
            {
                SaveData();

                using (DreamcastShortcutService dreamcast = new DreamcastShortcutService(optionsData))
                {
                    dreamcast.CreatePresetGameList();
                }

                MessageBox.Show(i18n.MainWindow.SavePresetsMessage, "DREAMCAST");
            }
            else
            {
                MessageBox.Show(message, "DREAMCAST");
            }
        }

        [RelayCommand]
        private async Task ChangeLanguage()
        {
            SaveData();
            var oldWindow = Application.Current.MainWindow;
            Application.Current.MainWindow = new MainWindow(new MainWindowVM());
            Application.Current.MainWindow.Show();
            oldWindow.Close();
        }

        private ShortcutDataInfo ReadData()
        {
            ShortcutDataInfo dataInfo = null;

            try
            {
                if (File.Exists(configFile))
                {
                    dataInfo = JsonConvert.DeserializeObject<ShortcutDataInfo>(File.ReadAllText(configFile, utf8WithoutBom));
                }
            }
            catch (Exception ex)
            { 
            }

            return dataInfo;
        }

        [RelayCommand]
        private async Task SearchGameFolder()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();

            bool initialDirectorySet = false;
            if (!string.IsNullOrEmpty(this.gamesFolder)
                && !string.IsNullOrEmpty(optionsData.DreamshellPartition))
            {
                string dreamshellPartition = Path.GetPathRoot(optionsData.DreamshellPartition);
                string gamesPartition = Path.GetPathRoot(this.gamesFolder);
                string gamesFolder = Path.Combine(dreamshellPartition, string.IsNullOrEmpty(gamesPartition) ? this.gamesFolder  : this.gamesFolder.Replace(Path.GetPathRoot(this.gamesFolder), string.Empty));

                if (Directory.Exists(gamesFolder))
                {
                    initialDirectorySet = true;
                    folderBrowser.InitialDirectory = Path.Combine(dreamshellPartition, gamesFolder);
                }                
            }

            if (!initialDirectorySet && !string.IsNullOrEmpty(optionsData.DreamshellPartition))
            {
                folderBrowser.InitialDirectory = optionsData.DreamshellPartition;
            }

            System.Windows.Forms.DialogResult result = folderBrowser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(optionsData.DreamshellPartition))
                {
                    if (Path.GetPathRoot(optionsData.DreamshellPartition) == Path.GetPathRoot(folderBrowser.SelectedPath))
                    {
                        OptionsData.GamesFolder = GamesFolder = folderBrowser.SelectedPath.Replace(Path.GetPathRoot(folderBrowser.SelectedPath), string.Empty);
                    }
                    else
                    {
                        MessageBox.Show(i18n.MainWindow.PartitionGamesMessage, "DREAMCAST");
                    }
                }
                else
                {
                    OptionsData.GamesFolder = GamesFolder = folderBrowser.SelectedPath.Replace(Path.GetPathRoot(folderBrowser.SelectedPath), string.Empty);
                    //OptionsData.GamesFolder = GamesFolder = folderBrowser.SelectedPath;
                }
            }
        }

        private void SaveData()
        {
            if (!string.IsNullOrEmpty(Path.GetPathRoot(gamesFolder)))
            {
                GamesFolder = gamesFolder.Replace(Path.GetPathRoot(gamesFolder), string.Empty);
            }

            OptionsData.SD = DeviceSD;
            OptionsData.GamesFolder = GamesFolder;

            if (configShortcuts)
            {
                OptionsData.ActiveOption = 1;
            }
            else
            {
                OptionsData.ActiveOption = 2;
            }

            ShortcutDataInfo shortcutData = new ShortcutDataInfo
            {
                GeneralOptions = generalData,
                ShorcutOptions = optionsData
            };
            
            if (File.Exists(configFile))
            { 
                File.Delete(configFile);
            }
            
            File.WriteAllText(configFile, JsonConvert.SerializeObject(shortcutData, Formatting.Indented), utf8WithoutBom);
        }
    }
}
