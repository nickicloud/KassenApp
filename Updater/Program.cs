using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Updater
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== KasseApp Updater ===");
            
            if (args.Length == 0)
            {
                Console.WriteLine("Fehler: Keine Download-URL übergeben.");
                Console.ReadKey();
                return;
            }

            string downloadUrl = args[0];
            string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update_temp.zip");
            string appExeName = "KasseApp.exe"; // Vollständiger Name für Start
            string processName = "KasseApp";    // Name für die Prozess-Prüfung

            // 1. Warten auf Beenden der Haupt-App
            Console.WriteLine("Warte darauf, dass die KasseApp beendet wird...");
            int attempts = 0;
            while (Process.GetProcessesByName(processName).Length > 0 && attempts < 10)
            {
                await Task.Delay(1000);
                attempts++;
            }

            try
            {
                // 2. Download
                using (var client = new HttpClient())
                {
                    Console.WriteLine("Lade Update herunter...");
                    var data = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(zipPath, data);
                }

                // 3. Entpacken
                Console.WriteLine("Installiere Update...");
                string installPath = AppDomain.CurrentDomain.BaseDirectory;

                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // Wichtig: config.json NIEMALS überschreiben
                        if (entry.FullName.Equals("config.json", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("Überspringe config.json...");
                            continue;
                        }

                        // Zielpfad sauber zusammenbauen
                        string destinationPath = Path.GetFullPath(Path.Combine(installPath, entry.FullName));

                        // Falls es ein Verzeichnis im ZIP ist
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        // Unterverzeichnisse erstellen, falls nötig
                        string? directory = Path.GetDirectoryName(destinationPath);
                        if (directory != null) Directory.CreateDirectory(directory);

                        // Datei extrahieren und überschreiben
                        entry.ExtractToFile(destinationPath, true);
                        Console.WriteLine($"Update: {entry.FullName}");
                    }
                }

                // 4. Abschluss
                Console.WriteLine("Update abgeschlossen. Räume auf...");
                if (File.Exists(zipPath)) File.Delete(zipPath);

                Console.WriteLine("Starte KasseApp neu...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(installPath, appExeName),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n--- FEHLER BEIM UPDATE ---");
                Console.WriteLine(ex.Message);
                Console.WriteLine("\nFalls die App noch offen ist, schließe sie manuell.");
                Console.WriteLine("Drücke eine beliebige Taste zum Beenden...");
                Console.ReadKey();
            }
        }
    }
}