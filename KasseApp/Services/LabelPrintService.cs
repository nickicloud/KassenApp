using System;
using System.Drawing;
using System.Drawing.Printing;
using ZXing;
using ZXing.Common;

namespace KasseApp
{
    // Service, der Etiketten entweder auf A4 oder auf einem Etikettendrucker druckt
    public class LabelPrintService
    {
        // Name des A4-Druckers
        private readonly string _a4PrinterName;
        // Name des Etikettendruckers
        private readonly string _labelPrinterName;

        public LabelPrintService(string a4PrinterName, string labelPrinterName)
        {
            _a4PrinterName = a4PrinterName;
            _labelPrinterName = labelPrinterName;
        }

        /// <summary>
        /// A4: ein großes Etikett pro Seite, anzahl Seiten.
        /// </summary>
        public void PrintA4Label(Artikel artikel, int anzahl)
        {
            // Für jede gewünschte Etiketten-Seite einen eigenen Druckauftrag
            for (int i = 0; i < anzahl; i++)
            {
                using var doc = new PrintDocument();
                // Ziel-Drucker setzen
                doc.PrinterSettings.PrinterName = _a4PrinterName;
                doc.DocumentName = "Artikel-Etikett (A4)";

                // Papiergröße A4 in hundredths of an inch (8.27 x 11.69 inch ≈ 827 x 1169)
                doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);

                // Druck-Layout definieren
                doc.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    using var fontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
                    using var fontText = new Font("Segoe UI", 12);
                    using var fontBarcodeText = new Font("Segoe UI", 10);
                    float y = 30;

                    // Rahmen fürs Etikett
                    g.DrawRectangle(Pens.Black, 20, 20, 280, 150);

                    // Artikelname
                    g.DrawString(artikel.Name, fontTitle, Brushes.Black, 28, y);
                    y += 35;

                    // Preis
                    g.DrawString($"{artikel.Preis:0.00} €", fontText, Brushes.Black, 28, y);
                    y += 30;

                    // Barcode‑Position und ‑Größe
                    int barcodeX = 28;
                    int barcodeY = (int)y;
                    int barcodeWidth = 250;
                    int barcodeHeight = 60;

                    // Barcode zeichnen
                    DrawBarcode(g, artikel.Barcode, barcodeX, barcodeY, barcodeWidth, barcodeHeight);

                    // Barcode‑Nummer zentriert unter dem Barcode ausgeben
                    float textY = barcodeY + barcodeHeight + 2;
                    var size = g.MeasureString(artikel.Barcode, fontBarcodeText);
                    float centeredX = barcodeX + (barcodeWidth - size.Width) / 2f;
                    g.DrawString(artikel.Barcode, fontBarcodeText, Brushes.Black, centeredX, textY);
                };

                // Druckauftrag senden
                doc.Print();
            }
        }

        /// <summary>
        /// Etikettendrucker: Papiergröße z.B. 70 x 37 mm, kleiner Barcode.
        /// </summary>
        public void PrintLabelPrinter(Artikel artikel, int anzahl)
        {
            // Labelgröße in mm (an deinen Drucker anpassbar)
            const double widthMm = 35;
            const double heightMm = 40;

            int ToHundredthsOfInch(double mm) => (int)Math.Round(mm / 25.4 * 100);

            int paperWidth = ToHundredthsOfInch(widthMm);
            int paperHeight = ToHundredthsOfInch(heightMm);

            for (int i = 0; i < anzahl; i++)
            {
                using var doc = new PrintDocument();
                doc.PrinterSettings.PrinterName = _labelPrinterName;
                doc.DocumentName = "Artikel-Etikett (Label)";

                var labelSize = new PaperSize("Etikett", paperWidth, paperHeight);
                doc.DefaultPageSettings.PaperSize = labelSize;
                doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                doc.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    using var fontText = new Font("Segoe UI", 8);
                    using var fontBarcodeText = new Font("Segoe UI", 7);
                    float y = 3;

                    // Name mit automatischem Zeilenumbruch (140 Pixel Breite verfügbar)
                    DrawWrappedText(g, artikel.Name, fontText, 3, y, 140f);
                    y += 2 * (fontText.Height + 4); // genug Platz für bis zu 2 Zeilen Name

                    // Preis (jetzt unter dem Namen)
                    g.DrawString($"{artikel.Preis:0.00} €", fontText, Brushes.Black, 3, y);
                    y += fontText.Height + 4;

                    // Barcode (weiter unten)
                    int barcodeX = 3;
                    int barcodeY = (int)y;
                    int barcodeWidth = 120;
                    int barcodeHeight = 30;

                    DrawBarcode(g, artikel.Barcode, barcodeX, barcodeY, barcodeWidth, barcodeHeight);

                    // Barcode-Nummer zentriert unter dem Barcode
                    float textY = barcodeY + barcodeHeight + 1;
                    var size = g.MeasureString(artikel.Barcode, fontBarcodeText);
                    float centeredX = barcodeX + (barcodeWidth - size.Width) / 2f;
                    g.DrawString(artikel.Barcode, fontBarcodeText, Brushes.Black, centeredX, textY);
                };

                doc.Print();
            }
        }

        // KORREKTE Hilfsfunktion: trennt NUR bei Leerzeichen, behält "+"!
        private void DrawWrappedText(Graphics g, string text, Font font, float x, float y, float maxWidth)
        {
            // NUR bei Leerzeichen trennen, "+" bleibt dran (z.B. "Brombeer +" bleibt zusammen)
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            float lineHeight = font.Height + 2;
            string currentLine = "";
            float currentY = y;

            foreach (var word in words)
            {
                // Teste, ob Wort in aktuelle Zeile passt
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var testSize = g.MeasureString(testLine, font);

                if (testSize.Width > maxWidth)
                {
                    // Aktuelle Zeile ausgeben
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        g.DrawString(currentLine, font, Brushes.Black, x, currentY);
                        currentY += lineHeight;
                    }
                    // Neues Wort als neue Zeile starten
                    currentLine = word;
                }
                else
                {
                    // Wort zur aktuellen Zeile hinzufügen
                    currentLine = testLine;
                }
            }

            // Letzte Zeile ausgeben
            if (!string.IsNullOrEmpty(currentLine))
            {
                g.DrawString(currentLine, font, Brushes.Black, x, currentY);
            }
        }

        // Hilfsfunktion: erzeugt mit ZXing einen CODE_128 Barcode und zeichnet ihn ins Graphics-Objekt
        private void DrawBarcode(Graphics g, string text, int x, int y, int width, int height)
        {
            // Leere / ungültige Texte ignorieren
            if (string.IsNullOrWhiteSpace(text))
                return;

            // ZXing-Writer für Pixel-Daten konfigurieren
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = 1   // kleiner Rand um den Barcode
                }
            };

            // Barcode generieren
            var pixelData = writer.Write(text);

            // PixelDaten in ein Bitmap kopieren
            using var bmp = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                bmp.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bmpData.Scan0, pixelData.Pixels.Length);
            bmp.UnlockBits(bmpData);

            // Barcode-Bitmap an gewünschter Position zeichnen
            g.DrawImage(bmp, x, y);
        }
    }
}
