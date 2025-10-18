using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaktuSolat.Models;

[Table("waktu_solat")]
public class WaktuSolatEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("czone")]
    [MaxLength(200)]
    public string czone { get; set; } = string.Empty;

    [Column("cbearing")]
    [MaxLength(200)]
    public string cbearing { get; set; } = string.Empty;

    [Column("tarikh_masehi")]
    [MaxLength(20)]
    public string TarikhMasehi { get; set; } = string.Empty;

    [Column("tarikh_hijrah")]
    [MaxLength(20)]
    public string TarikhHijrah { get; set; } = string.Empty;

    [Column("imsak")]
    [MaxLength(15)]
    public string Imsak { get; set; } = string.Empty;

    [Column("subuh")]
    [MaxLength(15)]
    public string Subuh { get; set; } = string.Empty;

    [Column("syuruk")]
    [MaxLength(15)]
    public string Syuruk { get; set; } = string.Empty;

    [Column("dhuha")]
    [MaxLength(15)]
    public string Dhuha { get; set; } = string.Empty;

    [Column("zohor")]
    [MaxLength(15)]
    public string Zohor { get; set; } = string.Empty;

    [Column("asar")]
    [MaxLength(15)]
    public string Asar { get; set; } = string.Empty;

    [Column("maghrib")]
    [MaxLength(15)]
    public string Maghrib { get; set; } = string.Empty;

    [Column("isyak")]
    [MaxLength(15)]
    public string Isyak { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}