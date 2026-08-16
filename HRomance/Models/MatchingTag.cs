namespace HRomance.Models;

public class MatchingTag
{
    public DateTime Datum { get; set; }

    public List<MatchingZuweisung> Zuweisungen { get; set; } = new();
}
