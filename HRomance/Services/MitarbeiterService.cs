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
            return await _context.Mitarbeiter.ToListAsync();
        }

        // Mitarbeiter anhand der ID laden
        public async Task<Mitarbeiter?> GetByIdAsync(int id)
        {
            return await _context.Mitarbeiter.FindAsync(id);
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
            _context.Mitarbeiter.Update(mitarbeiter);
            await _context.SaveChangesAsync();
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