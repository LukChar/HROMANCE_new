using HRomance.Models;
using HRomance.Services;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Tests;

public class MatchingServiceTests
{
    private static readonly DateTime Startdatum = new(2026, 10, 1);

    [Fact]
    public async Task ErstelleVorschlaegeAsync_GibtDreiVorschlaegeZurueck()
    {
        var name = TestDatenbank.NeuerName();

        using var context = TestDatenbank.NeuerContext(name);
        var qualifikationA = new Qualifikation { Name = "A" };
        var qualifikationB = new Qualifikation { Name = "B" };
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Auftrag Eins",
            Kunde = new Kunde { Firmenname = "Test GmbH" },
            Startdatum = new DateTime(2026, 10, 2),
            Enddatum = new DateTime(2026, 10, 3),
            Qualifikationen = { qualifikationA }
        });
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Auftrag Zwei",
            Kunde = new Kunde { Firmenname = "Test GmbH" },
            Startdatum = new DateTime(2026, 10, 6),
            Enddatum = new DateTime(2026, 10, 7),
            Qualifikationen = { qualifikationB }
        });
        context.Mitarbeiter.Add(new Mitarbeiter
        {
            Personalnummer = "P-001",
            Vorname = "Anna",
            Nachname = "Berger",
            Qualifikationen = { qualifikationA }
        });
        context.Mitarbeiter.Add(new Mitarbeiter
        {
            Personalnummer = "P-002",
            Vorname = "Max",
            Nachname = "Muster",
            Qualifikationen = { qualifikationB }
        });
        context.Mitarbeiter.Add(new Mitarbeiter
        {
            Personalnummer = "P-003",
            Vorname = "Lisa",
            Nachname = "Moser",
            Qualifikationen = { qualifikationA, qualifikationB }
        });
        await context.SaveChangesAsync();

        var service = new MatchingService(context);

        var vorschlaege = await service.ErstelleVorschlaegeAsync(Startdatum, 14);

        Assert.Equal(3, vorschlaege.Count);
        Assert.Equal(1, vorschlaege[0].Nummer);
        Assert.Equal("Beste Qualifikation", vorschlaege[0].Name);
        Assert.Equal(2, vorschlaege[1].Nummer);
        Assert.Equal("Ausgeglichen", vorschlaege[1].Name);
        Assert.Equal(3, vorschlaege[2].Nummer);
        Assert.Equal("Alternative", vorschlaege[2].Name);
    }

    [Fact]
    public async Task ErstelleVorschlaegeAsync_GibtLeereListeZurueckWennKeineOffenenAuftraege()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new MatchingService(context);

        var vorschlaege = await service.ErstelleVorschlaegeAsync(Startdatum, 14);

        Assert.Empty(vorschlaege);
    }

    [Fact]
    public async Task VorschlagEins_WaehltMitarbeiterMitMeistenPassendenQualifikationen()
    {
        var name = TestDatenbank.NeuerName();

        using var context = TestDatenbank.NeuerContext(name);
        var qualifikationA = new Qualifikation { Name = "A" };
        var qualifikationB = new Qualifikation { Name = "B" };
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Auftrag Eins",
            Kunde = new Kunde { Firmenname = "Test GmbH" },
            Startdatum = new DateTime(2026, 10, 2),
            Enddatum = new DateTime(2026, 10, 3),
            Qualifikationen = { qualifikationA, qualifikationB }
        });
        context.Mitarbeiter.Add(new Mitarbeiter
        {
            Personalnummer = "P-001",
            Vorname = "Xaver",
            Nachname = "Eder",
            Qualifikationen = { qualifikationA }
        });
        var mitarbeiterY = new Mitarbeiter
        {
            Personalnummer = "P-002",
            Vorname = "Yvonne",
            Nachname = "Faber",
            Qualifikationen = { qualifikationA, qualifikationB }
        };
        context.Mitarbeiter.Add(mitarbeiterY);
        await context.SaveChangesAsync();

        var service = new MatchingService(context);

        var vorschlaege = await service.ErstelleVorschlaegeAsync(Startdatum, 14);
        var vorschlagEins = service.VorschlagAuswaehlen(vorschlaege, 1);

        Assert.NotNull(vorschlagEins);
        var zuweisung = Assert.Single(vorschlagEins.Zuweisungen);
        Assert.NotNull(zuweisung.Mitarbeiter);
        Assert.Equal(mitarbeiterY.Id, zuweisung.Mitarbeiter.Id);
    }

    [Fact]
    public async Task VorschlagZwei_BevorzugtMitarbeiterMitWenigerEinsaetzen()
    {
        var name = TestDatenbank.NeuerName();

        using var context = TestDatenbank.NeuerContext(name);
        var qualifikationA = new Qualifikation { Name = "A" };
        var mitarbeiterX = new Mitarbeiter
        {
            Personalnummer = "P-001",
            Vorname = "Xaver",
            Nachname = "Eder",
            Qualifikationen = { qualifikationA }
        };
        var mitarbeiterY = new Mitarbeiter
        {
            Personalnummer = "P-002",
            Vorname = "Yvonne",
            Nachname = "Faber",
            Qualifikationen = { qualifikationA }
        };
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Offener Auftrag",
            Kunde = new Kunde { Firmenname = "Test GmbH" },
            Startdatum = new DateTime(2026, 10, 1),
            Enddatum = new DateTime(2026, 10, 2),
            Qualifikationen = { qualifikationA }
        });
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Bestehend Eins",
            Startdatum = new DateTime(2026, 10, 5),
            Enddatum = new DateTime(2026, 10, 6),
            Mitarbeiter = { mitarbeiterX }
        });
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Bestehend Zwei",
            Startdatum = new DateTime(2026, 10, 7),
            Enddatum = new DateTime(2026, 10, 8),
            Mitarbeiter = { mitarbeiterX }
        });
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Bestehend Drei",
            Startdatum = new DateTime(2026, 10, 9),
            Enddatum = new DateTime(2026, 10, 10),
            Mitarbeiter = { mitarbeiterX }
        });
        context.Mitarbeiter.Add(mitarbeiterY);
        await context.SaveChangesAsync();

        var service = new MatchingService(context);

        var vorschlaege = await service.ErstelleVorschlaegeAsync(Startdatum, 14);
        var vorschlagZwei = service.VorschlagAuswaehlen(vorschlaege, 2);

        Assert.NotNull(vorschlagZwei);
        var zuweisung = Assert.Single(vorschlagZwei.Zuweisungen);
        Assert.NotNull(zuweisung.Mitarbeiter);
        Assert.Equal(mitarbeiterY.Id, zuweisung.Mitarbeiter.Id);
    }

    [Fact]
    public async Task ManuellNichtVerfuegbarerMitarbeiter_WirdNichtZugewiesen()
    {
        var name = TestDatenbank.NeuerName();

        using var context = TestDatenbank.NeuerContext(name);
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Offener Auftrag",
            Kunde = new Kunde { Firmenname = "Test GmbH" },
            Startdatum = new DateTime(2026, 10, 2),
            Enddatum = new DateTime(2026, 10, 3)
        });
        context.Mitarbeiter.Add(new Mitarbeiter
        {
            Personalnummer = "P-001",
            Vorname = "Anna",
            Nachname = "Berger",
            Verfuegbar = false
        });
        await context.SaveChangesAsync();

        var service = new MatchingService(context);

        var vorschlaege = await service.ErstelleVorschlaegeAsync(Startdatum, 14);

        Assert.All(vorschlaege, vorschlag =>
            Assert.All(vorschlag.Zuweisungen, zuweisung =>
                Assert.Null(zuweisung.Mitarbeiter)));
    }

    [Fact]
    public async Task MitarbeiterMitGenehmigterAbwesenheit_WirdNichtZugewiesen()
    {
        var name = TestDatenbank.NeuerName();

        using var context = TestDatenbank.NeuerContext(name);
        var mitarbeiter = new Mitarbeiter
        {
            Personalnummer = "P-001",
            Vorname = "Anna",
            Nachname = "Berger"
        };
        context.Auftraege.Add(new Auftrag
        {
            Titel = "Offener Auftrag",
            Kunde = new Kunde { Firmenname = "Test GmbH" },
            Startdatum = new DateTime(2026, 10, 2),
            Enddatum = new DateTime(2026, 10, 3)
        });
        context.Mitarbeiter.Add(mitarbeiter);
        await context.SaveChangesAsync();

        context.Abwesenheiten.Add(new Abwesenheit
        {
            MitarbeiterId = mitarbeiter.Id,
            Von = new DateTime(2026, 10, 1),
            Bis = new DateTime(2026, 10, 5),
            Typ = "Urlaub",
            Status = "Genehmigt"
        });
        await context.SaveChangesAsync();

        var service = new MatchingService(context);

        var vorschlaege = await service.ErstelleVorschlaegeAsync(Startdatum, 14);

        Assert.All(vorschlaege, vorschlag =>
            Assert.All(vorschlag.Zuweisungen, zuweisung =>
                Assert.Null(zuweisung.Mitarbeiter)));
    }

    [Fact]
    public async Task VorschlagUebernehmenAsync_WeistMitarbeiterZuUndSetztBesetzt()
    {
        var name = TestDatenbank.NeuerName();
        int auftragId;
        int mitarbeiterId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var auftrag = new Auftrag
            {
                Titel = "Offener Auftrag",
                Kunde = new Kunde { Firmenname = "Test GmbH" },
                Startdatum = new DateTime(2026, 10, 2),
                Enddatum = new DateTime(2026, 10, 3)
            };
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-001",
                Vorname = "Anna",
                Nachname = "Berger"
            };

            context.Auftraege.Add(auftrag);
            context.Mitarbeiter.Add(mitarbeiter);
            await context.SaveChangesAsync();
            auftragId = auftrag.Id;
            mitarbeiterId = mitarbeiter.Id;
        }

        bool ergebnis;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new MatchingService(context);
            var vorschlag = new MatchingVorschlag
            {
                Nummer = 1,
                Name = "Beste Qualifikation",
                Zuweisungen =
                {
                    new MatchingZuweisung
                    {
                        Auftrag = new Auftrag { Id = auftragId, Titel = "Offener Auftrag" },
                        Mitarbeiter = new Mitarbeiter { Id = mitarbeiterId }
                    }
                }
            };

            ergebnis = await service.VorschlagUebernehmenAsync(vorschlag);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var gespeicherter = await context.Auftraege
                .Include(a => a.Mitarbeiter)
                .SingleAsync(a => a.Id == auftragId);

            Assert.True(ergebnis);
            Assert.True(gespeicherter.Besetzt);
            var zugewiesener = Assert.Single(gespeicherter.Mitarbeiter);
            Assert.Equal(mitarbeiterId, zugewiesener.Id);
        }
    }

    [Fact]
    public async Task VorschlagUebernehmenAsync_GibtFalseZurueckWennMitarbeiterNichtMehrVerfuegbar()
    {
        var name = TestDatenbank.NeuerName();
        int auftragId;
        int mitarbeiterId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-001",
                Vorname = "Anna",
                Nachname = "Berger"
            };
            var offenerAuftrag = new Auftrag
            {
                Titel = "Offener Auftrag",
                Kunde = new Kunde { Firmenname = "Test GmbH" },
                Startdatum = new DateTime(2026, 10, 2),
                Enddatum = new DateTime(2026, 10, 3)
            };
            var ueberlappenderAuftrag = new Auftrag
            {
                Titel = "Ueberlappender Auftrag",
                Startdatum = new DateTime(2026, 10, 1),
                Enddatum = new DateTime(2026, 10, 4),
                Mitarbeiter = { mitarbeiter }
            };

            context.Auftraege.Add(offenerAuftrag);
            context.Auftraege.Add(ueberlappenderAuftrag);
            await context.SaveChangesAsync();
            auftragId = offenerAuftrag.Id;
            mitarbeiterId = mitarbeiter.Id;
        }

        bool ergebnis;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new MatchingService(context);
            var vorschlag = new MatchingVorschlag
            {
                Nummer = 1,
                Name = "Beste Qualifikation",
                Zuweisungen =
                {
                    new MatchingZuweisung
                    {
                        Auftrag = new Auftrag { Id = auftragId, Titel = "Offener Auftrag" },
                        Mitarbeiter = new Mitarbeiter { Id = mitarbeiterId }
                    }
                }
            };

            ergebnis = await service.VorschlagUebernehmenAsync(vorschlag);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var gespeicherter = await context.Auftraege
                .Include(a => a.Mitarbeiter)
                .SingleAsync(a => a.Id == auftragId);

            Assert.False(ergebnis);
            Assert.False(gespeicherter.Besetzt);
            Assert.Empty(gespeicherter.Mitarbeiter);
        }
    }
}
