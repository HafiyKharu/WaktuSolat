using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaktuSolat.Models;

public class InputZone
{
    public string Negeri { get; set; } = string.Empty;
    public List<InputZones> Zones { get; set; } = new List<InputZones>();
}

public class InputZones
{
    public string ZoneCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ZoneGroup
{
    public string State { get; set; } = string.Empty;
    public List<ZoneOption> Zones { get; set; } = new List<ZoneOption>();
}

public class ZoneOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}