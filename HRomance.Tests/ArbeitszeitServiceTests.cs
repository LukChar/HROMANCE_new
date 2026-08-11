using HRomance.Data;
using HRomance.Models;
using HRomance.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRomance.Tests;

public class ArbeitszeitServiceTests
{
    [Theory]
    [InlineData(8, 0, "08:00")]
    [InlineData(12, 0, "12:00")]
    [InlineData(13, 0, "13:00")]
    [InlineData(16, 30, "16:30")]
    [InlineData(23, 45, "23:45")]
    public void UhrzeitWirdImVierundzwanzigStundenFormatAngezeigt(
        int stunde,
        int minute,
        string erwartet)
    {
        using var testdatenbank = new Testdatenbank();

        var anzeige = testdatenbank.Service.ZeitAnzeigen(new TimeOnly(stunde, minute));

        Assert.Equal(erwartet, anzeige);
        Assert.DoesNotContain("AM", anzeige);
        Assert.DoesNotContain("PM", anzeige);
    }

    [Fact]
    public void ZeitraumWirdEinheitlichImVierundzwanzigStundenFormatAngezeigt()
    {
        using var testdatenbank = new Testdatenbank();

        var anzeige = testdatenbank.Service.ZeitraumAnzeigen(
            new TimeOnly(8, 0),
            new TimeOnly(16, 30));

        Assert.Equal("08:00 - 16:30", anzeige);
        Assert.DoesNotContain("AM", anzeige);
        Assert.DoesNotContain("PM", anzeige);
    }

    [Theory]
    [InlineData("08:00", 8, 0)]
    [InlineData("16:00", 16, 0)]
    [InlineData("23:59", 23, 59)]
    public void GueltigeZeittexteWerdenGelesen(string text, int stunde, int minute)
    {
        using var testdatenbank = new Testdatenbank();

        var istGueltig = testdatenbank.Service.TryParseZeit(text, out var zeit);

        Assert.True(istGueltig);
        Assert.Equal(new TimeOnly(stunde, minute), zeit);
    }

    [Theory]
    [InlineData("8:00 AM")]
    [InlineData("04:00 PM")]
    [InlineData("25:00")]
    [InlineData("12:70")]
    [InlineData("8")]
    [InlineData("16")]
    public void UngueltigeZeittexteWerdenAbgelehnt(string text)
    {
        using var testdatenbank = new Testdatenbank();

        var istGueltig = testdatenbank.Service.TryParseZeit(text, out _);

        Assert.False(istGueltig);
    }

    [Fact]
    public void BestehendeZeitWirdAlsFormulartextAngezeigt()
    {
        using var testdatenbank = new Testdatenbank();

        var text = testdatenbank.Service.ZeitAnzeigen(new TimeOnly(16, 0));

        Assert.Equal("16:00", text);
    }

    [Fact]
    public void FormulartextWirdZurBestehendenZeitZurueckgewandelt()
    {
        using var testdatenbank = new Testdatenbank();

        var istGueltig = testdatenbank.Service.TryParseZeit("16:30", out var zeit);

        Assert.True(istGueltig);
        Assert.Equal(new TimeOnly(16, 30), zeit);
    }

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
    public async Task ZukuenftigerArbeitszeiteintragZaehltNochNichtZumMonatssaldo()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen();
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 11, 8, 0, 16, 30, 0);
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 12, 8, 0, 15, 0, 0);

        var werte = await testdatenbank.Service.GetMonatswerteAsync(
            mitarbeiter.Id, 2026, 8, 40, new DateTime(2026, 8, 11));

        Assert.Equal(8.5, werte.Ist);
        Assert.Equal(56, werte.Soll);
        Assert.Equal(-47.5, werte.Saldo);
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
    public async Task MonatssollDesAktuellenMonatsEndetHeute()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen();
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 11, 8, 0, 12, 0, 0);
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 11, 13, 0, 17, 0, 0);
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 12, 8, 0, 16, 0, 0);

        var werte = await testdatenbank.Service.GetMonatswerteAsync(
            mitarbeiter.Id, 2026, 8, 40, new DateTime(2026, 8, 11));

        Assert.Equal(56, werte.Soll);
    }

    [Fact]
    public void VierzigWochenstundenErgebenAchtSollstundenProTag()
    {
        using var testdatenbank = new Testdatenbank();

        var sollstunden = testdatenbank.Service.BerechneSollstundenProTag(40);

        Assert.Equal(8, sollstunden);
    }

    [Fact]
    public void AchtunddreissigKommaFuenfWochenstundenErgebenSiebenKommaSiebenProTag()
    {
        using var testdatenbank = new Testdatenbank();

        var sollstunden = testdatenbank.Service.BerechneSollstundenProTag(38.5);

        Assert.Equal(7.7, sollstunden);
    }

    [Fact]
    public void ZwanzigWerktageErgebenHundertsechzigSollstunden()
    {
        using var testdatenbank = new Testdatenbank();

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 2, 40, new DateTime(2026, 3, 1));

        Assert.Equal(160, sollstunden);
    }

    [Fact]
    public void AktuellerMonatZaehltArbeitstageNurBisHeute()
    {
        using var testdatenbank = new Testdatenbank();

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 8, 40, new DateTime(2026, 8, 11));

        Assert.Equal(56, sollstunden);
    }

    [Fact]
    public void SpaetereArbeitstageDesAktuellenMonatsWerdenNichtGezaehlt()
    {
        using var testdatenbank = new Testdatenbank();

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 8, 40, new DateTime(2026, 8, 11));

        Assert.NotEqual(168, sollstunden);
        Assert.Equal(56, sollstunden);
    }

    [Fact]
    public void SamstagUndSonntagWerdenNichtGezaehlt()
    {
        using var testdatenbank = new Testdatenbank();

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 8, 40, new DateTime(2026, 8, 9));

        Assert.Equal(40, sollstunden);
    }

    [Fact]
    public void VergangenerMonatWirdVollstaendigBerechnet()
    {
        using var testdatenbank = new Testdatenbank();

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 7, 40, new DateTime(2026, 8, 11));

        Assert.Equal(184, sollstunden);
    }

    [Fact]
    public void ZukuenftigerMonatHatKeineSollstunden()
    {
        using var testdatenbank = new Testdatenbank();

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 9, 40, new DateTime(2026, 8, 11));

        Assert.Equal(0, sollstunden);
    }

    [Fact]
    public void VierzigIststundenUndAchtundvierzigSollstundenErgebenMinusAcht()
    {
        using var testdatenbank = new Testdatenbank();

        var saldo = testdatenbank.Service.BerechneSaldo(40, 48);

        Assert.Equal(-8, saldo);
    }

    [Fact]
    public void ZweiundfuenfzigIststundenUndAchtundvierzigSollstundenErgebenPlusVier()
    {
        using var testdatenbank = new Testdatenbank();

        var saldo = testdatenbank.Service.BerechneSaldo(52, 48);

        Assert.Equal(4, saldo);
    }

    [Fact]
    public void GenehmigterUrlaubReduziertTagessollAufNull()
    {
        using var testdatenbank = new Testdatenbank();
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 3, 3, "Urlaub", "Genehmigt")
        };

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 8, 40, new DateTime(2026, 8, 3), abwesenheiten, 1);

        Assert.Equal(0, sollstunden);
    }

    [Fact]
    public void NormalerArbeitstagHatAchtSollstunden()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeiten = new List<Arbeitszeit> { ArbeitszeitAmTag(1, 3, 8) };

        var auswertung = testdatenbank.Service.BerechneMonatsauswertung(
            arbeitszeiten, [], 1, 2026, 8, 40, new DateTime(2026, 8, 3));

        Assert.Equal(8, auswertung.Arbeitszeit);
        Assert.Equal(8, auswertung.Soll);
        Assert.Equal(0, auswertung.Saldo);
    }

    [Fact]
    public void UrlaubOhneArbeitszeitErgibtNullsaldo()
    {
        using var testdatenbank = new Testdatenbank();
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 3, 3, "Urlaub", "Genehmigt")
        };

        var auswertung = testdatenbank.Service.BerechneMonatsauswertung(
            [], abwesenheiten, 1, 2026, 8, 40, new DateTime(2026, 8, 3));

        Assert.Equal(0, auswertung.Arbeitszeit);
        Assert.Equal(0, auswertung.Soll);
        Assert.Equal(1, auswertung.Abwesenheitstage);
        Assert.Equal(0, auswertung.Saldo);
    }

    [Fact]
    public void ArbeitAmGenehmigtenUrlaubstagWirdIgnoriert()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeiten = new List<Arbeitszeit> { ArbeitszeitAmTag(1, 3, 8) };
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 3, 3, "Urlaub", "Genehmigt")
        };

        var auswertung = testdatenbank.Service.BerechneMonatsauswertung(
            arbeitszeiten, abwesenheiten, 1, 2026, 8, 40, new DateTime(2026, 8, 3));

        Assert.Equal(0, auswertung.Arbeitszeit);
        Assert.Equal(0, auswertung.Soll);
        Assert.Equal(1, auswertung.Abwesenheitstage);
        Assert.Equal(0, auswertung.Saldo);
    }

    [Fact]
    public void OffenerUrlaubReduziertSollNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeiten = new List<Arbeitszeit> { ArbeitszeitAmTag(1, 3, 8) };
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 3, 3, "Urlaub", "Offen")
        };

        var auswertung = testdatenbank.Service.BerechneMonatsauswertung(
            arbeitszeiten, abwesenheiten, 1, 2026, 8, 40, new DateTime(2026, 8, 3));

        Assert.Equal(8, auswertung.Arbeitszeit);
        Assert.Equal(8, auswertung.Soll);
    }

    [Fact]
    public void AbgelehnterUrlaubReduziertSollNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeiten = new List<Arbeitszeit> { ArbeitszeitAmTag(1, 3, 8) };
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 3, 3, "Urlaub", "Abgelehnt")
        };

        var auswertung = testdatenbank.Service.BerechneMonatsauswertung(
            arbeitszeiten, abwesenheiten, 1, 2026, 8, 40, new DateTime(2026, 8, 3));

        Assert.Equal(8, auswertung.Arbeitszeit);
        Assert.Equal(8, auswertung.Soll);
    }

    [Fact]
    public void UrlaubVonMontagBisFreitagReduziertGesamtesSoll()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeiten = new List<Arbeitszeit>
        {
            ArbeitszeitAmTag(1, 3, 8),
            ArbeitszeitAmTag(1, 4, 8),
            ArbeitszeitAmTag(1, 5, 8),
            ArbeitszeitAmTag(1, 6, 8),
            ArbeitszeitAmTag(1, 7, 8)
        };
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 3, 7, "Urlaub", "Genehmigt")
        };

        var auswertung = testdatenbank.Service.BerechneMonatsauswertung(
            arbeitszeiten, abwesenheiten, 1, 2026, 8, 40, new DateTime(2026, 8, 7));

        Assert.Equal(0, auswertung.Arbeitszeit);
        Assert.Equal(0, auswertung.Soll);
        Assert.Equal(5, auswertung.Abwesenheitstage);
    }

    [Fact]
    public void UrlaubAmWochenendeVeraendertSollNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 8, 9, "Urlaub", "Genehmigt")
        };

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 8, 40, new DateTime(2026, 8, 9), abwesenheiten, 1);

        Assert.Equal(40, sollstunden);
    }

    [Fact]
    public void UrlaubUeberMonatsgrenzeReduziertNurAugusttage()
    {
        using var testdatenbank = new Testdatenbank();
        var abwesenheiten = new List<Abwesenheit>
        {
            new Abwesenheit
            {
                MitarbeiterId = 1,
                Von = new DateTime(2026, 7, 30),
                Bis = new DateTime(2026, 8, 3),
                Typ = "Urlaub",
                Status = "Genehmigt"
            }
        };

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 8, 40, new DateTime(2026, 8, 5), abwesenheiten, 1);

        Assert.Equal(16, sollstunden);
    }

    [Fact]
    public void ZukuenftigerUrlaubVeraendertAktuellesSollNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 20, 22, "Urlaub", "Genehmigt")
        };

        var sollstunden = testdatenbank.Service.BerechneMonatssoll(
            2026, 8, 40, new DateTime(2026, 8, 11), abwesenheiten, 1);

        Assert.Equal(56, sollstunden);
    }

    [Fact]
    public void GenehmigterZeitausgleichReduziertSollNicht()
    {
        using var testdatenbank = new Testdatenbank();
        var arbeitszeiten = new List<Arbeitszeit> { ArbeitszeitAmTag(1, 3, 8) };
        var abwesenheiten = new List<Abwesenheit>
        {
            AbwesenheitVonBis(1, 3, 3, "Zeitausgleich", "Genehmigt")
        };

        var auswertung = testdatenbank.Service.BerechneMonatsauswertung(
            arbeitszeiten, abwesenheiten, 1, 2026, 8, 40, new DateTime(2026, 8, 3));

        Assert.Equal(8, auswertung.Arbeitszeit);
        Assert.Equal(8, auswertung.Soll);
        Assert.Equal(0, auswertung.Abwesenheitstage);
    }

    [Fact]
    public void MehrstundenErgebenPositivenMonatssaldo()
    {
        using var testdatenbank = new Testdatenbank();

        var saldo = testdatenbank.Service.BerechneSaldo(168.5, 160);

        Assert.Equal(8.5, saldo);
    }

    [Fact]
    public void MinderstundenErgebenNegativenMonatssaldo()
    {
        using var testdatenbank = new Testdatenbank();

        var saldo = testdatenbank.Service.BerechneSaldo(152.5, 160);

        Assert.Equal(-7.5, saldo);
    }

    [Fact]
    public void GleicheIstUndSollstundenErgebenNullsaldo()
    {
        using var testdatenbank = new Testdatenbank();

        var saldo = testdatenbank.Service.BerechneSaldo(160, 160);

        Assert.Equal(0, saldo);
    }

    [Fact]
    public async Task MitarbeiterdetailsVerwendenDieselbeMonatsauswertungWieDashboard()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = await testdatenbank.MitarbeiterHinzufuegen();
        await testdatenbank.ArbeitszeitHinzufuegen(mitarbeiter.Id, 3, 8, 0, 16, 0, 0);
        await testdatenbank.AbwesenheitHinzufuegen(mitarbeiter.Id, 4, "Urlaub", "Genehmigt");
        var heute = new DateTime(2026, 8, 4);

        var detailsAuswertung = await testdatenbank.Service.GetMonatsauswertungAsync(
            mitarbeiter.Id, 2026, 8, mitarbeiter.Wochenarbeitszeit, heute);
        var dashboardAuswertung = testdatenbank.Service.BerechneMonatsauswertung(
            [ArbeitszeitAmTag(mitarbeiter.Id, 3, 8)],
            [AbwesenheitVonBis(mitarbeiter.Id, 4, 4, "Urlaub", "Genehmigt")],
            mitarbeiter.Id,
            2026,
            8,
            mitarbeiter.Wochenarbeitszeit,
            heute);

        Assert.Equal(dashboardAuswertung, detailsAuswertung);
        Assert.Equal(1, detailsAuswertung.Abwesenheitstage);
    }

    [Fact]
    public async Task WochenarbeitszeitWirdGespeichertUndGeladen()
    {
        using var testdatenbank = new Testdatenbank();
        var mitarbeiter = new Mitarbeiter
        {
            Personalnummer = Guid.NewGuid().ToString(),
            Vorname = "Fritz",
            Nachname = "Schreiner",
            Wochenarbeitszeit = 38.5
        };

        await testdatenbank.MitarbeiterService.AddAsync(mitarbeiter);
        var gespeichert = await testdatenbank.MitarbeiterService.GetByIdAsync(mitarbeiter.Id);

        Assert.NotNull(gespeichert);
        Assert.Equal(38.5, gespeichert.Wochenarbeitszeit);
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

    private static Abwesenheit AbwesenheitVonBis(
        int mitarbeiterId,
        int vonTag,
        int bisTag,
        string typ,
        string status)
    {
        return new Abwesenheit
        {
            MitarbeiterId = mitarbeiterId,
            Von = new DateTime(2026, 8, vonTag),
            Bis = new DateTime(2026, 8, bisTag),
            Typ = typ,
            Status = status
        };
    }

    private static Arbeitszeit ArbeitszeitAmTag(int mitarbeiterId, int tag, int stunden)
    {
        return new Arbeitszeit
        {
            MitarbeiterId = mitarbeiterId,
            Datum = new DateTime(2026, 8, tag),
            Beginn = new TimeOnly(8, 0),
            Ende = new TimeOnly(8 + stunden, 0)
        };
    }

    private sealed class Testdatenbank : IDisposable
    {
        private readonly SqliteConnection verbindung;
        private readonly ApplicationDbContext context;

        public ArbeitszeitService Service { get; }
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
            Service = new ArbeitszeitService(context);
            MitarbeiterService = new MitarbeiterService(context);
        }

        public async Task<Mitarbeiter> MitarbeiterHinzufuegen()
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = Guid.NewGuid().ToString(),
                Vorname = "Test",
                Nachname = "Person",
                SollStundenProTag = 8,
                Wochenarbeitszeit = 40
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

        public async Task AbwesenheitHinzufuegen(
            int mitarbeiterId,
            int tag,
            string typ,
            string status)
        {
            context.Abwesenheiten.Add(new Abwesenheit
            {
                MitarbeiterId = mitarbeiterId,
                Von = new DateTime(2026, 8, tag),
                Bis = new DateTime(2026, 8, tag),
                Typ = typ,
                Status = status
            });

            await context.SaveChangesAsync();
        }

        public void Dispose()
        {
            context.Dispose();
            verbindung.Dispose();
        }
    }
}
