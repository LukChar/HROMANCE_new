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
                .ToListAsync();
        }

        public async Task<Auftrag?> GetByIdAsync(int id)
        {
            return await _context.Auftraege
                .Include(a => a.Kunde)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddAsync(Auftrag auftrag)
        {
            _context.Auftraege.Add(auftrag);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Auftrag auftrag)
        {
            _context.Auftraege.Update(auftrag);
            await _context.SaveChangesAsync();
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
    }
}