using HRomance.Models;
using HRomance.Services;
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
        var projektleitung = await QualifikationAnlegen(context, "Projektleitung");
        var lagerlogistik = await QualifikationAnlegen(context, "Lagerlogistik");
        var servicetechnik = await QualifikationAnlegen(context, "Servicetechnik");

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
        var sofia = await MitarbeiterAnlegen(context, "P008", "Sofia", "Kern",
            "sofia.kern@hromance.test", "+43 000 100008", 35, [elektriker, projektleitung]);
        var lukas = await MitarbeiterAnlegen(context, "P009", "Lukas", "Baumann",
            "lukas.baumann@hromance.test", "+43 000 100009", 40, [tischler, lagerlogistik]);
        var mia = await MitarbeiterAnlegen(context, "P010", "Mia", "Hofer",
            "mia.hofer@hromance.test", "+43 000 100010", 32, [softwareentwickler, projektleitung]);
        var leon = await MitarbeiterAnlegen(context, "P011", "Leon", "Auer",
            "leon.auer@hromance.test", "+43 000 100011", 40, [mechaniker, schweisser, servicetechnik]);
        var emma = await MitarbeiterAnlegen(context, "P012", "Emma", "Seidel",
            "emma.seidel@hromance.test", "+43 000 100012", 30, [lagerlogistik, servicetechnik]);

        List<Mitarbeiter> demoMitarbeiter =
            [fritz, hans, max, anna, lisa, martin, daniel, sofia, lukas, mia, leon, emma];

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
        var bergblickHotel = await KundeAnlegen(context, "Bergblick Hotelbetrieb GmbH", "Nina Beispiel",
            "kontakt@bergblick.example.test", "+43 000 200006");
        var zentraleLogistik = await KundeAnlegen(context, "Zentrale Logistik GmbH", "Felix Muster",
            "kontakt@zentralelogistik.example.test", "+43 000 200007");
        var stadtwerkeDemo = await KundeAnlegen(context, "Stadtwerke Demo GmbH", "Clara Beispiel",
            "kontakt@stadtwerke.example.test", "+43 000 200008");
        var panoramaBau = await KundeAnlegen(context, "Panorama Bau GmbH", "Jonas Demo",
            "kontakt@panoramabau.example.test", "+43 000 200009");
        var innovationszentrum = await KundeAnlegen(context, "Innovationszentrum Muster GmbH", "Sarah Muster",
            "kontakt@innovationszentrum.example.test", "+43 000 200010");

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

        await AuftragAnlegen(context, "Renovierung Besprechungsräume",
            "Erneuerung der Einbaumöbel in mehreren Besprechungsräumen.", "Linz",
            heute.AddDays(-120), heute.AddDays(-116), alpenTechnik, [tischler], [fritz, lukas]);
        await AuftragAnlegen(context, "Anlagenservice Standort Süd",
            "Wartung und Austausch mehrerer Verschleißteile.", "Graz",
            heute.AddDays(-105), heute.AddDays(-102), demoIndustrie, [mechaniker, servicetechnik], [hans, leon]);
        await AuftragAnlegen(context, "Montage neuer Lagerregale",
            "Aufbau und Sicherung der neuen Regalanlage.", "Wels",
            heute.AddDays(-92), heute.AddDays(-89), zentraleLogistik, [lagerlogistik], [lukas, emma]);
        await AuftragAnlegen(context, "Netzwerkcheck Verwaltung",
            "Prüfung der Arbeitsplätze und Aktualisierung der Verwaltungssoftware.", "Wien",
            heute.AddDays(-80), heute.AddDays(-78), innovationszentrum, [softwareentwickler], [lisa, mia]);
        await AuftragAnlegen(context, "Schaltschrankprüfung West",
            "Messung und Dokumentation der bestehenden Schaltschränke.", "Salzburg",
            heute.AddDays(-65), heute.AddDays(-63), stadtwerkeDemo, [elektriker], [anna, sofia]);
        await AuftragAnlegen(context, "Reparatur Absauganlage",
            "Fehlersuche und mechanische Reparatur der Absauganlage.", "Steyr",
            heute.AddDays(-50), heute.AddDays(-47), musterMaschinenbau, [mechaniker], [hans]);
        await AuftragAnlegen(context, "Möbelaustausch Hotelzimmer",
            "Austausch beschädigter Einbaumöbel und Beschläge.", "Bad Ischl",
            heute.AddDays(-35), heute.AddDays(-31), bergblickHotel, [tischler], [fritz, lukas]);
        await AuftragAnlegen(context, "Software-Rollout Buchhaltung",
            "Installation und gemeinsame Abnahme der neuen Fachanwendung.", "Linz",
            heute.AddDays(-20), heute.AddDays(-18), nordwerk, [softwareentwickler, projektleitung], [lisa, mia]);

        await AuftragAnlegen(context, "Beleuchtungsumbau Bürotrakt",
            "Umbau auf energiesparende Beleuchtung im gesamten Bürotrakt.", "Linz",
            heute.AddDays(20), heute.AddDays(24), panoramaBau, [elektriker], [anna, sofia]);
        await AuftragAnlegen(context, "Service Verpackungsmaschine",
            "Geplanter Service mit Funktionsprüfung und Probelauf.", "Wels",
            heute.AddDays(27), heute.AddDays(29), demoIndustrie, [mechaniker, servicetechnik], [leon]);
        await AuftragAnlegen(context, "Lageroptimierung Zentrallager",
            "Umbau von Lagerplätzen und neue Kennzeichnung der Bereiche.", "Enns",
            heute.AddDays(34), heute.AddDays(38), zentraleLogistik, [lagerlogistik], [emma, lukas]);
        await AuftragAnlegen(context, "Projektstart Energiezentrale",
            "Technische Aufnahme und Koordination der ersten Umsetzungsschritte.", "St. Pölten",
            heute.AddDays(40), heute.AddDays(42), stadtwerkeDemo, [projektleitung, elektriker], [sofia]);
        await AuftragAnlegen(context, "Schweißkonstruktion Ladezone",
            "Fertigung und Montage einer Schutzkonstruktion für die Ladezone.", "Amstetten",
            heute.AddDays(47), heute.AddDays(51), zentraleLogistik, [schweisser], [max, leon]);
        await AuftragAnlegen(context, "Büroausstattung Neubau",
            "Montage von Schränken, Tischen und Empfangselementen.", "Krems",
            heute.AddDays(54), heute.AddDays(59), panoramaBau, [tischler], []);
        await AuftragAnlegen(context, "Software-Schnittstelle Produktion",
            "Einrichtung und Test der neuen Produktionsschnittstelle.", "Steyr",
            heute.AddDays(61), heute.AddDays(64), musterMaschinenbau, [softwareentwickler], [lisa, mia]);
        await AuftragAnlegen(context, "Pumpenwartung Wasserwerk",
            "Wartung, Dichtungstausch und abschließender Probelauf.", "Melk",
            heute.AddDays(68), heute.AddDays(71), stadtwerkeDemo, [mechaniker, servicetechnik], [hans, emma]);
        await AuftragAnlegen(context, "Baustrom Einrichtung",
            "Einrichtung und Prüfung der temporären Stromversorgung.", "Wiener Neustadt",
            heute.AddDays(75), heute.AddDays(78), panoramaBau, [elektriker], []);
        await AuftragAnlegen(context, "Fördertechnik Erweiterung",
            "Mechanische Erweiterung und elektrische Inbetriebnahme.", "Linz",
            heute.AddDays(82), heute.AddDays(87), alpenTechnik, [mechaniker, elektriker], [leon, sofia]);
        await AuftragAnlegen(context, "Inventur Ersatzteillager",
            "Bestandsaufnahme und Neuordnung der Ersatzteilplätze.", "Wels",
            heute.AddDays(89), heute.AddDays(91), nordwerk, [lagerlogistik], [emma]);
        await AuftragAnlegen(context, "Maschinenumbau Linie 3",
            "Umbau, Schweißarbeiten und technische Abnahme der Linie.", "Steyr",
            heute.AddDays(96), heute.AddDays(101), musterMaschinenbau, [mechaniker, schweisser], [max, hans]);
        await AuftragAnlegen(context, "Haustechnik Hoteltrakt",
            "Prüfung und kleinere Reparaturen im neuen Hoteltrakt.", "Bad Ischl",
            heute.AddDays(103), heute.AddDays(106), bergblickHotel, [elektriker, servicetechnik], [anna, emma]);
        await AuftragAnlegen(context, "Datenmigration Kundenportal",
            "Vorbereitung, Übernahme und Kontrolle der bestehenden Kundendaten.", "Wien",
            heute.AddDays(110), heute.AddDays(114), innovationszentrum, [softwareentwickler, projektleitung], [mia]);
        await AuftragAnlegen(context, "Werkstattmontage Prüfstände",
            "Montage und Ausrichtung von drei neuen Prüfständen.", "Amstetten",
            heute.AddDays(117), heute.AddDays(121), donauService, [mechaniker], [leon]);
        await AuftragAnlegen(context, "Prüfung Trafostation",
            "Sichtprüfung, Messungen und Dokumentation der Trafostation.", "Linz",
            heute.AddDays(124), heute.AddDays(126), stadtwerkeDemo, [elektriker], [sofia]);
        await AuftragAnlegen(context, "Ersatzteillager Neuaufbau",
            "Aufbau der Lagerstruktur und Einordnung des Startbestands.", "Enns",
            heute.AddDays(131), heute.AddDays(136), zentraleLogistik, [lagerlogistik], [lukas, emma]);
        await AuftragAnlegen(context, "Roboterschutzgitter",
            "Fertigung und Montage der neuen Schutzgitter.", "Wels",
            heute.AddDays(138), heute.AddDays(142), demoIndustrie, [schweisser], []);
        await AuftragAnlegen(context, "Jahreswartung Produktionshalle",
            "Gemeinsame Jahreswartung aller zentralen Produktionsanlagen.", "Linz",
            heute.AddDays(145), heute.AddDays(150), alpenTechnik, [mechaniker, elektriker, servicetechnik], [hans, anna, leon]);
        await AuftragAnlegen(context, "Empfangsausbau Hotel",
            "Fertigung und Montage einer neuen Empfangstheke.", "Bad Ischl",
            heute.AddDays(152), heute.AddDays(156), bergblickHotel, [tischler], [fritz, lukas]);
        await AuftragAnlegen(context, "Systemschulung Disposition",
            "Einrichtung der Schulungsumgebung und Einführung der Anwender.", "Wien",
            heute.AddDays(159), heute.AddDays(160), innovationszentrum, [softwareentwickler], [lisa]);
        await AuftragAnlegen(context, "Hallenabnahme Neubau",
            "Technische Endkontrolle und gemeinsame Abnahme der neuen Halle.", "Krems",
            heute.AddDays(166), heute.AddDays(168), panoramaBau, [projektleitung, elektriker], [sofia]);
        await AuftragAnlegen(context, "Winterservice Fuhrparkhalle",
            "Kontrolle der Hallentechnik und Vorbereitung auf den Winterbetrieb.", "St. Pölten",
            heute.AddDays(173), heute.AddDays(176), donauService, [servicetechnik], [emma]);

        await AuftragAnlegen(context, "Empfangstheke Reparatur",
            "Reparatur und Nachjustierung der bestehenden Empfangstheke.", "Linz",
            heute.AddDays(1), heute.AddDays(1), innovationszentrum, [tischler], []);
        await AuftragAnlegen(context, "Notbeleuchtung Lagerhalle",
            "Prüfung und Austausch mehrerer Leuchten der Notbeleuchtung.", "Enns",
            heute.AddDays(1), heute.AddDays(2), zentraleLogistik, [elektriker], []);
        await AuftragAnlegen(context, "Getriebetausch Verpackungslinie",
            "Ausbau des alten Getriebes und Einbau des vorbereiteten Ersatzteils.", "Wels",
            heute.AddDays(2), heute.AddDays(4), demoIndustrie, [mechaniker], []);
        await AuftragAnlegen(context, "Inventursoftware Einrichtung",
            "Einrichtung und Test der mobilen Inventurerfassung.", "Steyr",
            heute.AddDays(3), heute.AddDays(3), musterMaschinenbau, [softwareentwickler], []);
        await AuftragAnlegen(context, "Geländer Reparatur Ladehof",
            "Reparatur beschädigter Geländerteile und Kontrolle der Schweißnähte.", "Amstetten",
            heute.AddDays(4), heute.AddDays(6), donauService, [schweisser], []);
        await AuftragAnlegen(context, "Lagerplätze Neuordnung",
            "Neuordnung und Kennzeichnung der Stellplätze im Versandlager.", "Linz",
            heute.AddDays(5), heute.AddDays(5), nordwerk, [lagerlogistik], []);
        await AuftragAnlegen(context, "Torsteuerung Wartung",
            "Wartung der Torsteuerung und abschließender Funktionstest.", "Krems",
            heute.AddDays(6), heute.AddDays(8), panoramaBau, [servicetechnik], []);
        await AuftragAnlegen(context, "Baustellenstart Bürogebäude",
            "Technische Aufnahme und Koordination der ersten Arbeiten.", "Wien",
            heute.AddDays(7), heute.AddDays(7), panoramaBau, [projektleitung, elektriker], []);
        await AuftragAnlegen(context, "Zimmertüren Nacharbeiten",
            "Nacharbeiten und Einstellen mehrerer neu montierter Zimmertüren.", "Bad Ischl",
            heute.AddDays(8), heute.AddDays(9), bergblickHotel, [tischler], []);
        await AuftragAnlegen(context, "Produktionsdaten Export",
            "Erstellung und Test eines neuen Exports für Produktionsdaten.", "Steyr",
            heute.AddDays(9), heute.AddDays(11), musterMaschinenbau, [softwareentwickler], []);
        await AuftragAnlegen(context, "Hydraulikprüfung Hebeanlage",
            "Prüfung der Hydraulik und Austausch eines Verschleißteils.", "Wels",
            heute.AddDays(10), heute.AddDays(10), alpenTechnik, [mechaniker], []);
        await AuftragAnlegen(context, "Wareneingang Beschilderung",
            "Montage neuer Bereichsschilder und Aktualisierung der Lagerkennzeichnung.", "Enns",
            heute.AddDays(11), heute.AddDays(13), zentraleLogistik, [lagerlogistik], []);
        await AuftragAnlegen(context, "Unterverteilung Bürotrakt",
            "Prüfung und kleinere Erweiterung der elektrischen Unterverteilung.", "Linz",
            heute.AddDays(12), heute.AddDays(12), stadtwerkeDemo, [elektriker], []);
        await AuftragAnlegen(context, "Kompressor Störungsbehebung",
            "Fehlersuche, Reparatur und Probelauf des Werkstattkompressors.", "Amstetten",
            heute.AddDays(13), heute.AddDays(13), donauService, [mechaniker, servicetechnik], []);
        await AuftragAnlegen(context, "Schließanlage Hotelbereich",
            "Kontrolle und Nacharbeit der mechanischen Schließanlage.", "Bad Ischl",
            heute.AddDays(25), heute.AddDays(27), bergblickHotel, [servicetechnik], []);
        await AuftragAnlegen(context, "Montage Packtische",
            "Aufbau und Ausrichtung neuer Packtische im Versandbereich.", "Enns",
            heute.AddDays(32), heute.AddDays(34), zentraleLogistik, [tischler, lagerlogistik], []);
        await AuftragAnlegen(context, "Energiebericht Schnittstelle",
            "Einrichtung einer Schnittstelle für die monatlichen Energiedaten.", "St. Pölten",
            heute.AddDays(45), heute.AddDays(48), stadtwerkeDemo, [softwareentwickler], []);
        await AuftragAnlegen(context, "Treppengeländer Fertigung",
            "Fertigung und Montage eines neuen Treppengeländers.", "Krems",
            heute.AddDays(58), heute.AddDays(62), panoramaBau, [schweisser], []);
        await AuftragAnlegen(context, "Maschinenservice Außenstelle",
            "Geplanter Service und Kontrolle der Sicherheitseinrichtungen.", "Graz",
            heute.AddDays(72), heute.AddDays(75), demoIndustrie, [mechaniker, servicetechnik], []);
        await AuftragAnlegen(context, "Projektplanung Besucherzentrum",
            "Aufnahme der Anforderungen und technische Projektplanung.", "Wien",
            heute.AddDays(90), heute.AddDays(92), innovationszentrum, [projektleitung], []);

        await MaterialAnlegen(context, "Wartung Produktionsanlage", "Werkzeugkoffer", 1);
        await MaterialAnlegen(context, "Wartung Produktionsanlage", "Ersatzfilter", 2);
        await MaterialAnlegen(context, "Wartung Produktionsanlage", "Prüfprotokoll", 1);
        await MaterialAnlegen(context, "Elektroinstallation Werkhalle", "Kabelrolle", 3);
        await MaterialAnlegen(context, "Elektroinstallation Werkhalle", "Leuchten", 12);
        await MaterialAnlegen(context, "Möbelmontage Empfang", "Montageschrauben-Satz", 2);
        await MaterialAnlegen(context, "Beleuchtungsumbau Bürotrakt", "LED-Paneele", 24);
        await MaterialAnlegen(context, "Beleuchtungsumbau Bürotrakt", "Kabelkanal", 18);
        await MaterialAnlegen(context, "Service Verpackungsmaschine", "Wartungssatz", 1);
        await MaterialAnlegen(context, "Service Verpackungsmaschine", "Maschinenöl", 3);
        await MaterialAnlegen(context, "Lageroptimierung Zentrallager", "Regalschilder", 60);
        await MaterialAnlegen(context, "Lageroptimierung Zentrallager", "Bodenmarkierung", 8);
        await MaterialAnlegen(context, "Schweißkonstruktion Ladezone", "Stahlprofile", 16);
        await MaterialAnlegen(context, "Schweißkonstruktion Ladezone", "Schweißdraht", 4);
        await MaterialAnlegen(context, "Büroausstattung Neubau", "Montagesatz", 12);
        await MaterialAnlegen(context, "Büroausstattung Neubau", "Arbeitsplatten", 6);
        await MaterialAnlegen(context, "Pumpenwartung Wasserwerk", "Dichtungssatz", 2);
        await MaterialAnlegen(context, "Pumpenwartung Wasserwerk", "Prüfprotokoll", 1);
        await MaterialAnlegen(context, "Baustrom Einrichtung", "Baustromverteiler", 2);
        await MaterialAnlegen(context, "Baustrom Einrichtung", "Verlängerungskabel", 8);
        await MaterialAnlegen(context, "Fördertechnik Erweiterung", "Montagewinkel", 20);
        await MaterialAnlegen(context, "Fördertechnik Erweiterung", "Sensoren", 6);
        await MaterialAnlegen(context, "Maschinenumbau Linie 3", "Schutzbleche", 8);
        await MaterialAnlegen(context, "Maschinenumbau Linie 3", "Lagersatz", 3);
        await MaterialAnlegen(context, "Haustechnik Hoteltrakt", "Sicherungssatz", 2);
        await MaterialAnlegen(context, "Haustechnik Hoteltrakt", "Leuchtmittel", 20);
        await MaterialAnlegen(context, "Werkstattmontage Prüfstände", "Befestigungsanker", 24);
        await MaterialAnlegen(context, "Werkstattmontage Prüfstände", "Nivellierplatten", 12);
        await MaterialAnlegen(context, "Ersatzteillager Neuaufbau", "Lagerboxen", 80);
        await MaterialAnlegen(context, "Ersatzteillager Neuaufbau", "Etikettenrollen", 6);

        await AbwesenheitAnlegen(context, fritz, heute.AddDays(8), heute.AddDays(9), "Urlaub", "Genehmigt", "Demo-Urlaub");
        await AbwesenheitAnlegen(context, hans, heute.AddDays(15), heute.AddDays(16), "Urlaub", "Offen", "Geplanter Urlaub");
        await AbwesenheitAnlegen(context, anna, heute.AddDays(-5), heute.AddDays(-5), "Sonstige Abwesenheit", "Genehmigt", "Behördentermin");
        await AbwesenheitAnlegen(context, max, heute.AddDays(18), heute.AddDays(18), "Urlaub", "Abgelehnt", "Terminkonflikt");
        await AbwesenheitAnlegen(context, lisa, heute.AddDays(6), heute.AddDays(6), "Zeitausgleich", "Offen", "Überstundenabbau");
        var urlaubImVormonat = WerktagImMonat(heute.AddMonths(-1), 8);
        var krankenstandVorZweiMonaten = WerktagImMonat(heute.AddMonths(-2), 12);
        await AbwesenheitAnlegen(context, fritz, urlaubImVormonat, urlaubImVormonat.AddDays(1),
            "Urlaub", "Genehmigt", "Demo-Urlaub im Vormonat");
        await AbwesenheitAnlegen(context, lisa, krankenstandVorZweiMonaten, krankenstandVorZweiMonaten,
            "Krankenstand", "Genehmigt", "Demo-Krankenstand");
        await AbwesenheitAnlegen(context, sofia, heute.AddDays(38), heute.AddDays(39),
            "Urlaub", "Genehmigt", "Kurzurlaub im Herbst");
        await AbwesenheitAnlegen(context, lukas, heute.AddDays(50), heute.AddDays(54),
            "Urlaub", "Offen", "Geplanter Familienurlaub");
        await AbwesenheitAnlegen(context, mia, heute.AddDays(72), heute.AddDays(72),
            "Zeitausgleich", "Genehmigt", "Zeitausgleich Projekttag");
        await AbwesenheitAnlegen(context, leon, heute.AddDays(95), heute.AddDays(97),
            "Urlaub", "Abgelehnt", "Überschneidung Maschinenumbau");
        await AbwesenheitAnlegen(context, emma, heute.AddDays(12), heute.AddDays(12),
            "Sonstige Abwesenheit", "Genehmigt", "Privater Termin");
        await AbwesenheitAnlegen(context, hans, heute.AddDays(-28), heute.AddDays(-26),
            "Krankenstand", "Genehmigt", "Krankenstand im Vormonat");
        await AbwesenheitAnlegen(context, anna, heute.AddDays(32), heute.AddDays(35),
            "Urlaub", "Offen", "Urlaubsantrag Herbst");
        await AbwesenheitAnlegen(context, max, heute.AddDays(44), heute.AddDays(45),
            "Zeitausgleich", "Genehmigt", "Zeitausgleich nach Montage");
        await AbwesenheitAnlegen(context, lisa, heute.AddDays(108), heute.AddDays(112),
            "Urlaub", "Genehmigt", "Urlaub vor Datenmigration");
        await AbwesenheitAnlegen(context, fritz, heute.AddDays(149), heute.AddDays(153),
            "Urlaub", "Offen", "Winterurlaub");
        await AbwesenheitAnlegen(context, sofia, heute.AddDays(-45), heute.AddDays(-45),
            "Krankenstand", "Genehmigt", "Eintägiger Krankenstand");
        await AbwesenheitAnlegen(context, lukas, heute.AddDays(-70), heute.AddDays(-68),
            "Urlaub", "Genehmigt", "Urlaub im Frühsommer");
        await AbwesenheitAnlegen(context, mia, heute.AddDays(18), heute.AddDays(18),
            "Sonstige Abwesenheit", "Offen", "Weiterbildung");
        await AbwesenheitAnlegen(context, leon, heute.AddDays(132), heute.AddDays(134),
            "Urlaub", "Genehmigt", "Urlaub im Winter");
        await AbwesenheitAnlegen(context, emma, heute.AddDays(88), heute.AddDays(89),
            "Zeitausgleich", "Offen", "Zeitausgleich Lagerumbau");
        await AbwesenheitAnlegen(context, martin, heute.AddDays(60), heute.AddDays(64),
            "Urlaub", "Genehmigt", "Urlaub Administration");
        await AbwesenheitAnlegen(context, daniel, heute.AddDays(118), heute.AddDays(120),
            "Urlaub", "Offen", "Urlaub Disposition");
        await AbwesenheitAnlegen(context, max, heute.AddDays(-15), heute.AddDays(-14),
            "Krankenstand", "Genehmigt", "Kurzer Krankenstand");

        await HistorischeArbeitszeitenAnlegen(context, demoMitarbeiter, heute);

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
                Qualifikationen = new List<Qualifikation>()
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

    private static async Task HistorischeArbeitszeitenAnlegen(
        ApplicationDbContext context,
        List<Mitarbeiter> mitarbeiter,
        DateTime heute)
    {
        var vorhandeneDemozeiten = await context.Arbeitszeiten
            .Where(a => a.Notiz == "Development-Demodaten"
                || a.Notiz == "Demo-Arbeitszeit")
            .ToListAsync();

        context.Arbeitszeiten.RemoveRange(vorhandeneDemozeiten);

        var ersterTag = new DateTime(heute.Year, heute.Month, 1).AddMonths(-5);
        var letzterTag = heute.AddDays(-1);
        var genehmigteAbwesenheiten = await context.Abwesenheiten
            .Where(a => a.Status == "Genehmigt")
            .ToListAsync();
        var arbeitszeitService = new ArbeitszeitService(context);

        foreach (var person in mitarbeiter)
        {
            var datum = ersterTag;

            while (datum <= letzterTag)
            {
                var istWochenende = datum.DayOfWeek == DayOfWeek.Saturday
                    || datum.DayOfWeek == DayOfWeek.Sunday;
                var istFeiertag = arbeitszeitService.IstGesetzlicherFeiertag(datum);
                var istAbwesend = genehmigteAbwesenheiten.Any(a =>
                    a.MitarbeiterId == person.Id
                    && a.Von.Date <= datum.Date
                    && a.Bis.Date >= datum.Date);

                if (!istWochenende && !istFeiertag && !istAbwesend)
                {
                    var sollstunden = person.Wochenarbeitszeit / 5;
                    var abweichungInMinuten = ((datum.Day + person.Id) % 5 - 2) * 15;
                    var arbeitsminuten = (int)Math.Round(sollstunden * 60) + abweichungInMinuten;
                    var beginn = new TimeOnly(8, 0);
                    var pauseMinuten = arbeitsminuten > 360 ? 30 : 0;
                    var ende = beginn.AddMinutes(arbeitsminuten + pauseMinuten);

                    context.Arbeitszeiten.Add(new Arbeitszeit
                    {
                        MitarbeiterId = person.Id,
                        Datum = datum,
                        Beginn = beginn,
                        Ende = ende,
                        PauseMinuten = pauseMinuten,
                        Notiz = "Demo-Arbeitszeit"
                    });
                }

                datum = datum.AddDays(1);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task MaterialAnlegen(
        ApplicationDbContext context,
        string auftragstitel,
        string bezeichnung,
        int anzahl)
    {
        var auftrag = await context.Auftraege.FirstAsync(a => a.Titel == auftragstitel);
        var material = await context.Materialeintraege.FirstOrDefaultAsync(m =>
            m.AuftragId == auftrag.Id && m.Bezeichnung == bezeichnung);

        if (material == null)
        {
            material = new Materialeintrag
            {
                AuftragId = auftrag.Id,
                Bezeichnung = bezeichnung
            };
            context.Materialeintraege.Add(material);
        }

        material.Anzahl = anzahl;
        await context.SaveChangesAsync();
    }

    private static DateTime WerktagImMonat(DateTime datum, int tag)
    {
        var werktag = new DateTime(datum.Year, datum.Month, tag);

        while (werktag.DayOfWeek == DayOfWeek.Saturday
            || werktag.DayOfWeek == DayOfWeek.Sunday)
        {
            werktag = werktag.AddDays(1);
        }

        return werktag;
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
        var vorhandeneAbwesenheit = await context.Abwesenheiten.FirstOrDefaultAsync(a =>
            a.MitarbeiterId == mitarbeiter.Id
            && a.Grund == grund);

        if (vorhandeneAbwesenheit == null)
        {
            vorhandeneAbwesenheit = new Abwesenheit();
            context.Abwesenheiten.Add(vorhandeneAbwesenheit);
        }

        vorhandeneAbwesenheit.MitarbeiterId = mitarbeiter.Id;
        vorhandeneAbwesenheit.Von = von;
        vorhandeneAbwesenheit.Bis = bis;
        vorhandeneAbwesenheit.Typ = typ;
        vorhandeneAbwesenheit.Status = status;
        vorhandeneAbwesenheit.Grund = grund;

        await context.SaveChangesAsync();
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
