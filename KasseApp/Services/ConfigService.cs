using System.IO;
using System.Text.Json;

namespace KasseApp
{
    public class ConfigRoot
    {
        public DbConfig Database { get; set; } = new();
        public GeneralConfig General { get; set; } = new();
    }

    public class GeneralConfig
    {
        public string Language { get; set; } = "de";
        public string ReceiptPrinterName { get; set; } = "";
    }

    public static class ConfigService
    {
        private const string ConfigFileName = "config.json";

        // Lädt die Konfiguration aus der JSON-Datei.
        public static ConfigRoot Load()
        {
            if (!File.Exists(ConfigFileName))
                throw new FileNotFoundException("config.json nicht gefunden", ConfigFileName);

            var json = File.ReadAllText(ConfigFileName);
            var config = JsonSerializer.Deserialize<ConfigRoot>(json);

            if (config == null || config.Database == null || string.IsNullOrWhiteSpace(config.Database.Host))
                throw new InvalidDataException("config.json ist ungültig oder Database.Host ist leer.");

            return config;
        }

        // Speichert die Konfiguration zurück in die Datei.
        public static void Save(ConfigRoot config)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigFileName, json);
        }
    }
}