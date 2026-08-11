using HRomance.Data;
using HRomance.Models;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Services
{
    public class QualifikationService
    {
        private readonly ApplicationDbContext _context;

        public QualifikationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Qualifikation>> GetAllAsync()
        {
            return await _context.Qualifikationen.ToListAsync();
        }

        public async Task<Qualifikation?> GetByIdAsync(int id)
        {
            return await _context.Qualifikationen.FindAsync(id);
        }

        public async Task AddAsync(Qualifikation qualifikation)
        {
            _context.Qualifikationen.Add(qualifikation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Qualifikation qualifikation)
        {
            var vorhandeneQualifikation =
                await _context.Qualifikationen.FindAsync(qualifikation.Id);

            if (vorhandeneQualifikation != null)
            {
                vorhandeneQualifikation.Name = qualifikation.Name;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var qualifikation = await _context.Qualifikationen.FindAsync(id);

            if (qualifikation != null)
            {
                _context.Qualifikationen.Remove(qualifikation);
                await _context.SaveChangesAsync();
            }
        }
    }
}
