using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaktuSolat.Models;

[Table("zones")]
public class Zone
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("zone_code")]
    [MaxLength(20)]
    [Required]
    public string ZoneCode { get; set; } = string.Empty;

    [Column("state")]
    [MaxLength(100)]
    [Required]
    public string State { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(200)]
    [Required]
    public string Description { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// DTO for zone input
public class ZoneInput
{
    public string ZoneCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BulkZoneInput
{
    public List<ZoneInput> Zones { get; set; } = new List<ZoneInput>();
}

public class ZoneOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ZoneGroup
{
    public string State { get; set; } = string.Empty;
    public List<ZoneOption> Zones { get; set; } = new();
}