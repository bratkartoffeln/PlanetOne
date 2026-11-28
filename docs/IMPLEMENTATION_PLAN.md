# PlanetOne Implementierungsplan

Dieser Plan zerlegt das Spielkonzept in ausführbare Codex-Arbeitspakete. Unity und C# sind eingerichtet; M0 bis M4 sind umgesetzt und abgenommen. Für M1/M2 dokumentiert T-006 in DECISIONS die konkret verwendete Prüfmethode und den noch offenen vollständigen zusätzlichen Maustest. M3 ist mit Core-Tests, Windows-Build, Grafiklauf und gezielten tatsächlichen Maus-/Tastatureingaben geprüft; siehe [M3_NACHWEIS](M3_NACHWEIS.md). P08–P10 sind in Version 1.0.3 abgeschlossen; Nachweise und konkrete Grenzen stehen in [M4_NACHWEIS](M4_NACHWEIS.md).

## Roadmap ab M10

Die Paketfolge konkretisiert die Erweiterungen aus Abschnitt 16 des [Spielkonzepts](SPIELKONZEPT.md). Jede Abnahme setzt einen reproduzierbaren Spielstand und die jeweils genannten beobachtbaren Ergebnisse voraus.

| Meilenstein und Paket | Voraussetzung | Lieferumfang und Abnahme |
| --- | --- | --- |
| M10 · P23 Kolonielogistik und Referenzpartie | M0–M9 | Kuriere erreichen Bauplätze und Fabriken über passierbares Gelände. Freie Bestände, Reservierungen und Fehlmengen sind unterscheidbar. Fahrten erzeugen speicherbare Nutzungsspuren. TEST-043 und beide F05-Siegpfade prüfen die Versorgung bis zum Sprungtor. |
| M11 · P24 Atmosphärische Rohstoffgewinnung | M10, Gaswelten und Luftlogistik | Eine schwebende Förderstation gewinnt einen definierten atmosphärischen Rohstoff. Luftfrachter liefern die tatsächlich gewonnene Menge an Lager und Raumhafen. Bei voller Kapazität pausiert die Förderung. Speichern während einer Lieferung erhält Ladung und Auftrag. |
| M12 · P25 Weitere Sternsysteme und Sprungverbindungen | M10, Sprungtor und Raumtransport | Ein Mehrsystem-Szenario verbindet zwei deterministisch erzeugte Systeme. Ein Schiff mit Ladung verlässt das erste System und trifft nach definierter Reisezeit im zweiten ein. Spielstand, Besitz und Sicht bleiben während des Sprungs eindeutig. Die bestehenden Einzelsystem-Szenarien behalten ihre Siegbedingungen. |
| M13 · P26 Weitere Schiffsklassen und Bergung | M12, Raumkampf und Frachträume | Eine baubare Bergungseinheit birgt endliche, aus einem tatsächlichen Verlust entstandene Fracht. Zwei konkurrierende Bergungsaufträge können dieselbe Ladung insgesamt nur einmal entnehmen. Schiffsbau, Auftrag und Entladung sind für Spieler und KI über dieselben Befehle erreichbar. |
| M14 · P27 Kampagne und Sonderszenarien | M11–M13, Szenariomenü | Zwei verknüpfte Missionen und ein eigenständig startbares Sonderszenario verwenden definierte Startbedingungen und sichtbare Ziele. Sieg schaltet die Folgemission frei. Niederlage, Neustart und Laden mitten in der Mission erhalten den richtigen Kampagnenfortschritt. |
| M15 · P28 Balance und Plattformabnahme | M14 | Eine vollständige Kampagne und die regulären Siegpfade laufen im gebauten Windows-Player. Laufzeit, Speichern, Bedienung und Installer werden auf dem Zielgerät geprüft. Für Web, Mobilgeräte und Konsolen wird je Ziel die Machbarkeit und der erforderliche Geräte- und Eingabetest dokumentiert. Eine Portierung braucht eine eigene Zielplattformentscheidung. |

M10 und M11 sind lokal umgesetzt. [M10-Nachweis](M10_NACHWEIS.md) und [M11-Nachweis](M11_NACHWEIS.md) enthalten Abnahmeszenarien und Startbefehle. M12–M15 sind geplant. Nummerierung, Reihenfolge und Paketgrenzen sind in D-058 festgehalten.

## Vorheriger Ergänzungsauftrag 1.0.18

**GUI, Bewegung, fahrzeuggebundener Bau und Verkehr** erweitern die bestehenden Pakete P04/P05, P06/P07, P08, P11, P19–P22; kein zusätzliches Sternsystem oder neuer Meilenstein wird vorgezogen. Verbindlich sind D-038 bis D-047, T-059 bis T-074, GUI-015 bis GUI-021, SURFACE-005/006/007/010, SPACE-012, LOG-014, ARCH-017 und TEST-034 bis TEST-039.

- **Im Code umgesetzt:** drei überarbeitete Layouts und Menübereiche, separate Schriftgröße 90–140 Prozent, 40 × 32 Waben für neue reguläre Karten, Modell-/Bauvorschau-Erweiterungen und unregelmäßige Waldflächen aus dem System-Seed. Geländehöhen werden zwischengespeichert; der Oberflächenaufbau verteilt sich während der Atmosphärenblende auf mehrere Frames. Die Kamerafahrt umfasst 1,8 Sekunden Animation plus kurze variable Vorbereitung. Hinzu kommen geglättete Bewegung und gerundete Straßen, unabhängige Türme, echte sichtgefilterte Kampfereignisse, konstante Orbitlinienbreite und größere Asteroiden. Reale Baufahrzeuge verwenden höchstens zwei von vier vorhandenen Kurierkapazitäten; Gebäude-Arbeitszeiten sind verdoppelt. Die Bauphasen wurden mit Schema 12 eingeführt; aktuelles Format ist Schema 13.
- **Historisch geprüft, vor der Verkehrsüberarbeitung:** 177 unabhängige Core-Tests erneut nach der Waldänderung bestanden, einschließlich neun neuer Bauprüfungen, drei Kampfereignisprüfungen, bezahltem F05-Gesamtdurchlauf und Kooperationssieg. Windows-Build und vollständiger isolierter 1440×900-Lauf mit 849 Einzelbedingungen bestanden. Danach folgten die Sitzungs-Auswahlkorrektur mit 75 bestandenen Bedingungen und die Rahmen-/Typografieprüfung mit 302 Bedingungen bei 1280×720. Der vollständige Lauf liegt vor diesen letzten beiden Korrekturen. Der Referenzablauf verwendet normale Prioritäten, frühe Kolonisierung und bedarfsgerechten Werkstattbetrieb ohne zusätzliche Güter oder KI-Ausnahmen.
- **Zusätzlich implementiert:** direkte Rechtsklick-Bewegung/Angriff, Strg-Kontextmenü, tatsächliche Auswahlrahmen und Angriffsfadenkreuz; Körperabstände, dynamische Boden-/Raumbelegung, getrennte Ankunftsplätze und kooperatives Platzmachen bei erhaltenem Auftrag und Bestand. Nahtfreie Shader-Abtastung der vorhandenen Planetenatlanten sowie persistenter Planetenfokus und sanfte Seitenlistenfahrt. Aktuelles Speicherformat ist **13**, einschließlich Verkehrsfortsetzung.
- **Aktuell geprüft:** 212 vollständige Core-/Plattformtests einschließlich beider regulärer F05-Siege bestanden. 45 gezielte Verkehrs-/Bau-/KI-Tests bestanden; der Baufahrzeugfall prüft zusätzlich eine enge Passage ohne Ausweichroute. Interaktion bei 720p: 194 Bedingungen; Gestaltung bei 720p: 488 Bedingungen. Der aktuelle Unity-Windows-Build ist erfolgreich.
- **Noch offen:** beauftragter PR-Abschluss. Installer/ZIP, vollständiger installierter Playerlauf und Deinstallation bestanden. Der abschließende vollständige 1440×900-Playerlauf bestand 1053 Einzelbedingungen plus Gesamt-PASS. Laufreihenfolge, Bildherkunft und Leistungswerte stehen in [GUI_BEWEGUNG_BAU_NACHWEIS](GUI_BEWEGUNG_BAU_NACHWEIS.md).
- **Abgrenzung:** Referenzabgleich und Videoquellen sind im Nachweis beschrieben. Eine Pflicht zur Altspeicher-Kompatibilität und ein Wechsel auf Catan-Kantenstraßen sind nicht Teil des Auftrags. Physische Maus-/Tastaturabnahme und andere Plattformen bleiben getrennte Kriterien.

## Vorheriger Meilenstein M9 (1.0.17)

Der Fortsetzungsauftrag ergänzt **P22: Spieleinstieg und lokale Partien**. Der bisherige Plan endete bei M8; D-037 hält die konkrete Arbeitsannahme fest. Umfang, Prüfungen und Git-Stand stehen in [M9_NACHWEIS](M9_NACHWEIS.md). M8 und die Befehlsoberfläche 1.0.16 sind inzwischen über PR #15/#16 in `main` integriert; deren übrige Abnahmegrenzen bleiben erhalten.

## Vorheriger Meilenstein M8 (1.0.15)

Zusätzlicher Bedienungsauftrag **1.0.16**: Objektbefehle und Rechtsklickmenüs sind implementiert; Windows-Build, 161 Core-Tests, vollständiger 1440×900-Lauf mit 476 Einzelprüfungen und gezielter 1280×720-Lauf mit 78 Einzelprüfungen bestanden. [Recherche, Belege und Git-Grenze](BEFEHLS_GUI_NACHWEIS.md).

P19–P21 sind lokal implementiert; 161 Core-Tests einschließlich beider vollständiger regulärer F05-Verläufe bestehen. [M8_NACHWEIS](M8_NACHWEIS.md) dokumentiert echte Dateisnapshots, Windows-/Installerprüfung, getrennte Leistungsmessungen und die offenen Abnahmegrenzen. Physische Maus-/Tastaturprüfung, subjektive Bewertung und Git-Integration bleiben ausdrücklich getrennt vom automatisierten Nachweis. Der historische M7-Stand steht in [M7_NACHWEIS](M7_NACHWEIS.md).

## Projektweite Rasteränderung 1.0.6

Nutzerauftrag vom 24. November 2026: Waben statt Schachbrett. Betrifft P03/P04/P06/P07/P12 sowie ihre gemeinsamen Darstellungs- und Speicherpfade. Hexraster, sechs Nachbarn, Kameraauswahl, Bau/Transport, Sicht/Deckung/Reichweiten und siebenzellige Landeflächen sind umgesetzt. Nachweise: 82 Core-Tests, Windows-Build, fensterloser Player-Test mit 104 Einzelprüfungen; [HEXRASTER_NACHWEIS](HEXRASTER_NACHWEIS.md). M6 bleibt außerhalb dieses Auftrags.

## Verbindliche Grundlagen

- [Spielregeln](./GAME_DESIGN.md): Anforderungen der Gruppen CORE, SPACE, SURFACE, ECO, LOG, RES, COMBAT, INV und DEF.
- [KI, Diplomatie und Sieg](./AI_DIPLOMACY.md): Anforderungen der Gruppen AI, DIP und WIN.
- [Technische Architektur](./TECHNICAL_ARCHITECTURE.md): Modulgrenzen, Zuständigkeiten, Daten und Schnittstellen.
- [Entscheidungen](./DECISIONS.md): offene beziehungsweise getroffene technische Entscheidungen und ihre Begründungen.
- [Abnahmeszenarien](./ACCEPTANCE_TESTS.md): wiederholbare Szenarien TEST-001 bis TEST-039 mit Regel- und Paketzuordnung.

Die Spezifikationen bestimmen das Verhalten. Der Plan bestimmt die Reihenfolge und den Umfang einer Lieferung. Ein Verweis auf eine Anforderung bedeutet nicht, dass sie bereits umgesetzt oder getestet wurde.

Unity mit C# ist seit dem Nutzerauftrag vom 23. November 2026 verbindlich. P01 hat die ausgewählte Umgebung eingerichtet und Projektimport, Start und Windows-Build nachgewiesen. Vollständige Paketabschlüsse benötigen weiterhin ihre jeweils beschriebenen Prüfungen.

## Erstes Goal und erster vertikaler Prototyp

**Erstelle einen lauffähigen Unity-Prototyp mit C#, der ein Sonnensystem und eine bebaubare Planetenoberfläche über eine gemeinsame Simulation verbindet.**

Für diesen ersten zusammenhängenden Entwicklungsabschnitt wurden Teile von P01–P05 und der Ansichts-/Orbitanteil von P08 kombiniert. Das Goal ist erreicht: Ein startbarer Windows-Build unterstützt den folgenden Aufbauablauf, der durch Kernprüfungen und einen grafischen Integrationstest nachgewiesen wurde:

1. Ein Sonnensystem mit bewegten Planeten anzeigen und eine eigene Planetenoberfläche öffnen.
2. Gebäude zulässig platzieren; angeliefertes Baumaterial und Baufortschritt beobachten.
3. Eine einfache Erz-zu-Metall-Kette mit erreichbaren lokalen Lieferwegen betreiben; fehlende Versorgung erkennen.
4. Zwischen Oberfläche und Sonnensystem wechseln, ohne zweite Bestände oder verlorenen Fortschritt.
5. Pausieren und Befehle vormerken, fortsetzen sowie speichern und laden.
6. Den gleichen Kernzustand mit gezielten automatisierten Regelprüfungen und den sichtbaren Ablauf im Unity-Build nachweisen.

Der visuelle Stil ist minimalistisch. Platzhaltergeometrie ist ein geeignetes erstes Darstellungsmittel, sofern Bedienung und Zustände klar erkennbar sind. Interplanetare Frachter, Schiffsbau, Koloniegründung, Forschung, Gefechte, KI und Diplomatie werden für dieses erste Goal nicht vorgetäuscht und behalten ihre nachfolgenden Pakete.

Die Teilumsetzung des vertikalen Prototyps schließt P02–P05 oder P08 nicht automatisch ab. Alle noch fehlenden Paketanforderungen bleiben ausdrücklich offen.

### Nachweis des ersten Goals

- `tools/Unity.ps1 -Action Prepare` und `-Action Build`: erfolgreich, jeweils Exitcode 0.
- `Builds/Windows/PlanetOne.exe`: erzeugter und gestarteter Windows-Mono-Player.
- `tools/Test-Core.ps1`: 14 bestandene Verhaltensprüfungen.
- `tools/Test-Prototype.ps1 -Visible`: 19 bestandene Prüfbedingungen und drei Renderbilder; System, Oberfläche und wiederhergestellte Oberfläche wurden anhand der Bilder kontrolliert.
- Testkarte: zwei eigene Kolonien und ein neutraler Mond, je Oberfläche 18 × 14 Felder und vier Kuriere pro Kolonie.
- Grafischer Test programmatisch im echten Player; vollständige manuelle Mausbedienungsabnahme und andere Plattformen noch offen. Die vorläufige Testhardware steht in [DECISIONS](DECISIONS.md).

## Zweites Goal: tragfähige Koloniewirtschaft

**Erweitere den vorhandenen minimalistischen Unity-Prototyp zu einer spielbaren, erweiterbaren Kolonie, die ihre Bevölkerung und Grundproduktion aus lokalen Ressourcen versorgt. Schließe dafür die offenen Grundlagen von M1 und die Koloniewirtschaft von M2 ab, einschließlich belastbarer Transportwege, verständlicher Engpassanzeigen und zuverlässigem Speichern und Laden.**

Status: am 23. November 2026 definiert und anschließend vom Nutzer als aktives Goal gestartet. Umfang sind die offenen Anforderungen von P02–P05. Die bestehende gemeinsame Sonnensystem-/Oberflächensimulation und der minimalistische Stil bleiben erhalten. Zum Abschluss gehört der beauftragte versionsbasierte Commit-/PR-Ablauf nach `main`.

Technisch abgenommen in Produktversion 1.0.1 nach T-006: 28 Core-Tests, Windows-Build und 58 Prüfbedingungen im sichtbaren Player mit vier visuell kontrollierten Renderbildern. Der 20-Minuten-Nachweis umfasst selbst erzeugte Nahrung sowie Ausbaukosten von 116 Metall/26 Bauteilen gegenüber Startvorräten von 100/20. Der zusätzliche physische Maustest bleibt offen; der Git-Abschluss erfolgt über [PR #1](https://github.com/bratkartoffeln/PlanetOne/pull/1).

### Abschlusskriterien

1. **Bevölkerung und Versorgung:** Wohnraum, Nahrungsverbrauch, Wachstum und verfügbare Arbeitskräfte wirken tatsächlich auf die Kolonie. Nahrungsmangel stoppt zunächst Wachstum und senkt anschließend Produktivität; die in ECO-007 vorgesehene Wiederaufbaumöglichkeit bleibt erhalten.
2. **Erweiterbare Wirtschaft:** Nahrung, Erz, Metall und Bauteile werden lokal erzeugt, transportiert, gelagert und verbraucht. Nach der Startphase finanzieren selbst erzeugte Materialien weitere Grundgebäude. Energie, Arbeitskräfte und Lagerkapazität begrenzen den Betrieb nachvollziehbar.
3. **Unterbrechung und Erholung:** Straßen lassen sich entfernen und wiederherstellen. Betroffene Lieferungen warten oder verwenden einen verfügbaren Umweg; nach Reparatur läuft die Versorgung weiter. Geladene Waren bleiben beim Transporter, Reservierungen werden korrekt behandelt, und keine Ware wird dupliziert oder durch einen bloßen Wegeabriss gelöscht. Bauabbruch und Gebäudeabriss haben ausgewiesene Rückgewinnungsregeln.
4. **Bedienbare Engpässe:** Gebäude zeigen fehlende Eingänge, fehlenden Weg, Energie-/Arbeitskräftemangel und volle Lager unterscheidbar an. Der Spieler kann die Ursache mit regulären Bau-, Wege- und Betriebsbefehlen beheben. Der Aufbauablauf wird zusätzlich im sichtbaren Player auf Bedienbarkeit geprüft.
5. **Vollständige Grundlage:** Fraktionszuordnung, Berechtigungen, Befehlsrückmeldungen und die für P03 vorgesehenen Szenario-Beziehungen sind umgesetzt. Fremde Fraktionen können keine eigenen Gebäude oder Vorräte steuern. Pause, 1×/2×, Ansichtswechsel und Save/Load ergeben bei gleichem Befehlsablauf und gleicher Simulationszeit gleichwertige Zustände, einschließlich Bevölkerung, laufender Produktion, Fracht und vorgemerkter Aufträge.
6. **Reproduzierbare Abnahme:** Ein Szenario mit dokumentiertem Seed, Startvorräten und Aufbaufolge weist nach der Aufbauphase 20 Simulationsminuten laufende Grundversorgung ohne Importe oder zusätzliche Ressourceninjektionen nach. Der Nachweis umfasst den Verbrauch der anfänglichen Nahrung, deren Ersatz aus eigener Produktion, einen aus eigener Produktion bezahlten Ausbau sowie getrennte Störungs-/Erholungstests. Endliche Erzvorkommen bleiben endlich; das Goal verspricht keine unbegrenzte Autarkie. Relevante Regeltests, Windows-Build, grafischer Integrationstest und dokumentierte Bedienungsprüfung sind erfolgreich.

Fachliche Grundlage sind insbesondere SURFACE-003 bis SURFACE-008, ECO-001 bis ECO-007 und LOG-008; Nachweise bauen auf TEST-001, TEST-002 und dem Wirtschafts-/Wiederaufbauanteil von TEST-006 auf. Die vollständigen Paketkriterien von P02–P05 bleiben maßgeblich. Schiffsbau, interplanetare Routen, Forschung, Gefechte, KI und Diplomatie behalten ihre nachfolgenden Meilensteine.

## Ablauf und Abhängigkeiten

| Meilenstein | Ergebnis | Pakete | Voraussetzung |
|---|---|---|---|
| M0 | Unity 6000.3.29f1 mit C# eingerichtet; Start und Build nachgewiesen | P01 | Ausgewählte und beauftragte Unity-Einrichtung |
| M1 | Gemeinsame Simulation mit Befehlen und Speichergrundlage | P02–P03 | M0 |
| M2 | Funktionierende Koloniewirtschaft | P04–P05 | M1 |
| M3 | Bodengefechte und versorgte Verteidigungsstellungen | P06–P07 | M2 |
| M4 | Schiffsbau, Kolonisierung, Warenverkehr und Forschung im beweglichen Sonnensystem | P08–P10 | M2; Ansicht wird für das erste Goal vorgezogen |
| M5 | Durchgängiger Raum- und Bodenkrieg | P11–P13 | M3 und M4 |
| M6 | Eigenständig wirtschaftende und militärisch anpassungsfähige KI | P14–P15 | M5 |
| M7 | Mehrere KI, Diplomatie und gemeinsamer Sieg | P16–P18 | M6 |
| M8 | Geprüftes vollständiges Szenario und vereinbarte Exporte | P19–P21 | M7 |
| M9 | Verständlicher Spieleinstieg und getrennte lokale Partien | P22 | M8 und Befehlsoberfläche 1.0.16 |
| M10 | Freie Kolonielogistik und reguläre Referenzsiege | P23 | M9 |
| M11 | Atmosphärische Rohstoffgewinnung | P24 | M10 |
| M12 | Weitere Sternsysteme und Reise durch Sprungverbindungen | P25 | M10 |
| M13 | Weitere Schiffsklassen und Bergung | P26 | M12 |
| M14 | Kampagne und Sonderszenarien | P27 | M11–M13 |
| M15 | Balance und Plattformabnahme | P28 | M14 |

M3 und M4 können nach M2 parallel bearbeitet werden, sofern die Schnittstellen und Dateizuständigkeiten vorher geklärt sind. Die vorgezogene reine Raumansicht benötigt nur die gemeinsame Simulation; die übrigen Abhängigkeiten von P08 bleiben erhalten. Parallele Agenten bearbeiten getrennte Dateien beziehungsweise ausdrücklich zugewiesene Module.

Ein Auftrag nennt normalerweise ein Paket. Codex erledigt dessen notwendigen Umfang und Prüfungen selbstständig. Neue Produktentscheidungen werden nicht durch beiläufige Implementierungsannahmen ersetzt. Routineentscheidungen innerhalb dokumentierter Regeln benötigen keine erneute Bestätigung.

## M0 Unity Einrichtung und Entwicklungswerkzeuge

### P01 Unity Projekt und Entwicklungsumgebung einrichten

- **Status:** Abgeschlossen. **Abhängigkeit:** Keine weitere Auswahlfreigabe; Einrichtung war ausdrücklich beauftragt.
- **Module:** Projektaufbau, Entwicklungswerkzeuge, Plattformadapter.
- **Umfang:** Unity 6000.3.29f1 mit C# einrichten und Projektgerüst anlegen. Windows als primäre Testplattform sowie spätere Exportziele und ihre Priorität festhalten. Tatsächliche lokale Voraussetzungen prüfen. Editorvorbereitung, Windows-Build und unabhängige C#-Regeltests reproduzierbar dokumentieren.
- **Nicht enthalten:** Spielmechaniken, zusätzliche Onlinedienste oder eine Veröffentlichung.
- **Abnahme:** Bei abgeschlossener Auswahl sind Engine, Sprache, Version, Testwerkzeuge, Minimalprojekt und reproduzierbarer lokaler Start beschrieben. Nicht lokal verfügbare Exportziele sind als ungeprüft erkennbar. Eine kleine Referenzszene startet auf der primären Testplattform.
- **Prüfung:** Verifizierte Start- und Buildschritte protokollieren. Einheitenzahl, Kartengröße und Hardware für spätere Leistungsmessungen als vorläufiges Testprofil festlegen. Keine Befehle erfinden, deren Werkzeuge noch nicht eingerichtet sind.
- **Nachweis:** Unity 6000.3.29f1 importiert das Projekt; Prepare und Build enden mit Exitcode 0. Windows-Mono-Player und sichtbarer Integrationstest laufen erfolgreich. Vorläufiges Testprofil und Grenzen sind in DECISIONS festgehalten.

## M1 Gemeinsame Simulation

### P02 Zeit, Identitäten und Szenariozustand

- **Status:** Abgeschlossen in 1.0.1. Fraktionen, stabile IDs, gemeinsame Zeit, Pause sowie 1×/2×/4× sind geprüft; beide Kolonien laufen unabhängig von der sichtbaren Ansicht. **Abhängigkeit:** P01 abgeschlossen.
- **Module:** Simulation, Weltzustand, Szenariodaten.
- **Umfang:** Stabile Entitätskennungen, Fraktionen, gemeinsame Simulationszeit, kontrollierter Zufallsstart und feste Simulationsschritte. Pause und vorgesehene Spielgeschwindigkeiten wirken auf alle Schauplätze. Darstellung liest den Zustand; sie ist nicht Eigentümer der Spielzeit.
- **Nicht enthalten:** Produktionsketten, Kämpfe oder eine plattformübergreifend garantierte bitgenaue Simulation.
- **Abnahme:** Gegeben sind zwei Orte mit zeitabhängigen Vorgängen. Nach dem Wechsel der sichtbaren Ansicht schreiten beide gleich weit fort. Während Pause bleiben Weltvorgänge unverändert; Auswahl, Oberfläche und vorgemerkte Befehle bleiben bedienbar.
- **Prüfung:** Automatisierte Zeit- und Pausenprüfung einschließlich verschiedener Renderbildraten. Gleicher Startwert und gleiche Befehle ergeben innerhalb der festgelegten Testumgebung nachvollziehbare Resultate.

### P03 Befehle, Ereignisse und Speicherschnittstelle

- **Status:** Abgeschlossen in 1.0.1. Validierte Bau-, Wege-, Betriebs- und Zeitbefehle, begründete Rückmeldungen, begrenzte Ereignisprotokolle, paarweise Peace/War-Szenariobeziehungen sowie Speicherformat 2 mit V1-Migration sind umgesetzt und geprüft. **Abhängigkeit:** P02 abgeschlossen.
- **Module:** Befehlsverarbeitung, Ereignisse, Persistenz.
- **Umfang:** Einheitlicher validierter Befehlsweg für Spieler und spätere KI, begründete Ablehnungen, Ereignisausgabe und versionierter Speicherschnappschuss. Pausenbefehle bleiben geordnet und werden zum vorgesehenen Simulationsschritt wirksam. Das Szenario kann bereits paarweise Peace/War-Beziehungen für Prüfgefechte setzen; dynamische Diplomatie folgt mit P16.
- **Nicht enthalten:** Komfortfunktionen eines umfangreichen Speicherstandmenüs oder eine Netzwerkarchitektur.
- **Abnahme:** Ein unberechtigter oder ungültiger Befehl verändert den Zustand nicht. Speichern und Laden stellen Zeit, Kennungen, Zufallszustand und ausstehende Befehle wieder her. Die Fortsetzung führt weder zur doppelten Ausführung noch zum Verlust eines Auftrags.
- **Prüfung:** Automatisierte Befehlsvalidierung und Speicher-Lade-Vergleiche. Ab diesem Paket erweitert jede neue Mechanik ihren gespeicherten Zustand mit.

## M2 Koloniewirtschaft

### P04 Gelände, Bau und lokale Wege

- **Status:** Technisch abgeschlossen in 1.0.1 nach T-006: Platzierung, Materialanlieferung, zwei priorisierte Bauplätze, Straßenabbau, Umleitung, Unterbrechung und Reparatur sowie Abbruch/Rückbau. Der zusätzliche physische Maustest bleibt separat offen. **Abhängigkeit:** P03 abgeschlossen.
- **Module:** Oberfläche, Gebäude, Bauaufträge, Navigation, Eingabe.
- **Umfang:** Eine überschaubare Kolonieregion mit bebaubaren und gesperrten Bereichen; Platzierungsvorschau, Bauzeit, Materialbedarf, Lager, Straßen und lokale Transportverbindungen. Zunächst lesbare Platzhaltergrafik verwenden.
- **Nicht enthalten:** Gesamte Planetenkugeln, umfangreiche Geländeeditoren oder endgültige Grafik.
- **Abnahme:** Ein Gebäude kann nicht in gesperrtem Gelände oder über einem bestehenden Gebäude errichtet werden. Ohne angeliefertes Material wird es nicht fertig. Ein unterbrochener Weg verhindert die betroffene Lieferung und zeigt den Grund an.
- **Prüfung:** Automatisierte Bau- und Erreichbarkeitsregeln; Aufbau einer Lieferkette einschließlich absichtlicher Wegeunterbrechung über die normalen Controller-Bedienpfade im sichtbaren Player und visuelle Bildkontrolle. T-006 hält die gesonderte, zusätzliche Mausprüfung fest.

### P05 Produktion, Versorgung und Wiederaufbau

- **Status:** Technisch abgeschlossen in 1.0.1 nach T-006: lokale Produktion, Nahrung, Bevölkerung/Wohnraum/Arbeitskräfte, Energiepuffer, Betriebsprioritäten und Wiederaufbau. Der 20-Minuten-Test weist Selbstversorgung und Ausbau aus eigener Metall- und Bauteilproduktion nach. Die zusätzliche Mausprüfung bleibt offen; Balancingwerte bleiben vorläufig. **Abhängigkeit:** P04 technisch abgeschlossen.
- **Module:** Inventare, Produktion, Bevölkerung, Energie, Wirtschaftsoberfläche.
- **Umfang:** Nahrung, Erz, Metall und Bauteile als erste Kette; lokale Energie, Arbeitskräfte, Lagerkapazität und nachvollziehbare Engpassanzeigen. Definitionen sehen die sechs vereinbarten Handelsgüter vor; Kristalle und Elektronik werden mit P10 produktiv genutzt.
- **Nicht enthalten:** Geldmarkt, Treibstoffwirtschaft oder zusätzliche Warenklassen.
- **Abnahme:** Fehlende Eingänge, Energie oder Arbeitskräfte stoppen beziehungsweise begrenzen die betroffene Produktion nach den Spielregeln. Eine abgeschlossene Produktion verbraucht genau ihre Eingänge und liefert genau ihre Ausgänge. Eine Heimatkolonie kann Grundgebäude und grundlegende Logistik ohne Kristallimporte herstellen.
- **Prüfung:** Automatisierte Mengenerhaltung, Kapazitätsgrenzen und Wiederaufbaukette. Der sichtbare Engpassdurchlauf über normale Controller-Bedienpfade zeigt nicht nur „Mangel“, sondern dessen lokale Ursache; ergänzende Bildkontrolle nach T-006.

## M3 Bodengefechte und Tower Defense

### P06 Bewegliche Bodentruppen und Sicht

- **Status:** Abgeschlossen in 1.0.2. Reguläre Rekrutierung, sechs Bodenrollen, Bewegung, Sicht, Deckung, Angriff, Halten und Rückzug; Core-, Grafik- und echte Bediennachweise in [M3_NACHWEIS](M3_NACHWEIS.md). **Abhängigkeit:** P05 erfüllt.
- **Module:** Bodennavigation, Auswahl, Befehle, Sicht, Kampf, Einheitenproduktion.
- **Umfang:** Infanterie, Panzerabwehr, Kampfpanzer, Artillerie sowie Pionier-/Versorgungsfahrzeug in einfachen Ausprägungen. Bewegung, Angriff, Halten und Rückzug; Deckung, Reichweite und Sicht. Einheiten entstehen durch reguläre Produktionsaufträge.
- **Nicht enthalten:** Komplexe Spezialfähigkeiten oder separate Luftkampfmechaniken.
- **Abnahme:** Verdeckte Gegner lassen sich nicht als aktuell sichtbare Ziele auswählen. Artillerie benötigt passende Zielinformation. Eine mobile Reserve kann eine Stellung unterstützen und anschließend zurückgezogen werden. Verlust und Neubau wirken auf den tatsächlichen Einheitenbestand.
- **Prüfung:** Automatisierte Sicht-, Reichweiten- und Schadensregeln; manuelles Gefecht auf zwei unterschiedlichen Angriffswegen.

### P07 Türme, Mauern und militärische Versorgung

- **Status:** Abgeschlossen in 1.0.2. Vier Stellungen, zerstörbare Mauern/Tore, Szenario-Torrechte, Depots, endliche Vorräte, Energiepuffer, Pionierversorgung und Reparatur; Flugabwehr gegen Übergangsziele automatisiert geprüft. Reguläre Landung bleibt P12. Nachweise in [M3_NACHWEIS](M3_NACHWEIS.md). **Abhängigkeit:** P06 abgeschlossen.
- **Module:** Verteidigungsanlagen, Navigation, lokale Logistik, Munition, Reparatur.
- **Umfang:** Vier Grundstellungen, zerstörbare Mauern, berechtigte Torpassage, Versorgungsdepots, lokale Munitionsvorräte, Reparatur und mobile Flugabwehr. Flugabwehr besitzt bereits ihre Zielregeln; reguläre Landungstransporter folgen mit P12.
- **Nicht enthalten:** Kostenlos erzeugte Angriffswellen, globale Turmlimits oder fortgeschrittene Schilde.
- **Abnahme:** Mauern verändern Wege, ohne Angriffsziele dauerhaft unerreichbar zu machen. Türme können bei fehlenden Betriebsmitteln nicht unbegrenzt weiterfeuern. Ein unterbrochener Versorgungsweg wirkt erst über vorhandene Vorräte. Eine Stellung lässt sich mit passender Aufklärung und Artillerie überwinden.
- **Prüfung:** Automatisierte Tore, Vorratsverbrauch und Gebäudeschaden. Manuelle Verteidigung mit zwei Linien und einer durchbrochenen Versorgung. Vorbereitete Testarmeen sind als isolierte Testdaten gekennzeichnet.

## M4 Raumansicht, Handel und Forschung

### P08 Sonnensystem und Ansichtswechsel

- **Status:** Abgeschlossen in 1.0.3. Bezahlter Schiffsbau, freie Flüge, bewegte Ziele, Eskorte, Bewachen, Sensorwissen und eindeutige Koloniegründung. Core-, Grafik- und native Bediennachweise in [M4_NACHWEIS](M4_NACHWEIS.md). **Abhängigkeit:** P02/P05 erfüllt.
- **Module:** Orbitbewegung, Raumnavigation, Sensoren, Raumdarstellung, Kamera.
- **Umfang:** Stern, Planeten, Mond, vorhersehbare Bahnen, frei bewegliche Schiffe und Wechsel zu bestehenden Oberflächen. Flüge zu bewegten Zielen und dauerhafte Eskorte beziehungsweise Planetenverteidigung. Ein Raumhafen baut aus lokalen Waren einfache Frachter und Kolonisationsschiffe; ein Gründungsbefehl prüft Standort und Besitz und überführt genau ein Schiff samt tatsächlicher Ausrüstung in eine neue Kolonie. Die zugehörigen Basistechnologien sind bis P10 in der Prüfkarte ausdrücklich vorab freigeschaltet.
- **Nicht enthalten:** Mehrere Sonnensysteme, freie dreidimensionale Kampfbewegung oder realistische Himmelsmechanik.
- **Abnahme:** Ein Schiff erreicht einen bewegten Zielplaneten ohne wiederholte manuelle Zielkorrektur. Eine aus eigener Produktion finanzierte zweite Kolonie entsteht mit genau den deklarierten Anfangsbeständen; der Gründungsträger ist anschließend verbraucht. Ein wiederholter Gründungsbefehl erzeugt keine Kopie. Ein Ansichtswechsel erzeugt keine zweite Kolonie und setzt keine Produktion zurück. Sichtinformation bleibt fraktionsgebunden.
- **Prüfung:** Automatisierte Zielverfolgung und Zustandsidentität; manueller Flug während mehrerer Ansichtswechsel.

### P09 Frachter und automatische Bedarfsversorgung

- **Status:** Abgeschlossen in 1.0.3. Reservierte physische Fracht, Hafenleistung, Exportreserve, Nachfüllschwellen, feste Route und automatisches Netz; Abbruch, volle Lager und Verlust geprüft. Details in [M4_NACHWEIS](M4_NACHWEIS.md). **Abhängigkeit:** P08 abgeschlossen.
- **Module:** Transportaufträge, Inventarreservierung, Raumhafen, Routen, Warnungen.
- **Umfang:** Reale Frachtkapazität, Be- und Entladung, Mindestbestände, Exportschutz, bereits unterwegs befindliche Ware und Lieferprioritäten. Feste Route und automatisch versorgtes Kolonienetz.
- **Nicht enthalten:** Handel mit fremden Fraktionen; dieser folgt mit P16.
- **Abnahme:** Gegeben ist ein Defizit auf Planet B und exportierbare Ware auf Planet A. Ein Frachter reserviert Ware und Zielplatz, holt die Ware ab und liefert sie physisch. Eine parallele Bedarfsauswertung bestellt dieselbe Menge nicht doppelt. Auftragabbruch, Zielverlust und Schiffsverlust lösen Reservierungen korrekt auf.
- **Prüfung:** Automatisierte konkurrierende Aufträge, Teilmengen, Verlust und volle Lager. Manueller Vergleich zwischen Produktionsmangel und unzureichender Transportleistung.

### P10 Forschung und vollständige Warenketten

- **Status:** Abgeschlossen in 1.0.3. Endlicher Kristallabbau, Elektronik und fünf verzweigte Technologien mit lokalen Proben, reichsweiten Freigaben und Wiederaufnahme nach Laborverlust. Durchgängiger Wirtschaftsnachweis in [M4_NACHWEIS](M4_NACHWEIS.md). **Abhängigkeit:** P09 abgeschlossen.
- **Module:** Forschung, Freischaltungen, Entdeckungen, Produktionsdefinitionen.
- **Umfang:** Kristallabbau, Elektronik und eine kurze verzweigte Forschungsfolge mit Voraussetzungen, Materialverbrauch und reichsweiten Bauplänen. Technologiedaten werden auf ungültige Verweise und Zyklen geprüft.
- **Nicht enthalten:** Der endgültige Umfang des Forschungsbaums oder seltene narrative Entdeckungen.
- **Abnahme:** Eine Freischaltung erfordert ihre dokumentierten Voraussetzungen und tatsächlich verfügbare Versuchsmaterialien. Eine freigeschaltete Einheit benötigt beim Bau weiterhin lokale Waren und Infrastruktur. Kolonisierung und grundlegende Gegenmittel gegen Festungen bleiben ohne Kristalltechnologie erreichbar.
- **Prüfung:** Automatisierte Voraussetzungen und Freischaltungen; kompletter Ablauf von Grundwirtschaft über Außenposten bis zur ersten Elektronikproduktion.

## M5 Durchgängige Invasion

### P11 Raumgefechte und lokale Orbitalbereiche

- **Status:** In 1.0.5 implementiert und lokal automatisiert geprüft; vollständige manuelle Abnahme und Git-Integration offen. Siehe [M5_NACHWEIS](M5_NACHWEIS.md). **Abhängigkeit:** P07 und P10 erfüllt.
- **Module:** Raumkampf, Eskorten, Rückzug, Reparatur, Orbitalbereiche.
- **Umfang:** Orbitalwerft errichten und über reale Hafentransfers versorgen; Aufklärer, Korvette, Eskortfregatte und Torpedoschiff aus Material und Produktionszeit fertigen. Frachter abfangen und schützen, Schäden reparieren, lokale Gefahrenzonen um Planeten darstellen.
- **Nicht enthalten:** Vollständige Flottenvielfalt oder planetare Eroberung.
- **Abnahme:** Eine Eskorte verfolgt ihren Transport und reagiert auf einen Angriff. Ein zerstörter Frachter verliert seine tatsächliche Ladung. Ein verstecktes Schiff weit außerhalb des Anflugbereichs blockiert keine gesamte Planetenoperation.
- **Prüfung:** Automatisierte Ziel- und Schadensregeln; manueller Konvoikampf mit Rückzug und anschließender Reparatur.

### P12 Verladung, Landung und Brückenkopf

- **Status:** In 1.0.5 implementiert und lokal automatisiert geprüft; vollständige manuelle Abnahme und Git-Integration offen. Siehe [M5_NACHWEIS](M5_NACHWEIS.md). **Abhängigkeit:** P11 lokal umgesetzt.
- **Module:** Truppentransport, Invasionszustände, Landezonen, Warnungen, Felddepots.
- **Umfang:** Vorhandene Bodentruppen verladen, zu einer geeigneten Landefläche transportieren, sichtbar absteigen und entladen. Begrenzte Startvorräte, angreifbare Verstärkungstransporte und Evakuierung.
- **Nicht enthalten:** Automatische Planeteneroberung oder kostenlos nachproduzierte Wellen.
- **Abnahme:** Jede gelandete Einheit lässt sich ihrem vorherigen Bestand und Transport zuordnen. Einheiten sind während des Transports nicht zugleich am Ausgangsort aktiv. Transportverluste werden genau einmal verbucht. Flugabwehr kann geeignete Anflugziele bekämpfen. Eine abgeschnittene Bodenarmee bleibt bestehen und nutzt ihre örtlichen Vorräte.
- **Prüfung:** Automatisierte Bestandsübertragung und Landeberechtigung; manuelle Landung, Nachlieferung und Evakuierung unter Bedrohung.

### P13 Eroberung und begrenzte Orbitalunterstützung

- **Status:** In 1.0.5 implementiert und lokal automatisiert geprüft; vollständige manuelle Abnahme und Git-Integration offen. Siehe [M5_NACHWEIS](M5_NACHWEIS.md). **Abhängigkeit:** P12 lokal umgesetzt.
- **Module:** Gebäudebesetzung, Besitzwechsel, Invasion, Orbitalfeuer, Schilde.
- **Umfang:** Verwaltungszentrale und Versorgungspunkt tatsächlich sichern, Gebäude übernehmen und verbliebene Verteidiger weiterführen. Orbitalunterstützung mit Zielaufklärung, Vorbereitung und Gegenwehr; Schutzschild und planetare Abwehrbatterie in einfacher Form.
- **Nicht enthalten:** Diplomatische Besitzabsprachen oder gemeinsamer Szenariosieg.
- **Abnahme:** Raumüberlegenheit allein überträgt keinen Planeten. Infanterie und gesicherte Versorgung können ihn übernehmen; überlebende Verteidiger verschwinden nicht. Orbitalfeuer beschädigt Ziele, übernimmt sie aber nicht. Ein Gegenangriff kann relevante Anlagen zurückerobern.
- **Prüfung:** Automatisierte Besitz- und Versorgungskriterien; vollständige manuelle Invasion mit mindestens einer Verteidigungslinie und alternativem Landeort.

**Erstes zusammenhängend spielbares Teilergebnis nach M5:** Ein System mit zwei Startwelten und einem umkämpften Mond. Wirtschaft, Forschung, Frachter, Raumkampf, Landung, Bodenkampf und Eroberung funktionieren gemeinsam. Ein Skriptgegner darf hier den Prüfablauf steuern; er ist ausdrücklich noch kein Nachweis intelligenter Fraktions-KI und kein Ersatz für M6/M7.

## M6 Eigenständige Computerfraktionen

### P14 Wirtschaft, Forschung und begrenztes Wissen

- **Status:** In 1.0.7 lokal umgesetzt und automatisiert geprüft: eigene Wirtschaft/Forschung/Kolonisierung, Engpassmaßnahmen, gefilterte Wahrnehmung und gespeicherte Entscheidungsprotokolle. [M6_NACHWEIS](M6_NACHWEIS.md) beschreibt Zeitfenster und Grenzen; Git-Integration und erneute manuelle Abnahme offen. **Abhängigkeit:** P13.
- **Module:** KI-Wahrnehmung, Wirtschaftsplanung, Forschung, gemeinsame Befehlsverarbeitung.
- **Umfang:** Eine KI baut ihre Grundversorgung auf, erkennt Engpässe, reserviert Ressourcen, erschließt Standorte und erforscht passende Technik. Entscheidungen erfolgen aus eigenem Wissen und öffentlichen Informationen.
- **Nicht enthalten:** Rohstoffboni, allwissende Gegnerdaten oder Diplomatie.
- **Abnahme:** Nach Unterbrechung einer Lieferroute erkennt die KI deren Ursache und versucht eine passende Gegenmaßnahme. Ein ungesehener gegnerischer Neubau verändert ihre Planung erst nach legitimer Information. Alle Aufträge durchlaufen dieselbe Validierung wie Spielerbefehle.
- **Prüfung:** Wiederholbare Wirtschaftsszenarien sowie Informationsabschirmung; Entscheidungsprotokoll mit Ziel, beobachteten Gründen und gewähltem Auftrag.

### P15 Militärische Planung und Anpassung

- **Status:** In 1.0.7 lokal umgesetzt und automatisiert geprüft: reale Beschaffung, Eskorte, exklusive Operationszuweisungen, veränderte Landeplanung, vollständige vorbereitete Invasion und physischer Rückzug. [M6_NACHWEIS](M6_NACHWEIS.md); keine behauptete feste Siegzeit aus Normalstart. Git-Integration und erneute manuelle Abnahme offen. **Abhängigkeit:** P14.
- **Module:** KI-Strategie, Operationsplanung, taktische Steuerung, Verteidigung.
- **Umfang:** Aufklären, Ressourcen und Kräfte sammeln, Konvois eskortieren, Invasionen vorbereiten, Reserven einsetzen und abbrechen. Auf starke bekannte Turmstellungen mit passender Ausrüstung, anderer Landefläche oder Zielwechsel reagieren.
- **Nicht enthalten:** Maschinelles Lernen oder perfekte vorausschauende Entscheidungen.
- **Abnahme:** Bei unzureichender Transportkapazität verschiebt die KI die Landung oder beschafft Transporter. Nach Aufklärung einer für ihre Armee ungeeigneten Stellung verändert sie ihren Plan vor einem erneuten Angriff. Ein Rückzug erhält Überlebende und löst Operationsreservierungen auf.
- **Prüfung:** Vergleichbare Szenarien mit verschiedenen Verteidigungen und mehreren Startwerten. Protokolle müssen Anpassung, tatsächliche Ressourcen und nachvollziehbare Abbruchgründe zeigen.

## M7 Diplomatie und gemeinsamer Sieg

### P16 Verträge und Rechte

- **Status:** In 1.0.8 lokal implementiert und automatisiert geprüft: Verträge, Rechte, physischer Handel und KI-Verhandlungen. [M7_NACHWEIS](M7_NACHWEIS.md); manuelle Bedienungsabnahme und Git-Integration offen. **Abhängigkeit:** P15 lokal erfüllt.
- **Module:** Diplomatie, Handelsabwicklung, Berechtigungen, Beziehungen, Dialogoberfläche.
- **Umfang:** Paarweise Peace/War-Beziehungen mit ausdrücklicher Kriegserklärung sowie Waffenstillstand, Nichtangriff, physischer Handel, Transit, Nutzung und Verteidigungspakt mit Zuständen, Laufzeiten und Folgen. Angebote können Spieler oder KI beginnen. Mehrere KI handeln auch miteinander.
- **Nicht enthalten:** Gemeinsame Siegauswertung oder zusammengelegte Fraktionslager.
- **Abnahme:** Ein Handelsvertrag erlaubt keine unvereinbarte Truppenlandung. Eine gültige feindliche Invasion benötigt dagegen keine Zustimmung des Verteidigers. Ware wird geliefert und nicht bei Vertragsabschluss teleportiert. Gewöhnliche automatische Waffenwahl erklärt keinen Krieg. Ein eigener Angriffskrieg aktiviert keine unpassende Verteidigungspflicht. Ablehnungen und Vertragsverletzungen besitzen erkennbare Gründe.
- **Prüfung:** Automatisierte Rechte- und Zustandsmatrix einschließlich Ablauf, Kündigung und Vertragsbruch; manuelle Verhandlung mit zwei KI-Fraktionen.

### P17 Koalitionen und gemeinsame Operationen

- **Status:** In 1.0.8 lokal implementiert und automatisiert geprüft: einstimmiger Beitritt, aktuelle gemeinsame Sicht, reale exklusive Zusagen, gemeinsame Invasion/Eigentum und Räumung. [M7_NACHWEIS](M7_NACHWEIS.md); manuelle Bedienungsabnahme und Git-Integration offen. **Abhängigkeit:** P16 lokal erfüllt.
- **Module:** Koalitionen, Sichtfreigabe, Operationszusagen, Stationsrechte, Besitzabsprachen.
- **Umfang:** Koalitionsmitgliedschaft, vereinbarte gemeinsame Sicht, konkrete Hilfszusagen, reservierte Kräfte, vorgesehener Eigentümer einer Eroberung und geordnete Räumung bei Vertragsende.
- **Nicht enthalten:** Automatische Vollkontrolle über verbündete Einheiten oder kostenlose gemeinsame Produktion.
- **Abnahme:** Eine KI sagt einen verfügbaren Beitrag zu, bringt ihn an den vereinbarten Ort oder meldet eine begründete Änderung. Dieselbe Einheit wird nicht gleichzeitig für zwei unvereinbare Einsätze reserviert. Eine gemeinsame Eroberung weist den vereinbarten Eigentümer zu. Stationierte Partner nutzen eine Vertragsbeendigung nicht zum sofortigen Angriff aus dem Inneren einer Basis.
- **Prüfung:** Automatisierte Mitgliedschaft, Reservierungen und Räumungsrechte; manuelle gemeinsame Verteidigung und gemeinsame Invasion.

### P18 Szenarioziel, Kapitulation und gemeinsamer Sieg

- **Status:** In 1.0.8 lokal implementiert und automatisiert geprüft: bezahlter Torbau/Komponentenlieferung, öffentliche Sicherungsphase, gemeinsamer Sieg sowie einmalige Kapitulation/Eliminierung. [M7_NACHWEIS](M7_NACHWEIS.md); manuelle Bedienungsabnahme und Git-Integration offen. **Abhängigkeit:** P17 lokal erfüllt.
- **Module:** Szenarioziele, Sprungtorprojekt, Fraktionsstatus, Siegoberfläche.
- **Umfang:** Öffentliches Aufbauziel mit realen Beiträgen und abschließender Aktivierung. Koalitionssieg, Kapitulation, echte Eliminierung einschließlich einmaliger Restbesitz-/Vertragsabwicklung und Fortsetzung nach Hauptstadtverlust gemäß WIN-Regeln.
- **Nicht enthalten:** Kampagne, Mehrspieler oder zusätzliche Siegvarianten.
- **Abnahme:** Der Verlust einer Hauptstadt eliminiert keine Fraktion mit verbleibender Wiederaufbaumöglichkeit. Versteckte Restschiffe verhindern kein erfülltes Szenarioziel. Mitgliedschaftsänderungen während der Schlussaktivierung brechen diese Phase ab und verlangen einen Neustart. Alle beteiligten Koalitionsmitglieder gewinnen gemeinsam; auch eine vollständig friedliche Kooperation kann das Bauziel erfüllen.
- **Prüfung:** Automatisierte Sieg- und Niederlagematrix einschließlich Beitritt, Austritt und Eliminierung während der Aktivierung; manueller Abschluss des vollständigen Szenarios.

## M8 Gesamtprüfung und Export

### Zusätzlicher Auftrag: GUI und Grafik, 1.0.9

- **Status:** Implementiert; lokale Prüfungen und Git-Integrationsstand siehe [GUI_GRAFIK_NACHWEIS](GUI_GRAFIK_NACHWEIS.md). Kein Abschluss von P19–P21.
- **Grundlage:** Nutzerauftrag D-028 nach Layoutauswahl; M7 als Entwicklungsbasis. Regeln GUI-001 bis GUI-004, CORE-007/008 und die bestehenden Sichtgrenzen.
- **Umfang:** Drei gespeicherte Layouts, skalierbare Oberfläche, Spielmenü/Einstellungen, bebilderter Baukatalog, Texturen und Materialfamilien, überarbeitete Gebäude-/Einheitenmodelle, Planeten und Weltraum.
- **Abnahme:** Sofortiger Layoutwechsel ohne Simulationsänderung, korrekte Wabenauswahl und getrennte GUI-/Welteingabe; gemeinsame Funktionen in allen Ansichten. Reale GUI-Bilder auf isoliertem Desktop, keine Störung des Arbeitsdesktops. Grenzen der manuellen/Plattformprüfung separat ausweisen.

### Zusätzlicher Auftrag: räumliche Grafik, 1.0.10

Die vorhergehende Grafiklieferung wird durch **1.0.10, räumliche Modelle und natürliches Relief**, ergänzt: D-029, GUI-005/006, T-036 bis T-038. Betroffene Module sind Runtime-Darstellung, Modelle/Materialien, Kamera, Auswahl und Bewegungsdarstellung. Umfang und lokale Nachweise stehen in [3D_GRAFIK_NACHWEIS](3D_GRAFIK_NACHWEIS.md). Der Auftrag schließt P19–P21 nicht vorzeitig ab; Bildtreue zur Konzepttafel und technische Funktionsprüfung bleiben getrennte Abnahmekriterien.

### GUI und Weltraumansicht nach Konzepttafel – 1.0.11

- **Status:** Lokal umgesetzt und geprüft; Git-Integration bleibt von den Pflichtchecks abhängig. [Nachweis](GUI_WELTRAUM_NACHWEIS.md).
- **Grundlage:** D-030, GUI-001 bis GUI-007, TEST-026, CORE-007/008; aufbauend auf 1.0.10.
- **Umfang:** Drei konkrete HUD-Aufteilungen, kompakte Auswahlkarten, eindeutiger Wechsel zwischen System und Oberfläche, vollständige Verwaltungszugänge, Symbole und Tooltips; neue Planeten-/Nebeltexturen, Atmosphären, Korona und lesbare Objektmarkierungen.
- **Abnahme:** Keine Simulationsänderung durch Navigation, kein Durchklicken von Karten/Menüs, echte Flugbefehle über die gemeinsame Befehlsschnittstelle; Bildkontrolle des tatsächlichen Players auf isoliertem Desktop. Keine vorgezogene M8-, Plattform- oder pixelgleiche Grafikabnahme.

### Freie Systemkamera und Keplerbahnen – 1.0.12

- **Status:** Umsetzung auf 1.0.11; lokale Ergebnisse und Git-Integrationsgrenze in [KAMERA_ORBIT_NACHWEIS](KAMERA_ORBIT_NACHWEIS.md). Kein Abschluss von M8.
- **Grundlage:** D-031, GUI-008, SPACE-002/003 und TEST-027.
- **Umfang:** Freie Rechtszieh-Kamera, perspektivische Sternentiefen und ortsfester Nebel, langsame Eigenrotation, autoritative elliptische Keplerbahnen mit längeren Perioden, positionsstetige V8-Migration; Mauszeiger-Popup entfernen und Namensschilder an Textbreite anpassen.
- **Abnahme:** Automatisierte Bahn-/Save-Prüfungen und Trennung von Kamerageste/Befehl; tatsächliche Hintergrundprojektion und Zustandserhaltung bei Kamerawechsel, zwei GUI-Auflösungen auf isoliertem Desktop, fensterloser GPU-Test.

### Planetentypen, Luftfracht und Oberflächen – 1.0.13

- **Status:** Umsetzung und lokale Prüfung auf Basis 1.0.12; Ergebnis und Git-Integrationsgrenze im [PLANETENTYPEN_NACHWEIS](PLANETENTYPEN_NACHWEIS.md). M8 bleibt offen.
- **Grundlage:** D-032, T-045 bis T-047, SPACE-006/009/010, ECO-014, GUI-009 und TEST-028.
- **Module:** Core-Weltmerkmale, Kartengenerierung, Bau-/Bewegungsvalidierung, physische Logistik, KI-Beobachtung/-Wege, Savegame-Schema 10; Runtime-Biome, Modelle, Materialien, Kamera und Prüfkarte.
- **Abnahme:** Plausible Planetenzuordnung, Wasser als echte Bau-/Wegegrenze, Gasgründung und Luftfracht ohne Bodeninfrastruktur, unveränderte Warenintegrität und Altdaten, tatsächliche texturierte Spielbilder sowie Rechte-Maustaste-Gesten auf isoliertem Desktop.

### Größenverhältnisse, Korona und Asteroidenabbau – 1.0.14

- **Status:** Umsetzung und lokale Prüfung auf Basis 1.0.13; Nachweise und tatsächliche Git-Integrationsgrenze im [ASTEROIDEN_NACHWEIS](ASTEROIDEN_NACHWEIS.md). M8 bleibt offen.
- **Grundlage:** D-033/034, T-048 bis T-051, GUI-010/011, SPACE-011, ECO-015 und TEST-029/030.
- **Module:** Core-Systemgenerator, gemeinsame Befehle, endliche Abbauvorkommen, Raumfracht-/Hafenreservierung, Verlust-/Besitzwechselpfade, Schema 11; Runtime-Systemmaßstab, Projektion, Shader und Abbauverwaltung.
- **Abnahme:** Seed-stabile optionale Gürtel in freien Bahnregionen; Größenobergrenzen; seitlich symmetrische Korona im echten GPU-Render; physische Förderung und Lieferung; konkurrierende Ansprüche, Sicht-/Eigentumsgrenzen, Abbruch, Verlust, Save/Load und exakte Altdatenmigration. Sichtkontrolle der drei Layouts auf isoliertem Desktop.

### P19 Speicherstände und Integrationsgrenzen absichern

- **Status:** Implementiert und lokal automatisiert geprüft in 1.0.15; echte Dateisnapshots und Player-Wiederherstellung, siehe [M8_NACHWEIS](M8_NACHWEIS.md). Physischer manueller Fortsetzungsvergleich und Git-Integration bleiben offen. **Abhängigkeit:** P18.
- **Module:** Persistenz und alle zustandsführenden Systeme.
- **Umfang:** Vollständige Speicher-Lade-Prüfung an kritischen Übergängen; saubere Fehler bei ungültigen oder inkompatiblen Daten. Bestehende Tests um tatsächlich gefundene Integrationsfehler ergänzen.
- **Nicht enthalten:** Cloudspeicher oder unbegrenzte Kompatibilität mit allen zukünftigen Versionen.
- **Abnahme:** Laden während Frachttransport, Truppenabstieg, Gebäudebesetzung, Vertragskündigung und Toraktivierung erhält Bestände, Reservierungen, Rechte und Fortschritt. Es entstehen keine Kopien von Waren oder Einheiten und keine ungewollten Friedens- oder Besitzwechsel.
- **Prüfung:** Automatisierte gespeicherte Übergangsszenarien und manueller Fortsetzungsvergleich. Sichtinformationen und KI-Planreservierungen werden mitgeprüft.

### P20 Plattformen, Bedienung und Leistung prüfen

Ergänzung **1.0.16**: recherchierte Objektbefehle, Rechtsklickmenüs und aufgeteilte Flottenverwaltung nach GUI-012 / TEST-032. Umsetzung, Eingabeprüfung und Abnahmegrenzen im [Befehls-GUI-Nachweis](BEFEHLS_GUI_NACHWEIS.md).

- **Status:** Windows-Build, 1440×900/1280×720-GUI, getrennte Leistungsbudgets sowie ZIP-/Installervergleich, Start aus Testinstallation und Deinstallation bestanden in 1.0.15; Laufdaten in [M8_NACHWEIS](M8_NACHWEIS.md). Physische manuelle Eingabe und andere Plattformen ungeprüft; kein uneingeschränkter Plattformabschluss. **Abhängigkeit:** P19.
- **Module:** Plattformadapter, Eingabe, Darstellung, Benutzeroberfläche, Export.
- **Umfang:** Vereinbarte Exportziele aus P01 bauen und auf tatsächlich verfügbaren Zielumgebungen prüfen. Warnungen, Ansichtswechsel, skalierbare Oberflächen und Eingabeprofile prüfen. Simulation und Darstellung getrennt auf dem vereinbarten Lastprofil messen.
- **Nicht enthalten:** Unvereinbarte Storeveröffentlichung oder behauptete Unterstützung ungeprüfter Hardware.
- **Abnahme:** Nachgewiesene Exporte starten und führen das Referenzszenario aus. Laufzeit- und Speicherziele aus P01 sind mit Hardware, Szenariogröße und Messmethode belegt. Nicht verfügbare Zielumgebungen bleiben ausdrücklich ungeprüft. Eingabebefehle bleiben funktional gleich, auch wenn ihre Darstellung plattformspezifisch ist.
- **Prüfung:** Reproduzierbare Buildnachweise, gezielte Leistungsmessungen und manuelle Bedienungsprüfung; Optimierungen werden nur an gemessenen Engpässen vorgenommen.

### P21 Vollständiges Referenzszenario abnehmen

- **Status:** F05 implementiert; regulärer Verlauf einschließlich KI-Invasion, Konvoiverlust, Wiederaufbau und Torerfolg sowie zweiter Verlauf mit erfülltem Handel und Koalitionssieg automatisiert bestanden. Subjektive Bewertung und Git-Abschluss bleiben offen; [M8_NACHWEIS](M8_NACHWEIS.md). **Abhängigkeit:** P20.
- **Module:** Gesamtes Spiel, Szenariodaten, Ergebnisanzeige.
- **Umfang:** Ein Stern, drei Startwelten, zwei umkämpfte Monde, ein Spieler und zwei wirtschaftlich eigenständige KI. Alle Startwelten tragen die Grundversorgung; Kristallvorkommen sind erreichbar und ermöglichen konkurrierende beziehungsweise kooperative Entwicklung. Kein kostenloser Nachschub für reguläre Fraktionen.
- **Nicht enthalten:** Zusätzliche Systeme, Kampagne, fertige künstlerische Ausstattung oder Mehrspieler.
- **Abnahme:** Ein vollständiger Durchlauf zeigt Aufbau, Forschung, automatische Lieferung, Raumkampf, echte Invasion, adaptive KI, mindestens einen Vertrag und ein abgeschlossenes Szenarioziel. Ein zweiter gezielter Durchlauf weist den gemeinsamen Koalitionssieg nach. Wirtschaftlicher Wiederaufbau nach einem Konvoiverlust bleibt möglich.
- **Prüfung:** Protokollierte Spieltests mit Startwert, wesentlichen Entscheidungen, Ergebnis und gefundenen Fehlern. Automatisierte Gesamtzustandsprüfungen sichern Mengen, Eigentümer, Rechte und Zielerfüllung; subjektive Lesbarkeit und Spielspaß werden im manuellen Test bewertet.

## M9 Spieleinstieg und lokale Partien

### P22 Startmenü, Speicherplätze und Starthilfe

- **Status:** Implementiert; lokale Prüfungen und Git-Integration siehe [M9_NACHWEIS](M9_NACHWEIS.md). **Abhängigkeit:** P19–P21 und GUI-012.
- **Module:** Runtime-Sitzungsführung und Menüs, Plattform-Dateiadapter, Integrationsprüfungen. Regeln GUI-013/014, ARCH-014/015 und TEST-033.
- **Umfang:** Startmenü, drei vorhandene reguläre Szenarien mit frei wählbarem Startwert, getrennte Prüfkarten, acht Speicherplätze mit Sicherungen und alten Einzeldateien, Bestätigungen bei Zustandsersatz, vierseitige Starthilfe mit Navigation. Menüs halten die laufende Einzelspielersimulation an.
- **Nicht enthalten:** Neue Spielregeln, zusätzliche Systeme, Kampagne, automatische Ressourcenhilfen, Cloudspeicher oder automatische Veröffentlichung.
- **Abnahme:** Gültige Starts entsprechen exakt der Szenariofabrik. Getrennte Speicherziele, Backup-Wiederherstellung, unterstützte Einzeldateien, voller Katalog und Abbruch erhalten ihre ausgewiesenen Zustände. Nicht unterstützte Formate werden nach D-041 verständlich abgelehnt und nicht verändert; dauerhafte Altformatkompatibilität ist während der Entwicklung keine Pflicht. Sichtbare Fehlermeldungen, Layouts, Tastaturkurzbefehle und der bisherige Spielablauf sind geprüft. Starthilfe gibt keine versteckten Spielaufträge.
- **Prüfung:** Echte Dateisnapshots, Controller-/Eingabeprüfungen und Renderkontrolle in zwei Auflösungen auf dem isolierten Windows-Desktop. Physische Eingabe bleibt entsprechend dem beauftragten Testverfahren gesondert ausgewiesen.

## Definition of Done für ein Arbeitspaket

1. Das vereinbarte Verhalten funktioniert innerhalb seines Umfangs; bestehende Spielregeln werden nicht stillschweigend vereinfacht.
2. Relevante Anforderungen sind in der Änderung beziehungsweise ihrem Nachweis benannt. Neue Entscheidungen stehen in DECISIONS.
3. Automatisierte Prüfungen decken bedeutsame Zustandsübergänge und Fehlerfälle ab. Reine Wiederholungen der Implementierung gelten nicht als Verhaltensnachweis.
4. Sichtbare Funktionen wurden in der Anwendung manuell geprüft, soweit eine ausführbare Umgebung verfügbar ist.
5. Neue zustandsführende Daten sind speicherbar; neue Inhalte verwenden stabile Kennungen und nachvollziehbare Definitionen.
6. Start-, Build- und Prüfschritte sind tatsächlich ausgeführt oder mit einem konkreten Hindernis als ungeprüft markiert.
7. Verbleibende Fehler und Einschränkungen sind dokumentiert. Offene Abnahmekriterien verhindern den Status „abgeschlossen“.
8. Fremde Änderungen bleiben erhalten. Dokumentation, Aufgabenstatus und tatsächlicher Repository-Zustand stimmen überein.

## Vorlage für einen Codex-Auftrag

```text
Setze in PlanetOne das Arbeitspaket PXX aus docs/IMPLEMENTATION_PLAN.md um.

Lies zuerst AGENTS.md, den aktuellen Repository-Stand sowie die zum Paket
gehörenden Spielregeln, KI-Regeln, Architekturentscheidungen und Abhängigkeiten.
Prüfe, welche Voraussetzungen tatsächlich implementiert und verifiziert sind.

Arbeite innerhalb des beschriebenen Umfangs. Erhalte vorhandene Nutzeränderungen.
Verwende Unity und C# sowie die dokumentierte Editorversion. Prüfe den tatsächlichen
Werkzeugstand; führe unabhängig prüfbare Core-Arbeit bei fehlendem Editor fort und
kennzeichne einen noch ausstehenden Unity-Laufzeitnachweis ausdrücklich.
Erfinde keine fehlenden Spielregeln, Ressourcenboni oder kostenlosen Transporte.

Implementiere die Funktion einschließlich verständlicher Fehlerzustände und des
notwendigen gespeicherten Zustands. Führe passende Verhaltensprüfungen und die
relevante manuelle Prüfung durch. Halte technische Entscheidungen aktuell.

Berichte zum Abschluss: umgesetztes Verhalten, geänderte Dateien, tatsächlich
ausgeführte Prüfungen und verbleibende Einschränkungen. Setze das Paket erst dann
auf abgeschlossen, wenn seine Abnahmekriterien nachweislich erfüllt sind.
```

## Getrennte Prüfarten

Automatisierte Prüfungen sichern Bestandsänderungen, Berechtigungen, Sichtgrenzen, Befehle, Reservierungen und Zustandsübergänge. Sie können definierte Testbestände aufbauen; solche Daten dürfen nicht als heimliche Erzeugungsregeln in reguläre Partien gelangen.

Manuelle Prüfungen beurteilen Bedienbarkeit, Warnungen, räumliche Lesbarkeit, sinnvolle Reaktionszeit und taktische Wahlmöglichkeiten. Ein erfolgreicher automatisierter Test ersetzt keine sichtbare Prüfung eines neuen Spielablaufs.

Leistungsmessungen verwenden ein festgehaltenes Szenario und Hardwareprofil. Ein schneller leerer Prototyp beweist keine ausreichende Leistung bei mehreren Kolonien, KI und gleichzeitigen Gefechten. Umfang und Grenzwerte werden vor der jeweiligen Messung festgelegt.

## Modellgestaltung in 1.0.21

Die grafische Überarbeitung von Kolonie und Flotte betrifft die Darstellung bestehender Spielinhalte aus Aufbau, Logistik und Militär. [Modellgestaltung und Windows-Prüfung](MODELLE_1_0_21.md)

### Betriebsanimationen in 1.0.22

D-061 ergänzt LOG-018 um ein Zeitfenster je Ausfahrt. Bodenbewegung, Durchfahrtstore, Ladeschleusen, Fahnen und Partikeleffekte gehören zur Darstellung von P04, P06 und P08. Der Windows-Player prüft die Stellung der Ketten gegen das Gelände-Mesh, die Gelenke bewegter Infanterie und den Zyklus der Türen. Für den separaten Konvoitest dient `tools/Test-Presentation.ps1 -CourierSpacingOnly`. Der vollständige Durchlauf verwendet `tools/Test-Presentation.ps1 -ReferenceSave TestResults/Reference/victory.json`. [Abnahme TEST-045](ACCEPTANCE_TESTS.md#test-045--fahrzeuge-figuren-und-betriebsanimationen)

Am 28. November 2026 bestanden lokal alle 227 Coretests, der gesonderte Konvoitest und der vollständige Playerdurchlauf mit 1.157 PASS-Meldungen. Nach der abschließenden Ausrichtung der Torpfosten und Montage der Leuchten bestand der neu gebaute Windows-Player den Darstellungsdurchlauf `-PolishOnly` mit 606 PASS-Meldungen.

## Ergänzung 1.0.18: Zweispurige Straßen und Priorität stehender Einheiten

- **Umfang:** D-045–047 / LOG-015, SURFACE-011, GUI-022 und TEST-040; Core-Fahrspurpositionen, Vorfahrt, Bau-Rückfahrt, freie Umwege, passende Fahrzeughüllen sowie rein darstellende Zielpfeile.
- **Betroffene Module:** `SimulationGroundTraffic`, `SimulationLogistics`, `SimulationMilitary`, `SimulationSpaceTraffic`, Speicherprüfung; `PrototypeSmoothMotion`, `PrototypeVehicleSizing`, `PrototypeOrderArrows` und deren Integrationsprüfungen.
- **Status:** Implementiert; gezielte Verhaltenstests und isolierte Zielpfeil-/Modellprüfung bestanden. Beide vollständigen Referenzpartien und abschließende Leistungsmessung bestanden. Auch Paket und installierter Playerlauf bestanden. Git-Integration wird im Gesamtnachweis nachgeführt; noch kein integrierter Abschluss behauptet.

### Korrektur: schmale zweispurige Straßen

LOG-015 / TEST-040 / T-070: frühere Breite wiederherstellen, beide Richtungsfahrspuren erhalten, Straßenkuriere und Baufahrzeuge samt Modell und Kollisionskörper verkleinern. 45 gezielte Prüfungen und 212 vollständige Coretests einschließlich beider F05-Siege bestanden. Aktueller isolierter Modellnachweis und Paket werden im Gesamtnachweis dokumentiert.

### Versorgungsnetz – bestätigter Folgeauftrag

LOG-016 / TEST-041 / D-048 / T-075–077 ersetzen die sichtbaren Straßen durch bedarfsabhängige Lieferlinien und eine Logistikansicht. Größere Fahrzeuge, atomare Endpunktverbindungen, echte Warenfahrten und Be-/Entladung sind implementiert. 215 vollständige Coretests und 1076 Bedingungen im vollständigen isolierten Playerlauf bestehen. Aktuelle Darstellungsnachkorrektur und Paketnachweis stehen im GUI-Nachweis; GitHub-Pflichtchecks und Merge bleiben separat auszuweisen.

### Folgeauftrag: Verbindungsstraßen, Befestigungen und direkte Fahrt

D-049–053 / T-078–081 / LOG-017 / GUI-023 / SURFACE-012 / TEST-042 ergänzen das bestätigte Versorgungsnetz. Schmale Straßen, verbundene schmale Mauern/Tore samt Vorschau, Angriffsanfahrt bis zur Schussposition, transparente auswahlabhängige Bögen, direkte freie Bodenfahrt und Zentrieren bei Auftragswechsel sind implementiert. Die Belegungsplanung nutzt die aktuelle Fahrzeugwabe; tatsächliche Körperabstände bleiben geprüft. Landschaftsklick hebt die Einheitenauswahl auf. Aktuelle Test- und Paketnachweise folgen im GUI-Nachweis; ältere dortige Ergebnisse bleiben historisch.

### Version 1.0.23 · Bodenverteidigung und Gebäudefuhrpark

P06/P07, LOG-019, D-062–064 und TEST-046 ergänzen den laufenden Prototyp um zugeordnete Transporter, Werkstätten, Schadensstufen, Richtungstore und gemeinsame Infanteriestandplätze. `tools/Test-Core.ps1` bestand mit 232 Kernprüfungen einschließlich beider Referenzsiege. Der mit `tools/Unity.ps1 -Action Build` gebaute Windows-Player bestand `tools/Test-Presentation.ps1 -PolishOnly` auf einem isolierten Desktop. Die Aufnahmen `construction-delivery.png` und `damaged-tank-tracks.png` zeigen die Entladung am Bauplatz und den beschädigten Panzer in Fahrt.

### Version 1.0.24 · Geschütztürme, Fahrkurven und Energiebalken

SURFACE-012 und TEST-046: Alle vier Bodengeschütze stehen auf einem erhöhten Drehkranz. Bodeneinheiten runden Wegpunkte innerhalb der passierbaren Wabe ab. Beide Ketten schreiben ihre eigene Spur. Der Energiebalken folgt der zwischen den Simulationsticks dargestellten Einheit. `tools/Test-Core.ps1` bestand mit 233 Prüfungen, darunter Kurvenfahrt, Laden in der Kurve und Durchfahrten zwischen geparkten Fahrzeugen. Der mit `tools/Unity.ps1 -Action Build` gebaute Windows-Player bestand `tools/Test-Presentation.ps1 -PolishOnly` auf dem isolierten Desktop. Die Aufnahmen `tower-wall-clearance.png` und `tank-turn-tracks-hud.png` zeigen die geprüften Spielsituationen.

### Version 1.0.25 · Niedrigere Turmsockel

Die Drehachse der vier Bodengeschütze liegt auf 0,52 Welteinheiten über der Modellbasis. Im Mauerring von TEST-046 ergab die Prüfung im Windows-Player bei allen vier Türmen 0,10 Welteinheiten Abstand zwischen Drehteller und Maueranschluss. Auch der höchste Punkt der angrenzenden Mauern blieb beim Rundumschwenk frei.
