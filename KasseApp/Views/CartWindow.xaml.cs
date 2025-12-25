using System.Collections.ObjectModel;
using System.Windows;

namespace KasseApp.Views
{
    public partial class CartWindow : Window
    {
        private readonly LanguageService _lang;
        private readonly ObservableCollection<WarenkorbPosition> _warenkorb;

        public CartWindow(LanguageService lang, ObservableCollection<WarenkorbPosition> warenkorb)
        {
            _lang = lang;
            _warenkorb = warenkorb;

            InitializeComponent();
            dgCart.ItemsSource = _warenkorb;
            ApplyLang();
        }

        private void ApplyLang()
        {
            Title = _lang.T("CartWindow_Title");
            txtTitleBar.Text = _lang.T("CartWindow_Header");
            colArtikel.Header = _lang.T("CartWindow_Col_Artikel");
            colMenge.Header = _lang.T("CartWindow_Col_Menge");
            colPreis.Header = _lang.T("CartWindow_Col_Preis");
            btnPay.Content = _lang.T("CartWindow_Pay");
            btnCancel.Content = _lang.T("CartWindow_Cancel");
            btnClear.Content = _lang.T("CartWindow_Clear");          // neuen Key anlegen
            miDeletePos.Header = _lang.T("CartWindow_DeletePosition"); // neuen Key anlegen
        }

        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_warenkorb.Count == 0)
                return;

            _warenkorb.Clear();
        }

        private void Menu_DeletePosition_Click(object sender, RoutedEventArgs e)
        {
            if (dgCart.SelectedItem is not WarenkorbPosition pos)
                return;

            _warenkorb.Remove(pos);
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
