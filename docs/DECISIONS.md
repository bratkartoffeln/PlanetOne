# Entscheidungen und Plattformstrategie

Stand: 23. November 2026. Unity mit C# ist eingerichtet; Produktversion 1.0.1 erweitert den Windows-Prototyp um die tragfähige Koloniewirtschaft. Diese Datei unterscheidet bindende Produktvorgaben, technische Festlegungen und den tatsächlich nachgewiesenen Umfang.

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
| D-011 | Arbeitsannahme | 2,5D-Darstellung mit taktischen Bewegungen auf Ebenen | Orthografische Kamera und einfache 3D-Geometrie im Built-in-Renderer; keine frei steuerbare dritte Raumachse im Grundspiel |
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

M0 und das erste Prototyp-Goal sind abgeschlossen. In 1.0.1 ist auch M1 umgesetzt und geprüft; M2 ist fachlich umgesetzt und automatisiert nachgewiesen. Die zusätzliche manuelle Bedienungsprüfung ist beim Nutzer angefragt. P08 enthält weiterhin nur den Ansichts-/Orbitanteil. Abhängige Pakete erhalten ihren Abschlussstatus erst nach ihren eigenen Nachweisen; weitere Freigaberunden für bereits beauftragte Arbeit sind daraus nicht abzuleiten.

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
