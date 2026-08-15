using System.ComponentModel.DataAnnotations;

namespace HRomance.Models;

public class Arbeitszeit
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Bitte einen Mitarbeiter auswählen.")]
    public int MitarbeiterId { get; set; }

    public Mitarbeiter? Mitarbeiter { get; set; }

    public DateTime Datum { get; set; }

    public TimeOnly Beginn { get; set; }

    public TimeOnly Ende { get; set; }

    public int PauseMinuten { get; set; }

    public string? Notiz { get; set; }
}
