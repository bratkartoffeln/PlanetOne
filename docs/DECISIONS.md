# Entscheidungen und Plattformstrategie

## GUI nach Konzepttafel und Weltraumansicht (24. November 2026)

- **D-030, Vorgabe:** GUI an die vorhandenen Vorlagen anpassen, intuitiver und schöner gestalten; die Weltraumansicht soll ebenfalls der Vorlage entsprechen. Die drei wählbaren Layouts und die räumliche Darstellung bleiben erhalten.
- **T-039, entschieden:** Version 1.0.11 trennt Auswahl von Verwaltung. Planetenklick bleibt im System; Oberflächenaktion beziehungsweise Doppelklick öffnet die Welt. A nutzt eine feste rechte Verwaltung, B eine linke Navigation und ein breites Befehlsdeck, C ein schwebendes Dock. B/C zeigen kompakte Auswahlkarten; umfangreiche Funktionen öffnen eigene Arbeitsbereiche. Der Koloniename öffnet die Übersicht; Testszenarien und Sitzungswechsel liegen im Spielmenü.
- **T-040, entschieden:** Eigene Imagegen-Texturen für Planeten und Nebel, echte Kugelgeometrie mit sonnenabhängiger Tag-/Nachtseite, Atmosphären und Korona. Der Hintergrund folgt der Kamera. Schiffe werden auf einer Darstellungsebene in Höhe 2,3 gerendert, damit große Planetenkörper sie nicht verschlucken; Auswahl, Markierungen und Zielprojektion verwenden dieselbe Ebene. Keine Änderung der simulierten Flugpositionen, Reichweiten, Planetenzahl oder Bestände. Savegame-Schema bleibt 8.
- **T-041, Nachweis:** Auswahl, Menüs und Zielbefehle über die tatsächlichen Controllerpfade testen; vollständige GUI-Bilder in zwei Auflösungen auf getrenntem unsichtbarem Desktop. Bildkontrolle und automatisierte Bedienpfade gelten nicht als vollständiger manueller Klicktest oder pixelgleiche Abnahme. [Ergebnisse](GUI_WELTRAUM_NACHWEIS.md), [Assets und vollständige Prompts](ART_ASSETS.md).

## Räumliche Grafik und natürliche Landschaft (24. November 2026)

- **D-029, Vorgabe:** Die vom Nutzer ausgewählte Konzeptgrafik soll bildgetreu (»1:1«) umgesetzt werden; die frühere Einordnung als unverbindliche Stilrichtung genügt ausdrücklich nicht. Gelände soll natürlich wirken und Berge/Täler besitzen. Gebäude, Fahrzeuge, Raumschiffe und Weltobjekte werden vollständig dreidimensional gestaltet; Kamera räumlich drehbar, Fahrzeugausrichtung abhängig von der Fahrtrichtung. Alle drei Layouts bleiben wählbar.
- **T-036, entschieden:** Version 1.0.10 setzt geschlossene räumliche Rümpfe, zusammengesetzte Gebäudemodelle, Glas-/Materialshader, Schatten und eine dreh-/neigbare orthografische Kamera ein. Keine Bildtafeln als Ersatz für Gebäude oder Fahrzeuge. Die generierten Modelltafeln dienen nur als Referenz; `docs/GUI_REFERENCE.png` hält die ursprüngliche Zielgrafik fest. Geometrie und aktuelle Renderbilder sind von dieser Zielgrafik zu unterscheiden.
- **T-037, Arbeitsannahme:** Relief ist deterministische Darstellung anhand Weltkennung und Abmessungen. Die sechsseitige Spiellogik, Sperrfelder, Reichweiten und Bestandsregeln bleiben unverändert; Höhe führt keine unangekündigten Kampfboni oder neuen Unpassierbarkeiten ein. Straßen/Fahrzeuge folgen der Oberfläche, Gebäude stehen auf waagerechten Fundamenten. Darstellungsauswahl verwendet den ersten sichtbaren Geländetreffer. Kamera und Blickrichtung bleiben lokale Präsentationszustände; Savegame-Schema 8 unverändert.
- **T-038, Prüfung:** Bestehende Core-Regressionen, Windows-Build und isolierter Playerlauf; zusätzlich vier Kamerarichtungen, Reliefhöhen, Auswahl sichtbarer Waben und tatsächliche Bewegungsrichtung von Boden-/Raumeinheiten. Kein Screenreader, Computer-Use oder Umschalten des Arbeitsdesktops. [Ergebnisse und offene Grenzen](3D_GRAFIK_NACHWEIS.md).

## GUI und Grafik (24. November 2026)

- **D-028, Vorgabe:** Komplette GUI und Grafik mit Texturen überarbeiten. Referenzen: Command & Conquer, Haegemonia und Solar 2. Nach Vorlage dreier Layoutkonzepte beauftragt der Nutzer ausdrücklich alle drei mit Auswahl in den Einstellungen. Dies erweitert den früheren minimalistischen Assetumfang; Waben und Spielregeln bleiben erhalten.
- **T-034, entschieden:** Version 1.0.9 baut auf dem M7-Branch 1.0.8 auf. Drei Layouts teilen eine Bedienlogik und dieselbe Simulation. Anzeigeeinstellungen liegen separat in `presentation.json`; das Savegame-Schema bleibt unverändert. Eigener Materialatlas, prozedurale Materialien und Katalogbilder aus den realen Modellen; keine kopierten Assets der Referenzspiele.
- **T-035, entschieden:** Vollständige GUI-Bilder über einen separaten Windows-Desktop mittels `CreateDesktop`/`CreateProcess`, ohne `SwitchDesktop` oder globale Eingabe. Zusätzlich bleiben fensterlose GPU- und Core-Tests erhalten. Physische manuelle Bedienung und M8-Plattform-/Lastabnahme werden nicht daraus abgeleitet. [Nachweis](GUI_GRAFIK_NACHWEIS.md).

Stand: 24. November 2026. Unity mit C# ist eingerichtet; Produktversion 1.0.8 ergänzt lokal geprüftes M7 auf M6 und dem Hexraster. M5 und CI sind über PR #4/#5 in main integriert; die vollständige manuelle M5-Abnahme bleibt offen. Diese Datei unterscheidet bindende Produktvorgaben, technische Festlegungen und den tatsächlich nachgewiesenen Umfang.

## Diplomatie und Sieg: M7 (24. November 2026)

- **D-027, Vorgabe:** Nächsten Meilenstein M7/P16–P18 umsetzen; Tests weiterhin ohne Screenreader/Computer-Use und ohne Störung des Arbeitsdesktops. Der versionsbasierte Git-Ablauf bleibt beauftragt.
- **T-028, entschieden:** Version 1.0.8 baut auf 1.0.7 auf; PR #6/#7 waren zu Beginn weiterhin offen und main noch auf M5. Kein Umgehen der fehlenden Unity-CI-Aktivierung. Drei friedlich startende unabhängige Fraktionen, zwei KI und ein neutraler Mond als M7-Partie; zweiter Mond und vollständige Gesamtprüfung bleiben M8.
- **T-029, Arbeitsannahme/entschieden:** Verträge mit expliziter Zustimmung und getrennten Rechten. Standarddauer 12.000 Ticks, Kündigungsfrist 1.200 Ticks; geschützte Orbitbereiche Radius 3. Handel wird durch eigene Frachter beider Partner physisch erfüllt; Waren bleiben bis tatsächlicher Ankunft beim Lieferanten und kehren bei Abbruch real zurück. Verteidigungspakt verspricht den paarweisen Kriegseintritt bei äußerer Kriegserklärung, ohne andere Schutzverträge still zu brechen. Exakte Kräfte zusätzlich über Operationszusagen.
- **T-030, entschieden:** Einstimmige Koalitionen teilen Sensoren, nicht Bestände oder Steuerung. Gemeinsame Operationen reservieren vorhandene Schiffe und legen Ziel/Eigentümer fest. Gemeinsame Infanterie erhält Besetzungsfortschritt. 2.400 Ticks lokale Räumung schützen ausschließlich die bisherigen Kontingente und erlauben Abzug/Verladung statt Innenangriff; keine Löschung bei Fristablauf.
- **T-031, Arbeitsannahme:** Sprungtor benötigt Sensoren/Elektronik, 40 Metall/16 Bauteile für den Bau, acht Energie/vier Arbeit und 12 Nahrung/40 Metall/24 Bauteile/8 Kristalle/8 Elektronik für das Projekt. Öffentliche Sicherung dauert 1.200 Ticks. Komponentenverbrauch erst beim Erfolg; Unterbrechung setzt nur Fortschritt zurück. Kontrolle, Versorgung und Mitglieder werden vor Abschluss geprüft. Alle Zahlen liegen in Definitions-/Vertragsdaten.
- **T-032, entschieden:** Einmaliges Ausscheiden beendet Befehle, Produktion und Verpflichtungen. Neutraler Gebäude-Restbesitz behält reale Vorräte; ausdrückliche beidseitige Kapitulationsvereinbarung kann bestehende IDs übergeben. Kolonieschiffe zählen nur ausgerüstet und bei tatsächlich erreichbarer legaler Gründungswelt. Spielstandformat 8 migriert V1–V7 ohne nachträgliche Diplomatieaktivierung. [M7_NACHWEIS](M7_NACHWEIS.md) trennt 126 Core-Tests, Windows-Build, 131 Player-Prüfungen und die offene manuelle/CI-Abnahme.

## Eigenständige KI: M6 (24. November 2026)

- **D-026, Vorgabe:** Mit dem nächsten Meilenstein M6/P14–P15 fortfahren. Tests weiterhin ohne Screenreader/Computer-Use und ohne Eingriffe in den Arbeitsdesktop.
- **T-024, entschieden:** Produktversion 1.0.7 auf eigenem Versionsbranch, aufbauend auf dem noch offenen Hexraster-PR #6. Separate reguläre Partie mit einer KI und gleichen Anfangsmitteln; Aster gehört dem Spieler, Nereid der KI, Krieg ist sichtbar deklarierte Szenariovorgabe. Keine neuen Ressourcenboni oder Startarmeen.
- **T-025, entschieden:** Abgetrennte Wahrnehmungsdaten vor dem Entscheider, regelbasierte Planung alle 40 Ticks (zwei Sekunden), ausschließlich gemeinsame validierte Befehle. Statische Geografie kartierter Welten ist öffentlich, Infrastruktur, Rohstoffabbau und fremde Inventare nicht. Wirtschaft und Operationsplanung erhalten abwechselnd Planungsgelegenheiten; Aufklärung darf andere Aufträge nicht dauerhaft verdrängen.
- **T-026, Arbeitsannahme:** Eine exklusive Offensive je KI; mindestens drei tatsächlich geladene Truppen einschließlich Pionier, Eskorte und Startversorgung. Wiederaufbaureserve 16 Metall/4 Bauteile, begründete Ausnahme für Grundversorgung; 60 Sekunden Wiederanlaufsperre nach Abbruch. Transportengpässe werden durch Bedarfs-/Verbrauchssteuerung behandelt, nicht durch zusätzliche kostenlose Kuriere.
- **T-027, entschieden/Nachweis:** Speicherformat 7 erhält KI-Wissen, Profile, Planungszeit, Operationen und Entscheidungsprotokoll; V1–V6 erhalten eine leere KI-Liste. 96 Core-Tests, Windows-Build und 113 fensterlose Player-Prüfungen; echte Kolonisierung und separate taktische Invasionsfixture geprüft. Keine vollständige manuelle IMGUI-Abnahme. Details: [M6_NACHWEIS](M6_NACHWEIS.md).

## Wabenraster (24. November 2026)

- **D-024, Vorgabe:** Bodenfelder projektweit als Sechsecke in Wabenstruktur statt Schachbrett; Darstellung und Spielregeln gemeinsam umstellen.
- **T-022, entschieden:** Odd-r-Hexraster, sechs Nachbarn, eine Weltlängeneinheit je Nachbarkante. Gemeinsame Core-Geometrie für Auswahl, Wege, Deckung, Bodenreichweite und Sicht. Landezone aus Zentrum plus sechs Nachbarn, weiterhin vier Truppenplätze. Dies ersetzt die vier Nachbarfelder aus T-009 und die 2×2-Fläche aus T-020.
- **T-023, entschieden:** Speicherformat 6 erhält alte Feldadressen, Zwischenpositionen, Wege und alle Bestände. Alte begonnene Landungen werden beim Abschluss erneut geprüft. 82 Core-Tests, Windows-Build und fensterloser GPU-Integrationstest; Details und Grenzen in [HEXRASTER_NACHWEIS](HEXRASTER_NACHWEIS.md).
- **D-025, Vorgabe/Nachweis:** Auf ausdrückliches „ja“ des Nutzers ist der klassische main-Branchschutz für Administratoren nicht mehr erzwungen. PR-Pflicht und Pflichtcheck bleiben; die bestehende Ruleset-Ausnahme gilt für Admin-PRs. Das ist keine pauschale Freigabe zum Umgehen fehlgeschlagener Checks bei Entwicklungsabschlüssen.

## Ergänzungen für M5 (24. November 2026)

- **D-023, Vorgabe:** Nächsten Meilenstein M5 mit P11–P13 umsetzen. Tests ohne Screenreader; nachfolgend ausdrücklich ohne Computer-Use-Skill und ohne Störung des laufenden Arbeitsdesktops. VirtualBox ist bei Bedarf als isolierte Testumgebung erlaubt.
- **T-019, entschieden:** Produktversion 1.0.5 auf separatem Versionsbranch, abhängig vom noch ungemergten CI-Stand 1.0.4. Speicherformat 5 ergänzt wirkliche Truppenobjekte im Träger, Hülle/Munition, Werftproduktion, Übergangsphasen, Landeköpfe und lokalen Gebäudebesitz; V4 migriert ohne Ressourcenänderung.
- **T-020, Arbeitsannahme:** Orbitalwerft als planetengebundene Station aus realem Hafenbau; militärischer Werftbau mit einzelnen Warenübertragungen. Vier Transportplätze, ursprünglich lokale 2×2-Landezonen (ab 1.0.6 durch T-022 ersetzt), acht Sekunden Übergang/Sicherung, sechs Sekunden Feldbau/Verwaltungsreparatur. Strategischer Besitz und örtlicher Anlagenbesitz bleiben getrennt; fremde aktive Anlagen werden nicht automatisch übernommen.
- **T-021, Nachweis:** 74 Core-Tests, Windows-Build und 103 grafische Einzelprüfungen im echten Player; M5-Renderbilder als Dateien kontrolliert. Vollständige manuelle Bedienungsabnahme und GitHub-Unity-Build bleiben offen. Details, Grenzen und Tests ohne Eingriffe in den Arbeitsdesktop: [M5_NACHWEIS](M5_NACHWEIS.md).

## Ergänzung CI und Releases (24. November 2026)

- **D-022, Vorgabe:** GitHub testet und baut PRs gegen `main` als Merge-Voraussetzung. Eine ausschließlich manuell gestartete Release-Pipeline bietet den aktuellen Build zum Download an.
- **T-018, entschieden:** Gehostete GitHub-Runner, Core-/Versionsprüfungen, Unity-Windows-Build und Installer-Paketierung mit gemeinsamem Pflichtcheck. Release übernimmt das geprüfte Artefakt des exakten aktuellen `main`-Commits. Installer pro Benutzer sowie portable ZIP; keine kostenpflichtige Signatur und kein lokaler öffentlicher PR-Runner. Unity-Secrets sind eine separat einzurichtende Voraussetzung. Nachweise und Bedienung: [CI_RELEASE](CI_RELEASE.md).

## Ergänzungen für M4

- **D-021, Vorgabe:** Nach abgeschlossenem M3 den nächsten Meilenstein umsetzen; Nutzerauftrag vom 23. November 2026. M4 umfasst P08–P10, einschließlich Prüfungen und beauftragter Versionsintegration.
- **T-012, entschieden:** Speicherformat 4 ergänzt Schiffe, Frachtaufträge, Routen, Hafenregeln, Forschung und Raumaufklärung. Migration erfolgt V1 → V2 → V3 → V4. Produktversion 1.0.3 bleibt unabhängig.
- **T-013, Arbeitsannahme:** Ein Schiffsbauauftrag je Hafen, ein Forschungsvorhaben je Fraktion, vier zivile Bewohner als Rekrutierungsuntergrenze. Ein Kolonieschiff kostet inklusive Startladung 44 Nahrung/100 Metall/32 Bauteile und zwölf Bewohner; übertragen werden 40 Nahrung/80 Metall/20 Bauteile und seine tatsächliche Besatzung. Gründung bei (8,6) mit Verwaltung und entfalteten Grundstraßen. Alle Kosten und Zeiten stehen in zentralen Definitionen.
- **T-014, Arbeitsannahme:** Hafenpuffer konkretisieren die Kolonie-Versorgung je Gut. Mindestbestand, Ziel, Exportreserve und Priorität; lokale und interplanetare Zusagen zählen mit. Frachter tragen bis zu 40 Waren eines Typs, Hafenanschluss transferiert eine Ware je 0,5 Sekunden und bedient ein Schiff zugleich. Abgebrochene beladene Aufträge suchen den nächsten eigenen Rücknahmehafen mit Platz; ohne solchen bleibt die Ware an Bord. Gefahrenauswertung mit realem Raumkampf folgt P11.
- **T-015, Arbeitsannahme:** Kurzer zirkelfreier Forschungsgraph: Raumtransport → Koloniemodule/Kristallabbau → Elektronik → Sensoren. Unbewaffnetes Sensorschiff als tatsächliche spätere Freischaltung; keine Kriegsschiffproduktion aus P11 vorgezogen. Laborverlust erhält Fortschritt und bezahlte Proben, neuer Standort wird ausdrücklich zugeordnet.
- **T-016, entschieden:** Die M4-Demonstration wird aus normalen Anfangsbeständen ausschließlich durch reguläre Simulationsbefehle vorbereitet. Keine eingesetzten Zusatzschiffe, Forschungsfreigaben oder Rohstoffe. Eigener Speicherpfad und Rückkehr zur vorherigen Sitzung; normale Starts bleiben unverändert bis auf das endliche Kristallvorkommen des neutralen Mondes.
- **T-017, Nachweis:** 58 Core-Tests, Windows-Build, 91 grafische Einzelprüfungen und elf kontrollierte Renderbilder unter `TestResults/Smoke/20261123-202734`. Native Prüfung bestätigt Flug, Ansicht, Gründung, Save/Load, Forschungsauftrag, Hafenregel, freie Erkundungsbewegung und Rahmenauswahl. Vollständige Kristall-/Elektronikkette im Core nachgewiesen. Details und Grenzen in [M4_NACHWEIS](M4_NACHWEIS.md).

## Ergänzungen für M3

- **D-020, Vorgabe:** Nach Rekonstruktion und Dokumentation des vollständigen Spielkonzepts direkt M3 mit P06/P07 umsetzen. Nutzerauftrag vom 23. November 2026; [SPIELKONZEPT](SPIELKONZEPT.md) bündelt die erhaltenen Fachregeln, ohne ein Chattranskript zu behaupten.
- **T-007, entschieden:** Speicherformat 3 erweitert Einheiten, Aufträge, Sichtungen, Schäden, Rekrutierung, Vorräte und Szenario-Torrechte. V1 wird über die bestehende V2-Migration übernommen; V2 erhält leere Militärzustände und intakte Gebäude. Produktversion 1.0.2 bleibt davon unabhängig.
- **T-008, Arbeitsannahme:** Eine Rekrutierung je Kaserne/Fahrzeugwerk; reale Anlieferung, Zeit und Verbrauch von Bewohnern bis zur verbleibenden Vier-Personen-Basis. Startmunition und Rationen sind in den Rekrutierungskosten enthalten. Freiwilliger Abbruch durch Rückbau führt investierte Rekrutierungsgüter lokal und Bewohner zur Kolonie zurück; Zerstörung erstattet sie nicht.
- **T-009, Arbeitsannahme:** Ursprüngliche Bodennavigation über vier Nachbarfelder (ab 1.0.6 sechs gemäß T-022), keine individuelle Kollisions-/Formationssimulation. Infanterie nutzt benachbarte Felsen und Gebäude als Deckung und darf eigene fertige zivile Gebäude betreten. Gegnerbesetzung bleibt P13. Türme haben zehn Einheiten lokalen Energiespeicher und bei Netzausfall eine vorhandene örtliche Besatzung; Nachschub und Reparatur bleiben endlich. Werte stehen in zentralen Definitionen.
- **T-010, entschieden:** Die separat gestartete M3-Prüfkarte kennzeichnet fertige Gebäude, Armeen und Vorräte ausdrücklich als endliche Testfixture. Sie nutzt einen getrennten Speicherpfad und hält die vorherige Koloniesitzung im Speicher. Die normalen Spielstarts erhalten keine kostenlosen Militärkräfte. Flugabwehr prüft bereits `Ground`, `LandingTransition` und `Space`; reguläre Träger folgen P12.
- **T-011, Nachweis:** 43 Core-Tests, erfolgreicher Windows-Build, 71 grafische Einzelprüfungen und sieben kontrollierte Renderbilder in `TestResults/Smoke/20261123-181956`. Echte Maus-/Tastaturprüfung auf Windows bestätigt Auswahl, korrigierten Auswahlrahmen, Gruppen, beide Angriffsrouten, Rückzug, F5/F9 und Straßenunterbrechung mit anschließend erschöpftem Turmpuffer. Details in [M3_NACHWEIS](M3_NACHWEIS.md). Dies ergänzt T-006; die vollständige manuelle ältere Wirtschaftsabnahme und andere Plattformen bleiben offen.

## Statusbedeutung

- **Vorgabe:** Ausdrücklicher Nutzerwunsch, der bei der Umsetzung zu erhalten ist.
- **Arbeitsannahme:** Konkrete Ausgestaltung des bisherigen Konzepts; bei neuen Erkenntnissen begründet änderbar.
- **Offen:** Noch keine Auswahl; vor dem genannten abhängigen Schritt auflösen.
- **Entschieden:** Spätere technische Auswahl mit Datum, Begründung und Konsequenzen eintragen.

## Produktentscheidungen

| ID | Status | Entscheidung | Konsequenz |
|---|---|---|---|
| D-001 | Vorgabe | Echtzeit-Aufbau überwiegend im Weltraum; begehbare Ansichten von Planetensystemen und bebaubare Oberflächen | Beide Ebenen verwenden eine gemeinsame Zeit und Welt |
| D-002 | Vorgabe | Planeten umkreisen unterschiedliche Sterne; Monde sind möglich | Bewegte Bezugspunkte und mehrere Himmelskörpertypen im Datenmodell |
| D-003 | Vorgabe | Physischer Transport, Produktionsketten und Forschung mit Freischaltungen | Waren, Kapazitäten und Voraussetzungen dürfen nicht nur kosmetisch sein |
| D-004 | Vorgabe | Tatsächliche steuerbare Bodengefechte bei Invasionen | Kein Ersatz durch eine globale Eroberungsuhr |
| D-005 | Vorgabe | Tower-Defense-Elemente | Feste Verteidigungen, Versorgungsabhängigkeit und adaptive Angreifer |
| D-006 | Vorgabe | Einzelspieler mit einem oder mehreren intelligenten Computergegnern; Kooperation möglich | Eigene KI-Wirtschaft, Diplomatie, Koalitionen und gemeinsamer Sieg |
| D-007 | Vorgabe | Breite Plattformunterstützung als Auswahlkriterium | Exportfähigkeit muss für die gewählte Kombination aus Engine und Sprache geprüft werden |
| D-008 | Vorgabe | Unity mit C# einsetzen | Ersetzt die frühere Vorgabe, die Engine offenzulassen; Unity-Einrichtung und Projektumsetzung sind beauftragt |
| D-009 | Arbeitsannahme | Erste vollständige Karte: ein System, drei Startwelten, zwei Monde, Spieler und zwei KI | Ausreichend für Gegner, Verbündete und Konflikte um Expansion |
| D-010 | Arbeitsannahme | Sechs Handelsgüter, lokale Militärdepots, kein Treibstoff für Grundbewegung | Umfang der Warenketten begrenzen; Transport und Nachschub trotzdem real |
| D-011 | Arbeitsannahme, Darstellung erweitert durch D-029 | Taktische Bewegungen auf Ebenen, vollständige räumliche Darstellung | Dreh-/neigbare orthografische Kamera, 3D-Modelle und Relief im Built-in-Renderer; keine frei steuerbare dritte Simulationsachse im Grundspiel |
| D-012 | Arbeitsannahme | Taktische Pause; alle Schauplätze laufen sonst gleichzeitig | Bedienung und KI nicht an die aktive Kamera koppeln |
| D-013 | Arbeitsannahme | Gemeinsames Sprungtorprojekt als erstes Szenarioziel | Kein erzwungener Schlusskrieg zwischen Verbündeten |
| D-014 | Arbeitsannahme | Desktop zuerst mit Maus und Tastatur; andere Eingaben als getrennte Adapter | Breite Portierbarkeit vorbereiten, ohne alle Plattformen gleichzeitig zu versprechen |
| D-015 | Arbeitsannahme | Keine Mehrspieler-, Cloud- oder ML-Abhängigkeit im ersten Spiel | Lokale Simulation und gewöhnliche planende Spiel-KI genügen |
| D-016 | Vorgabe | Minimalistische optische Gestaltung | Einfache Formen, kleine Farbpalette, ruhige Oberfläche und klar erkennbare Zustände |
| D-017 | Vorgabe | Erstes Goal: lauffähiger Unity-Prototyp mit gemeinsamer Sonnensystem- und bebaubarer Oberflächensimulation | Zuerst einen zusammenhängenden Aufbauablauf mit Pause und Speicherstand fertigstellen |
| D-018 | Vorgabe | Abgeschlossene Entwicklungsarbeit über Versionsbranch, sinnvollen Commit, Remote-Push und PR-Merge integrieren; anschließend lokal zum Hauptbranch zurückkehren | Projektspezifischer Skill unter `.agents/skills/commit-pr`; Ziel und Hauptbranch bleiben nach ausdrücklicher Klärung `main` |
| D-019 | Arbeitsannahme | Zentrale Produktversion `MAJOR.MINOR.PATCH` in `VERSION`; Standarderhöhung Patch, Branchname entspricht exakt der Zielversion | Ausgangswert des bisherigen Unity-Prototyps `1.0` wird als `1.0.0` normalisiert; nächster Arbeitsstand `1.0.1`. Unity-Produktfelder werden synchronisiert, Plattform-Buildzähler monoton erhöht; Engine-, Paket- und Savegame-Versionen bleiben getrennt |

## Technischer Entscheidungsstand für M0

| ID | Festlegung oder offener Nachweis | Entscheidungsnachweis | Zeitpunkt |
|---|---|---|---|
| T-001 | Unity 6000.3.29f1 mit C#; Revision 96d2fe17529c, installiert und aktiviert | Nutzerauswahl vom 23. November 2026; Release vom 19. November 2026 über offizielle Unity-Release-API ermittelt; Projektimport und Windows-Mono-Build erfolgreich | M0 abgeschlossen |
| T-002 | Windows mit Maus und Tastatur als erster Build; weitere Desktop-, Web- und Mobilziele später, Konsolen erst mit passenden Zugängen | Entwicklungsreihenfolge aus Portabilitätsziel und vorhandener Windows-Umgebung; noch keine Zusage ungeprüfter Exporte | M0 und jeweilige Exportphase |
| T-003 | Einfache niedrigpolygonale 3D-Geometrie mit orthografischer Kamera und Unity Built-in-Renderer | Beide Ansichten im sichtbaren Player gerendert und anhand der Bilder geprüft; vollständiger manueller Bedienungstest bleibt offen | Erster sichtbarer Prototyp geprüft |
| T-004 | Vorläufige Testumgebung: Windows 11 Professional 10.0.26300, 32.530 MB gemeldeter RAM, RTX 3060 Ti mit 8 GB, Direct3D 12, 1440 × 900 | Zwei eigene Kolonien, ein neutraler Mond, je 18 × 14 Felder, vier Kuriere pro Kolonie; CPU nicht erhoben, keine Last- oder Performancegarantie für das vollständige Spiel | Referenz für ersten Prototyp; Leistungsbudget vor M8 festlegen |
| T-005 | Unity-Editor für Projektbuild; `tools/Unity.ps1` für Prepare, Build und Open; `tools/Test-Core.ps1` für Kernregeln; `tools/Test-Prototype.ps1 -Visible` für den Player | Produktversion 1.0.1: Build Exitcode 0, 28 Core-Tests bestanden; sichtbarer Integrationstest mit 58 Prüfbedingungen und vier Renderbildern bestanden | Koloniewirtschaft automatisiert nachgewiesen |
| T-006 | Technische Abnahme von M1/M2 über Core-Regeltests, normale Controller-Bedienpfade im sichtbaren Player und visuelle Kontrolle der Renderbilder | Lokale Arbeitsentscheidung: Der native Desktopzugriff fehlt trotz Wiederholungsversuchen. Der zusätzliche physische Maus-/Tastaturtest wird als offene Praxisprüfung weitergeführt und nicht als bestanden behauptet. Diese Entscheidung ändert keine Spielregeln und ist keine angenommene Nutzerbestätigung. | 1.0.1; zusätzliche Praxisprüfung spätestens in M8 |

M0 sowie M1 und M2 sind technisch abgenommen; für M1/M2 gilt die ausdrücklich dokumentierte Prüfmethode T-006. Die zusätzliche manuelle Bedienungsprüfung bleibt offen. Historischer Stand nach T-006: P08 enthielt nur den Ansichts-/Orbitanteil; P08–P10 sind inzwischen nach T-017 abgeschlossen. Abhängige Pakete erhalten ihren Abschlussstatus erst nach ihren eigenen Nachweisen; weitere Freigaberunden für bereits beauftragte Arbeit sind daraus nicht abzuleiten.

Die Editorwerkzeuge verwenden die statischen Einstiegspunkte `PlanetOne.Editor.BuildTools.PrepareProject` und `PlanetOne.Editor.BuildTools.BuildWindows`. Beide wurden erfolgreich ausgeführt. Der Player liegt unter `Builds/Windows/PlanetOne.exe`.

## Verifizierter lokaler Werkzeugstand

- Unity-Editor: `C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe`.
- Unity Hub: Version `4.0.1.65535`. Die erforderliche Lizenzaktivierung hat der Nutzer selbst durchgeführt.
- C#-Regeltests: Roslyn aus Visual Studio Build Tools, Console-Testlauf ohne dotnet-SDK; Protokoll `TestResults/Core/results.txt` mit 28 bestandenen Tests.
- Player: Windows, Mono-Scripting-Backend. Projektvorbereitung und Build sind mit den Skripten aus der README reproduzierbar; Logs liegen unter `Logs`.
- Grafischer Integrationsnachweis für 1.0.1: `tools/Test-Prototype.ps1 -Visible`, 58 bestandene Prüfbedingungen und `space.png`, `surface.png`, `surface-details.png`, `surface-restored.png` unter `TestResults/Smoke/20261123-172324`. Alle vier Bildinhalte wurden visuell kontrolliert; der Player wurde zuvor erfolgreich gebaut.
- Versionswerkzeuge: `Tests/Tools/Test-Version.ps1` besteht mit zwölf Tests und 19 isolierten Fixtures. `tools/Version.ps1 -Action Check` bestätigt drei Produktversionsfelder und vier Plattform-Buildzähler. Windows-Player und Versionsbranch führen den Produktstand 1.0.1.

Der grafische Ablauf setzt Funktionen programmatisch im tatsächlichen Player um. Er ersetzt keinen vollständigen manuellen Klicktest. Ein anfangs versteckter Testlauf lieferte keinen verwendbaren Backbuffer; der dokumentierte Bildnachweis benötigt daher ein sichtbares Fenster und `-Visible`. Andere Plattformen und das spätere große Lastprofil sind noch nicht geprüft.

In der verwendeten Agent-Sandbox war der lokale Unity-LicenseClient für den Build nicht erreichbar. Die Ausführung außerhalb dieses Sandbox-Kontexts war erfolgreich; normale PowerShell-Aufrufe auf dem Rechner benötigen deshalb keine erneute Lizenzaktivierung. Bei automatisierter Wiederholung diese Werkzeugzugriffsgrenze von einem tatsächlichen Lizenzfehler unterscheiden.

## Minimalistischer Gestaltungsrahmen

- Dunkler Weltraum und klar begrenzte, ruhige Geländegrundflächen.
- Einfache geometrische Planeten, Gebäude und Transportmarker; keine externen Detailassets als Voraussetzung des Prototyps.
- Wenige Akzentfarben für Auswahl, funktionsfähige Anlagen und Warnungen; wichtige Zustände zusätzlich durch Beschriftung oder Form vermitteln.
- Schmale Informationsbereiche mit klarer Hierarchie: Ort, Zeit, lokale Waren, Auswahl und aktuell mögliche Handlung.
- Sichtbare Umlaufbahnen, Bauvorschau und Lieferbewegungen erklären Funktion. Dekorative Effekte dürfen diese Informationen nicht überdecken.
- Texte und Eingabeflächen bleiben skalierbar. Keine optische Vereinfachung rechtfertigt unlesbare Schrift oder ausschließlich farbcodierte Fehlermeldungen.

## Hintergrund der Plattformentscheidung

Die Programmiersprache allein bestimmt die Plattformabdeckung nicht. Exportwerkzeuge, Renderer, benötigte Bibliotheken, Eingabekonzept und die Zugänge zu Zielgeräten gehören zur Auswahl.

| Kandidat | Nutzen für dieses Projekt | Plattformaspekt |
|---|---|---|
| C# mit Unity | Kandidat bei Priorität auf breite Zielplattformen und strukturierte Simulationslogik | Unity dokumentiert Windows, macOS, Linux, Android, iOS und Web; geschlossene Plattformen erfordern entsprechenden Zugang |
| GDScript mit Godot | Kandidat bei einer offenen Engine und Desktop-, Mobil- sowie Browserzielen | Godot dokumentiert diese Exporte; Konsolen benötigen eigene oder externe Exportlösungen |
| C# mit Godot | Kandidat, wenn C# gewünscht und Browser kein Pflichtziel ist | Laut aktueller Godot-4-Dokumentation kein Webexport für C#-Projekte |

**Getroffene Auswahl:** C# mit Unity. Der obige Vergleich dokumentiert den vorausgegangenen Entscheidungsrahmen; Godot ist keine parallel umzusetzende Variante. Die Auswahl betrifft die Technologie, nicht bereits eine bestätigte Freigabe auf allen exportierbaren Plattformen.

Quellen für die Plattformangaben, geprüft am 23. November 2026:

- [Unity Platform development](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific)
- [Unity Gameplay programming mit C#](https://docs.unity.com/en-us/engine/6000.0/manual/scripting)
- [Godot unterstützte Funktionen und Plattformen](https://docs.godotengine.org/en/stable/about/list_of_features.html)
- [Godot C# und Exportgrenzen](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)
- [Godot Konsolenunterstützung](https://godotengine.org/consoles/)

Bei Einrichtung von T-001 die aktuelle offizielle Dokumentation prüfen und die genaue installierte Version festhalten. Eine exportierbare Anwendung ist noch keine bestätigte Performance-, Bedienungs- oder Store-Freigabe. Mobile Bedienung, Browser-Speicherung und Konsolenzugänge separat prüfen; keine Preise oder Lizenzbedingungen aus dieser Tabelle ableiten.

## Vorgehen bei späteren Änderungen

Eine Entscheidung mit Datum, Anlass, gewählter Variante, verworfenen relevanten Alternativen und Folgen ergänzen. Betroffene Regeln, Daten, Pakete und Prüfungen verlinken. Eine technische Empfehlung wird erst durch einen tatsächlichen Entscheidungsauftrag oder eine Nutzerauswahl zur Festlegung.

Kosten, Reichweiten, Produktionsraten, Schildstärken und Zeitspannen werden durch Spieltests abgestimmt. Sie gehören in veränderbare Definitionsdaten; Änderungen dieser Zahlen allein erfordern keine neue Engineentscheidung.
