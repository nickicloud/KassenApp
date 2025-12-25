using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;

namespace KasseApp
{
    public class ReceiptService
    {
        private readonly string _printerName;
        private List<WarenkorbPosition> _currentItems = new List<WarenkorbPosition>();

        public ReceiptService(string printerName)
        {
            _printerName = printerName;
        }

        public void PrintReceipt(List<WarenkorbPosition> items)
        {
            if (items == null || items.Count == 0)
                return;

            _currentItems = items;

            var printDoc = new PrintDocument();

            // Wenn ein Druckername konfiguriert wurde, prüfen ob es ihn gibt.
            if (!string.IsNullOrWhiteSpace(_printerName))
            {
                bool exists = PrinterSettings.InstalledPrinters
                    .Cast<string>()
                    .Any(p => string.Equals(p, _printerName, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    printDoc.PrinterSettings.PrinterName = _printerName;
                }
                // sonst: still den Standarddrucker verwenden
            }

            printDoc.PrintPage += PrintDoc_PrintPage;

            try
            {
                printDoc.Print();
            }
            catch (InvalidPrinterException ex)
            {
                // Zum Debuggen: Fehler anzeigen, damit du siehst, was los ist.
                System.Windows.MessageBox.Show(
                    $"Druckerfehler:\n{ex.Message}\n\nBitte Drucker in config.json prüfen.",
                    "Druckerfehler");
            }
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

            foreach (var pos in _currentItems)
            {
                decimal total = pos.Gesamtpreis;
                summe += total;

                string name = pos.Artikel.Name ?? string.Empty;
                if (name.Length > 12)
                    name = name.Substring(0, 12);
                else
                    name = name.PadRight(12);

                string line = $"{name} {pos.Menge,2}x {pos.Artikel.Preis,5:0.00} {total,6:0.00}";
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
