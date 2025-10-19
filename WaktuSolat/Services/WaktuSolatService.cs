using WaktuSolat.Models;
using WaktuSolat.Repository;

namespace WaktuSolat.Services;

public class WaktuSolatService
{
    private readonly WaktuSolatRepository _repository;
    private readonly ScrapWaktuSolatService _scrapService;

    public WaktuSolatService(
        WaktuSolatRepository repository,
        ScrapWaktuSolatService scrapService)
    {
        _repository = repository;
        _scrapService = scrapService;
    }

    /// <summary>
    /// Get waktu solat for today. If not in database, scrape and save it.
    /// </summary>
    public async Task<WaktuSolatEntity?> GetTodayWaktuSolatAsync(string zoneCode)
    {
        try
        {
            Console.WriteLine($"=== Getting waktu solat for zone: {zoneCode} ===");

            // Step 1: Try to get from database
            var existingData = await _repository.GetTodayPrayerTimeAsync(zoneCode);

            if (existingData != null)
            {
                Console.WriteLine($"✓ Found cached data for zone {zoneCode}");
                return existingData;
            }

            // Step 2: Not in database, scrape it
            Console.WriteLine($"No cached data. Scraping for zone {zoneCode}...");
            var scrapedData = await _scrapService.ScrapeWaktuSolatAsync(zoneCode);

            if (scrapedData == null)
            {
                Console.WriteLine($"✗ Failed to scrape data for zone {zoneCode}");
                return null;
            }

            // Step 3: Save to database
            var saved = await _repository.SaveAsync(scrapedData);

            if (!saved)
            {
                Console.WriteLine($"✗ Failed to save data for zone {zoneCode}");
                return scrapedData; // Return scraped data even if save failed
            }

            Console.WriteLine($"✓ Successfully scraped and saved data for zone {zoneCode}");

            // Step 4: Return the fresh data
            return scrapedData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting waktu solat for zone {zoneCode}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Force refresh waktu solat by scraping new data
    /// </summary>
    public async Task<WaktuSolatEntity?> RefreshWaktuSolatAsync(string zoneCode)
    {
        try
        {
            Console.WriteLine($"=== Force refreshing waktu solat for zone: {zoneCode} ===");

            // Step 1: Scrape fresh data
            var scrapedData = await _scrapService.ScrapeWaktuSolatAsync(zoneCode);

            if (scrapedData == null)
            {
                Console.WriteLine($"✗ Failed to scrape data for zone {zoneCode}");
                return null;
            }

            // Step 2: Save/Update to database
            var saved = await _repository.SaveAsync(scrapedData);

            if (!saved)
            {
                Console.WriteLine($"✗ Failed to save refreshed data for zone {zoneCode}");
                return scrapedData; // Return scraped data even if save failed
            }

            Console.WriteLine($"✓ Successfully refreshed and saved data for zone {zoneCode}");
            return scrapedData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error refreshing waktu solat for zone {zoneCode}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get waktu solat history for a zone
    /// </summary>
    public async Task<List<WaktuSolatEntity>> GetWaktuSolatHistoryAsync(string zoneCode, int days = 7)
    {
        try
        {
            Console.WriteLine($"Getting {days} days history for zone {zoneCode}");
            return await _repository.GetByZoneAsync(zoneCode, days);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting history for zone {zoneCode}: {ex.Message}");
            throw;
        }
    }
}