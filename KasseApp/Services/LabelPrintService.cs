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

                var labelSize = new PaperSize($"{widthMm}x{heightMm}", paperWidth, paperHeight);
                doc.DefaultPageSettings.PaperSize = labelSize;
                doc.DefaultPageSettings.Margins = new Margins(marginMm, marginMm, marginMm, marginMm);
                doc.OriginAtMargins = true;

                doc.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    // sicherer Bereich innerhalb der Margins [web:333]
                    Rectangle mb = e.MarginBounds;

                    float x0 = mb.X;
                    float y0 = mb.Y;
                    float w = mb.Width;
                    float h = mb.Height;

                    // 40x20: bisschen Reserve unten, sonst verschwindet Nummer gern
                    float padX = w * 0.06f;
                    float padTop = h * 0.10f;
                    float padBottom = h * 0.30f; // kleiner extra Abstand nach unten

                    float x = x0 + padX;
                    float y = y0 + padTop;
                    float iw = w - 2 * padX;
                    float ih = h - padTop - padBottom;

                    using var center = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    float scale = heightMm / 20f;

                    using var fontPrice = new Font("Segoe UI", 13.5f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var fontNumber = new Font("Segoe UI", 7.8f * scale, FontStyle.Bold, GraphicsUnit.Pixel);

                    float gapAfterPrice = ih * 0.04f;
                    float gapBarcodeToNumber = Math.Max(1f, ih * 0.008f); // nahe am Barcode

                    // Höhen
                    float priceH = Math.Max(fontPrice.Height, ih * 0.30f);
                    float numberH = Math.Max(fontNumber.Height, ih * 0.14f);

                    float barcodeBlockH = ih - priceH - numberH - gapAfterPrice - gapBarcodeToNumber;

                    // Barcode soll komplett sein -> Minimum erzwingen
                    float minBarcode = ih * 0.44f;
                    if (barcodeBlockH < minBarcode)
                    {
                        float need = minBarcode - barcodeBlockH;
                        numberH = Math.Max(fontNumber.Height, numberH - need);
                        barcodeBlockH = ih - priceH - numberH - gapAfterPrice - gapBarcodeToNumber;
                    }

                    // 1) Preis
                    var priceRect = new RectangleF(x, y, iw, priceH);
                    g.DrawString($"{artikel.Preis:0.00} €", fontPrice, Brushes.Black, priceRect, center);
                    y += priceH + gapAfterPrice;

                    // 2) Barcode (Grafik)
                    int bW = (int)(iw * 0.98f);
                    int bH = (int)(barcodeBlockH * 0.90f);
                    int bX = (int)(x + (iw - bW) / 2f);
                    int bY = (int)(y + (barcodeBlockH - bH) / 2f);

                    DrawBarcode(g, artikel.Barcode, bX, bY, bW, bH);
                    y += barcodeBlockH + gapBarcodeToNumber;

                    // 3) Nummer unter Barcode
                    var numberRect = new RectangleF(x, y, iw, numberH);
                    g.DrawString(artikel.Barcode ?? "", fontNumber, Brushes.Black, numberRect, center);

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
            if (string.IsNullOrWhiteSpace(text)) return;
            if (width <= 1 || height <= 1) return;

            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = 0,
                    PureBarcode = true
                }
            };

            var pixelData = writer.Write(text);

            using var bmp = CreateBitmapFromPixelData(pixelData);
            g.DrawImage(bmp, new Rectangle(x, y, width, height));
        }

        private static Bitmap CreateBitmapFromPixelData(ZXing.Rendering.PixelData pixelData)
        {
            var bmp = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb);

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

            try
            {
                int bytesPerPixel = 4;
                int srcStride = pixelData.Width * bytesPerPixel;
                int dstStride = bmpData.Stride;

                var src = (byte[])pixelData.Pixels;

                for (int y = 0; y < pixelData.Height; y++)
                {
                    IntPtr dstRow = bmpData.Scan0 + (y * dstStride);
                    int srcOffset = y * srcStride;
                    System.Runtime.InteropServices.Marshal.Copy(src, srcOffset, dstRow, srcStride);
                }
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }
    }
}
