using Dapper;
using Npgsql;
using WaktuSolat.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace WaktuSolat.Repository;

public class ZoneRepository
{
    private readonly string _connectionString;

    public ZoneRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    /// <summary>
    /// Insert or update a single zone
    /// </summary>
    public async Task<bool> InsertZoneAsync(ZoneInput zone)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "CALL insert_zone(@ZoneCode, @State, @Description)";

            await connection.ExecuteAsync(sql, new
            {
                ZoneCode = zone.ZoneCode.ToUpper(),
                State = zone.State,
                Description = zone.Description
            });

            Console.WriteLine($"✓ Zone {zone.ZoneCode} inserted/updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error inserting zone: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Bulk insert or update zones
    /// </summary>
    public async Task<bool> BulkInsertZonesAsync(List<ZoneInput> zones)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Convert to JSON format expected by stored procedure
            var zonesJson = JsonSerializer.Serialize(zones.Select(z => new
            {
                zoneCode = z.ZoneCode,
                state = z.State,
                description = z.Description
            }));

            var sql = "CALL bulk_insert_zones(@Zones::json)";

            await connection.ExecuteAsync(sql, new { Zones = zonesJson });

            Console.WriteLine($"✓ Bulk inserted/updated {zones.Count} zones successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error bulk inserting zones: {ex.Message}");
            Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Get all zones from database
    /// </summary>
    public async Task<List<Zone>> GetAllZonesAsync()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    id AS Id,
                    zone_code AS ZoneCode,
                    state AS State,
                    description AS Description,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM zones
                ORDER BY state, zone_code";

            var result = await connection.QueryAsync<Zone>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting all zones: {ex.Message}");
            return new List<Zone>();
        }
    }

    /// Get zones grouped by state
    public async Task<List<ZoneGroup>> GetZonesGroupedAsync()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var zones = await GetAllZonesAsync();

            var grouped = zones
                .GroupBy(z => z.State)
                .Select(g => new ZoneGroup
                {
                    State = g.Key,
                    Zones = g.Select(z => new ZoneOption
                    {
                        Value = z.ZoneCode,
                        Text = z.Description
                    }).ToList()
                })
                .OrderBy(g => g.State)
                .ToList();

            return grouped;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting grouped zones: {ex.Message}");
            return new List<ZoneGroup>();
        }
    }
}