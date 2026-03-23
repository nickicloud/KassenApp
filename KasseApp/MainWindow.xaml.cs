using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using KasseApp.Views;
using Microsoft.Extensions.Configuration;
using Npgsql; // für PostgresException (UniqueViolation 23505)

namespace KasseApp
{
    public partial class MainWindow : Window
    {
        private readonly ArtikelRepository _artikelRepo;
        private readonly LanguageService _lang;
        private readonly ReceiptService _receiptService;
        private readonly LabelPrintService _labelPrintService;
        private readonly IConfiguration _configuration;

        private ObservableCollection<Artikel> _artikelListe = new ObservableCollection<Artikel>();
        private readonly ObservableCollection<WarenkorbPosition> _warenkorb = new ObservableCollection<WarenkorbPosition>();
        private readonly ObservableCollection<ScanHistoryEntry> _barcodeHistory = new ObservableCollection<ScanHistoryEntry>();

        private bool _hideZeroStock = false;

        public MainWindow()
        {
            InitializeComponent();

            // Fix für "jeder zweite Artikel ist weiß"
            ApplyRowColorFix();

            var config = ConfigService.Load();

            _lang = new LanguageService();
            _lang.Load(config.General.Language);

            _artikelRepo = new ArtikelRepository(config.Database.ToConnectionString());
            _receiptService = new ReceiptService(config.General.ReceiptPrinterName);

            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();

            _labelPrintService = new LabelPrintService(_configuration);

            ApplyLanguageTexts();
            ApplyColumnVisibilityFromConfig(); // nur lesen

            _ = LoadArtikelAsync();

            lstBarcodeHistory.ItemsSource = _barcodeHistory;
            ClearDetails();
        }

        // ----------------------------
        // Fix: Alternating Row Colors
        // ----------------------------
        private void ApplyRowColorFix()
        {
            dgArtikel.AlternationCount = 2; // AlternationIndex aktivieren [web:198]

            var baseStyle = dgArtikel.RowStyle; // falls XAML schon RowStyle hat
            var style = new Style(typeof(DataGridRow), baseStyle);

            style.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(DataGridRow.ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(DataGridRow.FontSizeProperty, 13.0));
            style.Setters.Add(new Setter(DataGridRow.CursorProperty, Cursors.Hand));
            style.Setters.Add(new Setter(UIElement.SnapsToDevicePixelsProperty, true));

            // Odd rows
            var odd = new Trigger
            {
                Property = ItemsControl.AlternationIndexProperty,
                Value = 1
            };
            odd.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                (Brush)new BrushConverter().ConvertFromString("#FF111827")));
            style.Triggers.Add(odd);

            // Hover
            var hover = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                (Brush)new BrushConverter().ConvertFromString("#FF111827")));
            style.Triggers.Add(hover);

            // Selected
            var selected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            selected.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                (Brush)new BrushConverter().ConvertFromString("#402563EB")));
            selected.Setters.Add(new Setter(DataGridRow.BorderBrushProperty,
                (Brush)new BrushConverter().ConvertFromString("#FF2563EB")));
            selected.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            style.Triggers.Add(selected);

            dgArtikel.RowStyle = style;
        }

        // ----------------------------
        // Columns visibility (config)
        // ----------------------------
        private void ApplyColumnVisibilityFromConfig()
        {
            bool showBarcode = _configuration.GetValue("Ui:Columns:Barcode", true);
            bool showName = _configuration.GetValue("Ui:Columns:Name", true);
            bool showPreis = _configuration.GetValue("Ui:Columns:Preis", true);
            bool showBestand = _configuration.GetValue("Ui:Columns:Bestand", true);

            SetColumnVisibility("Barcode", showBarcode);
            SetColumnVisibility("Name", showName);
            SetColumnVisibility("Preis", showPreis);
            SetColumnVisibility("Bestand", showBestand);
        }

        private void SetColumnVisibility(string member, bool visible)
        {
            var col = dgArtikel.Columns.FirstOrDefault(c =>
                string.Equals(c.SortMemberPath, member, StringComparison.OrdinalIgnoreCase));

            if (col == null) return;

            col.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        // ----------------------------
        // Language texts
        // ----------------------------
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
            btnPrintLabel.Content = _lang.T("Button_PrintLabel");

            colBarcode.Header = _lang.T("Grid_Col_Barcode");
            colName.Header = _lang.T("Grid_Col_Name");
            colPreis.Header = _lang.T("Grid_Col_Preis");
            colBestand.Header = _lang.T("Grid_Col_Bestand");

            lblHistoryHeader.Text = _lang.T("History_Header");
            lblDetailHeader.Text = _lang.T("History_Detail_Header");

            miCopyId.Header = _lang.T("Context_Product_CopyId");
            miAddToCart.Header = _lang.T("Context_Product_AddToCart");
        }

        // ----------------------------
        // Load + filter
        // ----------------------------
        private async Task LoadArtikelAsync()
        {
            try
            {
                var liste = await _artikelRepo.GetAllAsync();
                _artikelListe = new ObservableCollection<Artikel>(liste);
                dgArtikel.ItemsSource = _artikelListe;

                ApplyColumnVisibilityFromConfig();
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

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyZeroFilter();

        // ----------------------------
        // Sorting: Preis -> Name
        // ----------------------------
        public void dgArtikel_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (!string.Equals(e.Column.SortMemberPath, "Preis", StringComparison.OrdinalIgnoreCase))
                return;

            e.Handled = true;

            var view = CollectionViewSource.GetDefaultView(dgArtikel.ItemsSource);
            if (view == null) return;

            var dir = e.Column.SortDirection != ListSortDirection.Ascending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Preis", dir));
                view.SortDescriptions.Add(new SortDescription("Name", dir)); // sekundär ABC [web:2]
            }

            foreach (var col in dgArtikel.Columns)
                col.SortDirection = null;

            e.Column.SortDirection = dir;
        }

        // ----------------------------
        // Barcode button
        // ----------------------------
        private async void BtnBarcode_Click(object sender, RoutedEventArgs e)
        {
            var window = new BarcodeWindow(_artikelRepo, _lang) { Owner = this };

            if (window.ShowDialog() == true && window.SelectedArtikel != null)
            {
                var artikel = window.SelectedArtikel;

                txtSearch.Text = artikel.Barcode;
                await LoadArtikelAsync();

                var pos = _warenkorb.FirstOrDefault(p => p.Artikel.Barcode == artikel.Barcode);
                if (pos == null)
                {
                    pos = new WarenkorbPosition { Artikel = artikel, Menge = 1 };
                    _warenkorb.Add(pos);
                }
                else
                {
                    pos.Menge++;
                }

                _barcodeHistory.Add(new ScanHistoryEntry
                {
                    Barcode = artikel.Barcode,
                    Name = artikel.Name,
                    Preis = artikel.Preis,
                    BestandNachScan = artikel.Bestand,
                    MengeImWarenkorb = pos.Menge
                });
            }
        }

        // ----------------------------
        // History selection
        // ----------------------------
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

        // ----------------------------
        // Context menu selection
        // ----------------------------
        private DataGridRow GetDataGridRowAtPoint(DataGrid grid, Point point)
        {
            DependencyObject element = grid.InputHitTest(point) as DependencyObject;
            while (element != null && element is not DataGridRow)
                element = VisualTreeHelper.GetParent(element);
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
                    var miRefresh = emptyMenu.Items.Count > 0 ? emptyMenu.Items[0] as MenuItem : null;
                    if (miRefresh != null)
                        miRefresh.Header = _lang.T("Context_Empty_Refresh");

                    var miZero = emptyMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.IsCheckable);
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
            if (dgArtikel.SelectedItem is not Artikel a) return;
            Clipboard.SetText(a.Barcode);
        }

        private void Menu_AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel a) return;

            var pos = _warenkorb.FirstOrDefault(p => p.Artikel.Barcode == a.Barcode);
            if (pos == null)
            {
                pos = new WarenkorbPosition { Artikel = a, Menge = 1 };
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

        private async void Menu_Refresh_Click(object sender, RoutedEventArgs e) => await LoadArtikelAsync();

        private void Menu_ToggleZero_Click(object sender, RoutedEventArgs e)
        {
            _hideZeroStock = !_hideZeroStock;
            ApplyZeroFilter();
        }

        // ----------------------------
        // Pay
        // ----------------------------
        private async void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new CartWindow(_lang, _warenkorb) { Owner = this };
            var result = cartWindow.ShowDialog();

            if (result != true || _warenkorb.Count == 0) return;

            foreach (var pos in _warenkorb)
            {
                pos.Artikel.Bestand -= pos.Menge;
                if (pos.Artikel.Bestand < 0) pos.Artikel.Bestand = 0;
            }

            _receiptService.PrintReceipt(_warenkorb.ToList());

            foreach (var pos in _warenkorb)
                await _artikelRepo.UpdateBestandAsync(pos.Artikel.Barcode, pos.Artikel.Bestand);

            await LoadArtikelAsync();
            _warenkorb.Clear();
            _barcodeHistory.Clear();
            ClearDetails();
        }

        // ----------------------------
        // NEW: prevent crash on duplicate barcode
        // ----------------------------
        private async void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ArtikelDialog(_lang) { Owner = this };

            if (dialog.ShowDialog() != true)
                return;

            var artikel = dialog.Artikel;

            // schneller UI-Check (freundlich, aber DB check ist entscheidend)
            if (IsBarcodeAlreadyInList(artikel.Barcode))
            {
                MessageBox.Show(
                    "Dieser Barcode existiert bereits. Bitte einen anderen Barcode eingeben.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning); // MessageBox API [web:210]
                return;
            }

            try
            {
                await _artikelRepo.InsertAsync(artikel);
                _artikelListe.Add(artikel);
                ApplyZeroFilter();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // PostgreSQL unique_violation 23505 => Duplicate Barcode [web:212]
                MessageBox.Show(
                    "Dieser Barcode existiert bereits. Bitte einen anderen Barcode eingeben.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning); // [web:210]
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Fehler beim Speichern des Artikels:\n" + ex.Message,
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error); // [web:210]
            }
        }

        private bool IsBarcodeAlreadyInList(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;

            barcode = barcode.Trim();

            return _artikelListe.Any(a =>
                string.Equals(a.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
        }

        // ----------------------------
        // Edit / Delete / Label
        // ----------------------------
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel selected) return;

            var copy = new Artikel
            {
                Barcode = selected.Barcode,
                Name = selected.Name,
                Preis = selected.Preis,
                Bestand = selected.Bestand
            };

            var dialog = new ArtikelDialog(_lang, copy) { Owner = this };

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
            if (dgArtikel.SelectedItem is not Artikel selected) return;

            string text = string.Format(_lang.T("Dialog_Delete_Text"), selected.Name);

            if (MessageBox.Show(text, _lang.T("Dialog_Delete_Title"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await _artikelRepo.DeleteAsync(selected.Barcode);
            _artikelListe.Remove(selected);
        }

        private async void BtnPrintLabel_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is not Artikel selected)
            {
                MessageBox.Show(_lang.T("Message_NoArticleSelected"));
                return;
            }

            var window = new LabelPrintWindow(_lang, _labelPrintService, selected) { Owner = this };
            var result = window.ShowDialog();

            if (result == true)
            {
                selected.Bestand += window.Anzahl;
                await _artikelRepo.UpdateBestandAsync(selected.Barcode, selected.Bestand);

                dgArtikel.Items.Refresh();
                ApplyZeroFilter();
            }
        }

        private async void BtnNew_Click_Alt(object sender, RoutedEventArgs e)
        {
            // Nicht benutzt – nur Platzhalter, falls du irgendwo doppelte Handler hast.
            await Task.CompletedTask;
        }

        // ----------------------------
        // Double click
        // ----------------------------
        private void dgArtikel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgArtikel.SelectedItem is Artikel)
                BtnEdit_Click(sender, e);
        }

        // ----------------------------
        // Window controls
        // ----------------------------
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
