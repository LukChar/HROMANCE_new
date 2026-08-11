using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HRomance.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var dienste = new ServiceCollection();

        dienste.AddLogging();
        dienste.AddDbContext<ApplicationDbContext>(optionen =>
            optionen.UseSqlite("Data Source=Data/app.db"));

        dienste.AddIdentityCore<ApplicationUser>(optionen =>
        {
            optionen.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        var serviceProvider = dienste.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ApplicationDbContext>();
    }
}
