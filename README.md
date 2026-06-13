# KasseApp – Modernes WPF Kassensystem (.NET 9)

KasseApp ist ein modernes, dunkles Kassensystem auf Basis von WPF und .NET 9.  
Die Anwendung verwaltet Artikel, Bestände und einen Warenkorb, unterstützt Barcodes und bietet ein durchgängig modernes UI‑Design.

## Features

- Artikelverwaltung mit:
  - Barcode, Name, Preis, Bestand
  - Anlegen, Bearbeiten, Löschen von Artikeln
- Warenkorb:
  - Artikel hinzufügen (per Auswahl oder Barcode)
  - Menge automatisch erhöhen, wenn Artikel mehrfach gescannt wird
  - Kontextmenü zum Löschen einzelner Positionen
  - Button zum vollständigen Leeren des Warenkorbs
  - Automatische **Gesamtsummen‑Berechnung** im Warenkorb
- Bestandsführung:
  - Beim Bezahlen wird der **Bestand jedes Artikels um die verkaufte Menge reduziert**
  - Aktualisierung in der Datenbank
- Barcode‑Unterstützung:
  - Separates Barcode‑Fenster im runden „Scanner‑Design“
  - Eingabefeld + große Anzeige + Artikelinfos
  - Hinzufügen zum Warenkorb mit einem Klick
  - Historie der zuletzt gescannten Barcodes im Hauptfenster
- Mehrsprachigkeit:
  - Sprachdateien als JSON (`Lang/lang.xx.json`)
  - Texte werden über `LanguageService` geladen (`lang.de.json` etc.)
- Modernes Dark‑UI:
  - Eigener Titelbalken, runde Ecken, flache Buttons
  - Eigene Styles für Buttons, TextBoxen und Kontextmenüs
  - Optionales App‑Icon über `icon.ico` / `icon.png`

## Technik

- .NET 9 / C#
- WPF (Windows Desktop)
- MVVM‑ähnliche Struktur mit Services
- Datenzugriff über `ArtikelRepository`
- Sprachen über `LanguageService` und JSON‑Dateien
- Bon‑Druck über `ReceiptService`

## Projektstruktur (Auszug)

KasseApp/
KasseApp/
App.xaml
MainWindow.xaml / .cs
Views/
ArtikelDialog.xaml / .cs
BarcodeWindow.xaml / .cs
CartWindow.xaml / .cs
Services/
LanguageService.cs
ConfigService.cs
ReceiptService.cs
Models/
Artikel.cs
WarenkorbPosition.cs
ScanHistoryEntry.cs
Config.cs
Lang/
lang.de.json
Assets/
icon.ico

text

## Konfiguration

Die Anwendung nutzt eine Konfigurationsdatei (z.B. `config.json`), die u.a. folgende Informationen enthält:

- Datenbankverbindung (`Database.ToConnectionString()`)
- Sprache (`General.Language`, z.B. `"de"`)
- Druckername für Quittungen (`General.ReceiptPrinterName`)

Die Konfiguration wird über `ConfigService.Load()` geladen.

## Sprachdateien

Alle UI‑Texte werden über den `LanguageService` geladen.  
Beispiel: `Lang/lang.de.json`:

{
"Title_MainWindow": "Kassensystem",
"Main_Header": "Kassensystem",
"Button_Barcode": "Barcode",
"Button_Pay": "Bezahlen",
"Button_New": "Neu",
"Button_Edit": "Bearbeiten",
"Button_Delete": "Löschen",
"CartWindow_Title": "Warenkorb",
"CartWindow_Header": "Warenkorb",
"CartWindow_Col_Artikel": "Artikel",
"CartWindow_Col_Menge": "Menge",
"CartWindow_Col_Preis": "Preis",
"CartWindow_Clear": "Leeren",
"CartWindow_Pay": "Bezahlen",
"CartWindow_Cancel": "Abbrechen",
"CartWindow_DeletePosition": "Position löschen",
"CartWindow_Total": "Summe"
// ...
}

text

## App‑Icon

- Lege dein Icon z.B. unter `Assets/icon.ico` ab.
- In `App`‑Eigenschaften (Projekt → Eigenschaften → Anwendung) das Symbol auswählen.
- In `MainWindow.xaml` (optional) zusätzlich:

<Window ... Icon="Assets/icon.ico">

text

## Build & Ausführung

### Voraussetzungen

- Windows 10/11
- .NET 9 SDK
- JetBrains Rider oder Visual Studio

### Development‑Build

1. Repository klonen:
git clone <repo-url>
cd KasseApp/KasseApp

text
2. Pakete wiederherstellen / Build:
dotnet restore
dotnet build

text
3. Starten:
dotnet run

text

### Veröffentlichung (Publish)

Um die App als Ordner (für andere Nutzer) zu veröffentlichen:

dotnet publish -c Release -r win-x64 --self-contained false -o publish

text

- Im Ordner `publish` liegt dann `KasseApp.exe`.
- Den Ordner kann man zippen und weitergeben.

## Bedienung

1. **Artikel pflegen**:
   - „Neu“: neuen Artikel mit Barcode, Name, Preis und Bestand anlegen.
   - „Bearbeiten“: bestehenden Artikel anpassen (Barcode bleibt bei Edit schreibgeschützt).
   - „Löschen“: Artikel aus dem System entfernen.

2. **Warenkorb**:
   - Per Kontextmenü „In Warenkorb hinzufügen“ oder per Barcode‑Scan.
   - Im Warenkorb:
     - Rechtsklick auf eine Position → „Position löschen“.
     - Button „Leeren“ → gesamter Warenkorb wird gelöscht.
     - Über den Buttons wird immer die aktuelle **Gesamtsumme** angezeigt.

3. **Bezahlen**:
   - Klick auf „Bezahlen“ im Hauptfenster öffnet das Warenkorb‑Fenster.
   - Nach Bestätigung:
     - Der Bestand jedes Artikels wird um die verkaufte Menge reduziert.
     - Die Änderung wird in der Datenbank gespeichert.
     - Ein Bon wird über den konfigurierten Drucker gedruckt.
     - Warenkorb und Barcode‑Verlauf werden geleert.

## Lizenz
  MIT License
