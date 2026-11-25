# Planetentypen, Oberflächen und Luftfracht – 1.0.13

Stand: 25. November 2026. Zusätzlicher Nutzerauftrag D-032 auf Basis 1.0.12; kein Abschluss der Gesamtprüfung M8. Regeln SPACE-006/009/010, ECO-014, GUI-009 und TEST-028. Unity 6000.3.29f1, Windows, Built-in-Renderer; Savegame-Schema 10.

## Recherche und daraus abgeleitete Spielentscheidungen

NASA unterscheidet terrestrische, Supererden-, Neptun- und Gasriesenklassen anhand ihrer physikalischen Eigenschaften. Wüste, Bewuchs oder Ozean sind hier zusätzliche Oberflächenzustände einer felsigen Welt, keine gleichwertigen astronomischen Größenklassen. Eine Eiswelt mit festem Untergrund ist insbesondere kein Eisriese wie Uranus. [NASA: Planet types](https://science.nasa.gov/exoplanets/planet-types/).

Die habitable Zone hängt von der Helligkeit des Sterns ab. Ihre Lage allein beweist weder eine erhaltene Atmosphäre noch flüssiges Oberflächenwasser oder Leben. Im Spiel ist Flora eine bewusste Science-Fiction-Annahme. [NASA: Habitable zone](https://science.nasa.gov/exoplanets/habitable-zone/).

Albedo und Atmosphäre beeinflussen die Energiebilanz. Die Erde ist durch den natürlichen Treibhauseffekt mehr als 30 Grad wärmer als eine entsprechende Welt ohne Atmosphäre. Der Prototyp verwendet deshalb einen einfachen Richtwert mit unterschiedlichen Albedos und festen Atmosphärenzuschlägen, kein vollständiges Klimamodell. [NASA: Climate and Earth’s Energy Budget](https://science.nasa.gov/earth/earth-observatory/climate-and-earths-energy-budget/), [NASA Exoplanet Archive: Gleichgewichtstemperatur und Modellannahmen](https://exoplanetarchive.ipac.caltech.edu/docs/TableColumnDescriptors.html).

Jupiter besitzt keine feste begehbare Oberfläche, ist ungefähr elfmal so groß wie die Erde und umkreist die Sonne in rund 5,2 AE Abstand. Seine schnelle Eigenrotation und gebänderte Wolkenstruktur dienen als Referenz für Zephyr. Aktiv stabilisierte, geschlossene Plattformen und Schwebetriebwerke sind erfundene Spieltechnik; insbesondere wird kein technisch nachgewiesenes schwebendes Gasriesenhabitat behauptet. [NASA: Jupiter facts](https://science.nasa.gov/jupiter/jupiter-facts/).

## Konfiguration

Der Zentralstern besitzt im Referenzmodell eine Sonnenleuchtkraft. Für primäre Planeten gilt `a_AE = OrbitRadius / 10`, `S/S_Erde = 1/a_AE²` und `T_eq = 278,3 K × (1 − Albedo)^(1/4) / sqrt(a_AE)`. Der angezeigte Klimarichtwert addiert einen festen Atmosphärenzuschlag. Das ist eine Plausibilitätszuordnung der Startwelten; die Biomwahl verändert sich nicht automatisch während einer Partie. Gas-Innenwärme, Wetter, Jahreszeiten und Atmosphärenverlust werden nicht simuliert.

| Welt | Typ | Sternabstand, AE | Radius, Erdradien | Albedo / Zuschlag | Gelände |
|---|---|---:|---:|---|---|
| Sahra | Wüste | 0,72 | 0,82 | 0,30 / 15 K | Warme Dünen und trockene Rücken |
| Aster | Bewachsen | 1,00 | 1,00 | 0,30 / 33 K | Sanfte Ebenen, Hügel, Waldgruppen |
| Pelagia | Ozean | 1,25 | 1,25 | 0,23 / 45 K | Meer, sandige Küsten, bebaubare Inseln |
| Nereid | Eis | 1,80 | 0,90 | 0,60 / 12 K | Eistexturen, Schneehöhen, gefrorene Felsen |
| Khepri | Felsmond | folgt Nereid: 1,80 | 0,28 | 0,12 / 0 K | Luftloses Gestein, Kristallvorkommen |
| Helios, Diplomatie-Szenario | Kalte Wüste | 2,50 | 0,82 | 0,30 / 15 K | Trockene, kalte Fels-/Sandflächen |
| Zephyr | Gasriese | 5,20 | 9,50 | 0,50 / kein Oberflächenwert | Wolken unter einer schwebenden Stationsregion |

Radien in der Informationskarte sind physikalische Referenzwerte. Sichtbare Kugelgrößen werden für Lesbarkeit komprimiert: Gasriesen sind sichtbar größer, Monde kleiner; der Stern wird ebenfalls gesondert dargestellt. Die bestehenden großen Mondbahnen sind Darstellungs-/Spielmaßstab und keine astronomische AE-Skalierung. Keplerperioden und Exzentrizitäten bleiben gemeinsam simuliert. Eigenrotation: Gas 240 Sekunden, bewachsen/Ozean/Eis 600 Sekunden, Wüste 900 Sekunden, Fels 1.200 Sekunden. Pause hält die Darstellung an.

## Tatsächliche Spielregeln

- Neue Welten haben **28 × 22 = 616 Waben**, vorher 252. Alte Spielstände behalten ihre gespeicherten Maße und Infrastruktur. Die Höhenfelder haben verschiedene Formen; kein gemeinsamer Randwall und kein obligatorisches mittiges Tal.
- Wasserfelder blockieren Bauen, Straßen und Bodeneinheiten. Land-/Wasserzuordnung und sichtbare Küste stammen aus demselben kontinuierlichen Höhenfeld. Noch keine Seefahrzeuge oder Brücken; die bebaubare Startinsel hat reguläre Erzstellen und Versorgungswege.
- Gaswelten besitzen weder Straßen noch Erz-/Kristallvorkommen, Bodenminen, Mauern oder Straßentore. Der Core lehnt solche Befehle und ungültige gespeicherte Infrastruktur ab. Oberflächenmodelle schweben auf Plattformen; Lieferer und Truppen werden als Fluggeräte ohne Räder dargestellt. Die bestehenden taktischen Werte und Zielebenen bleiben erhalten; dies ist keine zusätzliche Luftkampfsimulation.
- Vier lokale Frachtdrohnen tragen je acht Waren, bewegen sich mit drei Hexsegmenten pro Sekunde und benötigen je 0,5 Sekunden für Laden/Entladen. Flugkorridore können andere Plattformen überqueren. Ladung wird real reserviert, geladen, geliefert und zurückgeführt. Baustellen, Abbruch und Save/Load nutzen die bisherigen Integritätsregeln. Bei Verlust oder Eroberung gelten weiterhin die gemeinsamen Logistikregeln.
- Ein Kolonieschiff überträgt auf einer Gaswelt genau seine echten Vorräte und seine Besatzung; es entstehen keine zusätzlichen Erzquellen. Der Raumhafen ermöglicht reguläre Importe von Erz, Metall und weiteren Waren. Hydroponik bleibt geschlossen; Energie- und Produktionsrezepte sind bestehende Spielbalancewerte, keine nach Sternabstand neu berechneten Kraftwerksleistungen.

## Bedienung und Darstellung

Rechtsziehen verschiebt die Oberfläche ab sechs Pixeln. Ein kurzer Rechtsklick sendet einen Einheitenbefehl erst beim Loslassen; in einem Bau-/Zielmodus beendet er diesen. Ziehen erteilt keinen Befehl. WASD/Pfeile verschieben, Q/E beziehungsweise mittlere Maustaste drehen/neigen, Mausrad zoomt, Pos1 kehrt zum Koloniebereich zurück. Alle drei Layouts bleiben wählbar.

Die fehlende Geländetextur hatte eine konkrete Ursache: Das Relief lag auf Ebene 29, die die Weltkamera seit der getrennten Hintergrundkamera ausblendete. Das sichtbare Gelände liegt jetzt auf Ebene 0. Eigene Biomatlanten liefern Boden- und Planetentexturen. Gaswolken sind unbeleuchtet weich dargestellt; das unsichtbare Auswahlhöhenfeld ist kein sichtbarer Boden. [Assets und vollständige Prompts](ART_ASSETS.md).

**Spielmenü → Testszenarien → Planetentypen / Luftfracht** öffnet eine getrennte Prüfkarte. Sie setzt ausdrücklich Kolonien und je 60 Nahrung, 100 Metall und 40 Bauteile als Testanfangswerte. Solaranlagen und Hydroponik werden danach über gewöhnliche Befehle tatsächlich bezahlt und beliefert. Diese Testvorräte werden niemals in reguläre Partien eingefügt. Rückkehr über das Spielmenü zur vorherigen Sitzung; normale Partien enthalten die zusätzlichen neutralen Welten und müssen sie regulär erkunden und besiedeln.

## Reproduzierbare Prüfung

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1 -Width 1280 -Height 720
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1
```

Die GUI-Prüfung verwendet `CreateDesktop` und `CreateProcess` auf einem separaten Windows-Desktop, ohne `SwitchDesktop`, Screenreader, Computer-Use oder globale Eingabesimulation. Der fensterlose GPU-Test rendert die Welt separat; IMGUI wird ausschließlich durch die isolierten GUI-Läufe visuell nachgewiesen. Keine neue VM erforderlich.

Core: **140 Prüfungen bestanden**, einschließlich echter V9-Migration, Flugfracht/Bauabbruch und Wasserregeln. Die ursprüngliche Datei `prototype-v9.json` stammt aus dem isolierten 1.0.12-Playerlauf `20261124-224010`; die Tests vergleichen sämtliche Altfelder und bewahren damit Waren, Karten und Bahnen.

Unity-Windows-Build und Versionsprüfung 1.0.13 erfolgreich. Je **296 Player-Prüfbedingungen und 80 PNGs** in den drei abschließenden Läufen:

| Lauf | Verzeichnis unter `TestResults` | Ergebnis |
|---|---|---|
| Vollständige GUI, 1440 × 900 | `Presentation/20261125-085339` | Bestanden |
| Vollständige GUI, 1280 × 720 | `Presentation/20261125-085448` | Bestanden |
| Fensterloser GPU-Lauf | `Smoke/20261125-085516` | Bestanden; Welt ohne IMGUI |

Die Ergebnisdateien enthalten zusätzlich eine übergeordnete PASS-Zeile. Keine Exceptions, GUILayout-Fehler oder Shaderwarnungen in den finalen Playerlogs. Der ausgewählte Panzer erhält beim Rechtsziehen keinen Befehl und bei einem kurzen Rechtsklick genau einen Befehl beim Loslassen. Oberflächenhöhe, Kollisionsauswahl, Geländesichtbarkeit und vollständige Zustandserhaltung sind automatisiert geprüft. Alle sechs Biome wurden im tatsächlichen Player aufgenommen und die Bilder visuell kontrolliert. Zusätzlich wurden gezackte Küsten und harte Wolkenschatten nach der Bildkontrolle korrigiert.

### Bildnachweise aus dem finalen Player

| Ansicht | Unverändertes Spielbild |
|---|---|
| Gesamtes System | [Planetentypen und Abstände](PLANETEN_1_0_13/planet-types-system.png) |
| Sahra | [Wüstentextur und Dünen](PLANETEN_1_0_13/biome-Desert.png) |
| Aster | [Bewachsene Landschaft](PLANETEN_1_0_13/biome-Temperate.png) |
| Pelagia | [Wasser, Küste und Insel](PLANETEN_1_0_13/biome-Ocean.png) |
| Nereid | [Verschobene und gedrehte Eiswelt](PLANETEN_1_0_13/biome-panned-Ice.png) |
| Khepri | [Felsmond und Kristalle](PLANETEN_1_0_13/biome-Rocky.png) |
| Zephyr | [Schwebende Stationen und Luftfracht](PLANETEN_1_0_13/gas-air-freight.png) |

Die lokale Abnahme ist abgeschlossen. Git-Integration erfolgt über den Versionsbranch `1.0.13` mit Ziel `main`; ein Merge setzt bestandene Pflichtchecks voraus. Der aktuelle PR-/CI-Status wird am Versions-PR dokumentiert. Keine vollständige manuelle Spielabnahme, pixelgleiche Referenzabnahme oder zusätzliche Plattformzertifizierung behauptet.
