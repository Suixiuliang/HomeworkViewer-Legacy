#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HomeworkViewer
{
    public enum ImageFillMode
    {
        Fill, Uniform, UniformToFill, Stretch, Tile, Center
    }

    public class EveningClassTime
    {
        public string Start { get; set; }
        public string End { get; set; }
    }

    public class AppConfig
    {
        public bool EnableGlassReflection { get; set; } = true;
        public string LastMode { get; set; } = "大理";
        public int FontSizeLevel { get; set; } = 1;
        public int CardOpacity { get; set; } = 15;
        public bool FontColorWhite { get; set; } = true;
        public string BarColor { get; set; } = "128,255,255";
        public int EveningClassCount { get; set; } = 3;
        public List<EveningClassTime> EveningClassTimes { get; set; } = new();
        public int ScrollSpeed { get; set; } = 30;
        public int AutoPageInterval { get; set; } = 10;
        public Dictionary<string, string> ClassReps { get; set; } = new();
        public string BackgroundEffect { get; set; } = "Mica";
        public int UpdatePending { get; set; } = 0;
        public bool ShowDueTime { get; set; } = true;
        public bool ShowMouseGlow { get; set; } = true;
        public bool UseBackgroundImage { get; set; } = false;
        public string BackgroundImagePath { get; set; } = "";
        public ImageFillMode BackgroundImageMode { get; set; } = ImageFillMode.Uniform;

        // 字体设置（统一）
        public bool UseCustomFont { get; set; } = false;
        public string CustomFontName { get; set; } = "微软雅黑";

        // 时钟设置
        public bool ShowClock { get; set; } = true;
        public bool ClockColonFlash { get; set; } = true;

        public int RowHeight { get; set; } = 0;
        public int ColumnWidth { get; set; } = 0;
        public string ExportFormat { get; set; } = "txt";
        public bool MgmtEnabled { get; set; } = false;
        public string MgmtManifestUrl { get; set; } = "";
        public Dictionary<string, int> MgmtVersions { get; set; } = new();
        public bool MgmtForceRemote { get; set; } = false;
        public string OrganizationName { get; set; } = "";
        public List<string> CustomSubjects { get; set; } = new();
        public bool ExtendFridayHomeworkToWeekend { get; set; } = true;
        public bool ApplyBarColorToCardBorder { get; set; } = false;
        public int FirstRunCompleted { get; set; } = 0;

        [Obsolete] public int BackgroundOpacity { get; set; } = 12;

        private static string GetConfigPath()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "Homework", "config.json");
        }

        public static AppConfig Load()
        {
            string path = GetConfigPath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    if (config.EveningClassTimes == null) config.EveningClassTimes = new();
                    while (config.EveningClassTimes.Count < config.EveningClassCount)
                        config.EveningClassTimes.Add(new EveningClassTime { Start = "19:00", End = "19:50" });
                    if (config.EveningClassTimes.Count > config.EveningClassCount)
                        config.EveningClassTimes = config.EveningClassTimes.GetRange(0, config.EveningClassCount);
                    if (config.ClassReps == null) config.ClassReps = new();
                    if (config.CustomSubjects == null || config.CustomSubjects.Count == 0)
                        config.CustomSubjects = new List<string> { "语文", "数学", "英语", "物理", "化学", "生物" };
                    if (config.AutoPageInterval <= 0) config.AutoPageInterval = 10;
                    if (!Enum.IsDefined(typeof(ImageFillMode), config.BackgroundImageMode))
                        config.BackgroundImageMode = ImageFillMode.Uniform;
                    if (string.IsNullOrEmpty(config.CustomFontName)) config.CustomFontName = "微软雅黑";
                    return config;
                }
                catch { }
            }
            var defaultConfig = new AppConfig();
            defaultConfig.EveningClassTimes.Clear();
            for (int i = 0; i < defaultConfig.EveningClassCount; i++)
                defaultConfig.EveningClassTimes.Add(new EveningClassTime { Start = "19:00", End = "19:50" });
            defaultConfig.ClassReps = new();
            defaultConfig.CustomSubjects = new List<string> { "语文", "数学", "英语", "物理", "化学", "生物" };
            defaultConfig.AutoPageInterval = 10;
            defaultConfig.FontColorWhite = true;
            defaultConfig.ApplyBarColorToCardBorder = false;
            defaultConfig.UseCustomFont = false;
            defaultConfig.CustomFontName = "微软雅黑";
            defaultConfig.ShowClock = true;
            defaultConfig.ClockColonFlash = true;
            return defaultConfig;
        }

        public void Save()
        {
            string path = GetConfigPath();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}