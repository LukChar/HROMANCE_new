using HRomance.Models;
using HRomance.Services;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Tests;

public class KundeServiceTests
{
    [Fact]
    public async Task GetAllAsync_GibtAlleKundenZurueck()
    {
        var name = TestDatenbank.NeuerName();

        using (var context = TestDatenbank.NeuerContext(name))
        {
            context.Kunden.Add(new Kunde { Firmenname = "Alpha GmbH" });
            context.Kunden.Add(new Kunde { Firmenname = "Beta GmbH" });
            await context.SaveChangesAsync();
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new KundeService(context);

            var kunden = await service.GetAllAsync();

            Assert.Equal(2, kunden.Count);
        }
    }

    [Fact]
    public async Task AddAsync_SpeichertKundeMitPflichtfeldern()
    {
        var name = TestDatenbank.NeuerName();

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new KundeService(context);

            await service.AddAsync(new Kunde { Firmenname = "Test GmbH" });
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var kunden = await context.Kunden.ToListAsync();

            Assert.Single(kunden);
            Assert.Equal("Test GmbH", kunden[0].Firmenname);
        }
    }

    [Fact]
    public async Task UpdateAsync_AktualisiertKundendaten()
    {
        var name = TestDatenbank.NeuerName();
        int kundeId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var kunde = new Kunde { Firmenname = "Alt GmbH" };
            context.Kunden.Add(kunde);
            await context.SaveChangesAsync();
            kundeId = kunde.Id;
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new KundeService(context);

            await service.UpdateAsync(new Kunde { Id = kundeId, Firmenname = "Neu GmbH" });
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var gespeicherter = await context.Kunden.SingleAsync(k => k.Id == kundeId);

            Assert.Equal("Neu GmbH", gespeicherter.Firmenname);
        }
    }
}
