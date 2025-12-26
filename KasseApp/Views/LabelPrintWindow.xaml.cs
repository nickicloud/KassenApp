using System;
using System.Windows;

namespace KasseApp.Views
{
    public partial class LabelPrintWindow : Window
    {
        private readonly LanguageService _lang;
        private readonly LabelPrintService _labelService;

        public Artikel Artikel { get; }
        public int Anzahl { get; private set; } = 1;
        public bool UseA4 { get; private set; } = true;

        public LabelPrintWindow(LanguageService lang, LabelPrintService labelService, Artikel artikel)
        {
            _lang = lang;
            _labelService = labelService;
            Artikel = artikel;

            InitializeComponent();
            ApplyLang();

            lblArtikelInfo.Text = $"{artikel.Barcode} – {artikel.Name} – {artikel.Preis:0.00} €";
            txtAnzahl.Text = "1";
        }

        private void ApplyLang()
        {
            Title = _lang.T("LabelWindow_Title");
            txtTitleBar.Text = Title;
            btnPrint.Content = _lang.T("LabelWindow_Print");
            btnCancel.Content = _lang.T("LabelWindow_Cancel");
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtAnzahl.Text, out var n) || n <= 0)
            {
                MessageBox.Show(_lang.T("LabelWindow_InvalidCount"));
                return;
            }

            Anzahl = n;
            UseA4 = rbA4.IsChecked == true;

            try
            {
                // Drucken
                if (UseA4)
                    _labelService.PrintA4Label(Artikel, Anzahl);
                else
                    _labelService.PrintLabelPrinter(Artikel, Anzahl);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(_lang.T("LabelWindow_PrintError") + Environment.NewLine + ex.Message);
            }
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
