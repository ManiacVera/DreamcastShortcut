using ExcelDataReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DreamcastShortcutApp.DTO.Enum;
using DreamcastShortcutApp.DTO.Info;
using DreamcastShortcutApp.DTO.Constants;
using DreamcastShortcutApp.DTO.Struct;
using System.IO.Compression;
using System.Diagnostics;

namespace DreamcastShortcutApp.Service
{
    public class DreamcastShortcutService : IDisposable
    {
        const string ISO_SHORTCUT_FORMAT = @"module -o -f /ide/DS/modules/minilzo.klf
module -o -f /ide/DS/modules/isofs.klf
module -o -f /ide/DS/modules/isoldr.klf
isoldr{0}
console --show";

        const string PRESET_FORMAT = @"title = [@title]
device = [@device]
dma = [@dma]
async = [@async]
cdda = [@cdda]
irq = [@irq]
low = [@low]
heap = [@heap]
fastboot = [@fastboot]
type = [@type]
mode = [@mode]
memory = [@memory]
vmu = [@vmu]
scrhotkey = [@scrhotkey]
pa1 = [@pa1]
pv1 = [@pv1]
pa2 = [@pa2]
pv2 = [@pv2]
";

        private readonly DreamshellUtilities dsUtilities;
        public const int MAX_SIZE_FILENAME_SHORTCUT = 25;

        static Dictionary<string, string> parameterDictionary = new Dictionary<string, string>
            {
                { "title", "-f"},
                { "device", "-d"}, // AUTO ES EN BLANCO
                { "dma", "-a"},
                { "async", "-e"},
                { "cdda", "-g"},
                { "irq", "-q"},
                { "low", "-l"},
                { "heap", "-h"}, // CONVERTIR A ENTERO CUANDO SEAN SOLO NÚMEROS
                { "fastboot", "-s"}, // -i CUANDO NO ESTA ACTIVADO
                { "type", ""},//os_chk NO SE PARA QUE SE USA
                { "mode", "-j"},
                { "memory", "-x"},
                { "vmu", "-v"}, // DISABLE VMU
                { "scrhotkey", "-k"},
                { "pa1", "-b"}, //patch_a[0] NUNCA LO HE USADO
                { "pv1", ""}, //patch_v[0]
                { "pa2", ""}, //patch_a[1]
                { "pv2", ""} //patch_v[0]
            };

        private ShorcutOptionsInfo options = null;

        public DreamcastShortcutService(ShorcutOptionsInfo options)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            this.options = options;
            dsUtilities = new DreamshellUtilities();

            if (!string.IsNullOrWhiteSpace(options.DreamshellPartition))
            {
                string partition = Path.GetPathRoot(options.DreamshellPartition);
                options.DestinationFolder = partition;
                options.GamesFolder = Path.Combine(partition, options.GamesFolder.Substring(options.GamesFolder.IndexOf('\\') + 1));
                options.PresetFolder = Path.Combine(partition, AppConstants.PRESETS_FOLDER);
                options.CoversFolder = Path.Combine(partition, AppConstants.COVERS_FOLDER);
                options.DreamshellFolderGames = options.GamesFolder.Replace(Path.GetPathRoot(options.GamesFolder), string.Empty).Replace('\\', '/');
            }
            else
            {
                options.DreamshellPartition = Path.GetPathRoot(options.GamesFolder);
            }
        }

        ~DreamcastShortcutService()
        {
            
        }

        public void Dispose()
        {
            dsUtilities.Dispose();
        }

        public static void ConvertPngToJpg(string folder, bool rewrite = true, int width = 0, int height = 0)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".png")
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        string path = Path.GetDirectoryName(file);
                        Image png = Image.FromFile(file);

                        if (width > 0 && height > 0)
                        {
                            png = (Image)ResizeImage(png, width, height);
                        }

                        if (rewrite && File.Exists(path + @"/" + name + ".jpg"))
                        {
                            File.Delete(path + @"/" + name + ".jpg");
                        }

                        png.Save(path + @"/" + name + ".jpg", ImageFormat.Jpeg);
                        png.Dispose();
                        //File.Delete(file);
                    }
                }
            }
        }

        public static void ConvertJpgToPng(string folder, bool rewrite = true, int width = 0, int height = 0)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".jpg")
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        string path = Path.GetDirectoryName(file);
                        Image jpg = Image.FromFile(file);

                        if (width > 0 && height > 0)
                        {
                            jpg = (Image)ResizeImage(jpg, width, height);
                        }

                        if (rewrite && File.Exists(path + @"/" + name + ".png"))
                        {
                            File.Delete(path + @"/" + name + ".png");
                        }

                        jpg.Save(path + @"/" + name + ".png", ImageFormat.Png);
                        jpg.Dispose();
                        //File.Delete(file);
                    }
                }
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static int ConvertRomanToArabicNumber(string romanNumber)
        {
            var values = new Dictionary<char, int>();
            values.Add('I', 1);
            values.Add('V', 5);
            values.Add('X', 10);
            values.Add('L', 50);
            values.Add('C', 100);
            values.Add('D', 500);
            values.Add('M', 1000);

            int result = 0;
            for (int i = 0; i < romanNumber.Length; i++)
            {
                if (i == romanNumber.Length - 1)
                {
                    result += values[romanNumber[i]];
                    break;
                }

                if (values[romanNumber[i]] < values[romanNumber[i + 1]])
                    result -= values[romanNumber[i]];
                else
                    result += values[romanNumber[i]];
            }
            return result;
        }

        public static string ConvertArabicToRomanNumber(int number)
        {

            string[][] romanNumerals = new string[][]
            {
                new string[] { "", "i", "ii", "iii", "iv", "v", "vi", "vii", "viii", "ix" }, // ones
                new string[] { "", "x", "xx", "xxx", "xl", "l", "lx", "lxx", "lxxx", "xc" }, // tens
                new string[] { "", "c", "cc", "ccc", "cd", "d", "dc", "dcc", "dccc", "cm" }, // hundreds
                new string[] { "", "m", "mm", "mmm" } // thousands
            };

            // split integer string into array and reverse array
            char[] intArr = number.ToString().ToCharArray();
            Array.Reverse(intArr);
            int len = intArr.Length;
            string romanNumeral = "";

            // starting with the highest place (for 3046, it would be the thousands 
            // place, or 3), get the roman numeral representation for that place 
            // and append it to the final roman numeral string
            for (int i = len - 1; i >= 0; i--)
            {
                romanNumeral += romanNumerals[i][intArr[i] - '0'];
            }

            return romanNumeral;

        }

        private string ReplaceArabicNumber(string stringToReplace)
        {
            string result = null;
            
            MatchCollection matches = Regex.Matches(stringToReplace, @"[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (matches.Count > 0)
            {
                int displacement = 0;
                result = string.Empty;
                foreach (Match match in matches)
                {
                    result += stringToReplace.Substring(displacement, match.Index - displacement);
                    result += ConvertArabicToRomanNumber(int.Parse(match.Value));
                    displacement = match.Index + match.Value.Length;
                }

                if (displacement < stringToReplace.Length - 1)
                {
                    result += stringToReplace.Substring(displacement);
                }
            }
            else
            {
                result = stringToReplace;
            }

            return result;
        }

        private string ReplaceYearAcronym(string stringToReplace)
        {
            const string DOUBLE_ZERO = "00";
            Regex yearRegex = new Regex(@"(?<LEFT>2)(?<ACRONYM>k)(?<RIGHT>[1-9])|(?<LEFT>2)(?<ACRONYM>k)", RegexOptions.IgnoreCase);
            return yearRegex.Replace(stringToReplace,
                    m => string.Concat(m.Groups["LEFT"].Value, DOUBLE_ZERO, !string.IsNullOrEmpty(m.Groups["RIGHT"].Value) ? m.Groups["RIGHT"].Value : "0"));
        }

        private string SeparateNumbersFromString(string stringToSeparate, string separator = " ")
        {
            string result = null;
            MatchCollection matches = Regex.Matches(stringToSeparate, @"[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (stringToSeparate?.Count() > 0 && separator != null)
            {
                int displacement = 0;
                result = string.Empty;
                foreach (Match match in matches)
                {
                    result += stringToSeparate.Substring(displacement, match.Index - displacement);

                    if (string.IsNullOrEmpty(result))
                    {
                        result += match.Value;
                    }
                    else if (result[result.Length - 1] != ' ')
                    {
                        result += string.Concat(separator, match.Value);
                    }
                    else
                    {
                        result += match.Value;
                    }

                    displacement = match.Index + match.Value.Length;

                    if ((displacement < stringToSeparate.Length - 1)
                        && stringToSeparate.Substring(displacement, 1) != separator)
                    {
                        result = string.Concat(result, separator);
                    }
                }

                if (displacement < stringToSeparate.Length - 1)
                {
                    //if (result[result.Length - 1] != ' ')
                    //{
                    //    result += string.Concat(separator, stringToSeparate.Substring(displacement + 1));
                    //}
                    //else
                    {
                        result += stringToSeparate.Substring(displacement);
                    }
                }

                result = result.Trim();
            }
            else
            {
                result = stringToSeparate;
            }

            return result;
        }

        private int QuantityMatchWord(string file1, string file2, bool exactQuantity = true)
        {
            int quantityMatch = 0;

            file1 = ReplaceArabicNumber(file1);
            file2 = ReplaceArabicNumber(file2);

            MatchCollection matches1 = Regex.Matches(file1, @"[\w]+", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (matches1.Count > 0)
            {
                IEnumerable<string> matches1Enumeable = matches1.Cast<Match>().Select(s => string.Format(@"\b{0}\b", s.Value)).Distinct();

                quantityMatch = Regex.Matches(file2
                    , string.Join("|", matches1Enumeable)
                    , RegexOptions.IgnoreCase | RegexOptions.Multiline).Distinct().Count();

                if (exactQuantity && quantityMatch != matches1Enumeable.Count())
                {
                    quantityMatch = 0;
                }
            }

            return quantityMatch;
        }

        private string GetFirstLetters(string words, int minLength = 3)
        {
            string result = new string(words.Split(' ').Where(f => f != string.Empty).Select(x => x[0]).ToArray());

            if (minLength > 0 && result.Length >= minLength)
            {
                result = result.Substring(0, minLength);
            }
            else
            {
                if (words.Length >= minLength)
                {
                    result = words.Substring(0, minLength);
                }
                else
                {
                    result = words;
                }
            }

            return result;
        }

        private string GameNameWithoutExtension(string game)
        {
            const string FILE_EXTENSION = ".gdi|.cdi|.iso|.png|.jpg|.raw|.pvr|.cfg|.dsc";
            Regex fileExtensionRegex = new Regex(@FILE_EXTENSION, RegexOptions.IgnoreCase);

            game = game.Substring(game.LastIndexOf('\\') + 1);
            game = game.Substring(game.LastIndexOf('/') + 1);
            return fileExtensionRegex.Replace(game, "");
        }

        private string GetFileGame(string folderGame, bool isDirectory = true)
        {
            string fileGame = null;
            if (!string.IsNullOrEmpty(folderGame))
            {
                if (isDirectory)
                {
                    fileGame = Directory.GetFiles(folderGame) 
                        .Where(f => !Regex.IsMatch(f, @"track[0-9]+\.iso"))
                        .Where(f => f.ToLower().EndsWith(".gdi") || f.ToLower().EndsWith(".iso") || f.ToLower().EndsWith(".cdi")).FirstOrDefault();
                }
                else
                {
                    if (!Regex.IsMatch(folderGame, @"track[0-9]+\.iso")
                        && (folderGame.ToLower().EndsWith(".gdi") || folderGame.ToLower().EndsWith(".iso") || folderGame.ToLower().EndsWith(".cdi")))
                    {
                        fileGame = folderGame.Trim();
                    }
                }

                if (fileGame != null)
                {
                    fileGame = Path.GetFileName(fileGame);
                }
            }

            return fileGame;
        }

        private static int ParseInt(string number) 
        {
            return Convert.ToInt32(Convert.ToDouble(number));
        }

        private static DreamshellInfo ConvertPresetToConfig(PresetInfo presetInfo, string titleGame = null, int formatGame = 0)
        {
            DreamshellInfo dreamGameInfo = null;

            if (presetInfo != null)
            {
                const string DEFAULT_MEMORY_DMA = "0x8c000100";
                const string DEFAULT_MEMORY = "0x8c004000";
                const string DEFAULT_VALUE = "00000000";
                string bootMode = "0";
                string bootType = "0";
                CddaModeInfo cddaMode = null;
                string heapMode = DEFAULT_VALUE;
                string async = "0";
                string presetValue = string.Empty;
                //string device = "auto";
                string loaderFile = string.Empty;
                string bootMemory = null;

                if (!string.IsNullOrWhiteSpace(presetInfo.memory))
                {
                    bootMemory = presetInfo.memory.Trim();
                }
                else
                {
                    if (presetInfo.dma == 0)
                    {
                        bootMemory = DEFAULT_MEMORY_DMA;
                    }
                    else
                    {
                        bootMemory = DEFAULT_MEMORY;
                    }
                }

                if (!string.IsNullOrWhiteSpace(presetInfo.mode))
                {
                    switch (Convert.ToInt32(presetInfo.mode))
                    {
                        case (int)IsoLoaderBootModeEnum.BOOT_MODE_IPBIN:
                            bootMode = "IPBIN";
                            break;

                        case (int)IsoLoaderBootModeEnum.BOOT_MODE_IPBIN_TRUNC:
                            bootMode = "IPBINCUT";
                            break;

                        default:
                            bootMode = "DIRECT";
                            break;
                    }
                }
                else
                {
                    bootMode = "DIRECT";
                }

                if (!string.IsNullOrWhiteSpace(presetInfo.type))
                {
                    switch (Convert.ToInt32(presetInfo.type))
                    {
                        case (int)IsoLoaderExecTypeEnum.BIN_TYPE_KATANA:
                            bootType = "KATANA";
                            break;
                        case (int)IsoLoaderExecTypeEnum.BIN_TYPE_WINCE:
                            bootType = "WINCE";
                            break;
                        case (int)IsoLoaderExecTypeEnum.BIN_TYPE_KOS:
                            bootType = "HOMEBREW";
                            break;
                        default:
                            bootType = "AUTO";
                            break;
                    }
                }
                else
                {
                    bootType = "AUTO";
                }

                if (!string.IsNullOrEmpty(presetInfo.cdda) && long.Parse(presetInfo.cdda) > 0)
                {
                    cddaMode = GetModeCDDA(presetInfo.cdda);
                }

                if (presetInfo.dma == 1)
                {
                    if (presetInfo.async > 0)
                    {
                        async = presetInfo.async.ToString();
                    }
                    else
                    {
                        async = "AUTO";
                    }
                }
                else
                {
                    if (presetInfo.async > 0)
                    {
                        async = presetInfo.async.ToString();
                    }
                    else
                    {
                        async = "8";
                    }
                }

                heapMode = GetModeHeap(presetInfo.heap);

                if (!string.IsNullOrWhiteSpace(presetInfo.device))
                {
                    if (presetInfo.device.Trim().ToLower() == "auto")
                    {
                        loaderFile = string.Empty;
                    }
                    else
                    {
                        loaderFile = IsLowLevel(0, presetInfo.device.Trim()) == 1 ? string.Empty : presetInfo.device.Trim();
                    }
                }
                else
                {
                    loaderFile = string.Empty;
                }

                if (string.IsNullOrEmpty(titleGame))
                {
                    titleGame = presetInfo.title;
                }

                dreamGameInfo = new DreamshellInfo
                {
                    DreamshellVersion = DreamshellVersionEnum.DS4_LOADER_V0_8.ToString(),
                    Game = titleGame,
                    Region = "NTSC-U",
                    LoaderVersion = "DS 4.0",
                    LoaderFile = loaderFile,
                    FormatGdi = formatGame == (int)FormatGameEnum.GDI ? 1 : 0,
                    FormatCdi = formatGame == (int)FormatGameEnum.CDI ? 1 : 0,
                    FormatIso = formatGame == (int)FormatGameEnum.ISO ? 1 : 0,
                    BootType = bootType,
                    BootMemory = bootMemory,
                    BootMode = bootMode,
                    FastBoot = presetInfo.fastboot,
                    Irq = presetInfo.irq,
                    Dma = presetInfo.dma,
                    LowLevel = IsLowLevel(presetInfo.low, presetInfo.device),
                    Async = async,
                    HeapMemory = heapMode,
                    Cdda = cddaMode != null ? 1 : 0,
                    CddaSource = cddaMode != null ? cddaMode.Source : string.Empty,
                    CddaDestination = cddaMode != null ? cddaMode.Destination : string.Empty,
                    CddaPosition = cddaMode != null ? cddaMode.Position : string.Empty,
                    CddaChannel = cddaMode != null ? cddaMode.Channel : string.Empty,
                    Vmu = string.IsNullOrWhiteSpace(presetInfo.vmu) ? presetInfo.vmu :  string.Empty,
                    ScrHotkey = string.IsNullOrWhiteSpace(presetInfo.scrhotkey) ? presetInfo.scrhotkey : string.Empty,
                    PatchA1 = string.IsNullOrWhiteSpace(presetInfo.pa1) ? presetInfo.pa1 : string.Empty,
                    PatchA2 = string.IsNullOrWhiteSpace(presetInfo.pv1) ? presetInfo.pv1 : string.Empty,
                    PatchV1 = string.IsNullOrWhiteSpace(presetInfo.pa2) ? presetInfo.pa2 : string.Empty,
                    PatchV2 = string.IsNullOrWhiteSpace(presetInfo.pv2) ? presetInfo.pv2 : string.Empty
                };
            }

            return dreamGameInfo;
        }

        private static PresetInfo ConvertConfigToPreset(DreamshellInfo dreamGameInfo)
        {
            PresetInfo presetInfo = null;

            if (dreamGameInfo != null)
            {
                const string DEFAULT_MEMORY_DMA = "0x8c000100";
                const string DEFAULT_MEMORY = "0x8c004000";
                const string DEFAULT_VALUE = "00000000";
                string mode = "0";
                string type = "0";
                string cddaMode = DEFAULT_VALUE;
                string heapMode = DEFAULT_VALUE;
                string async = "0";
                string presetValue = string.Empty;
                string device = "auto";
                string bootMemory = null;

                if (!string.IsNullOrWhiteSpace(dreamGameInfo.BootMemory))
                {
                    if (dreamGameInfo.BootMemory.ToLower().Trim().Substring(0, 2) == "0x")
                    {
                        bootMemory = dreamGameInfo.BootMemory.Trim();
                    }
                    else
                    {
                        bootMemory = "0x8ce00000";
                    }
                }
                else
                {
                    if (dreamGameInfo.Dma != null && dreamGameInfo.Dma.Value == 1)
                    {
                        bootMemory = DEFAULT_MEMORY_DMA;
                    }
                    else
                    {
                        bootMemory = DEFAULT_MEMORY;
                    }
                }

                switch (dreamGameInfo.BootMode?.ToUpper())
                {
                    case "IPBIN":
                        mode = Convert.ToString((int)IsoLoaderBootModeEnum.BOOT_MODE_IPBIN);
                        break;

                    case "IPBINCUT":
                        mode = Convert.ToString((int)IsoLoaderBootModeEnum.BOOT_MODE_IPBIN_TRUNC);
                        break;
                    default:
                        mode = Convert.ToString((int)IsoLoaderBootModeEnum.BOOT_MODE_DIRECT);
                        break;
                }

                switch (dreamGameInfo.BootType?.ToUpper())
                {
                    case "KATANA":
                        type = Convert.ToString((int)IsoLoaderExecTypeEnum.BIN_TYPE_KATANA);
                        break;
                    case "WINCE":
                        type = Convert.ToString((int)IsoLoaderExecTypeEnum.BIN_TYPE_WINCE);
                        break;
                    case "HOMEBREW":
                    case "KOS":
                        type = Convert.ToString((int)IsoLoaderExecTypeEnum.BIN_TYPE_KOS);
                        break;
                    default:
                        type = Convert.ToString((int)IsoLoaderExecTypeEnum.BIN_TYPE_AUTO);
                        break;
                }

                if (dreamGameInfo.Cdda == 1)
                {
                    cddaMode = GetModeCDDA(dreamGameInfo);
                }

                if (dreamGameInfo.Dma == 1)
                {
                    if (!string.IsNullOrWhiteSpace(dreamGameInfo.Async))
                    {
                        if (dreamGameInfo.Async.ToLower() == "none" || dreamGameInfo.Async.ToLower() == "auto")
                        {
                            async = "0";
                        }
                        else
                        {
                            async = dreamGameInfo.Async.Trim();
                        }
                    }
                    else
                    {
                        async = "0";
                    }
                }
                else
                {
                    async = "8";
                }

                heapMode = GetModeHeap(dreamGameInfo);

                if (!string.IsNullOrWhiteSpace(dreamGameInfo.LoaderFile))
                {
                    if (dreamGameInfo.LoaderFile.Trim().ToLower() == "auto")
                    {
                        device = "auto";
                    }
                    else
                    {
                        device = dreamGameInfo.LoaderFile.Trim();
                    }
                }
                else
                {
                    device = "auto";
                }

                presetInfo = new PresetInfo
                {
                    title = dreamGameInfo.Game.Trim(),
                    device = device,
                    dma = dreamGameInfo.Dma != null ? dreamGameInfo.Dma.Value : 0,
                    async = ParseInt(async),
                    cdda = cddaMode,
                    irq = dreamGameInfo.Irq != null ? dreamGameInfo.Irq.Value : 0,
                    low = dreamGameInfo.LowLevel != null ? IsLowLevel(dreamGameInfo.LowLevel.Value, dreamGameInfo.LoaderFile) : 0,
                    heap = heapMode,
                    fastboot = dreamGameInfo.FastBoot != null ? dreamGameInfo.FastBoot.Value : 0,
                    type = type,
                    mode = mode,
                    memory = bootMemory,
                    vmu = !string.IsNullOrWhiteSpace(dreamGameInfo.Vmu) ? dreamGameInfo.Vmu.Trim() : "0",
                    scrhotkey = !string.IsNullOrWhiteSpace(dreamGameInfo.ScrHotkey) ? dreamGameInfo.ScrHotkey.Trim() : "0",
                    pa1 = !string.IsNullOrWhiteSpace(dreamGameInfo.PatchA1) ? dreamGameInfo.PatchA1.Trim() : DEFAULT_VALUE,
                    pv1 = !string.IsNullOrWhiteSpace(dreamGameInfo.PatchV1) ? dreamGameInfo.PatchV1.Trim() : DEFAULT_VALUE,
                    pa2 = !string.IsNullOrWhiteSpace(dreamGameInfo.PatchA2) ? dreamGameInfo.PatchA2.Trim() : DEFAULT_VALUE,
                    pv2 = !string.IsNullOrWhiteSpace(dreamGameInfo.PatchV2) ? dreamGameInfo.PatchV2.Trim() : DEFAULT_VALUE
                };
            }

            return presetInfo;
        }

        private static List<PresetInfo> GetPresetGameListByConfigFile(string configFile, string dreamshellVersion, bool sd = false)
        {
            List<PresetInfo> presetList = new List<PresetInfo>();

            if (!string.IsNullOrEmpty(dreamshellVersion))
            {
                List<DreamshellInfo> dreamshellInfoList = ReadConfigGameList(dreamshellVersion);
                if (dreamshellInfoList?.Count() > 0)
                {
                    foreach (DreamshellInfo dreamGameInfo in dreamshellInfoList)
                    {
                        PresetInfo presetInfo = ConvertConfigToPreset(dreamGameInfo);
                        if (presetInfo != null)
                        {
                            presetList.Add(presetInfo);
                        }
                    }
                }
            }

            return presetList;
        }

        private static List<PresetInfo> GetPresetGameList(string presetFolder, bool sd = false)
        {
            List<PresetInfo> presetList = new List<PresetInfo>();

            string[] fileArray = Directory.GetFiles(presetFolder, "*.cfg");
            if (fileArray?.Length > 0)
            {
                if (sd)
                {
                    fileArray = fileArray.Where(f => Path.GetFileName(f).StartsWith("sd")).ToArray();
                }
                else
                {
                    fileArray = fileArray.Where(f => Path.GetFileName(f).StartsWith("ide")).ToArray();
                }

                string[] lineSplit = null;
                List<Dictionary<string, object>> presetDictionaryList = new List<Dictionary<string, object>>();

                foreach (string contentFile in fileArray)
                {
                    Dictionary<string, object> presetDictionary = new Dictionary<string, object>();
                    foreach (string line in File.ReadAllLines(contentFile))
                    {
                        lineSplit = line.Split('=');
                        if (lineSplit?.Length > 0 &&
                            !presetDictionary.ContainsKey(lineSplit[0].Trim()))
                        {
                            if (lineSplit?.Length > 1)
                            {
                                presetDictionary.Add(lineSplit[0].Trim(), lineSplit[1].Trim());
                            }
                            else
                            {
                                presetDictionary.Add(lineSplit[0].Trim(), null);
                            }
                        }
                    }

                    if (presetDictionary?.Count > 0)
                    {
                        presetDictionaryList.Add(presetDictionary);
                    }
                }

                if (presetDictionaryList?.Count > 0)
                {
                    presetList = JsonConvert.DeserializeObject<List<PresetInfo>>(JsonConvert.SerializeObject(presetDictionaryList));
                }
            }

            return presetList;
        }

        public static List<DreamshellInfo> ReadConfigGameList(string dsVersion = "DS4_LOADER_V0_8", bool sd = false)
        {
            List<DreamshellInfo> dsInfoList = null;

            if (!string.IsNullOrWhiteSpace(dsVersion))
            {
                string configPath = null;
                string table = null;

                if (sd)
                {
                    configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format(@"{0}\{1}", AppConstants.RESOURCES_FOLDER, AppConstants.SD_DREAMSHELL_CONFIG));
                }
                else
                {
                    configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format(@"{0}\{1}", AppConstants.RESOURCES_FOLDER, AppConstants.IDE_DREAMSHELL_CONFIG));
                }

                //if (dsVersion == DreamshellVersionEnum.DS4_LOADER_V08.ToString())
                //{
                //    table = "DS4_LOADER_V08";
                //}
                //else
                //{
                //    table = "DS4_LOADER_V06";
                //}

                table = dsVersion;

                using (var stream = System.IO.File.Open(configPath, FileMode.Open, FileAccess.Read))
                {
                    IExcelDataReader excelDataReader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
                    var conf = new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = a => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true
                        }
                    };

                    DataSet dataSet = excelDataReader.AsDataSet(conf);
                    dataSet.Tables[table].Columns.Add("DreamshellVersion", typeof(string));

                    DataTable dtCloned = dataSet.Tables[table].Clone();

                    dtCloned.Columns["FastBoot"].DataType = typeof(Int32);
                    dtCloned.Columns["Irq"].DataType = typeof(Int32);
                    dtCloned.Columns["Dma"].DataType = typeof(Int32);
                    dtCloned.Columns["Cdda"].DataType = typeof(Int32);
                    dtCloned.Columns["FormatGdi"].DataType = typeof(Int32);
                    dtCloned.Columns["FormatCdi"].DataType = typeof(Int32);
                    dtCloned.Columns["FormatIso"].DataType = typeof(Int32);
                    //dtCloned.Columns["LoaderVersion"].DataType = typeof(float);

                    foreach (DataRow row in dataSet.Tables[table].Rows)
                    {
                        row["DreamshellVersion"] = dsVersion;
                        dtCloned.ImportRow(row);
                    }

                    dsInfoList = JsonConvert.DeserializeObject<List<DreamshellInfo>>(JsonConvert.SerializeObject(dtCloned));
                }
            }

            return dsInfoList;
        }

        public static string MakePresetFileName()
        {
            //snprintf(filename, sizeof(filename),
            //    "%s/apps/%s/presets/%s_%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x.cfg",
            //    getenv("PATH"), lib_get_name() + 4, dev, md5[0],
            //    md5[1], md5[2], md5[3], md5[4], md5[5],
            //    md5[6], md5[7], md5[8], md5[9], md5[10],
            //    md5[11], md5[12], md5[13], md5[14], md5[15]);

            return null;
        }

        private List<string> ReadDefaultImageList()
        {
            return ReadLinesFile(@"Resources\DefaultImages.txt");
        }

        private List<string> ReadDefaultShortcutList()
        {
            return ReadLinesFile(@"Resources\DefaultShortcuts.txt");
        }

        private List<string> ReadLinesFile(string resourceFile)
        {
            List<string> defaultImageList = new List<string>();
            string defaultImagesTxt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceFile);
            if (File.Exists(defaultImagesTxt))
            {
                defaultImageList = File.ReadAllLines(defaultImagesTxt).ToList();
            }

            return defaultImageList;
        }

        private List<IPBinInfo> ReadIpBinFromGame(string folderGames)
        {
            List<IPBinInfo> ipBinList = new List<IPBinInfo>();

            if (Directory.Exists(folderGames) || File.Exists(folderGames))
            {
                string presetName = string.Empty;
                List<string> fileList = new List<string>();

                if (Directory.Exists(folderGames))
                {
                    fileList = Directory.GetFiles(folderGames, "*.*", SearchOption.AllDirectories)?.Where(f => Path.GetFileName(f).ToLower().StartsWith("track03")).ToList();
                }
                else
                {
                    fileList = new List<string> { File.ReadAllText(folderGames) };
                }

                if (fileList?.Count > 0)
                {
                    IPBinMetaStruct ipBinStruct;
                    foreach (string file in fileList)
                    {
                        presetName = dsUtilities.MakePresetFileName(file);
                        if (!string.IsNullOrEmpty(presetName))
                        {
                            ipBinStruct = dsUtilities.ReadIpDataStruct(file);
                            ipBinList.Add(new IPBinInfo
                            { 
                                PresetName = presetName,
                                HardwareID = ipBinStruct.HardwareID.Trim(),
                                MakerID = ipBinStruct.MakerID.Trim(),
                                DeviceInfo = ipBinStruct.DeviceInfo.Trim(),
                                CountryCodes = ipBinStruct.CountryCodes.Trim(),
                                Ctrl = ipBinStruct.Ctrl.Trim(),
                                Dev = ipBinStruct.Dev.Trim(),
                                VGA = ipBinStruct.VGA.Trim(),
                                WinCE = ipBinStruct.WinCE.Trim(),
                                Unk = ipBinStruct.Unk.Trim(),
                                ProductID = ipBinStruct.ProductID.Trim(),
                                ProductVersion = ipBinStruct.ProductVersion.Trim(),
                                ReleaseDate = ipBinStruct.ReleaseDate.Trim(),
                                BootFile = ipBinStruct.BootFile.Trim(),
                                SoftwareMakerInfo = ipBinStruct.SoftwareMakerInfo.Trim(),
                                Title = ipBinStruct.Title.Trim()
                            });
                        }
                    }
                }
            }

            return ipBinList;
        }

        private bool AlgorithmCompareList(string title, List<string> compareList, ref int resultIndex)
        {
            bool success = false;

            if (!string.IsNullOrWhiteSpace(title))
            {
                if (!StringCompareList(title, compareList, ref resultIndex, OptionStringCompareEnum.EQUAL
                                | OptionStringCompareEnum.START_WITH))
                {
                    if (!ArabicCompareList(title, compareList, ref resultIndex, OptionStringCompareEnum.EQUAL | OptionStringCompareEnum.START_WITH | OptionStringCompareEnum.CONTAINS))
                    {
                        if (!StringCompareListReplaceSymbols(title, compareList, ref resultIndex))
                        {
                            if (!AcronymCompareList(title, compareList, ref resultIndex))
                            {
                                if (!ArabicCompareList(title, compareList, ref resultIndex, OptionStringCompareEnum.WORDS))
                                {
                                }
                                else
                                {
                                    success = true;
                                }
                            }
                            else
                            {
                                success = true;
                            }
                        }
                        else
                        {
                            success = true;
                        }
                    }
                    else
                    {
                        success = true;
                    }
                }
                else
                {
                    success = true;
                }
            }

            return success;
        }

        private bool ArabicCompareList(string compareString, List<string> compareList, ref int resultIndex, OptionStringCompareEnum option = OptionStringCompareEnum.ALL)
        {
            if (!string.IsNullOrWhiteSpace(compareString) && compareList?.Count > 0)
            {
                Regex symbolRegex = new Regex(@"[*'"",&#^@\-.;:?¿+{}[\]\\/=()%$!|]");
                compareString = symbolRegex.Replace(ReplaceArabicNumber(SeparateNumbersFromString(ReplaceYearAcronym(GameNameWithoutExtension(compareString))).ToLower()), string.Empty);
                compareList = compareList.Select(s => symbolRegex.Replace(ReplaceArabicNumber(SeparateNumbersFromString(ReplaceYearAcronym(GameNameWithoutExtension(s))).ToLower()), string.Empty)).ToList();

                Regex spaceRegex = new Regex(@"\s\s+");
                compareString = spaceRegex.Replace(compareString.ToLower(), " ");
                compareList = compareList.Select(s => spaceRegex.Replace(s.ToLower(), " ")).ToList();

                return StringCompareList(compareString, compareList, ref resultIndex, option);
            }
            else
            {
                return false;
            }
        }

        private bool AcronymCompareList(string compareString, List<string> compareList, ref int resultIndex)
        {
            if (!string.IsNullOrWhiteSpace(compareString) && compareList?.Count > 0)
            {
                Regex symbolRegex = new Regex(@"[*'"",&#^@\-.;:?¿+{}[\]\\/=()%$!|]");
                compareString = symbolRegex.Replace(GameNameWithoutExtension(compareString).ToLower(), string.Empty);
                compareList = compareList.Select(s => symbolRegex.Replace(GameNameWithoutExtension(s).ToLower(), string.Empty)).ToList();

                Regex spaceRegex = new Regex(@"\s\s+");
                compareString = spaceRegex.Replace(compareString, " ");
                compareList = compareList.Select(s => spaceRegex.Replace(s.ToLower(), " ")).ToList();

                compareString = GetFirstLetters(compareString.ToLower());
                compareList = compareList.Select(s => GetFirstLetters(s.ToLower())).ToList();

                return StringCompareList(compareString, compareList, ref resultIndex, OptionStringCompareEnum.EQUAL
                    | OptionStringCompareEnum.START_WITH | OptionStringCompareEnum.CONTAINS);
            }
            else
            {
                return false;
            }
        }

        private bool StringCompareListReplaceSymbols(string compareString, List<string> compareList, 
            ref int resultIndex, string simbols = @"[*'"",&#^@\-.;:?¿+{}[\]\\/=()%$!| ]", string replaceWith = "")
        {
            if (!string.IsNullOrWhiteSpace(compareString) && compareList?.Count > 0)
            {
                Regex symbolRegex = new Regex(simbols);
                compareString = symbolRegex.Replace(SeparateNumbersFromString(ReplaceYearAcronym(GameNameWithoutExtension(compareString))).ToLower(), replaceWith);
                compareList = compareList.Select(s => symbolRegex.Replace(SeparateNumbersFromString(ReplaceYearAcronym(GameNameWithoutExtension(s))).ToLower(), replaceWith)).ToList();

                return StringCompareList(compareString, compareList, ref resultIndex, 
                    OptionStringCompareEnum.EQUAL | OptionStringCompareEnum.START_WITH | OptionStringCompareEnum.CONTAINS);
            }
            else
            {
                return false;
            }
        }

        private bool StringCompareList(string compareString, List<string> compareList, ref int resultIndex, OptionStringCompareEnum option = OptionStringCompareEnum.ALL) //, Action<T> successFunction)
        {
            if (!string.IsNullOrWhiteSpace(compareString) && compareList?.Count > 0)
            {
                string result = null;
                compareString = SeparateNumbersFromString(ReplaceYearAcronym(GameNameWithoutExtension(compareString))).ToLower();
                compareList = compareList.Select(s => SeparateNumbersFromString(ReplaceYearAcronym(GameNameWithoutExtension(s))).ToLower()).ToList();

                // VALIDAR IDENTICO
                if (option.HasFlag(OptionStringCompareEnum.ALL) || option.HasFlag(OptionStringCompareEnum.EQUAL))
                {
                    if ((result = compareList.FirstOrDefault(f => f == compareString)) != null)
                    {
                        resultIndex = compareList.IndexOf(result);
                        return true;
                    }
                }

                // VALIDAR SI COMIENZA POR
                if (option.HasFlag(OptionStringCompareEnum.ALL) || option.HasFlag(OptionStringCompareEnum.START_WITH))
                {
                    if ((result = compareList.FirstOrDefault(f => compareString.StartsWith(f))) != null)
                    {
                        resultIndex = compareList.IndexOf(result);
                        return true;
                    }

                    if ((result = compareList.FirstOrDefault(f => f.StartsWith(compareString))) != null)
                    {
                        resultIndex = compareList.IndexOf(result);
                        return true;
                    }
                }

                // VALIDAR SI CONTIENE LA PALABRA
                if (option.HasFlag(OptionStringCompareEnum.ALL) || option.HasFlag(OptionStringCompareEnum.CONTAINS))
                {
                    if ((result = compareList.FirstOrDefault(f => compareString.Contains(f))) != null)
                    {
                        resultIndex = compareList.IndexOf(result);
                        return true;
                    }

                    if ((result = compareList.FirstOrDefault(f => f.Contains(compareString))) != null)
                    {
                        resultIndex = compareList.IndexOf(result);
                        return true;
                    }
                }

                // VALIDAR LA CANTIDAD DE PALABRAS CONTENIDAS
                if (option.HasFlag(OptionStringCompareEnum.ALL) || option.HasFlag(OptionStringCompareEnum.WORDS))
                {
                    const int MIN_QUANTITY = 2;
                    int maxQuantity = 0;
                    int currentQuantity = 0;
                    bool exactQuantityWord = false;

                    foreach (string itemCompare in compareList)
                    {
                        currentQuantity = QuantityMatchWord(compareString, itemCompare, false);
                        if (currentQuantity > 0 && ((exactQuantityWord == false && currentQuantity > maxQuantity) 
                                                    || (exactQuantityWord == true && currentQuantity == maxQuantity)))
                        {
                            maxQuantity = currentQuantity;
                            compareString = itemCompare;

                            if (exactQuantityWord)
                            {
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(compareString) 
                        && ((exactQuantityWord && maxQuantity > 0) || (!exactQuantityWord && currentQuantity >= MIN_QUANTITY)))
                    {
                        result = compareString;
                        resultIndex = compareList.IndexOf(result);
                        return true;
                    }
                }

                //successFunction(result);
            }

            return false;
        }

        private static string GetModeHeap(DreamshellInfo dreamshellInfo)
        {
            uint heap = (uint)IsoLoaderHeapModeEnum.HEAP_MODE_AUTO;

            if (!string.IsNullOrWhiteSpace(dreamshellInfo.HeapMemory))
            {
                dreamshellInfo.HeapMemory = dreamshellInfo.HeapMemory.ToUpper().Trim();

                if (dreamshellInfo.HeapMemory == "AUTO")
                {
                    heap = (uint)IsoLoaderHeapModeEnum.HEAP_MODE_AUTO;
                }
                else if (dreamshellInfo.HeapMemory == "BTL" || dreamshellInfo.HeapMemory == "BEHIND")
                {
                    heap = (uint)IsoLoaderHeapModeEnum.HEAP_MODE_BEHIND;
                }
                else if (dreamshellInfo.HeapMemory == "IH" || dreamshellInfo.HeapMemory == "INGAME")
                {
                    heap = (uint)IsoLoaderHeapModeEnum.HEAP_MODE_INGAME;
                }
                else if (dreamshellInfo.HeapMemory == "MAPLE")
                {
                    heap = (uint)IsoLoaderHeapModeEnum.HEAP_MODE_MAPLE;
                }
                else
                {
                    if (dreamshellInfo.HeapMemory != null)
                    {
                        if (Regex.IsMatch(dreamshellInfo.HeapMemory, @"^\d+$"))
                        {
                            heap = (uint)IsoLoaderHeapModeEnum.HEAP_MODE_AUTO;
                        }
                        else
                        {
                            try
                            {
                                heap = dreamshellInfo.HeapMemory.ToLower().StartsWith("0x")
                                    ? Convert.ToUInt32(dreamshellInfo.HeapMemory.Substring(2), 16)
                                     : Convert.ToUInt32(dreamshellInfo.HeapMemory, 10);
                            }
                            catch (Exception ex)
                            {
                                heap = (uint)IsoLoaderHeapModeEnum.HEAP_MODE_AUTO;
                            }
                        }
                    }
                    else
                    {
                        heap = (uint)IsoLoaderHeapModeEnum.HEAP_MODE_AUTO;
                    }
                }
            }

            return string.Format("{0:X}", heap).ToString().PadLeft(8, '0');

            //return dreamshellInfo.HeapMemory != null && dreamshellInfo.HeapMemory.ToLower().StartsWith("0x")
            //    ? string.Format("0x{0:X}", heap).ToString().PadLeft(8, '0')
            //    : string.Format("{0:X}", heap).ToString().PadLeft(8, '0');
        }

        private static string GetModeHeap(string heap)
        {
            string heapMode = null;
            IsoLoaderHeapModeEnum heapValue;

            if (!string.IsNullOrWhiteSpace(heap))
            {
                heapValue = (IsoLoaderHeapModeEnum)Convert.ToUInt32(heap, 16);

                switch (heapValue)
                {
                    case IsoLoaderHeapModeEnum.HEAP_MODE_AUTO:
                        heapMode = "AUTO";
                        break;

                    case IsoLoaderHeapModeEnum.HEAP_MODE_BEHIND:
                        heapMode = "BTL";
                        break;

                    case IsoLoaderHeapModeEnum.HEAP_MODE_INGAME:
                        heapMode = "INGAME";
                        break;

                    default:
                        {
                            heapMode = string.Format("0x" , heap);
                        }
                        break;
                }
            }
            else
            {
                heap = "AUTO";
            }

            return heapMode;
        }

        private static string GetModeCDDA(DreamshellInfo dreamshellInfo)
        {
            IsoLoaderCddaModeEnum mode = IsoLoaderCddaModeEnum.CDDA_MODE_EXTENDED;

            if (dreamshellInfo.CddaSource?.Trim().ToUpper() == "PIO")
            {
                mode = mode | IsoLoaderCddaModeEnum.CDDA_MODE_SRC_PIO;
            }
            else
            {
                mode = mode | IsoLoaderCddaModeEnum.CDDA_MODE_SRC_DMA;
            }

            if (dreamshellInfo.CddaDestination?.Trim().ToUpper() == "SQ")
            {
                mode = mode | IsoLoaderCddaModeEnum.CDDA_MODE_DST_SQ;
            }
            else
            {
                mode = mode | IsoLoaderCddaModeEnum.CDDA_MODE_DST_DMA;
            }

            if (dreamshellInfo.CddaPosition?.Trim().ToUpper() == "TMU1")
            {
                mode = mode | IsoLoaderCddaModeEnum.CDDA_MODE_POS_TMU1;
            }
            else
            {
                mode = mode | IsoLoaderCddaModeEnum.CDDA_MODE_POS_TMU2;
            }

            if (dreamshellInfo.CddaChannel?.Trim().ToUpper() == "FIXED")
            {
                mode = mode | IsoLoaderCddaModeEnum.CDDA_MODE_CH_FIXED;
            }
            else
            {
                mode = mode | IsoLoaderCddaModeEnum.CDDA_MODE_CH_ADAPT;
            }

            return string.Format("{0:X}", ((uint)mode)).ToString().PadLeft(8, '0');
        }

        private static CddaModeInfo GetModeCDDA(string cdda)
        {
            CddaModeInfo cddaMode = null;
            IsoLoaderCddaModeEnum cddaValue;

            if (!string.IsNullOrWhiteSpace(cdda))
            {
                cddaMode = new CddaModeInfo();
                cddaValue = (IsoLoaderCddaModeEnum)Convert.ToUInt32(cdda, 16);

                if (cddaValue.HasFlag(IsoLoaderCddaModeEnum.CDDA_MODE_SRC_PIO))
                {
                    cddaMode.Source = "PIO";
                }
                else //(cddaValue.HasFlag(IsoLoaderCddaModeEnum.CDDA_MODE_SRC_DMA))
                {
                    cddaMode.Source = "DMA";
                }

                if (cddaValue.HasFlag(IsoLoaderCddaModeEnum.CDDA_MODE_DST_SQ))
                {
                    cddaMode.Destination = "SQ";
                }
                else //(cddaValue.HasFlag(IsoLoaderCddaModeEnum.CDDA_MODE_DST_DMA))
                {
                    cddaMode.Destination = "DMA";
                }

                if (cddaValue.HasFlag(IsoLoaderCddaModeEnum.CDDA_MODE_POS_TMU1))
                {
                    cddaMode.Position = "TMU1";
                }
                else //(cddaValue.HasFlag(IsoLoaderCddaModeEnum.CDDA_MODE_POS_TMU2))
                {
                    cddaMode.Position = "TMU2";
                }

                if (cddaValue.HasFlag(IsoLoaderCddaModeEnum.CDDA_MODE_CH_FIXED))
                {
                    cddaMode.Channel = "FIXED";
                }
                else //(cddaValue.HasFlag(IsoLoaderCddaModeEnum.CDDA_MODE_CH_ADAPT))
                {
                    cddaMode.Channel = "ADAPTIVE";
                }
            }

            return cddaMode;
        }

        private string GetDefaultCover(ShortcutValueInfo shortcut)
        {
            string coverFile = null;
            string defaultCovert = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\gd.png");
            string pvrCovers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pvr");
            string pvcCoverPath = Path.Combine(pvrCovers, Path.GetFileNameWithoutExtension(shortcut.GameFileName));
            string defaultPvr = Path.Combine(pvrCovers, "0GDTEX.PVR");

            if (File.Exists(string.Concat(pvcCoverPath, ".png")))
            {
                coverFile = string.Concat(pvcCoverPath, ".png");
            }
            else
            {
                if (!Directory.Exists(pvrCovers))
                {
                    Directory.CreateDirectory(pvrCovers);
                }

                string gdiTools = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    AppConstants.GDI_TOOLS_FOLDER, AppConstants.GDI_TOOLS);

                Process runProcess = null;
                ProcessStartInfo processGdi = new ProcessStartInfo(gdiTools, 
                    string.Format(" -i \"{0}\" -e 0GDTEX.PVR -o \"{1}\"", shortcut.GameFullPath, pvrCovers))
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                runProcess = Process.Start(processGdi);
                runProcess.WaitForExit();

                if (File.Exists(defaultPvr))
                {
                    string pvrTools = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        AppConstants.GDI_TOOLS_FOLDER, AppConstants.PVR_2_PNG);

                    ProcessStartInfo processPvr = new ProcessStartInfo(pvrTools,
                        string.Format("\"{0}\" \"{1}\"", defaultPvr, string.Concat(pvcCoverPath, ".png")))
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    runProcess = Process.Start(processPvr);
                    runProcess.WaitForExit();

                    File.Delete(defaultPvr);
                }

                if (File.Exists(string.Concat(pvcCoverPath, ".png")))
                {
                    coverFile = string.Concat(pvcCoverPath, ".png");
                }
                else if (File.Exists(defaultCovert))
                {
                    coverFile = defaultCovert;
                }
            }

            return coverFile;
        }

        private Dictionary<string, ShortcutValueInfo> CreateShortcutGameFolder()
        {
            Dictionary<string, ShortcutValueInfo> shortcutDictionary = null;
            string[] directoryArray = Directory.GetDirectories(options.GamesFolder, "*", SearchOption.AllDirectories);
            if (directoryArray?.Length > 0)
            {
                if (options.DreamshellFolderGames != null)
                {
                    if (options.DreamshellFolderGames[options.DreamshellFolderGames.Length - 1] == '/')
                    {
                        options.DreamshellFolderGames = options.DreamshellFolderGames.Substring(0, options.DreamshellFolderGames.Length - 1);
                    }

                    if (options.DreamshellFolderGames[0] == '/')
                    {
                        options.DreamshellFolderGames = options.DreamshellFolderGames.Substring(1);
                    }

                    if (options.SD)
                    {
                        options.DreamshellFolderGames = "/sd/" + options.DreamshellFolderGames.Trim();
                    }
                    else
                    {
                        options.DreamshellFolderGames = "/ide/" + options.DreamshellFolderGames.Trim();
                    }
                }

                const string ISOLDR_FORMAT = "[@fastboot][@low][@dma][@irq][@async][@device][@memory][@title][@mode][@cdda][@heap]";
                List<PresetInfo> presetList = null; //GetPresetGameList(options.PresetFolder, options.SD);
                string fileGame = null;
                string titleGame = null;
                PresetInfo presetInfo = null;
                string title = null;
                string cdda = null;
                string heap = null;
                string device = null;
                string memory = null;
                string isoShortcut = null;
                string directoryNameAux = null;
                string onlyDirectoryGame = null;

                ShortcutSourceEnum sourceEnum = (ShortcutSourceEnum)options.ShortcutsSource;
                if (sourceEnum == ShortcutSourceEnum.DREAMSHELL)
                {
                    presetList = GetPresetGameList(options.PresetFolder, options.SD);
                }
                else
                {
                    presetList = new List<PresetInfo>();
                    foreach (DreamshellVersionEnum enumerator in Enum.GetValues(typeof(DreamshellVersionEnum))
                        .Cast<DreamshellVersionEnum>().OrderByDescending(o => (int)o))
                    {
                        if ((int)enumerator <= options.DreamshellVersion)
                        {
                            presetList.AddRange(GetPresetGameListByConfigFile(options.PresetFolder, enumerator.ToString(), options.SD));
                        }
                    }
                }

                shortcutDictionary = new Dictionary<string, ShortcutValueInfo>();
                int resultIndexCompare = -1;
                presetList = presetList.OrderByDescending(o => o.title).ToList();
                List<string> dreamshellGamesList = presetList.Select(s => s.title).ToList();
                IPBinMetaStruct iPBinMetaStruct;

                foreach (string directoryGame in directoryArray)
                {
                    title = string.Empty;
                    fileGame = GetFileGame(directoryGame, true);
                    resultIndexCompare = -1;
                    presetInfo = null;

                    iPBinMetaStruct = dsUtilities.ReadIpDataStruct(Path.Combine(directoryGame, AppConstants.TRACK03_ISO));
                    if (!string.IsNullOrWhiteSpace(iPBinMetaStruct.Title))
                    {
                        titleGame = iPBinMetaStruct.Title;
                    }
                    else
                    {
                        iPBinMetaStruct = dsUtilities.ReadIpDataStruct(Path.Combine(directoryGame, AppConstants.TRACK03_BIN));
                        if (!string.IsNullOrWhiteSpace(iPBinMetaStruct.Title))
                        {
                            titleGame = iPBinMetaStruct.Title;
                        }
                    }

                    //if (fileGame.ToLower().Contains("tennis"))
                    //{
                    //    int i = 0;
                    //}

                    if (!AlgorithmCompareList(titleGame, dreamshellGamesList, ref resultIndexCompare))
                    {
                        if (!AlgorithmCompareList(fileGame, dreamshellGamesList, ref resultIndexCompare))
                        {
                            continue;
                        }
                    }

                    if (resultIndexCompare >= 0)
                    {
                        presetInfo = presetList[resultIndexCompare];
                        if (!string.IsNullOrEmpty(presetInfo?.title))
                        {
                            device = null;
                            cdda = null;
                            heap = null;

                            // LUIS VERA, REVISAR SI SOLO DEJA LAS CARPETAS HASTA DONDE ESTA EL JUEGO
                            directoryNameAux = directoryGame.Substring(options.GamesFolder.Length + 1);
                            if (directoryNameAux[0] == '\\')
                            {
                                directoryNameAux = directoryNameAux.Substring(1);
                            }

                            if (directoryNameAux[directoryNameAux.Length - 1] == '\\')
                            {
                                directoryNameAux = directoryNameAux.Substring(0, directoryNameAux.Length - 1);
                            }

                            onlyDirectoryGame = directoryGame.Substring(directoryGame.LastIndexOf('\\') + 1);

                            //if (options.UseGamesFolder)
                            //{
                            //    title = " -f " + string.Format("{0}/{1}", directoryGame, fileGame).Replace('\\', '/').Replace(' ', '\\');
                            //}
                            //else
                            {
                                title = " -f " + string.Format("{0}/{1}/{2}", options.DreamshellFolderGames, directoryNameAux, fileGame).Replace(' ', '\\');
                            }

                            if (!string.IsNullOrEmpty(presetInfo.cdda?.Replace("0", string.Empty)))
                            {
                                cdda = string.Format(" -g 0x{0}", presetInfo.cdda.Trim());
                            }
                            else
                            {
                                cdda = string.Empty;
                            }

                            if (!string.IsNullOrEmpty(presetInfo.heap?.Replace("0", string.Empty)))
                            {
                                heap = " -h 1";
                            }
                            else
                            {
                                heap = " -h 0";
                            }

                            if (!string.IsNullOrEmpty(presetInfo.device) && presetInfo.device != "auto")
                            {
                                device = string.Format(" -d {0}", presetInfo.device.Trim());
                            }
                            else
                            {
                                device = string.Empty;
                            }

                            if (!string.IsNullOrEmpty(presetInfo.memory))
                            {
                                if (presetInfo.memory.ToLower().Trim().Substring(0, 2) == "0x")
                                {
                                    memory = string.Format(" -x {0}", presetInfo.memory.Trim());
                                }
                                else
                                {   
                                    memory = " -x 0x8ce00000";
                                }
                            }
                            else
                            {
                                memory = " -x 0x8c004000";
                            }

                            isoShortcut = ISOLDR_FORMAT.Replace("[@fastboot]", presetInfo.fastboot == 1 ? " -s" : " -i")
                                .Replace("[@low]", presetInfo.low == 1 ? " -l" : string.Empty)
                                .Replace("[@dma]", presetInfo.dma == 1 ? " -a" : string.Empty)
                                .Replace("[@device]", device)
                                .Replace("[@irq]", presetInfo.irq == 1 ? " -q" : string.Empty)
                                .Replace("[@async]", presetInfo.async > 0 ? string.Format(" -e {0}", presetInfo.async) : string.Empty)
                                .Replace("[@memory]", memory)
                                .Replace("[@title]", title)
                                .Replace("[@mode]", !string.IsNullOrEmpty(presetInfo.mode) ? string.Format(" -j {0}", presetInfo.mode) : string.Empty)
                                .Replace("[@cdda]", cdda)
                                .Replace("[@heap]", heap);                            

                            if (!shortcutDictionary.ContainsKey(onlyDirectoryGame))
                            {
                                shortcutDictionary.Add(onlyDirectoryGame.ToUpper(),
                                    new ShortcutValueInfo
                                    {
                                        PresetValue = string.Format(ISO_SHORTCUT_FORMAT, isoShortcut).Replace("\r\n", "\n"),
                                        GameTitle = title?.Trim(),
                                        GameFileName = fileGame,
                                        GameFullPath = string.Format("{0}/{1}/{2}", options.GamesFolder, directoryNameAux, fileGame)
                                    });
                            }
                        }
                    }
                }
            }

            return shortcutDictionary;
        }

        public static string DumpPresetsConfigFormat(string presetFolder, bool sd)
        {
            string content = null;
            List<PresetInfo> presetList = GetPresetGameList(presetFolder, sd);

            if (presetList?.Count() > 0)
            {
                List<DreamshellInfo> dreamGameList = new List<DreamshellInfo>();
                DreamshellInfo dreamGame = null;

                foreach (PresetInfo preset in presetList)
                {
                    if ((dreamGame = ConvertPresetToConfig(preset)) != null)
                    {
                        dreamGameList.Add(dreamGame);
                    }
                }

                content = string.Join(System.Environment.NewLine, dreamGameList.OrderBy(o => o.Game).Select(s => 
                        string.Join(";", s.Game, s.Region, s.LoaderVersion, s.LoaderFile, s.FormatGdi.Value, s.FormatCdi.Value, 
                        s.FormatIso.Value, s.BootType, s.BootMemory, s.BootMode, s.FastBoot.Value, 
                        s.Irq.Value, s.Dma.Value, s.LowLevel.Value, s.Async, s.HeapMemory, s.Cdda.Value, s.CddaSource, s.CddaDestination,
                        s.CddaPosition, s.CddaChannel, s.Vmu, s.ScrHotkey, s.PatchA1, s.PatchA2, s.PatchV1, s.PatchV2)));
            }

            return content;

            //if (!string.IsNullOrWhiteSpace(folderGames))
            //{
            //    List<string> gameList = null;
            //    FileAttributes fileAttributes = File.GetAttributes(folderGames);

            //    if (fileAttributes.HasFlag(FileAttributes.Directory) && Directory.Exists(folderGames))
            //    {
            //        gameList = Directory.GetFiles(folderGames, "*",SearchOption.AllDirectories)
            //            .Where(f => !f.ToLower().StartsWith("track") && (Path.GetExtension(f.ToLower()) == "gdi" 
            //            || Path.GetExtension(f.ToLower()) == "iso" || Path.GetExtension(f.ToLower()) == "cdi")).ToList();
            //    }
            //    else if (File.Exists(folderGames))
            //    {
            //        gameList = new List<string> { folderGames };
            //    }

            //    foreach (string game in gameList)
            //    {
            //        GetPresetGameList(game);
            //    }
            //}
        }

        public void CreateShortcutStructureFile()
        {
            Dictionary<string, ShortcutValueInfo> shortcutDictionary = CreateShortcutGameFolder();
            if (shortcutDictionary?.Count > 0)
            {
                CopyFirmware(options.DestinationFolder, options.SD);

                //const string FILE_NAME_REGEX = @"-f\ (?<GAME>(?<NAME>[a-zA-Z0-9\\/_\-]+)(?<EXTENSION>\.gdi|\.iso|\.cdi))";
                List<string> gameCountList = null;
                if (options.GameMoreOneDisk == (int)GameMoreOneDiskEnum.OMIT)
                {
                    gameCountList = shortcutDictionary.GroupBy(g => g.Value.GameFileName).Where(f => f.Count() > 1).Select(s => s.Key).ToList();
                    shortcutDictionary = shortcutDictionary.Where(f => !gameCountList.Contains(f.Value.GameFileName)).ToDictionary(x => x.Key, x => x.Value);
                }

                if (options.RandomShortcuts)
                {
                    Random random = new Random();
                    shortcutDictionary = shortcutDictionary.OrderBy(s => random.NextDouble()).ToDictionary(x => x.Key, x => x.Value);
                }

                if (options.MaxShortcuts > 0)
                {
                    shortcutDictionary = shortcutDictionary.Take(options.MaxShortcuts).ToDictionary(x => x.Key, x => x.Value);

                    if (options.GameMoreOneDisk == (int)GameMoreOneDiskEnum.TAKE_ALL_SAME)
                    {
                        //var gameCount = shortcutDictionary.GroupBy(g => g.Value.GameFileName).Where(f => f.Count() > 1).Select(s => new { GameFileName = s.Key, Count = s.Count() });

                        //var shortcutFiltered = shortcutDictionary.Where(f => gameCount.Any(a => a.GameFileName == f.Value.GameFileName));
                        //if (shortcutFiltered.Count() > 0)
                        //{
                        //    int quantityDelete = gameCount.Where(f => shortcutFiltered.Any(a => a.Value.GameFileName == f.GameFileName)).Sum(s => s.Count);

                        //}
                    }
                }

                const string DS_APPS_SCRIPTS_FOLDER = "DS/apps/main/scripts";
                const string DS_APPS_IMAGES_FOLDER = "DS/apps/main/images";

                if (!Directory.Exists(Path.Combine(options.DestinationFolder, DS_APPS_SCRIPTS_FOLDER)))
                {
                    Directory.CreateDirectory(Path.Combine(options.DestinationFolder, DS_APPS_SCRIPTS_FOLDER));
                }

                if (!Directory.Exists(Path.Combine(options.DestinationFolder, DS_APPS_IMAGES_FOLDER)))
                {
                    Directory.CreateDirectory(Path.Combine(options.DestinationFolder, DS_APPS_IMAGES_FOLDER));
                }

                const string FORMAT_DSC_FILE = "{0}.dsc";
                const string FORMAT_IMAGE_FILE = "{0}.{1}";
                string imageType = options.IsCoversPng ? "png" : "jpg";
                string fullPathFile = null;
                string fullPathCoverFile = null;
                string[] coverFileArray = null;
                string coverFile = null;
                if (!string.IsNullOrEmpty(options.CoversFolder))
                {
                    coverFileArray = Directory.GetFiles(options.CoversFolder).OrderByDescending(o => o).ToArray();
                }

                Encoding utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                string shortcutName = "";
                List<string> shotcutSaveList = new List<string>();
                int shortcutSaveCount = 0;
                int shortcutCount = 0;
                DateTime currentDateTime = DateTime.Now;
                DateTime newDateTime = new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, 0, 0, 0);

                if (options.RemoveShortcuts)
                {
                    {
                        string resourceFolder = Path.Combine(options.DestinationFolder, DS_APPS_IMAGES_FOLDER);
                        if (Directory.Exists(resourceFolder))
                        {
                            List<string> defaultList = ReadDefaultImageList();
                            string[] fileArray = Directory.GetFiles(resourceFolder);

                            if (fileArray != null)
                            {
                                foreach (string itemFile in fileArray.Where(f => Path.GetExtension(f) == ".png" || Path.GetExtension(f) == ".jpg"))
                                {
                                    if (!defaultList.Any(a => Path.GetFileNameWithoutExtension(a).ToLower() == Path.GetFileNameWithoutExtension(itemFile).ToLower()))
                                    {
                                        File.Delete(itemFile);
                                    }
                                }
                            }
                        }
                    }

                    {
                        string resourceFolder = Path.Combine(options.DestinationFolder, DS_APPS_SCRIPTS_FOLDER);
                        if (Directory.Exists(resourceFolder))
                        {
                            List<string> defaultList = ReadDefaultShortcutList();
                            string[] fileArray = Directory.GetFiles(resourceFolder, "*.dsc");

                            if (fileArray != null)
                            {
                                foreach (string itemFile in fileArray)
                                {
                                    if (!defaultList.Any(a => Path.GetFileNameWithoutExtension(a).ToLower() == Path.GetFileNameWithoutExtension(itemFile).ToLower()))
                                    {
                                        File.Delete(itemFile);
                                    }
                                }
                            }
                        }
                    }
                }

                int resultIndexCompare = -1;
                foreach (KeyValuePair<string, ShortcutValueInfo> shortcut in shortcutDictionary.OrderBy(o => o.Key))
                {
                    if (options.MaxSizeName > 0)
                    {
                        if (shortcut.Key.Length > options.MaxSizeName)
                        {
                            shortcutName = shortcut.Key.Substring(0, options.MaxSizeName - 4).Trim();
                            if (shotcutSaveList.Any(f => f == shortcutName))
                            {
                                shortcutSaveCount = shotcutSaveList.Where(f => f == shortcutName).Count() + 1;
                                shortcutName = string.Format("{0} GD{1}", shortcutName, shortcutSaveCount);
                            }
                        }
                        else
                        {
                            shortcutName = shortcut.Key;
                        }

                        shotcutSaveList.Add(shortcutName);
                    }
                    else
                    {
                        shortcutName = shortcut.Key;
                    }

                    Regex symbolRegex = new Regex(@"[*'"",&#^@\-;?¿+{}[\]=()%$!|]");
                    Regex symbolCoverRegex = new Regex(@"[*'"",&#^@\-.;:?¿+{}[\]\\/=()%$!| ]");

                    fullPathFile = Path.Combine(options.DestinationFolder, DS_APPS_SCRIPTS_FOLDER, options.ShowName ? string.Format(FORMAT_DSC_FILE, shortcutName) : "_" + string.Format(FORMAT_DSC_FILE, shortcutName));
                    fullPathFile = symbolRegex.Replace(fullPathFile, " ");

                    if (File.Exists(fullPathFile))
                    {
                        File.Delete(fullPathFile);
                    }
                    File.WriteAllText(fullPathFile, shortcut.Value.PresetValue, utf8WithoutBom);
                    File.SetCreationTime(fullPathFile, newDateTime.AddMinutes(shortcutCount));
                    File.SetLastWriteTime(fullPathFile, newDateTime.AddMinutes(shortcutCount));

                    //if (shortcut.Key.ToLower().Contains("tennis"))
                    //{
                    //    int i = 0;
                    //}

                    resultIndexCompare = -1;
                    
                    if (!AlgorithmCompareList(shortcut.Key, coverFileArray.ToList(), ref resultIndexCompare))
                    {
                        // SI NO ENCONTRO EL NOMBRE ENTOCES BUSCARLO POR EL GDI, ISO, CDI
                        if (!AlgorithmCompareList(shortcut.Value.GameFileName, coverFileArray.ToList(), ref resultIndexCompare))
                        {
                            // PRIMERO BUSCA SOBRE EL TITULO DEL JUEGO ENCONTRADO EN EL TRACK03
                            if (!AlgorithmCompareList(shortcut.Value.GameTitle, coverFileArray.ToList(), ref resultIndexCompare))
                            {
                                //continue;
                            }
                        }
                    }

                    if (resultIndexCompare >= 0)
                    {
                        coverFile = coverFileArray[resultIndexCompare];
                    }
                    else
                    {
                        coverFile = GetDefaultCover(shortcut.Value);                        
                    }

                    if (!string.IsNullOrEmpty(coverFile))
                    {
                        Image coverImage = Image.FromFile(coverFile);
                        if (coverImage != null)
                        {
                            if (options.ImageSize != (int)ShortcutImageSizeEnum.DEFAULT_SIZE
                                && Enum.IsDefined(typeof(ShortcutImageSizeEnum), options.ImageSize))
                            {
                                coverImage = (Image)ResizeImage(coverImage, options.ImageSize, options.ImageSize);
                            }

                            fullPathCoverFile = Path.Combine(options.DestinationFolder, DS_APPS_IMAGES_FOLDER, options.ShowName ? string.Format(FORMAT_IMAGE_FILE, shortcutName, imageType) : "_" + string.Format(FORMAT_IMAGE_FILE, shortcutName, imageType));
                            fullPathCoverFile = symbolRegex.Replace(fullPathCoverFile, " ");

                            if (File.Exists(fullPathCoverFile))
                            {
                                File.Delete(fullPathCoverFile);
                            }

                            // ES FORZOSO GUARDARLA EN PNG, DREAMSHELL NO ACEPTA JPG EN SHORTCUT
                            coverImage.Save(fullPathCoverFile, ImageFormat.Png);
                            coverImage.Dispose();

                            File.SetCreationTime(fullPathCoverFile, newDateTime.AddMinutes(shortcutCount));
                            File.SetLastWriteTime(fullPathCoverFile, newDateTime.AddMinutes(shortcutCount));
                        }
                    }
                    shortcutCount++;
                }
            }
        }

        public void CreatePresetGameList()
        {
            if (options.DreamshellVersion > 0 && !string.IsNullOrWhiteSpace(options.DreamshellFolderGames))
            {
                DreamshellVersionEnum dreamshellVersion = (DreamshellVersionEnum)options.DreamshellVersion;
                List<DreamshellInfo> dreamshellInfoList = new List<DreamshellInfo>();

                foreach (DreamshellVersionEnum enumerator in Enum.GetValues(typeof(DreamshellVersionEnum))
                    .Cast<DreamshellVersionEnum>().OrderByDescending(o => (int)o))
                {
                    if ((int)enumerator <= options.DreamshellVersion)
                    {
                        dreamshellInfoList.AddRange(ReadConfigGameList(enumerator.ToString()));
                    }
                }

                List<IPBinInfo> ipBinList = ReadIpBinFromGame(options.GamesFolder);
                if (dreamshellInfoList?.Count > 0 && ipBinList?.Count > 0)
                {
                    CopyFirmware(options.DestinationFolder, options.SD);

                    const string DS_APPS_PRESETS_FOLDER = "DS/apps/iso_loader/presets";
                    const string DS_APPS_PRESETS_BACKUP_FOLDER = "DS/apps/iso_loader";
                    string fullPathFile = string.Empty;
                    dreamshellInfoList = dreamshellInfoList.OrderByDescending(o => o.Game).ToList();
                    List<string> dreamshellGamesList = dreamshellInfoList.Select(s => s.Game).ToList();
                    int resultIndexCompare = -1;
                    string presetValue = string.Empty;
                    Encoding utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    int presetCount = 0;
                    DateTime currentDateTime = DateTime.Now;
                    DateTime newDateTime = new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, 0, 0, 0);

                    if (options.OverwritePreset
                        && Directory.Exists(Path.Combine(options.DestinationFolder, DS_APPS_PRESETS_FOLDER))
                        && !File.Exists(Path.Combine(options.DestinationFolder, DS_APPS_PRESETS_BACKUP_FOLDER, "presets_bk.zip")))
                    {
                        ZipFile.CreateFromDirectory(Path.Combine(options.DestinationFolder, DS_APPS_PRESETS_FOLDER)
                            , Path.Combine(options.DestinationFolder, DS_APPS_PRESETS_BACKUP_FOLDER, "presets_bk.zip"));
                    }

                    PresetInfo presetInfo = null;
                    foreach (IPBinInfo ipBin in ipBinList.OrderBy(s => s.Title))
                    {
                        resultIndexCompare = -1;

                        if (!AlgorithmCompareList(ipBin.Title, dreamshellGamesList, ref resultIndexCompare))
                        {
                            continue;
                        }

                        if (resultIndexCompare >= 0)
                        {
                            presetInfo = ConvertConfigToPreset(dreamshellInfoList[resultIndexCompare]);

                            presetValue = PRESET_FORMAT.Replace("[@title]", ipBin.Title)
                                    .Replace("[@device]", presetInfo.device)
                                    .Replace("[@dma]", presetInfo.dma.ToString())
                                    .Replace("[@async]", presetInfo.async.ToString())
                                    .Replace("[@cdda]", presetInfo.cdda)
                                    .Replace("[@irq]", presetInfo.irq.ToString())
                                    .Replace("[@low]", presetInfo.low.ToString())
                                    .Replace("[@heap]", presetInfo.heap)
                                    .Replace("[@fastboot]", presetInfo.fastboot.ToString())
                                    .Replace("[@type]", presetInfo.type)
                                    .Replace("[@mode]", presetInfo.mode)
                                    .Replace("[@memory]", presetInfo.memory)
                                    .Replace("[@vmu]", presetInfo.vmu)
                                    .Replace("[@scrhotkey]", presetInfo.scrhotkey)
                                    .Replace("[@pa1]", presetInfo.pa1)
                                    .Replace("[@pv1]", presetInfo.pv1)
                                    .Replace("[@pa2]", presetInfo.pa2)
                                    .Replace("[@pv2]", presetInfo.pv2);

                            presetValue = presetValue.Replace("\r\n", "\n");

                            //if (presetInfo.title.ToLower().Contains("shenmue"))
                            //{
                            //    int i = 0;
                            //}

                            //fullPathFile = Path.Combine(@"C:\Users\c.lverar\Desktop\Main DS\presetsCreated", ipBin.PresetName);
                            fullPathFile = Path.Combine(options.DestinationFolder, DS_APPS_PRESETS_FOLDER, ipBin.PresetName);

                            if (options.OverwritePreset)
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(fullPathFile)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(fullPathFile));
                                }

                                if (File.Exists(fullPathFile))
                                {
                                    File.Delete(fullPathFile);
                                }

                                File.WriteAllText(fullPathFile, presetValue, utf8WithoutBom);
                                File.SetCreationTime(fullPathFile, newDateTime.AddMinutes(presetCount));
                                File.SetLastWriteTime(fullPathFile, newDateTime.AddMinutes(presetCount));
                                presetCount++;

                            }
                        }
                    }
                }
            }
        }

        public static void CopyFirmware(string destinationFolder, bool sd = false)
        {
            string dsFirmwareFolder = Path.Combine(destinationFolder, AppConstants.DS_FIRMWARE_FOLDER);
            string firmwareFolder = null;

            if (sd)
            {
                firmwareFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.FIRMWARE_SD_FOLDER);
            }
            else
            {
                firmwareFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.FIRMWARE_IDE_FOLDER);
            }

            if (!Directory.Exists(dsFirmwareFolder))
            {
                Directory.CreateDirectory(dsFirmwareFolder);
            }

            if (!Directory.Exists(firmwareFolder))
            {
                Directory.CreateDirectory(firmwareFolder);
            }

            string[] fileArray = Directory.GetFiles(firmwareFolder, "*.bin");
            if (fileArray?.Length > 0)
            {
                foreach (string firmware in fileArray)
                {
                    File.Copy(firmware, Path.Combine(dsFirmwareFolder, Path.GetFileName(firmware)), true);
                }
            }
        }

        private static int IsLowLevel(int lowLevel, string versionLoader)
        {
            string[] lowLevelArray = { "7", "8", "9", "10" };
            return lowLevel == 1 || lowLevelArray.Contains(versionLoader?.Trim()) ? 1 : 0;
        }
    }
}
