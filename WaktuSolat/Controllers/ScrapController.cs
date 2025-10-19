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
    private readonly ZoneService _zoneService;
    private readonly IConfiguration _configuration;

    public ScrapController(
        WaktuSolatService waktuSolatService,
        ZoneService zoneService,
        IConfiguration configuration)
    {
        _waktuSolatService = waktuSolatService;
        _zoneService = zoneService;
        _configuration = configuration;
    }

    /// Scrape and save waktu solat for all zones using PARALLEL processing with retry
    /// GET: api/scrap/all?parallel=true&maxDegree=3&retryFailed=true
    [HttpGet("all")]
    public async Task<IActionResult> ScrapeAllZones(
        [FromQuery] bool parallel = true,
        [FromQuery] int maxDegree = 3,
        [FromQuery] bool retryFailed = true)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"=== Scraping all zones (Parallel: {parallel}, Max Degree: {maxDegree}) ===");

            // Get zones from database (will auto-scrape if empty)
            var allZones = await _zoneService.GetAllZonesAsync();
            var results = new ConcurrentBag<object>();
            var errors = new ConcurrentBag<object>();

            if (!allZones.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No zones available. Please run POST /api/zone/scrape first."
                });
            }

            if (parallel)
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxDegree
                };

                await Parallel.ForEachAsync(allZones, parallelOptions, async (zone, ct) =>
                {
                    try
                    {
                        var result = await _waktuSolatService.RefreshWaktuSolatAsync(zone.ZoneCode);

                        if (result != null)
                        {
                            results.Add(new
                            {
                                zone = zone.ZoneCode,
                                state = zone.State,
                                success = true
                            });
                            Console.WriteLine($"✓ [{results.Count + errors.Count}/{allZones.Count}] Scraped {zone.ZoneCode}");
                        }
                        else
                        {
                            errors.Add(new
                            {
                                zone = zone.ZoneCode,
                                state = zone.State,
                                error = "Failed to scrape"
                            });
                            Console.WriteLine($"✗ [{results.Count + errors.Count}/{allZones.Count}] Failed {zone.ZoneCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new
                        {
                            zone = zone.ZoneCode,
                            state = zone.State,
                            error = ex.Message
                        });
                        Console.WriteLine($"✗ [{results.Count + errors.Count}/{allZones.Count}] Error {zone.ZoneCode}: {ex.Message}");
                    }
                });
            }
            else
            {
                int processed = 0;
                foreach (var zone in allZones)
                {
                    try
                    {
                        processed++;
                        var result = await _waktuSolatService.RefreshWaktuSolatAsync(zone.ZoneCode);

                        if (result != null)
                        {
                            results.Add(new
                            {
                                zone = zone.ZoneCode,
                                state = zone.State,
                                success = true
                            });
                            Console.WriteLine($"✓ [{processed}/{allZones.Count}] Scraped {zone.ZoneCode}");
                        }
                        else
                        {
                            errors.Add(new
                            {
                                zone = zone.ZoneCode,
                                state = zone.State,
                                error = "Failed to scrape"
                            });
                            Console.WriteLine($"✗ [{processed}/{allZones.Count}] Failed {zone.ZoneCode}");
                        }

                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new
                        {
                            zone = zone.ZoneCode,
                            state = zone.State,
                            error = ex.Message
                        });
                        Console.WriteLine($"✗ [{processed}/{allZones.Count}] Error {zone.ZoneCode}: {ex.Message}");
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
                        await Task.Delay(2000);

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

                errors = retriedErrors;
            }

            return Ok(new
            {
                success = true,
                message = $"Completed in {stopwatch.Elapsed.TotalMinutes:F2} minutes. Success: {results.Count}, Errors: {errors.Count}",
                parallel = parallel,
                maxDegreeOfParallelism = maxDegree,
                totalZones = allZones.Count,
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
}