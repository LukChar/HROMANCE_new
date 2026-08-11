using HRomance.Data;
using HRomance.Models;
using HRomance.Services;
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

    [Theory]
    [InlineData("fritz.schreiner@hromance.test", "P001", "Fritz", "Schreiner")]
    [InlineData("hans.elektriker@hromance.test", "P002", "Hans", "Berger")]
    [InlineData("max.muster@hromance.test", "P003", "Max", "Leitner")]
    [InlineData("anna.gruber@hromance.test", "P004", "Anna", "Gruber")]
    [InlineData("lisa.moser@hromance.test", "P005", "Lisa", "Moser")]
    [InlineData("admin@hromance.test", "P006", "Martin", "Admin")]
    [InlineData("disponent@hromance.test", "P007", "Daniel", "Disponent")]
    public async Task DemoBenutzerIstRichtigemMitarbeiterZugeordnet(
        string email,
        string personalnummer,
        string vorname,
        string nachname)
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();
        await testdatenbank.SeedAusfuehren();

        var user = await testdatenbank.Context.Users
            .Include(u => u.Mitarbeiter)
            .FirstAsync(u => u.Email == email);

        Assert.NotNull(user.Mitarbeiter);
        Assert.Equal(personalnummer, user.Mitarbeiter.Personalnummer);
        Assert.Equal(vorname, user.Mitarbeiter.Vorname);
        Assert.Equal(nachname, user.Mitarbeiter.Nachname);
    }

    [Fact]
    public async Task SeedKorrigiertBestehendenFalschenDemoMitarbeiter()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();
        testdatenbank.Context.Mitarbeiter.Add(new Mitarbeiter
        {
            Personalnummer = "P001",
            Vorname = "Max",
            Nachname = "Muster2"
        });
        await testdatenbank.Context.SaveChangesAsync();

        await testdatenbank.SeedAusfuehren();

        var fritz = await testdatenbank.Context.Mitarbeiter
            .FirstAsync(m => m.Personalnummer == "P001");
        Assert.Equal("Fritz", fritz.Vorname);
        Assert.Equal("Schreiner", fritz.Nachname);
        Assert.NotEqual("Max Muster2", fritz.Vorname + " " + fritz.Nachname);
    }

    [Fact]
    public async Task FritzLaedtNurSeinenZugewiesenenAuftrag()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();
        await testdatenbank.SeedAusfuehren();

        var fritz = await testdatenbank.Context.Mitarbeiter
            .FirstAsync(m => m.Personalnummer == "P001");
        var auftragService = new AuftragService(testdatenbank.Context);

        var auftraege = await auftragService.GetByMitarbeiterAsync(fritz.Id);

        var auftrag = Assert.Single(auftraege);
        Assert.Equal("Möbelmontage Empfang", auftrag.Titel);
    }

    [Fact]
    public async Task MehrfachesSeedErzeugtKeineDoppeltenMitarbeiter()
    {
        await using var testdatenbank = await Testdatenbank.Erstellen();

        await testdatenbank.SeedAusfuehren();
        await testdatenbank.SeedAusfuehren();

        Assert.Equal(7, await testdatenbank.Context.Mitarbeiter.CountAsync());
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
