using HRomance.Models;
using HRomance.Services;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Tests;

public class MitarbeiterServiceTests
{
    [Fact]
    public async Task GetAllAsync_GibtAlleMitarbeiterMitQualifikationenZurueck()
    {
        var name = TestDatenbank.NeuerName();

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var qualifikationA = new Qualifikation { Name = "Elektriker" };
            var qualifikationB = new Qualifikation { Name = "Schweisser" };
            var ersterMitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-001",
                Vorname = "Anna",
                Nachname = "Berger",
                Qualifikationen = { qualifikationA, qualifikationB }
            };
            var zweiterMitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-002",
                Vorname = "Max",
                Nachname = "Muster"
            };

            context.Mitarbeiter.Add(ersterMitarbeiter);
            context.Mitarbeiter.Add(zweiterMitarbeiter);
            await context.SaveChangesAsync();
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new MitarbeiterService(context);

            var mitarbeiter = await service.GetAllAsync();

            Assert.Equal(2, mitarbeiter.Count);
            var anna = mitarbeiter.Single(m => m.Personalnummer == "P-001");
            var max = mitarbeiter.Single(m => m.Personalnummer == "P-002");
            Assert.Equal(2, anna.Qualifikationen.Count);
            Assert.Empty(max.Qualifikationen);
        }
    }

    [Fact]
    public async Task GetByIdAsync_GibtNullZurueckBeiUngueltigerId()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new MitarbeiterService(context);

        var mitarbeiter = await service.GetByIdAsync(999);

        Assert.Null(mitarbeiter);
    }

    [Fact]
    public void PasstZurSuche_FindetNachVorname()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new MitarbeiterService(context);
        var mitarbeiter = new Mitarbeiter { Vorname = "Anna", Nachname = "Berger" };

        Assert.True(service.PasstZurSuche(mitarbeiter, "Anna"));
    }

    [Fact]
    public void PasstZurSuche_FindetNachPersonalnummer()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new MitarbeiterService(context);
        var mitarbeiter = new Mitarbeiter { Personalnummer = "P-001" };

        Assert.True(service.PasstZurSuche(mitarbeiter, "P-001"));
    }

    [Fact]
    public void PasstZurSuche_FindetNachQualifikationsname()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new MitarbeiterService(context);
        var mitarbeiter = new Mitarbeiter
        {
            Vorname = "Anna",
            Qualifikationen = { new Qualifikation { Name = "Elektriker" } }
        };

        Assert.True(service.PasstZurSuche(mitarbeiter, "Elektriker"));
    }

    [Fact]
    public void PasstZurSuche_GibtFalseZurueckBeiLeeremTreffer()
    {
        using var context = TestDatenbank.NeuerContext(TestDatenbank.NeuerName());
        var service = new MitarbeiterService(context);
        var mitarbeiter = new Mitarbeiter { Vorname = "Anna" };

        Assert.False(service.PasstZurSuche(mitarbeiter, "XYZ"));
    }

    [Fact]
    public async Task AddAsync_SpeichertMitarbeiterInDerDatenbank()
    {
        var name = TestDatenbank.NeuerName();

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new MitarbeiterService(context);
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-100",
                Vorname = "Max",
                Nachname = "Muster"
            };

            await service.AddAsync(mitarbeiter);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var gespeicherte = await context.Mitarbeiter.ToListAsync();

            Assert.Single(gespeicherte);
            Assert.Equal("P-100", gespeicherte[0].Personalnummer);
        }
    }

    [Fact]
    public async Task UpdateAsync_AktualisiertFelderUndQualifikationen()
    {
        var name = TestDatenbank.NeuerName();
        int mitarbeiterId;
        int neueQualifikationId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var alteQualifikation = new Qualifikation { Name = "Elektriker" };
            var neueQualifikation = new Qualifikation { Name = "Schweisser" };
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-001",
                Vorname = "Anna",
                Nachname = "Berger",
                Qualifikationen = { alteQualifikation }
            };

            context.Mitarbeiter.Add(mitarbeiter);
            context.Qualifikationen.Add(neueQualifikation);
            await context.SaveChangesAsync();

            mitarbeiterId = mitarbeiter.Id;
            neueQualifikationId = neueQualifikation.Id;
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new MitarbeiterService(context);
            var neueQualifikation =
                await context.Qualifikationen.FindAsync(neueQualifikationId);
            var aenderung = new Mitarbeiter
            {
                Id = mitarbeiterId,
                Personalnummer = "P-001",
                Vorname = "Maria",
                Nachname = "Berger",
                Qualifikationen = { neueQualifikation! }
            };

            await service.UpdateAsync(aenderung);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var gespeicherter = await context.Mitarbeiter
                .Include(m => m.Qualifikationen)
                .SingleAsync(m => m.Id == mitarbeiterId);

            Assert.Equal("Maria", gespeicherter.Vorname);
            Assert.Single(gespeicherter.Qualifikationen);
            Assert.Equal("Schweisser", gespeicherter.Qualifikationen[0].Name);
        }
    }

    [Fact]
    public async Task DeleteAsync_EntferntMitarbeiterAusDerDatenbank()
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

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new MitarbeiterService(context);

            await service.DeleteAsync(mitarbeiterId);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            Assert.Empty(await context.Mitarbeiter.ToListAsync());
        }
    }
}
