using System;
using System.Drawing;
using System.Drawing.Printing;
using ZXing;
using ZXing.Common;

namespace KasseApp
{
    public class LabelPrintService
    {
        private readonly string _a4PrinterName;
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
            for (int i = 0; i < anzahl; i++)
            {
                using var doc = new PrintDocument();
                doc.PrinterSettings.PrinterName = _a4PrinterName;
                doc.DocumentName = "Artikel-Etikett (A4)";

                // A4-Papier (hundredths of an inch: 8.27 x 11.69 inch)
                doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);

                doc.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    using var fontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
                    using var fontText = new Font("Segoe UI", 12);
                    float y = 30;

                    // Rahmen
                    g.DrawRectangle(Pens.Black, 20, 20, 280, 150);

                    // Name
                    g.DrawString(artikel.Name, fontTitle, Brushes.Black, 28, y);
                    y += 35;

                    // Preis
                    g.DrawString($"{artikel.Preis:0.00} €", fontText, Brushes.Black, 28, y);
                    y += 30;

                    // Barcode
                    DrawBarcode(g, artikel.Barcode, 28, (int)y, 250, 60);
                };

                doc.Print();
            }
        }

        /// <summary>
        /// Etikettendrucker: Papiergröße z.B. 70 x 37 mm, kleiner Barcode.
        /// </summary>
        public void PrintLabelPrinter(Artikel artikel, int anzahl)
        {
            // Labelgröße in mm (bei Bedarf anpassen)
            const double widthMm = 70;
            const double heightMm = 37;

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
                    float y = 3;

                    // Name (evtl. kürzen)
                    string name = artikel.Name;
                    if (name.Length > 30)
                        name = name.Substring(0, 30) + "...";

                    g.DrawString(name, fontText, Brushes.Black, 3, y);
                    y += 14;

                    g.DrawString($"{artikel.Preis:0.00} €", fontText, Brushes.Black, 3, y);
                    y += 14;

                    // kleiner Barcode
                    int barcodeWidth = 120;   // hier bei Bedarf anpassen
                    int barcodeHeight = 30;   // hier bei Bedarf anpassen

                    DrawBarcode(g, artikel.Barcode, 3, (int)y, barcodeWidth, barcodeHeight);
                };

                doc.Print();
            }
        }

        private void DrawBarcode(Graphics g, string text, int x, int y, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = 1
                }
            };

            var pixelData = writer.Write(text);

            using var bmp = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                bmp.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bmpData.Scan0, pixelData.Pixels.Length);
            bmp.UnlockBits(bmpData);

            g.DrawImage(bmp, x, y);
        }
    }
}
