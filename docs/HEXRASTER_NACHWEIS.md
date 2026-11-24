# Wabenoberflächen in Version 1.0.6

Stand: 24. November 2026. Nutzerauftrag: Bodenfelder projektweit als Sechsecke statt Schachbrett. Dies ist eine Änderung der gemeinsamen Oberflächengeometrie für M1–M5, kein Vorziehen von M6.

## Regeln und Umsetzung

- SURFACE-001/005, COMBAT und INV-003: regelmäßige Sechsecke mit Spitze nach oben und versetzten ungeraden Zeilen (odd-r). Jedes innere Feld hat sechs gleichberechtigte Nachbarn. Der Abstand benachbarter Mittelpunkte beträgt eine Welt-/Reichweiteneinheit.
- `HexGrid` im Unity-unabhängigen Kern definiert Nachbarn, Entfernungen, Feldwahl und Landezone. Straßenlogistik, Bauanschluss, Truppenwege, Rekrutierungsausgänge, Deckung und Wegvalidierung nutzen diese Nachbarschaft. Sichtstrahlen und Bodenreichweiten verwenden die tatsächliche Wabengeometrie. Raumkoordinaten bleiben kartesisch.
- Gelände, Straßen, Erzgrund, Fundamente, Bauvorschau, Auswahl und Landekopf sind sechseckig. Gebäude, bewegte Kuriere, Einheiten, Zielanzeigen, Sichtungen und Schilde verwenden dieselbe Koordinatenabbildung. Die Kameraauswahl berücksichtigt schräge Wabenkanten.
- Landefläche: eine zentrale Wabe und ihre sechs Nachbarn, weiterhin vier Truppenplätze. Alle sieben Felder müssen frei, begehbar, auf der Karte und außerhalb aktiver feindlicher Schilde sein. Reservierungen dürfen sich nicht überschneiden. Die reservierten Waben werden während Abstieg, Landung und Aufstieg markiert.

## Spielstände

Speicherformat 6 migriert Versionen 1–5 über die bestehende Kette. Feldadressen bleiben Spalte/Zeile; Gelände, Vorkommen, Bauwerke, Waren, Kennungen, Befehle und Fraktionen werden nicht verschoben oder neu erzeugt. Kontinuierliche Bodenpositionen interpolieren den Zeilenversatz. Jeder frühere horizontale oder vertikale Wegabschnitt ist weiterhin eine gültige Hexkante, daher bleiben Wege und bereits zurückgelegte Strecken erhalten.

Ein alter gelandeter Träger darf auch an einem früher gültigen Kartenrand geladen werden und wieder starten. Neue beziehungsweise noch nicht abgeschlossene Landungen werden gegen die vollständige neue Landezone geprüft; ein nun versperrter Abstieg wird regulär abgebrochen. Neue Wegwahl, Reichweiten und taktische Fortsetzung folgen nach der Migration bewusst den Hexregeln.

`Tests/Core/Fixtures/invasion-v5.json` ist der unveränderte tatsächliche 1.0.5-Spielstand aus `TestResults/Smoke/20261124-091107/smoke-save.json`. Die Prüfung vergleicht den gesamten serialisierten Zustand: außer der Schemaversion verändert die Migration nichts. Eine weitere Speicher-Lade-Fortsetzung bleibt identisch.

## Reproduzierbare Prüfung

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1
```

- 82 Core-Tests bestanden, einschließlich acht neuer Hexprüfungen: Feldzentren und beide Seiten aller sechs Kanten, symmetrische Nachbarschaft und gleiche Geschwindigkeit, Bewegung mit Save/Load, schräge Straßenanbindung mit tatsächlichem Warenbau, Deckung/Sicht, Landereservierungen, Wegvalidierung und echter V5-Spielstand.
- Windows-Mono-Build mit Unity 6000.3.29f1 erfolgreich. Produktversion 1.0.6, Speicherformat 6.
- Rendernachweis: `TestResults/Smoke/20261124-111617`; Kolonie- und Invasionsbilder als Dateien kontrolliert.
- Player-Integration im fensterlosen `-batchmode` mit GPU: 104 Einzelprüfungen und 14 Renderbilder. Kameraauswahl prüft alle 252 Feldzentren und ihre sechs Kanten durch Projektion und Rückprojektion über denselben Codepfad wie der Mauszeiger.
- Der Test rendert die echte Szene über `Camera.Render` in eine `RenderTexture`. Kein sichtbares Fenster, keine Desktop-Eingaben, kein Screenreader, kein Computer-Use-Skill und keine VM erforderlich. Normaler Spielstand und Testspeicher sind getrennt. Dies ersetzt den bisher sichtbaren Standardtest; `-Visible` bleibt eine ausdrückliche Option für isolierte Testdesktops.
- Grenze: Die fensterlosen Bilder enthalten die Spielwelt, keine IMGUI-Bedienoberfläche. Eine erneute manuelle Bedienungs-/Beschriftungsabnahme ist nicht erfolgt; andere Plattformen sind ungeprüft. Die Tests belegen keine bitgleiche taktische Fortsetzung nach einem Wechsel von Quadrat- auf Hexregeln.

Unity beschreibt `-batchmode` als Betrieb ohne Anzeige und Benutzereingaben; `-nographics` würde dagegen das für RenderTexture benötigte Grafikgerät deaktivieren: [Player-Kommandozeilenreferenz 6000.3](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/command-line-arguments/player).
