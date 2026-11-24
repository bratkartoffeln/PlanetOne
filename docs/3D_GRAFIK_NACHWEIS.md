# Räumliche Grafik und natürliches Gelände – 1.0.10

Nutzerauftrag D-029 vom 24. November 2026: die Grafik der ausgewählten Konzepttafel bildgetreu umsetzen, natürliche Berge/Täler ergänzen und Gebäude, Fahrzeuge, Schiffe und Weltobjekte vollständig dreidimensional darstellen. Die drei Layouts bleiben in den Einstellungen wählbar. Regeln GUI-003 bis GUI-006, Testfall TEST-025. Dies ist eine Präsentationslieferung auf M7, kein Abschluss von M8.

## Umgesetzte Bedienung

| Eingabe | Funktion |
|---|---|
| Q / E | Kamera um die aktuelle Welt drehen |
| Mittlere Maustaste ziehen | Blickrichtung und Neigung ändern |
| Pos1 | Standardblickrichtung wiederherstellen |
| WASD | Kamera relativ zur Blickrichtung verschieben |
| Mausrad | Abstandsausschnitt vergrößern/verkleinern |
| Einstellungen → Darstellung | Layout A, B oder C, Wabengrenzen und GUI-Größe wählen |

Kamera-Neigung ist auf 30° bis 80° begrenzt; Drehung ist rundum möglich, in Oberfläche und Systemansicht. Kamera, Layout und Blickrichtung steuern keine Simulationsregeln. Rechtsklick für Einheitenbefehle bleibt erhalten; geöffnete Menüs sperren Kamerabefehle und Weltbedienung.

## Modelle und Landschaft

Alle 24 Gebäudetypen bestehen aus räumlichen Körpern. Die Zentrale besitzt radiale Module, Turm und Antenne; Farmen transparente Glaskuppeln mit Streben und Pflanzen; Solaranlagen geneigte Module auf Stützen. Industrie besitzt Tanks und Rohrleitungen, der Hafen eine Landefläche und Hangartor. Türme, Tore, Mauern und Sprungtor sind aus mehreren Blickwinkeln sichtbar. Vorschaubilder werden aus denselben Modellen gerendert.

Infanterie, Panzer, Artillerie, Flak, Pioniere und Gütertransporter erhalten eigene Aufbauten. Geschlossene abgeschrägte Rümpfe, Räder/Laufwerke, Türme und Werkzeuge ersetzen die bisherigen einfachen Marker. Raumfahrzeuge besitzen dreidimensionale Rümpfe, Brücken, Antriebe und klassenabhängige Fracht-, Sensor-, Waffen-, Kolonie- beziehungsweise Werftaufbauten. Fraktionsmarkierungen bleiben erhalten. Fahrzeuge und Schiffe drehen in die tatsächliche Bewegungsrichtung. Bodenfahrzeuge passen auch ihre Neigung an den Untergrund an; Stillstand erhält ihre letzte Fahrtrichtung.

Die Landschaft ist ein durchgehendes Mesh mit deterministischen, unregelmäßigen Höhenzügen und Senken. Mehrere Materialmaßstäbe und steilheitsabhängige Färbung reduzieren sichtbare Wiederholung. Kantige Felsgruppen ersetzen kugelförmige Felsmarker. Straßen bilden schmale Verbindungen über das Relief; Wabengrenzen, Bauvorschau und Auswahl folgen der Höhe. Gebäude stehen auf waagerechten Fundamenten mit Stützen. Der Terrain-MeshCollider liefert die vorderste anklickbare Oberfläche.

Das Relief ändert keine Core-Regel. Bisherige Hexwege, Sperrfelder, Sichtweiten, Waren und Besitz bleiben erhalten; es gibt keine neuen Höhenboni oder zusätzlichen Baukosten. Savegame-Schema bleibt 8. Aussehen wird anhand Weltkennung und Abmessungen reproduziert und nicht als zweite Simulationswelt gespeichert.

Die GUI verwendet eine schmale Ressourcenzeile, gerahmte Metallpaneele, echte Modellporträts und eine Reliefkarte beziehungsweise Orbitalringe im Radar. B behält das breite Befehlsdeck und die linke Navigation; C bietet ein zentriertes schwebendes Dock. Ressourcenanzeigen lesen nur eigene Gebäudeinventare.

## Reproduzierbare Prüfungen

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1 -Width 1280 -Height 720
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
```

Der GUI-Test startet einen regulären Player auf einem separaten Windows-Desktop, ohne dorthin umzuschalten oder globale Maus-/Tastatureingaben zu senden. Die Screenshots enthalten die echte GUI. Der Batchtest rendert nur die Welt. Kein Screenreader, Computer-Use-Skill oder sichtbarer Unity-Editor. Es werden keine vorhandenen VMs gestartet oder verändert.

Die Prüfungen vergleichen Auswahl sichtbarer Wabenzentren und -kanten, drehen die Kamera in vier Richtungen, prüfen unterschiedliche Geländehöhen und Wiederaufbau, kontrollieren die tatsächlichen Transform-Ausrichtungen von bewegtem Panzer und Kolonieschiff und vergleichen den vollständigen Spielstand vor/nach Kameradrehungen. Alle bisherigen Wirtschafts-, Kampf-, Invasions-, KI-, Diplomatie- und Save/Load-Prüfungen bleiben erhalten.

126 Core-Tests, Unity-6000.3.29f1-Windows-Build und Versionsprüfung für 1.0.10 bestanden. Der endgültige Player bestand **176 Einzelprüfungen** je Lauf (177 PASS-Zeilen mit Gesamtergebnis):

| Lauf | Ausgabeordner |
|---|---|
| GUI, 1440 × 900 | `TestResults/Presentation/20261124-163154` |
| GUI, 1280 × 720 | `TestResults/Presentation/20261124-163235` |
| Fensterloser GPU-Batchtest | `TestResults/Smoke/20261124-163326` |

Je Lauf wurden 55 PNGs erzeugt, darunter die sechs Layoutansichten, Einstellungen, 24 Gebäudeporträts und neue Bilder aus vier Geländerichtungen sowie räumlicher Fahrzeug-/Flottenansicht. Die endgültigen GUI-Bilder für gedrehte Kolonie, Militär, Raumfahrt, Brückenlayout, freie Ansicht und Einstellungen wurden kontrolliert; keine GUI-/Shaderfehler oder Exceptions in den finalen Logs.

Bei der Entwicklung korrigiert: flache Auswahlrahmen, falsche Sichtbarkeit hinter Geländerücken, unterbrochene Straßenstreifen, zu schmale Navigationsschaltflächen und versehentliche Beleuchtung/Schatten auf dem Weltraumhintergrund. Der Endlauf prüft 113 sichtbare Wabenzentren und 636 Kantenpunkte in der Standardkamera; gedrehte Kameras prüfen 112/120/113/117 sichtbare Zentren. Verdeckte Zellen werden ausdrücklich nicht als anklickbar behauptet.

Version 1.0.10 baut auf dem noch offenen 1.0.9-Branch auf. `main` stand beim Abgleich weiterhin auf M5; der PR gegen `main` umfasst deshalb auch die vorherigen offenen Hexraster-, M6-, M7- und GUI-Lieferungen. Die bereits bekannte fehlende Unity-Aktivierung der GitHub-CI wird durch den lokalen Build nicht ersetzt; der aktuelle PR-Check wird separat ausgewiesen.

## Abnahmegrenzen

Die räumlichen Modelle und Materialien sind eigene Echtzeitumsetzungen. **Eine pixelgleiche Übereinstimmung mit `GUI_REFERENCE.png` ist noch nicht erreicht beziehungsweise abgenommen.** Insbesondere Detailgrad der Modelle, Beleuchtung, Landschaftskomposition und einzelne Abstände der GUI weichen weiterhin von der Konzepttafel ab. Die Referenz wird deshalb nicht als tatsächliches Spielbild ausgegeben; bestandene technische Tests belegen keine vollständige visuelle Gleichheit.

Die automatisierten Controllerabläufe und die Kontrolle der Playerbilder ersetzen keinen vollständigen manuellen Klicktest. Andere Plattformen, High-DPI/Mehrmonitorbetrieb und das M8-Lastprofil bleiben ungeprüft. Ein fehlgeschlagener Unity-CI-Check wird nicht umgangen; lokaler Nachweis und Git-Integration werden getrennt ausgewiesen.
