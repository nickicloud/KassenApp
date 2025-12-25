using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KasseApp
{
    public class LanguageService
    {
        private Dictionary<string, string> _dict = new();

        /// <summary>
        /// Lädt die Sprachdatei aus dem Ordner "Lang" neben der EXE.
        /// languageCode: z.B. "de" -> Datei "lang.de.json"
        /// </summary>
        public void Load(string languageCode)
        {
            // Basisverzeichnis der laufenden EXE, z.B. bin\Debug\net9.0-windows
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Dateiname nach Schema lang.xx.json
            var fileName = $"lang.{languageCode}.json";

            // Ordner "Lang" unterhalb der EXE
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
                    // Wirf eine klare Exception – hier crasht aktuell dein Programm
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

        /// <summary>
        /// Gibt den Text zu einem Key zurück oder den Key selbst, falls nicht gefunden.
        /// </summary>
        public string T(string key)
            => _dict.TryGetValue(key, out var value) ? value : key;
    }
}
