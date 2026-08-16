using HRomance.Data;
using HRomance.Models;
using HRomance.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRomance.Tests;

public class MatchingServiceTests
{
    private static readonly DateTime Startdatum = new(2026, 8, 12);

    [Fact]
    public async Task VollstaendigeQualifikationStehtVorTeilweiserQualifikation()
    {
        using var datenbank = new Testdatenbank();
        var elektro = await datenbank.QualifikationHinzufuegen("Elektro");
        var mechanik = await datenbank.QualifikationHinzufuegen("Mechanik");
        var vollstaendig = await datenbank.MitarbeiterHinzufuegen("Voll", true, elektro, mechanik);
        await datenbank.MitarbeiterHinzufuegen("Teil", true, elektro);
        await datenbank.AuftragHinzufuegen("Auftrag", 1, 2, null, elektro, mechanik);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.Equal(vollstaendig.Id, vorschlaege[0].Zuweisungen[0].Mitarbeiter?.Id);
        Assert.Equal(2, vorschlaege[0].Zuweisungen[0].PassendeQualifikationen);
    }

    [Fact]
    public async Task GenehmigteAbwesenheitSperrtMitarbeiter()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        var mitarbeiter = await datenbank.MitarbeiterHinzufuegen("Gesperrt", true, qualifikation);
        await datenbank.AbwesenheitHinzufuegen(mitarbeiter, 1, 3, "Genehmigt");
        await datenbank.AuftragHinzufuegen("Auftrag", 2, 2, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.Null(vorschlaege[0].Zuweisungen[0].Mitarbeiter);
    }

    [Fact]
    public async Task OffeneAbwesenheitSperrtMitarbeiterNicht()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        var mitarbeiter = await datenbank.MitarbeiterHinzufuegen("Verfügbar", true, qualifikation);
        await datenbank.AbwesenheitHinzufuegen(mitarbeiter, 1, 3, "Offen");
        await datenbank.AuftragHinzufuegen("Auftrag", 2, 2, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.Equal(mitarbeiter.Id, vorschlaege[0].Zuweisungen[0].Mitarbeiter?.Id);
    }

    [Fact]
    public async Task UeberschneidenderBestehenderAuftragSperrtMitarbeiter()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        var mitarbeiter = await datenbank.MitarbeiterHinzufuegen("Beschäftigt", true, qualifikation);
        await datenbank.AuftragHinzufuegen("Bestehend", 1, 3, mitarbeiter, qualifikation);
        await datenbank.AuftragHinzufuegen("Offen", 2, 4, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.Null(vorschlaege[0].Zuweisungen[0].Mitarbeiter);
    }

    [Fact]
    public async Task GrundsaetzlichNichtVerfuegbarerMitarbeiterWirdNichtVorgeschlagen()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        await datenbank.MitarbeiterHinzufuegen("Nicht verfügbar", false, qualifikation);
        await datenbank.AuftragHinzufuegen("Auftrag", 1, 2, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.Null(vorschlaege[0].Zuweisungen[0].Mitarbeiter);
    }

    [Fact]
    public async Task VorschlagEnthaeltAlleDreiOffenenAuftraege()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Allgemein");
        await datenbank.MitarbeiterHinzufuegen("Eins", true, qualifikation);
        await datenbank.MitarbeiterHinzufuegen("Zwei", true, qualifikation);
        await datenbank.MitarbeiterHinzufuegen("Drei", true, qualifikation);
        await datenbank.AuftragHinzufuegen("A", 1, 1, null, qualifikation);
        await datenbank.AuftragHinzufuegen("B", 2, 2, null, qualifikation);
        await datenbank.AuftragHinzufuegen("C", 3, 3, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.Equal(3, vorschlaege[0].Zuweisungen.Count);
        Assert.All(vorschlaege[0].Zuweisungen, zuweisung => Assert.NotNull(zuweisung.Mitarbeiter));
    }

    [Fact]
    public async Task MitarbeiterWirdNichtZweiUeberlappendenAuftraegenVorgeschlagen()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Allgemein");
        var mitarbeiter = await datenbank.MitarbeiterHinzufuegen("Einziger", true, qualifikation);
        await datenbank.AuftragHinzufuegen("A", 1, 3, null, qualifikation);
        await datenbank.AuftragHinzufuegen("B", 2, 4, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);
        var zuweisungen = vorschlaege[0].Zuweisungen
            .Count(zuweisung => zuweisung.Mitarbeiter?.Id == mitarbeiter.Id);

        Assert.Equal(1, zuweisungen);
    }

    [Fact]
    public async Task AuftragBleibtOhneZuweisungWennNiemandPasst()
    {
        using var datenbank = new Testdatenbank();
        var elektro = await datenbank.QualifikationHinzufuegen("Elektro");
        var tischler = await datenbank.QualifikationHinzufuegen("Tischler");
        await datenbank.MitarbeiterHinzufuegen("Tischler", true, tischler);
        await datenbank.AuftragHinzufuegen("Elektroauftrag", 1, 2, null, elektro);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.Single(vorschlaege[0].Zuweisungen);
        Assert.Null(vorschlaege[0].Zuweisungen[0].Mitarbeiter);
    }

    [Fact]
    public async Task VorschlaegeUnterscheidenSichBeiGleichwertigenAlternativen()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        await datenbank.MitarbeiterHinzufuegen("Eins", true, qualifikation);
        await datenbank.MitarbeiterHinzufuegen("Zwei", true, qualifikation);
        await datenbank.AuftragHinzufuegen("Auftrag", 1, 2, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.NotEqual(
            vorschlaege[0].Zuweisungen[0].Mitarbeiter?.Id,
            vorschlaege[1].Zuweisungen[0].Mitarbeiter?.Id);
    }

    [Fact]
    public async Task AlternativeVerwendetKeinenGesperrtenMitarbeiter()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        var erster = await datenbank.MitarbeiterHinzufuegen("Eins", true, qualifikation);
        var zweiter = await datenbank.MitarbeiterHinzufuegen("Zwei", true, qualifikation);
        var gesperrt = await datenbank.MitarbeiterHinzufuegen("Gesperrt", false, qualifikation);
        await datenbank.AuftragHinzufuegen("Auftrag", 1, 2, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);
        var alternative = vorschlaege[2].Zuweisungen[0].Mitarbeiter;

        Assert.NotNull(alternative);
        Assert.NotEqual(gesperrt.Id, alternative.Id);
        Assert.True(alternative.Id == erster.Id || alternative.Id == zweiter.Id);
    }

    [Fact]
    public async Task WenigerAusgelasteterMitarbeiterWirdBevorzugt()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        var belastet = await datenbank.MitarbeiterHinzufuegen("Belastet", true, qualifikation);
        var frei = await datenbank.MitarbeiterHinzufuegen("Frei", true, qualifikation);
        await datenbank.AuftragHinzufuegen("Einsatz 1", 1, 1, belastet, qualifikation);
        await datenbank.AuftragHinzufuegen("Einsatz 2", 3, 3, belastet, qualifikation);
        await datenbank.AuftragHinzufuegen("Einsatz 3", 5, 5, belastet, qualifikation);
        await datenbank.AuftragHinzufuegen("Offen", 7, 7, null, qualifikation);

        var vorschlaege = await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum);

        Assert.Equal(frei.Id, vorschlaege[0].Zuweisungen[0].Mitarbeiter?.Id);
    }

    [Fact]
    public async Task VorschlagUebernehmenOrdnetRichtigenMitarbeiterZu()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        var mitarbeiter = await datenbank.MitarbeiterHinzufuegen("Passend", true, qualifikation);
        var auftrag = await datenbank.AuftragHinzufuegen("Offen", 1, 2, null, qualifikation);
        var vorschlag = (await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum))[0];

        var uebernommen = await datenbank.Service.VorschlagUebernehmenAsync(vorschlag);
        var gespeichert = await datenbank.Context.Auftraege
            .Include(a => a.Mitarbeiter)
            .FirstAsync(a => a.Id == auftrag.Id);

        Assert.True(uebernommen);
        Assert.Equal(mitarbeiter.Id, Assert.Single(gespeichert.Mitarbeiter).Id);
    }

    [Fact]
    public async Task NeuerKonfliktLehntUebernahmeVollstaendigAb()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        var mitarbeiter = await datenbank.MitarbeiterHinzufuegen("Passend", true, qualifikation);
        var offenerAuftrag = await datenbank.AuftragHinzufuegen("Offen", 1, 3, null, qualifikation);
        var vorschlag = (await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum))[0];
        await datenbank.AuftragHinzufuegen("Neuer Konflikt", 2, 4, mitarbeiter, qualifikation);

        var uebernommen = await datenbank.Service.VorschlagUebernehmenAsync(vorschlag);
        var gespeichert = await datenbank.Context.Auftraege
            .Include(a => a.Mitarbeiter)
            .FirstAsync(a => a.Id == offenerAuftrag.Id);

        Assert.False(uebernommen);
        Assert.Empty(gespeichert.Mitarbeiter);
    }

    [Fact]
    public async Task BestehendeZuweisungWirdBeiUebernahmeNichtGeloescht()
    {
        using var datenbank = new Testdatenbank();
        var qualifikation = await datenbank.QualifikationHinzufuegen("Elektro");
        var vorgeschlagen = await datenbank.MitarbeiterHinzufuegen("Vorgeschlagen", true, qualifikation);
        var bereitsZugewiesen = await datenbank.MitarbeiterHinzufuegen("Bestehend", true, qualifikation);
        var auftrag = await datenbank.AuftragHinzufuegen("Offen", 1, 2, null, qualifikation);
        var vorschlag = (await datenbank.Service.ErstelleVorschlaegeAsync(Startdatum))[0];
        await datenbank.MitarbeiterZuweisen(auftrag, bereitsZugewiesen);

        var uebernommen = await datenbank.Service.VorschlagUebernehmenAsync(vorschlag);
        var gespeichert = await datenbank.Context.Auftraege
            .Include(a => a.Mitarbeiter)
            .FirstAsync(a => a.Id == auftrag.Id);

        Assert.False(uebernommen);
        Assert.Equal(bereitsZugewiesen.Id, Assert.Single(gespeichert.Mitarbeiter).Id);
        Assert.DoesNotContain(gespeichert.Mitarbeiter, m => m.Id == vorgeschlagen.Id);
    }

    [Fact]
    public void VorschlagAuswaehlenVerwendetNurGewaehlteNummer()
    {
        using var datenbank = new Testdatenbank();
        var vorschlaege = Testvorschlaege();

        var ausgewaehlt = datenbank.Service.VorschlagAuswaehlen(vorschlaege, 1);

        Assert.Same(vorschlaege[0], ausgewaehlt);
    }

    [Fact]
    public void AusgeglichenKannDirektAusgewaehltWerden()
    {
        using var datenbank = new Testdatenbank();
        var vorschlaege = Testvorschlaege();

        var ausgewaehlt = datenbank.Service.VorschlagAuswaehlen(vorschlaege, 2);

        Assert.Equal("Ausgeglichen", ausgewaehlt?.Name);
        Assert.Same(vorschlaege[1], ausgewaehlt);
    }

    [Fact]
    public void AlternativeKannDirektAusgewaehltWerden()
    {
        using var datenbank = new Testdatenbank();
        var vorschlaege = Testvorschlaege();

        var ausgewaehlt = datenbank.Service.VorschlagAuswaehlen(vorschlaege, 3);

        Assert.Equal("Alternative", ausgewaehlt?.Name);
        Assert.Same(vorschlaege[2], ausgewaehlt);
    }

    [Fact]
    public void TagesgruppenSindChronologischSortiert()
    {
        using var datenbank = new Testdatenbank();
        var vorschlag = VorschlagMitZuweisungen(
            Zuweisung("Spät", 5, 5, NeuerTestmitarbeiter(1)),
            Zuweisung("Früh", 1, 1, NeuerTestmitarbeiter(2)),
            Zuweisung("Mitte", 3, 3, NeuerTestmitarbeiter(3)));

        var tage = datenbank.Service.TagesgruppenErstellen(vorschlag);

        Assert.Equal(Startdatum, tage[0].Datum);
        Assert.Equal(Startdatum.AddDays(2), tage[1].Datum);
        Assert.Equal(Startdatum.AddDays(4), tage[2].Datum);
    }

    [Fact]
    public void EintaegigerAuftragErscheintAnGenauEinemTag()
    {
        using var datenbank = new Testdatenbank();
        var vorschlag = VorschlagMitZuweisungen(
            Zuweisung("Eintägig", 2, 2, NeuerTestmitarbeiter(1)));

        var tage = datenbank.Service.TagesgruppenErstellen(vorschlag);

        Assert.Single(tage);
        Assert.Single(tage[0].Zuweisungen);
    }

    [Fact]
    public void MehrtaegigerAuftragErscheintInVierTagesgruppen()
    {
        using var datenbank = new Testdatenbank();
        var zuweisung = Zuweisung("Mehrtägig", 8, 11, NeuerTestmitarbeiter(1));
        var vorschlag = VorschlagMitZuweisungen(zuweisung);

        var tage = datenbank.Service.TagesgruppenErstellen(vorschlag);

        Assert.Equal(4, tage.Count);
        Assert.All(tage, tag => Assert.Same(zuweisung, Assert.Single(tag.Zuweisungen)));
    }

    [Fact]
    public void TagesaufbereitungErzeugtKeineWeitereDatenbankzuweisung()
    {
        using var datenbank = new Testdatenbank();
        var mitarbeiter = NeuerTestmitarbeiter(1);
        var zuweisung = Zuweisung("Mehrtägig", 8, 11, mitarbeiter);
        var vorschlag = VorschlagMitZuweisungen(zuweisung);

        datenbank.Service.TagesgruppenErstellen(vorschlag);

        Assert.Empty(zuweisung.Auftrag.Mitarbeiter);
    }

    [Fact]
    public void MehrereAuftraegeAmSelbenTagBleibenInEinerTagesgruppe()
    {
        using var datenbank = new Testdatenbank();
        var vorschlag = VorschlagMitZuweisungen(
            Zuweisung("A", 2, 2, NeuerTestmitarbeiter(1)),
            Zuweisung("B", 2, 2, NeuerTestmitarbeiter(2)),
            Zuweisung("C", 2, 3, NeuerTestmitarbeiter(3)));

        var tage = datenbank.Service.TagesgruppenErstellen(vorschlag);

        Assert.Equal(3, tage[0].Zuweisungen.Count);
        Assert.Equal(Startdatum.AddDays(1), tage[0].Datum);
    }

    [Fact]
    public void UnbesetzterAuftragBleibtInTagesgruppeSichtbar()
    {
        using var datenbank = new Testdatenbank();
        var unbesetzt = Zuweisung("Unbesetzt", 1, 1, null);
        var vorschlag = VorschlagMitZuweisungen(unbesetzt);

        var tage = datenbank.Service.TagesgruppenErstellen(vorschlag);

        Assert.Same(unbesetzt, Assert.Single(Assert.Single(tage).Zuweisungen));
        Assert.Null(unbesetzt.Mitarbeiter);
    }

    [Fact]
    public void ZusammenfassungZaehltAuftraegeMitarbeiterUndUnbesetzte()
    {
        using var datenbank = new Testdatenbank();
        var erster = NeuerTestmitarbeiter(1);
        var zweiter = NeuerTestmitarbeiter(2);
        var vorschlag = VorschlagMitZuweisungen(
            Zuweisung("A", 1, 1, erster),
            Zuweisung("B", 2, 2, erster),
            Zuweisung("C", 3, 3, zweiter),
            Zuweisung("D", 4, 4, null));

        var zusammenfassung = datenbank.Service.ZusammenfassungErstellen(vorschlag);

        Assert.Equal(4, zusammenfassung.Auftraege);
        Assert.Equal(2, zusammenfassung.Mitarbeiter);
        Assert.Equal(1, zusammenfassung.Unbesetzt);
    }

    private static List<MatchingVorschlag> Testvorschlaege()
    {
        return
        [
            new MatchingVorschlag { Nummer = 1, Name = "Beste Qualifikation" },
            new MatchingVorschlag { Nummer = 2, Name = "Ausgeglichen" },
            new MatchingVorschlag { Nummer = 3, Name = "Alternative" }
        ];
    }

    private static MatchingVorschlag VorschlagMitZuweisungen(
        params MatchingZuweisung[] zuweisungen)
    {
        var vorschlag = new MatchingVorschlag { Nummer = 1, Name = "Test" };

        foreach (var zuweisung in zuweisungen)
        {
            vorschlag.Zuweisungen.Add(zuweisung);
        }

        return vorschlag;
    }

    private static MatchingZuweisung Zuweisung(
        string titel,
        int starttag,
        int endtag,
        Mitarbeiter? mitarbeiter)
    {
        return new MatchingZuweisung
        {
            Auftrag = new Auftrag
            {
                Titel = titel,
                Startdatum = Startdatum.AddDays(starttag - 1),
                Enddatum = Startdatum.AddDays(endtag - 1)
            },
            Mitarbeiter = mitarbeiter
        };
    }

    private static Mitarbeiter NeuerTestmitarbeiter(int id)
    {
        return new Mitarbeiter
        {
            Id = id,
            Personalnummer = "P" + id,
            Vorname = "Test",
            Nachname = id.ToString()
        };
    }

    private sealed class Testdatenbank : IDisposable
    {
        private readonly SqliteConnection verbindung;

        public ApplicationDbContext Context { get; }
        public MatchingService Service { get; }

        public Testdatenbank()
        {
            verbindung = new SqliteConnection("Data Source=:memory:");
            verbindung.Open();
            var optionen = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(verbindung)
                .Options;
            Context = new ApplicationDbContext(optionen);
            Context.Database.EnsureCreated();
            Service = new MatchingService(Context);
        }

        public async Task<Qualifikation> QualifikationHinzufuegen(string name)
        {
            var qualifikation = new Qualifikation { Name = name };
            Context.Qualifikationen.Add(qualifikation);
            await Context.SaveChangesAsync();
            return qualifikation;
        }

        public async Task<Mitarbeiter> MitarbeiterHinzufuegen(
            string vorname,
            bool verfuegbar,
            params Qualifikation[] qualifikationen)
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = Guid.NewGuid().ToString(),
                Vorname = vorname,
                Nachname = "Test",
                Verfuegbar = verfuegbar
            };

            foreach (var qualifikation in qualifikationen)
            {
                mitarbeiter.Qualifikationen.Add(qualifikation);
            }

            Context.Mitarbeiter.Add(mitarbeiter);
            await Context.SaveChangesAsync();
            return mitarbeiter;
        }

        public async Task<Auftrag> AuftragHinzufuegen(
            string titel,
            int starttag,
            int endtag,
            Mitarbeiter? mitarbeiter,
            params Qualifikation[] qualifikationen)
        {
            var auftrag = new Auftrag
            {
                Titel = titel,
                Startdatum = Startdatum.AddDays(starttag - 1),
                Enddatum = Startdatum.AddDays(endtag - 1),
                Kunde = new Kunde { Firmenname = "Testkunde " + titel }
            };

            foreach (var qualifikation in qualifikationen)
            {
                auftrag.Qualifikationen.Add(qualifikation);
            }

            if (mitarbeiter != null)
            {
                auftrag.Mitarbeiter.Add(mitarbeiter);
                auftrag.Besetzt = true;
            }

            Context.Auftraege.Add(auftrag);
            await Context.SaveChangesAsync();
            return auftrag;
        }

        public async Task AbwesenheitHinzufuegen(
            Mitarbeiter mitarbeiter,
            int starttag,
            int endtag,
            string status)
        {
            Context.Abwesenheiten.Add(new Abwesenheit
            {
                MitarbeiterId = mitarbeiter.Id,
                Von = Startdatum.AddDays(starttag - 1),
                Bis = Startdatum.AddDays(endtag - 1),
                Typ = "Urlaub",
                Status = status
            });
            await Context.SaveChangesAsync();
        }

        public async Task MitarbeiterZuweisen(Auftrag auftrag, Mitarbeiter mitarbeiter)
        {
            auftrag.Mitarbeiter.Add(mitarbeiter);
            auftrag.Besetzt = true;
            await Context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Context.Dispose();
            verbindung.Dispose();
        }
    }
}
