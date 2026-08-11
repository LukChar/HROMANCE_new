using System.ComponentModel.DataAnnotations;

namespace HRomance.Models;

public class Abwesenheit
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Bitte einen Mitarbeiter auswählen.")]
    public int MitarbeiterId { get; set; }

    public Mitarbeiter? Mitarbeiter { get; set; }

    public DateTime Von { get; set; }

    public DateTime Bis { get; set; }

    public string Typ { get; set; } = string.Empty;

    public string? Grund { get; set; }

    public string Status { get; set; } = "Offen";
}
