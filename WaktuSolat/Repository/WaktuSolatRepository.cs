using Dapper;
using Npgsql;
using WaktuSolat.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

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

    /// <summary>
    /// Get today's prayer time using PostgreSQL function
    /// </summary>
    public async Task<WaktuSolatEntity?> GetTodayPrayerTimeAsync(string zoneCode)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            Console.WriteLine($"Getting prayer time for zone: {zoneCode} using stored function");

            // Call the PostgreSQL function that returns JSON
            var sql = "SELECT getwaktusolat(@ZoneCode, CURRENT_DATE)::text";

            var jsonResult = await connection.QuerySingleOrDefaultAsync<string>(
                sql,
                new { ZoneCode = zoneCode.ToUpper() }
            );

            if (string.IsNullOrWhiteSpace(jsonResult))
            {
                Console.WriteLine($"✗ No result from function for zone {zoneCode}");
                return null;
            }

            // Parse the JSON response
            var jsonDoc = JsonDocument.Parse(jsonResult);
            var root = jsonDoc.RootElement;

            // Check if success
            if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean() == false)
            {
                Console.WriteLine($"✗ No prayer time found for zone {zoneCode}");
                return null;
            }

            // Get the data array
            if (!root.TryGetProperty("data", out var dataProp) || dataProp.ValueKind == JsonValueKind.Null)
            {
                Console.WriteLine($"✗ No data in result for zone {zoneCode}");
                return null;
            }

            // Get first item from array
            var firstItem = dataProp.EnumerateArray().FirstOrDefault();
            if (firstItem.ValueKind == JsonValueKind.Undefined)
            {
                Console.WriteLine($"✗ Empty data array for zone {zoneCode}");
                return null;
            }

            // Map JSON to entity
            var entity = new WaktuSolatEntity
            {
                Id = firstItem.GetProperty("id").GetInt32(),
                czone = firstItem.GetProperty("czone").GetString() ?? string.Empty,
                cbearing = firstItem.GetProperty("cbearing").GetString() ?? string.Empty,
                TarikhMasehi = firstItem.GetProperty("tarikhMasehi").GetString() ?? string.Empty,
                TarikhHijrah = firstItem.GetProperty("tarikhHijrah").GetString() ?? string.Empty,
                Imsak = firstItem.GetProperty("imsak").GetString() ?? string.Empty,
                Subuh = firstItem.GetProperty("subuh").GetString() ?? string.Empty,
                Syuruk = firstItem.GetProperty("syuruk").GetString() ?? string.Empty,
                Dhuha = firstItem.GetProperty("dhuha").GetString() ?? string.Empty,
                Zohor = firstItem.GetProperty("zohor").GetString() ?? string.Empty,
                Asar = firstItem.GetProperty("asar").GetString() ?? string.Empty,
                Maghrib = firstItem.GetProperty("maghrib").GetString() ?? string.Empty,
                Isyak = firstItem.GetProperty("isyak").GetString() ?? string.Empty,
                CreatedAt = firstItem.GetProperty("createdAt").GetDateTime()
            };

            Console.WriteLine($"✓ Found prayer time for zone {entity.czone} on {entity.TarikhMasehi}");
            return entity;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error retrieving prayer time: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get prayer time for specific date using PostgreSQL function
    /// </summary>
    public async Task<WaktuSolatEntity?> GetPrayerTimeByDateAsync(string zoneCode, DateTime date)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            Console.WriteLine($"Getting prayer time for zone: {zoneCode}, date: {date:dd/MM/yyyy}");

            var sql = "SELECT getwaktusolat(@ZoneCode, @Date)::text";

            var jsonResult = await connection.QuerySingleOrDefaultAsync<string>(
                sql,
                new { ZoneCode = zoneCode.ToUpper(), Date = date }
            );

            if (string.IsNullOrWhiteSpace(jsonResult))
            {
                Console.WriteLine($"✗ No result from function");
                return null;
            }

            var jsonDoc = JsonDocument.Parse(jsonResult);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean() == false)
            {
                Console.WriteLine($"✗ No prayer time found");
                return null;
            }

            if (!root.TryGetProperty("data", out var dataProp) || dataProp.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            var firstItem = dataProp.EnumerateArray().FirstOrDefault();
            if (firstItem.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            var entity = new WaktuSolatEntity
            {
                Id = firstItem.GetProperty("id").GetInt32(),
                czone = firstItem.GetProperty("czone").GetString() ?? string.Empty,
                cbearing = firstItem.GetProperty("cbearing").GetString() ?? string.Empty,
                TarikhMasehi = firstItem.GetProperty("tarikhMasehi").GetString() ?? string.Empty,
                TarikhHijrah = firstItem.GetProperty("tarikhHijrah").GetString() ?? string.Empty,
                Imsak = firstItem.GetProperty("imsak").GetString() ?? string.Empty,
                Subuh = firstItem.GetProperty("subuh").GetString() ?? string.Empty,
                Syuruk = firstItem.GetProperty("syuruk").GetString() ?? string.Empty,
                Dhuha = firstItem.GetProperty("dhuha").GetString() ?? string.Empty,
                Zohor = firstItem.GetProperty("zohor").GetString() ?? string.Empty,
                Asar = firstItem.GetProperty("asar").GetString() ?? string.Empty,
                Maghrib = firstItem.GetProperty("maghrib").GetString() ?? string.Empty,
                Isyak = firstItem.GetProperty("isyak").GetString() ?? string.Empty,
                CreatedAt = firstItem.GetProperty("createdAt").GetDateTime()
            };

            Console.WriteLine($"✓ Found prayer time for zone {entity.czone} on {entity.TarikhMasehi}");
            return entity;
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

    /// <summary>
    /// Get waktu solat as raw JSON string using stored function
    /// </summary>
    public async Task<string> GetTodayPrayerTimeAsJsonAsync(string zoneCode)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT getwaktusolat(@ZoneCode, CURRENT_DATE)::text";

            var jsonResult = await connection.QuerySingleOrDefaultAsync<string>(
                sql,
                new { ZoneCode = zoneCode.ToUpper() }
            );

            return jsonResult ?? "{}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error retrieving JSON: {ex.Message}");
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = ex.Message,
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Get waktu solat as raw JSON string for specific date
    /// </summary>
    public async Task<string> GetPrayerTimeAsJsonAsync(string zoneCode, DateTime date)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT getwaktusolat(@ZoneCode, @Date)::text";

            var jsonResult = await connection.QuerySingleOrDefaultAsync<string>(
                sql,
                new { ZoneCode = zoneCode.ToUpper(), Date = date }
            );

            return jsonResult ?? "{}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error retrieving JSON: {ex.Message}");
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = ex.Message,
                data = (object?)null
            });
        }
    }
}