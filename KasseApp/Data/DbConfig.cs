namespace KasseApp
{
    public class DbConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string SslMode { get; set; } = "Require";

        // Baut den Npgsql-Connection-String zusammen.
        public string ToConnectionString()
        {
            return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};SslMode={SslMode}";
        }
    }
}