using HRomance.Models;
using HRomance.Services;

namespace HRomance.Tests;

public class ArbeitszeitServiceTests
{
    private static ArbeitszeitService NeuerService(string name)
    {
        return new ArbeitszeitService(TestDatenbank.NeuerContext(name));
    }

    [Fact]
    public void Validierungsfehler_GibtFehlermeldungWennEndeVorBeginn()
    {
        var service = NeuerService(TestDatenbank.NeuerName());
        var arbeitszeit = new Arbeitszeit
        {
            Beginn = new TimeOnly(16, 0),
            Ende = new TimeOnly(8, 0)
        };

        Assert.Equal("Das Ende muss nach dem Beginn liegen.",
            service.Validierungsfehler(arbeitszeit));
    }

    [Fact]
    public void Validierungsfehler_GibtFehlermeldungBeiNegativerPause()
    {
        var service = NeuerService(TestDatenbank.NeuerName());
        var arbeitszeit = new Arbeitszeit
        {
            Beginn = new TimeOnly(8, 0),
            Ende = new TimeOnly(16, 0),
            PauseMinuten = -10
        };

        Assert.Equal("Die Pause darf nicht negativ sein.",
            service.Validierungsfehler(arbeitszeit));
    }

    [Fact]
    public void Validierungsfehler_GibtFehlermeldungWennPauseLaengerAlsArbeitszeit()
    {
        var service = NeuerService(TestDatenbank.NeuerName());
        var arbeitszeit = new Arbeitszeit
        {
            Beginn = new TimeOnly(8, 0),
            Ende = new TimeOnly(9, 0),
            PauseMinuten = 120
        };

        Assert.Equal("Die Pause darf nicht länger als die gesamte Arbeitsdauer sein.",
            service.Validierungsfehler(arbeitszeit));
    }

    [Fact]
    public void Validierungsfehler_GibtLeerenStringBeiGueltigerEingabe()
    {
        var service = NeuerService(TestDatenbank.NeuerName());
        var arbeitszeit = new Arbeitszeit
        {
            Beginn = new TimeOnly(8, 0),
            Ende = new TimeOnly(16, 0),
            PauseMinuten = 30
        };

        Assert.Equal(string.Empty, service.Validierungsfehler(arbeitszeit));
    }

    [Fact]
    public void BerechneArbeitsstunden_BerechnetNettoStundenKorrekt()
    {
        var service = NeuerService(TestDatenbank.NeuerName());
        var arbeitszeit = new Arbeitszeit
        {
            Beginn = new TimeOnly(8, 0),
            Ende = new TimeOnly(16, 0),
            PauseMinuten = 30
        };

        Assert.Equal(7.5, service.BerechneArbeitsstunden(arbeitszeit));
    }

    [Fact]
    public void BerechneArbeitsstunden_GibtNullZurueckBeiUngueltigerEingabe()
    {
        var service = NeuerService(TestDatenbank.NeuerName());
        var arbeitszeit = new Arbeitszeit
        {
            Beginn = new TimeOnly(16, 0),
            Ende = new TimeOnly(8, 0)
        };

        Assert.Equal(0, service.BerechneArbeitsstunden(arbeitszeit));
    }

    [Fact]
    public void IstGesetzlicherFeiertag_ErkenntFixenFeiertag()
    {
        var service = NeuerService(TestDatenbank.NeuerName());

        Assert.True(service.IstGesetzlicherFeiertag(new DateTime(2026, 1, 1)));
    }

    [Fact]
    public void IstGesetzlicherFeiertag_ErkenntBeweglichenFeiertag()
    {
        var service = NeuerService(TestDatenbank.NeuerName());

        Assert.True(service.IstGesetzlicherFeiertag(new DateTime(2026, 4, 6)));
    }

    [Fact]
    public void IstGesetzlicherFeiertag_GibtFalseFuerNormalenArbeitstag()
    {
        var service = NeuerService(TestDatenbank.NeuerName());

        Assert.False(service.IstGesetzlicherFeiertag(new DateTime(2026, 3, 17)));
    }

    [Fact]
    public void BerechneMonatssoll_ZiehtWochenendenUndFeiertageAb()
    {
        var service = NeuerService(TestDatenbank.NeuerName());

        var sollstunden =
            service.BerechneMonatssoll(2026, 1, 40, new DateTime(2026, 1, 31));

        Assert.Equal(160, sollstunden);
    }

    [Fact]
    public void BerechneMonatssoll_ZiehtGenehmigteAbwesenheitenAb()
    {
        var service = NeuerService(TestDatenbank.NeuerName());
        var abwesenheiten = new List<Abwesenheit>
        {
            new()
            {
                MitarbeiterId = 1,
                Von = new DateTime(2026, 1, 12),
                Bis = new DateTime(2026, 1, 16),
                Typ = "Urlaub",
                Status = "Genehmigt"
            }
        };

        var sollstunden = service.BerechneMonatssoll(
            2026, 1, 40, new DateTime(2026, 1, 31), abwesenheiten, 1);

        Assert.Equal(120, sollstunden);
    }

    [Fact]
    public async Task GetMonatsuebersichtAsync_BerechnetLaufendenSaldoKorrekt()
    {
        var name = TestDatenbank.NeuerName();
        int mitarbeiterId;

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var mitarbeiter = new Mitarbeiter
            {
                Personalnummer = "P-001",
                Vorname = "Anna",
                Nachname = "Berger",
                Wochenarbeitszeit = 40
            };
            context.Mitarbeiter.Add(mitarbeiter);
            await context.SaveChangesAsync();
            mitarbeiterId = mitarbeiter.Id;

            var tage = new[]
            {
                new DateTime(2026, 7, 6),
                new DateTime(2026, 8, 4),
                new DateTime(2026, 9, 2)
            };

            foreach (var tag in tage)
            {
                context.Arbeitszeiten.Add(new Arbeitszeit
                {
                    MitarbeiterId = mitarbeiterId,
                    Datum = tag,
                    Beginn = new TimeOnly(8, 0),
                    Ende = new TimeOnly(16, 0),
                    PauseMinuten = 30
                });
            }

            await context.SaveChangesAsync();
        }

        using (var context = TestDatenbank.NeuerContext(name))
        {
            var service = new ArbeitszeitService(context);

            var monate = await service.GetMonatsuebersichtAsync(
                mitarbeiterId, 40, 3, new DateTime(2026, 9, 30));

            Assert.Equal(3, monate.Count);
            Assert.Equal(monate[2].Saldo, monate[2].LaufenderSaldo);
            Assert.Equal(monate[1].Saldo + monate[2].LaufenderSaldo,
                monate[1].LaufenderSaldo);
            Assert.Equal(monate[0].Saldo + monate[1].LaufenderSaldo,
                monate[0].LaufenderSaldo);
        }
    }
}
