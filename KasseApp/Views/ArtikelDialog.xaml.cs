using System.Globalization;
using System.Windows;

namespace KasseApp.Views
{
    public partial class ArtikelDialog : Window
    {
        private readonly LanguageService _lang;
        public Artikel Artikel { get; private set; }

        public ArtikelDialog(LanguageService lang)
        {
            _lang = lang;
            InitializeComponent();
            Artikel = new Artikel();
            ApplyLang(false);
        }

        public ArtikelDialog(LanguageService lang, Artikel existing)
        {
            _lang = lang;
            InitializeComponent();
            Artikel = existing;
            ApplyLang(true);

            txtBarcode.Text = existing.Barcode;
            txtBarcode.IsReadOnly = true;          // Barcode nicht mehr editierbar
            txtBarcode.IsTabStop = false;          // optional: nicht fokussierbar
            txtBarcode.Cursor = System.Windows.Input.Cursors.Arrow;

            txtName.Text = existing.Name;
            txtPreis.Text = existing.Preis.ToString(CultureInfo.InvariantCulture);
            txtBestand.Text = existing.Bestand.ToString();
            txtZusatzZahl.Text = existing.ZusatzZahl.ToString();
            txtZusatzText.Text = existing.ZusatzText.ToString();
        }


        private void ApplyLang(bool edit)
        {
            Title = edit ? _lang.T("ArtikelDialog_Title_Edit") : _lang.T("ArtikelDialog_Title_New");
            txtTitleBar.Text = Title;

            lblBarcode.Text = _lang.T("ArtikelDialog_Barcode");
            lblName.Text = _lang.T("ArtikelDialog_Name");
            lblPreis.Text = _lang.T("ArtikelDialog_Preis");
            lblBestand.Text = _lang.T("ArtikelDialog_Bestand");
            lblZusatzZahl.Text = _lang.T("ArtikelDialog_ZusatzZahl");
            lblZusatzText.Text = _lang.T("ArtikelDialog_ZusatzText");

            btnOk.Content = _lang.T("ArtikelDialog_Ok");
            btnCancel.Content = _lang.T("ArtikelDialog_Cancel");
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Artikel.Barcode = txtBarcode.Text.Trim();
            Artikel.Name = txtName.Text.Trim();

            if (decimal.TryParse(txtPreis.Text.Replace(',', '.'),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out var preis))
                Artikel.Preis = preis;

            if (int.TryParse(txtBestand.Text, out var best))
                Artikel.Bestand = best;
            if (int.TryParse(txtZusatzZahl.Text, out var zahl))
                Artikel.ZusatzZahl = zahl;
            Artikel.ZusatzText = txtZusatzText.Text.Trim();


            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
