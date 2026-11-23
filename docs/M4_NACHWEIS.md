# M4 Raumfahrt, Versorgung und Forschung

Stand: 23. November 2026, Produktversion **1.0.3**, Unity 6000.3.29f1, Speicherformat **4**. P08, P09 und P10 sind im beschriebenen Umfang umgesetzt und geprüft. M5-Raumkampf, reguläre Landungstransporter, Orbitalwerften und Invasionen sind weiterhin offen.

## Spielbares Verhalten

Ein angeschlossener Raumhafen baut Frachter und Kolonieschiffe aus tatsächlich angelieferten Waren, lokaler Besatzung und Produktionszeit. Ein Forschungslabor schaltet zunächst Raumtransport und Koloniemodule frei. Die Freischaltung erzeugt kein Schiff. Vier Bewohner bleiben als zivile Wiederaufbaubasis zurück. Fehlende Energie, Arbeit, Besatzung oder Waren halten den Bau auf. Ein freiwilliger Hafenrückbau gibt bereits bezahltes Schiffsmaterial am Standort und Besatzung zurück; militärische Zerstörung tut dies nicht.

Schiffe fliegen auf derselben Ebene frei zu Kartenpositionen oder folgen bewegten Planeten und Monden. Zielpositionen werden jeden festen Simulationsschritt neu berechnet. Bewachen hält den Bezug zur Welt; Eskorte folgt einem eigenen Schiff. Der Wechsel zur Oberfläche verändert weder Flug noch Produktion. Sicht beruht auf eigenen Kolonien und Schiffssensoren. Unbekannte Welten werden durch tatsächliche Annäherung entdeckt; erkundete Bahnen bleiben bekannt. Verdeckte gegnerische Schiffe werden nur als letzte Sichtung mit altem Ort und Zeitpunkt angezeigt. Fremde Oberflächen werden nicht als uneingeschränkte Inventaransicht geöffnet.

Ein angekommenes Kolonieschiff gründet nur auf einer unbesiedelten, geeigneten Welt. Es wird mit unveränderlicher Kennung als verbraucht markiert. Seine zwölf Bewohner und sein tatsächliches Frachtmanifest gehen genau einmal auf Verwaltung und Startlager über. Der vorläufige Gründungsplatz liegt bei `(8,6)`; das Koloniemodul entfaltet dort Verwaltung und grundlegende Straßen. Wiederholte, entfernte oder fremde Gründungen werden abgewiesen.

Frachter besitzen 40 Plätze. Ein Auftrag reserviert tatsächlich vorhandene Quellware oberhalb der Exportreserve und freien Zielraum. Quellbestand, lokale Lieferungen, geplante Raumlieferungen und bereits geladene Fracht verwenden dieselben Bestands- und Reservierungsregeln. Teilmengen sind erlaubt. Beladung und Entladung dauern je 0,5 Sekunden pro Ware; ein Hafen hat einen gleichzeitig nutzbaren Transferanschluss. Bewegung und Hafenbetrieb sind sichtbar getrennt: ein Schiff kann angekommen sein und wegen Energie, Arbeit, Straßenanschluss oder belegtem Anschluss warten.

Einmalige Lieferungen, wiederholte feste Routen und ein automatischer Frachterpool sind bedienbar. Je Hafen und Gut gelten Mindestbestand, Zielbestand, Exportreserve und Priorität. Unterhalb der Nachfüllschwelle wird bis zum Ziel aufgefüllt; zugesagte lokale und interplanetare Lieferungen zählen mit. Nahrung hat anfangs hohe Priorität. Lokale Kuriere holen Pufferbestände aus erreichbaren Lagern; das interplanetare Netz ergänzt fehlende Mengen. Die Exportreserve schützt ausschließlich Fernexporte und sperrt keine örtliche Grundversorgung. Verschiedene Waren erhalten nacheinander eigene Frachtaufträge; es gibt noch keine gemischten Manifeste pro Fahrt.

Bei Abbruch vor Beladung werden nur Reservierungen gelöst. Nach Beladung bleibt die Ware auf dem Schiff und wird physisch zum nächsten eigenen Hafen mit genügend Platz zurückgebracht. Ohne geeigneten Hafen bleibt sie an Bord. Ein verlorener Zielhafen erhält keine Ware des bisherigen Eigentümers. Schiffsverlust verbucht die tatsächliche Ladung genau einmal als Verlust. Der Verlustpfad ist automatisiert geprüft; ein reguläres Raumgefecht als Auslöser folgt in P11.

Khepri besitzt ein endliches, violett dargestelltes Kristallvorkommen. Die Kristallmine benötigt die entsprechende Forschung und arbeitet nur auf diesem Vorkommen. Das Elektronikwerk verarbeitet zwei Metall und einen Kristall zu einer Elektronik. Physische Importe können Forschung und Produktion direkt über lokale Kuriere versorgen. Straßen zwischen den tatsächlichen Quell- und Zielanlagen sind erforderlich; eine Verbindung beider Anlagen zur Verwaltung allein garantiert keinen Weg zwischen getrennten Straßenzweigen.

## Forschung und vorläufige Balance

| Technologie | Voraussetzungen | Material im Labor | Zeit | Ergebnis |
|---|---|---|---|---|
| Raumtransport | Keine | 4 Metall, 2 Bauteile | 12 s | Frachter |
| Koloniemodule | Raumtransport | 6 Metall, 3 Bauteile | 16 s | Kolonieschiffe |
| Kristallabbau | Raumtransport | 4 Metall, 2 Bauteile | 14 s | Kristallmine |
| Elektronik | Koloniemodule und Kristallabbau | 4 Metall, 2 Bauteile, 4 Kristalle | 20 s | Elektronikwerk |
| Fortgeschrittene Sensoren | Elektronik | 4 Bauteile, 4 Elektronik | 20 s | Unbewaffnetes Sensorschiff mit größerer Sicht |

Ein Forschungsprojekt je Fraktion besitzt ein ausführendes Labor. Material wird dort einmal verbraucht. Laborverlust pausiert den vorhandenen Fortschritt; derselbe Auftrag kann einem neuen eigenen Labor zugeordnet werden. Bereits bezahlte Proben werden nicht erneut verlangt. Abgeschlossene Technologien gelten reichsweit. Definitionsprüfung weist doppelte Kennungen, fehlende Voraussetzungen, negative Kosten und Zyklen zurück. Dies ist die kurze verzweigte M4-Folge, noch nicht der endgültige Technologiebaum aller fünf Forschungsrichtungen.

| Schiff | Kosten | Besatzung | Bauzeit | Geschwindigkeit / Sicht |
|---|---|---|---|---|
| Frachter | 4 Nahrung, 16 Metall, 6 Bauteile | 2 | 12 s | 6 / 5 |
| Kolonieschiff | 44 Nahrung, 100 Metall, 32 Bauteile | 12 | 20 s | 5 / 6 |
| Sensorschiff | 4 Nahrung, 16 Metall, 6 Bauteile, 4 Elektronik | 2 | 14 s | 8 / 12 |

Das Kolonieschiff nimmt aus diesen Kosten **40 Nahrung, 80 Metall und 20 Bauteile** als physische Startladung mit; nur der übrige Anteil ist verbrauchter Schiffsbau. Gründung erzeugt keine zusätzliche Materialgutschrift. Definitionen liegen in `SpaceState.cs` und `Definitions.cs`, nicht in der Oberfläche. Zahlen bleiben vorläufige Balancewerte.

## Bedienung und reproduzierbarer Start

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1 -Visible
& .\Builds\Windows\PlanetOne.exe
```

Unter **Raumfahrt und Forschung** stehen die Reiter **Flotte**, **Forschung** und **Logistik** auch während einer Oberflächenansicht bereit. Gebäude werden weiterhin unter **Bauen** errichtet. Der Forschungsstandort ist bevorzugt ein Labor auf der geöffneten eigenen Oberfläche, sonst das erste eigene Labor.

- **Flotte:** Schiff anklicken oder in der Liste wählen, Zielwelt wählen, Hinflug/Bewachen/Gründen beauftragen. Rechtsklick auf einen Planeten fliegt dorthin; Rechtsklick in den freien Raum setzt eine Position zur Erkundung. Rahmen und Umschalt erweitern die Auswahl. Strg+1 bis Strg+3 speichern Schiffsgruppen; 1 bis 3 wählen sie wieder. Mehrere ausgewählte Schiffe können dem zuerst ausgewählten Schiff als Eskorte folgen.
- **Forschung:** Voraussetzungen und örtliche Materialkosten lesen, Forschung beginnen oder nach Laborverlust am neuen Standort fortsetzen.
- **Logistik:** Quellhafen und Ware wählen, Mindestbestand/Ziel/Exportreserve und Priorität speichern. Frachter und Zielkolonie wählen, dann einmalige Lieferung, feste Route oder automatisches Netz beauftragen. Die Anzeige trennt Hafenbestand, zugesagte Lieferung, reservierten Export und den aktuellen Schiffsstatus.
- **Pause und Speicherstand:** Leertaste, F5 und F9 verwenden dieselben gemeinsamen Zustände wie Wirtschaft und Bodengefechte.

Die Schaltfläche **M4 Aufbau und Raumfahrt** erzeugt eine ausdrücklich gekennzeichnete Demonstrationssitzung. `SpaceFixture.Create` startet mit der normalen Karte und entwickelt sie ausschließlich durch reguläre Bau-, Forschungs- und Schiffsbefehle sowie Simulationszeit. Es werden keine Schiffe, Forschungsfreigaben oder Zusatzwaren hineingesetzt. Der vorbereitete Stand nach ungefähr 27 Simulationsminuten besitzt die Grundwirtschaft, Labor, Lager, Hafen, die ersten drei Technologien und je einen Frachter und ein Kolonieschiff. Er pausiert vor der Besiedlung Khepris. Die zweite ursprüngliche Kolonie bleibt aus dem bisherigen Prototyp erhalten; Khepri wird zur zusätzlichen selbst finanzierten Kolonie.

Die bisherige Sitzung bleibt im Speicher erreichbar. Die Demonstration speichert separat nach `space-fixture-save.json`; M3 behält `military-fixture-save.json`, normales Spiel `prototype-save.json`. Ein Programmneustart erhält die vorherige normale Sitzung nur, wenn sie vorher normal gespeichert wurde. Der Grafiktest verwendet ausschließlich seinen eigenen Testordner.

## Nachweise und Regelzuordnung

**Core: 58 bestandene Verhaltenstests**, davon 15 neue M4-Fälle in `Tests/Core/SpaceTests.cs`. Die bisherigen 43 Wirtschafts- und Militärtests bestehen weiter. Neue Fälle prüfen bezahlten Schiffsbau, Besatzung und Freigaben, bewegte Monde/Eskorte, freie Erkundung und Sichtgrenzen, eindeutige Koloniegründung, konkurrierende Teilmengen und Exportreserven, Transferdauer und Hafenleistung, Abbruch vor/nach Beladung, Verlust und fremden Zielhafen, automatische Nachfüllschwellen, feste Routen, Forschung bei Laborverlust, Graphfehler, volles Ziellager, Rückbau eines bezahlten Schiffsauftrags, Migration und identische Save/Load-Fortsetzung.

Der durchgängige Wirtschaftstest startet aus dem regulären Anfangszustand, finanziert Ausbau und Schiffe, gründet Khepri, baut dort Grundversorgung, Raumhafen und Kristallmine, liefert Kristalle physisch zur Heimatwelt, erforscht Elektronik, betreibt das Elektronikwerk und baut nach Sensorforschung ein Sensorschiff. Gemessene Produktionssummen am Ende: **606 Metall, 167 Bauteile, 50 Kristalle und 8 Elektronik**. Es sind kumulierte Erzeugungsmengen dieses Tests, keine geschenkten Anfangsbestände und kein verbleibendes Reichslager.

**Windows-Build erfolgreich, Exitcode 0. Grafiklauf: 91 bestandene Einzelprüfungen plus eine PASS-Gesamtzeile**, gespeichert in `TestResults/Smoke/20261123-202734`. Elf Renderbilder wurden visuell kontrolliert: die bisherigen vier Wirtschafts- und drei Militärbilder sowie `space-fleet.png`, `space-colony.png`, `space-logistics.png`, `space-research.png`. Die neuen Controller-Prüfungen zeigen bezahlten Aufbau, sichtbare Schiffe, Flug mit Ansichtswechsel, Speichern während des Flugs, tatsächliche Gründung, örtliche Hafenbelieferung, reale interplanetare Lieferung und exakte abschließende Speicherung. Die Seitenleisten wurden nach sichtbarem Breitenüberlauf mit schmalen Reitern korrigiert; Schiffsbeschriftungen gleicher Position werden gestaffelt.

**Native Windows-Bedienung:** Demonstration über die Schaltfläche gestartet; Kolonieschiff ausgewählt; Flug in Pause vorgemerkt und mit Leertaste gestartet; währenddessen zur Mondoberfläche gewechselt; nach Ankunft tatsächlich gegründet; Pause und F5/F9 verwendet. Elektronikforschung über den Reiter beauftragt und korrektes Warten auf noch fehlende Proben beobachtet. Eine Metall-Versorgungsregel mit Minimum 8, Ziel 24 und Exportreserve 12 gespeichert; die Anzeige bestätigte die Regel und 24 örtlich zugesagte Waren. In einem weiteren Lauf Frachter per Kartenklick gewählt, freien Flug per Rechtsklick beauftragt, Bewegung bis **Position erreicht** beobachtet und beide eigenen Schiffe gemeinsam per Rahmen ausgewählt. Gruppenwiederwahl und jede mögliche Eingabekombination wurden nicht zusätzlich nativ durchgetestet. Die vollständige automatische Kristall-/Elektronikkette ist ein Core-Nachweis; der grafische Lieferlauf ein Controller-Nachweis.

| Paket | Regeln / Szenarien | Ergebnis |
|---|---|---|
| P08 | SPACE-001–003, SPACE-005, SPACE-008–009, CORE-007–009; TEST-005, Sichtanteil TEST-011 | Schiffe, freie Bewegung, bewegte Ziele, Eskorte, Sensorwissen und bezahlte Gründung |
| P09 | LOG-001–006, LOG-008, LOG-012–013, ARCH-008–011; TEST-003–005 | Physische reservierte Fracht, Hafenleistung, automatische und feste Routen, Abbruch/Verlust |
| P10 | ECO-001–006, RES-001–007; aktueller Forschungsanteil TEST-006 | Endlicher Kristallabbau, Elektronik, verzweigter Graph, örtliche Proben und reichsweite Freigaben |
| Persistenz | ARCH-014; erweiterter Teil TEST-020 | Format 4 mit V1 → V2 → V3 → V4, Waren-/Kapazitätsprüfung, Forschungs- und Flugfortsetzung |

## Grenzen und anschließende Pakete

Die erste Frachtplanung verwendet einen Warentyp pro Fahrt und priorisierte Bedarfsauswertung, keine globale Routenoptimierung. Der Hafenpuffer konkretisiert die Kolonie-Versorgungsregel; Reichssummen bleiben reine Anzeigen. Es gibt keinen Treibstoffverbrauch. Eskorte und Bewachen bewegen sich bereits physisch, bewaffnete Reaktion, reale Raumblockaden, Gefahrenumwege und begleitete Konvoikämpfe benötigen P11. Handelsrechte mit fremden Fraktionen folgen P16. Das Sensorschiff ist unbewaffnet und ersetzt keine P11-Kriegsschiffklasse.

Auswahlgruppen gehören zur lokalen Darstellung und werden beim Laden neu gebildet. Das neue Speicherformat bewahrt die autoritativen Aufträge und Frachten; alte Formate erhalten leere Raumfahrt-/Forschungszustände und ein geeignetes Kristallvorkommen auf dem bislang neutralen Mond. Zusätzliche Plattformen, große Lastprofile, vollständige Fraktions-KI und die gesamte Partie sind weiterhin ungeprüft. Die ältere vollständige manuelle Wirtschaftsabnahme bleibt getrennt offen.
