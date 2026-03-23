using System.Threading.Tasks;
using System.Windows;

namespace KasseApp.Views
{
    public partial class BarcodeWindow : Window
    {
        private readonly ArtikelRepository _repo;
        private readonly LanguageService _lang;

        public Artikel? SelectedArtikel { get; private set; }

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
            txtTitleBar.Text = _lang.T("BarcodeWindow_Title");        // z.B. "Barcode-Scan"
            btnClose.Content = _lang.T("BarcodeWindow_Cancel");       // Text für „Schließen“
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
                txtArtikelInfo.Text = _lang.T("BarcodeWindow_Info"); // z.B. „Bitte Barcode scannen oder eingeben.“
            }
            else
            {
                txtArtikelInfo.Text = $"{artikel.Name} – {artikel.Preis:0.00} € (Bestand: {artikel.Bestand})";
            }
        }

        private async void txtBarcodeInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                await LoadArtikelAsync(txtBarcodeInput.Text.Trim());
                if (SelectedArtikel == null)
                    return;
            }

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
