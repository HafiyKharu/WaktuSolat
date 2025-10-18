using Dapper;
using Npgsql;
using WaktuSolat.Models;

namespace WaktuSolat.Repository;

public class WaktuSolatRepository
{
    private readonly string _connectionString;

    public WaktuSolatRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<bool> SaveAsync(WaktuSolatEntity data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Ensure CreatedAt is UTC
            data.CreatedAt = DateTime.UtcNow;

            // Extract just the zone code (e.g., "WLY01") from full zone string
            var zoneCode = data.czone?.Split('-')[0].Trim().ToUpper() ?? data.czone?.ToUpper();

            // Check if record exists
            var checkSql = @"
                SELECT id 
                FROM waktu_solat 
                WHERE UPPER(czone) LIKE '%' || @ZoneCode || '%' 
                    AND tarikh_masehi = @TarikhMasehi 
                LIMIT 1";

            var existingId = await connection.QuerySingleOrDefaultAsync<int?>(
                checkSql,
                new { ZoneCode = zoneCode, data.TarikhMasehi }
            );

            if (existingId.HasValue)
            {
                // Update existing record
                var updateSql = @"
                    UPDATE waktu_solat 
                    SET 
                        cbearing = @cbearing,
                        tarikh_hijrah = @TarikhHijrah,
                        imsak = @Imsak,
                        subuh = @Subuh,
                        syuruk = @Syuruk,
                        dhuha = @Dhuha,
                        zohor = @Zohor,
                        asar = @Asar,
                        maghrib = @Maghrib,
                        isyak = @Isyak,
                        created_at = @CreatedAt
                    WHERE id = @Id";

                var rowsAffected = await connection.ExecuteAsync(updateSql, new
                {
                    data.cbearing,
                    data.TarikhHijrah,
                    data.Imsak,
                    data.Subuh,
                    data.Syuruk,
                    data.Dhuha,
                    data.Zohor,
                    data.Asar,
                    data.Maghrib,
                    data.Isyak,
                    data.CreatedAt,
                    Id = existingId.Value
                });

                Console.WriteLine($"✓ Updated existing record for zone {data.czone} on {data.TarikhMasehi}");
                return rowsAffected > 0;
            }
            else
            {
                // Insert new record
                var insertSql = @"
                    INSERT INTO waktu_solat (
                        czone, cbearing, tarikh_masehi, tarikh_hijrah,
                        imsak, subuh, syuruk, dhuha, zohor, asar, maghrib, isyak,
                        created_at
                    )
                    VALUES (
                        @czone, @cbearing, @TarikhMasehi, @TarikhHijrah,
                        @Imsak, @Subuh, @Syuruk, @Dhuha, @Zohor, @Asar, @Maghrib, @Isyak,
                        @CreatedAt
                    )";

                var rowsAffected = await connection.ExecuteAsync(insertSql, data);

                Console.WriteLine($"✓ Inserted new record for zone {data.czone} on {data.TarikhMasehi}");
                return rowsAffected > 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error saving to database: {ex.Message}");
            Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<WaktuSolatEntity?> GetTodayPrayerTimeAsync(string zoneCode)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var today = DateTime.Now.ToString("dd/MM/yyyy");
            
            Console.WriteLine($"Searching for zone: {zoneCode}, date: {today}");

            var sql = @"
                SELECT 
                    id AS Id,
                    czone,
                    cbearing,
                    tarikh_masehi AS TarikhMasehi,
                    tarikh_hijrah AS TarikhHijrah,
                    imsak AS Imsak,
                    subuh AS Subuh,
                    syuruk AS Syuruk,
                    dhuha AS Dhuha,
                    zohor AS Zohor,
                    asar AS Asar,
                    maghrib AS Maghrib,
                    isyak AS Isyak,
                    created_at AS CreatedAt
                FROM waktu_solat
                WHERE UPPER(czone) LIKE '%' || UPPER(@ZoneCode) || '%'
                    AND tarikh_masehi = @Today
                ORDER BY created_at DESC
                LIMIT 1";

            var result = await connection.QuerySingleOrDefaultAsync<WaktuSolatEntity>(
                sql,
                new { ZoneCode = zoneCode.ToUpper(), Today = today }
            );

            if (result != null)
            {
                Console.WriteLine($"✓ Found prayer time for zone {result.czone} on {result.TarikhMasehi}");
            }
            else
            {
                Console.WriteLine($"✗ No prayer time found for zone {zoneCode} on {today}");
                
                // Debug: Show what's in the database
                var debugSql = @"
                    SELECT czone, tarikh_masehi AS TarikhMasehi, created_at AS CreatedAt
                    FROM waktu_solat
                    ORDER BY created_at DESC
                    LIMIT 5";

                var allRecords = await connection.QueryAsync(debugSql);
                
                Console.WriteLine($"Recent records in database:");
                foreach (var record in allRecords)
                {
                    Console.WriteLine($"  - Zone: {record.czone}, Date: {record.TarikhMasehi}, Created: {record.CreatedAt}");
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error retrieving prayer time: {ex.Message}");
            throw;
        }
    }

    public async Task<List<WaktuSolatEntity>> GetByZoneAsync(string zoneCode, int days = 7)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    id AS Id,
                    czone,
                    cbearing,
                    tarikh_masehi AS TarikhMasehi,
                    tarikh_hijrah AS TarikhHijrah,
                    imsak AS Imsak,
                    subuh AS Subuh,
                    syuruk AS Syuruk,
                    dhuha AS Dhuha,
                    zohor AS Zohor,
                    asar AS Asar,
                    maghrib AS Maghrib,
                    isyak AS Isyak,
                    created_at AS CreatedAt
                FROM waktu_solat
                WHERE UPPER(czone) LIKE '%' || UPPER(@ZoneCode) || '%'
                ORDER BY created_at DESC
                LIMIT @Days";

            var result = await connection.QueryAsync<WaktuSolatEntity>(
                sql,
                new { ZoneCode = zoneCode.ToUpper(), Days = days }
            );

            return result.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error retrieving data: {ex.Message}");
            throw;
        }
    }

    // Optional: Using stored function
    public async Task<WaktuSolatEntity?> GetTodayPrayerTimeUsingFunctionAsync(string zoneCode)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            Console.WriteLine($"Calling stored function with zone: {zoneCode}");

            var sql = "SELECT * FROM get_waktu_solat(@ZoneCode)";

            var result = await connection.QuerySingleOrDefaultAsync<WaktuSolatEntity>(
                sql,
                new { ZoneCode = zoneCode.ToUpper() }
            );

            if (result != null)
            {
                Console.WriteLine($"✓ Found prayer time for zone {result.czone} on {result.TarikhMasehi}");
            }
            else
            {
                Console.WriteLine($"✗ No prayer time found for zone {zoneCode}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error retrieving prayer time: {ex.Message}");
            throw;
        }
    }
}