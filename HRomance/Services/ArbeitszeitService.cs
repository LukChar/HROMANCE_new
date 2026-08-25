using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

    public string ZeitAnzeigen(TimeOnly zeit)
    {
        return zeit.ToString("HH:mm");
    }

    public string ZeitraumAnzeigen(TimeOnly beginn, TimeOnly ende)
    {
        return ZeitAnzeigen(beginn) + " - " + ZeitAnzeigen(ende);
    }

    public bool TryParseZeit(string text, out TimeOnly zeit)
    {
        return TimeOnly.TryParseExact(
            text,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out zeit);
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

    public double BerechneSollstundenProTag(double wochenarbeitszeit)
    {
        return wochenarbeitszeit / 5;
    }

    public double BerechneMonatssoll(int jahr, int monat, double wochenarbeitszeit)
    {
        return BerechneMonatssoll(jahr, monat, wochenarbeitszeit, DateTime.Today);
    }

    public double BerechneMonatssoll(
        int jahr,
        int monat,
        double wochenarbeitszeit,
        DateTime aktuellesDatum)
    {
        return BerechneMonatssoll(
            jahr,
            monat,
            wochenarbeitszeit,
            aktuellesDatum,
            new List<Abwesenheit>(),
            0);
    }

    public double BerechneMonatssoll(
        int jahr,
        int monat,
        double wochenarbeitszeit,
        DateTime aktuellesDatum,
        List<Abwesenheit> abwesenheiten,
        int mitarbeiterId)
    {
        var sollstunden = 0.0;
        var sollstundenProTag = BerechneSollstundenProTag(wochenarbeitszeit);
        var ersterTag = new DateTime(jahr, monat, 1);
        var aktuellerMonat = new DateTime(aktuellesDatum.Year, aktuellesDatum.Month, 1);

        if (ersterTag > aktuellerMonat)
        {
            return 0;
        }

        var letzterTag = new DateTime(jahr, monat, DateTime.DaysInMonth(jahr, monat));

        if (ersterTag == aktuellerMonat)
        {
            letzterTag = aktuellesDatum.Date;
        }

        var tag = ersterTag;

        while (tag <= letzterTag)
        {
            if (tag.DayOfWeek != DayOfWeek.Saturday
                && tag.DayOfWeek != DayOfWeek.Sunday
                && !IstGesetzlicherFeiertag(tag)
                && !HatSollreduzierendeAbwesenheit(abwesenheiten, mitarbeiterId, tag))
            {
                sollstunden += sollstundenProTag;
            }

            tag = tag.AddDays(1);
        }

        return sollstunden;
    }

    public double BerechneSaldo(double iststunden, double sollstunden)
    {
        return iststunden - sollstunden;
    }

    public bool IstGesetzlicherFeiertag(DateTime datum)
    {
        var tag = datum.Date;
        var ostersonntag = BerechneOstersonntag(tag.Year);

        if (tag == new DateTime(tag.Year, 1, 1)
            || tag == new DateTime(tag.Year, 1, 6)
            || tag == new DateTime(tag.Year, 5, 1)
            || tag == new DateTime(tag.Year, 8, 15)
            || tag == new DateTime(tag.Year, 10, 26)
            || tag == new DateTime(tag.Year, 11, 1)
            || tag == new DateTime(tag.Year, 12, 8)
            || tag == new DateTime(tag.Year, 12, 25)
            || tag == new DateTime(tag.Year, 12, 26))
        {
            return true;
        }

        return tag == ostersonntag.AddDays(1)
            || tag == ostersonntag.AddDays(39)
            || tag == ostersonntag.AddDays(50)
            || tag == ostersonntag.AddDays(60);
    }

    private DateTime BerechneOstersonntag(int jahr)
    {
        var a = jahr % 19;
        var b = jahr / 100;
        var c = jahr % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var monat = (h + l - 7 * m + 114) / 31;
        var tag = (h + l - 7 * m + 114) % 31 + 1;

        return new DateTime(jahr, monat, tag);
    }

    private bool HatSollreduzierendeAbwesenheit(
        List<Abwesenheit> abwesenheiten,
        int mitarbeiterId,
        DateTime datum)
    {
        foreach (var abwesenheit in abwesenheiten)
        {
            if (abwesenheit.MitarbeiterId == mitarbeiterId
                && abwesenheit.Status == "Genehmigt"
                && abwesenheit.Von.Date <= datum.Date
                && abwesenheit.Bis.Date >= datum.Date
                && (abwesenheit.Typ == "Urlaub"
                    || abwesenheit.Typ == "Krankenstand"
                    || abwesenheit.Typ == "Sonstige Abwesenheit"))
            {
                return true;
            }
        }

        return false;
    }

    public (double Arbeitszeit, double Soll, int Abwesenheitstage, double Saldo)
        BerechneMonatsauswertung(
            List<Arbeitszeit> arbeitszeiten,
            List<Abwesenheit> abwesenheiten,
            int mitarbeiterId,
            int jahr,
            int monat,
            double wochenarbeitszeit,
            DateTime aktuellesDatum)
    {
        var arbeitsstunden = 0.0;
        var sollstunden = 0.0;
        var abwesenheitstage = 0;
        var tagesSoll = BerechneSollstundenProTag(wochenarbeitszeit);
        var ersterTag = new DateTime(jahr, monat, 1);
        var aktuellerMonat = new DateTime(aktuellesDatum.Year, aktuellesDatum.Month, 1);

        if (ersterTag > aktuellerMonat)
        {
            return (0, 0, 0, 0);
        }

        var letzterTag = new DateTime(jahr, monat, DateTime.DaysInMonth(jahr, monat));

        if (ersterTag == aktuellerMonat)
        {
            letzterTag = aktuellesDatum.Date;
        }

        var tag = ersterTag;

        while (tag <= letzterTag)
        {
            if (tag.DayOfWeek != DayOfWeek.Saturday
                && tag.DayOfWeek != DayOfWeek.Sunday)
            {
                if (HatSollreduzierendeAbwesenheit(abwesenheiten, mitarbeiterId, tag))
                {
                    abwesenheitstage++;
                }
                else
                {
                    if (!IstGesetzlicherFeiertag(tag))
                    {
                        sollstunden += tagesSoll;
                    }

                    foreach (var arbeitszeit in arbeitszeiten)
                    {
                        if (arbeitszeit.MitarbeiterId == mitarbeiterId
                            && arbeitszeit.Datum.Date == tag.Date)
                        {
                            arbeitsstunden += BerechneArbeitsstunden(arbeitszeit);
                        }
                    }
                }
            }

            tag = tag.AddDays(1);
        }

        var saldo = BerechneSaldo(arbeitsstunden, sollstunden);
        return (arbeitsstunden, sollstunden, abwesenheitstage, saldo);
    }

    public async Task<(double Ist, double Soll, double Saldo)> GetMonatswerteAsync(
        int mitarbeiterId,
        int jahr,
        int monat,
        double wochenarbeitszeit)
    {
        return await GetMonatswerteAsync(
            mitarbeiterId,
            jahr,
            monat,
            wochenarbeitszeit,
            DateTime.Today);
    }

    public async Task<(double Ist, double Soll, double Saldo)> GetMonatswerteAsync(
        int mitarbeiterId,
        int jahr,
        int monat,
        double wochenarbeitszeit,
        DateTime aktuellesDatum)
    {
        var auswertung = await GetMonatsauswertungAsync(
            mitarbeiterId,
            jahr,
            monat,
            wochenarbeitszeit,
            aktuellesDatum);

        return (auswertung.Arbeitszeit, auswertung.Soll, auswertung.Saldo);
    }

    public async Task<(double Arbeitszeit, double Soll, int Abwesenheitstage, double Saldo)>
        GetMonatsauswertungAsync(
            int mitarbeiterId,
            int jahr,
            int monat,
            double wochenarbeitszeit,
            DateTime aktuellesDatum)
    {
        var alleArbeitszeiten = await GetByMitarbeiterAsync(mitarbeiterId);
        var alleAbwesenheiten = await _context.Abwesenheiten
            .Where(a => a.MitarbeiterId == mitarbeiterId)
            .ToListAsync();

        return BerechneMonatsauswertung(
            alleArbeitszeiten,
            alleAbwesenheiten,
            mitarbeiterId,
            jahr,
            monat,
            wochenarbeitszeit,
            aktuellesDatum);
    }

    public async Task<List<ArbeitszeitMonat>> GetMonatsuebersichtAsync(
        int mitarbeiterId,
        double wochenarbeitszeit,
        int anzahlMonate,
        DateTime aktuellesDatum)
    {
        var monate = new List<ArbeitszeitMonat>();
        var alleArbeitszeiten = await GetByMitarbeiterAsync(mitarbeiterId);
        var alleAbwesenheiten = await _context.Abwesenheiten
            .Where(a => a.MitarbeiterId == mitarbeiterId)
            .ToListAsync();
        var ersterMonat = new DateTime(aktuellesDatum.Year, aktuellesDatum.Month, 1);

        for (var i = 0; i < anzahlMonate; i++)
        {
            var monat = ersterMonat.AddMonths(-i);
            var auswertung = BerechneMonatsauswertung(
                alleArbeitszeiten,
                alleAbwesenheiten,
                mitarbeiterId,
                monat.Year,
                monat.Month,
                wochenarbeitszeit,
                aktuellesDatum);

            monate.Add(new ArbeitszeitMonat
            {
                Jahr = monat.Year,
                Monat = monat.Month,
                Ist = auswertung.Arbeitszeit,
                Soll = auswertung.Soll,
                Saldo = auswertung.Saldo,
                Abwesenheitstage = auswertung.Abwesenheitstage
            });
        }

        var laufenderSaldo = 0.0;

        for (var i = monate.Count - 1; i >= 0; i--)
        {
            laufenderSaldo += monate[i].Saldo;
            monate[i].LaufenderSaldo = laufenderSaldo;
        }

        return monate;
    }
}
