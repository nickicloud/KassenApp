namespace KasseApp
{
    public class Artikel
    {
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Preis { get; set; }
        public int Bestand { get; set; }
        public int ZusatzZahl { get; set; }
        public string ZusatzText { get; set; } = "";
    }
}