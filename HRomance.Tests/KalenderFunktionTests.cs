using HRomance.Data;
using HRomance.Models;
using HRomance.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRomance.Tests;

public class KalenderFunktionTests
{
    [Fact]
    public void AlleMitarbeiterLaesstDatenVerschiedenerMitarbeiterZu()
    {
        using var testdatenbank = new Testdatenbank();
        var erster = new Arbeitszeit { MitarbeiterId = 1 };
        var zweiter = new Arbeitszeit { MitarbeiterId = 2 };

        Assert.True(testdatenbank.ArbeitszeitService.PasstZuMitarbeiter(erster, 0));
        Assert.True(testdatenbank.ArbeitszeitService.PasstZuMitarbeiter(zweiter, 0));
    }

    [Fact]
    public void MitarbeiterfilterZeigtNurPassendeArbeitszeiten()
    {
        using var testdatenbank = new Testdatenbank();
        var passend = new Arbeitszeit { MitarbeiterId = 1 };
        var unpassend = new Arbeitszeit { MitarbeiterId = 2 };

        Assert.True(testdatenbank.ArbeitszeitService.PasstZuMitarbeiter(passend, 1));
        Assert.False(testdatenbank.ArbeitszeitService.PasstZuMitarbeiter(unpassend, 1));
    }

    [Fact]
    public void MitarbeiterfilterZeigtNurPassendeAbwesenheiten()
    {
        using var testdatenbank = new Testdatenbank();
        var passend = new Abwesenheit { MitarbeiterId = 1 };
        var unpassend = new Abwesenheit { MitarbeiterId = 2 };

        Assert.True(testdatenbank.AbwesenheitService.PasstZuMitarbeiter(passend, 1));
        Assert.False(testdatenbank.AbwesenheitService.PasstZuMitarbeiter(unpassend, 1));
    }

    [Fact]
    public void MitarbeiterfilterZeigtNurZugewieseneAuftraege()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = new Auftrag();
        auftrag.Mitarbeiter.Add(new Mitarbeiter { Id = 1 });

        Assert.True(testdatenbank.AuftragService.PasstZuMitarbeiter(auftrag, 1));
        Assert.False(testdatenbank.AuftragService.PasstZuMitarbeiter(auftrag, 2));
    }

    [Fact]
    public void MitarbeiterOhneEintraegeErzeugtEinfachKeineTreffer()
    {
        using var testdatenbank = new Testdatenbank();

        Assert.False(testdatenbank.ArbeitszeitService.PasstZuMitarbeiter(new Arbeitszeit { MitarbeiterId = 1 }, 99));
        Assert.False(testdatenbank.AbwesenheitService.PasstZuMitarbeiter(new Abwesenheit { MitarbeiterId = 1 }, 99));
        Assert.False(testdatenbank.AuftragService.PasstZuMitarbeiter(new Auftrag(), 99));
    }

    [Fact]
    public void MitarbeitersucheFindetVorname()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = SuchMitarbeiter();

        Assert.True(testdatenbank.MitarbeiterService.PasstZurSuche(mitarbeiter, "fritz"));
    }

    [Fact]
    public void MitarbeitersucheFindetNachname()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = SuchMitarbeiter();

        Assert.True(testdatenbank.MitarbeiterService.PasstZurSuche(mitarbeiter, "MUSTER"));
    }

    [Fact]
    public void MitarbeitersucheFindetPersonalnummer()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = SuchMitarbeiter();

        Assert.True(testdatenbank.MitarbeiterService.PasstZurSuche(mitarbeiter, "P-123"));
    }

    [Fact]
    public void MitarbeitersucheFindetVollstaendigenNamen()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = SuchMitarbeiter();

        Assert.True(testdatenbank.MitarbeiterService.PasstZurSuche(mitarbeiter, "fritz muster"));
    }

    [Fact]
    public void MitarbeitersucheFindetQualifikation()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = SuchMitarbeiter();

        Assert.True(testdatenbank.MitarbeiterService.PasstZurSuche(mitarbeiter, "tischler"));
    }

    [Fact]
    public void MitarbeitersucheOhneTrefferIstFalsch()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = SuchMitarbeiter();

        Assert.False(testdatenbank.MitarbeiterService.PasstZurSuche(mitarbeiter, "Elektriker"));
    }

    [Fact]
    public void LeereMitarbeitersucheFindetAlle()
    {
        using var testdatenbank = new Testdatenbank();
        var erster = SuchMitarbeiter();
        var zweiter = NeuerSuchMitarbeiter("Anna", "Beispiel", "P-456");

        Assert.True(testdatenbank.MitarbeiterService.PasstZurSuche(erster, string.Empty));
        Assert.True(testdatenbank.MitarbeiterService.PasstZurSuche(zweiter, string.Empty));
    }

    [Fact]
    public void ArbeitskopieEnthaeltStartUndEndzeit()
    {
        using var testdatenbank = new Testdatenbank();
        var original = NeueArbeitszeit();

        var kopie = testdatenbank.ArbeitszeitService.ErstelleArbeitskopie(original);

        Assert.Equal(new TimeOnly(8, 15), kopie.Beginn);
        Assert.Equal(new TimeOnly(16, 45), kopie.Ende);
        Assert.Equal(30, kopie.PauseMinuten);
    }

    [Fact]
    public void AenderungDerArbeitskopieVeraendertOriginalNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var original = NeueArbeitszeit();
        var kopie = testdatenbank.ArbeitszeitService.ErstelleArbeitskopie(original);

        kopie.Beginn = new TimeOnly(9, 0);
        kopie.Ende = new TimeOnly(17, 0);

        Assert.Equal(new TimeOnly(8, 15), original.Beginn);
        Assert.Equal(new TimeOnly(16, 45), original.Ende);
    }

    [Fact]
    public async Task SpeichernUebernimmtGeaenderteArbeitszeit()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = await testdatenbank.ArbeitszeitHinzufuegen();
        var kopie = testdatenbank.ArbeitszeitService.ErstelleArbeitskopie(arbeitszeit);
        kopie.Beginn = new TimeOnly(9, 0);
        kopie.Ende = new TimeOnly(17, 30);

        await testdatenbank.ArbeitszeitService.UpdateAsync(kopie);
        var gespeichert = await testdatenbank.ArbeitszeitService.GetByIdAsync(arbeitszeit.Id);

        Assert.Equal(new TimeOnly(9, 0), gespeichert?.Beginn);
        Assert.Equal(new TimeOnly(17, 30), gespeichert?.Ende);
    }

    [Fact]
    public async Task AuftragsdatenKoennenFuerReadOnlyPopupGeladenWerden()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = await testdatenbank.AuftragHinzufuegen();

        var geladen = await testdatenbank.AuftragService.GetByIdAsync(auftrag.Id);

        Assert.Equal("Montage Halle", geladen?.Titel);
        Assert.Equal("Testkunde", geladen?.Kunde?.Firmenname);
        Assert.Equal("Beschreibung", geladen?.Beschreibung);
        Assert.Single(geladen?.Mitarbeiter ?? []);
    }

    private static Arbeitszeit NeueArbeitszeit()
    {
        return new Arbeitszeit
        {
            MitarbeiterId = 1,
            Datum = new DateTime(2026, 8, 11),
            Beginn = new TimeOnly(8, 15),
            Ende = new TimeOnly(16, 45),
            PauseMinuten = 30
        };
    }

    private static Mitarbeiter SuchMitarbeiter()
    {
        var mitarbeiter = NeuerSuchMitarbeiter("Fritz", "Muster", "P-123");
        mitarbeiter.Qualifikationen.Add(new Qualifikation { Name = "Tischler" });
        return mitarbeiter;
    }

    private static Mitarbeiter NeuerSuchMitarbeiter(string vorname, string nachname, string personalnummer)
    {
        return new Mitarbeiter
        {
            Vorname = vorname,
            Nachname = nachname,
            Personalnummer = personalnummer
        };
    }

    private sealed class Testdatenbank : IDisposable
    {
        private readonly SqliteConnection verbindung;
        private readonly ApplicationDbContext context;

        public ArbeitszeitService ArbeitszeitService { get; }
        public AbwesenheitService AbwesenheitService { get; }
        public AuftragService AuftragService { get; }
        public MitarbeiterService MitarbeiterService { get; }

        public Testdatenbank()
        {
            verbindung = new SqliteConnection("Data Source=:memory:");
            verbindung.Open();

            var optionen = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(verbindung)
                .Options;

            context = new ApplicationDbContext(optionen);
            context.Database.EnsureCreated();
            ArbeitszeitService = new ArbeitszeitService(context);
            AbwesenheitService = new AbwesenheitService(context);
            AuftragService = new AuftragService(context);
            MitarbeiterService = new MitarbeiterService(context);
        }

        public async Task<Arbeitszeit> ArbeitszeitHinzufuegen()
        {
            var mitarbeiter = NeuerMitarbeiter("1");
            var arbeitszeit = NeueArbeitszeit();
            arbeitszeit.Mitarbeiter = mitarbeiter;
            arbeitszeit.MitarbeiterId = 0;
            context.Arbeitszeiten.Add(arbeitszeit);
            await context.SaveChangesAsync();
            return arbeitszeit;
        }

        public async Task<Auftrag> AuftragHinzufuegen()
        {
            var auftrag = new Auftrag
            {
                Titel = "Montage Halle",
                Beschreibung = "Beschreibung",
                Einsatzort = "Wien",
                Startdatum = new DateTime(2026, 8, 11),
                Enddatum = new DateTime(2026, 8, 12),
                Kunde = new Kunde { Firmenname = "Testkunde" }
            };

            auftrag.Mitarbeiter.Add(NeuerMitarbeiter("2"));
            context.Auftraege.Add(auftrag);
            await context.SaveChangesAsync();
            return auftrag;
        }

        private static Mitarbeiter NeuerMitarbeiter(string nummer)
        {
            return new Mitarbeiter
            {
                Personalnummer = nummer,
                Vorname = "Max",
                Nachname = "Test"
            };
        }

        public void Dispose()
        {
            context.Dispose();
            verbindung.Dispose();
        }
    }
}
