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

        public async Task<List<Mitarbeiter>> GetPassendeMitarbeiterAsync(int auftragId)
        {
            var auftrag = await _context.Auftraege
                .FirstOrDefaultAsync(a => a.Id == auftragId);

            if (auftrag == null)
            {
                return new List<Mitarbeiter>();
            }

            return await _context.Mitarbeiter
                .Where(m =>
                    m.Verfuegbar &&
                    m.Qualifikation == auftrag.BenoetigteQualifikation)
                .ToListAsync();
        }
    }
}