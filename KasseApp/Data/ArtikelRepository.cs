using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KasseApp
{
    public class ArtikelRepository
    {
        private readonly string _connectionString;

        public ArtikelRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Artikel>> GetAllAsync()
        {
            var result = new List<Artikel>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(
                "SELECT barcode, name, preis, bestand, zusatzzahl, zusatztext FROM artikel ORDER BY name",
                conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new Artikel
                {
                    Barcode = reader.GetString(0),
                    Name = reader.GetString(1),
                    Preis = reader.GetDecimal(2),
                    Bestand = reader.GetInt32(3),
                    ZusatzZahl = reader.GetInt32(4),
                    ZusatzText = reader.GetString(5)
                });
            }

            return result;
        }

        public async Task<Artikel?> GetByBarcodeAsync(string barcode)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(
                "SELECT barcode, name, preis, bestand, zusatzzahl, zusatztext FROM artikel WHERE barcode = @barcode",
                conn);
            cmd.Parameters.AddWithValue("barcode", barcode);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Artikel
                {
                    Barcode = reader.GetString(0),
                    Name = reader.GetString(1),
                    Preis = reader.GetDecimal(2),
                    Bestand = reader.GetInt32(3),
                    ZusatzZahl = reader.GetInt32(4),
                    ZusatzText = reader.GetString(5)
                };
            }

            return null;
        }

        public async Task InsertAsync(Artikel artikel)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(
                "INSERT INTO artikel (barcode, name, preis, bestand, zusatzzahl, zusatztext) VALUES (@barcode, @name, @preis, @bestand, @zusatzzahl, @zusatztext)",
                conn);

            cmd.Parameters.AddWithValue("barcode", artikel.Barcode);
            cmd.Parameters.AddWithValue("name", artikel.Name);
            cmd.Parameters.AddWithValue("preis", artikel.Preis);
            cmd.Parameters.AddWithValue("bestand", artikel.Bestand);
            cmd.Parameters.AddWithValue("zusatzzahl", artikel.ZusatzZahl);
            cmd.Parameters.AddWithValue("zusatztext", artikel.ZusatzText);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Artikel artikel)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(
                "UPDATE artikel SET name = @name, preis = @preis, bestand = @bestand, zusatzzahl = @zusatzzahl, zusatztext = @zusatztext WHERE barcode = @barcode",
                conn);

            cmd.Parameters.AddWithValue("barcode", artikel.Barcode);
            cmd.Parameters.AddWithValue("name", artikel.Name);
            cmd.Parameters.AddWithValue("preis", artikel.Preis);
            cmd.Parameters.AddWithValue("bestand", artikel.Bestand);
            cmd.Parameters.AddWithValue("zusatzzahl", artikel.ZusatzZahl);
            cmd.Parameters.AddWithValue("zusatztext", artikel.ZusatzText);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string barcode)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(
                "DELETE FROM artikel WHERE barcode = @barcode",
                conn);
            cmd.Parameters.AddWithValue("barcode", barcode);

            await cmd.ExecuteNonQueryAsync();
        }
        
        public async Task UpdateBestandAsync(string barcode, int neuerBestand)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(
                "UPDATE artikel SET bestand = @bestand WHERE barcode = @barcode",
                conn);

            cmd.Parameters.AddWithValue("bestand", neuerBestand);
            cmd.Parameters.AddWithValue("barcode", barcode);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
