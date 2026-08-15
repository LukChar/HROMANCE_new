using System.ComponentModel.DataAnnotations;

namespace HRomance.Models;

public class Qualifikation
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public List<Mitarbeiter> Mitarbeiter { get; set; } = new();

    public List<Auftrag> Auftraege { get; set; } = new();
}
