using HRomance.Models;
using HRomance.Services;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Tests;

public class AbwesenheitServiceTests
{
    [Fact]
    public async Task PersoenlichenAntragHinzufuegenAsync_ErstelltAntragMitStatusOffen()
    {
        var name = TestDatenbank.NeuerName();
        int mitarbeiterId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-001",
                Vorname = "Anna",
                Nachname = "Berger"
            };
            context.Mitarbeiter.Add(mitarbeiter);
            await context.SaveChangesAsync();
            mitarbeiterId = mitarbeiter.Id;
        }

        bool ergebnis;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AbwesenheitService(context);
            var antrag = new Abwesenheit
            {
                Typ = "Urlaub",
                Von = new DateTime(2026, 12, 1),
                Bis = new DateTime(2026, 12, 5)
            };

            ergebnis = await service.PersoenlichenAntragHinzufuegenAsync(antrag, mitarbeiterId);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var abwesenheiten = await context.Abwesenheiten.ToListAsync();

            Assert.True(ergebnis);
            Assert.Single(abwesenheiten);
            Assert.Equal("Offen", abwesenheiten[0].Status);
            Assert.Equal(mitarbeiterId, abwesenheiten[0].MitarbeiterId);
        }
    }

    [Fact]
    public async Task PersoenlichenAntragHinzufuegenAsync_LehntAbWennBisVorVon()
    {
        var name = TestDatenbank.NeuerName();
        bool ergebnis;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AbwesenheitService(context);
            var antrag = new Abwesenheit
            {
                Typ = "Urlaub",
                Von = new DateTime(2026, 12, 5),
                Bis = new DateTime(2026, 12, 1)
            };

            ergebnis = await service.PersoenlichenAntragHinzufuegenAsync(antrag, 1);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            Assert.False(ergebnis);
            Assert.Empty(await context.Abwesenheiten.ToListAsync());
        }
    }

    [Fact]
    public async Task StatusAendernAsync_GenehmigtOffenenAntrag()
    {
        var name = TestDatenbank.NeuerName();
        var abwesenheitId = await AbwesenheitSpeichern(name, "Offen");

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AbwesenheitService(context);

            await service.StatusAendernAsync(abwesenheitId, "Genehmigt");
        }

        Assert.Equal("Genehmigt", await StatusLesen(name, abwesenheitId));
    }

    [Fact]
    public async Task StatusAendernAsync_LehntOffenenAntragAb()
    {
        var name = TestDatenbank.NeuerName();
        var abwesenheitId = await AbwesenheitSpeichern(name, "Offen");

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AbwesenheitService(context);

            await service.StatusAendernAsync(abwesenheitId, "Abgelehnt");
        }

        Assert.Equal("Abgelehnt", await StatusLesen(name, abwesenheitId));
    }

    [Fact]
    public async Task StatusAendernAsync_AendertBereitsGenehmigtenAntragNicht()
    {
        var name = TestDatenbank.NeuerName();
        var abwesenheitId = await AbwesenheitSpeichern(name, "Genehmigt");

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AbwesenheitService(context);

            await service.StatusAendernAsync(abwesenheitId, "Abgelehnt");
        }

        Assert.Equal("Genehmigt", await StatusLesen(name, abwesenheitId));
    }

    [Fact]
    public async Task StatusAendernAsync_AkzeptiertNurGueltigeStatuswerte()
    {
        var name = TestDatenbank.NeuerName();
        var abwesenheitId = await AbwesenheitSpeichern(name, "Offen");

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AbwesenheitService(context);

            await service.StatusAendernAsync(abwesenheitId, "Ungueltig");
        }

        Assert.Equal("Offen", await StatusLesen(name, abwesenheitId));
    }

    [Fact]
    public void FilternUndSortieren_FiltertNachTyp()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new AbwesenheitService(context);
        var antraege = new List<Abwesenheit>
        {
            new() { Typ = "Urlaub" },
            new() { Typ = "Krankenstand" },
            new() { Typ = "Urlaub" }
        };

        var ergebnis = service.FilternUndSortieren(antraege, "", "Urlaub", "Alle", true);

        Assert.Equal(2, ergebnis.Count);
        Assert.All(ergebnis, antrag => Assert.Equal("Urlaub", antrag.Typ));
    }

    [Fact]
    public void FilternUndSortieren_FiltertNachStatus()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new AbwesenheitService(context);
        var antraege = new List<Abwesenheit>
        {
            new() { Typ = "Urlaub", Status = "Offen" },
            new() { Typ = "Urlaub", Status = "Genehmigt" },
            new() { Typ = "Urlaub", Status = "Offen" }
        };

        var ergebnis = service.FilternUndSortieren(antraege, "", "Alle", "Offen", true);

        Assert.Equal(2, ergebnis.Count);
        Assert.All(ergebnis, antrag => Assert.Equal("Offen", antrag.Status));
    }

    [Fact]
    public void FilternUndSortieren_SortiertNachDatumAbsteigend()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new AbwesenheitService(context);
        var antraege = new List<Abwesenheit>
        {
            new() { Typ = "Urlaub", Von = new DateTime(2026, 10, 1) },
            new() { Typ = "Urlaub", Von = new DateTime(2026, 10, 15) },
            new() { Typ = "Urlaub", Von = new DateTime(2026, 10, 5) }
        };

        var ergebnis = service.FilternUndSortieren(antraege, "", "Alle", "Alle", true);

        Assert.Equal(new DateTime(2026, 10, 15), ergebnis[0].Von);
        Assert.Equal(new DateTime(2026, 10, 5), ergebnis[1].Von);
        Assert.Equal(new DateTime(2026, 10, 1), ergebnis[2].Von);
    }

    private static async Task<int> AbwesenheitSpeichern(string name, string status)
    {
        using var context = TestDatenbank.NeuerContext(name);
        var abwesenheit = new Abwesenheit
        {
            MitarbeiterId = 1,
            Von = new DateTime(2026, 12, 1),
            Bis = new DateTime(2026, 12, 5),
            Typ = "Urlaub",
            Status = status
        };

        context.Abwesenheiten.Add(abwesenheit);
        await context.SaveChangesAsync();
        return abwesenheit.Id;
    }

    private static async Task<string> StatusLesen(string name, int id)
    {
        using var context = TestDatenbank.NeuerContext(name);
        var abwesenheit = await context.Abwesenheiten.SingleAsync(a => a.Id == id);
        return abwesenheit.Status;
    }
}
