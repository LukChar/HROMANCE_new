using HRomance.Models;

public class MatchingErgebnis
{
    public Mitarbeiter Mitarbeiter { get; set; } = null!;

    public int Punkte { get; set; }

    public string Bewertung { get; set; } = string.Empty;
}