using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KasseApp
{
    public class LanguageService
    {
        private Dictionary<string, string> _dict = new();

        
        public void Load(string languageCode)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Dateiname nach Schema lang.xx.json
            var fileName = $"lang.{languageCode}.json";
            
            var fullPath = Path.Combine(baseDir, "Lang", fileName);

            // Wenn die Datei nicht existiert, versuche zur Sicherheit lang.de.json
            if (!File.Exists(fullPath))
            {
                var fallback = Path.Combine(baseDir, "Lang", "lang.de.json");
                if (File.Exists(fallback))
                {
                    fullPath = fallback;
                }
                else
                {
                    throw new FileNotFoundException(
                        $"Language file not found. Expected at: {fullPath}",
                        fullPath);
                }
            }

            var json = File.ReadAllText(fullPath);

            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            _dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options)
                    ?? new Dictionary<string, string>();
        }
        
        public string T(string key)
            => _dict.TryGetValue(key, out var value) ? value : key;
    }
}
