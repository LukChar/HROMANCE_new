using HRomance.Models;
using HRomance.Services;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Tests;

public class AuftragServiceTests
{
    [Fact]
    public async Task GetAllAsync_LaedtAuftraegeMitKundeQualifikationenUndMitarbeitern()
    {
        var name = TestDatenbank.NeuerName();

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var kunde = new Kunde { Firmenname = "Test GmbH" };
            var qualifikation = new Qualifikation { Name = "Elektriker" };
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-001",
                Vorname = "Anna",
                Nachname = "Berger"
            };
            var auftrag = new Auftrag
            {
                Titel = "Baustelle Nord",
                Kunde = kunde,
                Startdatum = new DateTime(2026, 10, 1),
                Enddatum = new DateTime(2026, 10, 5),
                Qualifikationen = { qualifikation },
                Mitarbeiter = { mitarbeiter }
            };

            context.Auftraege.Add(auftrag);
            await context.SaveChangesAsync();
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AuftragService(context);

            var auftraege = await service.GetAllAsync();

            Assert.Single(auftraege);
            Assert.NotNull(auftraege[0].Kunde);
            Assert.Single(auftraege[0].Qualifikationen);
            Assert.Single(auftraege[0].Mitarbeiter);
        }
    }

    [Fact]
    public async Task MaterialHinzufuegenAsync_FuegtMaterialHinzu()
    {
        var name = TestDatenbank.NeuerName();
        int auftragId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var auftrag = new Auftrag { Titel = "Baustelle Nord" };
            context.Auftraege.Add(auftrag);
            await context.SaveChangesAsync();
            auftragId = auftrag.Id;
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AuftragService(context);

            await service.MaterialHinzufuegenAsync(auftragId, "Schrauben", 50);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var material = await context.Materialeintraege.ToListAsync();

            Assert.Single(material);
            Assert.Equal("Schrauben", material[0].Bezeichnung);
            Assert.Equal(50, material[0].Anzahl);
        }
    }

    [Fact]
    public async Task MaterialHinzufuegenAsync_LehntLeereBezeichnungAb()
    {
        var name = TestDatenbank.NeuerName();
        int auftragId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var auftrag = new Auftrag { Titel = "Baustelle Nord" };
            context.Auftraege.Add(auftrag);
            await context.SaveChangesAsync();
            auftragId = auftrag.Id;
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AuftragService(context);

            await service.MaterialHinzufuegenAsync(auftragId, "", 10);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            Assert.Empty(await context.Materialeintraege.ToListAsync());
        }
    }

    [Fact]
    public async Task MaterialHinzufuegenAsync_LehntAnzahlKleinerEinsAb()
    {
        var name = TestDatenbank.NeuerName();
        int auftragId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var auftrag = new Auftrag { Titel = "Baustelle Nord" };
            context.Auftraege.Add(auftrag);
            await context.SaveChangesAsync();
            auftragId = auftrag.Id;
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AuftragService(context);

            await service.MaterialHinzufuegenAsync(auftragId, "Schrauben", 0);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            Assert.Empty(await context.Materialeintraege.ToListAsync());
        }
    }

    [Fact]
    public async Task MitarbeiterZuweisenAsync_SetztBesetztAufTrue()
    {
        var name = TestDatenbank.NeuerName();
        int auftragId;
        int mitarbeiterId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var auftrag = new Auftrag { Titel = "Baustelle Nord", Besetzt = false };
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

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AuftragService(context);

            await service.MitarbeiterZuweisenAsync(auftragId, new List<int> { mitarbeiterId });
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var gespeicherter = await context.Auftraege
                .Include(a => a.Mitarbeiter)
                .SingleAsync(a => a.Id == auftragId);

            Assert.True(gespeicherter.Besetzt);
            Assert.Single(gespeicherter.Mitarbeiter);
        }
    }

    [Fact]
    public async Task MitarbeiterEntfernenAsync_SetztBesetztAufFalseWennLetzterEntfernt()
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
            var auftrag = new Auftrag
            {
                Titel = "Baustelle Nord",
                Besetzt = true,
                Mitarbeiter = { mitarbeiter }
            };

            context.Auftraege.Add(auftrag);
            await context.SaveChangesAsync();
            auftragId = auftrag.Id;
            mitarbeiterId = mitarbeiter.Id;
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new AuftragService(context);

            await service.MitarbeiterEntfernenAsync(auftragId, mitarbeiterId);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var gespeicherter = await context.Auftraege
                .Include(a => a.Mitarbeiter)
                .SingleAsync(a => a.Id == auftragId);

            Assert.False(gespeicherter.Besetzt);
            Assert.Empty(gespeicherter.Mitarbeiter);
        }
    }

    [Fact]
    public async Task MitarbeiterVerfuegbarkeitPruefenAsync_ErkenntAbwesenheit()
    {
        var name = TestDatenbank.NeuerName();

        using var context = TestDatenbank.NeuerContext(name);
        var mitarbeiter = new Mitarbeiter
        {
            Personalnummer = "P-001",
            Vorname = "Anna",
            Nachname = "Berger"
        };
        var auftrag = new Auftrag
        {
            Titel = "Baustelle Nord",
            Startdatum = new DateTime(2026, 10, 1),
            Enddatum = new DateTime(2026, 10, 5)
        };

        context.Mitarbeiter.Add(mitarbeiter);
        context.Auftraege.Add(auftrag);
        await context.SaveChangesAsync();

        context.Abwesenheiten.Add(new Abwesenheit
        {
            MitarbeiterId = mitarbeiter.Id,
            Von = new DateTime(2026, 10, 3),
            Bis = new DateTime(2026, 10, 7),
            Typ = "Urlaub",
            Status = "Genehmigt"
        });
        await context.SaveChangesAsync();

        var service = new AuftragService(context);

        var ergebnis =
            await service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, auftrag);

        Assert.StartsWith("Nicht verfügbar", ergebnis);
    }

    [Fact]
    public async Task MitarbeiterVerfuegbarkeitPruefenAsync_ErkenntUeberlappendenAuftrag()
    {
        var name = TestDatenbank.NeuerName();

        using var context = TestDatenbank.NeuerContext(name);
        var mitarbeiter = new Mitarbeiter
        {
            Personalnummer = "P-001",
            Vorname = "Anna",
            Nachname = "Berger"
        };
        var auftragA = new Auftrag
        {
            Titel = "Baustelle Nord",
            Startdatum = new DateTime(2026, 10, 1),
            Enddatum = new DateTime(2026, 10, 5),
            Mitarbeiter = { mitarbeiter }
        };
        var auftragB = new Auftrag
        {
            Titel = "Baustelle Sued",
            Startdatum = new DateTime(2026, 10, 3),
            Enddatum = new DateTime(2026, 10, 8)
        };

        context.Auftraege.Add(auftragA);
        context.Auftraege.Add(auftragB);
        await context.SaveChangesAsync();

        var service = new AuftragService(context);

        var ergebnis =
            await service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, auftragB);

        Assert.StartsWith("Nicht verfügbar - Auftrag:", ergebnis);
    }

    [Fact]
    public async Task MitarbeiterVerfuegbarkeitPruefenAsync_GibtVerfuegbarZurueck()
    {
        var name = TestDatenbank.NeuerName();

        using var context = TestDatenbank.NeuerContext(name);
        var mitarbeiter = new Mitarbeiter
        {
            Personalnummer = "P-001",
            Vorname = "Anna",
            Nachname = "Berger",
            Verfuegbar = true
        };
        var auftrag = new Auftrag
        {
            Titel = "Baustelle Nord",
            Startdatum = new DateTime(2026, 10, 1),
            Enddatum = new DateTime(2026, 10, 5)
        };

        context.Mitarbeiter.Add(mitarbeiter);
        context.Auftraege.Add(auftrag);
        await context.SaveChangesAsync();

        var service = new AuftragService(context);

        var ergebnis =
            await service.MitarbeiterVerfuegbarkeitPruefenAsync(mitarbeiter.Id, auftrag);

        Assert.Equal("Verfügbar", ergebnis);
    }

    [Fact]
    public void AnzahlPassenderQualifikationen_ZaehltKorrekt()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new AuftragService(context);
        var qualifikationA = new Qualifikation { Id = 1, Name = "A" };
        var qualifikationB = new Qualifikation { Id = 2, Name = "B" };
        var qualifikationC = new Qualifikation { Id = 3, Name = "C" };
        var auftrag = new Auftrag
        {
            Titel = "Baustelle Nord",
            Qualifikationen = { qualifikationA, qualifikationB }
        };
        var mitarbeiter = new Mitarbeiter
        {
            Qualifikationen = { qualifikationA, qualifikationB, qualifikationC }
        };

        Assert.Equal(2, service.AnzahlPassenderQualifikationen(auftrag, mitarbeiter));
    }

    [Fact]
    public void AnzahlPassenderQualifikationen_GibtNullZurueckBeiKeinerUebereinstimmung()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new AuftragService(context);
        var auftrag = new Auftrag
        {
            Titel = "Baustelle Nord",
            Qualifikationen = { new Qualifikation { Id = 1, Name = "A" } }
        };
        var mitarbeiter = new Mitarbeiter
        {
            Qualifikationen = { new Qualifikation { Id = 2, Name = "B" } }
        };

        Assert.Equal(0, service.AnzahlPassenderQualifikationen(auftrag, mitarbeiter));
    }
}
