using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Services
{
    public class KundeService
    {
        private readonly ApplicationDbContext _context;

        public KundeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Kunde>> GetAllAsync()
        {
            return await _context.Kunden.ToListAsync();
        }

        public async Task<Kunde?> GetByIdAsync(int id)
        {
            return await _context.Kunden.FindAsync(id);
        }

        public async Task AddAsync(Kunde kunde)
        {
            _context.Kunden.Add(kunde);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Kunde kunde)
        {
            var vorhandenerKunde = await _context.Kunden.FindAsync(kunde.Id);

            if (vorhandenerKunde != null)
            {
                vorhandenerKunde.Firmenname = kunde.Firmenname;
                vorhandenerKunde.Ansprechpartner = kunde.Ansprechpartner;
                vorhandenerKunde.Email = kunde.Email;
                vorhandenerKunde.Telefon = kunde.Telefon;

                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var kunde = await _context.Kunden.FindAsync(id);

            if (kunde != null)
            {
                _context.Kunden.Remove(kunde);
                await _context.SaveChangesAsync();
            }
        }
    }
}
