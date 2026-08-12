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
    public async Task PersoenlicherAntragWirdAngemeldetemMitarbeiterZugeordnetUndIstOffen()
    {
        using var testdatenbank = new Testdatenbank();
        var fritz = await testdatenbank.MitarbeiterHinzufuegen("Fritz");
        var hans = await testdatenbank.MitarbeiterHinzufuegen("Hans");
        var antrag = new Abwesenheit
        {
            MitarbeiterId = hans.Id,
            Typ = "Urlaub",
            Von = new DateTime(2026, 8, 20),
            Bis = new DateTime(2026, 8, 22),
            Status = "Genehmigt"
        };

        var gespeichert = await testdatenbank.Service
            .PersoenlichenAntragHinzufuegenAsync(antrag, fritz.Id);
        var geladenerAntrag = await testdatenbank.Service.GetByIdAsync(antrag.Id);

        Assert.True(gespeichert);
        Assert.Equal(fritz.Id, geladenerAntrag?.MitarbeiterId);
        Assert.NotEqual(hans.Id, geladenerAntrag?.MitarbeiterId);
        Assert.Equal("Offen", geladenerAntrag?.Status);
    }

    [Fact]
    public async Task BisVorVonSpeichertKeinenPersoenlichenAntrag()
    {
        using var testdatenbank = new Testdatenbank();
        var fritz = await testdatenbank.MitarbeiterHinzufuegen("Fritz");
        var antrag = new Abwesenheit
        {
            Typ = "Urlaub",
            Von = new DateTime(2026, 8, 22),
            Bis = new DateTime(2026, 8, 20)
        };

        var gespeichert = await testdatenbank.Service
            .PersoenlichenAntragHinzufuegenAsync(antrag, fritz.Id);

        Assert.False(gespeichert);
        Assert.Empty(await testdatenbank.Service.GetAllAsync());
    }

    [Fact]
    public async Task PersoenlicheListeEnthaeltNurEigeneAntraege()
    {
        using var testdatenbank = new Testdatenbank();
        var fritz = await testdatenbank.MitarbeiterHinzufuegen("Fritz");
        var hans = await testdatenbank.MitarbeiterHinzufuegen("Hans");
        await testdatenbank.Service.PersoenlichenAntragHinzufuegenAsync(
            NeuerPersoenlicherAntrag("Urlaub"), fritz.Id);
        await testdatenbank.Service.PersoenlichenAntragHinzufuegenAsync(
            NeuerPersoenlicherAntrag("Zeitausgleich"), hans.Id);

        var fritzAntraege = await testdatenbank.Service.GetByMitarbeiterAsync(fritz.Id);
        var hansAntraege = await testdatenbank.Service.GetByMitarbeiterAsync(hans.Id);
        var alleAntraege = await testdatenbank.Service.GetAllAsync();

        Assert.Single(fritzAntraege);
        Assert.Equal("Urlaub", fritzAntraege[0].Typ);
        Assert.Single(hansAntraege);
        Assert.Equal("Zeitausgleich", hansAntraege[0].Typ);
        Assert.Equal(2, alleAntraege.Count);
    }

    [Fact]
    public async Task DerselbeAntragWirdBeiMehrfachemSubmitNurEinmalGespeichert()
    {
        using var testdatenbank = new Testdatenbank();
        var fritz = await testdatenbank.MitarbeiterHinzufuegen("Fritz");
        var antrag = NeuerPersoenlicherAntrag("Urlaub");

        var erstesSpeichern = await testdatenbank.Service
            .PersoenlichenAntragHinzufuegenAsync(antrag, fritz.Id);
        var zweitesSpeichern = await testdatenbank.Service
            .PersoenlichenAntragHinzufuegenAsync(antrag, fritz.Id);
        var gespeicherteAntraege = await testdatenbank.Service.GetAllAsync();

        Assert.True(erstesSpeichern);
        Assert.False(zweitesSpeichern);
        Assert.Single(gespeicherteAntraege);
    }

    [Fact]
    public async Task NeuerAntragIstSofortInListeUndOffenerKpiEnthalten()
    {
        using var testdatenbank = new Testdatenbank();
        var fritz = await testdatenbank.MitarbeiterHinzufuegen("Fritz");
        var antrag = NeuerPersoenlicherAntrag("Urlaub");

        await testdatenbank.Service.PersoenlichenAntragHinzufuegenAsync(antrag, fritz.Id);
        var antraege = await testdatenbank.Service.GetByMitarbeiterAsync(fritz.Id);
        var offeneAntraege = 0;

        foreach (var gespeicherterAntrag in antraege)
        {
            if (gespeicherterAntrag.Status == "Offen")
            {
                offeneAntraege++;
            }
        }

        Assert.Single(antraege);
        Assert.Equal(antrag.Id, antraege[0].Id);
        Assert.Equal(1, offeneAntraege);
    }

    [Fact]
    public void MehrtaegigeAbwesenheitUmfasstStartMitteUndEnde()
    {
        using var testdatenbank = new Testdatenbank();
        var abwesenheit = NeueAbwesenheit(
            new DateTime(2026, 8, 19),
            new DateTime(2026, 8, 21));
        var sichtbareTage = 0;

        for (var tag = 18; tag <= 22; tag++)
        {
            var datum = new DateTime(2026, 8, tag);

            if (testdatenbank.Service.IstAbwesendAmTag(abwesenheit, datum))
            {
                sichtbareTage++;
            }
        }

        Assert.Equal(3, sichtbareTage);
        Assert.Equal("absence-start", testdatenbank.Service.KalenderSegmentKlasse(abwesenheit, new DateTime(2026, 8, 19)));
        Assert.Equal("absence-middle", testdatenbank.Service.KalenderSegmentKlasse(abwesenheit, new DateTime(2026, 8, 20)));
        Assert.Equal("absence-end", testdatenbank.Service.KalenderSegmentKlasse(abwesenheit, new DateTime(2026, 8, 21)));
    }

    [Fact]
    public void AbwesenheitUeberWochenwechselBeginntInNeuerZeileNeu()
    {
        using var testdatenbank = new Testdatenbank();
        var abwesenheit = NeueAbwesenheit(
            new DateTime(2026, 8, 21),
            new DateTime(2026, 8, 24));

        Assert.Equal("absence-start", testdatenbank.Service.KalenderSegmentKlasse(abwesenheit, new DateTime(2026, 8, 21)));
        Assert.Equal("absence-middle", testdatenbank.Service.KalenderSegmentKlasse(abwesenheit, new DateTime(2026, 8, 22)));
        Assert.Equal("absence-end", testdatenbank.Service.KalenderSegmentKlasse(abwesenheit, new DateTime(2026, 8, 23)));
        Assert.Equal("absence-single", testdatenbank.Service.KalenderSegmentKlasse(abwesenheit, new DateTime(2026, 8, 24)));
        Assert.True(testdatenbank.Service.IstKalenderSegmentStart(abwesenheit, new DateTime(2026, 8, 24)));
    }

    [Fact]
    public void EintaegigeAbwesenheitWirdAlsEinzelnesSegmentDargestellt()
    {
        using var testdatenbank = new Testdatenbank();
        var abwesenheit = NeueAbwesenheit(
            new DateTime(2026, 8, 20),
            new DateTime(2026, 8, 20));

        Assert.Equal("absence-single", testdatenbank.Service.KalenderSegmentKlasse(abwesenheit, new DateTime(2026, 8, 20)));
        Assert.True(testdatenbank.Service.IstKalenderSegmentStart(abwesenheit, new DateTime(2026, 8, 20)));
    }

    [Fact]
    public void OffenerAntragIstImKalenderSichtbarUndAbgelehnterNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var datum = new DateTime(2026, 8, 20);
        var offenerAntrag = NeuerPersoenlicherAntrag("Urlaub");
        var abgelehnterAntrag = NeuerPersoenlicherAntrag("Urlaub");
        abgelehnterAntrag.Status = "Abgelehnt";

        Assert.True(testdatenbank.Service.IstAbwesendAmTag(offenerAntrag, datum));
        Assert.False(testdatenbank.Service.IstAbwesendAmTag(abgelehnterAntrag, datum));
    }

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

    private static Abwesenheit NeuerPersoenlicherAntrag(string typ)
    {
        return new Abwesenheit
        {
            Typ = typ,
            Von = new DateTime(2026, 8, 20),
            Bis = new DateTime(2026, 8, 20),
            Status = "Offen"
        };
    }

    private static Abwesenheit NeueAbwesenheit(DateTime von, DateTime bis)
    {
        return new Abwesenheit
        {
            MitarbeiterId = 1,
            Typ = "Urlaub",
            Von = von,
            Bis = bis,
            Status = "Genehmigt"
        };
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

        public async Task<Mitarbeiter> MitarbeiterHinzufuegen(string vorname)
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = Guid.NewGuid().ToString(),
                Vorname = vorname,
                Nachname = "Test"
            };

            context.Mitarbeiter.Add(mitarbeiter);
            await context.SaveChangesAsync();
            return mitarbeiter;
        }

        public void Dispose()
        {
            context.Dispose();
            verbindung.Dispose();
        }
    }
}
