namespace KasseApp
{
    public class WarenkorbPosition
    {
        public Artikel Artikel { get; set; } = null!;
        public int Menge { get; set; }
        public decimal Gesamtpreis => Artikel.Preis * Menge;
    }
}