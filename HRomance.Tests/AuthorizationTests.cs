using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace HRomance.Tests;

public class AuthorizationTests
{
    [Fact]
    public void ManagerrollenWerdenErkannt()
    {
        var admin = BenutzerMitRolle("Admin");
        var disponent = BenutzerMitRolle("Disponent");

        Assert.True(IstManager(admin));
        Assert.True(IstManager(disponent));
    }

    [Fact]
    public void MitarbeiterIstKeinManager()
    {
        var mitarbeiter = AngemeldeterMitarbeiter();

        Assert.False(IstManager(mitarbeiter));
    }

    [Fact]
    public void MitarbeiterseitenVerlangenEineAnmeldung()
    {
        AssertNurAnmeldung(typeof(HRomance.Components.Pages.Dashboard.Index));
        AssertNurAnmeldung(typeof(HRomance.Components.Pages.Kalender.Index));
        AssertNurAnmeldung(typeof(HRomance.Components.Pages.Antraege.Index));
    }

    [Fact]
    public void VerwaltungsseitenSindFuerManagerrollenGeschuetzt()
    {
        AssertManagerrollen(typeof(HRomance.Components.Pages.Home));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Mitarbeiter.Index));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Mitarbeiter.Create));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Mitarbeiter.Edit));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Mitarbeiter.Details));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Kunden.Index));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Kunden.Create));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Kunden.Edit));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Kunden.Details));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Auftraege.Index));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Auftraege.Create));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Auftraege.Edit));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Auftraege.Details));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Auftraege.Kalender));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Qualifikationen.Index));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Qualifikationen.Create));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Qualifikationen.Edit));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Matching.Index));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Disposition.Index));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Disposition.Tagesplan));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Disposition.Wochenplan));
        AssertManagerrollen(typeof(HRomance.Components.Pages.Disposition.Monatsplan));
    }

    [Fact]
    public void MitarbeiterDarfMitarbeiterverwaltungNichtDirektAufrufen()
    {
        var mitarbeiter = AngemeldeterMitarbeiter();
        var autorisierung = AutorisierungVon(typeof(HRomance.Components.Pages.Mitarbeiter.Index));

        Assert.False(IstManager(mitarbeiter));
        Assert.Equal("Admin,Disponent", autorisierung.Roles);
    }

    [Fact]
    public void ManagerDarfMitarbeiterverwaltungDirektAufrufen()
    {
        var disponent = BenutzerMitRolle("Disponent");
        var autorisierung = AutorisierungVon(typeof(HRomance.Components.Pages.Mitarbeiter.Index));

        Assert.True(IstManager(disponent));
        Assert.Contains("Disponent", autorisierung.Roles ?? string.Empty);
    }

    private static ClaimsPrincipal BenutzerMitRolle(string rolle)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Testbenutzer"),
            new(ClaimTypes.Role, rolle)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static ClaimsPrincipal AngemeldeterMitarbeiter()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Mitarbeiter")
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static bool IstManager(ClaimsPrincipal benutzer)
    {
        return benutzer.IsInRole("Admin") || benutzer.IsInRole("Disponent");
    }

    private static void AssertManagerrollen(Type seite)
    {
        var autorisierung = AutorisierungVon(seite);
        Assert.Equal("Admin,Disponent", autorisierung.Roles);
    }

    private static void AssertNurAnmeldung(Type seite)
    {
        var autorisierung = AutorisierungVon(seite);
        Assert.True(string.IsNullOrEmpty(autorisierung.Roles));
    }

    private static AuthorizeAttribute AutorisierungVon(Type seite)
    {
        var attribute = seite.GetCustomAttributes(typeof(AuthorizeAttribute), true);
        return Assert.IsType<AuthorizeAttribute>(Assert.Single(attribute));
    }
}
