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

        public void PrintA4Label(Artikel artikel, int anzahl)
        {
            for (int i = 0; i < anzahl; i++)
            {
                using var doc = new PrintDocument();
                doc.PrinterSettings.PrinterName = _a4PrinterName;
                doc.DocumentName = "Artikel-Etikett (A4)";
                doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);

                doc.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    using var fontTitle = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var fontText = new Font("Segoe UI", 12, GraphicsUnit.Pixel);
                    using var fontBarcodeText = new Font("Segoe UI", 10, GraphicsUnit.Pixel);

                    float y = 30;
                    g.DrawRectangle(Pens.Black, 20, 20, 280, 150);
                    g.DrawString(artikel.Name, fontTitle, Brushes.Black, 28, y);
                    y += 35;
                    g.DrawString($"{artikel.Preis:0.00} €", fontText, Brushes.Black, 28, y);
                    y += 30;

                    int barcodeX = 28;
                    int barcodeY = (int)y;
                    int barcodeWidth = 250;
                    int barcodeHeight = 60;

                    DrawBarcode(g, artikel.Barcode, barcodeX, barcodeY, barcodeWidth, barcodeHeight);

                    float textY = barcodeY + barcodeHeight + 2;
                    var size = g.MeasureString(artikel.Barcode, fontBarcodeText);
                    float centeredX = barcodeX + (barcodeWidth - size.Width) / 2f;
                    g.DrawString(artikel.Barcode, fontBarcodeText, Brushes.Black, centeredX, textY);
                };

                doc.Print();
            }
        }

        /// <summary>
        /// Name 10pt Bold, Preis & Nummer normal (ohne Bold).
        /// </summary>
        public void PrintLabelPrinter(Artikel artikel, int anzahl)
        {
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
                doc.OriginAtMargins = false;

                doc.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;

                    // Name: 10pt Bold (wie gewünscht)
                    using var fontName = new Font("Segoe UI", 10, FontStyle.Bold, GraphicsUnit.Pixel);
                    // Preis: 9pt NORMAL (ohne Bold)
                    using var fontPrice = new Font("Segoe UI", 9, GraphicsUnit.Pixel);
                    // Barcode-Nummer: 9pt NORMAL (ohne Bold)
                    using var fontBarcodeText = new Font("Segoe UI", 9, GraphicsUnit.Pixel);

                    RectangleF pa = e.PageSettings.PrintableArea;
                    float x0 = pa.X;
                    float y0 = pa.Y;
                    float w = pa.Width;
                    float h = pa.Height;

                    // 1. Name fett
                    float nameX = x0 + w * 0.08f;
                    float nameY = y0 + h * 0.06f;
                    float nameWidth = w * 0.84f;
                    var nameRect = new RectangleF(nameX, nameY, nameWidth, h * 0.25f);

                    using var nameFormat = new StringFormat 
                    { 
                        Alignment = StringAlignment.Near, 
                        LineAlignment = StringAlignment.Near, 
                        FormatFlags = StringFormatFlags.LineLimit 
                    };
                    g.DrawString(artikel.Name, fontName, Brushes.Black, nameRect, nameFormat);

                    float y = nameRect.Bottom + h * 0.025f;

                    // 2. Preis normal
                    string priceText = $"{artikel.Preis:0.00} €";
                    g.DrawString(priceText, fontPrice, Brushes.Black, nameX, y);
                    y += fontPrice.Height + h * 0.04f;

                    // 3. Barcode klein
                    int barcodeHeight = (int)(h * 0.18f);
                    int barcodeWidth = (int)(w * 0.70f);
                    int barcodeX = (int)(x0 + (w - barcodeWidth) / 2f);
                    int barcodeY = (int)y;

                    DrawBarcode(g, artikel.Barcode, barcodeX, barcodeY, barcodeWidth, barcodeHeight);

                    // 4. Barcode-Nummer normal
                    float numY = barcodeY + barcodeHeight + h * 0.015f;
                    var numSize = g.MeasureString(artikel.Barcode, fontBarcodeText);
                    float numX = x0 + (w - numSize.Width) / 2f;
                    g.DrawString(artikel.Barcode, fontBarcodeText, Brushes.Black, numX, numY);

                    e.HasMorePages = false;
                };

                doc.Print();
            }
        }

        private void DrawBarcode(Graphics g, string text, int x, int y, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

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

            using var bmp = new Bitmap(pixelData.Width, pixelData.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                bmp.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bmpData.Scan0, pixelData.Pixels.Length);
            bmp.UnlockBits(bmpData);

            g.DrawImage(bmp, new Rectangle(x, y, width, height));
        }

        private void DrawWrappedTextInRect(Graphics g, string text, Font font, Brush brush, RectangleF rect, StringFormat format)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            g.DrawString(text, font, brush, rect, format);
        }
    }
}
