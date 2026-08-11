using HRomance.Data;
using HRomance.Models;
using HRomance.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRomance.Tests;

public class DashboardTests
{
    [Fact]
    public void OffeneAuftraegeWerdenKorrektGezaehlt()
    {
        using var testdatenbank = new Testdatenbank();
        var auftraege = new List<Auftrag> { NeuerAuftrag(), NeuerAuftrag(), BesetzterAuftrag() };
        var anzahl = 0;

        foreach (var auftrag in auftraege)
        {
            if (!testdatenbank.AuftragService.IstBesetzt(auftrag))
            {
                anzahl++;
            }
        }

        Assert.Equal(2, anzahl);
    }

    [Fact]
    public void BesetzteAuftraegeWerdenKorrektGezaehlt()
    {
        using var testdatenbank = new Testdatenbank();
        var auftraege = new List<Auftrag> { NeuerAuftrag(), BesetzterAuftrag(), BesetzterAuftrag() };
        var anzahl = 0;

        foreach (var auftrag in auftraege)
        {
            if (testdatenbank.AuftragService.IstBesetzt(auftrag))
            {
                anzahl++;
            }
        }

        Assert.Equal(2, anzahl);
    }

    [Fact]
    public void OffeneAntraegeWerdenKorrektGezaehlt()
    {
        using var testdatenbank = new Testdatenbank();
        var antraege = new List<Abwesenheit>
        {
            NeueAbwesenheit("Offen"),
            NeueAbwesenheit("Offen"),
            NeueAbwesenheit("Genehmigt")
        };
        var anzahl = 0;

        foreach (var antrag in antraege)
        {
            if (testdatenbank.AbwesenheitService.IstOffenerAntrag(antrag))
            {
                anzahl++;
            }
        }

        Assert.Equal(2, anzahl);
    }

    [Fact]
    public void BearbeiteteAntraegeZaehlenNichtAlsOffen()
    {
        using var testdatenbank = new Testdatenbank();

        Assert.False(testdatenbank.AbwesenheitService.IstOffenerAntrag(NeueAbwesenheit("Genehmigt")));
        Assert.False(testdatenbank.AbwesenheitService.IstOffenerAntrag(NeueAbwesenheit("Abgelehnt")));
    }

    [Fact]
    public void LaufenderAuftragIstHeutigerEinsatz()
    {
        using var testdatenbank = new Testdatenbank();
        var heute = new DateTime(2026, 8, 11);
        var auftrag = NeuerAuftrag(10, 12);

        Assert.True(testdatenbank.AuftragService.IstEinsatzAmTag(auftrag, heute));
    }

    [Fact]
    public void VergangenerAuftragIstKeinHeutigerEinsatz()
    {
        using var testdatenbank = new Testdatenbank();
        var heute = new DateTime(2026, 8, 11);
        var auftrag = NeuerAuftrag(5, 10);

        Assert.False(testdatenbank.AuftragService.IstEinsatzAmTag(auftrag, heute));
    }

    [Fact]
    public void ZukuenftigerAuftragIstKeinHeutigerEinsatz()
    {
        using var testdatenbank = new Testdatenbank();
        var heute = new DateTime(2026, 8, 11);
        var auftrag = NeuerAuftrag(12, 15);

        Assert.False(testdatenbank.AuftragService.IstEinsatzAmTag(auftrag, heute));
    }

    [Fact]
    public void GenehmigteAbwesenheitWirdHeuteAngezeigt()
    {
        using var testdatenbank = new Testdatenbank();
        var heute = new DateTime(2026, 8, 11);
        var abwesenheit = NeueAbwesenheit("Genehmigt", 10, 12);

        Assert.True(testdatenbank.AbwesenheitService.IstAbwesendAmTag(abwesenheit, heute));
    }

    [Fact]
    public void AbgelehnteAbwesenheitWirdHeuteNichtAngezeigt()
    {
        using var testdatenbank = new Testdatenbank();
        var heute = new DateTime(2026, 8, 11);
        var abwesenheit = NeueAbwesenheit("Abgelehnt", 10, 12);

        Assert.False(testdatenbank.AbwesenheitService.IstAbwesendAmTag(abwesenheit, heute));
    }

    [Fact]
    public void MonatsstundenWerdenFuerAusgewaehltenMitarbeiterBerechnet()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeiten = new List<Arbeitszeit>
        {
            NeueArbeitszeit(1, 8, 16, 30),
            NeueArbeitszeit(1, 8, 12, 0),
            NeueArbeitszeit(2, 8, 16, 0)
        };

        var stunden = testdatenbank.ArbeitszeitService.BerechneMonatsstunden(
            arbeitszeiten, 1, 2026, 8);

        Assert.Equal(11.5, stunden);
    }

    [Fact]
    public void MitarbeiterSiehtNurEigeneAbwesenheiten()
    {
        using var testdatenbank = new Testdatenbank();
        var eigeneAbwesenheit = NeueAbwesenheit("Genehmigt");
        eigeneAbwesenheit.MitarbeiterId = 1;

        Assert.True(testdatenbank.AbwesenheitService.PasstZuMitarbeiter(eigeneAbwesenheit, 1));
    }

    [Fact]
    public void MitarbeiterSiehtKeineFremdenAbwesenheiten()
    {
        using var testdatenbank = new Testdatenbank();
        var fremdeAbwesenheit = NeueAbwesenheit("Genehmigt");
        fremdeAbwesenheit.MitarbeiterId = 2;

        Assert.False(testdatenbank.AbwesenheitService.PasstZuMitarbeiter(fremdeAbwesenheit, 1));
    }

    [Fact]
    public void MitarbeiterSiehtZugewiesenenAuftrag()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = NeuerAuftrag();
        auftrag.Mitarbeiter.Add(new Mitarbeiter { Id = 1 });

        Assert.True(testdatenbank.AuftragService.PasstZuMitarbeiter(auftrag, 1));
    }

    [Fact]
    public void MitarbeiterSiehtNichtZugewiesenenAuftragNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = NeuerAuftrag();
        auftrag.Mitarbeiter.Add(new Mitarbeiter { Id = 2 });

        Assert.False(testdatenbank.AuftragService.PasstZuMitarbeiter(auftrag, 1));
    }

    [Fact]
    public void FuenfArbeitstageUeberspringenSamstagUndSonntag()
    {
        using var testdatenbank = new Testdatenbank();
        var start = new DateTime(2026, 8, 13);

        var arbeitstage = testdatenbank.AuftragService.NaechsteArbeitstage(start, 5);

        Assert.Equal(new DateTime(2026, 8, 13), arbeitstage[0]);
        Assert.Equal(new DateTime(2026, 8, 14), arbeitstage[1]);
        Assert.Equal(new DateTime(2026, 8, 17), arbeitstage[2]);
        Assert.Equal(new DateTime(2026, 8, 18), arbeitstage[3]);
        Assert.Equal(new DateTime(2026, 8, 19), arbeitstage[4]);
    }

    [Fact]
    public void AuftragInnerhalbDerNaechstenFuenfArbeitstageWirdAngezeigt()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = NeuerAuftrag(17, 17);

        var sichtbar = testdatenbank.AuftragService.IstInNaechstenFuenfArbeitstagen(
            auftrag, new DateTime(2026, 8, 13));

        Assert.True(sichtbar);
    }

    [Fact]
    public void AuftragAusserhalbDerNaechstenFuenfArbeitstageWirdNichtAngezeigt()
    {
        using var testdatenbank = new Testdatenbank();
        var auftrag = NeuerAuftrag(20, 20);

        var sichtbar = testdatenbank.AuftragService.IstInNaechstenFuenfArbeitstagen(
            auftrag, new DateTime(2026, 8, 13));

        Assert.False(sichtbar);
    }

    [Fact]
    public void ManagerKennzahlenVerwendenWeiterhinGesamtdaten()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = new List<Mitarbeiter> { new(), new(), new() };
        var auftraege = new List<Auftrag> { NeuerAuftrag(), BesetzterAuftrag() };
        var offeneAuftraege = 0;
        var besetzteAuftraege = 0;

        foreach (var auftrag in auftraege)
        {
            if (testdatenbank.AuftragService.IstBesetzt(auftrag))
            {
                besetzteAuftraege++;
            }
            else
            {
                offeneAuftraege++;
            }
        }

        Assert.Equal(3, mitarbeiter.Count);
        Assert.Equal(1, offeneAuftraege);
        Assert.Equal(1, besetzteAuftraege);
    }

    private static Auftrag NeuerAuftrag(int startTag = 10, int endeTag = 12)
    {
        return new Auftrag
        {
            Startdatum = new DateTime(2026, 8, startTag),
            Enddatum = new DateTime(2026, 8, endeTag)
        };
    }

    private static Auftrag BesetzterAuftrag()
    {
        var auftrag = NeuerAuftrag();
        auftrag.Mitarbeiter.Add(new Mitarbeiter { Id = 1 });
        return auftrag;
    }

    private static Abwesenheit NeueAbwesenheit(string status, int vonTag = 10, int bisTag = 12)
    {
        return new Abwesenheit
        {
            Status = status,
            Von = new DateTime(2026, 8, vonTag),
            Bis = new DateTime(2026, 8, bisTag)
        };
    }

    private static Arbeitszeit NeueArbeitszeit(
        int mitarbeiterId,
        int beginnStunde,
        int endeStunde,
        int pauseMinuten)
    {
        return new Arbeitszeit
        {
            MitarbeiterId = mitarbeiterId,
            Datum = new DateTime(2026, 8, 11),
            Beginn = new TimeOnly(beginnStunde, 0),
            Ende = new TimeOnly(endeStunde, 0),
            PauseMinuten = pauseMinuten
        };
    }

    private sealed class Testdatenbank : IDisposable
    {
        private readonly SqliteConnection verbindung;
        private readonly ApplicationDbContext context;

        public AuftragService AuftragService { get; }
        public AbwesenheitService AbwesenheitService { get; }
        public ArbeitszeitService ArbeitszeitService { get; }

        public Testdatenbank()
        {
            verbindung = new SqliteConnection("Data Source=:memory:");
            verbindung.Open();

            var optionen = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(verbindung)
                .Options;

            context = new ApplicationDbContext(optionen);
            context.Database.EnsureCreated();
            AuftragService = new AuftragService(context);
            AbwesenheitService = new AbwesenheitService(context);
            ArbeitszeitService = new ArbeitszeitService(context);
        }

        public void Dispose()
        {
            context.Dispose();
            verbindung.Dispose();
        }
    }
}
