using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using KasseApp.Views;

namespace KasseApp
{
    public partial class MainWindow : Window
    {
        private readonly ArtikelRepository _artikelRepo;
        private readonly LanguageService _lang;
        private readonly ReceiptService _receiptService;

        private ObservableCollection<Artikel> _artikelListe = new();
        private ObservableCollection<WarenkorbPosition> _warenkorb = new();

        public MainWindow()
        {
            InitializeComponent();

            var config = ConfigService.Load();

            _lang = new LanguageService();
            _lang.Load(config.General.Language);

            _artikelRepo = new ArtikelRepository(config.Database.ToConnectionString());
            _receiptService = new ReceiptService(config.General.ReceiptPrinterName);

            this.Title = _lang.T("Title_MainWindow");

            _ = LoadArtikelAsync();

            btnBarcode.Click += BtnBarcode_Click;
            btnPay.Click += BtnPay_Click;
            btnNew.Click += BtnNew_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            txtSearch.TextChanged += TxtSearch_TextChanged;
        }

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

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dgArtikel.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(dgArtikel.ItemsSource);
            string search = txtSearch.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(search))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = obj =>
                {
                    if (obj is not Artikel a) return false;
                    return (a.Name?.ToLower().Contains(search) == true) ||
                           (a.Barcode?.ToLower().Contains(search) == true);
                };
            }
        }

        private async void BtnBarcode_Click(object sender, RoutedEventArgs e)
        {
            var window = new BarcodeWindow(_artikelRepo, _lang)
            {
                Owner = this
            };

            if (window.ShowDialog() == true && window.SelectedArtikel != null)
            {
                var artikel = window.SelectedArtikel;

                // Suchleiste auf Barcode setzen und filtern
                txtSearch.Text = artikel.Barcode;

                var pos = _warenkorb.FirstOrDefault(p => p.Artikel.Barcode == artikel.Barcode);
                if (window.AddRequested)
                {
                    if (pos == null)
                    {
                        _warenkorb.Add(new WarenkorbPosition
                        {
                            Artikel = artikel,
                            Menge = 1
                        });
                    }
                    else
                    {
                        pos.Menge++;
                    }
                }
                else
                {
                    if (pos != null)
                    {
                        pos.Menge--;
                        if (pos.Menge <= 0)
                            _warenkorb.Remove(pos);
                    }
                }
            }
        }

        private async void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            if (_warenkorb.Count == 0)
            {
                MessageBox.Show(_lang.T("Message_NoItems"));
                return;
            }

            _receiptService.PrintReceipt(_warenkorb.ToList());

            foreach (var pos in _warenkorb)
            {
                pos.Artikel.Bestand -= pos.Menge;
                await _artikelRepo.UpdateAsync(pos.Artikel);
            }

            await LoadArtikelAsync();
            _warenkorb.Clear();
        }

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

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel selected)
                return;

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
                selected.Name = dialog.Artikel.Name;
                selected.Preis = dialog.Artikel.Preis;
                selected.Bestand = dialog.Artikel.Bestand;

                await _artikelRepo.UpdateAsync(selected);
                dgArtikel.Items.Refresh();
            }
        }

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
