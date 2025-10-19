using Microsoft.AspNetCore.Mvc;
using WaktuSolat.Services;

namespace WaktuSolat.Controllers;

public class HomeController : Controller
{
    private readonly WaktuSolatService _waktuSolatService;
    private readonly ZoneService _zoneService;
    private readonly IConfiguration _configuration;

    public HomeController(
        WaktuSolatService waktuSolatService,
        ZoneService zoneService,
        IConfiguration configuration)
    {
        _waktuSolatService = waktuSolatService;
        _zoneService = zoneService;
        _configuration = configuration;
    }

    /// Display waktu solat for selected zone
    public async Task<IActionResult> Index(string? zone)
    {
        try
        {
            var zoneCode = (zone ?? _configuration["ZoneCode"] ?? "WLY01").ToUpper();

            Console.WriteLine($"=== Displaying waktu solat for zone: {zoneCode} ===");

            // Get all available zones for dropdown (grouped by state)
            var allZones = await _zoneService.GetZonesGroupedAsync();
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
            return View();
        }
    }
}