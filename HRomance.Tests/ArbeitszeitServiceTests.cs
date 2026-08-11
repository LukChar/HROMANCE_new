using HRomance.Data;
using HRomance.Models;
using HRomance.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRomance.Tests;

public class ArbeitszeitServiceTests
{
    [Fact]
    public void AchtBisSechzehnDreissigMitPauseErgibtAchtStunden()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(8, 0, 16, 30, 30);

        var stunden = testdatenbank.Service.BerechneArbeitsstunden(arbeitszeit);

        Assert.Equal(8, stunden);
    }

    [Fact]
    public void NeunStundenMitSechzigMinutenPauseErgibtAchtStunden()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(8, 0, 17, 0, 60);

        var stunden = testdatenbank.Service.BerechneArbeitsstunden(arbeitszeit);

        Assert.Equal(8, stunden);
    }

    [Fact]
    public void ArbeitszeitOhnePauseWirdVollBerechnet()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(8, 0, 16, 0, 0);

        var stunden = testdatenbank.Service.BerechneArbeitsstunden(arbeitszeit);

        Assert.Equal(8, stunden);
    }

    [Fact]
    public void MehrarbeitErgibtPositivenTagessaldo()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(8, 0, 16, 30, 0);

        var saldo = testdatenbank.Service.BerechneTagessaldo([arbeitszeit], 8);

        Assert.Equal(0.5, saldo);
    }

    [Fact]
    public void WenigerarbeitErgibtNegativenTagessaldo()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(8, 0, 15, 0, 0);

        var saldo = testdatenbank.Service.BerechneTagessaldo([arbeitszeit], 8);

        Assert.Equal(-1, saldo);
    }

    [Fact]
    public void ZweiEintraegeAnEinemTagWerdenAddiert()
    {
        using var testdatenbank = new Testdatenbank();
        var vormittag = NeueArbeitszeit(8, 0, 12, 0, 0);
        var nachmittag = NeueArbeitszeit(13, 0, 17, 0, 0);

        var saldo = testdatenbank.Service.BerechneTagessaldo([vormittag, nachmittag], 0);

        Assert.Equal(8, saldo);
    }

    [Fact]
    public void MehrereArbeitszeitenWerdenZuMonatsstundenAddiert()
    {
        using var testdatenbank = new Testdatenbank();
        var ersterEintrag = NeueArbeitszeit(8, 0, 12, 0, 0);
        var zweiterEintrag = NeueArbeitszeit(13, 0, 17, 0, 0);
        ersterEintrag.MitarbeiterId = 1;
        zweiterEintrag.MitarbeiterId = 1;

        var stunden = testdatenbank.Service.BerechneMonatsstunden(
            [ersterEintrag, zweiterEintrag], 1, 2026, 8);

        Assert.Equal(8, stunden);
    }

    [Fact]
    public void MonatsstundenBeruecksichtigenNurSichtbarenMonat()
    {
        using var testdatenbank = new Testdatenbank();
        var august = NeueArbeitszeit(8, 0, 16, 0, 0);
        var september = NeueArbeitszeit(8, 0, 12, 0, 0);
        august.MitarbeiterId = 1;
        september.MitarbeiterId = 1;
        september.Datum = new DateTime(2026, 9, 1);

        var stunden = testdatenbank.Service.BerechneMonatsstunden(
            [august, september], 1, 2026, 8);

        Assert.Equal(8, stunden);
    }

    [Fact]
    public void MonatsstundenBeruecksichtigenNurAusgewaehltenMitarbeiter()
    {
        using var testdatenbank = new Testdatenbank();
        var ersterMitarbeiter = NeueArbeitszeit(8, 0, 16, 0, 0);
        var zweiterMitarbeiter = NeueArbeitszeit(8, 0, 12, 0, 0);
        ersterMitarbeiter.MitarbeiterId = 1;
        zweiterMitarbeiter.MitarbeiterId = 2;

        var stunden = testdatenbank.Service.BerechneMonatsstunden(
            [ersterMitarbeiter, zweiterMitarbeiter], 1, 2026, 8);

        Assert.Equal(8, stunden);
    }

    [Fact]
    public void MitarbeiterOhneArbeitszeitenHatNullMonatsstunden()
    {
        using var testdatenbank = new Testdatenbank();

        var stunden = testdatenbank.Service.BerechneMonatsstunden([], 1, 2026, 8);

        Assert.Equal(0, stunden);
    }

    [Fact]
    public async Task ZweiTagessaldenErgebenKorrektenMonatssaldo()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen();
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 11, 8, 0, 16, 30, 0);
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 12, 8, 0, 15, 0, 0);

        var werte = await testdatenbank.Service.GetMonatswerteAsync(mitarbeiter.Id, 2026, 8, 8);

        Assert.Equal(-0.5, werte.Saldo);
    }

    [Fact]
    public void EndeVorBeginnIstUngueltigUndErgibtKeineNegativenStunden()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(16, 0, 8, 0, 0);

        var fehler = testdatenbank.Service.Validierungsfehler(arbeitszeit);
        var stunden = testdatenbank.Service.BerechneArbeitsstunden(arbeitszeit);

        Assert.NotEmpty(fehler);
        Assert.Equal(0, stunden);
    }

    [Fact]
    public void NegativePauseIstUngueltig()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(8, 0, 16, 0, -15);

        var fehler = testdatenbank.Service.Validierungsfehler(arbeitszeit);

        Assert.Equal("Die Pause darf nicht negativ sein.", fehler);
    }

    [Fact]
    public void PauseLaengerAlsArbeitsdauerIstUngueltig()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(8, 0, 9, 0, 90);

        var fehler = testdatenbank.Service.Validierungsfehler(arbeitszeit);

        Assert.Equal("Die Pause darf nicht länger als die gesamte Arbeitsdauer sein.", fehler);
    }

    [Fact]
    public void AchtIststundenUndAchtSollstundenErgebenNullsaldo()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeit = NeueArbeitszeit(8, 0, 16, 0, 0);

        var saldo = testdatenbank.Service.BerechneTagessaldo([arbeitszeit], 8);

        Assert.Equal(0, saldo);
    }

    [Fact]
    public async Task MonatssollBeruecksichtigtNurTageMitArbeitszeit()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen();
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 11, 8, 0, 12, 0, 0);
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 11, 13, 0, 17, 0, 0);
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 12, 8, 0, 16, 0, 0);

        var werte = await testdatenbank.Service.GetMonatswerteAsync(mitarbeiter.Id, 2026, 8, 8);

        Assert.Equal(16, werte.Soll);
    }

    private static Arbeitszeit NeueArbeitszeit(
        int beginnStunde,
        int beginnMinute,
        int endeStunde,
        int endeMinute,
        int pause)
    {
        return new Arbeitszeit
        {
            Datum = new DateTime(2026, 8, 11),
            Beginn = new TimeOnly(beginnStunde, beginnMinute),
            Ende = new TimeOnly(endeStunde, endeMinute),
            PauseMinuten = pause
        };
    }

    private sealed class Testdatenbank : IDisposable
    {
        private readonly SqliteConnection verbindung;
        private readonly ApplicationDbContext context;

        public ArbeitszeitService Service { get; }

        public Testdatenbank()
        {
            verbindung = new SqliteConnection("Data Source=:memory:");
            verbindung.Open();

            var optionen = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(verbindung)
                .Options;

            context = new ApplicationDbContext(optionen);
            context.Database.EnsureCreated();
            Service = new ArbeitszeitService(context);
        }

        public async Task<Mitarbeiter> MitarbeiterHinzufuegen()
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = Guid.NewGuid().ToString(),
                Vorname = "Test",
                Nachname = "Person",
                SollStundenProTag = 8
            };

            context.Mitarbeiter.Add(mitarbeiter);
            await context.SaveChangesAsync();
            return mitarbeiter;
        }

        public async Task ArbeitszeitHinzufuegen(
            int mitarbeiterId,
            int tag,
            int beginnStunde,
            int beginnMinute,
            int endeStunde,
            int endeMinute,
            int pause)
        {
            var arbeitszeit = NeueArbeitszeit(
                beginnStunde,
                beginnMinute,
                endeStunde,
                endeMinute,
                pause);

            arbeitszeit.MitarbeiterId = mitarbeiterId;
            arbeitszeit.Datum = new DateTime(2026, 8, tag);
            await Service.AddAsync(arbeitszeit);
        }

        public void Dispose()
        {
            context.Dispose();
            verbindung.Dispose();
        }
    }
}
