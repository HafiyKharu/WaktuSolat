using Microsoft.AspNetCore.Mvc;
using WaktuSolat.Services;
using WaktuSolat.Models;

namespace WaktuSolat.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ZoneController : ControllerBase
{
    private readonly ZoneService _zoneService;

    public ZoneController(ZoneService zoneService)
    {
        _zoneService = zoneService;
    }

    /// Get all zones grouped by state (from database, auto-scrape)
    /// GET: api/zone
    [HttpGet]
    public async Task<IActionResult> GetAllZones()
    {
        try
        {
            var zones = await _zoneService.GetZonesGroupedAsync();

            return Ok(new
            {
                success = true,
                totalStates = zones.Count,
                totalZones = zones.Sum(z => z.Zones.Count),
                data = zones
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// Get all zones grouped by state (auto-scrape)
    /// GET: api/zone/scrape
    [HttpGet("/scrape")]
    public async Task<IActionResult> ScrapAllZones()
    {
        try
        {
            var zones = await _zoneService.ScrapAndSaveZonesAsync();

            return Ok(new
            {
                success = true,
                data = zones
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}