using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Services;

public class AbwesenheitService
{
    private readonly ApplicationDbContext _context;

    public AbwesenheitService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Abwesenheit>> GetAllAsync()
    {
        return await _context.Abwesenheiten
            .Include(a => a.Mitarbeiter)
            .ToListAsync();
    }

    public async Task<Abwesenheit?> GetByIdAsync(int id)
    {
        return await _context.Abwesenheiten
            .Include(a => a.Mitarbeiter)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Abwesenheit>> GetByMitarbeiterAsync(int mitarbeiterId)
    {
        return await _context.Abwesenheiten
            .Include(a => a.Mitarbeiter)
            .Where(a => a.MitarbeiterId == mitarbeiterId)
            .ToListAsync();
    }

    public async Task AddAsync(Abwesenheit abwesenheit)
    {
        _context.Abwesenheiten.Add(abwesenheit);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> PersoenlichenAntragHinzufuegenAsync(
        Abwesenheit antrag,
        int mitarbeiterId)
    {
        if (antrag.Id != 0
            || mitarbeiterId <= 0
            || antrag.Bis.Date < antrag.Von.Date)
        {
            return false;
        }

        antrag.MitarbeiterId = mitarbeiterId;
        antrag.Status = "Offen";

        _context.Abwesenheiten.Add(antrag);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UpdateAsync(Abwesenheit abwesenheit)
    {
        var vorhandeneAbwesenheit = await _context.Abwesenheiten.FindAsync(abwesenheit.Id);

        if (vorhandeneAbwesenheit != null)
        {
            vorhandeneAbwesenheit.MitarbeiterId = abwesenheit.MitarbeiterId;
            vorhandeneAbwesenheit.Von = abwesenheit.Von;
            vorhandeneAbwesenheit.Bis = abwesenheit.Bis;
            vorhandeneAbwesenheit.Typ = abwesenheit.Typ;
            vorhandeneAbwesenheit.Grund = abwesenheit.Grund;
            vorhandeneAbwesenheit.Status = abwesenheit.Status;

            await _context.SaveChangesAsync();
        }
    }

    public async Task StatusAendernAsync(int id, string neuerStatus)
    {
        var abwesenheit = await _context.Abwesenheiten.FindAsync(id);

        if (abwesenheit != null
            && abwesenheit.Status == "Offen"
            && (neuerStatus == "Genehmigt" || neuerStatus == "Abgelehnt"))
        {
            abwesenheit.Status = neuerStatus;
            await _context.SaveChangesAsync();
        }
    }

    public List<Abwesenheit> FilternUndSortieren(
        List<Abwesenheit> antraege,
        string suche,
        string art,
        string status,
        bool neuesteZuerst)
    {
        var ergebnis = new List<Abwesenheit>();

        foreach (var antrag in antraege)
        {
            var mitarbeiterName = string.Empty;

            if (antrag.Mitarbeiter != null)
            {
                mitarbeiterName = antrag.Mitarbeiter.Vorname + " " + antrag.Mitarbeiter.Nachname;
            }

            var passtZurSuche = suche == string.Empty
                || mitarbeiterName.Contains(suche, StringComparison.OrdinalIgnoreCase)
                || antrag.Typ.Contains(suche, StringComparison.OrdinalIgnoreCase)
                || (antrag.Grund?.Contains(suche, StringComparison.OrdinalIgnoreCase) == true);

            var passtZurArt = art == "Alle" || antrag.Typ == art;
            var passtZumStatus = status == "Alle" || antrag.Status == status;

            if (passtZurSuche && passtZurArt && passtZumStatus)
            {
                ergebnis.Add(antrag);
            }
        }

        for (var i = 0; i < ergebnis.Count - 1; i++)
        {
            for (var j = 0; j < ergebnis.Count - i - 1; j++)
            {
                var tauschen = neuesteZuerst
                    ? ergebnis[j].Von < ergebnis[j + 1].Von
                    : ergebnis[j].Von > ergebnis[j + 1].Von;

                if (tauschen)
                {
                    var zwischenspeicher = ergebnis[j];
                    ergebnis[j] = ergebnis[j + 1];
                    ergebnis[j + 1] = zwischenspeicher;
                }
            }
        }

        return ergebnis;
    }

    public bool PasstZuMitarbeiter(Abwesenheit abwesenheit, int mitarbeiterId)
    {
        return mitarbeiterId == 0 || abwesenheit.MitarbeiterId == mitarbeiterId;
    }

    public bool IstOffenerAntrag(Abwesenheit abwesenheit)
    {
        return abwesenheit.Status == "Offen";
    }

    public bool IstAbwesendAmTag(Abwesenheit abwesenheit, DateTime datum)
    {
        return abwesenheit.Status != "Abgelehnt"
            && abwesenheit.Von.Date <= datum.Date
            && abwesenheit.Bis.Date >= datum.Date;
    }

    public string KalenderSegmentKlasse(Abwesenheit abwesenheit, DateTime datum)
    {
        var beginnt = IstKalenderSegmentStart(abwesenheit, datum);
        var endet = IstKalenderSegmentEnde(abwesenheit, datum);

        if (beginnt && endet)
        {
            return "absence-single";
        }

        if (beginnt)
        {
            return "absence-start";
        }

        if (endet)
        {
            return "absence-end";
        }

        return "absence-middle";
    }

    public bool IstKalenderSegmentStart(Abwesenheit abwesenheit, DateTime datum)
    {
        return datum.Date == abwesenheit.Von.Date
            || datum.DayOfWeek == DayOfWeek.Monday
            || datum.Day == 1;
    }

    private bool IstKalenderSegmentEnde(Abwesenheit abwesenheit, DateTime datum)
    {
        var letzterTagImMonat = DateTime.DaysInMonth(datum.Year, datum.Month);

        return datum.Date == abwesenheit.Bis.Date
            || datum.DayOfWeek == DayOfWeek.Sunday
            || datum.Day == letzterTagImMonat;
    }

    public async Task DeleteAsync(int id)
    {
        var abwesenheit = await _context.Abwesenheiten.FindAsync(id);

        if (abwesenheit != null)
        {
            _context.Abwesenheiten.Remove(abwesenheit);
            await _context.SaveChangesAsync();
        }
    }
}
