using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace KasseApp
{
    public class ReceiptService
    {
        private readonly string _printerName;

        public ReceiptService(string printerName)
        {
            _printerName = printerName;
        }

        public void PrintReceipt(IList<WarenkorbPosition> warenkorb)
        {
            if (warenkorb == null || warenkorb.Count == 0)
                return;

            PrintDocument doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = _printerName;
            doc.DocumentName = "Kassenbon";

            doc.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                float y = 10;
                float lineHeight = 18;
                var font = new Font("Consolas", 9);
                var bold = new Font("Consolas", 9, FontStyle.Bold);

                g.DrawString("KasseApp", bold, Brushes.Black, 10, y);
                y += lineHeight;
                g.DrawString(DateTime.Now.ToString("dd.MM.yyyy HH:mm"), font, Brushes.Black, 10, y);
                y += lineHeight;

                g.DrawString("----------------------------------------", font, Brushes.Black, 10, y);
                y += lineHeight;

                decimal sum = 0m;

                foreach (var pos in warenkorb)
                {
                    decimal zeilenPreis = pos.Artikel.Preis * pos.Menge;
                    sum += zeilenPreis;

                    string line1 = $"{pos.Artikel.Name}";
                    string line2 = $"{pos.Menge} x {pos.Artikel.Preis:0.00} €   = {zeilenPreis:0.00} €";

                    g.DrawString(line1, font, Brushes.Black, 10, y);
                    y += lineHeight;
                    g.DrawString(line2, font, Brushes.Black, 10, y);
                    y += lineHeight;
                }

                g.DrawString("----------------------------------------", font, Brushes.Black, 10, y);
                y += lineHeight;

                g.DrawString($"Summe: {sum:0.00} €", bold, Brushes.Black, 10, y);
                y += lineHeight * 2;

                g.DrawString("Vielen Dank für Ihren Einkauf!", font, Brushes.Black, 10, y);
            };

            doc.Print();
        }
    }
}
