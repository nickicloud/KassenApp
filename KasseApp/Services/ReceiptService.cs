using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace KasseApp
{
    public class ReceiptService
    {
        private readonly string _printerName;
        private List<Artikel> _currentItems = new List<Artikel>();

        public ReceiptService(string printerName)
        {
            _printerName = printerName;
        }

        public void PrintReceipt(List<Artikel> items)
        {
            if (items == null || items.Count == 0)
                return;

            _currentItems = items;

            var printDoc = new PrintDocument();

            if (!string.IsNullOrWhiteSpace(_printerName))
            {
                printDoc.PrinterSettings.PrinterName = _printerName;
            }

            printDoc.PrintPage += PrintDoc_PrintPage;
            printDoc.Print();
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            using var font = new Font("Courier New", 9);
            float lineHeight = font.GetHeight(e.Graphics);
            float x = 0;
            float y = 0;

            e.Graphics.DrawString("KASSE", font, Brushes.Black, x, y);
            y += lineHeight * 2;

            decimal summe = 0;

            e.Graphics.DrawString("Artikelliste", font, Brushes.Black, x, y);
            y += lineHeight;
            e.Graphics.DrawString(new string('-', 32), font, Brushes.Black, x, y);
            y += lineHeight;

            foreach (var artikel in _currentItems)
            {
                decimal total = artikel.Preis;
                summe += total;

                string name = artikel.Name ?? string.Empty;
                if (name.Length > 16)
                    name = name.Substring(0, 16);
                else
                    name = name.PadRight(16);

                string line = $"{name} {artikel.Preis,6:0.00}";
                e.Graphics.DrawString(line, font, Brushes.Black, x, y);
                y += lineHeight;
            }

            y += lineHeight;

            e.Graphics.DrawString(new string('-', 32), font, Brushes.Black, x, y);
            y += lineHeight;

            string totalLine = $"Summe: {summe:0.00}";
            e.Graphics.DrawString(totalLine, font, Brushes.Black, x, y);
            y += lineHeight * 2;

            string dateLine = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            e.Graphics.DrawString(dateLine, font, Brushes.Black, x, y);

            e.HasMorePages = false;
        }
    }
}
