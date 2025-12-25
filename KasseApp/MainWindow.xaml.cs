using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using KasseApp.Views;

namespace KasseApp
{
    public partial class MainWindow : Window
    {
        private readonly ArtikelRepository _artikelRepo;
        private readonly LanguageService _lang;
        private readonly ReceiptService _receiptService;

        private ObservableCollection<Artikel> _artikelListe = new();
        private readonly ObservableCollection<WarenkorbPosition> _warenkorb = new();
        private readonly ObservableCollection<ScanHistoryEntry> _barcodeHistory = new();

        private bool _hideZeroStock = false;

        public MainWindow()
        {
            InitializeComponent();

            var config = ConfigService.Load();

            _lang = new LanguageService();
            _lang.Load(config.General.Language);

            _artikelRepo = new ArtikelRepository(config.Database.ToConnectionString());
            _receiptService = new ReceiptService(config.General.ReceiptPrinterName);

            ApplyLanguageTexts();
            _ = LoadArtikelAsync();

            btnBarcode.Click += BtnBarcode_Click;
            btnPay.Click += BtnPay_Click;
            btnNew.Click += BtnNew_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            txtSearch.TextChanged += TxtSearch_TextChanged;

            lstBarcodeHistory.ItemsSource = _barcodeHistory;
            ClearDetails();
        }

        private void ApplyLanguageTexts()
        {
            Title = _lang.T("Title_MainWindow");
            txtHeaderTitle.Text = _lang.T("Main_Header");

            txtSearch.ToolTip = _lang.T("Search_Placeholder");

            btnBarcode.Content = _lang.T("Button_Barcode");
            btnPay.Content = _lang.T("Button_Pay");
            btnNew.Content = _lang.T("Button_New");
            btnEdit.Content = _lang.T("Button_Edit");
            btnDelete.Content = _lang.T("Button_Delete");

            colBarcode.Header = _lang.T("Grid_Col_Barcode");
            colName.Header = _lang.T("Grid_Col_Name");
            colPreis.Header = _lang.T("Grid_Col_Preis");
            colBestand.Header = _lang.T("Grid_Col_Bestand");

            lblHistoryHeader.Text = _lang.T("History_Header");
            lblDetailHeader.Text = _lang.T("History_Detail_Header");

            miCopyId.Header = _lang.T("Context_Product_CopyId");
            miAddToCart.Header = _lang.T("Context_Product_AddToCart");
        }

        private async Task LoadArtikelAsync()
        {
            try
            {
                var liste = await _artikelRepo.GetAllAsync();
                _artikelListe = new ObservableCollection<Artikel>(liste);
                dgArtikel.ItemsSource = _artikelListe;
                ApplyZeroFilter();
            }
            catch
            {
                MessageBox.Show(_lang.T("Message_ErrorDb"));
            }
        }

        private void ApplyZeroFilter()
        {
            if (dgArtikel.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(dgArtikel.ItemsSource);
            string search = txtSearch.Text?.Trim().ToLower() ?? "";

            view.Filter = obj =>
            {
                if (obj is not Artikel a) return false;

                bool matchesSearch =
                    string.IsNullOrEmpty(search) ||
                    (a.Name?.ToLower().Contains(search) == true) ||
                    (a.Barcode?.ToLower().Contains(search) == true);

                bool passesStock = !_hideZeroStock || a.Bestand > 0;

                return matchesSearch && passesStock;
            };
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyZeroFilter();
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

                txtSearch.Text = artikel.Barcode;
                await LoadArtikelAsync();

                var pos = _warenkorb.FirstOrDefault(p => p.Artikel.Barcode == artikel.Barcode);
                if (pos == null)
                {
                    pos = new WarenkorbPosition
                    {
                        Artikel = artikel,
                        Menge = 1
                    };
                    _warenkorb.Add(pos);
                }
                else
                {
                    pos.Menge++;
                }

                var entry = new ScanHistoryEntry
                {
                    Barcode = artikel.Barcode,
                    Name = artikel.Name,
                    Preis = artikel.Preis,
                    BestandNachScan = artikel.Bestand,
                    MengeImWarenkorb = pos.Menge
                };
                _barcodeHistory.Add(entry);
            }
        }

        private void lstBarcodeHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstBarcodeHistory.SelectedItem is not ScanHistoryEntry entry)
            {
                ClearDetails();
                return;
            }

            lblDetailBarcode.Text = $"{_lang.T("History_Detail_Barcode")}: {entry.Barcode}";
            lblDetailName.Text = $"{_lang.T("History_Detail_Name")}: {entry.Name}";
            lblDetailPreis.Text = $"{_lang.T("History_Detail_Preis")}: {entry.Preis:0.00} €";
            lblDetailBestand.Text = $"{_lang.T("History_Detail_BestandNachScan")}: {entry.BestandNachScan}";
            lblDetailMenge.Text = $"{_lang.T("History_Detail_MengeImWarenkorb")}: {entry.MengeImWarenkorb}";
        }

        private void ClearDetails()
        {
            lblDetailBarcode.Text = $"{_lang.T("History_Detail_Barcode")}: -";
            lblDetailName.Text = $"{_lang.T("History_Detail_Name")}: -";
            lblDetailPreis.Text = $"{_lang.T("History_Detail_Preis")}: -";
            lblDetailBestand.Text = $"{_lang.T("History_Detail_BestandNachScan")}: -";
            lblDetailMenge.Text = $"{_lang.T("History_Detail_MengeImWarenkorb")}: -";
        }

        private DataGridRow? GetDataGridRowAtPoint(DataGrid grid, Point point)
        {
            var element = grid.InputHitTest(point) as DependencyObject;
            while (element != null && element is not DataGridRow)
            {
                element = VisualTreeHelper.GetParent(element);
            }
            return element as DataGridRow;
        }

        private void dgArtikel_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(dgArtikel);
            var row = GetDataGridRowAtPoint(dgArtikel, pos);

            if (row != null)
            {
                dgArtikel.SelectedItem = row.Item;
                dgArtikel.ContextMenu = cmArtikel;
            }
            else
            {
                dgArtikel.SelectedItem = null;
                if (FindResource("cmEmpty") is ContextMenu emptyMenu)
                {
                    var miRefresh = emptyMenu.Items[0] as MenuItem;
                    if (miRefresh != null)
                        miRefresh.Header = _lang.T("Context_Empty_Refresh");

                    var miZero = emptyMenu.Items
                        .OfType<MenuItem>()
                        .FirstOrDefault(m => m.IsCheckable);
                    if (miZero != null)
                    {
                        miZero.Header = _lang.T("Context_Empty_HideZero");
                        miZero.IsChecked = _hideZeroStock;
                    }

                    dgArtikel.ContextMenu = emptyMenu;
                }
            }
        }

        private void Menu_CopyId_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel a)
                return;

            Clipboard.SetText(a.Barcode);
        }

        private void Menu_AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel a)
                return;

            var pos = _warenkorb.FirstOrDefault(p => p.Artikel.Barcode == a.Barcode);
            if (pos == null)
            {
                pos = new WarenkorbPosition
                {
                    Artikel = a,
                    Menge = 1
                };
                _warenkorb.Add(pos);
            }
            else
            {
                pos.Menge++;
            }

            _barcodeHistory.Add(new ScanHistoryEntry
            {
                Barcode = a.Barcode,
                Name = a.Name,
                Preis = a.Preis,
                BestandNachScan = a.Bestand,
                MengeImWarenkorb = pos.Menge
            });
        }

        private async void Menu_Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadArtikelAsync();
        }

        private void Menu_ToggleZero_Click(object sender, RoutedEventArgs e)
        {
            _hideZeroStock = !_hideZeroStock;
            ApplyZeroFilter();
        }

        private async void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new CartWindow(_lang, _warenkorb)
            {
                Owner = this
            };

            var result = cartWindow.ShowDialog();

            if (result != true || _warenkorb.Count == 0)
            {
                return;
            }

            // Bestand lokal verringern
            foreach (var pos in _warenkorb)
            {
                pos.Artikel.Bestand -= pos.Menge;
                if (pos.Artikel.Bestand < 0)
                    pos.Artikel.Bestand = 0;
            }

            // Bon drucken
            _receiptService.PrintReceipt(_warenkorb.ToList());

            // Bestand in der DB speichern
            foreach (var pos in _warenkorb)
            {
                await _artikelRepo.UpdateBestandAsync(pos.Artikel.Barcode, pos.Artikel.Bestand);
            }

            await LoadArtikelAsync();
            _warenkorb.Clear();
            _barcodeHistory.Clear();
            ClearDetails();
        }

        private async void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ArtikelDialog(_lang)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                var artikel = dialog.Artikel;

                await _artikelRepo.InsertAsync(artikel);
                _artikelListe.Add(artikel);
                ApplyZeroFilter();
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

            var dialog = new ArtikelDialog(_lang, copy)
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
                ApplyZeroFilter();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel selected)
                return;

            string text = string.Format(_lang.T("Dialog_Delete_Text"), selected.Name);

            if (MessageBox.Show(text, _lang.T("Dialog_Delete_Title"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await _artikelRepo.DeleteAsync(selected.Barcode);
            _artikelListe.Remove(selected);
        }

        private void dgArtikel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgArtikel.SelectedItem is Artikel)
            {
                BtnEdit_Click(sender, e);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
