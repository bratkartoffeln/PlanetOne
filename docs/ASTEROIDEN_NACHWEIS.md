# Systemgrößen, Sonnenkorona und Asteroidenabbau – 1.0.14

## Auftrag und Bedienung

D-033 / GUI-010 / SPACE-011 / ECO-015 / TEST-029: kleinere Planeten, Monde und Asteroiden, größere sichtbare Bahnabstände, zufällig verteilte Rohstoffgürtel und korrekt mitverschobene Korona. Diese Erweiterung baut auf 1.0.13 auf und schließt M8 nicht vorzeitig ab.

Die Schaltflächen **Diplomatische Partie** und **Gegen Computer spielen** im Spielmenü erzeugen jetzt jeweils einen neuen System-Seed. Es gibt weiterhin ein Sternsystem pro Partie. 65 Prozent der Seeds wählen einen Gürtel, sofern die Konfiguration genug freien Raum bietet. Der feste ursprüngliche Start-Seed 12345 hat keinen Gürtel; bestehende Spielstände behalten ebenfalls ihre ursprüngliche Geografie.

1. **Flotte → Asteroidenabbau** öffnen und ein Vorkommen aus der blätterbaren Liste wählen. Alternativ dessen kleine Zielmarke in der Welt anklicken. **Vorkommen ansehen** verschiebt die Kamera dorthin.
2. Schiff auswählen und **Ausgewählte Schiffe hinschicken** verwenden. Kartierte Position und Durchmesser sind öffentlich; Bestände erscheinen erst bei aktueller Sensoraufklärung.
3. Einen freien eigenen Frachter auswählen, Raumtransport erforschen und einen eigenen Raumhafen als Rücknahmehafen wählen. **Erz abbauen und heimbringen** reserviert bis zu 40 Erz und den tatsächlichen freien Lagerplatz dieses Hafens.
4. Der Frachter fliegt zum bewegten Asteroiden, gewinnt alle 1,5 Sekunden ein Erz und nimmt es an Bord. Anschließend fliegt er zum bewegten Zielplaneten und nutzt dessen betriebsfähigen Hafenanschluss für die Entladung mit 0,5 Sekunden je Erz.
5. **Abbau / Fracht abbrechen** beendet den Auftrag und gibt offene Ansprüche frei. Bereits gewonnenes Erz bleibt an Bord und nutzt die vorhandene physische Rückführungslogik.

**Spielmenü → Testszenarien → Asteroidenabbau** startet eine klar bezeichnete separate Sitzung (Seed 2). Wirtschaft, Technologie und Frachter werden durch normale kostenpflichtige Befehle aufgebaut. Die vorherige Partie bleibt über die Rückkehraktion erreichbar. Die Prüfkarte ist nicht der normale Spielstart.

## Recherche und bewusste Vereinfachungen

Im Sonnensystem liegt der Hauptasteroidengürtel zwischen Mars und Jupiter. Seine Körper reichen von kleinen Felsbrocken bis zu hunderten Kilometern Durchmesser. Das begründet die Einordnung als kleinere Körper zwischen inneren Gesteinswelten und äußerem Riesenplaneten. [NASA: Asteroids](https://science.nasa.gov/solar-system/asteroids/).

Die NASA erläutert den Hauptgürtel näherungsweise mit 2,2–3,2 AE und betont die sehr großen leeren Zwischenräume. Deshalb erscheinen hier weit voneinander entfernte Körper und zurückhaltende Kartengrenzen; kein dichter Hindernisring. Die konkreten Spielbahnen sind aus der vorhandenen Planetenkonfiguration abgeleitet: etwa **2,46–4 AE** im Ausgangssystem mit Gürtel und **3,3–4 AE** in der diplomatischen Partie mit Helios. Das sind Spielkonfigurationen, keine behaupteten Messwerte eines realen Systems. [NASA Dawn: FAQ](https://science.nasa.gov/mission/dawn/faq/).

Der Generator verwendet 32 abbaubare Repräsentanten mit 0,2–8 km Modelldurchmesser, 80–320 Erz und geringer optischer Bahnneigung. Kreisbahnen mit `T ∝ a^(3/2)` sind der Kepler-Sonderfall. Häufigkeit, Lagerstättenmenge, Abbautempo und integrierte Bergbaudrohnen sind Balance-/Technikannahmen. Im aktuellen Warenmodell liefern die Vorkommen Erz; dessen bestehende Verhüttung erzeugt Metall. Weitere Mineralarten, Wassereisgewinnung, Resonanzen, Kollisionen, Gravitation mehrerer Körper und eine autonome neue KI-Bergbaustrategie sind nicht implementiert.

## Größen und Kamera

| Körper / Darstellung | Durchmesser in Darstellungseinheiten |
|---|---:|
| Stern | 8,6 |
| Größter erlaubter Planet | 4,3 |
| Zephyr / Gasriese | 4,1 |
| Pelagia / Ozeanwelt | 1,5 |
| Aster | 1,2 |
| Nereid | 1,08 |
| Sahra / Helios | 0,984 |
| Khepri / Mond | 0,28 |
| Asteroiden, inklusive unregelmäßiger Mesh-Ausdehnung | unter 0,028 |

Alle Raumpositionen werden um Faktor **2,2** auseinandergezogen. Dazu gehören Orbitlinien, Körper, Schiffe, alte Sichtungen und Orbitalbereiche. Zielklicks rechnen denselben Faktor zurück. Physikalische AE, Klimarichtwerte und die gemeinsame Simulation ändern sich nicht durch Kamera oder Zoom. Das Mausrad zoomt proportional und erlaubt auch kleine Zielgebiete. Körpergrößen und besonders taktisch erkennbare Schiffe bleiben für Bedienbarkeit gegenüber astronomischen Entfernungen überhöht; dies ist kein durchgängig maßstabsgetreues Modell.

Der Korona-/Atmosphärenshader berechnete vorher jeden Blickvektor zur Kameraposition. Das ist nur bei perspektivischer Projektion richtig. Bei der orthografischen Hauptkamera verwendet er jetzt die parallele Blickrichtung aus der View-Matrix. Die Korona bleibt dadurch um den Stern zentriert, auch außerhalb der Bildschirmmitte und nach Rotation.

## Zustandsintegrität

`AsteroidState`, `MiningJob` und `SimulationAsteroids` gehören zum Unity-unabhängigen Core. `MineAsteroid` verwendet dieselbe Fraktionsprüfung und Befehlswarteschlange wie andere Aufträge. Abbau kann keinen gleichzeitig belegten Frachter übernehmen. Auch Handelsverträge und gemeinsame Operationen können einen laufenden Abbauauftrag nicht überschreiben.

Die Lagerstätte reserviert nur den noch nicht geförderten Teil. Der Hafen reserviert die gesamte erwartete Ladung. Die Summe aus verbleibendem Vorkommen und `SpaceMinedOre` entspricht dem ursprünglichen Vorrat. Bei Übergabe an die vorhandene `FreightJob`-Logik bleibt die Hafenreservierung erhalten. Abbruch, Schiffsverlust und Hafenverlust lösen die Reservierungen ohne Erstattung bereits geförderten Erzes auf; Ladung wird zurückgeführt oder im Verlustzähler erfasst.

Savegame-Schema **11** validiert Geometrie, Kennungen, endliche Bestände, Reservierungen, Fortschritt, reale Ladung und Hafenziele. Echte V5-, V8-, V9- und V10-Dateien werden weiter geprüft. Für ältere Spielstände werden leere Gürtel-/Asteroidenlisten angelegt; keine neuen Ressourcen, geänderten Kennungen oder verschobenen Bahnen.

## Prüfstand

Am 25. November 2026 unter Windows ausgeführt:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1 -Width 1280 -Height 720
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
```

- **149 Core-Tests bestanden**, darunter neun neue Verhaltensszenarien für Generator, Sicht/Berechtigung, konkurrierende Abbauansprüche, genaue Hafenwahl, Warenbilanz, Abbruch/Verlust, feindliche Hafenübernahme, Fortsetzung und Manipulationsabwehr sowie echte V10-Migration.
- **Unity 6000.3.29f1 Windows-Build erfolgreich**; Player unter `Builds/Windows/PlanetOne.exe`, Produktversion 1.0.14 synchron geprüft.
- **Je 354 Player-Prüfbedingungen und 86 PNGs** in GUI 1440 × 900 (`TestResults/Presentation/20261125-095640`), GUI 1280 × 720 (`TestResults/Presentation/20261125-095755`) und GPU-Batchlauf (`TestResults/Smoke/20261125-095932`). Das zusätzliche `PASS` am Anfang der Protokolle ist das Gesamtergebnis, keine weitere Prüfbedingung.
- Der Koronatest rendert das tatsächliche Koronamaterial über eine GPU-Kamera mittig sowie auf beiden Seiten, einschließlich gedrehter Blickrichtung. Die relative mittlere Abweichung zwischen gespiegelten Helligkeitsproben bleibt unter drei Prozent. Zusätzlich wurden die echten Spielbilder kontrolliert.
- Tatsächliche Mesh-Ausdehnungen aller 32 Asteroiden bleiben unter einem Zehntel des Monddurchmessers. Der Player fliegt einen Frachter zum Vorkommen, zeigt Teilfracht, lädt den laufenden Auftrag identisch wieder und weist 12 geförderte sowie vollständig zurückgelieferte Erz nach.
- Keine Exceptions, GUILayout-Fehler oder Shaderwarnungen in den drei finalen Playerlogs. Der Batchlauf zeichnet die Welt über `Camera.Render` auf; die beiden getrennten Desktopläufe enthalten auch echtes IMGUI.

Die GUI lief auf einem separaten, nie aktivierten Windows-Desktop. Kein Screenreader, Computer-Use, sichtbarer App-Start oder globaler Maus-/Tastaturzugriff. Die Prüfungen sind programmatische Integrations- und Rendernachweise mit anschließender Bildkontrolle; sie behaupten keine vollständige manuelle Bedienbarkeitsabnahme oder Leistungsmessung einer großen Partie.

## Bildnachweise

- [System mit größeren Abständen](ASTEROIDEN_1_0_14/asteroid-system.png)
- [Seitlich verschobene, symmetrische Korona](ASTEROIDEN_1_0_14/corona-panned.png)
- [Abbau mit tatsächlicher Teilfracht und Rücknahmehafen](ASTEROIDEN_1_0_14/asteroid-mining-OrbitalBridge.png)
- [Abbauverwaltung bei 1280 × 720](ASTEROIDEN_1_0_14/asteroid-mining-1280.png)
- [Abgeschlossene Rücklieferung](ASTEROIDEN_1_0_14/asteroid-delivered.png)

## Git-Integration

Versionsbranch und PR-Ziel sind `1.0.14` und `main`. Da die vorherigen Versions-PRs noch offen sind, enthält der PR gegen `main` außerdem die bereits lokal geprüften Änderungen seit 1.0.6. `main` stand vor dieser Lieferung auf `f4b86af` (1.0.5). Der vorherige [PR #13](https://github.com/bratkartoffeln/PlanetOne/pull/13) konnte wegen fehlender Unity-Aktivierungssecrets im CI nicht integriert werden. Der Status des neuen PR wird anhand seines eigenen Workflowlaufs geprüft und im PR festgehalten. Lokaler Prüferfolg und Remote-Merge bleiben getrennt; kein Admin-Bypass und keine Umgehung der erforderlichen Buildprüfung.
