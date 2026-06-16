using HRomance.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Mitarbeiter> Mitarbeiter { get; set; }

        public DbSet<Kunde> Kunden { get; set; }

        public DbSet<Auftrag> Auftraege { get; set; }
    }
}
