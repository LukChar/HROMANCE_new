namespace HRomance.Models;

public class MatchingVorschlag
{
    public int Nummer { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<MatchingZuweisung> Zuweisungen { get; set; } = new();
}
