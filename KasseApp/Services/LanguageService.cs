using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class LanguageService
{
    private Dictionary<string, string> _translations = new();

    // Lädt die Sprachdatei anhand des Sprachcodes (z.B. "de" -> lang.de.json).
    public void Load(string languageCode)
    {
        string fileName = $"lang.{languageCode}.json";
        if (!File.Exists(fileName))
        {
            _translations = new Dictionary<string, string>();
            return;
        }

        var json = File.ReadAllText(fileName);
        _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                        ?? new Dictionary<string, string>();
    }

    // Gibt den Text zum Key zurück oder den Key selbst, wenn nichts gefunden wird.
    public string T(string key)
    {
        return _translations.TryGetValue(key, out var value) ? value : key;
    }
}