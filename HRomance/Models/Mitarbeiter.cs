using System.ComponentModel.DataAnnotations;

namespace HRomance.Models;

public class Mitarbeiter
{
    public int Id { get; set; }

    [Required]
    public string Personalnummer { get; set; } = string.Empty;

    [Required]
    public string Vorname { get; set; } = string.Empty;

    [Required]
    public string Nachname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Telefon { get; set; } = string.Empty;

    public string Qualifikation { get; set; } = string.Empty;

    public List<Qualifikation> Qualifikationen { get; set; } = new();

    public List<Auftrag> Auftraege { get; set; } = new();

    public bool Verfuegbar { get; set; } = true;

    [Range(0, 24, ErrorMessage = "Die Sollstunden müssen zwischen 0 und 24 liegen.")]
    public double SollStundenProTag { get; set; } = 8;
}
