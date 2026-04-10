using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KasseApp.Views
{
    public partial class BarcodeWindow : Window
    {
        private readonly ArtikelRepository _repo;
        private readonly LanguageService _lang;

        public Artikel? SelectedArtikel { get; private set; }
        
        // Diese Flagge sagt dem MainWindow, ob wir verkaufen (false) 
        // oder das Lager erhöhen (true) wollen.
        public bool IsLagerBuchung { get; private set; } = false;
        public bool IsLagerRemove { get; private set; } = false;

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
            if (string.IsNullOrWhiteSpace(barcode))
            {
                txtBarcodeDisplay.Text = "";
                txtArtikelInfo.Text = "";
                SelectedArtikel = null;
                return;
            }

            txtBarcodeDisplay.Text = barcode;

            var artikel = await _repo.GetByBarcodeAsync(barcode);
            SelectedArtikel = artikel;

            if (artikel == null)
            {
                txtArtikelInfo.Text = _lang.T("BarcodeWindow_Info");
            }
            else
            {
                // Anzeige von Name, Preis und aktuellem Bestand
                txtArtikelInfo.Text = $"{artikel.Name} – {artikel.Preis:0.00} € (Bestand: {artikel.Bestand})";
            }
        }

        private async void txtBarcodeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
        }

        // Button: Zum Warenkorb hinzufügen (Verkauf)
        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
                if (SelectedArtikel == null)
                    return;
            }

            IsLagerBuchung = false; // Sicherstellen, dass es kein Lager-Zusatz ist
            DialogResult = true;
            Close();
        }

        // Button: ZusatzZahl++ (Lager-Eingang)
        private async void btnZusatzZahlAdd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
                if (SelectedArtikel == null)
                    return;
            }

            IsLagerBuchung = true; // Markierung für das Lager-Update
            DialogResult = true;
            Close();
        }
        private async void btnZusatzZahlRemove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
                if (SelectedArtikel == null)
                    return;
            }

            IsLagerRemove = true; // Markierung für das Lager-Update
            DialogResult = true;
            Close();
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