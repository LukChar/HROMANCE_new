using System.ComponentModel.DataAnnotations;

namespace HRomance.Models;

public class Materialeintrag
{
    public int Id { get; set; }

    [Required]
    public string Bezeichnung { get; set; } = string.Empty;

    public int Anzahl { get; set; } = 1;

    public bool Erledigt { get; set; }

    public int AuftragId { get; set; }

    public Auftrag? Auftrag { get; set; }
}
