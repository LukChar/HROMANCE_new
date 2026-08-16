using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Services
{
    public class MitarbeiterService
    {
        private readonly ApplicationDbContext _context;

        public MitarbeiterService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Alle Mitarbeiter laden
        public async Task<List<Mitarbeiter>> GetAllAsync()
        {
            return await _context.Mitarbeiter
                .Include(m => m.Qualifikationen)
                .ToListAsync();
        }

        // Mitarbeiter anhand der ID laden
        public async Task<Mitarbeiter?> GetByIdAsync(int id)
        {
            return await _context.Mitarbeiter
                .Include(m => m.Qualifikationen)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public bool PasstZurSuche(Mitarbeiter mitarbeiter, string suche)
        {
            if (string.IsNullOrWhiteSpace(suche))
            {
                return true;
            }

            var suchtext = suche.Trim();

            if (mitarbeiter.Vorname.Contains(suchtext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (mitarbeiter.Nachname.Contains(suchtext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var vollstaendigerName = mitarbeiter.Vorname + " " + mitarbeiter.Nachname;

            if (vollstaendigerName.Contains(suchtext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (mitarbeiter.Personalnummer.Contains(suchtext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var qualifikation in mitarbeiter.Qualifikationen)
            {
                if (qualifikation.Name.Contains(suchtext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public Mitarbeiter ErstelleArbeitskopie(Mitarbeiter mitarbeiter)
        {
            return new Mitarbeiter
            {
                Id = mitarbeiter.Id,
                Personalnummer = mitarbeiter.Personalnummer,
                Vorname = mitarbeiter.Vorname,
                Nachname = mitarbeiter.Nachname,
                Email = mitarbeiter.Email,
                Telefon = mitarbeiter.Telefon,
                Qualifikation = mitarbeiter.Qualifikation,
                Verfuegbar = mitarbeiter.Verfuegbar,
                SollStundenProTag = mitarbeiter.SollStundenProTag,
                Wochenarbeitszeit = mitarbeiter.Wochenarbeitszeit
            };
        }

        // Neuen Mitarbeiter hinzufügen
        public async Task AddAsync(Mitarbeiter mitarbeiter)
        {
            _context.Mitarbeiter.Add(mitarbeiter);
            await _context.SaveChangesAsync();
        }

        // Mitarbeiter bearbeiten
        public async Task UpdateAsync(Mitarbeiter mitarbeiter)
        {
            var vorhandenerMitarbeiter = await _context.Mitarbeiter
                .Include(m => m.Qualifikationen)
                .FirstOrDefaultAsync(m => m.Id == mitarbeiter.Id);

            if (vorhandenerMitarbeiter != null)
            {
                vorhandenerMitarbeiter.Personalnummer = mitarbeiter.Personalnummer;
                vorhandenerMitarbeiter.Vorname = mitarbeiter.Vorname;
                vorhandenerMitarbeiter.Nachname = mitarbeiter.Nachname;
                vorhandenerMitarbeiter.Email = mitarbeiter.Email;
                vorhandenerMitarbeiter.Telefon = mitarbeiter.Telefon;
                vorhandenerMitarbeiter.Qualifikation = mitarbeiter.Qualifikation;
                vorhandenerMitarbeiter.Verfuegbar = mitarbeiter.Verfuegbar;
                vorhandenerMitarbeiter.SollStundenProTag = mitarbeiter.SollStundenProTag;
                vorhandenerMitarbeiter.Wochenarbeitszeit = mitarbeiter.Wochenarbeitszeit;

                vorhandenerMitarbeiter.Qualifikationen.Clear();

                foreach (var qualifikation in mitarbeiter.Qualifikationen)
                {
                    vorhandenerMitarbeiter.Qualifikationen.Add(qualifikation);
                }

                await _context.SaveChangesAsync();
            }
        }

        // Mitarbeiter löschen
        public async Task DeleteAsync(int id)
        {
            var mitarbeiter = await _context.Mitarbeiter.FindAsync(id);

            if (mitarbeiter != null)
            {
                _context.Mitarbeiter.Remove(mitarbeiter);
                await _context.SaveChangesAsync();
            }
        }
    }
}
