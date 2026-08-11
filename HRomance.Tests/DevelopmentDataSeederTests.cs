using HRomance.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRomance.Tests;

public class DevelopmentDataSeederTests
{
    [Fact]
    public async Task RollenWerdenNichtDoppeltErzeugt()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();

        await testdatenbank.SeedAusfuehren();
        await testdatenbank.SeedAusfuehren();

        Assert.Equal(2, await testdatenbank.Context.Roles.CountAsync());
    }

    [Fact]
    public async Task AdminTestbenutzerHatAdminRolle()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();
        await testdatenbank.SeedAusfuehren();

        var admin = await testdatenbank.UserManager.FindByEmailAsync("admin@hromance.test");

        Assert.NotNull(admin);
        Assert.True(await testdatenbank.UserManager.IsInRoleAsync(admin, "Admin"));
        Assert.True(await testdatenbank.UserManager.CheckPasswordAsync(admin, DevelopmentDataSeeder.DemoPasswort));
    }

    [Fact]
    public async Task DisponentTestbenutzerHatDisponentRolle()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();
        await testdatenbank.SeedAusfuehren();

        var disponent = await testdatenbank.UserManager.FindByEmailAsync("disponent@hromance.test");

        Assert.NotNull(disponent);
        Assert.True(await testdatenbank.UserManager.IsInRoleAsync(disponent, "Disponent"));
        Assert.True(await testdatenbank.UserManager.CheckPasswordAsync(disponent, DevelopmentDataSeeder.DemoPasswort));
    }

    [Fact]
    public async Task MitarbeiterTestbenutzerHatKeineManagerrolle()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();
        await testdatenbank.SeedAusfuehren();

        var user = await testdatenbank.UserManager.FindByEmailAsync("fritz.schreiner@hromance.test");

        Assert.NotNull(user);
        Assert.False(await testdatenbank.UserManager.IsInRoleAsync(user, "Admin"));
        Assert.False(await testdatenbank.UserManager.IsInRoleAsync(user, "Disponent"));
        Assert.True(await testdatenbank.UserManager.CheckPasswordAsync(user, DevelopmentDataSeeder.DemoPasswort));
    }

    [Fact]
    public async Task MitarbeiterTestbenutzerIstMitarbeiterZugeordnet()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();
        await testdatenbank.SeedAusfuehren();

        var user = await testdatenbank.Context.Users
            .Include(u => u.Mitarbeiter)
            .FirstAsync(u => u.Email == "fritz.schreiner@hromance.test");

        Assert.NotNull(user.Mitarbeiter);
        Assert.Equal("P001", user.Mitarbeiter.Personalnummer);
    }

    [Fact]
    public async Task MehrfachesSeedErzeugtKeineDoppeltenMitarbeiter()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();

        await testdatenbank.SeedAusfuehren();
        await testdatenbank.SeedAusfuehren();

        Assert.Equal(5, await testdatenbank.Context.Mitarbeiter.CountAsync());
    }

    [Fact]
    public async Task MehrfachesSeedErzeugtKeineDoppeltenKunden()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();

        await testdatenbank.SeedAusfuehren();
        await testdatenbank.SeedAusfuehren();

        Assert.Equal(5, await testdatenbank.Context.Kunden.CountAsync());
    }

    [Fact]
    public async Task MehrfachesSeedErzeugtKeineDoppeltenAuftraege()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();

        await testdatenbank.SeedAusfuehren();
        await testdatenbank.SeedAusfuehren();

        Assert.Equal(7, await testdatenbank.Context.Auftraege.CountAsync());
    }

    private sealed class Testdatenbank : IAsyncDisposable
    {
        private readonly SqliteConnection verbindung;
        private readonly ServiceProvider serviceProvider;
        private readonly IServiceScope scope;

        public ApplicationDbContext Context { get; }
        public UserManager<ApplicationUser> UserManager { get; }

        private Testdatenbank(
            SqliteConnection verbindung,
            ServiceProvider serviceProvider,
            IServiceScope scope,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            this.verbindung = verbindung;
            this.serviceProvider = serviceProvider;
            this.scope = scope;
            Context = context;
            UserManager = userManager;
        }

        public static async Task<Testdatenbank> Erstellen()
        {
            var verbindung = new SqliteConnection("Data Source=:memory:");
            await verbindung.OpenAsync();

            var dienste = new ServiceCollection();
            dienste.AddLogging();
            dienste.AddDbContext<ApplicationDbContext>(optionen => optionen.UseSqlite(verbindung));
            dienste.AddIdentityCore<ApplicationUser>(optionen =>
            {
                optionen.SignIn.RequireConfirmedAccount = true;
                optionen.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            var serviceProvider = dienste.BuildServiceProvider();
            var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.EnsureCreatedAsync();

            return new Testdatenbank(verbindung, serviceProvider, scope, context, userManager);
        }

        public async Task SeedAusfuehren()
        {
            await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider);
        }

        public async ValueTask DisposeAsync()
        {
            scope.Dispose();
            await serviceProvider.DisposeAsync();
            await verbindung.DisposeAsync();
        }
    }
}
