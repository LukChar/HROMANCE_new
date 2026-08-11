using HRomance.Data;
using HRomance.Models;
using HRomance.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRomance.Tests;

public class AuftragServiceTests
{
    [Fact]
    public async Task MitarbeiterOhneKonfliktIstVerfuegbar()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        var auftrag = NeuerAuftrag(12, 15);

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, auftrag);

        Assert.Equal("Verfügbar", ergebnis);
    }

    [Fact]
    public async Task ManuellDeaktivierterMitarbeiterIstNichtVerfuegbar()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(false);

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 15));

        Assert.Equal("Manuell nicht verfügbar", ergebnis);
    }

    [Fact]
    public async Task GenehmigterUrlaubBlockiert()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AbwesenheitHinzufuegen(mitarbeiter.Id, 14, 18, "Urlaub", "Genehmigt");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 15));

        Assert.Equal("Nicht verfügbar - Urlaub", ergebnis);
    }

    [Fact]
    public async Task OffeneAbwesenheitBlockiert()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AbwesenheitHinzufuegen(mitarbeiter.Id, 14, 18, "Krankenstand", "Offen");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 15));

        Assert.Equal("Nicht verfügbar - Krankenstand", ergebnis);
    }

    [Fact]
    public async Task AbgelehnteAbwesenheitBlockiertNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AbwesenheitHinzufuegen(mitarbeiter.Id, 14, 18, "Urlaub", "Abgelehnt");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 15));

        Assert.Equal("Verfügbar", ergebnis);
    }

    [Fact]
    public async Task AbwesenheitVorAuftragBlockiertNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AbwesenheitHinzufuegen(mitarbeiter.Id, 8, 11, "Urlaub", "Genehmigt");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 15));

        Assert.Equal("Verfügbar", ergebnis);
    }

    [Fact]
    public async Task AbwesenheitNachAuftragBlockiertNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AbwesenheitHinzufuegen(mitarbeiter.Id, 16, 18, "Urlaub", "Genehmigt");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 15));

        Assert.Equal("Verfügbar", ergebnis);
    }

    [Fact]
    public async Task UeberschneidenderAuftragErzeugtKonflikt()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AuftragHinzufuegen(mitarbeiter, 13, 16, "Montage Halle A");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 15));

        Assert.Contains("Auftrag: Montage Halle A", ergebnis);
    }

    [Fact]
    public async Task AuftragOhneUeberschneidungErzeugtKeinenKonflikt()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AuftragHinzufuegen(mitarbeiter, 16, 18, "Späterer Auftrag");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 15));

        Assert.Equal("Verfügbar", ergebnis);
    }

    [Fact]
    public async Task AktuellerAuftragErzeugtKeinenSelbstKonflikt()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        var auftrag = await testdatenbank.AuftragHinzufuegen(mitarbeiter, 12, 15, "Aktueller Auftrag");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, auftrag);

        Assert.Equal("Verfügbar", ergebnis);
    }

    [Fact]
    public void AlleQualifikationenWerdenGezaehlt()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = AuftragMitQualifikationen(1, 2, 3);
        var mitarbeiter = MitarbeiterMitQualifikationen(1, 2, 3);

        var anzahl = testdatenbank.Service.AnzahlPassenderQualifikationen(auftrag, mitarbeiter);

        Assert.Equal(3, anzahl);
    }

    [Fact]
    public void TeilweisePassendeQualifikationenWerdenGezaehlt()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = AuftragMitQualifikationen(1, 2, 3);
        var mitarbeiter = MitarbeiterMitQualifikationen(1, 3, 4);

        var anzahl = testdatenbank.Service.AnzahlPassenderQualifikationen(auftrag, mitarbeiter);

        Assert.Equal(2, anzahl);
    }

    [Fact]
    public void NichtPassendeQualifikationenErgebenNull()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = AuftragMitQualifikationen(1, 2, 3);
        var mitarbeiter = MitarbeiterMitQualifikationen(4, 5);

        var anzahl = testdatenbank.Service.AnzahlPassenderQualifikationen(auftrag, mitarbeiter);

        Assert.Equal(0, anzahl);
    }

    [Fact]
    public void VerfuegbareMitarbeiterStehenVorNichtVerfuegbaren()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = AuftragMitQualifikationen(1);
        var nichtVerfuegbar = MitarbeiterMitQualifikationen(1);
        var verfuegbar = MitarbeiterMitQualifikationen();
        nichtVerfuegbar.Id = 1;
        verfuegbar.Id = 2;
        var status = new Dictionary<int, string> { [1] = "Nicht verfügbar - Urlaub", [2] = "Verfügbar" };

        var ergebnis = testdatenbank.Service.MitarbeiterSortieren(auftrag, [nichtVerfuegbar, verfuegbar], status);

        Assert.Equal(2, ergebnis[0].Id);
    }

    [Fact]
    public void VerfuegbareMitarbeiterWerdenNachQualifikationenSortiert()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = AuftragMitQualifikationen(1, 2, 3);
        var eine = MitarbeiterMitQualifikationen(1);
        var drei = MitarbeiterMitQualifikationen(1, 2, 3);
        eine.Id = 1;
        drei.Id = 2;
        var status = new Dictionary<int, string> { [1] = "Verfügbar", [2] = "Verfügbar" };

        var ergebnis = testdatenbank.Service.MitarbeiterSortieren(auftrag, [eine, drei], status);

        Assert.Equal(2, ergebnis[0].Id);
    }

    [Fact]
    public void NichtVerfuegbareMitarbeiterWerdenNachQualifikationenSortiert()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = AuftragMitQualifikationen(1, 2, 3);
        var eine = MitarbeiterMitQualifikationen(1);
        var drei = MitarbeiterMitQualifikationen(1, 2, 3);
        eine.Id = 1;
        drei.Id = 2;
        var status = new Dictionary<int, string> { [1] = "Nicht verfügbar", [2] = "Nicht verfügbar - Urlaub" };

        var ergebnis = testdatenbank.Service.MitarbeiterSortieren(auftrag, [eine, drei], status);

        Assert.Equal(2, ergebnis[0].Id);
    }

    [Fact]
    public async Task AuftragAmGemeinsamenGrenztagErzeugtKonflikt()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AuftragHinzufuegen(mitarbeiter, 14, 16, "Grenzauftrag");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(12, 14));

        Assert.Contains("Grenzauftrag", ergebnis);
    }

    [Fact]
    public async Task AbwesenheitAmEinzigenAuftragstagErzeugtKonflikt()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen(true);
        await testdatenbank.AbwesenheitHinzufuegen(mitarbeiter.Id, 14, 14, "Urlaub", "Offen");

        var ergebnis = await testdatenbank.Service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, NeuerAuftrag(14, 14));

        Assert.Equal("Nicht verfügbar - Urlaub", ergebnis);
    }

    private static Auftrag NeuerAuftrag(int starttag, int endtag)
    {
        return new Auftrag
        {
            Titel = "Zu prüfender Auftrag",
            Startdatum = new DateTime(2026, 8, starttag),
            Enddatum = new DateTime(2026, 8, endtag)
        };
    }

    private static Auftrag AuftragMitQualifikationen(params int[] ids)
    {
        var auftrag = NeuerAuftrag(12, 15);

        foreach (var id in ids)
        {
            auftrag.Qualifikationen.Add(new Qualifikation { Id = id, Name = "Qualifikation " + id });
        }

        return auftrag;
    }

    private static Mitarbeiter MitarbeiterMitQualifikationen(params int[] ids)
    {
        var mitarbeiter = new Mitarbeiter { Vorname = "Test", Nachname = "Person" };

        foreach (var id in ids)
        {
            mitarbeiter.Qualifikationen.Add(new Qualifikation { Id = id, Name = "Qualifikation " + id });
        }

        return mitarbeiter;
    }

    private sealed class Testdatenbank : IDisposable
    {
        private readonly SqliteConnection verbindung;
        private readonly ApplicationDbContext context;

        public AuftragService Service { get; }

        public Testdatenbank()
        {
            verbindung = new SqliteConnection("Data Source=:memory:");
            verbindung.Open();

            var optionen = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(verbindung)
                .Options;

            context = new ApplicationDbContext(optionen);
            context.Database.EnsureCreated();
            Service = new AuftragService(context);
        }

        public async Task<Mitarbeiter> MitarbeiterHinzufuegen(bool verfuegbar)
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = Guid.NewGuid().ToString(),
                Vorname = "Max",
                Nachname = "Muster",
                Verfuegbar = verfuegbar
            };

            context.Mitarbeiter.Add(mitarbeiter);
            await context.SaveChangesAsync();
            return mitarbeiter;
        }

        public async Task AbwesenheitHinzufuegen(
            int mitarbeiterId,
            int vontag,
            int bistag,
            string typ,
            string status)
        {
            context.Abwesenheiten.Add(new Abwesenheit
            {
                MitarbeiterId = mitarbeiterId,
                Von = new DateTime(2026, 8, vontag),
                Bis = new DateTime(2026, 8, bistag),
                Typ = typ,
                Status = status
            });

            await context.SaveChangesAsync();
        }

        public async Task<Auftrag> AuftragHinzufuegen(
            Mitarbeiter mitarbeiter,
            int starttag,
            int endtag,
            string titel)
        {
            var kunde = new Kunde { Firmenname = "Testkunde " + Guid.NewGuid() };
            var auftrag = new Auftrag
            {
                Titel = titel,
                Startdatum = new DateTime(2026, 8, starttag),
                Enddatum = new DateTime(2026, 8, endtag),
                Kunde = kunde
            };

            auftrag.Mitarbeiter.Add(mitarbeiter);
            context.Auftraege.Add(auftrag);
            await context.SaveChangesAsync();
            return auftrag;
        }

        public void Dispose()
        {
            context.Dispose();
            verbindung.Dispose();
        }
    }
}
