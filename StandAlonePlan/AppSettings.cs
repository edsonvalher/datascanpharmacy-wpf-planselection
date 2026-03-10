using System;
using System.IO;

namespace StandAlonePlan
{
    /// <summary>
    /// Reads appsettings.json to determine data source (Mock vs Real).
    /// Mirrors the Configuration block from SAMPLE_C3_Component.md.
    /// </summary>
    public class AppSettings
    {
        public string DataSource { get; private set; } = "Mock";

        private static AppSettings? _current;
        public static AppSettings Current => _current ??= Load();

        public static AppSettings Load()
        {
            var settings = new AppSettings();
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    // Simple key-value extraction without a JSON library dependency
                    var match = System.Text.RegularExpressions.Regex.Match(
                        json, @"""DataSource""\s*:\s*""(\w+)""");
                    if (match.Success)
                        settings.DataSource = match.Groups[1].Value;
                }
            }
            catch { /* use defaults on any read error */ }
            return settings;
        }
    }
}
