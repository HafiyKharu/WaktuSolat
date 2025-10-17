using Microsoft.EntityFrameworkCore;
using WaktuSolat.Models;

namespace WaktuSolat.Repository;

public class WaktuSolatRepository
{
    private readonly WaktuSolatDbContext _context;

    public WaktuSolatRepository(WaktuSolatDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SaveAsync(WaktuSolatEntity data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            // Ensure CreatedAt is UTC
            data.CreatedAt = DateTime.UtcNow;

            // Extract just the zone code (e.g., "WLY01") from full zone string
            var zoneCode = data.czone?.Split('-')[0].Trim() ?? data.czone;

            var existingRecord = await _context.WaktuSolat
                .FirstOrDefaultAsync(w => 
                    w.czone.Contains(zoneCode) && 
                    w.TarikhMasehi == data.TarikhMasehi);

            if (existingRecord != null)
            {
                existingRecord.cbearing = data.cbearing;
                existingRecord.TarikhHijrah = data.TarikhHijrah;
                existingRecord.Imsak = data.Imsak;
                existingRecord.Subuh = data.Subuh;
                existingRecord.Syuruk = data.Syuruk;
                existingRecord.Dhuha = data.Dhuha;
                existingRecord.Zohor = data.Zohor;
                existingRecord.Asar = data.Asar;
                existingRecord.Maghrib = data.Maghrib;
                existingRecord.Isyak = data.Isyak;
                existingRecord.CreatedAt = DateTime.UtcNow;

                _context.WaktuSolat.Update(existingRecord);
                Console.WriteLine($"✓ Updated existing record for zone {data.czone} on {data.TarikhMasehi}");
            }
            else
            {
                await _context.WaktuSolat.AddAsync(data);
                Console.WriteLine($"✓ Inserted new record for zone {data.czone} on {data.TarikhMasehi}");
            }

            var result = await _context.SaveChangesAsync();
            Console.WriteLine($"✓ Successfully saved {result} record(s) to database");
            return true;
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
            var today = DateTime.Now.ToString("dd/MM/yyyy");
            
            Console.WriteLine($"Searching for zone: {zoneCode}, date: {today}");

            // Search using Contains to handle both "WLY01" and "WLY01 - Kuala Lumpur, Putrajaya"
            var result = await _context.WaktuSolat
                .Where(w => w.czone.Contains(zoneCode) && w.TarikhMasehi == today)
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync();

            if (result != null)
            {
                Console.WriteLine($"✓ Found prayer time for zone {result.czone} on {result.TarikhMasehi}");
            }
            else
            {
                Console.WriteLine($"✗ No prayer time found for zone {zoneCode} on {today}");
                
                // Debug: Show what's in the database
                var allRecords = await _context.WaktuSolat
                    .OrderByDescending(w => w.CreatedAt)
                    .Take(5)
                    .Select(w => new { w.czone, w.TarikhMasehi, w.CreatedAt })
                    .ToListAsync();
                
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
            return await _context.WaktuSolat
                .Where(w => w.czone.Contains(zoneCode))
                .OrderByDescending(w => w.CreatedAt)
                .Take(days)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error retrieving data: {ex.Message}");
            throw;
        }
    }
}