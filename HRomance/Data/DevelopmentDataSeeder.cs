using HRomance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRomance.Data;

public static class DevelopmentDataSeeder
{
    public const string DemoPasswort = "Demo123!";

    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] rollen = ["Admin", "Disponent"];

        foreach (var rolle in rollen)
        {
            if (!await roleManager.RoleExistsAsync(rolle))
            {
                await roleManager.CreateAsync(new IdentityRole(rolle));
            }
        }
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(services);

        var tischler = await QualifikationAnlegen(context, "Tischler");
        var elektriker = await QualifikationAnlegen(context, "Elektriker");
        var schweisser = await QualifikationAnlegen(context, "Schweißer");
        var mechaniker = await QualifikationAnlegen(context, "Mechaniker");
        var softwareentwickler = await QualifikationAnlegen(context, "Softwareentwickler");

        var fritz = await MitarbeiterAnlegen(context, "P001", "Fritz", "Schreiner",
            "fritz.schreiner@hromance.test", "+43 000 100001", 40, [tischler]);
        var hans = await MitarbeiterAnlegen(context, "P002", "Hans", "Berger",
            "hans.elektriker@hromance.test", "+43 000 100002", 38.5, [elektriker, mechaniker]);
        var max = await MitarbeiterAnlegen(context, "P003", "Max", "Leitner",
            "max.muster@hromance.test", "+43 000 100003", 40, [schweisser, mechaniker]);
        var anna = await MitarbeiterAnlegen(context, "P004", "Anna", "Gruber",
            "anna.gruber@hromance.test", "+43 000 100004", 30, [elektriker]);
        var lisa = await MitarbeiterAnlegen(context, "P005", "Lisa", "Moser",
            "lisa.moser@hromance.test", "+43 000 100005", 38.5, [softwareentwickler]);
        var martin = await MitarbeiterAnlegen(context, "P006", "Martin", "Admin",
            "admin@hromance.test", "+43 000 100006", 40, []);
        var daniel = await MitarbeiterAnlegen(context, "P007", "Daniel", "Disponent",
            "disponent@hromance.test", "+43 000 100007", 40, []);

        List<Mitarbeiter> demoMitarbeiter = [fritz, hans, max, anna, lisa, martin, daniel];

        foreach (var demoMitarbeiterEintrag in demoMitarbeiter)
        {
            await context.Entry(demoMitarbeiterEintrag)
                .Collection(m => m.Auftraege)
                .LoadAsync();
            demoMitarbeiterEintrag.Auftraege.Clear();
        }

        await context.SaveChangesAsync();

        var alpenTechnik = await KundeAnlegen(context, "Alpen Technik GmbH", "Mara Beispiel",
            "kontakt@alpentechnik.example.test", "+43 000 200001");
        var nordwerk = await KundeAnlegen(context, "Nordwerk GmbH", "Paul Demo",
            "kontakt@nordwerk.example.test", "+43 000 200002");
        var musterMaschinenbau = await KundeAnlegen(context, "Muster Maschinenbau GmbH", "Eva Muster",
            "kontakt@mustermaschinenbau.example.test", "+43 000 200003");
        var donauService = await KundeAnlegen(context, "Donau Service GmbH", "Noah Beispiel",
            "kontakt@donauservice.example.test", "+43 000 200004");
        var demoIndustrie = await KundeAnlegen(context, "Demo Industrie GmbH", "Lena Demo",
            "kontakt@demoindustrie.example.test", "+43 000 200005");

        var heute = DateTime.Today;

        await AuftragAnlegen(context, "Wartung Produktionsanlage",
            "Regelmäßige Wartung und Funktionsprüfung der Produktionsanlage.", "Linz",
            heute.AddDays(-2), heute.AddDays(2), alpenTechnik, [mechaniker], [hans]);
        await AuftragAnlegen(context, "Elektroinstallation Werkhalle",
            "Installation und Prüfung der neuen Hallenbeleuchtung.", "Wels",
            heute.AddDays(2), heute.AddDays(5), nordwerk, [elektriker], [anna]);
        await AuftragAnlegen(context, "Montage Förderanlage",
            "Mechanische Montage einer neuen Förderanlage.", "Steyr",
            heute.AddDays(7), heute.AddDays(10), musterMaschinenbau, [mechaniker], []);
        await AuftragAnlegen(context, "Schweißarbeiten Rohrleitung",
            "Schweißarbeiten an einer industriellen Rohrleitung.", "Amstetten",
            heute.AddDays(4), heute.AddDays(6), donauService, [schweisser], [max]);
        await AuftragAnlegen(context, "Software-Wartung",
            "Updates und Kontrolle der internen Verwaltungssoftware.", "Wien",
            heute, heute, demoIndustrie, [softwareentwickler], [lisa]);
        await AuftragAnlegen(context, "Möbelmontage Empfang",
            "Montage neuer Empfangsmöbel beim Kunden.", "Krems",
            heute.AddDays(12), heute.AddDays(12), donauService, [tischler], [fritz]);
        await AuftragAnlegen(context, "Maschinenprüfung",
            "Technische Kontrolle und Dokumentation einer Bestandsmaschine.", "St. Pölten",
            heute.AddDays(14), heute.AddDays(16), alpenTechnik, [elektriker, mechaniker], []);
        await AuftragAnlegen(context, "Standortbegehung Verwaltung",
            "Begehung und Abstimmung für den nächsten Kundeneinsatz.", "Linz",
            heute.AddDays(3), heute.AddDays(3), alpenTechnik, [], [martin]);
        await AuftragAnlegen(context, "Einsatzplanung Nordwerk",
            "Vorbereitung und Kontrolle der geplanten Mitarbeitereinsätze.", "Wels",
            heute.AddDays(5), heute.AddDays(5), nordwerk, [], [daniel]);

        await ArbeitszeitAnlegen(context, fritz, heute.AddDays(-3), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        await ArbeitszeitAnlegen(context, hans, heute.AddDays(-3), new TimeOnly(7, 30), new TimeOnly(16, 0), 30);
        await ArbeitszeitAnlegen(context, max, heute.AddDays(-2), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        await ArbeitszeitAnlegen(context, anna, heute.AddDays(-2), new TimeOnly(8, 0), new TimeOnly(14, 0), 30);
        await ArbeitszeitAnlegen(context, lisa, heute.AddDays(-1), new TimeOnly(8, 0), new TimeOnly(12, 0), 0);
        await ArbeitszeitAnlegen(context, fritz, heute.AddDays(-1), new TimeOnly(7, 30), new TimeOnly(16, 0), 30);
        await ArbeitszeitAnlegen(context, martin, heute.AddDays(-3), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        await ArbeitszeitAnlegen(context, martin, heute.AddDays(-1), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        await ArbeitszeitAnlegen(context, daniel, heute.AddDays(-3), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        await ArbeitszeitAnlegen(context, daniel, heute.AddDays(-1), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);

        await AbwesenheitAnlegen(context, fritz, heute.AddDays(8), heute.AddDays(9), "Urlaub", "Genehmigt", "Demo-Urlaub");
        await AbwesenheitAnlegen(context, hans, heute.AddDays(15), heute.AddDays(16), "Urlaub", "Offen", "Geplanter Urlaub");
        await AbwesenheitAnlegen(context, anna, heute.AddDays(-5), heute.AddDays(-5), "Sonstige Abwesenheit", "Genehmigt", "Behördentermin");
        await AbwesenheitAnlegen(context, max, heute.AddDays(18), heute.AddDays(18), "Urlaub", "Abgelehnt", "Terminkonflikt");
        await AbwesenheitAnlegen(context, lisa, heute.AddDays(6), heute.AddDays(6), "Zeitausgleich", "Offen", "Überstundenabbau");

        await BenutzerAnlegen(userManager, "admin@hromance.test", martin, "Admin");
        await BenutzerAnlegen(userManager, "disponent@hromance.test", daniel, "Disponent");
        await BenutzerAnlegen(userManager, "fritz.schreiner@hromance.test", fritz, null);
        await BenutzerAnlegen(userManager, "hans.elektriker@hromance.test", hans, null);
        await BenutzerAnlegen(userManager, "max.muster@hromance.test", max, null);
        await BenutzerAnlegen(userManager, "anna.gruber@hromance.test", anna, null);
        await BenutzerAnlegen(userManager, "lisa.moser@hromance.test", lisa, null);
    }

    private static async Task<Qualifikation> QualifikationAnlegen(ApplicationDbContext context, string name)
    {
        var qualifikation = await context.Qualifikationen.FirstOrDefaultAsync(q => q.Name == name);

        if (qualifikation == null)
        {
            qualifikation = new Qualifikation { Name = name };
            context.Qualifikationen.Add(qualifikation);
            await context.SaveChangesAsync();
        }

        return qualifikation;
    }

    private static async Task<Mitarbeiter> MitarbeiterAnlegen(
        ApplicationDbContext context,
        string personalnummer,
        string vorname,
        string nachname,
        string email,
        string telefon,
        double wochenarbeitszeit,
        List<Qualifikation> qualifikationen)
    {
        var mitarbeiter = await context.Mitarbeiter
            .Include(m => m.Qualifikationen)
            .FirstOrDefaultAsync(m => m.Personalnummer == personalnummer);

        if (mitarbeiter == null)
        {
            mitarbeiter = new Mitarbeiter
            {
                Personalnummer = personalnummer,
                Vorname = vorname,
                Nachname = nachname,
                Email = email,
                Telefon = telefon,
                Wochenarbeitszeit = wochenarbeitszeit,
                SollStundenProTag = wochenarbeitszeit / 5,
                Verfuegbar = true,
                Qualifikationen = qualifikationen
            };

            context.Mitarbeiter.Add(mitarbeiter);
        }

        mitarbeiter.Personalnummer = personalnummer;
        mitarbeiter.Vorname = vorname;
        mitarbeiter.Nachname = nachname;
        mitarbeiter.Email = email;
        mitarbeiter.Telefon = telefon;
        mitarbeiter.Wochenarbeitszeit = wochenarbeitszeit;
        mitarbeiter.SollStundenProTag = wochenarbeitszeit / 5;
        mitarbeiter.Verfuegbar = true;
        mitarbeiter.Qualifikationen.Clear();

        foreach (var qualifikation in qualifikationen)
        {
            mitarbeiter.Qualifikationen.Add(qualifikation);
        }

        await context.SaveChangesAsync();

        return mitarbeiter;
    }

    private static async Task<Kunde> KundeAnlegen(
        ApplicationDbContext context,
        string firmenname,
        string ansprechpartner,
        string email,
        string telefon)
    {
        var kunde = await context.Kunden.FirstOrDefaultAsync(k => k.Firmenname == firmenname);

        if (kunde == null)
        {
            kunde = new Kunde
            {
                Firmenname = firmenname,
                Ansprechpartner = ansprechpartner,
                Email = email,
                Telefon = telefon
            };

            context.Kunden.Add(kunde);
            await context.SaveChangesAsync();
        }

        return kunde;
    }

    private static async Task AuftragAnlegen(
        ApplicationDbContext context,
        string titel,
        string beschreibung,
        string einsatzort,
        DateTime startdatum,
        DateTime enddatum,
        Kunde kunde,
        List<Qualifikation> qualifikationen,
        List<Mitarbeiter> mitarbeiter)
    {
        var auftrag = await context.Auftraege
            .Include(a => a.Qualifikationen)
            .Include(a => a.Mitarbeiter)
            .FirstOrDefaultAsync(a => a.Titel == titel);

        if (auftrag == null)
        {
            auftrag = new Auftrag { Titel = titel };
            context.Auftraege.Add(auftrag);
        }

        auftrag.Titel = titel;
        auftrag.Beschreibung = beschreibung;
        auftrag.Einsatzort = einsatzort;
        auftrag.Startdatum = startdatum;
        auftrag.Enddatum = enddatum;
        auftrag.KundeId = kunde.Id;
        auftrag.Besetzt = mitarbeiter.Count > 0;
        auftrag.Qualifikationen.Clear();
        auftrag.Mitarbeiter.Clear();

        foreach (var qualifikation in qualifikationen)
        {
            auftrag.Qualifikationen.Add(qualifikation);
        }

        foreach (var person in mitarbeiter)
        {
            auftrag.Mitarbeiter.Add(person);
        }

        await context.SaveChangesAsync();
    }

    private static async Task ArbeitszeitAnlegen(
        ApplicationDbContext context,
        Mitarbeiter mitarbeiter,
        DateTime datum,
        TimeOnly beginn,
        TimeOnly ende,
        int pauseMinuten)
    {
        var vorhanden = await context.Arbeitszeiten.AnyAsync(a =>
            a.MitarbeiterId == mitarbeiter.Id
            && a.Datum == datum
            && a.Beginn == beginn);

        if (!vorhanden)
        {
            context.Arbeitszeiten.Add(new Arbeitszeit
            {
                MitarbeiterId = mitarbeiter.Id,
                Datum = datum,
                Beginn = beginn,
                Ende = ende,
                PauseMinuten = pauseMinuten,
                Notiz = "Development-Demodaten"
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task AbwesenheitAnlegen(
        ApplicationDbContext context,
        Mitarbeiter mitarbeiter,
        DateTime von,
        DateTime bis,
        string typ,
        string status,
        string grund)
    {
        var vorhanden = await context.Abwesenheiten.AnyAsync(a =>
            a.MitarbeiterId == mitarbeiter.Id
            && a.Von == von
            && a.Typ == typ
            && a.Status == status);

        if (!vorhanden)
        {
            context.Abwesenheiten.Add(new Abwesenheit
            {
                MitarbeiterId = mitarbeiter.Id,
                Von = von,
                Bis = bis,
                Typ = typ,
                Status = status,
                Grund = grund
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task BenutzerAnlegen(
        UserManager<ApplicationUser> userManager,
        string email,
        Mitarbeiter? mitarbeiter,
        string? rolle)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                MitarbeiterId = mitarbeiter?.Id
            };

            var ergebnis = await userManager.CreateAsync(user, DemoPasswort);

            if (!ergebnis.Succeeded)
            {
                throw new InvalidOperationException("Demo-Benutzer konnte nicht erstellt werden: " + email);
            }
        }
        else if (user.MitarbeiterId != mitarbeiter?.Id)
        {
            user.MitarbeiterId = mitarbeiter?.Id;
            await userManager.UpdateAsync(user);
        }

        if (rolle != null && !await userManager.IsInRoleAsync(user, rolle))
        {
            await userManager.AddToRoleAsync(user, rolle);
        }

        if (rolle == null)
        {
            var managerRollen = await userManager.GetRolesAsync(user);

            foreach (var managerRolle in managerRollen)
            {
                if (managerRolle == "Admin" || managerRolle == "Disponent")
                {
                    await userManager.RemoveFromRoleAsync(user, managerRolle);
                }
            }
        }
    }
}
