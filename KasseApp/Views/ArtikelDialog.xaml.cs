using System;
using System.Globalization;
using System.Windows;

namespace KasseApp.Views
{
    public partial class ArtikelDialog : Window
    {
        public Artikel Artikel { get; private set; }

        // Konstruktor für "Neu"
        public ArtikelDialog()
        {
            InitializeComponent();
            Artikel = new Artikel();

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (_, _) => DialogResult = false;
        }

        // Konstruktor für "Bearbeiten"
        public ArtikelDialog(Artikel artikel) : this()
        {
            // Bestehende Werte in die Textboxen laden
            Artikel = artikel;

            txtBarcode.Text = artikel.Barcode;
            txtBarcode.IsEnabled = false; // Primärschlüssel nicht änderbar

            txtName.Text = artikel.Name;
            txtPreis.Text = artikel.Preis.ToString("0.00", CultureInfo.InvariantCulture);
            txtBestand.Text = artikel.Bestand.ToString();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Basis-Validierung
            if (string.IsNullOrWhiteSpace(txtBarcode.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtPreis.Text) ||
                string.IsNullOrWhiteSpace(txtBestand.Text))
            {
                MessageBox.Show("Bitte alle Felder ausfüllen.");
                return;
            }

            if (!decimal.TryParse(txtPreis.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var preis))
            {
                MessageBox.Show("Preis ist ungültig.");
                return;
            }

            if (!int.TryParse(txtBestand.Text, out var bestand))
            {
                MessageBox.Show("Bestand ist ungültig.");
                return;
            }

            Artikel.Barcode = txtBarcode.Text.Trim();
            Artikel.Name = txtName.Text.Trim();
            Artikel.Preis = preis;
            Artikel.Bestand = bestand;

            DialogResult = true;
        }
    }
}
