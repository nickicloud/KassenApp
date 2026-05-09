using System;
using System.Threading; // Neu für CancellationToken
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KasseApp.Views
{
    public partial class BarcodeWindow : Window
    {
        private readonly ArtikelRepository _repo;
        private readonly LanguageService _lang;
        private CancellationTokenSource? _cts; 

        public Artikel? SelectedArtikel { get; private set; }
        
        // Verkaufen / Hinzufügen
        public bool IsLagerBuchung { get; private set; } = false;
        public bool IsLagerRemove { get; private set; } = false;
        
        public event System.Action<BarcodeWindow>? ArtikelProzessiert;

        public BarcodeWindow(ArtikelRepository repo, LanguageService lang)
        {
            _repo = repo;
            _lang = lang;
            InitializeComponent();
            ApplyLang();
            Loaded += (_, _) => txtBarcodeInput.Focus();
        }

        private void ApplyLang()
        {
            Title = _lang.T("BarcodeWindow_Title");
            txtTitleBar.Text = _lang.T("BarcodeWindow_Title");
            btnClose.Content = _lang.T("BarcodeWindow_Cancel");
        }

        private async Task LoadArtikelAsync(string barcode)
        {
            // Vorherige Suche abbrechen, falls der Scanner noch tippt
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            if (string.IsNullOrWhiteSpace(barcode))
            {
                txtBarcodeDisplay.Text = "";
                txtArtikelInfo.Text = "";
                txtArtikelInfo.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                SelectedArtikel = null;
                return;
            }

            try 
            {
                // Kurze Pause
                await Task.Delay(350, token);

                txtBarcodeDisplay.Text = barcode;
                var artikel = await _repo.GetByBarcodeAsync(barcode);
                
                if (token.IsCancellationRequested) return;

                SelectedArtikel = artikel;

                if (artikel == null)
                {
                    txtArtikelInfo.Text = "FEHLER: Barcode nicht gefunden!"; 
                    txtArtikelInfo.Foreground = Brushes.Red;
                }
                else
                {
                    txtArtikelInfo.Foreground = Brushes.White;
                    txtArtikelInfo.Text = $"{artikel.Name} – {artikel.Preis:0.00} € (Bestand: {artikel.Bestand})";

                    if (chkAutoAdd.IsChecked == true)
                    {
                        ExecuteAutoMode();
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void ExecuteAutoMode()
        {
            switch (cmbAutoMode.SelectedIndex)
            {
                case 0: 
                    IsLagerBuchung = false;
                    IsLagerRemove = false;
                    break;
                case 1: 
                    IsLagerBuchung = true;
                    IsLagerRemove = false;
                    break;
                case 2: 
                    IsLagerBuchung = false;
                    IsLagerRemove = true;
                    break;
            }

            ArtikelProzessiert?.Invoke(this);
            ResetForNextScan();
        }

        private async void txtBarcodeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
        }
        
        private void ResetForNextScan()
        {
            txtBarcodeInput.Clear();
            txtBarcodeInput.Focus();
        }

        // Button: Zum Warenkorb hinzufügen (Verkauf)
        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
                if (SelectedArtikel == null) return;
            }

            IsLagerBuchung = false; 
            IsLagerRemove = false;
            ArtikelProzessiert?.Invoke(this);
            ResetForNextScan();
        }

        // Button: ZusatzZahl++ (Lager-Eingang)
        private async void btnZusatzZahlAdd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
                if (SelectedArtikel == null) return;
            }

            IsLagerBuchung = true; 
            IsLagerRemove = false;
            ArtikelProzessiert?.Invoke(this);
            ResetForNextScan();
        }

        private async void btnZusatzZahlRemove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
                if (SelectedArtikel == null) return;
            }

            IsLagerRemove = true; 
            IsLagerBuchung = false;
            ArtikelProzessiert?.Invoke(this);
            ResetForNextScan();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            txtBarcodeInput.Text = "";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }
    }
}