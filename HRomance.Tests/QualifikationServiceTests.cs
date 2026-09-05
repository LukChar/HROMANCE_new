using HRomance.Models;
using HRomance.Services;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Tests;

public class QualifikationServiceTests
{
    [Fact]
    public async Task AddAsync_SpeichertQualifikation()
    {
        var name = TestDatenbank.NeuerName();

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new QualifikationService(context);

            await service.AddAsync(new Qualifikation { Name = "Schweisser" });
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var qualifikationen = await context.Qualifikationen.ToListAsync();

            Assert.Single(qualifikationen);
            Assert.Equal("Schweisser", qualifikationen[0].Name);
        }
    }

    [Fact]
    public async Task UpdateAsync_AendertDenNamen()
    {
        var name = TestDatenbank.NeuerName();
        int qualifikationId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var qualifikation = new Qualifikation { Name = "Alt" };
            context.Qualifikationen.Add(qualifikation);
            await context.SaveChangesAsync();
            qualifikationId = qualifikation.Id;
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new QualifikationService(context);

            await service.UpdateAsync(new Qualifikation { Id = qualifikationId, Name = "Neu" });
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var gespeicherte =
                await context.Qualifikationen.SingleAsync(q => q.Id == qualifikationId);

            Assert.Equal("Neu", gespeicherte.Name);
        }
    }

    [Fact]
    public async Task DeleteAsync_EntferntQualifikation()
    {
        var name = TestDatenbank.NeuerName();
        int qualifikationId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var qualifikation = new Qualifikation { Name = "Elektriker" };
            context.Qualifikationen.Add(qualifikation);
            await context.SaveChangesAsync();
            qualifikationId = qualifikation.Id;
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new QualifikationService(context);

            await service.DeleteAsync(qualifikationId);
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            Assert.Empty(await context.Qualifikationen.ToListAsync());
        }
    }
}
