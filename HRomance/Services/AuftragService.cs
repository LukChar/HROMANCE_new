using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Services
{
    public class AuftragService
    {
        private readonly ApplicationDbContext _context;

        public AuftragService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Auftrag>> GetAllAsync()
        {
            return await _context.Auftraege
                .Include(a => a.Kunde)
                .Include(a => a.Qualifikationen)
                .Include(a => a.Mitarbeiter)
                    .ThenInclude(m => m.Qualifikationen)
                .ToListAsync();
        }

        public async Task<Auftrag?> GetByIdAsync(int id)
        {
            return await _context.Auftraege
                .Include(a => a.Kunde)
                .Include(a => a.Qualifikationen)
                .Include(a => a.Mitarbeiter)
                    .ThenInclude(m => m.Qualifikationen)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Auftrag>> GetByMitarbeiterAsync(int mitarbeiterId)
        {
            return await _context.Auftraege
                .Include(a => a.Kunde)
                .Include(a => a.Qualifikationen)
                .Include(a => a.Mitarbeiter)
                    .ThenInclude(m => m.Qualifikationen)
                .Where(a => a.Mitarbeiter.Any(m => m.Id == mitarbeiterId))
                .ToListAsync();
        }

        public async Task AddAsync(Auftrag auftrag)
        {
            auftrag.Besetzt = auftrag.Mitarbeiter.Count > 0;
            _context.Auftraege.Add(auftrag);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Auftrag auftrag)
        {
            var vorhandenerAuftrag = await _context.Auftraege
                .Include(a => a.Qualifikationen)
                .Include(a => a.Mitarbeiter)
                .FirstOrDefaultAsync(a => a.Id == auftrag.Id);

            if (vorhandenerAuftrag != null)
            {
                vorhandenerAuftrag.Titel = auftrag.Titel;
                vorhandenerAuftrag.Beschreibung = auftrag.Beschreibung;
                vorhandenerAuftrag.Einsatzort = auftrag.Einsatzort;
                vorhandenerAuftrag.BenoetigteQualifikation = auftrag.BenoetigteQualifikation;
                vorhandenerAuftrag.Startdatum = auftrag.Startdatum;
                vorhandenerAuftrag.Enddatum = auftrag.Enddatum;
                vorhandenerAuftrag.KundeId = auftrag.KundeId;
                vorhandenerAuftrag.Besetzt = vorhandenerAuftrag.Mitarbeiter.Count > 0;

                vorhandenerAuftrag.Qualifikationen.Clear();

                foreach (var qualifikation in auftrag.Qualifikationen)
                {
                    vorhandenerAuftrag.Qualifikationen.Add(qualifikation);
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var auftrag = await _context.Auftraege.FindAsync(id);

            if (auftrag != null)
            {
                _context.Auftraege.Remove(auftrag);
                await _context.SaveChangesAsync();
            }
        }

        public async Task MitarbeiterZuweisenAsync(int auftragId, List<int> mitarbeiterIds)
        {
            var auftrag = await _context.Auftraege
                .Include(a => a.Mitarbeiter)
                .FirstOrDefaultAsync(a => a.Id == auftragId);

            if (auftrag == null)
            {
                return;
            }

            auftrag.Mitarbeiter.Clear();

            foreach (var mitarbeiterId in mitarbeiterIds)
            {
                var mitarbeiter = await _context.Mitarbeiter.FindAsync(mitarbeiterId);

                if (mitarbeiter != null)
                {
                    auftrag.Mitarbeiter.Add(mitarbeiter);
                }
            }

            auftrag.Besetzt = auftrag.Mitarbeiter.Count > 0;
            await _context.SaveChangesAsync();
        }

        public async Task MitarbeiterEntfernenAsync(int auftragId, int mitarbeiterId)
        {
            var auftrag = await _context.Auftraege
                .Include(a => a.Mitarbeiter)
                .FirstOrDefaultAsync(a => a.Id == auftragId);

            if (auftrag == null)
            {
                return;
            }

            Mitarbeiter? zuEntfernenderMitarbeiter = null;

            foreach (var mitarbeiter in auftrag.Mitarbeiter)
            {
                if (mitarbeiter.Id == mitarbeiterId)
                {
                    zuEntfernenderMitarbeiter = mitarbeiter;
                    break;
                }
            }

            if (zuEntfernenderMitarbeiter != null)
            {
                auftrag.Mitarbeiter.Remove(zuEntfernenderMitarbeiter);
            }

            auftrag.Besetzt = auftrag.Mitarbeiter.Count > 0;
            await _context.SaveChangesAsync();
        }

        public async Task<string> MitarbeiterVerfuegbarkeitPruefenAsync(int mitarbeiterId, Auftrag auftrag)
        {
            var mitarbeiter = await _context.Mitarbeiter.FindAsync(mitarbeiterId);

            if (mitarbeiter == null)
            {
                return "Nicht verfügbar";
            }

            var abwesenheiten = await _context.Abwesenheiten
                .Where(a => a.MitarbeiterId == mitarbeiterId)
                .ToListAsync();

            foreach (var abwesenheit in abwesenheiten)
            {
                if (abwesenheit.Status != "Abgelehnt"
                    && abwesenheit.Von.Date <= auftrag.Enddatum.Date
                    && abwesenheit.Bis.Date >= auftrag.Startdatum.Date)
                {
                    return "Nicht verfügbar - " + abwesenheit.Typ;
                }
            }

            var andereAuftraege = await _context.Auftraege
                .Include(a => a.Mitarbeiter)
                .Where(a => a.Id != auftrag.Id)
                .ToListAsync();

            foreach (var andererAuftrag in andereAuftraege)
            {
                var istZugewiesen = false;

                foreach (var zugewiesenerMitarbeiter in andererAuftrag.Mitarbeiter)
                {
                    if (zugewiesenerMitarbeiter.Id == mitarbeiterId)
                    {
                        istZugewiesen = true;
                        break;
                    }
                }

                if (istZugewiesen
                    && andererAuftrag.Startdatum.Date <= auftrag.Enddatum.Date
                    && andererAuftrag.Enddatum.Date >= auftrag.Startdatum.Date)
                {
                    return "Nicht verfügbar - Auftrag: "
                        + andererAuftrag.Titel
                        + " ("
                        + andererAuftrag.Startdatum.ToString("dd.MM.yyyy")
                        + " - "
                        + andererAuftrag.Enddatum.ToString("dd.MM.yyyy")
                        + ")";
                }
            }

            if (!mitarbeiter.Verfuegbar)
            {
                return "Manuell nicht verfügbar";
            }

            return "Verfügbar";
        }

        public int AnzahlPassenderQualifikationen(Auftrag auftrag, Mitarbeiter mitarbeiter)
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

        public bool PasstZuMitarbeiter(Auftrag auftrag, int mitarbeiterId)
        {
            if (mitarbeiterId == 0)
            {
                return true;
            }

            foreach (var mitarbeiter in auftrag.Mitarbeiter)
            {
                if (mitarbeiter.Id == mitarbeiterId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IstBesetzt(Auftrag auftrag)
        {
            return auftrag.Mitarbeiter.Count > 0;
        }

        public bool IstEinsatzAmTag(Auftrag auftrag, DateTime datum)
        {
            return auftrag.Startdatum.Date <= datum.Date
                && auftrag.Enddatum.Date >= datum.Date;
        }

        public Dictionary<int, int> KalenderSpurenBestimmen(List<Auftrag> auftraege)
        {
            var sortierteAuftraege = auftraege
                .OrderBy(auftrag => auftrag.Startdatum)
                .ThenBy(auftrag => auftrag.Enddatum)
                .ThenBy(auftrag => auftrag.Id)
                .ToList();
            var spuren = new List<List<Auftrag>>();
            var ergebnis = new Dictionary<int, int>();

            foreach (var auftrag in sortierteAuftraege)
            {
                var spur = 0;

                while (KalenderspurIstBelegt(spuren, spur, auftrag))
                {
                    spur++;
                }

                if (spur == spuren.Count)
                {
                    spuren.Add(new List<Auftrag>());
                }

                spuren[spur].Add(auftrag);
                ergebnis[auftrag.Id] = spur;
            }

            return ergebnis;
        }

        public bool IstKalenderSegmentStart(Auftrag auftrag, DateTime datum)
        {
            return datum.Date == auftrag.Startdatum.Date
                || datum.DayOfWeek == DayOfWeek.Monday
                || datum.Day == 1;
        }

        public string KalenderSegmentKlasse(Auftrag auftrag, DateTime datum)
        {
            var beginnt = IstKalenderSegmentStart(auftrag, datum);
            var letzterTagImMonat = DateTime.DaysInMonth(datum.Year, datum.Month);
            var endet = datum.Date == auftrag.Enddatum.Date
                || datum.DayOfWeek == DayOfWeek.Sunday
                || datum.Day == letzterTagImMonat;

            if (beginnt && endet)
            {
                return "single";
            }

            if (beginnt)
            {
                return "start";
            }

            if (endet)
            {
                return "end";
            }

            return "middle";
        }

        public int SichtbareKalenderSegmenttage(Auftrag auftrag, DateTime datum)
        {
            var letzterTag = auftrag.Enddatum.Date;
            var tageBisSonntag = (7 - (int)datum.DayOfWeek) % 7;
            var sonntag = datum.Date.AddDays(tageBisSonntag);
            var monatsende = new DateTime(datum.Year, datum.Month,
                DateTime.DaysInMonth(datum.Year, datum.Month));

            if (sonntag < letzterTag)
            {
                letzterTag = sonntag;
            }

            if (monatsende < letzterTag)
            {
                letzterTag = monatsende;
            }

            return (letzterTag - datum.Date).Days + 1;
        }

        private bool KalenderspurIstBelegt(
            List<List<Auftrag>> spuren,
            int spur,
            Auftrag auftrag)
        {
            if (spur >= spuren.Count)
            {
                return false;
            }

            foreach (var vorhandenerAuftrag in spuren[spur])
            {
                var ueberlappt = auftrag.Startdatum.Date <= vorhandenerAuftrag.Enddatum.Date
                    && vorhandenerAuftrag.Startdatum.Date <= auftrag.Enddatum.Date;

                if (ueberlappt)
                {
                    return true;
                }
            }

            return false;
        }

        public List<DateTime> NaechsteArbeitstage(DateTime startdatum, int anzahl)
        {
            var arbeitstage = new List<DateTime>();
            var tag = startdatum.Date;

            while (arbeitstage.Count < anzahl)
            {
                if (tag.DayOfWeek != DayOfWeek.Saturday
                    && tag.DayOfWeek != DayOfWeek.Sunday)
                {
                    arbeitstage.Add(tag);
                }

                tag = tag.AddDays(1);
            }

            return arbeitstage;
        }

        public bool IstInNaechstenFuenfArbeitstagen(Auftrag auftrag, DateTime heute)
        {
            var arbeitstage = NaechsteArbeitstage(heute, 5);

            foreach (var tag in arbeitstage)
            {
                if (IstEinsatzAmTag(auftrag, tag))
                {
                    return true;
                }
            }

            return false;
        }

        public List<Mitarbeiter> MitarbeiterSortieren(
            Auftrag auftrag,
            List<Mitarbeiter> mitarbeiter,
            Dictionary<int, string> verfuegbarkeiten)
        {
            var sortierteMitarbeiter = new List<Mitarbeiter>(mitarbeiter);

            for (var i = 0; i < sortierteMitarbeiter.Count - 1; i++)
            {
                for (var j = 0; j < sortierteMitarbeiter.Count - i - 1; j++)
                {
                    var erster = sortierteMitarbeiter[j];
                    var zweiter = sortierteMitarbeiter[j + 1];
                    var ersterVerfuegbar = verfuegbarkeiten[erster.Id] == "Verfügbar";
                    var zweiterVerfuegbar = verfuegbarkeiten[zweiter.Id] == "Verfügbar";
                    var tauschen = false;

                    if (ersterVerfuegbar != zweiterVerfuegbar)
                    {
                        tauschen = !ersterVerfuegbar;
                    }
                    else
                    {
                        var ersteAnzahl = AnzahlPassenderQualifikationen(auftrag, erster);
                        var zweiteAnzahl = AnzahlPassenderQualifikationen(auftrag, zweiter);

                        if (ersteAnzahl != zweiteAnzahl)
                        {
                            tauschen = ersteAnzahl < zweiteAnzahl;
                        }
                        else
                        {
                            var ersterName = erster.Nachname + " " + erster.Vorname;
                            var zweiterName = zweiter.Nachname + " " + zweiter.Vorname;
                            tauschen = string.Compare(
                                ersterName,
                                zweiterName,
                                StringComparison.OrdinalIgnoreCase) > 0;
                        }
                    }

                    if (tauschen)
                    {
                        sortierteMitarbeiter[j] = zweiter;
                        sortierteMitarbeiter[j + 1] = erster;
                    }
                }
            }

            return sortierteMitarbeiter;
        }
    }
}
