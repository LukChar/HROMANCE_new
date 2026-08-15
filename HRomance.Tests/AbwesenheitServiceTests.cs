using HRomance.Data;
using HRomance.Models;
using HRomance.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRomance.Tests;

public class AbwesenheitServiceTests
{
    [Fact]
    public async Task OffenerAntragKannGenehmigtWerden()
    {
        using var testdatenbank = new Testdatenbank();
        var antrag = await testdatenbank.AntragHinzufuegen("Offen");

        await testdatenbank.Service.StatusAendernAsync(antrag.Id, "Genehmigt");

        Assert.Equal("Genehmigt", (await testdatenbank.Service.GetByIdAsync(antrag.Id))?.Status);
    }

    [Fact]
    public async Task OffenerAntragKannAbgelehntWerden()
    {
        using var testdatenbank = new Testdatenbank();
        var antrag = await testdatenbank.AntragHinzufuegen("Offen");

        await testdatenbank.Service.StatusAendernAsync(antrag.Id, "Abgelehnt");

        Assert.Equal("Abgelehnt", (await testdatenbank.Service.GetByIdAsync(antrag.Id))?.Status);
    }

    [Fact]
    public async Task GenehmigterAntragBleibtGenehmigt()
    {
        using var testdatenbank = new Testdatenbank();
        var antrag = await testdatenbank.AntragHinzufuegen("Genehmigt");

        await testdatenbank.Service.StatusAendernAsync(antrag.Id, "Abgelehnt");

        Assert.Equal("Genehmigt", (await testdatenbank.Service.GetByIdAsync(antrag.Id))?.Status);
    }

    [Fact]
    public async Task AbgelehnterAntragBleibtAbgelehnt()
    {
        using var testdatenbank = new Testdatenbank();
        var antrag = await testdatenbank.AntragHinzufuegen("Abgelehnt");

        await testdatenbank.Service.StatusAendernAsync(antrag.Id, "Genehmigt");

        Assert.Equal("Abgelehnt", (await testdatenbank.Service.GetByIdAsync(antrag.Id))?.Status);
    }

    [Fact]
    public async Task ZeitausgleichWirdGespeichertUndGeladen()
    {
        using var testdatenbank = new Testdatenbank();
        var antrag = await testdatenbank.AntragHinzufuegen("Offen", "Zeitausgleich");

        var geladen = await testdatenbank.Service.GetByIdAsync(antrag.Id);

        Assert.Equal("Zeitausgleich", geladen?.Typ);
    }

    [Fact]
    public void ArtfilterUrlaubZeigtNurUrlaub()
    {
        using var testdatenbank = new Testdatenbank();
        var antraege = Testantraege();

        var ergebnis = testdatenbank.Service.FilternUndSortieren(antraege, "", "Urlaub", "Alle", true);

        Assert.All(ergebnis, antrag => Assert.Equal("Urlaub", antrag.Typ));
        Assert.Equal(2, ergebnis.Count);
    }

    [Fact]
    public void ArtfilterZeitausgleichZeigtNurZeitausgleich()
    {
        using var testdatenbank = new Testdatenbank();

        var ergebnis = testdatenbank.Service.FilternUndSortieren(Testantraege(), "", "Zeitausgleich", "Alle", true);

        Assert.Single(ergebnis);
        Assert.Equal("Zeitausgleich", ergebnis[0].Typ);
    }

    [Fact]
    public void StatusfilterOffenZeigtNurOffeneAntraege()
    {
        using var testdatenbank = new Testdatenbank();

        var ergebnis = testdatenbank.Service.FilternUndSortieren(Testantraege(), "", "Alle", "Offen", true);

        Assert.All(ergebnis, antrag => Assert.Equal("Offen", antrag.Status));
    }

    [Fact]
    public void ArtUndStatusfilterFunktionierenGemeinsam()
    {
        using var testdatenbank = new Testdatenbank();

        var ergebnis = testdatenbank.Service.FilternUndSortieren(Testantraege(), "", "Urlaub", "Offen", true);

        Assert.Single(ergebnis);
        Assert.Equal("Urlaub", ergebnis[0].Typ);
        Assert.Equal("Offen", ergebnis[0].Status);
    }

    [Fact]
    public void NeuesteAntraegeStehenZuerst()
    {
        using var testdatenbank = new Testdatenbank();

        var ergebnis = testdatenbank.Service.FilternUndSortieren(Testantraege(), "", "Alle", "Alle", true);

        Assert.Equal(new DateTime(2026, 8, 20), ergebnis[0].Von);
    }

    [Fact]
    public void AeltesteAntraegeStehenZuerst()
    {
        using var testdatenbank = new Testdatenbank();

        var ergebnis = testdatenbank.Service.FilternUndSortieren(Testantraege(), "", "Alle", "Alle", false);

        Assert.Equal(new DateTime(2026, 8, 5), ergebnis[0].Von);
    }

    [Fact]
    public void SucheNachMitarbeiternameFindetPassendenAntrag()
    {
        using var testdatenbank = new Testdatenbank();

        var ergebnis = testdatenbank.Service.FilternUndSortieren(Testantraege(), "Anna", "Alle", "Alle", true);

        Assert.Single(ergebnis);
        Assert.Equal("Anna", ergebnis[0].Mitarbeiter?.Vorname);
    }

    [Fact]
    public void SucheNachTypFindetPassendenAntrag()
    {
        using var testdatenbank = new Testdatenbank();

        var ergebnis = testdatenbank.Service.FilternUndSortieren(Testantraege(), "Krankenstand", "Alle", "Alle", true);

        Assert.Single(ergebnis);
        Assert.Equal("Krankenstand", ergebnis[0].Typ);
    }

    private static List<Abwesenheit> Testantraege()
    {
        return
        [
            NeuerAntrag("Anna", "Urlaub", "Offen", 10, "Sommerurlaub"),
            NeuerAntrag("Bernd", "Urlaub", "Genehmigt", 20, "Reise"),
            NeuerAntrag("Clara", "Zeitausgleich", "Offen", 5, "Überstunden"),
            NeuerAntrag("David", "Krankenstand", "Abgelehnt", 15, "Krank")
        ];
    }

    private static Abwesenheit NeuerAntrag(
        string vorname,
        string typ,
        string status,
        int tag,
        string grund)
    {
        return new Abwesenheit
        {
            Mitarbeiter = new Mitarbeiter { Vorname = vorname, Nachname = "Test" },
            Typ = typ,
            Status = status,
            Von = new DateTime(2026, 8, tag),
            Bis = new DateTime(2026, 8, tag),
            Grund = grund
        };
    }

    private sealed class Testdatenbank : IDisposable
    {
        private readonly SqliteConnection verbindung;
        private readonly ApplicationDbContext context;

        public AbwesenheitService Service { get; }

        public Testdatenbank()
        {
            verbindung = new SqliteConnection("Data Source=:memory:");
            verbindung.Open();

            var optionen = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(verbindung)
                .Options;

            context = new ApplicationDbContext(optionen);
            context.Database.EnsureCreated();
            Service = new AbwesenheitService(context);
        }

        public async Task<Abwesenheit> AntragHinzufuegen(string status, string typ = "Urlaub")
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = Guid.NewGuid().ToString(),
                Vorname = "Test",
                Nachname = "Person"
            };

            var antrag = new Abwesenheit
            {
                Mitarbeiter = mitarbeiter,
                Typ = typ,
                Von = new DateTime(2026, 8, 12),
                Bis = new DateTime(2026, 8, 15),
                Status = status
            };

            context.Abwesenheiten.Add(antrag);
            await context.SaveChangesAsync();
            return antrag;
        }

        public void Dispose()
        {
            context.Dispose();
            verbindung.Dispose();
        }
    }
}
