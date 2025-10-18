using Microsoft.AspNetCore.Mvc;
using WaktuSolat.Services;

namespace WaktuSolat.Controllers;

public class HomeController : Controller
{
    private readonly WaktuSolatService _waktuSolatService;
    private readonly IConfiguration _configuration;

    public HomeController(
        WaktuSolatService waktuSolatService,
        IConfiguration configuration)
    {
        _waktuSolatService = waktuSolatService;
        _configuration = configuration;
    }

    /// <summary>
    /// Display waktu solat for selected zone
    /// </summary>
    public async Task<IActionResult> Index(string? zone)
    {
        try
        {
            var zoneCode = (zone ?? _configuration["ZoneCode"] ?? "WLY01").ToUpper();

            Console.WriteLine($"=== Displaying waktu solat for zone: {zoneCode} ===");

            // Get all available zones for dropdown
            var allZones = await _waktuSolatService.GetAllZonesAsync();
            ViewBag.AllZones = allZones;
            ViewBag.SelectedZone = zoneCode;

            // Get waktu solat (will auto-scrape if not exists)
            var prayerTimes = await _waktuSolatService.GetTodayWaktuSolatAsync(zoneCode);

            if (prayerTimes == null)
            {
                ViewBag.Error = "Failed to retrieve prayer times. Please try again.";
                return View();
            }

            ViewBag.ZoneCode = zoneCode;

            return View(prayerTimes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error in HomeController: {ex.Message}");
            ViewBag.Error = $"Error: {ex.Message}";
            
            // Still try to load zones for dropdown
            try
            {
                var allZones = await _waktuSolatService.GetAllZonesAsync();
                ViewBag.AllZones = allZones;
                ViewBag.SelectedZone = zone ?? "WLY01";
            }
            catch { }

            return View();
        }
    }

    /// <summary>
    /// Get waktu solat history for a zone
    /// </summary>
    public async Task<IActionResult> History(string? zone, int days = 7)
    {
        try
        {
            var zoneCode = (zone ?? _configuration["ZoneCode"] ?? "WLY01").ToUpper();

            Console.WriteLine($"=== Getting history for zone: {zoneCode} ===");

            var history = await _waktuSolatService.GetWaktuSolatHistoryAsync(zoneCode, days);

            ViewBag.ZoneCode = zoneCode;
            ViewBag.Days = days;

            return View(history);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting history: {ex.Message}");
            ViewBag.Error = $"Error: {ex.Message}";
            return View();
        }
    }
}