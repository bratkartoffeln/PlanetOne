# PlanetOne

PlanetOne ist ein Einzelspieler-Echtzeitstrategiespiel mit planetarem Aufbau, physischer Warenlogistik, Raumgefechten, steuerbaren Bodeninvasionen, vorbereiteten Verteidigungsstellungen und diplomatisch handelnden Computerfraktionen.

Der erste Prototyp läuft mit **Unity 6000.3.29f1 und C#** als Windows-Anwendung. Er verbindet ein Sonnensystem mit bebaubaren Kolonieoberflächen über eine gemeinsame Simulation. Die Gestaltung verbindet klare Geometrie mit texturierten Oberflächen und dunklem Weltraum. Einrichtung und geprüfte Start- und Buildschritte stehen im [Entscheidungsstand](docs/DECISIONS.md).

## GUI, Bewegung und Fahrzeugbau in 1.0.18

Die drei Layouts **Kommandozentrale**, **Orbitale Brücke** und **Freier Weltraum** erhalten größere Texte, klar getrennte Aufgabenbereiche und überarbeitete Spiel-, Speicher-, Flotten-, Militär- und Diplomatiemenüs. Unter **Einstellungen → Schrift und Größe** lässt sich die Schrift unabhängig von der Oberflächengröße auf **90–140 %** einstellen. Die vorhandene Konzepttafel bleibt die gestalterische Referenz.

Neue reguläre Oberflächen umfassen **40 × 32 Waben**. Detailliertere Gebäudemodelle, typabhängiger Bewuchs mit unregelmäßigen, vom Startwert bestimmten Waldflächen, eine transparente Modellvorschau und sichtbare Bauphasen ergänzen das Gelände. Fahrzeuge folgen gerundeten Transportkorridoren; bewegte Modelle richten sich weich aus. Echte Schüsse lösen Geschosse, Mündungs- und Trefferbilder aus, Geschütztürme drehen unabhängig vom Fahrzeug. System und Oberfläche verbindet eine Kamerafahrt mit **1,8 Sekunden Animationszeit** zuzüglich kurzer, variabler Oberflächenvorbereitung unter der Atmosphärenblende. Bahnlinien behalten ihre Bildschirmbreite, Asteroiden erscheinen größer.

Gebäude werden jetzt von tatsächlich zugewiesenen Baufahrzeugen errichtet: erst Material liefern, dann anfahren, bauen und zurückkehren. Höchstens **zwei der vier vorhandenen Kuriere** sind dafür gebunden; auf Gaswelten übernehmen Frachtdrohnen diese Aufgabe. Reine Gebäude-Bauzeiten sind verdoppelt. Eine fehlende Verbindung oder ein fehlendes Fahrzeug stoppt den Bau. Kantenstraßen nach Catan wurden abgewogen, sind aber nicht umgesetzt.

Neu implementiert: **Rechtsklick bewegt ausgewählte Einheiten oder greift einen sichtbaren Kriegsgegner an; Strg + Rechtsklick öffnet das Kontextmenü.** Rechtsziehen verschiebt weiterhin die Kamera. Auswahlrahmen umfassen die tatsächlichen Modelle, ein rotes Fadenkreuz kennzeichnet Angriffsziele. Boden-, Kurier- und Raumverkehr berücksichtigen Körperabstände und machen eigenen blockierten Einheiten vorübergehend Platz. Planetentexturen erhalten eine nahtfreie Meridianabtastung. **Planet zentrieren** folgt dem bewegten Planeten bis zur nächsten manuellen Kameraaktion; die Seitenliste fliegt sanft zum ausgewählten Planeten im System. Das Speicherformat ist jetzt **13**.

**Versorgungsnetz statt Asphalt:** Größere Transporter fahren über das Gelände. „Verbindung planen“ verbindet ein Startgebäude mit einem Zielgebäude oder einem künftigen Bauplatz; einzelne Straßenfelder müssen nicht mehr gesetzt werden. Die Auswahl eines Gebäudes zeigt dessen Lieferungen. **L / Logistikansicht** blendet das gesamte Netz mit Fahrtrichtungen, Fracht und Wartezuständen ein. Ladebehälter zeigen tatsächlich transportierte Ware, kurze Symbole die Be-/Entladung. Gegenverkehr, physische Fracht und das während der Arbeit unbewegte Baufahrzeug bleiben erhalten. Gaswelten verwenden weiterhin Frachtdrohnen.

**Lokaler Prüfstand:** 215 vollständige Core-/Plattformtests bestanden. Der isolierte 720p-Gestaltungslauf bestand 516 Einzelbedingungen, der vollständige 1440×900-Playerlauf 1076 Einzelbedingungen plus Gesamt-PASS mit 193 PNGs. Unity-Windows-Build und Leistungsbudgets bestehen. Aktuelle Nachkorrekturen, Paketprüfung und GitHub-Integration werden im [Nachweis mit Bildherkunft und Messwerten](docs/GUI_BEWEGUNG_BAU_NACHWEIS.md) getrennt dokumentiert.

## M9: Startmenü und Spielstände in 1.0.17

**Neue Partie → Drei Kolonien** startet das vollständige Szenario. Zwei weitere reguläre Partien, ein optionaler Startwert und eine vierseitige **Starthilfe** stehen im Menü bereit. Prüfkarten sind separat erreichbar. Menü und Einstellungen halten die Spielzeit an; die Leertastenpause bleibt dabei erhalten.

**Speichern … / Laden …** bietet acht getrennte Speicherplätze mit jeweils einer ausdrücklich ladbaren Sicherung. **F5** verwendet zunächst einen freien Platz und aktualisiert danach den aktiven Platz; **F9** fragt vor dem Laden nach. Alte Spielstände bleiben zusätzlich ladbar und werden beim weiteren Speichern in einen neuen Platz übernommen. Ein belegter manueller Speicherplatz oder Partiewechsel erfordert eine Bestätigung. [Bedienung und Prüfstand](docs/M9_NACHWEIS.md).

## Objektbefehle und Kontextmenüs in 1.0.16

**Seit 1.0.18:** Linksklick wählt aus, kurzer Rechtsklick erteilt den direkten Bewegungs-/Angriffsbefehl, **Strg + Rechtsklick** öffnet passende Befehle und Rechtsziehen verschiebt die Kamera. Die untere Leiste zeigt die wichtigsten Aktionen zur Auswahl; **Alle Befehle …** macht das Kontextmenü auch ohne Rechtsklick erreichbar. Gebäude bieten Betrieb, Priorität, Produktion und Details. Die Flottenverwaltung trennt Schiffe, Aufträge, Schiffbau und Abbau. Rückbau erfordert eine Bestätigung mit Rückgewinnung; Escape oder ein verbrauchter Klick daneben schließt ein Menü. [Recherche und historischer Prüfstand 1.0.16](docs/BEFEHLS_GUI_NACHWEIS.md).

## M8: Referenzpartie und sichere Speicherstände in 1.0.15

**Spielmenü → Referenzpartie · Drei Kolonien** startet drei gleich ausgestattete Kolonien, zwei umkämpfte Kristallmonde und zwei selbstständig wirtschaftende KI. **Sicherung laden** stellt ausdrücklich den vorherigen gültigen Spielstand wieder her. Baustellen reservieren nur Material aus erreichbaren Lagern; die KI erfüllt Handelszusagen auch bei laufender automatischer Versorgung. Der [M8-Nachweis](docs/M8_NACHWEIS.md) beschreibt die vollständigen regulären und kooperativen Durchläufe, Dateiprüfungen, Windows-Paketprüfung, Leistungsmessung und verbleibenden Abnahmegrenzen.

## Größenverhältnisse, Korona und Asteroidenabbau in 1.0.14

Planeten bleiben unter dem halben Sternradius, Monde sind wesentlich kleiner. Auf Nutzerwunsch sind Asteroiden dreimal größer als im ersten 1.0.14-Stand und bleiben unter einem Viertel des Monddurchmessers. Ein weicher Nebelring macht den Gürtel aus der Ferne erkennbar; beim Hineinzoomen blendet er aus. **Brocken aus der Nähe** fokussiert einen einzelnen Asteroiden. Die sichtbaren Bahnabstände sind um Faktor **2,2** vergrößert. Die Sonnenkorona bleibt beim Verschieben und Drehen rund um den Stern verteilt. Ein Klick auf den Stern öffnet seine Infokarte mit Typ, Masse, Radius, Temperatur und Leuchtkraft; **Sterndetails** zeigt die vollständigen Angaben.

Neue diplomatische und KI-Partien erzeugen einen zufälligen System-Seed. Manche Systeme enthalten einen dünn besetzten Asteroidengürtel außerhalb der Gesteinsplaneten und innerhalb des Gasriesen. Unter **Flotte → Asteroidenabbau** ein Vorkommen auswählen, mit einem Schiff erkunden und einen freien Frachter mit erforschtem Raumtransport hinschicken. **Erz abbauen und heimbringen** führt bis zu 40 Erz zum ausgewählten eigenen Raumhafen zurück. Vorkommen sind endlich, die Fracht bleibt bis zur tatsächlichen Entladung an Bord. **Spielmenü → Testszenarien → Asteroidenabbau** bietet eine separate Prüfkarte mit regulär aufgebauter Wirtschaft und bezahltem Frachter. Ältere Spielstände erhalten keine nachträglich erzeugten Lagerstätten. [Bedienung, Recherche und Nachweis](docs/ASTEROIDEN_NACHWEIS.md).

## Planetentypen, Landschaften und Luftfracht in 1.0.13

Sechs Welttypen verbinden passende Planetenkugeln mit Fels-, Wüsten-, Pflanzen-, Insel-, Eis- und Gaslandschaften. Neue Karten bieten **28 × 22 Waben** statt 18 × 14. Der Boden ist wieder sichtbar texturiert; Ebenen, Dünen, Höhenzüge und Küsten ersetzen die einheitliche Talform. **Rechte Maustaste halten und ziehen** verschiebt jetzt auch die Oberfläche. Seit 1.0.18 öffnet Strg + Rechtsklick die passenden Objekt- und Zielbefehle; der kurze Rechtsklick erteilt direkt Bewegung oder Angriff.

Gasriesen besitzen schwebende Plattformen und vier Frachtdrohnen mit je acht Waren Kapazität. Es gibt dort keine Straßen, Räder oder Bodenminen; Rohstoffe kommen über Raumfracht. Planetenabstände, Größen und Klimarichtwerte folgen einem dokumentierten Modell für einen sonnenähnlichen Stern, Eigenrotation ist deutlicher sichtbar. Unter **Spielmenü → Testszenarien → Planetentypen / Luftfracht** lassen sich alle Umgebungen sofort in einer ausdrücklich gekennzeichneten Prüfkarte ansehen. Alte Spielstände behalten Karten und Bestände. [Recherche, Regeln und Nachweis](docs/PLANETENTYPEN_NACHWEIS.md).

## Freie Systemkamera und Keplerbahnen in 1.0.12

**Rechte Maustaste halten und ziehen** verschiebt die Systemansicht frei; Strg + Rechtsklick öffnet seit 1.0.18 das Objekt-/Zielmenü. **WASD/Pfeiltasten** verschieben ebenfalls, **Q/E** und die mittlere Maustaste drehen, **Pos1** zentriert wieder auf den Stern. Räumlich verteilte Sterne zeigen entfernungsabhängige Parallaxe, der Nebel dreht mit der Blickrichtung. Planeten drehen sich sehr langsam und umlaufen den Stern auf wesentlich langsameren Keplerbahnen. Mauszeiger-Popups sind entfernt, Namensschilder passen sich knapp an ihren Text an. [Bedienung, Umlaufzeiten und Nachweise](docs/KAMERA_ORBIT_NACHWEIS.md).

## GUI und Weltraumansicht in 1.0.11

Die drei Layouts folgen der [Konzepttafel](docs/GUI_REFERENCE.png) mit schmaler Ressourcenleiste, einheitlichen Symbolen und klar getrennten Auswahl- und Verwaltungsbereichen. Ein Klick auf einen Planeten zeigt seine Auswahlkarte; **Oberfläche öffnen** oder ein Doppelklick wechselt zur Kolonie. Der Koloniename oben öffnet deren Übersicht, der Pfeil führt zurück ins System. Nebel, detaillierte Planetentexturen, Atmosphären und Sonnenkorona überarbeiten die Weltraumansicht. [Bedienung, Bildvergleich und Prüfergebnisse](docs/GUI_WELTRAUM_NACHWEIS.md).

## Räumliche Modelle und Gelände in 1.0.10

Gebäude, Fahrzeuge und Raumschiffe bestehen aus vollständiger 3D-Geometrie. **Q/E** dreht die Kamera, **mittlere Maustaste ziehen** dreht und neigt sie, **Pos1** setzt die Blickrichtung zurück. Bewegliche Einheiten richten sich an ihrer tatsächlichen Fahrt aus. Durchgehendes Gelände besitzt Berge, Täler und Felsgruppen; Straßen, Auswahlrahmen und Bauplätze berücksichtigen die Höhe. Die Wabengeometrie der Simulation bleibt erhalten. [Umsetzung, Bedienung und Prüfgrenzen](docs/3D_GRAFIK_NACHWEIS.md).

## Drei Layouts und Texturen seit 1.0.9

Unter **Einstellungen → Darstellung** lassen sich **Kommandozentrale**, **Orbitale Brücke** und **Freier Weltraum** direkt wechseln. Layout, Wabengrenzen und Oberflächengröße werden unabhängig vom Spielstand gespeichert. Ein bebilderter Baukatalog, einheitliche Verwaltungsfenster, überarbeitete Gebäude/Schiffe und eigene Boden-, Metall-, Beton- und Solartexturen ergänzen die Darstellung. [Bedienung, Quellen und Prüfgrenzen](docs/GUI_GRAFIK_NACHWEIS.md).

`tools/Test-Presentation.ps1` prüft die vollständige Player-GUI auf einem separaten, nicht eingeblendeten Windows-Desktop. Kein Screenreader, Computer-Use oder Wechsel des Arbeitsdesktops ist erforderlich.

## Diplomatie und gemeinsamer Sieg in 1.0.8

**M7 Diplomatische Partie** startet friedlich mit drei getrennten Fraktionen und zwei selbstständig handelnden KI. **Diplomatie und Sprungtor** bietet Verträge, echte Handelslieferungen, getrennte Zugangsrechte, Koalitionen, konkrete Schiffsbeiträge und das gemeinsame Torziel. Krieg und Vertragsbruch sind ausdrückliche Entscheidungen; Räumungsfristen schützen den geordneten Abzug. Kapitulation und Eliminierung wickeln vorhandene Bestände einmalig ab. 126 Core-Tests, Windows-Build und 131 fensterlose Player-Prüfungen bestanden; [Bedienung, Regeln und Grenzen](docs/M7_NACHWEIS.md).

## Computergegner in 1.0.7

**M6 Gegen Computer spielen** startet eine separate Partie gegen eine selbstständig wirtschaftende KI. Gleiche Anfangsmittel, keine Gratisflotten: Aufbau, Forschung, Erkundung, Kolonisierung, Flotten-/Truppenproduktion und vorbereitete Invasionen nutzen reguläre Befehle. Wissen bleibt auf eigene Zustände und legitime Aufklärung begrenzt. Pläne, Reservierungen und Entscheidungen werden gespeichert. 96 Core-Tests, Windows-Build und 113 fensterlose Player-Prüfungen bestanden; [Nachweise und Grenzen](docs/M6_NACHWEIS.md).

## Wabenoberflächen in 1.0.6

Bodenfelder, Straßen, Bauvorschau und Auswahl verwenden ein Hexraster mit sechs Nachbarn. Wege, Sicht, Deckung, Reichweiten und Landeflächen folgen derselben Geometrie; alte Spielstände werden übernommen. 82 Core-Tests und 104 fensterlose Player-Prüfungen bestanden. [Regeln, Migration und Prüfschritte](docs/HEXRASTER_NACHWEIS.md). Der Standardaufruf `tools/Test-Prototype.ps1` öffnet kein Fenster und erzeugt Weltbilder ohne IMGUI; manuelle Bedienungsabnahme bleibt separat.

## Einstieg für Codex

1. [Arbeitsregeln](AGENTS.md) lesen.
2. [Entscheidungsstand](docs/DECISIONS.md) und den eingerichteten Unity-/Werkzeugstand prüfen.
3. Die Spielregeln in [GAME_DESIGN](docs/GAME_DESIGN.md) und [AI_DIPLOMACY](docs/AI_DIPLOMACY.md) lesen.
4. Die [technische Architektur](docs/TECHNICAL_ARCHITECTURE.md) und die zum Auftrag gehörenden [Abnahmeszenarien](docs/ACCEPTANCE_TESTS.md) beachten.
5. Ein abgegrenztes Paket aus dem [Umsetzungsplan](docs/IMPLEMENTATION_PLAN.md) bearbeiten und dessen Status erst nach erfolgreicher Prüfung ändern.

## Dokumente und Zuständigkeiten

Das vollständige Spielkonzept als zusammenhängende Übersicht steht in [SPIELKONZEPT](docs/SPIELKONZEPT.md). Es unterscheidet das geplante Gesamtspiel vom bereits geprüften Prototyp und verweist auf die verbindlichen Detailregeln.

| Dokument | Verbindlicher Inhalt |
|---|---|
| [GAME_DESIGN](docs/GAME_DESIGN.md) | Welt, Aufbau, Güter, Transporte, Forschung, Gefechte, Invasionen, Tower Defense und Bedienung |
| [AI_DIPLOMACY](docs/AI_DIPLOMACY.md) | KI-Verhalten, Informationsgrenzen, Verträge, gemeinsame Operationen, Koalitionen und Sieg |
| [TECHNICAL_ARCHITECTURE](docs/TECHNICAL_ARCHITECTURE.md) | Systemgrenzen, Zustandsmodell, Befehle, Simulation, Persistenz und Portabilität |
| [IMPLEMENTATION_PLAN](docs/IMPLEMENTATION_PLAN.md) | Abhängigkeiten, Arbeitspakete, Lieferumfang und Abschlusskriterien |
| [ACCEPTANCE_TESTS](docs/ACCEPTANCE_TESTS.md) | Konkrete Szenarien zum Nachweis der Spielregeln |
| [DECISIONS](docs/DECISIONS.md) | Festlegungen, Arbeitsannahmen, offene Entscheidungen und Plattformvergleich |

Die Anforderungen besitzen stabile IDs. Eine Referenz auf eine ID bezeichnet die Regel, nicht den Nachweis einer bereits vorhandenen Implementierung. Zahlen für Kosten, Reichweiten und Zeitspannen sind Balancingdaten, solange sie nicht ausdrücklich als feste Regel bezeichnet werden.

## Ziel des ersten vollständigen Szenarios

Ein Sternsystem mit drei Startwelten, zwei erschließbaren Monden, einem menschlichen Spieler und zwei eigenständigen KI-Fraktionen. Die Parteien bauen Wirtschaft und Forschung auf, transportieren Güter und Bodentruppen, führen Raum- und Bodengefechte und können gemeinsam ein Sprungtorprojekt abschließen.

Einzelne frühere Meilensteine verwenden kleinere Prüfkarten. Sie gelten noch nicht als vollständiges Spiel. Weitere Sonnensysteme, Mehrspieler, umfangreiche Kampagnen und zusätzliche Warenketten sind spätere Erweiterungen.

## Erstes Entwicklungsziel

**Ein lauffähiger Unity-Prototyp mit C#, der ein Sonnensystem und eine bebaubare Planetenoberfläche über eine gemeinsame Simulation verbindet.**

Der erste zusammenhängende Ablauf umfasst den Wechsel vom Sonnensystem zur Oberfläche, Gebäudeplatzierung, eine einfache Erz-zu-Metall-Produktion, lokale Lieferwege, taktische Pause sowie Speichern und Laden. Beide Ansichten lesen denselben Spielzustand. Klare Formen, zurückhaltende Farben und unmittelbar sichtbare Bau- und Versorgungszustände bilden bereits die vorgesehene Gestaltung.

Dieses erste Goal ist erreicht. Der Prototyp enthält Teile von P01–P05 und den Ansichts-/Orbitanteil von P08; er schließt diese Pakete nicht pauschal ab. Schiffsbau, Koloniegründung, interplanetare Frachter, Forschung, Gefechte, KI und Diplomatie behalten ihre späteren Pakete.

## Zweites Entwicklungsziel

Die Koloniewirtschaft ist um Wohnraum, Bevölkerung, Nahrungsverbrauch, Arbeitskräfte, Betriebsprioritäten und Energiepuffer erweitert. Zwei Baustellen je Kolonie können gleichzeitig arbeiten. Straßen lassen sich unterbrechen und reparieren; Abbruch und Rückbau holen Material über reale Transporte zurück. Ein freigehaltener Rücknahmeplatz verhindert, dass zurückkehrende Ladungen die gesamte Logistik blockieren.

Ein reproduzierbarer Core-Test baut drei Minuten auf und betreibt die Kolonie weitere 20 Simulationsminuten ohne Hunger. Er erreicht 28 Bewohner und finanziert insgesamt 116 Metall und 26 Bauteile Baukosten aus dokumentierten Startvorräten von 100 Metall und 20 Bauteilen sowie eigener Produktion. Speicherformat 2 übernimmt auch einen echten Spielstand des ursprünglichen Prototyps.

## Entwicklungsmeilensteine

**M5 in Version 1.0.5, lokal umgesetzt und automatisiert geprüft:** Orbitalwerft, Raumkampf, Eskorte, Rückzug und Reparatur, reale Truppenverladung, Landung/Flak, versorgter Landekopf, Evakuierung und örtliche Eroberung. Einstieg über **M5 Invasion prüfen** beziehungsweise den Reiter **Krieg**. [M5_NACHWEIS](docs/M5_NACHWEIS.md) dokumentiert 74 Core-Tests, 103 grafische Einzelprüfungen sowie die noch offene manuelle Abnahme und Git-Integration.

**M4 in Version 1.0.3:** Schiffsbau, freie Erkundungsflüge, Eskorte, Koloniegründung, interplanetare Fracht mit Reservierungen, feste Routen und automatische Hafenversorgung sowie Kristalle, Elektronik und ein verzweigter Forschungsbaum sind umgesetzt. Einstieg über **Raumfahrt und Forschung** oder **M4 Aufbau und Raumfahrt**. Regeln, Bedienung, Balance und Nachweise stehen in [M4_NACHWEIS](docs/M4_NACHWEIS.md).

**M3 in Version 1.0.2:** Bodentruppen werden mit real angeliefertem Material rekrutiert. Sicht, Deckung, Gefechte, Rückzug, vier Geschützstellungen, Mauern, Tore, Depots und Pioniernachschub sind umgesetzt. Die Systemübersicht bietet eine ausdrücklich gekennzeichnete Gefechtsprüfkarte. Bedienung, Testbestände und Nachweise stehen in [M3_NACHWEIS](docs/M3_NACHWEIS.md).

| Meilenstein | Spielbares beziehungsweise überprüfbares Ergebnis |
|---|---|
| M0 | Unity- und C#-Projekt eingerichtet; Start und Build nachgewiesen |
| M1 | Gemeinsame Simulation, validierte Befehle, Pause und Speichergrundlage |
| M2 | Bebaubare Kolonie mit lokalen Waren, Transportwegen und Produktion |
| M3 | Bodentruppen, Türme, Mauern und militärischer Nachschub |
| M4 | Schiffsbau, Kolonisierung, interplanetare Versorgung und Forschung |
| M5 | Zusammenhängender Raumkampf, Landung und Bodeninvasion |
| M6 | Eigenständig wirtschaftende und anpassungsfähige Computerfraktionen |
| M7 | Verträge, Koalitionen, gemeinsame Operationen und gemeinsamer Sieg |
| M8 | Vollständiges Szenario, Bedienungsprüfung und vereinbarte Exporte |

Die ausführlichen Pakete und Nachweise stehen im [Umsetzungsplan](docs/IMPLEMENTATION_PLAN.md).

## Starten und prüfen

Alle folgenden PowerShell-Aufrufe werden im Repository-Ordner ausgeführt. Die Einrichtung verwendet Unity `6000.3.29f1`; die Skripte erkennen den üblichen Installationspfad. Bei einer anderen Installation kann `tools/Unity.ps1` der Parameter `-UnityPath` übergeben werden.

Den lokal erzeugten Player starten:

```powershell
& .\Builds\Windows\PlanetOne.exe
```

Das Projekt im Unity-Editor öffnen:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Unity.ps1 -Action Open
```

Für automatisierte Vorbereitung und Build einen bereits geöffneten Editor mit diesem Projekt vorher schließen:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Unity.ps1 -Action Prepare
powershell -ExecutionPolicy Bypass -File .\tools\Unity.ps1 -Action Build
```

Der Windows-Build entsteht unter `Builds/Windows/PlanetOne.exe`. Die Buildausgabe ist lokal erzeugt; auf einem neuen Checkout wird sie mit dem Buildskript erstellt.

Kernregeln und grafischen Integrationsablauf prüfen:

**Aktuelle Testvorgabe:** Den laufenden Arbeitsdesktop nicht stören; keine Screenreader- oder Computer-Use-Prüfung. Core-Tests und Unity-Batch-Builds laufen ohne sichtbare Fenster. Der Standard-Player-Test läuft im echten Batchmodus ohne sichtbares Fenster und schreibt Weltbilder über RenderTexture. Eine optionale manuelle Prüfung mit `-Visible` darf ausschließlich in einer isolierten Testsitzung erfolgen.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Test-Core.ps1
powershell -ExecutionPolicy Bypass -File .\tools\Test-Prototype.ps1
```

Der Core-Test nutzt Roslyn aus Visual Studio Build Tools; dafür ist kein dotnet-SDK erforderlich. Der grafische Test startet den gebauten Player fensterlos und schreibt Prüfprotokoll sowie 18 Weltbilder nach `TestResults/Smoke/<Zeitstempel>`. Er prüft Controller und Simulation, rendert jedoch kein IMGUI und ersetzt keine manuelle Bedienungsabnahme.

## Versionierung und Git-Abschluss

GitHub prüft Pull Requests und baut Windows-Pakete. Der getrennte Workflow **Release Windows download** veröffentlicht nach manuellem Start den geprüften aktuellen `main`-Build als Installer und portable ZIP. Einrichtung, Unity-Secrets, Pflichtcheck und tatsächlicher Prüfstand stehen in [CI_RELEASE](docs/CI_RELEASE.md).

Die Produktversion steht zentral in [VERSION](VERSION). Sie wird mit `tools/Version.ps1` geprüft und in die Unity-Produkteinstellungen übernommen. Der Player zeigt `Application.version`; Engineversion, Paketversionen und Savegame-Format haben ihre eigenen Versionsstände.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Version.ps1 -Action Get
powershell -ExecutionPolicy Bypass -File .\tools\Version.ps1 -Action Next
powershell -ExecutionPolicy Bypass -File .\tools\Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File .\Tests\Tools\Test-Version.ps1
```

Der projektspezifische [Commit-/PR-Skill](.agents/skills/commit-pr/SKILL.md) wird mit `$commit-pr` oder beim beauftragten Goal-Abschluss verwendet: von `main` einen Branch mit der nächsten Versionsnummer anlegen, die Produktversion synchronisieren, Änderungen prüfen und sinnvoll committen, den Branch pushen, einen PR nach `main` mergen und lokal auf den aktualisierten `main` zurückwechseln. Standard ist ein Patch-Schritt; Minor und Major können ausdrücklich gewählt werden. Ein Wiederanlauf auf demselben Versionsbranch erhöht die Version nicht erneut.

## Erste Schritte im Spiel

1. In der Systemansicht einen eigenen Planeten anklicken oder über die Seitenleiste öffnen.
2. Im Reiter **Bauen** zuerst **Hydroponik** und **Solaranlage** neben dem vorhandenen Straßennetz errichten. Damit werden Nahrung und Energie gesichert. Neue Bauplätze bei Bedarf zuerst mit Straßen anschließen.
3. Eine **Erzmine** auf das bereits angeschlossene Erzfeld setzen, anschließend **Schmelze** und **Werkstatt** an Straßen bauen. Kuriere liefern Baumaterial; nach vollständiger Anlieferung fährt ein freies Baufahrzeug zur Baustelle. Fertige Betriebe erhalten Rohstoffe und erzeugen Metall beziehungsweise Bauteile. Diese Grundkette benötigt zehn der anfänglich zwölf Arbeitskräfte.
4. Ein **Wohnhabitat** schafft zusätzlichen Wohnraum. Bei ausreichender Nahrung wächst die Bevölkerung; damit werden weitere Arbeitskräfte verfügbar. Die Kolonieanzeige zeigt Wohnraum, Nahrungserzeugung und Bedarf sowie Energie und Arbeit. Auch die zweite Kolonie benötigt ihre eigene Versorgung.
5. Gebäude anklicken: Der Reiter **Auswahl** zeigt Engpässe, Bestand, ankommende Ware und Lagerkapazität. Bei vollen Produktionslagern zusätzliche **Lager** bauen; fehlende Energie mit Solaranlagen ergänzen. **Betrieb pausieren / fortsetzen** und die Prioritäten **Niedrig / Normal / Hoch** steuern Betrieb und Versorgung.
6. Im Reiter **Bauen** mit **Entfernen** neben **Straße bauen** einen Straßenabschnitt entfernen; mit **Straße bauen** die Verbindung reparieren. Unterbrochene Transporte behalten ihre Fracht und warten oder nutzen einen vorhandenen Umweg.

In **Auswahl** lassen sich unvollständige Bauaufträge abbrechen und fertige Gebäude zurückbauen. Die angezeigte Rückgewinnung bleibt vor Ort, bis Kuriere sie abholen; dafür sind Straße und freier Lagerplatz nötig. **Für Rückfracht freigehalten** bezeichnet reservierten Platz für eine mögliche Rücklieferung.

| Eingabe | Funktion |
|---|---|
| Linksklick | Planet auswählen, Bauauftrag erteilen oder Gebäude auswählen; Doppelklick/Oberflächenaktion startet die Kamerafahrt |
| Kurzer Rechtsklick | Auswahl bewegen oder sichtbaren Kriegsgegner angreifen; aktiven Platzierungs-/Zielmodus abbrechen |
| Strg + Rechtsklick | Objekt-/Zielmenü öffnen; keine automatische Aktion |
| Rechts ziehen | Kamera verschieben, ohne einen Befehl auszulösen |
| Escape | Menü schließen oder aktiven Werkzeugmodus abbrechen |
| Leertaste | Gemeinsame Simulation pausieren oder fortsetzen |
| Tab | Von der Oberfläche zur Systemansicht wechseln |
| WASD oder Pfeiltasten | Oberflächenkamera verschieben |
| Mausrad | Zoomen |
| F5 / F9 | Speichern / Laden |
| Reiter Bauen / Auswahl | Zwischen Bauwerkzeugen und Gebäudeinformationen wechseln |

Die obere Leiste bietet zusätzlich Pause, Spieltempo **1× / 2× / 4×**, Speichern und Laden. Während Pause können Bau-, Wege- und Betriebsaufträge vorgemerkt werden; sie werden nach Fortsetzen ausgeführt. Alle Kolonien arbeiten unabhängig von der geöffneten Ansicht weiter.

## Aktueller Stand

- **M0 abgeschlossen:** Unity importiert das Projekt; Vorbereitung und Windows-Mono-Build enden erfolgreich.
- **Früherer 1.0.18-Stand:** 177 Core-Tests bestanden einschließlich Baufahrzeugen, Kampfereignissen, F05 und Kooperationssieg. Danach sind 7 neue Bodenverkehrstests sowie 11 neue und 11 bestehende Raumtests bestanden; aktuelles Speicherformat 13. Der erneute vollständige Core-Lauf steht aus.
- **Lokale Grafikprüfung bestanden:** Windows-Build, vollständiger 1440×900-Lauf und spätere gezielte 1280×720-Läufe sind [chronologisch dokumentiert](docs/GUI_BEWEGUNG_BAU_NACHWEIS.md#prüfstand). Alle liefen auf dem isolierten Windows-Desktop ohne Screenreader oder Computer-Use. Die neuesten Verkehrs-, Direktbefehls-, Fokus- und Texturänderungen sind noch nicht durch einen erneuten Unity-Lauf belegt; Installer und Git-Abschluss stehen aus.
- **Versionswerkzeuge geprüft:** 12 Tests mit 19 isolierten Fixtures, einschließlich Rücknahme eines fehlgeschlagenen Schreibvorgangs. Produktversion, Playeranzeige und Versionsbranch sind synchron.
- **Reguläre Referenzpartie:** drei gleiche endliche Startkolonien, zwei umkämpfte Monde und zwei wirtschaftlich eigenständige KI; neu erzeugte reguläre Oberflächen mit 40 × 32 Waben. Vier Kuriere je Kolonie, davon höchstens zwei gleichzeitig im Baueinsatz. Prüfkarten bleiben getrennt.
- **Funktionsstand:** M0–M9 und ihre historischen Nachweise bleiben dokumentiert. 1.0.18 erweitert Darstellung und Konstruktion innerhalb dieser Pakete; der neue [Nachweis](docs/GUI_BEWEGUNG_BAU_NACHWEIS.md) führt offene Abnahme- und Integrationsschritte. Alte Spielstände müssen während dieser Entwicklung laut aktueller Nutzervorgabe nicht kompatibel bleiben; vorhandene Dateien werden nicht gelöscht.
- **Plattformstatus:** Windows-Player und programmatische Eingabeadapter sind geprüft. Weitere Desktop-, Web-, Mobil- und Konsolenexporte sowie eine vollständige manuelle Bedienungsabnahme sind ungeprüft. Der kleine Prototyp liefert noch keine Performancegarantie für das vollständige Spiel.
