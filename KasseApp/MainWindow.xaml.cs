using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KasseApp.Views;

namespace KasseApp
{
    public partial class MainWindow : Window
    {
        // Repository für DB-Zugriff auf Tabelle 'artikel'.
        private readonly ArtikelRepository _artikelRepo;

        // Sprachdienst für Texte aus lang.xx.json.
        private readonly LanguageService _lang;

        // Service zum Drucken des Kassenbons.
        private readonly ReceiptService _receiptService;

        // Artikelliste für das DataGrid.
        private ObservableCollection<Artikel> _artikelListe = new();

        // Einfache Warenkorb-Liste für den Kassenbon.
        private ObservableCollection<Artikel> _warenkorb = new();

        public MainWindow()
        {
            InitializeComponent();

            // Konfiguration laden (config.json).
            var config = ConfigService.Load();

            // Sprache laden.
            _lang = new LanguageService();
            _lang.Load(config.General.Language);

            // Repository & ReceiptService mit ConnectionString / Drucker initialisieren.
            _artikelRepo = new ArtikelRepository(config.Database.ToConnectionString());
            _receiptService = new ReceiptService(config.General.ReceiptPrinterName);

            // Fenstertitel aus Sprachdatei.
            this.Title = _lang.T("Title_MainWindow");

            // Artikel initial laden.
            _ = LoadArtikelAsync();

            // Button-Events verbinden.
            btnBarcode.Click += BtnBarcode_Click;
            btnPay.Click += BtnPay_Click;
            btnNew.Click += BtnNew_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
        }

        // Lädt alle Artikel aus der Datenbank und zeigt sie im DataGrid an.
        private async Task LoadArtikelAsync()
        {
            try
            {
                var liste = await _artikelRepo.GetAllAsync();
                _artikelListe = new ObservableCollection<Artikel>(liste);
                dgArtikel.ItemsSource = _artikelListe;
            }
            catch
            {
                MessageBox.Show(_lang.T("Message_ErrorDb"));
            }
        }

        // Öffnet das Barcode-Fenster und fügt den gefundenen Artikel dem Warenkorb hinzu.
        private async void BtnBarcode_Click(object sender, RoutedEventArgs e)
        {
            var window = new BarcodeWindow(_artikelRepo, _lang)
            {
                Owner = this
            };

            if (window.ShowDialog() == true && window.SelectedArtikel != null)
            {
                _warenkorb.Add(window.SelectedArtikel);
            }
        }

        // Startet den Bezahlvorgang und druckt den Kassenbon.
        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            if (_warenkorb.Count == 0)
            {
                MessageBox.Show(_lang.T("Message_NoItems"));
                return;
            }

            _receiptService.PrintReceipt(_warenkorb.ToList());
            _warenkorb.Clear();
        }

        // Neuen Artikel mit ArtikelDialog anlegen.
        private async void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ArtikelDialog
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                var artikel = dialog.Artikel;

                await _artikelRepo.InsertAsync(artikel);
                _artikelListe.Add(artikel);
            }
        }

        // Ausgewählten Artikel bearbeiten.
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel selected)
                return;

            // Kopie für den Dialog, damit bei Abbrechen nichts verändert wird.
            var copy = new Artikel
            {
                Barcode = selected.Barcode,
                Name = selected.Name,
                Preis = selected.Preis,
                Bestand = selected.Bestand
            };

            var dialog = new ArtikelDialog(copy)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                // Änderungen aus dem Dialog übernehmen.
                selected.Name = dialog.Artikel.Name;
                selected.Preis = dialog.Artikel.Preis;
                selected.Bestand = dialog.Artikel.Bestand;

                await _artikelRepo.UpdateAsync(selected);
                dgArtikel.Items.Refresh();
            }
        }

        // Ausgewählten Artikel löschen.
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel selected)
                return;

            if (MessageBox.Show($"Artikel '{selected.Name}' löschen?", "Löschen",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await _artikelRepo.DeleteAsync(selected.Barcode);
            _artikelListe.Remove(selected);
        }
    }
}
