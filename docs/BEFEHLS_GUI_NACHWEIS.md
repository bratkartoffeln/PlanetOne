# Befehlsoberfläche und Kontextmenüs – 1.0.16

Auftrag vom 25. November 2026: Befehls-GUI übersichtlicher gestalten, vorher recherchieren und Kontextmenüs direkt an angeklickten oder aktiven Elementen anbieten. Betrifft P20, GUI-012, TEST-032, D-036 und T-055/056.

## Recherche vor der Umsetzung

- [NN/g: Designing Effective Contextual Menus](https://www.nngroup.com/articles/contextual-menus-guidelines/): Kontextmenüs sparen Fläche und Wege, sind aber schlechter auffindbar. Daraus folgen ein sichtbarer Einstieg, ein benanntes Ziel, beschriftete Aktionen und sichtbare Gründe für gesperrte Aktionen. Häufige Befehle bleiben zusätzlich direkt erreichbar.
- [NN/g: Progressive Disclosure](https://www.nngroup.com/articles/progressive-disclosure/): Häufige Aufgaben zuerst, Spezialfunktionen bei Bedarf öffnen. Daraus folgen kompakte Auswahlkarten und getrennte Bereiche für Schiffsauswahl, Aufträge, Schiffbau und Abbau.
- [EA: Command & Conquer Remastered – Seitenleiste](https://help.ea.com/fi/articles/command-and-conquer/command-and-conquer-remastered/how-to-play/): Getrennte Bau-/Einheitenbereiche und ausdrücklich abbrechbare Werkzeugmodi dienen als RTS-Referenz. PlanetOne behält eigene Assets und seine drei Layouts.

Die Recherche fand vor den Codeänderungen statt. Dies ist eine begründete Gestaltungsentscheidung, keine Nutzerstudie. Eine vertikale Liste wurde gegenüber einem Radialmenü gewählt, weil lange deutsche Befehlsnamen zusammen mit kurzen Erklärungen lesbar bleiben.

## Bedienung

- **Linksklick:** Objekt auswählen. Unten erscheinen bis zu drei passende Hauptaktionen; **Alle Befehle …** und **Objektbefehle öffnen** machen das Kontextmenü auch ohne Rechtsklick erreichbar.
- **Kurzer Rechtsklick:** Befehle am angeklickten Objekt/Ziel öffnen. Das Öffnen erteilt keinen Spielbefehl. Mit gewählter Flotte erscheinen an einer Welt Flug-/Orbitaktionen; am Gelände beziehen sich Bewegungsaktionen auf die ausgewählten Bodentruppen.
- **Rechts halten und ziehen:** Kamera verschieben. Ab sechs Pixeln Bewegung entsteht weder Menü noch Einheitenbefehl.
- **Bewegen / Flugziel wählen:** Danach mit links direkt das Ziel wählen. Escape oder kurzer Rechtsklick beendet einen aktiven Ziel-/Baumodus.
- **Schließen:** Escape, Schließen-Schaltfläche oder Klick daneben. Der Außenklick wird verbraucht und löst keine Aktion in der darunterliegenden Spielwelt aus. Pfeiltasten und Enter bedienen die Menüliste.
- **Gebäude:** Betrieb, Priorität, Details/Lager und passende Produktion. Rückbau/Bauabbruch zeigt zuerst eine Bestätigung samt Rückgewinnung. Ausgewählte Pioniere können ein angeklicktes eigenes Gebäude reparieren; ein Depot bietet Nachschub für die Truppenauswahl.
- **Bodentruppen:** Bewegung, Halten, Angriff, Rückzug und Versorgung. Reparatur wird für Pioniere, Positionsfeuer für Artillerie angeboten.
- **Flotte:** Separate Reiter **Schiffe**, **Aufträge**, **Schiffbau**, **Abbau**. Ein Raumhafen öffnet unmittelbar seinen Schiffbau oder seine Frachtregeln. Stern und Asteroid bieten ihre jeweiligen Informationen/Aktionen.

Alle drei Layouts teilen dieselben Aktionen. Menüs halten Welt, Objektkennung und handelnde Einheiten fest. Sicht, Besitz und Existenz werden vor Ausführung erneut geprüft. Auswahl-/Ansichtswechsel und modale Dialoge schließen das Menü. Es bleibt innerhalb des Fensters, scrollt bei Platzmangel und folgt nicht dauerhaft dem Mauszeiger.

## Grenzen und Prüfung

Befehle laufen weiterhin durch die gemeinsame Core-Schnittstelle. Regeln, Kosten und Speicherformat 11 bleiben unverändert. Verfügbarkeitsanzeigen ersetzen keine Core-Validierung: Der laufende Spielzustand kann einen zuvor angebotenen Auftrag bei der Ausführung ablehnen. Die Rückbau-Bestätigung ist eine Bedienhandlung im Spiel.

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1 -Width 1280 -Height 720 -CommandUiOnly
```

`-CommandUiOnly` führt die gezielten Befehls-/Gestenprüfungen aus. Der vollständige Aufruf enthält diese zusätzlich zu den bisherigen Szenarien. Die Tests verwenden einen getrennten, niemals aktivierten Windows-Desktop; keinen Screenreader, Computer-Use oder globale Eingaben. Physische Bedienprobe und Nutzerstudie werden daraus nicht abgeleitet.

## Tatsächlicher Nachweis

- **161 Core-Tests bestanden**, einschließlich der vollständigen Referenzverläufe; `TestResults/commands-core.log` und `TestResults/Core/results.txt`.
- **Versionsprüfung 1.0.16 und Unity-Windows-Build bestanden**, `TestResults/commands-build.log` und `Logs/Unity-Build.log`.
- **1440×900, vollständiger Lauf:** `TestResults/Presentation/20261125-140248`, **476 Einzelbedingungen plus Gesamt-PASS**, 107 PNGs. Enthält alle drei Layouts und die bisherigen Spielabläufe sowie die neuen Befehlsprüfungen.
- **1280×720, gezielter Befehlslauf:** `TestResults/Presentation/20261125-140415`, **78 Einzelbedingungen plus Gesamt-PASS**, elf PNGs. Enthält alle drei Layouts, große Skalierung und die getrennten Flotten-/Produktionsbereiche; dies ist kein zweiter vollständiger Gesamtszenariolauf.
- Keine Exceptions, GUI- oder Shaderfehler in diesen beiden finalen Playerlogs. Die absichtliche Prüfung beschädigter Speicherstände erzeugt im vollständigen Lauf eine erwartete Warnmeldung.

Die Prüfungen zeigen unter anderem: Menüs ohne implizite Spielbefehle; unveränderte Simulation beim Öffnen; blockierte Kamera-/Welteingabe; Priorität am festgehaltenen Gebäude; Eigentumswechsel vor Ausführung; Abbruch und echter bestätigter Rückbau; korrekte Zuordnung von Pionier und angeklicktem Gebäude; genau ein ausdrücklicher Flug zur angeklickten Welt mit tatsächlicher Ausführung; kein automatischer Schiffbau beim Öffnen der Produktion. Bestehende Zieh-/Abbruchprüfungen sind an das neue Rechtsklickverhalten angepasst.

Visuell kontrollierte Belege: [Planetenbefehle für eine gewählte Flotte](BEFEHLE_1_0_16/planet-befehle.png), [Einheitenmenü bei 1280×720](BEFEHLE_1_0_16/einheiten-1280.png), [getrennte Flottenaufträge](BEFEHLE_1_0_16/flottenauftraege.png). Zusätzlich wurden Gebäude-/Produktionsbilder der finalen Läufe kontrolliert.

## Windows-Paket und Git

Implementierungscommit **`01a870ab72cd6c77cbc6e07d0c19883fba7ec205`** ist die im Paketmanifest festgehaltene Quelle. Nachfolgende Dokumentationscommits ändern den Player nicht.

```powershell
powershell -ExecutionPolicy Bypass -File tools/Package-Windows.ps1 -CompilerPath TestResults/Toolchains/InnoSetup-6.4.3/ISCC.exe
powershell -ExecutionPolicy Bypass -File tools/Test-Installer.ps1
```

Beide Aufrufe bestanden. Protokolle: `TestResults/commands-package.log` und `TestResults/commands-installer.log`. ZIP und installierte Dateien sind identisch; Installation und Deinstallation in einem eigenen Projekt-Testverzeichnis erfolgten ohne Registrierung/Verknüpfungen. Der Playerstart wurde in den oben genannten GUI-Läufen geprüft, nicht ein weiteres Mal aus der Testinstallation gestartet.

| Ausgabe unter `Builds/Distribution` | Bytes | SHA256 |
|---|---:|---|
| `PlanetOne-1.0.16-Windows-x64-Setup.exe` | 28.027.200 | `2e97004183e6a6197c8027a369e43a5ad79297d8018ae74cd68a08a44e4c7b63` |
| `PlanetOne-1.0.16-Windows-x64.zip` | 37.573.988 | `84d3a407c60db3bc577956409c4c52da1771224a2045e04bf60ce93c729501dc` |

Versionsbranch **1.0.16** baut auf 1.0.15 auf. `origin/main` war bei der abschließenden Prüfung weiterhin 1.0.12 (`ece80b4`). Git-Integration hängt vom erforderlichen Unity-Check ab; dessen Aktivierungssecrets fehlten bei den vorigen PRs. Ein lokaler erfolgreicher Build ersetzt den GitHub-Pflichtcheck nicht. Der PR hält das tatsächliche CI-/Merge-Ergebnis fest. Kein GitHub-Release wurde veröffentlicht.
