using HRomance.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        protected override Version SchemaVersion => IdentitySchemaVersions.Version3;

        public DbSet<Mitarbeiter> Mitarbeiter { get; set; }

        public DbSet<Kunde> Kunden { get; set; }

        public DbSet<Auftrag> Auftraege { get; set; }

        public DbSet<Qualifikation> Qualifikationen { get; set; }

        public DbSet<Arbeitszeit> Arbeitszeiten { get; set; }

        public DbSet<Abwesenheit> Abwesenheiten { get; set; }

        public DbSet<Materialeintrag> Materialeintraege { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .HasOne(user => user.Mitarbeiter)
                .WithOne()
                .HasForeignKey<ApplicationUser>(user => user.MitarbeiterId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
