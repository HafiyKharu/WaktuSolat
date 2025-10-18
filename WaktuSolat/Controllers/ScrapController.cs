using Microsoft.AspNetCore.Mvc;
using WaktuSolat.Services;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace WaktuSolat.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScrapController : ControllerBase
{
    private readonly WaktuSolatService _waktuSolatService;
    private readonly IConfiguration _configuration;

    public ScrapController(
        WaktuSolatService waktuSolatService,
        IConfiguration configuration)
    {
        _waktuSolatService = waktuSolatService;
        _configuration = configuration;
    }

    /// <summary>
    /// Scrape and save waktu solat for a specific zone
    /// GET: api/scrap/zone/WLY01
    /// </summary>
    [HttpGet("zone/{zoneCode}")]
    public async Task<IActionResult> ScrapeByZone(string zoneCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(zoneCode))
            {
                return BadRequest(new { error = "Zone code is required" });
            }

            Console.WriteLine($"=== Scraping zone: {zoneCode} ===");

            var result = await _waktuSolatService.RefreshWaktuSolatAsync(zoneCode.ToUpper());

            if (result == null)
            {
                return NotFound(new { error = $"Failed to scrape data for zone {zoneCode}" });
            }

            return Ok(new
            {
                success = true,
                message = $"Successfully scraped and saved data for zone {zoneCode}",
                data = result
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error scraping zone {zoneCode}: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Scrape and save waktu solat for all zones using PARALLEL processing with retry
    /// GET: api/scrap/all?parallel=true&maxDegree=3&retryFailed=true
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> ScrapeAllZones(
        [FromQuery] bool parallel = true, 
        [FromQuery] int maxDegree = 3,  // Reduced from 5 to 3 for stability
        [FromQuery] bool retryFailed = true)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"=== Scraping all zones (Parallel: {parallel}, Max Degree: {maxDegree}) ===");

            var allZones = await _waktuSolatService.GetAllZonesAsync();
            var results = new ConcurrentBag<object>();
            var errors = new ConcurrentBag<object>();

            var allZoneCodes = allZones
                .SelectMany(g => g.Zones.Select(z => new { Zone = z, State = g.State }))
                .ToList();

            if (parallel)
            {
                // PARALLEL PROCESSING with reduced concurrency for stability
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxDegree
                };

                await Parallel.ForEachAsync(allZoneCodes, parallelOptions, async (item, ct) =>
                {
                    try
                    {
                        var result = await _waktuSolatService.RefreshWaktuSolatAsync(item.Zone.Value);

                        if (result != null)
                        {
                            results.Add(new
                            {
                                zone = item.Zone.Value,
                                state = item.State,
                                success = true
                            });
                            Console.WriteLine($"✓ [{results.Count + errors.Count}/{allZoneCodes.Count}] Scraped {item.Zone.Value}");
                        }
                        else
                        {
                            errors.Add(new
                            {
                                zone = item.Zone.Value,
                                state = item.State,
                                error = "Failed to scrape"
                            });
                            Console.WriteLine($"✗ [{results.Count + errors.Count}/{allZoneCodes.Count}] Failed {item.Zone.Value}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new
                        {
                            zone = item.Zone.Value,
                            state = item.State,
                            error = ex.Message
                        });
                        Console.WriteLine($"✗ [{results.Count + errors.Count}/{allZoneCodes.Count}] Error {item.Zone.Value}: {ex.Message}");
                    }
                });
            }
            else
            {
                // SEQUENTIAL PROCESSING
                int processed = 0;
                foreach (var item in allZoneCodes)
                {
                    try
                    {
                        processed++;
                        var result = await _waktuSolatService.RefreshWaktuSolatAsync(item.Zone.Value);

                        if (result != null)
                        {
                            results.Add(new
                            {
                                zone = item.Zone.Value,
                                state = item.State,
                                success = true
                            });
                            Console.WriteLine($"✓ [{processed}/{allZoneCodes.Count}] Scraped {item.Zone.Value}");
                        }
                        else
                        {
                            errors.Add(new
                            {
                                zone = item.Zone.Value,
                                state = item.State,
                                error = "Failed to scrape"
                            });
                            Console.WriteLine($"✗ [{processed}/{allZoneCodes.Count}] Failed {item.Zone.Value}");
                        }

                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new
                        {
                            zone = item.Zone.Value,
                            state = item.State,
                            error = ex.Message
                        });
                        Console.WriteLine($"✗ [{processed}/{allZoneCodes.Count}] Error {item.Zone.Value}: {ex.Message}");
                    }
                }
            }

            stopwatch.Stop();

            // RETRY FAILED ZONES if requested
            List<object> retriedResults = new List<object>();
            if (retryFailed && errors.Any())
            {
                Console.WriteLine($"\n=== Retrying {errors.Count} failed zones ===");
                
                var failedZones = errors.Select(e => ((dynamic)e).zone.ToString()).ToList();
                var retriedErrors = new ConcurrentBag<object>();

                foreach (var zoneCode in failedZones)
                {
                    try
                    {
                        Console.WriteLine($"Retrying {zoneCode}...");
                        await Task.Delay(2000); // Wait before retry
                        
                        var result = await _waktuSolatService.RefreshWaktuSolatAsync(zoneCode);

                        if (result != null)
                        {
                            retriedResults.Add(new
                            {
                                zone = zoneCode,
                                success = true,
                                retry = true
                            });
                            results.Add(new { zone = zoneCode, success = true });
                            Console.WriteLine($"✓ Retry successful: {zoneCode}");
                        }
                        else
                        {
                            retriedErrors.Add(errors.First(e => ((dynamic)e).zone == zoneCode));
                            Console.WriteLine($"✗ Retry failed: {zoneCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        retriedErrors.Add(errors.First(e => ((dynamic)e).zone == zoneCode));
                        Console.WriteLine($"✗ Retry error {zoneCode}: {ex.Message}");
                    }
                }

                // Update errors with only the ones that failed retry
                errors = retriedErrors;
            }

            return Ok(new
            {
                success = true,
                message = $"Completed in {stopwatch.Elapsed.TotalMinutes:F2} minutes. Success: {results.Count}, Errors: {errors.Count}",
                parallel = parallel,
                maxDegreeOfParallelism = maxDegree,
                totalZones = allZoneCodes.Count,
                successCount = results.Count,
                errorCount = errors.Count,
                retried = retriedResults.Count,
                durationMinutes = stopwatch.Elapsed.TotalMinutes,
                results = results.OrderBy(r => ((dynamic)r).zone),
                errors = errors.OrderBy(e => ((dynamic)e).zone),
                retriedResults = retriedResults
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error scraping all zones: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Scrape and save waktu solat for zones in a specific state
    /// GET: api/scrap/state/Wilayah%20Persekutuan?parallel=true&maxDegree=3
    /// </summary>
    [HttpGet("state/{stateName}")]
    public async Task<IActionResult> ScrapeByState(string stateName, [FromQuery] bool parallel = true, [FromQuery] int maxDegree = 3)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(stateName))
            {
                return BadRequest(new { error = "State name is required" });
            }

            Console.WriteLine($"=== Scraping zones in state: {stateName} (Parallel: {parallel}) ===");

            var allZones = await _waktuSolatService.GetAllZonesAsync();
            var stateGroup = allZones.FirstOrDefault(g =>
                g.State.Equals(stateName, StringComparison.OrdinalIgnoreCase));

            if (stateGroup == null)
            {
                return NotFound(new { error = $"State '{stateName}' not found" });
            }

            var results = new ConcurrentBag<object>();
            var errors = new ConcurrentBag<object>();

            if (parallel)
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxDegree
                };

                await Parallel.ForEachAsync(stateGroup.Zones, parallelOptions, async (zone, ct) =>
                {
                    try
                    {
                        var result = await _waktuSolatService.RefreshWaktuSolatAsync(zone.Value);

                        if (result != null)
                        {
                            results.Add(new
                            {
                                zone = zone.Value,
                                description = zone.Text,
                                success = true
                            });
                            Console.WriteLine($"✓ Scraped {zone.Value}");
                        }
                        else
                        {
                            errors.Add(new
                            {
                                zone = zone.Value,
                                description = zone.Text,
                                error = "Failed to scrape"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new
                        {
                            zone = zone.Value,
                            description = zone.Text,
                            error = ex.Message
                        });
                        Console.WriteLine($"✗ Error scraping {zone.Value}: {ex.Message}");
                    }
                });
            }
            else
            {
                foreach (var zone in stateGroup.Zones)
                {
                    try
                    {
                        var result = await _waktuSolatService.RefreshWaktuSolatAsync(zone.Value);

                        if (result != null)
                        {
                            results.Add(new
                            {
                                zone = zone.Value,
                                description = zone.Text,
                                success = true
                            });
                            Console.WriteLine($"✓ Scraped {zone.Value}");
                        }
                        else
                        {
                            errors.Add(new
                            {
                                zone = zone.Value,
                                description = zone.Text,
                                error = "Failed to scrape"
                            });
                        }

                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new
                        {
                            zone = zone.Value,
                            description = zone.Text,
                            error = ex.Message
                        });
                        Console.WriteLine($"✗ Error scraping {zone.Value}: {ex.Message}");
                    }
                }
            }

            return Ok(new
            {
                success = true,
                message = $"Completed scraping state '{stateName}'. Success: {results.Count}, Errors: {errors.Count}",
                state = stateName,
                parallel = parallel,
                maxDegreeOfParallelism = maxDegree,
                totalZones = results.Count + errors.Count,
                successCount = results.Count,
                errorCount = errors.Count,
                results = results.OrderBy(r => ((dynamic)r).zone),
                errors = errors.OrderBy(e => ((dynamic)e).zone)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error scraping state {stateName}: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh/re-scrape a specific zone (force update)
    /// POST: api/scrap/refresh/WLY01
    /// </summary>
    [HttpPost("refresh/{zoneCode}")]
    public async Task<IActionResult> RefreshZone(string zoneCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(zoneCode))
            {
                return BadRequest(new { error = "Zone code is required" });
            }

            Console.WriteLine($"=== Force refreshing zone: {zoneCode} ===");

            var result = await _waktuSolatService.RefreshWaktuSolatAsync(zoneCode.ToUpper());

            if (result == null)
            {
                return NotFound(new { error = $"Failed to refresh data for zone {zoneCode}" });
            }

            return Ok(new
            {
                success = true,
                message = $"Successfully refreshed data for zone {zoneCode}",
                data = result
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error refreshing zone {zoneCode}: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get list of all available zones (without scraping)
    /// GET: api/scrap/zones
    /// </summary>
    [HttpGet("zones")]
    public async Task<IActionResult> GetAllZones()
    {
        try
        {
            var allZones = await _waktuSolatService.GetAllZonesAsync();

            return Ok(new
            {
                success = true,
                totalStates = allZones.Count,
                totalZones = allZones.Sum(z => z.Zones.Count),
                zones = allZones
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error getting zones: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}