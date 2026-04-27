using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using ZXing;
using ZXing.Common;

namespace KasseApp
{
    public class LabelPrintService
    {
        private readonly string _receiptPrinterName;
        private readonly string _a4PrinterName;
        private readonly string _labelPrinterName;
        private readonly double _labelWidthMm;
        private readonly double _labelHeightMm;
        private readonly double _labelMarginMm;

        public LabelPrintService(IConfiguration config)
        {
            var general = config.GetSection("General");
            _receiptPrinterName = general["ReceiptPrinterName"];
            _a4PrinterName = general["A4PrinterName"];
            _labelPrinterName = general["LabelPrinterName"];

            var label = general.GetSection("Label");
            _labelWidthMm = ParseDouble(label["WidthMm"]) ?? 35;
            _labelHeightMm = ParseDouble(label["HeightMm"]) ?? 40;
            _labelMarginMm = ParseDouble(label["MarginMm"]) ?? 0;
        }

        private static double? ParseDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
                ? result
                : (double?)null;
        }

        public void Print40x20mmLabel(Artikel artikel, int anzahl)
        {
            PrintCustomLabel(artikel, anzahl, 40, 20, 0);
        }

        public void PrintLabelPrinter(Artikel artikel, int anzahl)
        {
            PrintCustomLabel(artikel, anzahl, (int)_labelWidthMm, (int)_labelHeightMm, (int)_labelMarginMm);
        }

        private void PrintCustomLabel(Artikel artikel, int anzahl, int widthMm, int heightMm, int marginMm)
        {
            int ToHundredthsOfInch(double mm) => (int)Math.Round(mm / 25.4 * 100);
            int paperWidth = ToHundredthsOfInch(widthMm);
            int paperHeight = ToHundredthsOfInch(heightMm);

            for (int i = 0; i < anzahl; i++)
            {
                using var doc = new PrintDocument();
                doc.PrinterSettings.PrinterName = _labelPrinterName;
                doc.DocumentName = $"Label {widthMm}x{heightMm}mm";

                doc.DefaultPageSettings.PaperSize = new PaperSize("Custom", paperWidth, paperHeight);
                doc.DefaultPageSettings.Margins = new Margins(marginMm, marginMm, marginMm, marginMm);
                doc.OriginAtMargins = true;

                doc.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    RectangleF mb = e.MarginBounds;
                    float x = mb.X;
                    float y = mb.Y;
                    float w = mb.Width;
                    float h = mb.Height;

                    using var center = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Near
                    };

                    float scale = heightMm / 22f;

                    // 1) NAME (2 Zeilen Umbruch)
                    using var fontName = new Font("Segoe UI", 7.0f * scale + 2f, FontStyle.Regular, GraphicsUnit.Pixel);
                    float nameH = fontName.Height * 2f; 
                    var nameRect = new RectangleF(x, y, w, nameH);
                    g.DrawString(artikel.Name ?? "", fontName, Brushes.Black, nameRect, center);
                    
                    // 2) PREIS
                    using var fontPrice = new Font("Segoe UI", 11.0f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
                    float priceY = y + nameH - 4; 
                    var priceRect = new RectangleF(x, priceY, w, fontPrice.Height);
                    g.DrawString($"{artikel.Preis:0.00} €", fontPrice, Brushes.Black, priceRect, center);

                    // 3) BARCODE (Höhe reduziert auf 60% des verfügbaren Platzes)
                    using var fontNumber = new Font("Segoe UI", 6.0f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
                    float numberH = fontNumber.Height;
                    float barcodeY = priceY + fontPrice.Height - 8;
                    float availableBarcodeSpace = (mb.Bottom - barcodeY) - numberH - 2;
                    float barcodeH = availableBarcodeSpace * 0.4f; // HIER: Reduziert auf 60%

                    if (barcodeH > 5)
                    {
                        // Zentrierung des Barcodes im verfügbaren Block
                        float centeredBarcodeY = barcodeY + (availableBarcodeSpace - barcodeH - numberH) / 2;
                        DrawBarcode(g, artikel.Barcode, (int)x, (int)centeredBarcodeY, (int)w, (int)barcodeH);
                        
                        // 4) NUMMER (Direkt unter Barcode)
                        var numberRect = new RectangleF(x, centeredBarcodeY + barcodeH + 1, w, numberH - 2);
                        g.DrawString(artikel.Barcode ?? "", fontNumber, Brushes.Black, numberRect, center);
                    }

                    e.HasMorePages = false;
                };

                doc.Print();
            }
        }

        public void PrintReceipt(Artikel artikel, int anzahl)
        {
            for (int i = 0; i < anzahl; i++)
            {
                using var doc = new PrintDocument();
                doc.PrinterSettings.PrinterName = _receiptPrinterName;
                doc.DocumentName = "Quittung";
                doc.Print();
            }
        }

        private void DrawBarcode(Graphics g, string text, int x, int y, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(text) || width <= 5 || height <= 5) return;
            try
            {
                var writer = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions { Height = height, Width = width, Margin = 0, PureBarcode = true }
                };
                var pixelData = writer.Write(text);
                using var bmp = CreateBitmapFromPixelData(pixelData);
                g.DrawImage(bmp, new Rectangle(x, y, width, height));
            }
            catch { }
        }

        private static Bitmap CreateBitmapFromPixelData(ZXing.Rendering.PixelData pixelData)
        {
            var bmp = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bmpData.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }
            return bmp;
        }
    }
}