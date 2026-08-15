using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Services;

public class ArbeitszeitService
{
    private readonly ApplicationDbContext _context;

    public ArbeitszeitService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Arbeitszeit>> GetAllAsync()
    {
        return await _context.Arbeitszeiten
            .Include(a => a.Mitarbeiter)
            .ToListAsync();
    }

    public async Task<Arbeitszeit?> GetByIdAsync(int id)
    {
        return await _context.Arbeitszeiten
            .Include(a => a.Mitarbeiter)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Arbeitszeit>> GetByMitarbeiterAsync(int mitarbeiterId)
    {
        return await _context.Arbeitszeiten
            .Include(a => a.Mitarbeiter)
            .Where(a => a.MitarbeiterId == mitarbeiterId)
            .ToListAsync();
    }

    public async Task AddAsync(Arbeitszeit arbeitszeit)
    {
        if (Validierungsfehler(arbeitszeit) != string.Empty)
        {
            return;
        }

        _context.Arbeitszeiten.Add(arbeitszeit);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Arbeitszeit arbeitszeit)
    {
        if (Validierungsfehler(arbeitszeit) != string.Empty)
        {
            return;
        }

        var vorhandeneArbeitszeit = await _context.Arbeitszeiten.FindAsync(arbeitszeit.Id);

        if (vorhandeneArbeitszeit != null)
        {
            vorhandeneArbeitszeit.MitarbeiterId = arbeitszeit.MitarbeiterId;
            vorhandeneArbeitszeit.Datum = arbeitszeit.Datum;
            vorhandeneArbeitszeit.Beginn = arbeitszeit.Beginn;
            vorhandeneArbeitszeit.Ende = arbeitszeit.Ende;
            vorhandeneArbeitszeit.PauseMinuten = arbeitszeit.PauseMinuten;
            vorhandeneArbeitszeit.Notiz = arbeitszeit.Notiz;

            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var arbeitszeit = await _context.Arbeitszeiten.FindAsync(id);

        if (arbeitszeit != null)
        {
            _context.Arbeitszeiten.Remove(arbeitszeit);
            await _context.SaveChangesAsync();
        }
    }

    public string Validierungsfehler(Arbeitszeit arbeitszeit)
    {
        if (arbeitszeit.Ende <= arbeitszeit.Beginn)
        {
            return "Das Ende muss nach dem Beginn liegen.";
        }

        if (arbeitszeit.PauseMinuten < 0)
        {
            return "Die Pause darf nicht negativ sein.";
        }

        var gesamteDauer = arbeitszeit.Ende - arbeitszeit.Beginn;

        if (arbeitszeit.PauseMinuten > gesamteDauer.TotalMinutes)
        {
            return "Die Pause darf nicht länger als die gesamte Arbeitsdauer sein.";
        }

        return string.Empty;
    }

    public double BerechneArbeitsstunden(Arbeitszeit arbeitszeit)
    {
        if (Validierungsfehler(arbeitszeit) != string.Empty)
        {
            return 0;
        }

        var dauer = arbeitszeit.Ende - arbeitszeit.Beginn;
        return dauer.TotalHours - arbeitszeit.PauseMinuten / 60.0;
    }

    public double BerechneMonatsstunden(
        List<Arbeitszeit> arbeitszeiten,
        int mitarbeiterId,
        int jahr,
        int monat)
    {
        var monatsstunden = 0.0;

        foreach (var arbeitszeit in arbeitszeiten)
        {
            if (arbeitszeit.MitarbeiterId == mitarbeiterId
                && arbeitszeit.Datum.Year == jahr
                && arbeitszeit.Datum.Month == monat)
            {
                monatsstunden += BerechneArbeitsstunden(arbeitszeit);
            }
        }

        return monatsstunden;
    }

    public bool PasstZuMitarbeiter(Arbeitszeit arbeitszeit, int mitarbeiterId)
    {
        return mitarbeiterId == 0 || arbeitszeit.MitarbeiterId == mitarbeiterId;
    }

    public Arbeitszeit ErstelleArbeitskopie(Arbeitszeit arbeitszeit)
    {
        return new Arbeitszeit
        {
            Id = arbeitszeit.Id,
            MitarbeiterId = arbeitszeit.MitarbeiterId,
            Datum = arbeitszeit.Datum,
            Beginn = arbeitszeit.Beginn,
            Ende = arbeitszeit.Ende,
            PauseMinuten = arbeitszeit.PauseMinuten,
            Notiz = arbeitszeit.Notiz
        };
    }

    public double BerechneTagessaldo(List<Arbeitszeit> arbeitszeiten, double sollStunden)
    {
        var istStunden = 0.0;

        foreach (var arbeitszeit in arbeitszeiten)
        {
            istStunden += BerechneArbeitsstunden(arbeitszeit);
        }

        return istStunden - sollStunden;
    }

    public async Task<(double Ist, double Soll, double Saldo)> GetMonatswerteAsync(
        int mitarbeiterId,
        int jahr,
        int monat,
        double sollStundenProTag)
    {
        var alleArbeitszeiten = await GetByMitarbeiterAsync(mitarbeiterId);
        var istStunden = 0.0;
        var tageMitArbeitszeit = new List<DateTime>();

        foreach (var arbeitszeit in alleArbeitszeiten)
        {
            if (arbeitszeit.Datum.Year == jahr && arbeitszeit.Datum.Month == monat)
            {
                istStunden += BerechneArbeitsstunden(arbeitszeit);

                if (!tageMitArbeitszeit.Contains(arbeitszeit.Datum.Date))
                {
                    tageMitArbeitszeit.Add(arbeitszeit.Datum.Date);
                }
            }
        }

        var sollStunden = tageMitArbeitszeit.Count * sollStundenProTag;
        return (istStunden, sollStunden, istStunden - sollStunden);
    }
}
