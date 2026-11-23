# M3 Bodengefechte und Verteidigung

Stand: 23. November 2026, Produktversion 1.0.2, Unity 6000.3.29f1, Speicherformat 3. P06 und P07 sind umgesetzt und im unten genannten Umfang geprüft. Schiffsbau, Fraktions-KI, Verträge und reguläre Landungstransporter bleiben ihren späteren Paketen zugeordnet.

## Umgesetztes Verhalten

Kasernen rekrutieren Infanterie und Panzerabwehr; Fahrzeugwerke fertigen Kampfpanzer, Artillerie, Pionier-/Versorgungsfahrzeuge und mobile Flugabwehr. Pro Produktionsstätte läuft ein Auftrag. Material gelangt über die vorhandenen Kuriere zur Anlage. Erst mit allen lokalen Eingaben und ausreichender Bevölkerung beginnt die Produktion. Einheiten entstehen nach Ablauf der Produktionszeit genau einmal an einem freien angrenzenden Ausgang. Fehlende Energie, Arbeit oder ein blockierter Ausgang halten den Auftrag an. Vier Bewohner bleiben als zivile Wiederaufbaubasis erhalten.

Bewegung verwendet vierfach verbundene Bodenwege mit Prüfung des nächsten Segments. Halten, Angriff, Rückzug, positionsgebundenes Artilleriefeuer, Nachfüllen und Reparatur sind validierte Befehle. Gelände und Gebäude beeinflussen Begehbarkeit, Sichtlinie und Deckung. Eigene Infanterie kann fertige zivile Gebäude als Deckung nutzen; dies ist keine gegnerische Gebäudebesetzung nach P13. Einheitenkollisionen und individuelle Formationstaktik sind noch vereinfacht; Verbände können sich ein Feld teilen.

Aktuelle Sicht entsteht aus eigenen Einheiten und Gebäuden. Gegner werden nur bei gültiger Sicht dargestellt und als direkte Ziele angenommen. Gespeicherte Sichtungen behalten Ort und Zeitpunkt ihrer letzten Beobachtung. Bei Sichtverlust endet die direkte Verfolgung. Artillerie darf eine bekannte Position beschießen, verfolgt dort aber keinen ungesehenen Gegner. Krieg und Eigentum werden vor Angriff beziehungsweise Steuerung geprüft. Normale automatische Zielwahl beginnt keinen Krieg.

Vier Stellungen, Mauern und Tore sind regulär baubar und einschließlich Baustellen beschädigbar. Tore lassen eigene Einheiten und ausdrücklich in Szenariodaten berechtigte friedliche Partner passieren; verhandelte Rechte folgen in P16. Ein zerstörter Mauerabschnitt öffnet den Weg. Trefferpunkte, Munition, Rationen, mitgeführte Bauteile, Aufträge, Wege, Rekrutierung und Sichtungen werden gespeichert.

Militärdepots erhalten Nahrung und Bauteile über echte Lieferungen. Einheiten fahren zum Depot, Pioniere können Bauteile aufnehmen, zum Verband fahren, Munition nachfüllen und Schäden reparieren. Die Übergabe erfordert unmittelbare Nachbarschaft. Geladene Bauteile gehören bis zu Verbrauch oder Verlust dem Pionierfahrzeug. Örtliche Rationen und Munition sind aus bezahlten Gütern erzeugte Versorgungsvorräte, keine zusätzlichen Handelswaren. Fehlende Munition verhindert Feuer; fehlende Rationen verlängern die Nachladezeit, entfernen aber keine Einheit und verhindern keinen Rückzug.

Türme verwenden vorhandene Munition und wandeln lokal gelieferte Nahrung/Bauteile in weiteren Vorrat um. Jeder Turm besitzt einen begrenzten Energiepuffer. Bei Straßentrennung kann die vorhandene Besatzung diesen örtlichen Puffer nutzen; neuer externer Nachschub und Netzausgleich fehlen. Nach Erschöpfung kann der Turm nicht mehr schießen. Struktur, Munition, Energiepuffer und Reichweite stehen im Gebäudeinspektor.

Zerstörung besitzt null Baukostenrückgewinnung. Unabgeholte Lieferreservierungen werden aufgelöst; bereits geladene Waren bleiben auf ihren tatsächlichen Kurieren und werden über den bestehenden Rückfrachtablauf behandelt. Ein Lagerrest am zerstörten Standort kann deshalb noch bis zum Abschluss dieser Vorgänge existieren, ohne weiter zu produzieren. Laufende Rekrutierung wird bei freiwilligem Rückbau in lokale Waren und Bewohner zurückgeführt; militärische Zerstörung erstattet diese nicht. Nachweise für spätere Besetzung und Reparatur zerstörter Kontrollstandorte gehören zu P13.

## Regeln und Module

| Bereich | Grundlage | Umsetzung |
|---|---|---|
| Rekrutierung und Bestände | COMBAT-003–004, ECO-004, ARCH-008–009 | `MilitaryState.cs`, `SimulationMilitary.cs`, bestehende Bau-/Liefermodule |
| Sicht, Reichweite, Deckung und Schaden | SPACE-005, COMBAT-001, COMBAT-005–007, TEST-011 | Fraktionsbezogene Sichtabfragen, letzte Sichtungen, Zielvalidierung |
| Mauern, Tore und Stellung | SURFACE-007, SURFACE-009, DEF-001–008, TEST-007 | Reguläre Gebäudedefinitionen, Bodenwege, Struktur und Zielrollen |
| Nachschub und Reparatur | LOG-009–010, COMBAT-007 | Kuriere, Depots, örtliche Vorräte, beladene Pioniere |
| Speicherung | ARCH-014, aktueller Teil TEST-020 | `SaveMilitary.cs`, Migration V1 → V2 → V3 |
| Darstellung und Eingabe | CORE-009, ARCH-002, ARCH-015 | `PrototypeMilitary.cs`, bestehender Controller |

Einheiten- und Turmwerte stehen in `Definitions.cs` und `MilitaryState.cs`. Vorläufige Balance: acht Schuss pro umgewandeltem Bauteil; Türme verwenden dafür zusätzlich eine Nahrung, maximal 24 Schuss; zehn Energieeinheiten pro Turm; 16 reparierte Strukturpunkte pro Bauteil; eine Versorgungsaktion je Sekunde; eine Ration je 30 Sekunden. Einheiten erhalten ihre anfänglichen Vorräte aus ihren ausgewiesenen Rekrutierungskosten. Dies sind abstimmbare Daten und keine endgültige Balance.

## Reproduzierbarer Start

Im Repository ausführen:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1 -Visible
& .\Builds\Windows\PlanetOne.exe
```

Für reguläre Rekrutierung in einer Kolonie Nahrung, Energie und Baumaterialwirtschaft sichern, dann Kaserne beziehungsweise Fahrzeugwerk an das Straßennetz bauen. Im Gebäudeinspektor oder unter **Militär und Einheiten** einen Auftrag wählen. Ein Militärdepot versorgt Einheiten mit weiteren Vorräten.

**Gefechtsprüfkarte:** In der Systemübersicht **M3 Prüfkarte öffnen** wählen. Die laufende Kolonie bleibt als eigene Sitzung im Speicher erhalten; **Zur laufenden Kolonie zurück** kehrt dorthin zurück. Normales Speichern verwendet `prototype-save.json`; die Prüfkarte verwendet separat `military-fixture-save.json`. Ein Neustart des Programms erhält die vorherige Koloniesitzung nur, wenn sie zuvor normal gespeichert wurde. Der automatisierte Grafiktest schreibt ausschließlich in seinen Testordner.

Die ausdrücklich beschriftete Fixture verwendet Seed 3003 und eine Oberfläche mit 18 × 14 Feldern, zwei Verteidigungslinien und zwei Zugängen. Sie startet mit 28 Bewohnern, sechs eigenen und fünf gegnerischen Einheiten sowie vorbereiteten Gebäuden. Die zentralen Anfangsgüter sind 100 Nahrung, 100 Metall und 40 Bauteile; das Depot enthält zusätzlich acht Nahrung und 24 Bauteile. Vier Türme besitzen 16/16/8/8 Schuss. Einheiten besitzen die in ihren Definitionen angegebenen Startvorräte. Diese endlichen Bestände und fertigen Anlagen sind Testdaten. Die Angriffsschaltflächen geben lediglich Bewegungsbefehle an vorhandene Gegner; sie erzeugen keine neuen Truppen und sind keine Fraktions-KI.

| Eingabe | Wirkung |
|---|---|
| Linksklick auf eigene Einheit | Einheit auswählen |
| Umschalt und Linksklick | Auswahl erweitern |
| Linke Maustaste ziehen | Eigene Einheiten im Rahmen auswählen |
| Rechtsklick | Bewegen oder sichtbare feindliche Einheit angreifen |
| H | Ausgewählten Verbänden Stellung halten befehlen |
| Strg+1 bis Strg+3 / 1 bis 3 | Kontrollgruppe speichern / auswählen |
| Militärleiste und anschließender Kartenklick | Rückzug, Zielangriff, Artillerieposition, Nachschub oder Reparatur |
| Leertaste / F5 / F9 | Pause / Speichern / Laden |

## Ausgeführte Prüfungen

**Core:** 43 bestandene Verhaltenstests in `TestResults/Core/results.txt`; davon 15 zusätzliche M3-Prüfungen in `Tests/Core/MilitaryTests.cs`. Sie prüfen unter anderem regulären Bau und angelieferte Rekrutierungskosten, Duplikatschutz, Sichtverlust, Friedensrechte, Deckung, Reichweite, Bewegung auf zwei Wegen, Rückzug, Torrechte, Artilleriedurchbruch, endlichen Nachschub, Pionierreparatur, Flugabwehr-Zielebenen, Zerstörung bei laufenden Lieferungen, Speicherfortsetzung, Migration und Rekrutierungsabbruch. Die 28 bisherigen Wirtschafts- und Persistenztests bestehen weiterhin.

**Build und Grafik:** Windows-Mono-Build erfolgreich, Exitcode 0. Der abschließende sichtbare Grafiklauf liegt unter `TestResults/Smoke/20261123-181956`: 71 bestandene Einzelprüfungen und eine zusätzliche PASS-Gesamtzeile. Sieben Renderbilder wurden visuell kontrolliert: `space.png`, `surface.png`, `surface-details.png`, `surface-restored.png`, `military-defense.png`, `military-combat.png`, `military-restored.png`. Der Ablauf prüft Reservenbewegung, beide vorbereiteten Angriffe, Schaden, Rückzug und exakte militärische Save/Load-Wiederherstellung über die normalen Controller-Schnittstellen.

**Tatsächliche Windows-Bedienung:** Im sichtbaren Player mit Computer Use und nativen Maus-/Tastatureingaben geprüft: Prüfkarte über das Menü öffnen, Panzer per Kartenklick auswählen, Bewegung per Rechtsklick in Pause vormerken, beide Angriffsschaltflächen bedienen, mit Leertaste starten und pausieren, Gefechtsschäden und Verlust beobachten, Rückzug per Schaltfläche und Kartenklick ausführen, Kontrollgruppe mit Strg+1 speichern und nach Escape mit 1 wieder auswählen, F5/F9 verwenden. Die schnelle Rahmenauswahl wurde nach einem festgestellten Fehler auf ereignisbasierte Mauspositionen umgestellt; der Wiederholungslauf wählte alle sechs eigenen Verbände aus.

Zusätzlich wurden im Wirtschaftsmenü die beiden Straßenfelder `(10,2)` und `(9,3)` am Schnellfeuerturm `(10,3)` entfernt. Vor der Unterbrechung waren lokale Vorräte und Energie geladen. Nach Fortsetzen zeigte der Inspektor zunächst **Versorgungsweg unterbrochen; örtlicher Energiepuffer**, nach dessen Ablauf **Kein Straßenanschluss**. Munition und Material blieben erhalten. Damit ist die Bedienung einer Verteidigung mit zwei Linien, zwei Angriffswegen und anschließender Versorgungsunterbrechung tatsächlich beobachtet. Eine vollständige manuelle Abnahme sämtlicher älterer Wirtschaftsfunktionen ist dadurch nicht behauptet.

## Verbleibender Umfang

Flugabwehr-Zielregeln sind automatisiert mit ausdrücklich markierten Übergangszielen geprüft. Reguläre Landungstransporter, tatsächlicher Abstieg, planetare Eroberung und Orbitalunterstützung folgen in P11–P13. Verhandelte Torrechte folgen in P16; die aktuelle Berechtigungsliste ist eine gespeicherte Szenariovorgabe. Eigenständige Gegnerwirtschaft und strategische Anpassung folgen in M6. Weitere Plattformen, große Lastprofile und die vollständige Partie sind ungeprüft. M3 verspricht keine fertige militärische Gesamtbalance.
