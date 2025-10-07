using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using WaktuSolat.Models;
using Microsoft.Extensions.Configuration;

namespace WaktuSolat.Services;

public class ExtractExcel
{
    private readonly IConfiguration _config;
    private readonly string _zoneCode;
    private readonly string _filePath;

    public ExtractExcel(IConfiguration config)
    {
        _config = config;
        _zoneCode = _config["ZoneCode"] ?? throw new InvalidOperationException("ZoneCode is not configured in appsettings.json");
        _filePath = _config["FilePath"] ?? throw new InvalidOperationException("FilePath is not configured in appsettings.json");
    }

    public async Task ExportToCsvAsync(WaktuSolatEntity data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var writer = new StreamWriter(_filePath, false, System.Text.Encoding.UTF8);
            await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = !File.Exists(_filePath) || new FileInfo(_filePath).Length == 0
            });

            csv.WriteRecords([data]);
            await writer.FlushAsync();

            Console.WriteLine($"✓ Saved waktu solat for zone {_zoneCode} to: {_filePath}");
        }
        catch (IOException ioEx)
        {
            Console.WriteLine($"✗ File access error: {ioEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error exporting to CSV: {ex.Message}");
            throw;
        }
    }

    public async Task AppendToCsvAsync(WaktuSolatEntity data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileExists = File.Exists(_filePath) && new FileInfo(_filePath).Length > 0;

            await using var writer = new StreamWriter(_filePath, append: true, System.Text.Encoding.UTF8);
            await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = !fileExists
            });

            csv.WriteRecords(new[] { data });
            await writer.FlushAsync();

            Console.WriteLine($"✓ Appended waktu solat for zone {_zoneCode} to: {_filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error appending to CSV: {ex.Message}");
            throw;
        }
    }
}