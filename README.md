# PlanetOne

PlanetOne ist ein Einzelspieler-Echtzeitstrategiespiel mit planetarem Aufbau, physischer Warenlogistik, Raumgefechten, steuerbaren Bodeninvasionen, vorbereiteten Verteidigungsstellungen und diplomatisch handelnden Computerfraktionen.

Der erste Prototyp läuft mit **Unity 6000.3.29f1 und C#** als Windows-Anwendung. Er verbindet ein Sonnensystem mit bebaubaren Kolonieoberflächen über eine gemeinsame Simulation. Die Gestaltung ist minimalistisch: klare geometrische Formen, dunkler Weltraum und wenige Akzentfarben. Einrichtung und geprüfte Start- und Buildschritte stehen im [Entscheidungsstand](docs/DECISIONS.md).

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

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Test-Core.ps1
powershell -ExecutionPolicy Bypass -File .\tools\Test-Prototype.ps1 -Visible
```

Der Core-Test nutzt Roslyn aus Visual Studio Build Tools; dafür ist kein dotnet-SDK erforderlich. Der grafische Test startet den gebauten Player in einem sichtbaren Fenster und schreibt Prüfprotokoll sowie elf Renderbilder nach `TestResults/Smoke/<Zeitstempel>`. `-Visible` ist erforderlich, da der Grafikkontext des versteckten Fensters den Bildnachweis auf dieser Umgebung nicht zuverlässig liefert.

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
- **58 Core-Tests bestanden:** Wirtschaft und M3 sowie Schiffsbau, Erkundung, Gründung, Frachten, Reservierungen, Forschung, Kristall-/Elektronikkette und Speicherformat 4 mit Migration aus V1/V2/V3.
- **Grafischer Integrationstest bestanden:** 91 Einzelprüfungen und elf visuell kontrollierte Renderbilder. M3 und M4 wurden zusätzlich gezielt mit echten Maus-/Tastatureingaben geprüft; siehe [M3-Nachweis](docs/M3_NACHWEIS.md) und [M4-Nachweis](docs/M4_NACHWEIS.md).
- **Versionswerkzeuge geprüft:** 12 Tests mit 19 isolierten Fixtures, einschließlich Rücknahme eines fehlgeschlagenen Schreibvorgangs. Produktversion, Playeranzeige und Versionsbranch sind synchron.
- **Vorläufiges Testszenario:** zwei eigene Kolonien und ein neutraler Mond; je Oberfläche 18 × 14 Felder und vier Kuriere pro Kolonie.
- **Funktionsstand:** M1/M2 technisch abgenommen nach [T-006](docs/DECISIONS.md); M3 und M4 einschließlich gezielter echter Bedienprüfung abgeschlossen. Die vollständige ältere Wirtschaftsbedienungsabnahme bleibt separat offen. Als Nächstes folgt M5 mit Raumkampf, Landung und Invasion; KI und Diplomatie folgen danach.
- **Plattformstatus:** Windows-Player und programmatische Eingabeadapter sind geprüft. Weitere Desktop-, Web-, Mobil- und Konsolenexporte sowie eine vollständige manuelle Bedienungsabnahme sind ungeprüft. Der kleine Prototyp liefert noch keine Performancegarantie für das vollständige Spiel.
