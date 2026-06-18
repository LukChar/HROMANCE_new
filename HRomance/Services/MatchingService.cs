using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Services
{
    public class MatchingService
    {
        private readonly ApplicationDbContext _context;

        public MatchingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MatchingErgebnis>> GetPassendeMitarbeiterAsync(int auftragId)
        {
            var auftrag = await _context.Auftraege
                .FirstOrDefaultAsync(a => a.Id == auftragId);

            if (auftrag == null)
            {
                return new List<MatchingErgebnis>();
            }

            var mitarbeiterListe = await _context.Mitarbeiter.ToListAsync();

            var ergebnisse = new List<MatchingErgebnis>();

            foreach (var mitarbeiter in mitarbeiterListe)
            {
                int punkte = 0;

                // Verfügbarkeit
                if (mitarbeiter.Verfuegbar)
                    punkte += 50;

                // Qualifikation
                if (mitarbeiter.Qualifikation == auftrag.BenoetigteQualifikation)
                    punkte += 50;

                if (punkte > 0)
                {
                    ergebnisse.Add(new MatchingErgebnis
                    {
                        Mitarbeiter = mitarbeiter,
                        Punkte = punkte,
                        Bewertung = punkte switch
                        {
                            100 => "Sehr gut geeignet",
                            50 => "Geeignet",
                            _ => "Wenig geeignet"
                        }
                    });
                }
            }

            return ergebnisse
                .OrderByDescending(x => x.Punkte)
                .ToList();
        }
    }
}