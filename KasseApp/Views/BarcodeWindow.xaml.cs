using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KasseApp.Views
{
    public partial class BarcodeWindow : Window
    {
        private readonly ArtikelRepository _artikelRepo;
        private readonly LanguageService _lang;

        public Artikel? SelectedArtikel { get; private set; }
        public bool AddRequested { get; private set; } = true;

        public BarcodeWindow(ArtikelRepository artikelRepo, LanguageService lang)
        {
            InitializeComponent();

            _artikelRepo = artikelRepo;
            _lang = lang;

            this.Title = _lang.T("Title_BarcodeWindow");

            Loaded += (sender, args) => txtBarcodeInput.Focus();

            txtBarcodeInput.TextChanged += TxtBarcodeInput_TextChanged;
            txtBarcodeInput.KeyDown += TxtBarcodeInput_KeyDown;

            btnAdd.Click += BtnAdd_Click;
            btnRemove.Click += BtnRemove_Click;
            btnClose.Click += (_, _) => Close();
        }

        private void TxtBarcodeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtBarcodeDisplay.Text = txtBarcodeInput.Text;
        }

        private async void TxtBarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string barcode = txtBarcodeInput.Text.Trim();
                if (!string.IsNullOrEmpty(barcode))
                {
                    await SearchBarcodeAsync(barcode);
                }
            }
        }

        private async Task SearchBarcodeAsync(string barcode)
        {
            try
            {
                var artikel = await _artikelRepo.GetByBarcodeAsync(barcode);
                if (artikel != null)
                {
                    SelectedArtikel = artikel;
                    txtArtikelInfo.Text = $"{artikel.Name}  ({artikel.Preis:0.00} €, Bestand: {artikel.Bestand})";
                }
                else
                {
                    SelectedArtikel = null;
                    txtArtikelInfo.Text = "Barcode nicht gefunden.";
                }
            }
            catch
            {
                MessageBox.Show(_lang.T("Message_ErrorDb"));
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                MessageBox.Show("Kein Artikel geladen.");
                return;
            }

            AddRequested = true;
            DialogResult = true;
            Close();
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArtikel == null)
            {
                MessageBox.Show("Kein Artikel geladen.");
                return;
            }

            AddRequested = false;
            DialogResult = true;
            Close();
        }
    }
}
