using Microsoft.AspNetCore.Mvc;
using WaktuSolat.Repository;
using WaktuSolat.Services;

namespace WaktuSolat.Controllers;

public class HomeController : Controller
{
    private readonly WaktuSolatRepository _repository;
    private readonly ScrapWaktuSolatService _scrapService;
    private readonly IConfiguration _configuration;

    public HomeController(
        WaktuSolatRepository repository,
        ScrapWaktuSolatService scrapService,
        IConfiguration configuration)
    {
        _repository = repository;
        _scrapService = scrapService;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(string? zone)
    {
        try
        {
            var zoneCode = zone ?? _configuration["ZoneCode"] ?? "WLY01";

            Console.WriteLine($"=== HomeController.Index called with zone: {zoneCode} ===");

            // Get all available zones for dropdown
            var allZones = await _scrapService.GetAllZonesAsync();
            ViewBag.AllZones = allZones;
            ViewBag.SelectedZone = zoneCode;

            // Step 1: Check if data exists for today
            var prayerTimes = await _repository.GetTodayPrayerTimeAsync(zoneCode);

            // Step 2: If no data, scrape and save
            if (prayerTimes == null)
            {
                Console.WriteLine($"No data found for zone {zoneCode}. Starting scraping...");

                var scrapedData = await _scrapService.GetWaktuSolatAsync(zoneCode);

                if (scrapedData == null)
                {
                    ViewBag.Error = "Failed to retrieve prayer times from source.";
                    return View();
                }

                Console.WriteLine($"Scraped data: Zone={scrapedData.czone}, Date={scrapedData.TarikhMasehi}");

                // Step 3: Save to database
                var saved = await _repository.SaveAsync(scrapedData);

                if (!saved)
                {
                    ViewBag.Error = "Failed to save prayer times to database.";
                    return View();
                }

                Console.WriteLine($"✓ Scraped and saved data for zone {zoneCode}");

                // Step 4: Retrieve the saved data
                prayerTimes = await _repository.GetTodayPrayerTimeAsync(zoneCode);

                if (prayerTimes == null)
                {
                    ViewBag.Error = "Data was saved but could not be retrieved. Please refresh the page.";
                    return View();
                }
            }
            else
            {
                Console.WriteLine($"✓ Using cached data for zone {zoneCode}");
            }

            ViewBag.ZoneCode = zoneCode;
            ViewBag.IsScraped = true;

            return View(prayerTimes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error in HomeController: {ex.Message}");
            Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            ViewBag.Error = $"Error: {ex.Message}";
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> RefreshData(string? zone)
    {
        try
        {
            var zoneCode = zone ?? _configuration["ZoneCode"] ?? "WLY01";

            Console.WriteLine($"Manual refresh requested for zone {zoneCode}");

            // Force scrape new data
            var scrapedData = await _scrapService.GetWaktuSolatAsync(zoneCode);

            if (scrapedData == null)
            {
                TempData["Error"] = "Failed to retrieve prayer times from source.";
                return RedirectToAction("Index", new { zone = zoneCode });
            }

            // Save to database (will update if exists)
            await _repository.SaveAsync(scrapedData);

            TempData["Success"] = "Prayer times refreshed successfully!";

            return RedirectToAction("Index", new { zone = zoneCode });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error refreshing data: {ex.Message}");
            TempData["Error"] = $"Error: {ex.Message}";
            return RedirectToAction("Index", new { zone = zone });
        }
    }
}