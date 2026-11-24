# GUI und Grafik – Version 1.0.9

Historischer Lieferstand. Die Nutzervorgabe wurde danach ausdrücklich um bildgetreue Umsetzung der Konzepttafel, natürliches Relief und vollständig räumliche Modelle erweitert. Maßgeblich sind D-029 sowie [Version 1.0.10](3D_GRAFIK_NACHWEIS.md).

Auftrag vom 24. November 2026: die gesamte Oberfläche und Grafik überarbeiten, Materialien/Texturen ergänzen und alle drei vorgestellten Layouts in den Einstellungen auswählbar machen. Dies ist eine zusätzliche Präsentationslieferung auf M7, kein vorgezogener Abschluss von M8.

## Bedienung

**Einstellungen → Darstellung** bietet drei sofort umschaltbare Layouts:

| Layout | Anordnung |
|---|---|
| A – Kommandozentrale | Rechte Bau-/Verwaltungsleiste mit taktischer Übersicht; kompakte Befehle unten. |
| B – Orbitale Brücke | Kolonien und eigene Flotten links, Kontext rechts, taktische Übersicht und Befehlsdeck unten. |
| C – Freier Weltraum | Breite Spielkamera, schwebende Werkzeugleiste und ausblendbare Kontextkarte. „Details ein/aus“ schaltet die Karte; eine Objektwahl öffnet sie wieder. |

Die obere Navigation erreicht Übersicht, Bauen, Einheiten, Flotte, Forschung, Logistik und Diplomatie. „Übersicht“ zeigt die Verwaltung des aktuellen Orts; „System“ wechselt ins Sonnensystem. Der Baukatalog gliedert sich in Aufbau, Industrie und Militär und zeigt aus den echten Gebäudemodellen gerenderte Vorschaubilder mit Kosten. Bestehende Produktions-, Forschungs-, Militär-, Fracht- und Vertragsbefehle bleiben an dieselben validierten Controllerpfade gebunden.

Das Spielmenü bietet Fortsetzen, Speichern/Laden, Einstellungen, neue Partie und Beenden. Escape schließt zuerst einen offenen Dialog beziehungsweise beendet einen Bau-/Zielmodus, sonst öffnet es das Menü. Menü und Einstellungen pausieren nicht automatisch; die ausdrücklich gewählte Simulationspause bleibt erhalten. Geöffnete Dialoge sperren Tastaturbefehle, Weltklicks und die darunterliegende GUI.

Layout, Wabengrenzen und Oberflächengröße werden lokal unter `Application.persistentDataPath/presentation.json` gespeichert, unabhängig von Spielständen. Beschädigte Einstellungen fallen auf verwendbare Vorgaben zurück; Schreibfehler werden angezeigt. Das Savegame-Schema bleibt 8. Die Skalierung begrenzt die gewählte Größe anhand der Bildschirmabmessungen; kompakt/normal/groß stehen zur Auswahl.

## Grafischer Umfang

- Eigener Materialatlas für Regolith, Metall, Beton/Keramik und Solarzellen; kontinuierliche Weltkoordinaten auf Gelände, objektgebundene Texturen an beweglichen Modellen.
- Die sechsseitige Bau-/Bewegungsgeometrie bleibt unverändert. Wabengrenzen sind als zuschaltbare Shaderanzeige vom Bodenmaterial getrennt.
- Gebäudemodelle erhalten Fenster, Eingänge und funktionsbezogene Aufbauten: Tanks, Farmkuppeln, Bohrtürme, Feldspulen, Hangars, Frachtmodule und Landebaken. Schiffsrümpfe erhalten Brücke, Flügel und Triebwerke; Fahrzeuge Laufwerke, Infanterie Helm/Ausrüstung.
- Planetare Land-/Wasserflächen, Wolken und Polkappen sowie Mondoberflächen, Sternstruktur und zurückhaltender Nebelhintergrund werden prozedural im Shader erzeugt. Bekannte Objekte verwenden weiterhin die vorhandenen Sichtregeln.
- Sämtliche Verwaltungsbereiche nutzen die gemeinsame Schrift-, Schaltflächen-, Eingabe- und Panelgestaltung. Die GUI bleibt Unity-IMGUI; es wird keine zweite Bedienlogik eingeführt.

Die damalige Lieferung war eine stilisierte, geometrische Echtzeitdarstellung. Die anschließende Nutzervorgabe D-029 verlangt ausdrücklich die Bildtreue zur Konzepttafel und räumlich drehbare Modelle; die vereinfachte Interpretation als reine Stilrichtung ist damit überholt. Die Konzepttafel selbst ist weiterhin kein Screenshot des implementierten Players.

## Referenzen und Herkunft

Die vom Nutzer bestätigten Referenzen sind **Command & Conquer, Haegemonia und Solar 2**. Für C&C wurde die klassische Seitenleiste samt Remastered-Verbesserungen zugrunde gelegt:

- [C&C-Entwicklerbeitrag zur Seitenleiste](https://www.reddit.com/r/commandandconquer/comments/bx59ym/remaster_update_and_first_sidebar_preview/): Baukategorien und reduzierte Scrollwege.
- [Haegemonia – offizielle Produktseite und Bilder](https://store.steampowered.com/app/294770/): Raumschiffmaterialien, planetare Darstellung und technische Oberflächenästhetik.
- [Solar 2 – offizielle Produktseite und Bilder](https://store.steampowered.com/app/97000/): klare Himmelskörper und großzügige Systemansicht.

Es wurden keine Spielgrafiken dieser Titel übernommen. Der Atlas `Assets/PlanetOne/Runtime/Resources/SurfaceAtlas.png` wurde mit dem eingebauten Imagegen-Werkzeug eigens für PlanetOne erzeugt. Geometrie, Symbole aus Gebäuderenderings, GUI-Mikrotexturen und prozedurale Weltoberflächen entstehen im Projekt. [Vollständiger Atlas-Prompt](ART_ASSETS.md).

## Reproduzierbare Prüfungen

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1 -Width 1280 -Height 720
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
```

`Test-Prototype.ps1` prüft den Player im echten Batchmodus mit GPU-RenderTexture; diese Bilder enthalten keine GUI. `Test-Presentation.ps1` startet dagegen einen regulär rendernden Player auf einem **separaten Windows-Desktop**, schaltet niemals dorthin um und sendet keine globalen Eingaben. Der Player erzeugt dort selbst Screenshots einschließlich IMGUI. Der Launcher wartet auf genau seinen Prozess und räumt Prozesshandles/Desktop auch im Fehlerfall auf. Eine VirtualBox-VM oder Installation ist dafür nicht nötig.

Die automatisierten Prüfungen umfassen die bisherigen M1–M7-Abläufe sowie Layoutwechsel, erneutes Laden der Einstellungen, ungültige Einstellungen, Kamera-/Hex-Picking, Eingabeschnittstellen bei geöffneten Einstellungen, unveränderten vollständigen Simulationszustand und echte Gebäudekatalog-Renderings. Sechs Layout-/Weltbilder plus Einstellungsdialog werden zusätzlich zu den bisherigen Spielbildern geschrieben.

### Ergebnisse

126 Core-Tests bestanden. Windows-Build und Versionsprüfung für 1.0.9 bestanden. Der endgültige fensterlose Playerlauf `TestResults/Smoke/20261124-155126` und der isolierte GUI-Lauf `TestResults/Presentation/20261124-155114` bestanden jeweils **159 Einzelprüfungen** (160 PASS-Zeilen einschließlich Gesamtergebnis). Je Lauf wurden **49 PNGs** erzeugt: 18 bisherige Spielansichten, sechs Layoutansichten, Einstellungen und 24 Gebäudekatalogbilder.

Echte GUI-Bilder bei 1440 × 900 wurden unter `TestResults/Presentation/20261124-154625` geprüft; nach den letzten Kontextauswahl-/Eingabefeldkorrekturen wurde der endgültige Player zusätzlich bei **1280 × 720** geprüft. Baukatalog, Brückenlayout, freie Systemansicht und Einstellungsdialog wurden anhand der erzeugten Playerbilder kontrolliert. Kein `GUI Error`, keine Shaderfehler oder Exceptions im endgültigen GUI-Log. Die korrigierten Befunde waren doppelte Navigation, schwacher Hilfstextkontrast, nicht zurückgesetzte Randflächen beim Layoutwechsel und fehlende Sperrung der GUI unter Dialogen.

Der erste sandboxierte Editorversuch erreichte den vorhandenen Unity-Lizenzdienst nicht. Der ausschließlich eigene festhängende Batchprozess wurde beendet; die nachfolgenden autorisierten Builds mit Zugriff auf den lokalen Lizenzdienst liefen erfolgreich. Bestehende VirtualBox-VMs wurden nur inventarisiert, nicht gestartet oder verändert.

### Grenzen

Die Bildkontrolle der echten GUI ersetzt keinen vollständigen manuellen Spieldurchlauf mit physischer Maus und Tastatur. Die automatisierten Abläufe rufen die Controllerpfade auf; sie behaupten keine manuelle Schaltflächenbedienung. Ungeprüft bleiben andere Betriebssysteme, High-DPI-/Mehrmonitor-Konfigurationen und das M8-Lastprofil. Keine Tests verwenden Screenreader, Computer-Use-Skill oder Eingaben auf dem Arbeitsdesktop.

Git-Integration erfolgt über den Versionsbranch. Ein fehlgeschlagener Unity-CI-Check wird nicht umgangen; eine offene CI-Sperre bleibt getrennt vom lokalen Entwicklungs- und Prüfstand dokumentiert.
