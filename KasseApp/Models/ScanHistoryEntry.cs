namespace KasseApp
{
    public class ScanHistoryEntry
    {
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Preis { get; set; }
        public int BestandNachScan { get; set; }
        public int MengeImWarenkorb { get; set; }
    }
}