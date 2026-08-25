using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Services;

public class MatchingService
{
    private readonly ApplicationDbContext _context;

    public MatchingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MatchingVorschlag>> ErstelleVorschlaegeAsync(
        DateTime startdatum,
        int anzahlTage = 14)
    {
        var enddatum = startdatum.Date.AddDays(anzahlTage - 1);
        var auftraege = await _context.Auftraege
            .Include(a => a.Kunde)
            .Include(a => a.Qualifikationen)
            .Include(a => a.Mitarbeiter)
            .Where(a => a.Enddatum.Date >= startdatum.Date
                && a.Startdatum.Date <= enddatum
                && !a.Mitarbeiter.Any())
            .OrderBy(a => a.Startdatum)
            .ThenBy(a => a.Enddatum)
            .ThenBy(a => a.Id)
            .ToListAsync();

        if (auftraege.Count == 0)
        {
            return new List<MatchingVorschlag>();
        }

        var mitarbeiter = await _context.Mitarbeiter
            .Include(m => m.Qualifikationen)
            .ToListAsync();
        var abwesenheiten = await _context.Abwesenheiten.ToListAsync();
        var bestehendeAuftraege = await _context.Auftraege
            .Include(a => a.Mitarbeiter)
            .ToListAsync();

        return
        [
            ErstelleVorschlag(1, "Beste Qualifikation", auftraege, mitarbeiter,
                abwesenheiten, bestehendeAuftraege, startdatum.Date, enddatum),
            ErstelleVorschlag(2, "Ausgeglichen", auftraege, mitarbeiter,
                abwesenheiten, bestehendeAuftraege, startdatum.Date, enddatum),
            ErstelleVorschlag(3, "Alternative", auftraege, mitarbeiter,
                abwesenheiten, bestehendeAuftraege, startdatum.Date, enddatum)
        ];
    }

    public async Task<bool> VorschlagUebernehmenAsync(MatchingVorschlag vorschlag)
    {
        var auftraege = await _context.Auftraege
            .Include(a => a.Mitarbeiter)
            .Include(a => a.Qualifikationen)
            .ToListAsync();
        var mitarbeiter = await _context.Mitarbeiter
            .Include(m => m.Qualifikationen)
            .ToListAsync();
        var abwesenheiten = await _context.Abwesenheiten.ToListAsync();
        var zuPruefendeZuweisungen = new List<MatchingZuweisung>();

        foreach (var zuweisung in vorschlag.Zuweisungen)
        {
            if (zuweisung.Mitarbeiter == null)
            {
                continue;
            }

            var aktuellerAuftrag = AuftragFinden(auftraege, zuweisung.Auftrag.Id);
            var aktuellerMitarbeiter = MitarbeiterFinden(mitarbeiter, zuweisung.Mitarbeiter.Id);

            if (aktuellerAuftrag == null
                || aktuellerMitarbeiter == null
                || aktuellerAuftrag.Mitarbeiter.Count > 0
                || !IstVerfuegbar(aktuellerMitarbeiter, aktuellerAuftrag,
                    abwesenheiten, auftraege, zuPruefendeZuweisungen))
            {
                return false;
            }

            zuPruefendeZuweisungen.Add(new MatchingZuweisung
            {
                Auftrag = aktuellerAuftrag,
                Mitarbeiter = aktuellerMitarbeiter
            });
        }

        foreach (var zuweisung in zuPruefendeZuweisungen)
        {
            zuweisung.Auftrag.Mitarbeiter.Add(zuweisung.Mitarbeiter!);
            zuweisung.Auftrag.Besetzt = true;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MatchingZuweisung>> MoeglicheMitarbeiterAsync(
        MatchingVorschlag vorschlag,
        MatchingZuweisung zuBearbeiten)
    {
        var mitarbeiter = await _context.Mitarbeiter
            .Include(m => m.Qualifikationen)
            .ToListAsync();
        var abwesenheiten = await _context.Abwesenheiten.ToListAsync();
        var bestehendeAuftraege = await _context.Auftraege
            .Include(a => a.Mitarbeiter)
            .ToListAsync();
        var andereZuweisungen = new List<MatchingZuweisung>();

        foreach (var zuweisung in vorschlag.Zuweisungen)
        {
            if (zuweisung != zuBearbeiten)
            {
                andereZuweisungen.Add(zuweisung);
            }
        }

        var startdatum = zuBearbeiten.Auftrag.Startdatum.Date;
        var enddatum = zuBearbeiten.Auftrag.Enddatum.Date;

        return PassendeKandidaten(
            zuBearbeiten.Auftrag,
            mitarbeiter,
            abwesenheiten,
            bestehendeAuftraege,
            andereZuweisungen,
            startdatum,
            enddatum,
            vorschlag.Nummer);
    }

    public MatchingVorschlag? VorschlagAuswaehlen(
        List<MatchingVorschlag> vorschlaege,
        int nummer)
    {
        foreach (var vorschlag in vorschlaege)
        {
            if (vorschlag.Nummer == nummer)
            {
                return vorschlag;
            }
        }

        return null;
    }

    public List<MatchingTag> TagesgruppenErstellen(MatchingVorschlag vorschlag)
    {
        var tage = new List<MatchingTag>();

        foreach (var zuweisung in vorschlag.Zuweisungen)
        {
            var datum = zuweisung.Auftrag.Startdatum.Date;

            while (datum <= zuweisung.Auftrag.Enddatum.Date)
            {
                var tag = TagFinden(tage, datum);

                if (tag == null)
                {
                    tag = new MatchingTag { Datum = datum };
                    tage.Add(tag);
                }

                tag.Zuweisungen.Add(zuweisung);
                datum = datum.AddDays(1);
            }
        }

        for (var i = 0; i < tage.Count - 1; i++)
        {
            for (var j = 0; j < tage.Count - i - 1; j++)
            {
                if (tage[j].Datum > tage[j + 1].Datum)
                {
                    var zwischenspeicher = tage[j];
                    tage[j] = tage[j + 1];
                    tage[j + 1] = zwischenspeicher;
                }
            }
        }

        return tage;
    }

    public (int Auftraege, int Mitarbeiter, int Unbesetzt) ZusammenfassungErstellen(
        MatchingVorschlag vorschlag)
    {
        var mitarbeiterIds = new HashSet<int>();
        var unbesetzt = 0;

        foreach (var zuweisung in vorschlag.Zuweisungen)
        {
            if (zuweisung.Mitarbeiter == null)
            {
                unbesetzt++;
            }
            else
            {
                mitarbeiterIds.Add(zuweisung.Mitarbeiter.Id);
            }
        }

        return (vorschlag.Zuweisungen.Count, mitarbeiterIds.Count, unbesetzt);
    }

    private MatchingVorschlag ErstelleVorschlag(
        int nummer,
        string name,
        List<Auftrag> auftraege,
        List<Mitarbeiter> mitarbeiter,
        List<Abwesenheit> abwesenheiten,
        List<Auftrag> bestehendeAuftraege,
        DateTime startdatum,
        DateTime enddatum)
    {
        var vorschlag = new MatchingVorschlag
        {
            Nummer = nummer,
            Name = name
        };

        foreach (var auftrag in auftraege)
        {
            var kandidaten = PassendeKandidaten(
                auftrag,
                mitarbeiter,
                abwesenheiten,
                bestehendeAuftraege,
                vorschlag.Zuweisungen,
                startdatum,
                enddatum,
                nummer);

            if (kandidaten.Count == 0)
            {
                vorschlag.Zuweisungen.Add(new MatchingZuweisung { Auftrag = auftrag });
                continue;
            }

            var auswahl = kandidaten[0];

            if (nummer == 2
                && kandidaten.Count > 1
                && GleicheQualifikationsbewertung(kandidaten[0], kandidaten[1])
                && kandidaten[0].BestehendeEinsaetze == kandidaten[1].BestehendeEinsaetze)
            {
                auswahl = kandidaten[1];
            }
            else if (nummer == 3 && kandidaten.Count > 1)
            {
                auswahl = kandidaten[1];
            }

            vorschlag.Zuweisungen.Add(auswahl);
        }

        return vorschlag;
    }

    private List<MatchingZuweisung> PassendeKandidaten(
        Auftrag auftrag,
        List<Mitarbeiter> mitarbeiter,
        List<Abwesenheit> abwesenheiten,
        List<Auftrag> bestehendeAuftraege,
        List<MatchingZuweisung> vorgeschlageneZuweisungen,
        DateTime startdatum,
        DateTime enddatum,
        int vorschlagsnummer)
    {
        var kandidaten = new List<MatchingZuweisung>();

        foreach (var person in mitarbeiter)
        {
            if (!IstVerfuegbar(person, auftrag, abwesenheiten,
                bestehendeAuftraege, vorgeschlageneZuweisungen))
            {
                continue;
            }

            var passendeQualifikationen = AnzahlPassenderQualifikationen(auftrag, person);

            if (auftrag.Qualifikationen.Count > 0 && passendeQualifikationen == 0)
            {
                continue;
            }

            kandidaten.Add(new MatchingZuweisung
            {
                Auftrag = auftrag,
                Mitarbeiter = person,
                PassendeQualifikationen = passendeQualifikationen,
                BenoetigteQualifikationen = auftrag.Qualifikationen.Count,
                BestehendeEinsaetze = AnzahlEinsaetze(person, bestehendeAuftraege,
                    vorgeschlageneZuweisungen, startdatum, enddatum)
            });
        }

        for (var i = 0; i < kandidaten.Count - 1; i++)
        {
            for (var j = 0; j < kandidaten.Count - i - 1; j++)
            {
                if (SollVorherStehen(kandidaten[j + 1], kandidaten[j], vorschlagsnummer))
                {
                    var zwischenspeicher = kandidaten[j];
                    kandidaten[j] = kandidaten[j + 1];
                    kandidaten[j + 1] = zwischenspeicher;
                }
            }
        }

        return kandidaten;
    }

    private bool IstVerfuegbar(
        Mitarbeiter mitarbeiter,
        Auftrag auftrag,
        List<Abwesenheit> abwesenheiten,
        List<Auftrag> bestehendeAuftraege,
        List<MatchingZuweisung> vorgeschlageneZuweisungen)
    {
        if (!mitarbeiter.Verfuegbar)
        {
            return false;
        }

        foreach (var abwesenheit in abwesenheiten)
        {
            if (abwesenheit.MitarbeiterId == mitarbeiter.Id
                && abwesenheit.Status == "Genehmigt"
                && Ueberlappt(auftrag.Startdatum, auftrag.Enddatum,
                    abwesenheit.Von, abwesenheit.Bis))
            {
                return false;
            }
        }

        foreach (var bestehenderAuftrag in bestehendeAuftraege)
        {
            if (bestehenderAuftrag.Id == auftrag.Id
                || !Ueberlappt(auftrag.Startdatum, auftrag.Enddatum,
                    bestehenderAuftrag.Startdatum, bestehenderAuftrag.Enddatum))
            {
                continue;
            }

            foreach (var zugewiesenerMitarbeiter in bestehenderAuftrag.Mitarbeiter)
            {
                if (zugewiesenerMitarbeiter.Id == mitarbeiter.Id)
                {
                    return false;
                }
            }
        }

        foreach (var zuweisung in vorgeschlageneZuweisungen)
        {
            if (zuweisung.Mitarbeiter?.Id == mitarbeiter.Id
                && Ueberlappt(auftrag.Startdatum, auftrag.Enddatum,
                    zuweisung.Auftrag.Startdatum, zuweisung.Auftrag.Enddatum))
            {
                return false;
            }
        }

        return true;
    }

    private int AnzahlPassenderQualifikationen(Auftrag auftrag, Mitarbeiter mitarbeiter)
    {
        var anzahl = 0;

        foreach (var benoetigteQualifikation in auftrag.Qualifikationen)
        {
            foreach (var vorhandeneQualifikation in mitarbeiter.Qualifikationen)
            {
                if (vorhandeneQualifikation.Id == benoetigteQualifikation.Id)
                {
                    anzahl++;
                    break;
                }
            }
        }

        return anzahl;
    }

    private int AnzahlEinsaetze(
        Mitarbeiter mitarbeiter,
        List<Auftrag> bestehendeAuftraege,
        List<MatchingZuweisung> vorgeschlageneZuweisungen,
        DateTime startdatum,
        DateTime enddatum)
    {
        var anzahl = 0;

        foreach (var auftrag in bestehendeAuftraege)
        {
            if (!Ueberlappt(startdatum, enddatum, auftrag.Startdatum, auftrag.Enddatum))
            {
                continue;
            }

            foreach (var person in auftrag.Mitarbeiter)
            {
                if (person.Id == mitarbeiter.Id)
                {
                    anzahl++;
                    break;
                }
            }
        }

        foreach (var zuweisung in vorgeschlageneZuweisungen)
        {
            if (zuweisung.Mitarbeiter?.Id == mitarbeiter.Id)
            {
                anzahl++;
            }
        }

        return anzahl;
    }

    private bool SollVorherStehen(
        MatchingZuweisung erster,
        MatchingZuweisung zweiter,
        int vorschlagsnummer)
    {
        var ersterVollstaendig = erster.BenoetigteQualifikationen == 0
            || erster.PassendeQualifikationen == erster.BenoetigteQualifikationen;
        var zweiterVollstaendig = zweiter.BenoetigteQualifikationen == 0
            || zweiter.PassendeQualifikationen == zweiter.BenoetigteQualifikationen;

        if (ersterVollstaendig != zweiterVollstaendig)
        {
            return ersterVollstaendig;
        }

        if (vorschlagsnummer == 2
            && erster.BestehendeEinsaetze != zweiter.BestehendeEinsaetze)
        {
            return erster.BestehendeEinsaetze < zweiter.BestehendeEinsaetze;
        }

        if (erster.PassendeQualifikationen != zweiter.PassendeQualifikationen)
        {
            return erster.PassendeQualifikationen > zweiter.PassendeQualifikationen;
        }

        if (erster.BestehendeEinsaetze != zweiter.BestehendeEinsaetze)
        {
            return erster.BestehendeEinsaetze < zweiter.BestehendeEinsaetze;
        }

        return erster.Mitarbeiter!.Id < zweiter.Mitarbeiter!.Id;
    }

    private bool GleicheQualifikationsbewertung(
        MatchingZuweisung erster,
        MatchingZuweisung zweiter)
    {
        return erster.PassendeQualifikationen == zweiter.PassendeQualifikationen
            && erster.BenoetigteQualifikationen == zweiter.BenoetigteQualifikationen;
    }

    private bool Ueberlappt(
        DateTime startA,
        DateTime endeA,
        DateTime startB,
        DateTime endeB)
    {
        return startA.Date <= endeB.Date && startB.Date <= endeA.Date;
    }

    private Auftrag? AuftragFinden(List<Auftrag> auftraege, int id)
    {
        foreach (var auftrag in auftraege)
        {
            if (auftrag.Id == id)
            {
                return auftrag;
            }
        }

        return null;
    }

    private Mitarbeiter? MitarbeiterFinden(List<Mitarbeiter> mitarbeiter, int id)
    {
        foreach (var person in mitarbeiter)
        {
            if (person.Id == id)
            {
                return person;
            }
        }

        return null;
    }

    private MatchingTag? TagFinden(List<MatchingTag> tage, DateTime datum)
    {
        foreach (var tag in tage)
        {
            if (tag.Datum.Date == datum.Date)
            {
                return tag;
            }
        }

        return null;
    }
}
