# PlanetOne

PlanetOne ist ein Einzelspieler-Echtzeitstrategiespiel mit planetarem Aufbau, physischer Warenlogistik, Raumgefechten, steuerbaren Bodeninvasionen, vorbereiteten Verteidigungsstellungen und diplomatisch handelnden Computerfraktionen.

Der erste Prototyp läuft mit **Unity 6000.3.29f1 und C#** als Windows-Anwendung. Er verbindet ein Sonnensystem mit bebaubaren Kolonieoberflächen über eine gemeinsame Simulation. Die Gestaltung verbindet klare Geometrie mit texturierten Oberflächen und dunklem Weltraum. Einrichtung und geprüfte Start- und Buildschritte stehen im [Entscheidungsstand](docs/DECISIONS.md).

## Drei Layouts und Texturen in 1.0.9

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
3. Eine **Erzmine** auf das bereits angeschlossene Erzfeld setzen, anschließend **Schmelze** und **Werkstatt** an Straßen bauen. Kuriere liefern Baumaterial und Rohstoffe; daraus entstehen Metall und Bauteile. Diese Grundkette benötigt zehn der anfänglich zwölf Arbeitskräfte.
4. Ein **Wohnhabitat** schafft zusätzlichen Wohnraum. Bei ausreichender Nahrung wächst die Bevölkerung; damit werden weitere Arbeitskräfte verfügbar. Die Kolonieanzeige zeigt Wohnraum, Nahrungserzeugung und Bedarf sowie Energie und Arbeit. Auch die zweite Kolonie benötigt ihre eigene Versorgung.
5. Gebäude anklicken: Der Reiter **Auswahl** zeigt Engpässe, Bestand, ankommende Ware und Lagerkapazität. Bei vollen Produktionslagern zusätzliche **Lager** bauen; fehlende Energie mit Solaranlagen ergänzen. **Betrieb pausieren / fortsetzen** und die Prioritäten **Niedrig / Normal / Hoch** steuern Betrieb und Versorgung.
6. Im Reiter **Bauen** mit **Entfernen** neben **Straße bauen** einen Straßenabschnitt entfernen; mit **Straße bauen** die Verbindung reparieren. Unterbrochene Transporte behalten ihre Fracht und warten oder nutzen einen vorhandenen Umweg.

In **Auswahl** lassen sich unvollständige Bauaufträge abbrechen und fertige Gebäude zurückbauen. Die angezeigte Rückgewinnung bleibt vor Ort, bis Kuriere sie abholen; dafür sind Straße und freier Lagerplatz nötig. **Für Rückfracht freigehalten** bezeichnet reservierten Platz für eine mögliche Rücklieferung.

| Eingabe | Funktion |
|---|---|
| Linksklick | Planet öffnen, Bauauftrag erteilen oder Gebäude auswählen |
| Rechtsklick oder Esc | Platzierungsmodus beenden |
| Leertaste | Gemeinsame Simulation pausieren oder fortsetzen |
| Tab | Von der Oberfläche zur Systemansicht wechseln |
| WASD oder Pfeiltasten | Oberflächenkamera verschieben |
| Mausrad | Zoomen |
| F5 / F9 | Speichern / Laden |
| Reiter Bauen / Auswahl | Zwischen Bauwerkzeugen und Gebäudeinformationen wechseln |

Die obere Leiste bietet zusätzlich Pause, Spieltempo **1× / 2×**, Speichern und Laden. Während Pause können Bau-, Wege- und Betriebsaufträge vorgemerkt werden; sie werden nach Fortsetzen ausgeführt. Beide Kolonien arbeiten unabhängig von der geöffneten Ansicht weiter.

## Aktueller Stand

- **M0 abgeschlossen:** Unity importiert das Projekt; Vorbereitung und Windows-Mono-Build enden erfolgreich.
- **126 Core-Tests bestanden:** Wirtschaft, Hexraster, M3–M7 einschließlich Rechte, Vertragsfracht, gemeinsame Operationen, Sieg und Speicherformat 8 mit Migration aus V1–V7.
- **Grafischer Integrationstest bestanden:** 131 Einzelprüfungen und 18 erzeugte Renderbilder; neue M7-Weltansichten kontrolliert. Frühere native Bediennachweise für M3/M4 bleiben historisch; manuelle M7-IMGUI-Abnahme offen. Aktuelle Tests ohne Eingriffe in den Arbeitsdesktop: [M7-Nachweis](docs/M7_NACHWEIS.md).
- **Versionswerkzeuge geprüft:** 12 Tests mit 19 isolierten Fixtures, einschließlich Rücknahme eines fehlgeschlagenen Schreibvorgangs. Produktversion, Playeranzeige und Versionsbranch sind synchron.
- **Vorläufiges Testszenario:** zwei eigene Kolonien und ein neutraler Mond; je Oberfläche 18 × 14 Felder und vier Kuriere pro Kolonie.
- **Funktionsstand:** M1/M2 technisch abgenommen nach [T-006](docs/DECISIONS.md); M3 und M4 einschließlich gezielter echter Bedienprüfung abgeschlossen. Die vollständige ältere Wirtschaftsbedienungsabnahme bleibt separat offen. M5 ist implementiert und über PR #5 integriert. M6 (1.0.7) und M7 (1.0.8) sind lokal umgesetzt und automatisiert geprüft. Erneute manuelle Abnahme und Git-Integration bleiben offen; M8 folgt danach.
- **Plattformstatus:** Windows-Player und programmatische Eingabeadapter sind geprüft. Weitere Desktop-, Web-, Mobil- und Konsolenexporte sowie eine vollständige manuelle Bedienungsabnahme sind ungeprüft. Der kleine Prototyp liefert noch keine Performancegarantie für das vollständige Spiel.
