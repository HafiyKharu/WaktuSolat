using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WaktuSolat.Models;

namespace WaktuSolat.Repository;

public class WaktuSolatRepository
{
    private readonly WaktuSolatDbContext _context;
    private readonly string _zoneCode;

    public WaktuSolatRepository(WaktuSolatDbContext context, IConfiguration config)
    {
        _context = context;
        _zoneCode = config["ZoneCode"] ?? throw new InvalidOperationException("ZoneCode is not configured");
    }

    public async Task<bool> SaveAsync(WaktuSolatEntity data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            // Check if record already exists for today and zone
            var existingRecord = await _context.WaktuSolat
                .FirstOrDefaultAsync(w => 
                    w.czone == data.czone && 
                    w.TarikhMasehi == data.TarikhMasehi);

            if (existingRecord != null)
            {
                // Update existing record
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

                _context.WaktuSolat.Update(existingRecord);
                Console.WriteLine($"Updated existing record for zone {data.czone} on {data.TarikhMasehi}");
            }
            else
            {
                // Insert new record
                await _context.WaktuSolat.AddAsync(data);
                Console.WriteLine($"Inserted new record for zone {data.czone} on {data.TarikhMasehi}");
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"✓ Successfully saved waktu solat data to database");
            return true;
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine($"✗ Database update error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error saving to database: {ex.Message}");
            throw;
        }
    }

    public async Task<List<WaktuSolatEntity>> GetByZoneAsync(string zoneCode, int days = 7)
    {
        try
        {
            var startDate = DateTime.Now.AddDays(-days).ToString("dd/MM/yyyy");
            
            return await _context.WaktuSolat
                .Where(w => w.czone == zoneCode)
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