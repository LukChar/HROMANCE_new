namespace HRomance.Models;

public class MatchingZuweisung
{
    public Auftrag Auftrag { get; set; } = null!;

    public Mitarbeiter? Mitarbeiter { get; set; }

    public int PassendeQualifikationen { get; set; }

    public int BenoetigteQualifikationen { get; set; }

    public int BestehendeEinsaetze { get; set; }
}
