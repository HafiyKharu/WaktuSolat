using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaktuSolat.Models;

public class WaktuSolatEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string czone { get; set; } = string.Empty;
    public string cbearing { get; set; } = string.Empty;
    public string TarikhMasehi { get; set; } = string.Empty;
    public string TarikhHijrah { get; set; } = string.Empty;
    public string Imsak { get; set; } = string.Empty;
    public string Subuh { get; set; } = string.Empty;
    public string Syuruk { get; set; } = string.Empty;
    public string Dhuha { get; set; } = string.Empty;
    public string Zohor { get; set; } = string.Empty;
    public string Asar { get; set; } = string.Empty;
    public string Maghrib { get; set; } = string.Empty;
    public string Isyak { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}