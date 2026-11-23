# PlanetOne Implementierungsplan

Dieser Plan zerlegt das Spielkonzept in ausführbare Codex-Arbeitspakete. Unity und C# sind eingerichtet; M0 und das erste vertikale Prototyp-Goal sind abgeschlossen. Ein Paket gilt erst nach nachvollziehbarer Prüfung seiner vollständigen Abnahmekriterien als abgeschlossen. Der Prototyp erfüllt Teile mehrerer größerer Pakete; diese bleiben entsprechend als teilweise umgesetzt ausgewiesen.

## Verbindliche Grundlagen

- [Spielregeln](./GAME_DESIGN.md): Anforderungen der Gruppen CORE, SPACE, SURFACE, ECO, LOG, RES, COMBAT, INV und DEF.
- [KI, Diplomatie und Sieg](./AI_DIPLOMACY.md): Anforderungen der Gruppen AI, DIP und WIN.
- [Technische Architektur](./TECHNICAL_ARCHITECTURE.md): Modulgrenzen, Zuständigkeiten, Daten und Schnittstellen.
- [Entscheidungen](./DECISIONS.md): offene beziehungsweise getroffene technische Entscheidungen und ihre Begründungen.
- [Abnahmeszenarien](./ACCEPTANCE_TESTS.md): wiederholbare Szenarien TEST-001 bis TEST-022 mit Regel- und Paketzuordnung.

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

Implementiert und automatisiert geprüft in Produktversion 1.0.1: 28 Core-Tests, Windows-Build und 58 Prüfbedingungen im sichtbaren Player mit vier visuell kontrollierten Renderbildern. Der 20-Minuten-Nachweis umfasst selbst erzeugte Nahrung sowie Ausbaukosten von 116 Metall/26 Bauteilen gegenüber Startvorräten von 100/20. Bedienungsrückmeldung des Nutzers und abschließende Git-Integration werden separat nachgeführt.

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

- **Status:** Implementiert und automatisiert geprüft in 1.0.1: Platzierung, Materialanlieferung, zwei priorisierte Bauplätze, Straßenabbau, Umleitung, Unterbrechung und Reparatur sowie Abbruch/Rückbau. Die zusätzliche echte Mausbedienungsabnahme ist beim Nutzer angefragt. **Abhängigkeit:** P03 abgeschlossen.
- **Module:** Oberfläche, Gebäude, Bauaufträge, Navigation, Eingabe.
- **Umfang:** Eine überschaubare Kolonieregion mit bebaubaren und gesperrten Bereichen; Platzierungsvorschau, Bauzeit, Materialbedarf, Lager, Straßen und lokale Transportverbindungen. Zunächst lesbare Platzhaltergrafik verwenden.
- **Nicht enthalten:** Gesamte Planetenkugeln, umfangreiche Geländeeditoren oder endgültige Grafik.
- **Abnahme:** Ein Gebäude kann nicht in gesperrtem Gelände oder über einem bestehenden Gebäude errichtet werden. Ohne angeliefertes Material wird es nicht fertig. Ein unterbrochener Weg verhindert die betroffene Lieferung und zeigt den Grund an.
- **Prüfung:** Automatisierte Bau- und Erreichbarkeitsregeln; manueller Aufbau einer kurzen Lieferkette einschließlich einer absichtlichen Wegeunterbrechung.

### P05 Produktion, Versorgung und Wiederaufbau

- **Status:** Implementiert und fachlich geprüft in 1.0.1: lokale Produktion, Nahrung, Bevölkerung/Wohnraum/Arbeitskräfte, Energiepuffer, Betriebsprioritäten und Wiederaufbau. Der 20-Minuten-Test weist Selbstversorgung und Ausbau aus eigener Metall- und Bauteilproduktion nach. Manuelle Bedienungsabnahme wird zusammen mit P04 nachgeführt; Balancingwerte bleiben vorläufig. **Abhängigkeit:** P04 für vollständigen Paketabschluss.
- **Module:** Inventare, Produktion, Bevölkerung, Energie, Wirtschaftsoberfläche.
- **Umfang:** Nahrung, Erz, Metall und Bauteile als erste Kette; lokale Energie, Arbeitskräfte, Lagerkapazität und nachvollziehbare Engpassanzeigen. Definitionen sehen die sechs vereinbarten Handelsgüter vor; Kristalle und Elektronik werden mit P10 produktiv genutzt.
- **Nicht enthalten:** Geldmarkt, Treibstoffwirtschaft oder zusätzliche Warenklassen.
- **Abnahme:** Fehlende Eingänge, Energie oder Arbeitskräfte stoppen beziehungsweise begrenzen die betroffene Produktion nach den Spielregeln. Eine abgeschlossene Produktion verbraucht genau ihre Eingänge und liefert genau ihre Ausgänge. Eine Heimatkolonie kann Grundgebäude und grundlegende Logistik ohne Kristallimporte herstellen.
- **Prüfung:** Automatisierte Mengenerhaltung, Kapazitätsgrenzen und Wiederaufbaukette. Ein manueller Engpassdurchlauf zeigt nicht nur „Mangel“, sondern dessen lokale Ursache.

## M3 Bodengefechte und Tower Defense

### P06 Bewegliche Bodentruppen und Sicht

- **Status:** Offen. **Abhängigkeit:** P05.
- **Module:** Bodennavigation, Auswahl, Befehle, Sicht, Kampf, Einheitenproduktion.
- **Umfang:** Infanterie, Panzerabwehr, Kampfpanzer, Artillerie sowie Pionier-/Versorgungsfahrzeug in einfachen Ausprägungen. Bewegung, Angriff, Halten und Rückzug; Deckung, Reichweite und Sicht. Einheiten entstehen durch reguläre Produktionsaufträge.
- **Nicht enthalten:** Komplexe Spezialfähigkeiten oder separate Luftkampfmechaniken.
- **Abnahme:** Verdeckte Gegner lassen sich nicht als aktuell sichtbare Ziele auswählen. Artillerie benötigt passende Zielinformation. Eine mobile Reserve kann eine Stellung unterstützen und anschließend zurückgezogen werden. Verlust und Neubau wirken auf den tatsächlichen Einheitenbestand.
- **Prüfung:** Automatisierte Sicht-, Reichweiten- und Schadensregeln; manuelles Gefecht auf zwei unterschiedlichen Angriffswegen.

### P07 Türme, Mauern und militärische Versorgung

- **Status:** Offen. **Abhängigkeit:** P06.
- **Module:** Verteidigungsanlagen, Navigation, lokale Logistik, Munition, Reparatur.
- **Umfang:** Vier Grundstellungen, zerstörbare Mauern, berechtigte Torpassage, Versorgungsdepots, lokale Munitionsvorräte, Reparatur und mobile Flugabwehr. Flugabwehr besitzt bereits ihre Zielregeln; reguläre Landungstransporter folgen mit P12.
- **Nicht enthalten:** Kostenlos erzeugte Angriffswellen, globale Turmlimits oder fortgeschrittene Schilde.
- **Abnahme:** Mauern verändern Wege, ohne Angriffsziele dauerhaft unerreichbar zu machen. Türme können bei fehlenden Betriebsmitteln nicht unbegrenzt weiterfeuern. Ein unterbrochener Versorgungsweg wirkt erst über vorhandene Vorräte. Eine Stellung lässt sich mit passender Aufklärung und Artillerie überwinden.
- **Prüfung:** Automatisierte Tore, Vorratsverbrauch und Gebäudeschaden. Manuelle Verteidigung mit zwei Linien und einer durchbrochenen Versorgung. Vorbereitete Testarmeen sind als isolierte Testdaten gekennzeichnet.

## M4 Raumansicht, Handel und Forschung

### P08 Sonnensystem und Ansichtswechsel

- **Status:** Ansichts-/Orbitanteil umgesetzt und im ersten Goal geprüft. Schiffsbau, Koloniegründung, Schiffsbewegung, Aufklärung und vollständige Paketabnahme bleiben offen. **Abhängigkeit:** P02 für vorgezogene Ansicht, P05 für den übrigen Umfang.
- **Module:** Orbitbewegung, Raumnavigation, Sensoren, Raumdarstellung, Kamera.
- **Umfang:** Stern, Planeten, Mond, vorhersehbare Bahnen, frei bewegliche Schiffe und Wechsel zu bestehenden Oberflächen. Flüge zu bewegten Zielen und dauerhafte Eskorte beziehungsweise Planetenverteidigung. Ein Raumhafen baut aus lokalen Waren einfache Frachter und Kolonisationsschiffe; ein Gründungsbefehl prüft Standort und Besitz und überführt genau ein Schiff samt tatsächlicher Ausrüstung in eine neue Kolonie. Die zugehörigen Basistechnologien sind bis P10 in der Prüfkarte ausdrücklich vorab freigeschaltet.
- **Nicht enthalten:** Mehrere Sonnensysteme, freie dreidimensionale Kampfbewegung oder realistische Himmelsmechanik.
- **Abnahme:** Ein Schiff erreicht einen bewegten Zielplaneten ohne wiederholte manuelle Zielkorrektur. Eine aus eigener Produktion finanzierte zweite Kolonie entsteht mit genau den deklarierten Anfangsbeständen; der Gründungsträger ist anschließend verbraucht. Ein wiederholter Gründungsbefehl erzeugt keine Kopie. Ein Ansichtswechsel erzeugt keine zweite Kolonie und setzt keine Produktion zurück. Sichtinformation bleibt fraktionsgebunden.
- **Prüfung:** Automatisierte Zielverfolgung und Zustandsidentität; manueller Flug während mehrerer Ansichtswechsel.

### P09 Frachter und automatische Bedarfsversorgung

- **Status:** Offen. **Abhängigkeit:** P08.
- **Module:** Transportaufträge, Inventarreservierung, Raumhafen, Routen, Warnungen.
- **Umfang:** Reale Frachtkapazität, Be- und Entladung, Mindestbestände, Exportschutz, bereits unterwegs befindliche Ware und Lieferprioritäten. Feste Route und automatisch versorgtes Kolonienetz.
- **Nicht enthalten:** Handel mit fremden Fraktionen; dieser folgt mit P16.
- **Abnahme:** Gegeben ist ein Defizit auf Planet B und exportierbare Ware auf Planet A. Ein Frachter reserviert Ware und Zielplatz, holt die Ware ab und liefert sie physisch. Eine parallele Bedarfsauswertung bestellt dieselbe Menge nicht doppelt. Auftragabbruch, Zielverlust und Schiffsverlust lösen Reservierungen korrekt auf.
- **Prüfung:** Automatisierte konkurrierende Aufträge, Teilmengen, Verlust und volle Lager. Manueller Vergleich zwischen Produktionsmangel und unzureichender Transportleistung.

### P10 Forschung und vollständige Warenketten

- **Status:** Offen. **Abhängigkeit:** P09.
- **Module:** Forschung, Freischaltungen, Entdeckungen, Produktionsdefinitionen.
- **Umfang:** Kristallabbau, Elektronik und eine kurze verzweigte Forschungsfolge mit Voraussetzungen, Materialverbrauch und reichsweiten Bauplänen. Technologiedaten werden auf ungültige Verweise und Zyklen geprüft.
- **Nicht enthalten:** Der endgültige Umfang des Forschungsbaums oder seltene narrative Entdeckungen.
- **Abnahme:** Eine Freischaltung erfordert ihre dokumentierten Voraussetzungen und tatsächlich verfügbare Versuchsmaterialien. Eine freigeschaltete Einheit benötigt beim Bau weiterhin lokale Waren und Infrastruktur. Kolonisierung und grundlegende Gegenmittel gegen Festungen bleiben ohne Kristalltechnologie erreichbar.
- **Prüfung:** Automatisierte Voraussetzungen und Freischaltungen; kompletter Ablauf von Grundwirtschaft über Außenposten bis zur ersten Elektronikproduktion.

## M5 Durchgängige Invasion

### P11 Raumgefechte und lokale Orbitalbereiche

- **Status:** Offen. **Abhängigkeit:** P07 und P10.
- **Module:** Raumkampf, Eskorten, Rückzug, Reparatur, Orbitalbereiche.
- **Umfang:** Orbitalwerft errichten und über reale Hafentransfers versorgen; Aufklärer, Korvette, Eskortfregatte und Torpedoschiff aus Material und Produktionszeit fertigen. Frachter abfangen und schützen, Schäden reparieren, lokale Gefahrenzonen um Planeten darstellen.
- **Nicht enthalten:** Vollständige Flottenvielfalt oder planetare Eroberung.
- **Abnahme:** Eine Eskorte verfolgt ihren Transport und reagiert auf einen Angriff. Ein zerstörter Frachter verliert seine tatsächliche Ladung. Ein verstecktes Schiff weit außerhalb des Anflugbereichs blockiert keine gesamte Planetenoperation.
- **Prüfung:** Automatisierte Ziel- und Schadensregeln; manueller Konvoikampf mit Rückzug und anschließender Reparatur.

### P12 Verladung, Landung und Brückenkopf

- **Status:** Offen. **Abhängigkeit:** P11.
- **Module:** Truppentransport, Invasionszustände, Landezonen, Warnungen, Felddepots.
- **Umfang:** Vorhandene Bodentruppen verladen, zu einer geeigneten Landefläche transportieren, sichtbar absteigen und entladen. Begrenzte Startvorräte, angreifbare Verstärkungstransporte und Evakuierung.
- **Nicht enthalten:** Automatische Planeteneroberung oder kostenlos nachproduzierte Wellen.
- **Abnahme:** Jede gelandete Einheit lässt sich ihrem vorherigen Bestand und Transport zuordnen. Einheiten sind während des Transports nicht zugleich am Ausgangsort aktiv. Transportverluste werden genau einmal verbucht. Flugabwehr kann geeignete Anflugziele bekämpfen. Eine abgeschnittene Bodenarmee bleibt bestehen und nutzt ihre örtlichen Vorräte.
- **Prüfung:** Automatisierte Bestandsübertragung und Landeberechtigung; manuelle Landung, Nachlieferung und Evakuierung unter Bedrohung.

### P13 Eroberung und begrenzte Orbitalunterstützung

- **Status:** Offen. **Abhängigkeit:** P12.
- **Module:** Gebäudebesetzung, Besitzwechsel, Invasion, Orbitalfeuer, Schilde.
- **Umfang:** Verwaltungszentrale und Versorgungspunkt tatsächlich sichern, Gebäude übernehmen und verbliebene Verteidiger weiterführen. Orbitalunterstützung mit Zielaufklärung, Vorbereitung und Gegenwehr; Schutzschild und planetare Abwehrbatterie in einfacher Form.
- **Nicht enthalten:** Diplomatische Besitzabsprachen oder gemeinsamer Szenariosieg.
- **Abnahme:** Raumüberlegenheit allein überträgt keinen Planeten. Infanterie und gesicherte Versorgung können ihn übernehmen; überlebende Verteidiger verschwinden nicht. Orbitalfeuer beschädigt Ziele, übernimmt sie aber nicht. Ein Gegenangriff kann relevante Anlagen zurückerobern.
- **Prüfung:** Automatisierte Besitz- und Versorgungskriterien; vollständige manuelle Invasion mit mindestens einer Verteidigungslinie und alternativem Landeort.

**Erstes zusammenhängend spielbares Teilergebnis nach M5:** Ein System mit zwei Startwelten und einem umkämpften Mond. Wirtschaft, Forschung, Frachter, Raumkampf, Landung, Bodenkampf und Eroberung funktionieren gemeinsam. Ein Skriptgegner darf hier den Prüfablauf steuern; er ist ausdrücklich noch kein Nachweis intelligenter Fraktions-KI und kein Ersatz für M6/M7.

## M6 Eigenständige Computerfraktionen

### P14 Wirtschaft, Forschung und begrenztes Wissen

- **Status:** Offen. **Abhängigkeit:** P13.
- **Module:** KI-Wahrnehmung, Wirtschaftsplanung, Forschung, gemeinsame Befehlsverarbeitung.
- **Umfang:** Eine KI baut ihre Grundversorgung auf, erkennt Engpässe, reserviert Ressourcen, erschließt Standorte und erforscht passende Technik. Entscheidungen erfolgen aus eigenem Wissen und öffentlichen Informationen.
- **Nicht enthalten:** Rohstoffboni, allwissende Gegnerdaten oder Diplomatie.
- **Abnahme:** Nach Unterbrechung einer Lieferroute erkennt die KI deren Ursache und versucht eine passende Gegenmaßnahme. Ein ungesehener gegnerischer Neubau verändert ihre Planung erst nach legitimer Information. Alle Aufträge durchlaufen dieselbe Validierung wie Spielerbefehle.
- **Prüfung:** Wiederholbare Wirtschaftsszenarien sowie Informationsabschirmung; Entscheidungsprotokoll mit Ziel, beobachteten Gründen und gewähltem Auftrag.

### P15 Militärische Planung und Anpassung

- **Status:** Offen. **Abhängigkeit:** P14.
- **Module:** KI-Strategie, Operationsplanung, taktische Steuerung, Verteidigung.
- **Umfang:** Aufklären, Ressourcen und Kräfte sammeln, Konvois eskortieren, Invasionen vorbereiten, Reserven einsetzen und abbrechen. Auf starke bekannte Turmstellungen mit passender Ausrüstung, anderer Landefläche oder Zielwechsel reagieren.
- **Nicht enthalten:** Maschinelles Lernen oder perfekte vorausschauende Entscheidungen.
- **Abnahme:** Bei unzureichender Transportkapazität verschiebt die KI die Landung oder beschafft Transporter. Nach Aufklärung einer für ihre Armee ungeeigneten Stellung verändert sie ihren Plan vor einem erneuten Angriff. Ein Rückzug erhält Überlebende und löst Operationsreservierungen auf.
- **Prüfung:** Vergleichbare Szenarien mit verschiedenen Verteidigungen und mehreren Startwerten. Protokolle müssen Anpassung, tatsächliche Ressourcen und nachvollziehbare Abbruchgründe zeigen.

## M7 Diplomatie und gemeinsamer Sieg

### P16 Verträge und Rechte

- **Status:** Offen. **Abhängigkeit:** P15.
- **Module:** Diplomatie, Handelsabwicklung, Berechtigungen, Beziehungen, Dialogoberfläche.
- **Umfang:** Paarweise Peace/War-Beziehungen mit ausdrücklicher Kriegserklärung sowie Waffenstillstand, Nichtangriff, physischer Handel, Transit, Nutzung und Verteidigungspakt mit Zuständen, Laufzeiten und Folgen. Angebote können Spieler oder KI beginnen. Mehrere KI handeln auch miteinander.
- **Nicht enthalten:** Gemeinsame Siegauswertung oder zusammengelegte Fraktionslager.
- **Abnahme:** Ein Handelsvertrag erlaubt keine unvereinbarte Truppenlandung. Eine gültige feindliche Invasion benötigt dagegen keine Zustimmung des Verteidigers. Ware wird geliefert und nicht bei Vertragsabschluss teleportiert. Gewöhnliche automatische Waffenwahl erklärt keinen Krieg. Ein eigener Angriffskrieg aktiviert keine unpassende Verteidigungspflicht. Ablehnungen und Vertragsverletzungen besitzen erkennbare Gründe.
- **Prüfung:** Automatisierte Rechte- und Zustandsmatrix einschließlich Ablauf, Kündigung und Vertragsbruch; manuelle Verhandlung mit zwei KI-Fraktionen.

### P17 Koalitionen und gemeinsame Operationen

- **Status:** Offen. **Abhängigkeit:** P16.
- **Module:** Koalitionen, Sichtfreigabe, Operationszusagen, Stationsrechte, Besitzabsprachen.
- **Umfang:** Koalitionsmitgliedschaft, vereinbarte gemeinsame Sicht, konkrete Hilfszusagen, reservierte Kräfte, vorgesehener Eigentümer einer Eroberung und geordnete Räumung bei Vertragsende.
- **Nicht enthalten:** Automatische Vollkontrolle über verbündete Einheiten oder kostenlose gemeinsame Produktion.
- **Abnahme:** Eine KI sagt einen verfügbaren Beitrag zu, bringt ihn an den vereinbarten Ort oder meldet eine begründete Änderung. Dieselbe Einheit wird nicht gleichzeitig für zwei unvereinbare Einsätze reserviert. Eine gemeinsame Eroberung weist den vereinbarten Eigentümer zu. Stationierte Partner nutzen eine Vertragsbeendigung nicht zum sofortigen Angriff aus dem Inneren einer Basis.
- **Prüfung:** Automatisierte Mitgliedschaft, Reservierungen und Räumungsrechte; manuelle gemeinsame Verteidigung und gemeinsame Invasion.

### P18 Szenarioziel, Kapitulation und gemeinsamer Sieg

- **Status:** Offen. **Abhängigkeit:** P17.
- **Module:** Szenarioziele, Sprungtorprojekt, Fraktionsstatus, Siegoberfläche.
- **Umfang:** Öffentliches Aufbauziel mit realen Beiträgen und abschließender Aktivierung. Koalitionssieg, Kapitulation, echte Eliminierung einschließlich einmaliger Restbesitz-/Vertragsabwicklung und Fortsetzung nach Hauptstadtverlust gemäß WIN-Regeln.
- **Nicht enthalten:** Kampagne, Mehrspieler oder zusätzliche Siegvarianten.
- **Abnahme:** Der Verlust einer Hauptstadt eliminiert keine Fraktion mit verbleibender Wiederaufbaumöglichkeit. Versteckte Restschiffe verhindern kein erfülltes Szenarioziel. Mitgliedschaftsänderungen während der Schlussaktivierung brechen diese Phase ab und verlangen einen Neustart. Alle beteiligten Koalitionsmitglieder gewinnen gemeinsam; auch eine vollständig friedliche Kooperation kann das Bauziel erfüllen.
- **Prüfung:** Automatisierte Sieg- und Niederlagematrix einschließlich Beitritt, Austritt und Eliminierung während der Aktivierung; manueller Abschluss des vollständigen Szenarios.

## M8 Gesamtprüfung und Export

### P19 Speicherstände und Integrationsgrenzen absichern

- **Status:** Offen. **Abhängigkeit:** P18.
- **Module:** Persistenz und alle zustandsführenden Systeme.
- **Umfang:** Vollständige Speicher-Lade-Prüfung an kritischen Übergängen; saubere Fehler bei ungültigen oder inkompatiblen Daten. Bestehende Tests um tatsächlich gefundene Integrationsfehler ergänzen.
- **Nicht enthalten:** Cloudspeicher oder unbegrenzte Kompatibilität mit allen zukünftigen Versionen.
- **Abnahme:** Laden während Frachttransport, Truppenabstieg, Gebäudebesetzung, Vertragskündigung und Toraktivierung erhält Bestände, Reservierungen, Rechte und Fortschritt. Es entstehen keine Kopien von Waren oder Einheiten und keine ungewollten Friedens- oder Besitzwechsel.
- **Prüfung:** Automatisierte gespeicherte Übergangsszenarien und manueller Fortsetzungsvergleich. Sichtinformationen und KI-Planreservierungen werden mitgeprüft.

### P20 Plattformen, Bedienung und Leistung prüfen

- **Status:** Offen. **Abhängigkeit:** P19.
- **Module:** Plattformadapter, Eingabe, Darstellung, Benutzeroberfläche, Export.
- **Umfang:** Vereinbarte Exportziele aus P01 bauen und auf tatsächlich verfügbaren Zielumgebungen prüfen. Warnungen, Ansichtswechsel, skalierbare Oberflächen und Eingabeprofile prüfen. Simulation und Darstellung getrennt auf dem vereinbarten Lastprofil messen.
- **Nicht enthalten:** Unvereinbarte Storeveröffentlichung oder behauptete Unterstützung ungeprüfter Hardware.
- **Abnahme:** Nachgewiesene Exporte starten und führen das Referenzszenario aus. Laufzeit- und Speicherziele aus P01 sind mit Hardware, Szenariogröße und Messmethode belegt. Nicht verfügbare Zielumgebungen bleiben ausdrücklich ungeprüft. Eingabebefehle bleiben funktional gleich, auch wenn ihre Darstellung plattformspezifisch ist.
- **Prüfung:** Reproduzierbare Buildnachweise, gezielte Leistungsmessungen und manuelle Bedienungsprüfung; Optimierungen werden nur an gemessenen Engpässen vorgenommen.

### P21 Vollständiges Referenzszenario abnehmen

- **Status:** Offen. **Abhängigkeit:** P20.
- **Module:** Gesamtes Spiel, Szenariodaten, Ergebnisanzeige.
- **Umfang:** Ein Stern, drei Startwelten, zwei umkämpfte Monde, ein Spieler und zwei wirtschaftlich eigenständige KI. Alle Startwelten tragen die Grundversorgung; Kristallvorkommen sind erreichbar und ermöglichen konkurrierende beziehungsweise kooperative Entwicklung. Kein kostenloser Nachschub für reguläre Fraktionen.
- **Nicht enthalten:** Zusätzliche Systeme, Kampagne, fertige künstlerische Ausstattung oder Mehrspieler.
- **Abnahme:** Ein vollständiger Durchlauf zeigt Aufbau, Forschung, automatische Lieferung, Raumkampf, echte Invasion, adaptive KI, mindestens einen Vertrag und ein abgeschlossenes Szenarioziel. Ein zweiter gezielter Durchlauf weist den gemeinsamen Koalitionssieg nach. Wirtschaftlicher Wiederaufbau nach einem Konvoiverlust bleibt möglich.
- **Prüfung:** Protokollierte Spieltests mit Startwert, wesentlichen Entscheidungen, Ergebnis und gefundenen Fehlern. Automatisierte Gesamtzustandsprüfungen sichern Mengen, Eigentümer, Rechte und Zielerfüllung; subjektive Lesbarkeit und Spielspaß werden im manuellen Test bewertet.

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
